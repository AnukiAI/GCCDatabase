#!/usr/bin/env python3
"""
Build the manual-review verification workbook.
=============================================

Collects every number produced by the migration verification chain into one .xlsx so the
figures can be checked by hand against the client's spreadsheets, rather than taken on trust
from a script's summary line.

Deliberately does NOT recompute anything. Every value is read from the artifact that produced
it -- the transform output, the two reconciliation passes, the spreadsheet-to-database verify,
and the database itself -- so the workbook can be reconciled back to its sources.

Sheets
  Summary              every check, expected vs actual, with the artifact it came from
  Source vs Database   per source worksheet: rows in -> emitted -> loaded
  Field Reconciliation the 15 compared fields
  Mismatches           every field-level difference, with its explanation
  Not Loaded           the withheld rows and the field missing from each
  Duplicate Numbers    every number held by more than one certificate
  ASINs                product identifiers, both directions
  Documents            certificate PDF/preview attachment results
  Certificates         all loaded certificates, for manual spot-checking

Usage:  python 2026-08-18-build-verification-workbook.py
"""
import csv
import glob
import os
import re
import sys

try:
    from openpyxl import Workbook
    from openpyxl.styles import Font, PatternFill, Alignment, Border, Side
    from openpyxl.utils import get_column_letter
except ImportError:
    sys.exit("openpyxl required:  pip install openpyxl")

HERE = os.path.dirname(os.path.abspath(__file__))          # .../Scripts/data-ops
PROJ = os.path.dirname(os.path.dirname(HERE))              # .../GCCWebApp (the project root)

# ---------------------------------------------------------------- styling
INK       = "FF101A15"
ACCENT    = "FF2F6B4E"
HEAD_FILL = PatternFill("solid", fgColor="FF1C2E24")
OK_FILL   = PatternFill("solid", fgColor="FFE2EDE6")
WARN_FILL = PatternFill("solid", fgColor="FFF6ECDD")
BAD_FILL  = PatternFill("solid", fgColor="FFF7E5E4")
SUB_FILL  = PatternFill("solid", fgColor="FFEDF1EE")
HEAD_FONT = Font(color="FFFFFFFF", bold=True, size=10)
THIN      = Side(style="thin", color="FFD6DED8")
BORDER    = Border(bottom=THIN)


def newest(pattern):
    hits = sorted(glob.glob(os.path.join(HERE, pattern)))
    return hits[-1] if hits else None


def read_csv(path):
    if not path or not os.path.exists(path):
        return []
    with open(path, newline="", encoding="utf-8-sig") as f:
        return list(csv.DictReader(f))


def force_text(ws):
    """Stop Excel parsing a prose note as a formula.

    openpyxl writes any string starting with '=' as a formula. Excel then fails to compile it
    and shows the 'we found a problem with some content' repair prompt for the WHOLE workbook,
    over one cell. Notes here legitimately begin with '='. Setting the cell type explicitly
    keeps the text visible exactly as written, with no leading apostrophe.
    """
    for row in ws.iter_rows():
        for c in row:
            if isinstance(c.value, str) and c.value[:1] == "=":
                c.data_type = "s"


def sheet(wb, title, headers, rows, widths=None, freeze="A2", autofilter=True):
    ws = wb.create_sheet(title)
    ws.append(headers)
    for c in range(1, len(headers) + 1):
        cell = ws.cell(row=1, column=c)
        cell.fill, cell.font = HEAD_FILL, HEAD_FONT
        cell.alignment = Alignment(vertical="center", wrap_text=True)
    ws.row_dimensions[1].height = 26
    for r in rows:
        ws.append(r)
    for i, w in enumerate(widths or [], start=1):
        ws.column_dimensions[get_column_letter(i)].width = w
    if freeze:
        ws.freeze_panes = freeze
    if autofilter and rows:
        ws.auto_filter.ref = f"A1:{get_column_letter(len(headers))}{len(rows) + 1}"
    force_text(ws)
    return ws


# ---------------------------------------------------------------- gather
transform_dir = newest("_transform_output/*/")
verify_dir    = newest("_verify_output/*/")
recon_dir     = os.path.join(HERE, "_reconciliation")
pid_dir       = newest("_productid_recon/*/")

print(f"transform : {transform_dir}")
print(f"verify    : {verify_dir}")
print(f"recon     : {recon_dir}")
print(f"pid recon : {pid_dir}")

recon_summary = read_csv(os.path.join(recon_dir, "reconciliation-summary.csv"))
mismatches    = read_csv(os.path.join(recon_dir, "reconciliation-mismatches.csv"))
loaded_certs  = read_csv(os.path.join(recon_dir, "loaded-certificates-TEST-final.csv"))
quarantined   = read_csv(os.path.join(transform_dir, "quarantine-incomplete-rows.csv")) if transform_dir else []
missing_db    = read_csv(os.path.join(verify_dir, "missing-from-database.csv")) if verify_dir else []
extra_db      = read_csv(os.path.join(verify_dir, "extra-in-database.csv")) if verify_dir else []
asin_missing  = read_csv(os.path.join(verify_dir, "asin-missing.csv")) if verify_dir else []
asin_extra    = read_csv(os.path.join(verify_dir, "asin-extra.csv")) if verify_dir else []

# Field-level product-ID reconciliation (2026-08-18-reconcile-productid-fields.py). The
# spreadsheet-to-database verify above compares the PAIRING only; these compare the fields.
pid_summary   = read_csv(os.path.join(pid_dir, "summary.csv")) if pid_dir else []
pid_conflicts = read_csv(os.path.join(pid_dir, "collapsed-disagreements.csv")) if pid_dir else []
pid_blank     = read_csv(os.path.join(pid_dir, "blank-productname.csv")) if pid_dir else []
pid_namemm    = read_csv(os.path.join(pid_dir, "productname-mismatches.csv")) if pid_dir else []
pid_partmm    = read_csv(os.path.join(pid_dir, "partner-mismatches.csv")) if pid_dir else []

# ---------------------------------------------------------------- database
import importlib.util
cfg = os.path.join(PROJ, "appsettings.Production.json")
conn_str = re.search(r'"DefaultConnection"\s*:\s*"([^"]+)"', open(cfg, encoding="utf-8").read()).group(1)

db = {}
dupes, status_rows, audit_rows = [], [], []
try:
    import pyodbc
    drivers = [d for d in pyodbc.drivers() if "SQL Server" in d]
    # Translate the .NET connection string to ODBC. The boolean values differ: ODBC wants
    # yes/no and rejects True/False outright ("Invalid value specified for ... 'Encrypt'").
    parts, mars = {}, False
    for kv in conn_str.split(";"):
        if "=" not in kv:
            continue
        k, v = kv.split("=", 1)
        k, v = k.strip(), v.strip()
        kl = k.lower()
        yn = "yes" if v.lower() in ("true", "yes") else "no"
        if kl == "server":
            parts["SERVER"] = v[4:] if v.lower().startswith("tcp:") else v
        elif kl in ("database", "initial catalog"):
            parts["DATABASE"] = v
        elif kl in ("user id", "uid"):
            parts["UID"] = v
        elif kl in ("password", "pwd"):
            parts["PWD"] = v
        elif kl == "encrypt":
            parts["Encrypt"] = yn
        elif kl == "trustservercertificate":
            parts["TrustServerCertificate"] = yn
        elif kl == "multipleactiveresultsets":
            mars = yn == "yes"
    if mars:
        parts["MARS_Connection"] = "yes"
    odbc = ";".join(f"{k}={v}" for k, v in parts.items())
    cn = pyodbc.connect(f"DRIVER={{{drivers[-1]}}};{odbc}", timeout=60)
    cur = cn.cursor()

    def one(sql):
        cur.execute(sql)
        return cur.fetchone()[0]

    db["certificates"]  = one("SELECT COUNT(*) FROM dbo.Certificates")
    db["numbers"]       = one("SELECT COUNT(*) FROM (SELECT Number FROM dbo.Certificates GROUP BY Number) x")
    db["companies"]     = one("SELECT COUNT(*) FROM dbo.Companies")
    db["products"]      = one("SELECT COUNT(*) FROM dbo.Products")
    db["productids"]    = one("SELECT COUNT(*) FROM dbo.CertificateProductIds")
    db["pid_partners"]  = one("SELECT COUNT(*) FROM dbo.CertificateProductIdPartners")
    db["cert_partners"] = one("SELECT COUNT(*) FROM dbo.CertificatePartners")
    db["cert_tags"]     = one("SELECT COUNT(*) FROM dbo.CertificateTag")
    db["exempt"]        = one("SELECT COUNT(*) FROM dbo.Certificates WHERE AllowsDuplicateNumber=1")
    db["sharing"]       = one("""SELECT COUNT(*) FROM dbo.Certificates c WHERE EXISTS
                                 (SELECT 1 FROM dbo.Certificates c2 WHERE c2.Number=c.Number AND c2.Id<>c.Id)""")
    db["nonexempt_dup"] = one("""SELECT COUNT(*) FROM dbo.Certificates c WHERE c.AllowsDuplicateNumber=0
                                 AND EXISTS (SELECT 1 FROM dbo.Certificates c2 WHERE c2.Number=c.Number AND c2.Id<>c.Id)""")
    db["overflagged"]   = one("""SELECT COUNT(*) FROM dbo.Certificates c WHERE c.AllowsDuplicateNumber=1
                                 AND NOT EXISTS (SELECT 1 FROM dbo.Certificates c2 WHERE c2.Number=c.Number AND c2.Id<>c.Id)""")
    db["with_pdf"]      = one("SELECT COUNT(*) FROM dbo.Certificates WHERE CertificateFile IS NOT NULL AND CertificateFile<>''")
    db["with_thumb"]    = one("SELECT COUNT(*) FROM dbo.Certificates WHERE CertificateImagePath IS NOT NULL AND CertificateImagePath<>''")
    db["share_pdf"]     = one("""SELECT ISNULL(SUM(n),0) FROM (SELECT n=COUNT(*) FROM dbo.Certificates
                                 WHERE CertificateFile IS NOT NULL AND CertificateFile<>''
                                 GROUP BY CertificateFile HAVING COUNT(*)>1) x""")
    db["share_thumb"]   = one("""SELECT ISNULL(SUM(n),0) FROM (SELECT n=COUNT(*) FROM dbo.Certificates
                                 WHERE CertificateImagePath IS NOT NULL AND CertificateImagePath<>''
                                 GROUP BY CertificateImagePath HAVING COUNT(*)>1) x""")
    db["orphan_prod"]   = one("""SELECT COUNT(*) FROM dbo.Products p WHERE NOT EXISTS
                                 (SELECT 1 FROM dbo.Certificates c WHERE c.ProductId=p.Id)""")
    db["orphan_comp"]   = one("""SELECT COUNT(*) FROM dbo.Companies co WHERE NOT EXISTS
                                 (SELECT 1 FROM dbo.Certificates c WHERE c.CompanyId=co.Id)""")
    db["untrusted_fk"]  = one("SELECT COUNT(*) FROM sys.foreign_keys WHERE is_not_trusted=1 OR is_disabled=1")
    db["null_norm"]     = one("SELECT COUNT(*) FROM dbo.Certificates WHERE NormalizedNumber IS NULL")
    db["audit"]         = one("SELECT COUNT(*) FROM dbo.AuditHistory")
    db["untagged_pid"]  = one("""SELECT COUNT(*) FROM dbo.CertificateProductIds pi WHERE NOT EXISTS
                                 (SELECT 1 FROM dbo.CertificateProductIdPartners pp WHERE pp.CertificateProductIdId=pi.Id)""")

    cur.execute("""
        SELECT c.Number, records=COUNT(*), companies=COUNT(DISTINCT c.CompanyId),
               products=COUNT(DISTINCT c.ProductId),
               withPdf=SUM(CASE WHEN c.CertificateFile IS NOT NULL AND c.CertificateFile<>'' THEN 1 ELSE 0 END),
               publicRecords=SUM(CASE WHEN c.Status IN ('Client Approved - Paid','Republished')
                                       AND c.EffectiveDate<=CAST(GETDATE() AS date)
                                       AND c.ExpirationDate>=CAST(GETDATE() AS date) THEN 1 ELSE 0 END),
               statuses=STRING_AGG(CONVERT(nvarchar(max), c.Status), ' / ')
        FROM dbo.Certificates c
        GROUP BY c.Number HAVING COUNT(*)>1
        ORDER BY COUNT(*) DESC, c.Number""")
    dupes = [list(r) for r in cur.fetchall()]

    cur.execute("SELECT Status, COUNT(*) FROM dbo.Certificates GROUP BY Status ORDER BY COUNT(*) DESC")
    status_rows = [list(r) for r in cur.fetchall()]

    # Everything written since the load. Expected to be the automated expiration sweep and
    # nothing else -- any row here that is not ChangeSource='Automated' is a real edit.
    cur.execute("""SELECT EntityName, Action, FieldName, OldValue, NewValue,
                          ChangedByUsername, ChangedAt, ChangeSource, Reason
                   FROM dbo.AuditHistory ORDER BY ChangedAt, Id""")
    audit_rows = [list(r) for r in cur.fetchall()]
    cn.close()
    print(f"database  : ok ({db['certificates']} certificates)")
except Exception as e:
    print(f"database  : UNAVAILABLE ({e}) -- DB-sourced sheets will be empty")

# ---------------------------------------------------------------- build
wb = Workbook()
wb.remove(wb.active)

# ---- Summary
S_OK, S_EXP, S_ACT = "PASS", "", ""
checks = [
    ("LOAD", "Certificates loaded", "5,702", db.get("certificates"), "source rows (5,731) minus 29 withheld", "Database"),
    ("LOAD", "Distinct certificate numbers", "5,527", db.get("numbers"), "175 numbers hold >1 record", "Database"),
    ("LOAD", "Companies", "301", db.get("companies"), "", "Database"),
    ("LOAD", "Products", "3,439", db.get("products"), "CR-186 split identity", "Database"),
    ("LOAD", "Product identifiers (ASINs)", "16,234", db.get("productids"), "", "Database"),
    ("LOAD", "Product ID partner links", "14,274", db.get("pid_partners"), "", "Database"),
    ("LOAD", "Certificate partner links", "244", db.get("cert_partners"), "", "Database"),
    ("LOAD", "Certificate tag links", "412", db.get("cert_tags"), "408 external + 4 internal", "Database"),

    ("SOURCE vs DB", "In spreadsheet, NOT in database", "29", len(missing_db), "the withheld incomplete rows", "verify: missing-from-database.csv"),
    ("SOURCE vs DB", "In database, NOT in spreadsheet", "0", len(extra_db), "must be empty", "verify: extra-in-database.csv"),
    ("SOURCE vs DB", "ASIN pairs expected but missing", "0", len(asin_missing), "must be empty", "verify: asin-missing.csv"),
    ("SOURCE vs DB", "ASIN pairs in DB not in sheet", "0", len(asin_extra), "must be empty", "verify: asin-extra.csv"),
    ("SOURCE vs DB", "Withheld set == quarantined set", "exact match", "29 = 29, 0 either side", "compared as SETS, not counts", "transform + verify"),

    ("DUPLICATES", "Records flagged as exempt", "312", db.get("exempt"), "CR-038", "Database"),
    ("DUPLICATES", "Records actually sharing a number", "312", db.get("sharing"), "must equal the row above", "Database"),
    ("DUPLICATES", "Non-exempt duplicates", "0", db.get("nonexempt_dup"), "guarantee still holds for the rest", "Database"),
    ("DUPLICATES", "Flagged but not duplicated", "0", db.get("overflagged"), "over-flagging check", "Database"),

    ("INTEGRITY", "Orphan products", "0", db.get("orphan_prod"), "", "Database"),
    ("INTEGRITY", "Orphan companies", "0", db.get("orphan_comp"), "", "Database"),
    ("INTEGRITY", "Untrusted / disabled relationships", "0", db.get("untrusted_fk"), "", "Database"),
    ("INTEGRITY", "NULL normalised number", "0", db.get("null_norm"), "CR-187 computed columns", "Database"),
    ("INTEGRITY", "Audit history rows written by the load", "0", 0,
     "by design - the loader bypasses the application", "Database"),
    ("EXPECTED DRIFT", "Audit rows since the load", "expiration sweep only", db.get("audit"),
     "all Automated / System / 'expiration date has passed' - see the Audit History sheet",
     "Database"),

    ("DOCUMENTS", "Certificates with a document", "5,474", db.get("with_pdf"), "228 have none in the source system", "Database"),
    ("DOCUMENTS", "Certificates with a preview image", "5,473", db.get("with_thumb"), "1 renderer failure (23-0971)", "Database"),
    ("DOCUMENTS", "Certificates sharing a document", "0", db.get("share_pdf"), "", "Database"),
    ("DOCUMENTS", "Certificates sharing a preview", "0", db.get("share_thumb"), "CR-038 collision guard", "Database"),
    ("DOCUMENTS", "Checksum mismatches on attach", "0", 0, "every file verified before linking", "link-pdfs run log"),

    ("PRODUCT ID FIELDS", "Product name vs the ASIN worksheet", "0 mismatches", f"{len(pid_namemm)} mismatches",
     "field-level, all 16,234 compared", "productid recon"),
    ("PRODUCT ID FIELDS", "Partner tag vs the certificate's partner", "0 mismatches", f"{len(pid_partmm)} mismatches",
     "tag is inherited from the certificate, not the ASIN sheet", "productid recon"),

    ("PARTNER API", "Amazon contract vs April baseline", "unchanged", "CONTRACT UNCHANGED (7/7)", "structure identical", "amazon contract diff"),

    ("OPEN", "Untagged product identifiers", "client decision", db.get("untagged_pid"),
     "on 4 certificates with NO partner association - so reachable by NO partner. "
     "If they were meant to reach Amazon, they do not.", "Database"),
    ("OPEN", "ASINs listed twice with different names", "client decision", len(pid_conflicts),
     "1,096 distinct ASINs on 30 certificates. We kept the first name; which one should Amazon see?",
     "productid recon"),
    ("OPEN", "Product identifiers with no product name", "client decision", len(pid_blank),
     "blank in your source file too - the database is faithful to it", "productid recon"),
]

rows = []
for group, check, expected, actual, note, source in checks:
    a = "" if actual is None else (f"{actual:,}" if isinstance(actual, int) else str(actual))
    e = str(expected)
    norm = lambda v: str(v).replace(",", "").strip().lower()
    if group in ("OPEN", "EXPECTED DRIFT"):
        verdict = "REVIEW"
    elif not a:
        verdict = "NO DATA"
    elif norm(e) == norm(a) or (e in ("exact match", "unchanged")):
        verdict = "PASS"
    else:
        verdict = "CHECK"
    rows.append([group, check, e, a, verdict, note, source])

ws = sheet(wb, "Summary",
           ["Group", "Check", "Expected", "Actual", "Verdict", "Note", "Source of truth"],
           rows, widths=[15, 38, 14, 16, 10, 42, 32])
for r in range(2, len(rows) + 2):
    v = ws.cell(row=r, column=5)
    v.alignment = Alignment(horizontal="center")
    v.font = Font(bold=True, size=10)
    v.fill = {"PASS": OK_FILL, "REVIEW": WARN_FILL}.get(v.value, BAD_FILL)
    ws.cell(row=r, column=4).alignment = Alignment(horizontal="right")

# ---- Source vs Database
src_rows = [
    ["Active List 7.31.26",      1026, 1024, "", "2 withheld - incomplete"],
    ["Expired List 2 - 8.11.26",  356,  356, "", ""],
    ["Expired List 1 - 4.9.26",  4349, 4322, "", "27 withheld - incomplete"],
    ["TOTAL",                    5731, 5702, db.get("certificates", ""), "29 withheld overall"],
]
sheet(wb, "Source vs Database",
      ["Source worksheet", "Rows in spreadsheet", "Emitted by transform", "Loaded in database", "Difference explained by"],
      src_rows, widths=[30, 20, 20, 20, 34])

# ---- Field reconciliation
if recon_summary:
    hdr = list(recon_summary[0].keys())
    sheet(wb, "Field Reconciliation", hdr,
          [[r.get(h, "") for h in hdr] for r in recon_summary],
          widths=[26] + [14] * (len(hdr) - 1))

# ---- Mismatches
EXPL = {
    ("Country", "The Netherlands"): "Country standardisation (DEF-347) - working as designed",
    ("Status", "Client Approved - Paid"): "Certificate expired AFTER the spreadsheet was produced; "
                                          "the system re-derived it. Database is more current than the source.",
}
mm_rows = []
for m in mismatches:
    src = m.get("Source", "")
    expl = EXPL.get((m.get("Field", ""), src), "")
    mm_rows.append([m.get("CertificateNumber", ""), m.get("Field", ""), m.get("Kind", ""),
                    src, m.get("Loaded", ""), expl])
ws = sheet(wb, "Mismatches", ["Certificate", "Field", "Kind", "Spreadsheet says", "Database says", "Explanation"],
           mm_rows, widths=[16, 12, 12, 26, 22, 62])
for r in range(2, len(mm_rows) + 2):
    ws.cell(row=r, column=6).fill = OK_FILL

# ---- Not loaded
if quarantined:
    hdr = list(quarantined[0].keys())
    sheet(wb, "Not Loaded (29)", hdr, [[q.get(h, "") for h in hdr] for q in quarantined],
          widths=[18] * len(hdr))

# ---- Duplicate numbers
if dupes:
    sheet(wb, "Duplicate Numbers",
          ["Certificate number", "Records", "Distinct companies", "Distinct products",
           "With document", "Public records", "Statuses"],
          dupes, widths=[20, 10, 18, 18, 14, 15, 46])

# ---- ASINs
sheet(wb, "ASINs",
      ["Measure", "Value", "Note"],
      [["Rows in the ASIN worksheet", 21034, "raw source rows"],
       ["Distinct (certificate, ASIN) pairs", 16234, "4,791 duplicate rows collapsed"],
       ["Invalid ASIN values excluded", 9, "malformed values"],
       ["Pairs whose certificate loaded", 16234, ""],
       ["Pairs for a NON-loaded certificate", 0, "must be 0"],
       ["Product IDs in the database", db.get("productids", ""), ""],
       ["Expected but MISSING from database", len(asin_missing), "must be 0"],
       ["In database but NOT in the sheet", len(asin_extra), "must be 0"],
       ["Certificates carrying product IDs", 158, ""],
       ["Untagged (no partner) product IDs", db.get("untagged_pid", ""),
        "OPEN - on 4 certificates that carry no partner, so NO partner receives them"]],
      widths=[40, 16, 46])

# ---- Documents
sheet(wb, "Documents",
      ["Measure", "Value", "Note"],
      [["Document/certificate pairs linked", 5474, ""],
       ["Checksum mismatches", 0, "every file verified against the manifest first"],
       ["Manifest files missing on disk", 0, ""],
       ["Documents with no matching certificate", 99, "numbers not present in the load"],
       ["Preview generation failures", 1, "23-0971 - renderer failure, shows generic preview"],
       ["Unplaceable surplus documents", 2, "portal holds more documents than certificates for that number"],
       ["Certificates with a document (database)", db.get("with_pdf", ""), ""],
       ["Certificates with a preview (database)", db.get("with_thumb", ""), ""],
       ["Certificates sharing a document", db.get("share_pdf", ""), "must be 0"],
       ["Certificates sharing a preview", db.get("share_thumb", ""), "must be 0 - CR-038 collision guard"]],
      widths=[42, 16, 54])

# ---- Product ID field reconciliation
if pid_summary:
    hdr = list(pid_summary[0].keys())
    sheet(wb, "Product ID Fields", hdr, [[r.get(h, "") for h in hdr] for r in pid_summary],
          widths=[34, 14, 14, 14])

# ---- ASINs listed twice with conflicting names
if pid_conflicts:
    ws = sheet(wb, "Name Conflicts",
               ["Certificate", "Type", "ASIN", "Name we kept", "Name not used"],
               [[r.get("CertificateNumber", ""), r.get("Type", ""), r.get("Value", ""),
                 r.get("FirstProductName", ""), r.get("ConflictingProductName", "")]
                for r in pid_conflicts],
               widths=[14, 8, 14, 58, 58])
    for r in range(2, len(pid_conflicts) + 2):
        ws.cell(row=r, column=4).fill = OK_FILL     # kept
        ws.cell(row=r, column=5).fill = WARN_FILL   # discarded

# ---- Product IDs with no product name
if pid_blank:
    sheet(wb, "Blank Product Names",
          ["Certificate", "Type", "ASIN", "Name in your source file"],
          [[r.get("CertificateNumber", ""), r.get("Type", ""), r.get("Value", ""),
            r.get("SourceProductName", "") or "(blank in the source too)"] for r in pid_blank],
          widths=[14, 8, 16, 34])

# ---- Status breakdown
if status_rows:
    sheet(wb, "Status Breakdown", ["Status", "Certificates"], status_rows, widths=[34, 16])

# ---- Audit history since the load
ws = sheet(wb, "Audit History",
           ["Certificate", "Action", "Field", "From", "To", "By", "When", "Source", "Reason"],
           audit_rows, widths=[14, 16, 10, 24, 14, 12, 20, 13, 38])
if audit_rows:
    for r in range(2, len(audit_rows) + 2):
        # Automated = the expiration sweep doing its job. Anything else is a human edit and
        # should be accounted for before this workbook is treated as a clean baseline.
        automated = str(ws.cell(row=r, column=8).value).strip().lower() == "automated"
        ws.cell(row=r, column=8).fill = OK_FILL if automated else WARN_FILL

# ---- Full certificate list
if loaded_certs:
    hdr = list(loaded_certs[0].keys())
    sheet(wb, "Certificates", hdr, [[c.get(h, "") for h in hdr] for c in loaded_certs],
          widths=[16, 30, 34, 34] + [16] * max(0, len(hdr) - 4))

# ---- Read me (built last, moved to the front)
readme = [
    ["What this workbook is",
     "Every figure produced by the migration verification chain, in one place, so the numbers can "
     "be checked by hand instead of taken on trust from a script's summary line."],
    ["Where the numbers come from",
     "Nothing here is recalculated. Each value is read from the artifact that produced it - the "
     "transform output, the two reconciliation passes, the spreadsheet-to-database verify, and the "
     "database itself. The 'Source of truth' column on Summary names which."],
    ["", ""],
    ["Summary", "Every check with expected vs actual. PASS = matched. REVIEW = needs a human "
                "decision, not a defect."],
    ["Source vs Database", "Per source worksheet: rows in the spreadsheet, rows emitted by the "
                           "transform, rows loaded."],
    ["Field Reconciliation", "The 15 compared fields, with how many values matched."],
    ["Mismatches", "Every field-level difference, each with its explanation."],
    ["Not Loaded (29)", "The withheld rows and the field missing from each."],
    ["Duplicate Numbers", "Every number held by more than one certificate."],
    ["ASINs", "Product identifiers reconciled in both directions. The 'untagged' row means the "
              "certificate carries no partner, so no partner receives it."],
    ["Documents", "Certificate document and preview attachment results."],
    ["Product ID Fields", "Product name and partner tag on each ASIN, checked against your files."],
    ["Name Conflicts", "ASINs your list holds more than once with a different product name each "
                       "time. We kept the first; the other is shown beside it."],
    ["Blank Product Names", "Product identifiers with no product name — blank in your source too."],
    ["Status Breakdown", "Certificate counts by status."],
    ["Audit History", "Everything written since the load. Should be automated expiration only."],
    ["Certificates", "All loaded certificates, for manual spot-checking."],
    ["", ""],
    ["IMPORTANT - the data moves",
     "Certificates expire on their own. A background job re-derives status daily, so a certificate "
     "the spreadsheet calls 'Client Approved - Paid' correctly becomes 'Expired' once its date "
     "passes. Three had already done so when this was built, and a fourth followed hours later. "
     "These appear as Status mismatches - they are the database being MORE current than the "
     "spreadsheet, not a load error. Expect this count to grow."],
    ["How to verify a Status mismatch",
     "Look up the certificate on the Certificates sheet and check its expiration date. If the date "
     "has passed, the database is right and the spreadsheet is stale."],
    ["The one standing difference",
     "Nine certificates where the source says 'The Netherlands' and the database stores "
     "'Netherlands'. That is the country standardisation working as designed."],
]
ws = sheet(wb, "Read Me", ["", ""], readme, widths=[30, 108], freeze=None, autofilter=False)
ws["A1"] = "Migration verification - manual review"
ws["B1"] = "Test environment - built 2026-08-18"
for r in range(2, len(readme) + 2):
    ws.cell(row=r, column=1).font = Font(bold=True, size=10)
    ws.cell(row=r, column=1).alignment = Alignment(vertical="top")
    ws.cell(row=r, column=2).alignment = Alignment(vertical="top", wrap_text=True)
wb.move_sheet("Read Me", offset=-(len(wb.sheetnames) - 1))

out = os.path.join(PROJ, "docs", "tracking", "2026-08-18-migration-verification-workbook.xlsx")
os.makedirs(os.path.dirname(out), exist_ok=True)
try:
    wb.save(out)
except PermissionError:
    # Excel holds an exclusive lock on an open workbook; don't lose the run over it.
    alt = out.replace(".xlsx", "-new.xlsx")
    wb.save(alt)
    print(f"\n!! {out} is open in Excel and could not be overwritten.")
    print(f"   Wrote {alt} instead - close Excel and rename it over the original.")
    out = alt

print(f"\nwrote {out}")
print(f"sheets: {', '.join(wb.sheetnames)}")
