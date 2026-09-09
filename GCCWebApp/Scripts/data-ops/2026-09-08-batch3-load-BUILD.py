# -*- coding: utf-8 -*-
"""
BATCH 3 MIGRATION LOAD  |  client drop 2026-09-04  ->  one transactional T-SQL script
=====================================================================================
Additive top-up onto the already-migrated database (5,924 certificates on the test
environment as of 2026-09-08).  Reads ONE client workbook in DataMigrationTestDev\new
and emits `_batch3_output/<stamp>/batch3-load.sql` plus the audit CSVs.  READ-ONLY
against the sources; writes nothing to any database.

    python 2026-09-08-batch3-load-BUILD.py
    sqlcmd -S <server> -d <db> -I -b -i batch3-load.sql

WHAT IS IN THE DROP
-------------------
  GCC Database Certs for Upload_9.4.26.xlsx  sheet "Missing to Add 9.4.26"   88 rows

These are records the client identified as MISSING from the previous loads - they
fill gaps in an existing number sequence (e.g. 26-0434..26-0437 and 26-0440 sit
either side of 26-0438/26-0439, which batch 2 reconstructed).  All 88 numbers were
confirmed absent from the target on 2026-09-08 before this generator was written.

HOW THIS DIFFERS FROM THE BATCH-2 LOAD
--------------------------------------
This is deliberately a NARROWER run than batch 2.  Everything batch 2 had to reason
about - renewals, synthesized certificates, folded company names, a new taxonomy row
- is absent here, and the corresponding config tables below are empty ON PURPOSE
rather than by omission.  What is left is a plain additive insert.

1. **No ASIN / product-ID file.**  The drop folder's ASIN workbook
   (GCC ASINs for DB Upload_9.3.26.xlsm) is batch 2's and has ZERO overlap with
   these 88 certificate numbers - measured, not assumed.  So 26 of these rows are
   Amazon-partner certificates that will carry no product IDs until the client
   sends an ASIN list covering them.  Shawn's decision, 2026-09-08: load the
   certificates now, attach ASINs in a later pass.  `asins` is therefore optional;
   passing a workbook re-enables the batch-2 product-ID path unchanged.

2. **No renewals.**  All 88 numbers are new.  RENEWALS is empty and the SQL keeps
   its "already exists" abort, so a re-run or an unflagged renewal still stops the
   load rather than silently updating a record.

3. **No synthesized certificates and no folded company names.**  Two companies in
   the sheet do not exist in the target - "McKinney, a brand associated with ASSA
   ABLOY Accessories Hardware Group, Inc." (1 cert) and "Sylvamo North America,
   LLC" (1 cert).  Neither has a near-match among the 321 existing companies (26
   other ASSA ABLOY brands are present; no McKinney), so on Shawn's decision,
   2026-09-08, both are created verbatim by the load's normal company insert.
   They are listed in new-companies.csv for review before the run.

4. **No new ProductTypes.**  Every category, type and subcategory in the sheet
   already resolves in the target once the outline number is stripped.

5. **Certificate subcategories are read but NOT loaded** - see below.

CERTIFICATE SUBCATEGORIES ARE DELIBERATELY NOT LOADED  (Shawn, 2026-09-08)
---------------------------------------------------------------------------
The sheet carries six "Certificate Subcategories (for CSF and WD only)" columns
holding 140 values across the 88 rows.  They are read, counted, and written to
cert-subcategories-NOT-loaded.csv - and then dropped, on purpose.

`dbo.CertificateCertificationTypeSubcategories` holds **0 rows for all 5,924
certificates** in the target.  No migration has ever populated it, batch 2 included
(its loader reads the same columns into `_cert_subcats` and never emits them).  The
application supports the field on both sides - CertificateController Create/Edit
write the links, and CertificateTileInternalApiController projects them as the
sortable "Cert Subcategories" column - so that column renders blank for the entire
database.

Loading only these 88 would make them the sole populated certificates in the
database, which reads as a data anomaly rather than a fix.  The gap is raised as a
defect to be closed by one backfill across every certificate whose source sheet
carries the values.  Set LOAD_CERT_SUBCATEGORIES = True to emit them here instead.

WHAT IS CARRIED OVER FROM BATCH 2 UNCHANGED
-------------------------------------------
* **CertificateCountries is written** (CR-037) - one row per country even for the
  single-country rows, which all 88 of these are.  A certificate without one is
  invisible to the country filter.
* **Leading outline numbers are stripped** from Product Category / Type /
  Subcategory ("3.5 Doors, Door Hardware, and Accessories" -> "Doors, Door
  Hardware, and Accessories").
* **ProductSubcategoryId is populated** against ProductTypes by (Name, ParentId).
* **CertificationResults is written as an explicit "XX%" string** (CR-042/045).
  All 14 populated cells in this sheet hold the raw value 1 under a '0%' number
  format - displayed 100%.  to_percent() turns that into '100%', matching the 67
  existing Renewable-electricity certificates in the target, which store
  CertificationResults='100%' with CertificationResultsNumeric=100.  Writing the
  raw cell value would put "1%" on 14 certificates.
* The encoding canary, the full preflight, the single transaction and the emitted
  UNDO script.
"""


from __future__ import annotations

import argparse
import csv
import datetime
import importlib.util
import io
import os
import re
import sys
from collections import Counter, OrderedDict, defaultdict

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", errors="replace")

try:
    import openpyxl
except ImportError:
    sys.exit("openpyxl is required:  pip install openpyxl")

HERE = os.path.dirname(os.path.abspath(__file__))
APP_ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
REPO_ROOT = os.path.abspath(os.path.join(APP_ROOT, "..", ".."))
SRC_DIR = os.path.join(REPO_ROOT, "DataMigrationTestDev", "new")

DEFAULT_CERTS = os.path.join(SRC_DIR, "GCC Database Certs for Upload_9.4.26.xlsx")
DEFAULT_ASINS = None      # batch 3 ships no ASIN workbook - see the docstring
CERT_SHEET = "Missing to Add 9.4.26"
ASIN_SHEET = "ACTIVE LIST"

# Country canonicalisation is reused verbatim from the August generator so the two
# loads cannot disagree about what "The Netherlands" is.
GENERATOR = os.path.join(HERE, "2026-08-13-migration-sql-generator.py")

BATCH = 900          # SQL Server caps a VALUES clause at 1000 rows.
PUBLIC_STATUSES = {"Client Approved - Paid", "Republished"}

# Certificate numbers present in the sheet that already exist in the target and are
# renewals of those records rather than new certificates.  Verified against the
# client's production API 2026-09-03.  Anything NOT listed here that already exists
# aborts the load.
RENEWALS = set()               # batch 3: all 88 numbers verified absent 2026-09-08

# The renewals' state in the target BEFORE this load, read from gccwebap_certificates
# on 2026-09-03 and transcribed here so the undo restores the original rather than
# "whatever is there now" — after a second load those are not the same thing.
RENEWAL_PRE_STATE = {}

# Sheet spelling -> existing Companies.Name to attach to instead of creating.
MERGE_COMPANIES = {}           # batch 3: both unknown companies are created verbatim

# Companies in the sheet that did NOT exist in the target when this was written
# (checked against gccwebap_certificates, 321 companies, 2026-09-08). Listed so a
# THIRD unexpected new company shows up as a warning rather than sliding in
# unnoticed - the SQL's own 'NEW company' SELECT remains the authority at run time.
EXPECTED_NEW_COMPANIES = {
    "McKinney, a brand associated with ASSA ABLOY Accessories Hardware Group, Inc.",
    "Sylvamo North America, LLC",
}

# ProductTypes rows the sheet needs that the target does not have yet.
# name -> (category, parent type or None, level)
NEW_PRODUCT_TYPES = []         # batch 3: every taxonomy value already resolves

# Certificates rebuilt from the client's production system — see the module docstring.
SYNTHESIZED = []               # batch 3: nothing is reconstructed

# Column index -> canonical field name, for "Final Upload List 9.3.26".
# Positional rather than by header text: four columns share the header
# "Certificate Subcategories (for CSF and WD only)" and two carry trailing spaces.
CERT_COLS = {
    0: "Certificate #", 1: "Company", 2: "Product Name", 3: "Product Category",
    4: "Product Type", 5: "Subcategory", 6: "Certification Type", 7: "Effective Date",
    8: "Expiration Date", 9: "Location", 10: "Country", 11: "Status", 12: "Partner",
    13: "Cert Results", 14: "Pathway", 15: "External Tag", 16: "Internal Tag",
    17: "Company Email", 18: "Company URL",
}
CERT_SUBCAT_COLS = (19, 20, 21, 22, 23, 24)   # batch 3's sheet has six

# See the module docstring. The link table is empty for all 5,924 certificates in
# the target; loading only these 88 would make them the only populated ones.
LOAD_CERT_SUBCATEGORIES = False

ASIN_COLS = {
    0: "company", 1: "type", 2: "value", 3: "amazonCode", 5: "productName",
    6: "certificationType", 7: "certificateNumber", 11: "pathway",
}

# "4.3 Cleaning Supplies" -> "Cleaning Supplies".  Anchored and requiring whitespace
# after the number so a legitimate name that merely starts with a digit survives.
OUTLINE_PREFIX = re.compile(r"^\s*\d+(?:\.\d+)*\s+")


# ------------------------------------------------------------------ helpers --
def strip_outline(v) -> str:
    if v is None:
        return ""
    return OUTLINE_PREFIX.sub("", str(v).strip()).strip()


def cell(v) -> str:
    if v is None:
        return ""
    if isinstance(v, (datetime.datetime, datetime.date)):
        return v.strftime("%Y-%m-%d")
    if isinstance(v, float) and v.is_integer():
        return str(int(v))
    return str(v).strip()


def as_date(v, where: str) -> str:
    if v is None or (isinstance(v, str) and not v.strip()):
        return ""
    if isinstance(v, (datetime.datetime, datetime.date)):
        return v.strftime("%Y-%m-%d")
    s = str(v).strip()
    for fmt in ("%Y-%m-%d", "%m/%d/%Y", "%d/%m/%Y"):
        try:
            return datetime.datetime.strptime(s, fmt).strftime("%Y-%m-%d")
        except ValueError:
            pass
    sys.exit(f"ABORT: unparseable date {v!r} at {where}")


def to_percent(v, where: str) -> str:
    """CR-042.  Numeric -> explicit whole-percent string; anything else untouched."""
    s = cell(v)
    if not s or "%" in s:
        return s
    try:
        n = float(s)
    except ValueError:
        return s                       # "Multiple", "Gold", "+Vantage Vinyl Verified"
    pct = n * 100 if 0 <= n <= 1 else n
    if abs(pct - round(pct)) > 1e-9:
        sys.exit(f"ABORT: {where} certification result {s!r} is not a whole percentage "
                 f"({pct}). DEF-353: nothing is rounded silently — decide it by hand.")
    return f"{int(round(pct))}%"


def split_list(v):
    """Tags / partners / countries are ';' or ',' delimited.  'None' is not a value."""
    out = []
    for p in re.split(r"[;,|]", cell(v)):
        p = p.strip()
        if p and p.lower() not in ("none", "n/a - none") and p not in out:
            out.append(p)
    return out


def sq(v) -> str:
    if v is None:
        return "NULL"
    s = str(v).strip()
    return "NULL" if s == "" else "N'" + s.replace("'", "''") + "'"


def sdate(v) -> str:
    s = (v or "").strip()
    return f"'{s}'" if s else "NULL"


def emit_batched_insert(out, table, cols, rows):
    if not rows:
        out.append(f"-- (no rows for {table})")
        return
    collist = ", ".join(cols)
    for i in range(0, len(rows), BATCH):
        chunk = rows[i:i + BATCH]
        out.append(f"INSERT INTO {table} ({collist}) VALUES")
        out.append(",\n".join("  (" + ", ".join(r) + ")" for r in chunk) + ";")


def open_workbook(path):
    """openpyxl opens the file directly, so a workbook the client still has open in
    Excel (or that OneDrive is mid-sync on) raises PermissionError. Fall back to a
    private copy rather than making whoever runs this close Excel first."""
    try:
        return openpyxl.load_workbook(path, read_only=True, data_only=True)
    except PermissionError:
        import shutil, tempfile
        tmp = os.path.join(tempfile.gettempdir(), "gcc-batch3-" + os.path.basename(path))
        shutil.copy2(path, tmp)
        print(f"  (locked — read from a copy at {tmp})")
        return openpyxl.load_workbook(tmp, read_only=True, data_only=True)


def load_country_map():
    spec = importlib.util.spec_from_file_location("gen", GENERATOR)
    m = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(m)
    canon, n_c, n_a = m.load_country_map()
    return canon, m.cs_normalize, n_c, n_a


# -------------------------------------------------------------------- read --
def read_certs(path):
    wb = open_workbook(path)
    if CERT_SHEET not in wb.sheetnames:
        sys.exit(f"ABORT: sheet {CERT_SHEET!r} not in {path} (has {wb.sheetnames})")
    rows = list(wb[CERT_SHEET].iter_rows(values_only=True))
    wb.close()

    hdr = rows[0]
    if str(hdr[0]).strip() != "CERTIFICATE_NUMBER":
        sys.exit(f"ABORT: column A is {hdr[0]!r}, expected CERTIFICATE_NUMBER. "
                 "The sheet layout changed — re-map CERT_COLS before trusting this.")

    recs = []
    for excel_row, r in enumerate(rows[1:], start=2):
        if not r or not r[0]:
            continue
        g = lambda i: (r[i] if i < len(r) else None)
        num = cell(g(0))
        rec = {name: cell(g(i)) for i, name in CERT_COLS.items()}
        rec["Certificate #"] = num
        rec["Product Category"] = strip_outline(g(3))
        rec["Product Type"] = strip_outline(g(4))
        rec["Subcategory"] = strip_outline(g(5))
        rec["Effective Date"] = as_date(g(7), f"{num} EFFECTIVE_DATE (row {excel_row})")
        rec["Expiration Date"] = as_date(g(8), f"{num} EXPIRATION_DATE (row {excel_row})")
        rec["Cert Results"] = to_percent(g(13), f"{num} (row {excel_row})")
        rec["_cert_subcats"] = [cell(g(i)) for i in CERT_SUBCAT_COLS if cell(g(i))]
        rec["_row"] = excel_row
        recs.append(rec)
    return recs


def read_asins(path):
    wb = open_workbook(path)
    if ASIN_SHEET not in wb.sheetnames:
        sys.exit(f"ABORT: sheet {ASIN_SHEET!r} not in {path} (has {wb.sheetnames})")
    rows = list(wb[ASIN_SHEET].iter_rows(values_only=True))
    wb.close()

    hdr = rows[0]
    if str(hdr[7]).strip() != "certificateNumber":
        sys.exit(f"ABORT: column H is {hdr[7]!r}, expected certificateNumber.")

    recs = []
    for excel_row, r in enumerate(rows[1:], start=2):
        if not r or not any(v is not None for v in r):
            continue
        g = lambda i: (r[i] if i < len(r) else None)
        rec = {name: cell(g(i)) for i, name in ASIN_COLS.items()}
        rec["_row"] = excel_row
        if rec["certificateNumber"] and rec["value"]:
            recs.append(rec)
    return recs


# -------------------------------------------------------------------- main --
def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("certs", nargs="?", default=DEFAULT_CERTS)
    ap.add_argument("asins", nargs="?", default=DEFAULT_ASINS)
    ap.add_argument("--out", default=os.path.join(HERE, "_batch3_output"))
    args = ap.parse_args()

    stamp = datetime.datetime.now().strftime("%Y%m%d-%H%M%S")
    outdir = os.path.join(args.out, stamp)
    os.makedirs(outdir, exist_ok=True)

    print("BATCH 3 LOAD BUILD")
    print(f"  certs : {args.certs}")
    print(f"  asins : {args.asins or '(none)'}")
    print(f"  out   : {outdir}\n")

    canon, cs_normalize, n_countries, n_aliases = load_country_map()
    print(f"countries : {n_countries} canonical + {n_aliases} aliases from CountryList.cs")

    certs = read_certs(args.certs)
    asins = read_asins(args.asins) if args.asins else []
    if not args.asins:
        print("asins : (none supplied - no product IDs in this load)")
    print(f"cert rows : {len(certs)}")
    if args.asins:
        print(f"asin rows : {len(asins)}")

    # ---- company folding ---------------------------------------------------
    merged = []
    for r in certs:
        target = MERGE_COMPANIES.get(r["Company"])
        if target:
            merged.append((r["Certificate #"], r["Company"], target))
            r["_company_as_written"] = r["Company"]
            r["Company"] = target
    if merged:
        print(f"companies folded into existing records: {len(merged)}")
        for num, was, now in merged:
            print(f"    {num}: {was!r} -> {now!r}")

    # ---- renewals split off ------------------------------------------------
    renewals = [r for r in certs if r["Certificate #"] in RENEWALS]
    new_certs = [r for r in certs if r["Certificate #"] not in RENEWALS]
    got = {r["Certificate #"] for r in renewals}
    if got != RENEWALS:
        sys.exit(f"ABORT: RENEWALS lists {sorted(RENEWALS)} but the sheet carries {sorted(got)}. "
                 "Re-verify against the target before loading.")
    print(f"new certificates : {len(new_certs)}")
    print(f"renewals (UPDATE): {len(renewals)}  {sorted(got)}")

    # ---- synthesized -------------------------------------------------------
    for s in SYNTHESIZED:
        rec = dict(s)
        rec["_cert_subcats"] = []
        rec["_row"] = 0
        rec["_synthesized"] = True
        new_certs.append(rec)
    print(f"synthesized      : {len(SYNTHESIZED)}  {[s['Certificate #'] for s in SYNTHESIZED]}")

    dup = [n for n, c in Counter(r["Certificate #"] for r in new_certs).items() if c > 1]
    if dup:
        sys.exit(f"ABORT: duplicate certificate number(s) in the load set: {dup}")

    # ---- countries ---------------------------------------------------------
    unmapped = Counter()
    multi = []
    for r in new_certs + renewals:
        raw = r.get("Country", "")
        resolved = []
        for part in split_list(raw):
            c = canon.get(cs_normalize(part))
            if c is None:
                unmapped[part] += 1
                c = part
            if c not in resolved:
                resolved.append(c)
        r["_countries"] = resolved
        r["_country"] = resolved[0] if resolved else ""
        if len(resolved) > 1:
            multi.append((r["Certificate #"], raw, resolved))

    print(f"\nmulti-country certificates : {len(multi)}")
    for num, raw, res in multi:
        print(f"    {num}: {raw!r} -> primary {res[0]!r} + {len(res)} CertificateCountries rows {res}")
    no_country = [r["Certificate #"] for r in new_certs + renewals if not r["_countries"]]
    if no_country:
        print(f"certificates with NO country ({len(no_country)}): {no_country}")
        print("    -> Country stays NULL and no CertificateCountries row is written; they will not "
              "appear under any country filter until the client supplies one.")
    if unmapped:
        print(f"country values not in CountryList, passed through as written: {dict(unmapped)}")

    # ---- product ids -------------------------------------------------------
    load_numbers = {r["Certificate #"] for r in new_certs} | RENEWALS
    pid_rows, pid_orphans, seen_pid = [], [], set()
    for a in asins:
        num = a["certificateNumber"]
        key = (num, a["type"], a["value"])
        if num not in load_numbers:
            # not in this drop -> only loadable if it is already in the target; the
            # SQL preflight decides that, this only records the ones we know about.
            pid_orphans.append(a)
        if key in seen_pid:
            continue
        seen_pid.add(key)
        pid_rows.append({
            "certificateNumber": num, "type": a["type"], "value": a["value"],
            "Partner": "Amazon" if a["amazonCode"] else "", "productName": a["productName"],
        })
    dropped = len(asins) - len(pid_rows)
    print(f"\nproduct IDs: {len(asins)} rows -> {len(pid_rows)} distinct (cert, type, value); "
          f"{dropped} in-sheet duplicates collapsed")
    by_cert = Counter(p["certificateNumber"] for p in pid_rows)
    for num in sorted(by_cert):
        where = "this load" if num in load_numbers else "must already exist in the target"
        print(f"    {num}: {by_cert[num]:>4}   ({where})")

    # ---- tags / partners ---------------------------------------------------
    cert_tags, cert_partners = [], []
    for r in new_certs + renewals:
        for t in split_list(r.get("External Tag")) + split_list(r.get("Internal Tag")):
            cert_tags.append((r["Certificate #"], t))
        for p in split_list(r.get("Partner")):
            cert_partners.append((r["Certificate #"], p))
    # A company the 2026-09-08 check did not see is not necessarily wrong, but it is
    # not something to discover after the insert.
    sheet_companies = {r["Company"] for r in new_certs + renewals}
    surprises = sorted(EXPECTED_NEW_COMPANIES - sheet_companies)
    if surprises:
        print(f"\nWARNING: expected-new companies absent from the sheet: {surprises}")

    n_subs = sum(len(r.get("_cert_subcats", [])) for r in new_certs + renewals)
    n_certs_with_subs = sum(1 for r in new_certs + renewals if r.get("_cert_subcats"))
    if n_subs and not LOAD_CERT_SUBCATEGORIES:
        print(f"\ncertificate subcategories: {n_subs} values on {n_certs_with_subs} "
              "certificates are NOT loaded (see the module docstring); they are\n"
              "                           recorded in cert-subcategories-NOT-loaded.csv")

    print(f"\ncert-tag links     : {len(cert_tags)}")
    print(f"cert-partner links : {len(cert_partners)}")

    # ---- audit CSVs --------------------------------------------------------
    def write_csv(name, header, rows):
        with io.open(os.path.join(outdir, name), "w", encoding="utf-8-sig", newline="") as fh:
            w = csv.writer(fh)
            w.writerow(header)
            w.writerows(rows)
        print(f"  wrote {name} ({len(rows)} rows)")

    print("\naudit files:")
    write_csv("certs-loaded.csv",
              ["Certificate #", "Company", "Product Name", "Certification Type", "Status",
               "Product Category", "Product Type", "Subcategory", "Effective Date",
               "Expiration Date", "Location", "Country (primary)", "All countries",
               "Partner", "Pathway", "Cert Results", "External Tag", "Internal Tag",
               "Source row", "Synthesized"],
              [[r["Certificate #"], r["Company"], r.get("Product Name", ""),
                r["Certification Type"], r["Status"], r.get("Product Category", ""),
                r.get("Product Type", ""), r.get("Subcategory", ""), r["Effective Date"],
                r["Expiration Date"], r.get("Location", ""), r["_country"],
                "; ".join(r["_countries"]), r.get("Partner", ""), r.get("Pathway", ""),
                r.get("Cert Results", ""), r.get("External Tag", ""), r.get("Internal Tag", ""),
                r.get("_row", ""), "YES" if r.get("_synthesized") else ""]
               for r in sorted(new_certs, key=lambda x: x["Certificate #"])])
    write_csv("renewals-updated.csv",
              ["Certificate #", "Company", "New Status", "New Effective", "New Expiration",
               "New Location", "Country"],
              [[r["Certificate #"], r["Company"], r["Status"], r["Effective Date"],
                r["Expiration Date"], r.get("Location", ""), r["_country"]] for r in renewals])
    write_csv("multi-country.csv", ["Certificate #", "Source value", "Primary", "All countries"],
              [[n, raw, res[0], "; ".join(res)] for n, raw, res in multi])
    companies = Counter(r["Company"] for r in new_certs + renewals)
    write_csv("new-companies.csv",
              ["Company", "Certs in this load", "Expected new"],
              [[c, companies[c], "YES" if c in EXPECTED_NEW_COMPANIES else ""]
               for c in sorted(companies)])
    write_csv("cert-subcategories-NOT-loaded.csv",
              ["Certificate #", "Certification Type", "Subcategory"],
              [[r["Certificate #"], r.get("Certification Type", ""), sub]
               for r in sorted(new_certs, key=lambda x: x["Certificate #"])
               for sub in r.get("_cert_subcats", [])])
    write_csv("product-ids.csv", ["Certificate #", "Type", "Value", "Partner", "Product name"],
              [[p["certificateNumber"], p["type"], p["value"], p["Partner"], p["productName"]]
               for p in pid_rows])

    # ---- SQL ---------------------------------------------------------------
    sql_path = write_sql(outdir, stamp, new_certs, renewals, pid_rows, cert_tags, cert_partners)
    undo_path = write_undo(outdir, stamp, new_certs, renewals)

    print(f"\nWrote {sql_path}")
    print(f"Wrote {undo_path}")
    print("\nRun with:")
    print(f'  sqlcmd -S <server> -d <database> -I -b -i "{sql_path}"')
    print("  (-I is required: CR-187 persisted computed columns. -b makes a failure a "
          "non-zero exit.)")


# --------------------------------------------------------------------- sql --
def write_sql(outdir, stamp, certs, renewals, pids, cert_tags, cert_partners):
    out = []
    A = out.append

    A(f"""/* ============================================================================
   BATCH 3 MIGRATION LOAD  —  generated {datetime.datetime.now():%Y-%m-%d %H:%M:%S}
   ----------------------------------------------------------------------------
   Source: GCC Database Certs for Upload_9.4.26.xlsx, sheet "Missing to Add 9.4.26"
   Generated by Scripts/data-ops/2026-09-08-batch3-load-BUILD.py — do not hand-edit;
   change the generator and re-run, or the audit CSVs stop describing this file.

   Additive: it inserts {len(certs)} certificates, updates {len(renewals)} renewals in place,
   and touches nothing else that already exists. Everything runs in ONE transaction
   and rolls back whole if any assertion fails.

   RUN WITH:  sqlcmd -S <server> -d <database> -I -b -i this-file.sql
   -I (QUOTED_IDENTIFIER ON) is REQUIRED — CR-187 persisted computed columns.
============================================================================ */
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;

DECLARE @now datetime2(7) = SYSUTCDATETIME();
DECLARE @errmsg nvarchar(400);

-- Encoding canary. The August load was written as UTF-8, decoded by sqlcmd as the
-- ANSI codepage, and silently mangled 1,674 product names. These literals equal the
-- corresponding NCHAR only if this file was decoded correctly.
IF N'®' <> NCHAR(174) OR N'™' <> NCHAR(8482)
BEGIN
    RAISERROR('ABORT: this script was decoded with the wrong codepage — non-ASCII text would be corrupted. Re-run against the UTF-16 file as generated (sqlcmd auto-detects the BOM), or pass -f 65001.', 16, 1);
    RETURN;
END;

PRINT '=== BATCH 3 LOAD  ({stamp}) ===';
""")

    # ---------------- staging ----------------
    A(r"""-------------------------------------------------------------------- staging --
IF OBJECT_ID('tempdb..#Cert')        IS NOT NULL DROP TABLE #Cert;
IF OBJECT_ID('tempdb..#CertCountry') IS NOT NULL DROP TABLE #CertCountry;
IF OBJECT_ID('tempdb..#CertTag')     IS NOT NULL DROP TABLE #CertTag;
IF OBJECT_ID('tempdb..#CertPartner') IS NOT NULL DROP TABLE #CertPartner;
IF OBJECT_ID('tempdb..#Pid')         IS NOT NULL DROP TABLE #Pid;
IF OBJECT_ID('tempdb..#Renewal')     IS NOT NULL DROP TABLE #Renewal;

CREATE TABLE #Cert (
    Number         nvarchar(100)  NOT NULL,
    CertType       nvarchar(200)  NULL,
    Status         nvarchar(100)  NULL,
    Company        nvarchar(255)  NULL,
    ProductName    nvarchar(1000) NULL,
    Category       nvarchar(200)  NULL,
    ProductType    nvarchar(200)  NULL,
    Subcategory    nvarchar(200)  NULL,
    Pathway        nvarchar(200)  NULL,
    CertResults    nvarchar(2000) NULL,
    EffectiveDate  date NULL,
    ExpirationDate date NULL,
    Location       nvarchar(350)  NULL,
    Country        nvarchar(200)  NULL,
    CompanyEmail   nvarchar(255)  NULL,
    CompanyUrl     nvarchar(500)  NULL
);
CREATE TABLE #CertCountry (Number nvarchar(100) NOT NULL, Country nvarchar(200) NOT NULL);
CREATE TABLE #CertTag     (Number nvarchar(100) NOT NULL, TagName nvarchar(200) NOT NULL);
CREATE TABLE #CertPartner (Number nvarchar(100) NOT NULL, PartnerName nvarchar(100) NOT NULL);
CREATE TABLE #Pid (
    Seq         int identity(1,1),
    Number      nvarchar(100)  NOT NULL,
    IdType      nvarchar(20)   NOT NULL,
    IdValue     nvarchar(100)  NOT NULL,
    PartnerName nvarchar(100)  NULL,
    ProductName nvarchar(1000) NULL
);
CREATE TABLE #Renewal (
    Number         nvarchar(100) NOT NULL,
    Status         nvarchar(100) NOT NULL,
    EffectiveDate  date NOT NULL,
    ExpirationDate date NOT NULL,
    Location       nvarchar(350) NULL,
    Country        nvarchar(200) NULL
);
""")

    A("-- certificates ------------------------------------------------------------")
    emit_batched_insert(out, "#Cert",
        ["Number", "CertType", "Status", "Company", "ProductName", "Category", "ProductType",
         "Subcategory", "Pathway", "CertResults", "EffectiveDate", "ExpirationDate",
         "Location", "Country", "CompanyEmail", "CompanyUrl"],
        [[sq(r["Certificate #"]), sq(r.get("Certification Type")), sq(r.get("Status")),
          sq(r.get("Company")), sq(r.get("Product Name")), sq(r.get("Product Category")),
          sq(r.get("Product Type")), sq(r.get("Subcategory")), sq(r.get("Pathway")),
          sq(r.get("Cert Results")), sdate(r.get("Effective Date")),
          sdate(r.get("Expiration Date")), sq(r.get("Location")), sq(r["_country"]),
          sq(r.get("Company Email")), sq(r.get("Company URL"))]
         for r in certs])

    A("-- renewals (UPDATE in place) ----------------------------------------------")
    emit_batched_insert(out, "#Renewal",
        ["Number", "Status", "EffectiveDate", "ExpirationDate", "Location", "Country"],
        [[sq(r["Certificate #"]), sq(r["Status"]), sdate(r["Effective Date"]),
          sdate(r["Expiration Date"]), sq(r.get("Location")), sq(r["_country"])]
         for r in renewals])

    A("-- cert -> country (CR-037) ------------------------------------------------")
    cc = [(r["Certificate #"], c) for r in certs + renewals for c in r["_countries"]]
    emit_batched_insert(out, "#CertCountry", ["Number", "Country"],
                        [[sq(a), sq(b)] for a, b in cc])

    A("-- cert -> tag -------------------------------------------------------------")
    emit_batched_insert(out, "#CertTag", ["Number", "TagName"],
                        [[sq(a), sq(b)] for a, b in cert_tags])

    A("-- cert -> partner ---------------------------------------------------------")
    emit_batched_insert(out, "#CertPartner", ["Number", "PartnerName"],
                        [[sq(a), sq(b)] for a, b in cert_partners])

    A("-- product ids -------------------------------------------------------------")
    emit_batched_insert(out, "#Pid", ["Number", "IdType", "IdValue", "PartnerName", "ProductName"],
                        [[sq(p["certificateNumber"]), sq(p["type"]), sq(p["value"]),
                          sq(p["Partner"]), sq(p["productName"])] for p in pids])

    # ---------------- new taxonomy rows ----------------
    A(r"""
------------------------------------------------- new product-type rows (pre) --
-- These must exist BEFORE the preflight resolves product types, and they are
-- taxonomy the client authorised on the workbook's own "Field Options" sheet.
-- Created outside the load transaction on purpose: a ProductTypes row is
-- reference data, it is harmless if the load later rolls back, and creating it
-- inside the transaction would make the preflight's own lookup order fragile.""")
    for name, cat, parent, level in NEW_PRODUCT_TYPES:
        A(f"""
IF NOT EXISTS (SELECT 1 FROM dbo.ProductTypes WHERE Name = {sq(name)})
BEGIN
    DECLARE @cat_{name} int = (SELECT MIN(Id) FROM dbo.ProductCategories WHERE Name = {sq(cat)});
    DECLARE @par_{name} int = (SELECT MIN(Id) FROM dbo.ProductTypes WHERE Name = {sq(parent)});
    IF @cat_{name} IS NULL OR @par_{name} IS NULL
    BEGIN
        RAISERROR('ABORT: cannot place new ProductType {name} — parent category or type is missing.',16,1);
        RETURN;
    END;
    INSERT INTO dbo.ProductTypes (Name, CategoryId, ParentId, Level, SortOrder, IsActive)
    SELECT {sq(name)}, @cat_{name}, @par_{name}, {level},
           ISNULL((SELECT MAX(SortOrder) FROM dbo.ProductTypes WHERE ParentId = @par_{name}), 0) + 1, 1;
    PRINT '  created ProductType {name} (Level {level}, under {parent})';
END
ELSE PRINT '  ProductType {name} already present';""")

    # ---------------- preflight ----------------
    A(r"""
------------------------------------------------------------------ preflight --
-- Nothing is written until every one of these passes. Resolving a lookup to the
-- WRONG row is worse than failing, so an unresolved name aborts the run.

IF OBJECT_ID('tempdb..#CT') IS NOT NULL DROP TABLE #CT;
SELECT Name, MIN(Id) AS Id INTO #CT FROM dbo.CertificationTypes GROUP BY Name;
IF OBJECT_ID('tempdb..#PC') IS NOT NULL DROP TABLE #PC;
SELECT Name, MIN(Id) AS Id INTO #PC FROM dbo.ProductCategories GROUP BY Name;
IF OBJECT_ID('tempdb..#PT') IS NOT NULL DROP TABLE #PT;
SELECT Name, MIN(Id) AS Id INTO #PT FROM dbo.ProductTypes GROUP BY Name;
IF OBJECT_ID('tempdb..#PW') IS NOT NULL DROP TABLE #PW;
SELECT Name, MIN(Id) AS Id INTO #PW FROM dbo.Pathways GROUP BY Name;

-- A subcategory is a ProductTypes row whose PARENT is the row's Level-1 type —
-- resolving it by name alone would collide the day two types share a child name.
IF OBJECT_ID('tempdb..#PS') IS NOT NULL DROP TABLE #PS;
SELECT parent.Name AS ParentName, child.Name AS Name, MIN(child.Id) AS Id
INTO   #PS
FROM   dbo.ProductTypes child
JOIN   dbo.ProductTypes parent ON parent.Id = child.ParentId
GROUP BY parent.Name, child.Name;

DECLARE @defaultCategory int = (SELECT MIN(Id) FROM dbo.ProductCategories WHERE IsActive = 1);
IF @defaultCategory IS NULL
BEGIN RAISERROR('ABORT: no active ProductCategory exists; Products.CategoryId is NOT NULL.',16,1); RETURN; END;

PRINT '';
PRINT '--- preflight ---';
DECLARE @bad int;

SELECT @bad = COUNT(*) FROM #Cert s WHERE s.CertType IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM #CT x WHERE x.Name = s.CertType);
IF @bad > 0 BEGIN
    SELECT DISTINCT 'UNRESOLVED CertificationType' AS Problem, s.CertType AS Value FROM #Cert s
      WHERE s.CertType IS NOT NULL AND NOT EXISTS (SELECT 1 FROM #CT x WHERE x.Name = s.CertType);
    SET @errmsg = CAST(@bad AS nvarchar(10)) + ' row(s) reference an unknown Certification Type.';
    RAISERROR(@errmsg,16,1); RETURN; END;

SELECT @bad = COUNT(*) FROM #Cert s WHERE s.Category IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM #PC x WHERE x.Name = s.Category);
IF @bad > 0 BEGIN
    SELECT DISTINCT 'UNRESOLVED ProductCategory' AS Problem, s.Category AS Value FROM #Cert s
      WHERE s.Category IS NOT NULL AND NOT EXISTS (SELECT 1 FROM #PC x WHERE x.Name = s.Category);
    SET @errmsg = CAST(@bad AS nvarchar(10)) + ' row(s) reference an unknown Product Category.';
    RAISERROR(@errmsg,16,1); RETURN; END;

SELECT @bad = COUNT(*) FROM #Cert s WHERE s.ProductType IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM #PT x WHERE x.Name = s.ProductType);
IF @bad > 0 BEGIN
    SELECT DISTINCT 'UNRESOLVED ProductType' AS Problem, s.ProductType AS Value FROM #Cert s
      WHERE s.ProductType IS NOT NULL AND NOT EXISTS (SELECT 1 FROM #PT x WHERE x.Name = s.ProductType);
    SET @errmsg = CAST(@bad AS nvarchar(10)) + ' row(s) reference an unknown Product Type.';
    RAISERROR(@errmsg,16,1); RETURN; END;

SELECT @bad = COUNT(*) FROM #Cert s WHERE s.Subcategory IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM #PS x WHERE x.Name = s.Subcategory AND x.ParentName = s.ProductType);
IF @bad > 0 BEGIN
    SELECT DISTINCT 'UNRESOLVED ProductSubcategory' AS Problem,
           s.ProductType + ' > ' + s.Subcategory AS Value FROM #Cert s
      WHERE s.Subcategory IS NOT NULL
        AND NOT EXISTS (SELECT 1 FROM #PS x WHERE x.Name = s.Subcategory AND x.ParentName = s.ProductType);
    SET @errmsg = CAST(@bad AS nvarchar(10)) + ' row(s) name a subcategory that is not a child of their product type.';
    RAISERROR(@errmsg,16,1); RETURN; END;

SELECT @bad = COUNT(*) FROM #Cert s WHERE s.Pathway IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM #PW x WHERE x.Name = s.Pathway);
IF @bad > 0 BEGIN
    SELECT DISTINCT 'UNRESOLVED Pathway' AS Problem, s.Pathway AS Value FROM #Cert s
      WHERE s.Pathway IS NOT NULL AND NOT EXISTS (SELECT 1 FROM #PW x WHERE x.Name = s.Pathway);
    SET @errmsg = CAST(@bad AS nvarchar(10)) + ' row(s) reference an unknown Pathway.';
    RAISERROR(@errmsg,16,1); RETURN; END;

SELECT @bad = COUNT(*) FROM #CertTag t WHERE NOT EXISTS (SELECT 1 FROM dbo.Tags g WHERE g.Name = t.TagName);
IF @bad > 0 BEGIN
    SELECT DISTINCT 'UNRESOLVED Tag' AS Problem, t.TagName AS Value FROM #CertTag t
      WHERE NOT EXISTS (SELECT 1 FROM dbo.Tags g WHERE g.Name = t.TagName);
    SET @errmsg = CAST(@bad AS nvarchar(10)) + ' tag link(s) reference an unknown Tag. Tags are not auto-created.';
    RAISERROR(@errmsg,16,1); RETURN; END;

SELECT @bad = COUNT(*) FROM #CertPartner p WHERE NOT EXISTS (SELECT 1 FROM dbo.Partners r WHERE r.Name = p.PartnerName);
IF @bad > 0 BEGIN
    SELECT DISTINCT 'UNRESOLVED Partner' AS Problem, p.PartnerName AS Value FROM #CertPartner p
      WHERE NOT EXISTS (SELECT 1 FROM dbo.Partners r WHERE r.Name = p.PartnerName);
    SET @errmsg = CAST(@bad AS nvarchar(10)) + ' partner link(s) reference an unknown Partner.';
    RAISERROR(@errmsg,16,1); RETURN; END;

-- Status must be one of the 8 in Models/CertificateStatus.cs. A typo here would
-- create a ninth state that no filter, badge or workflow knows about.
SELECT @bad = COUNT(*) FROM #Cert s WHERE s.Status NOT IN
  ('Draft','Client Approved - Not Paid','Client Approved - Confidential','Client Approved - Paid',
   'Hold','Expired','Republished','Expired - No Annual Verification');
IF @bad > 0 BEGIN
    SELECT DISTINCT 'UNKNOWN Status' AS Problem, s.Status AS Value FROM #Cert s WHERE s.Status NOT IN
      ('Draft','Client Approved - Not Paid','Client Approved - Confidential','Client Approved - Paid',
       'Hold','Expired','Republished','Expired - No Annual Verification');
    SET @errmsg = CAST(@bad AS nvarchar(10)) + ' row(s) carry a status that is not one of the 8 defined states.';
    RAISERROR(@errmsg,16,1); RETURN; END;

-- Certificate.Number is UNIQUE and this is an ADDITIVE load: a number that is
-- already present is either a re-run or an unflagged renewal. Either way, stop.
SELECT @bad = COUNT(DISTINCT s.Number) FROM #Cert s JOIN dbo.Certificates c ON c.Number = s.Number;
IF @bad > 0 BEGIN
    SELECT DISTINCT 'ALREADY EXISTS' AS Problem, s.Number AS Value FROM #Cert s
      JOIN dbo.Certificates c ON c.Number = s.Number;
    SET @errmsg = CAST(@bad AS nvarchar(10)) + ' certificate number(s) already exist in the target. This is a re-run, or a renewal that is not in the RENEWALS list.';
    RAISERROR(@errmsg,16,1); RETURN; END;

-- ...and the mirror image: every renewal MUST already exist, exactly once.
SELECT @bad = COUNT(*) FROM #Renewal r
  WHERE (SELECT COUNT(*) FROM dbo.Certificates c WHERE c.Number = r.Number) <> 1;
IF @bad > 0 BEGIN
    SELECT 'RENEWAL TARGET NOT FOUND (or duplicated)' AS Problem, r.Number AS Value,
           (SELECT COUNT(*) FROM dbo.Certificates c WHERE c.Number = r.Number) AS Matches
    FROM #Renewal r WHERE (SELECT COUNT(*) FROM dbo.Certificates c WHERE c.Number = r.Number) <> 1;
    SET @errmsg = CAST(@bad AS nvarchar(10)) + ' renewal(s) do not resolve to exactly one existing certificate.';
    RAISERROR(@errmsg,16,1); RETURN; END;

-- DEF-353: dates are never defaulted or invented.
SELECT @bad = COUNT(*) FROM #Cert WHERE EffectiveDate IS NULL OR ExpirationDate IS NULL;
IF @bad > 0 BEGIN
    SET @errmsg = CAST(@bad AS nvarchar(10)) + ' row(s) have a missing Effective/Expiration date.';
    RAISERROR(@errmsg,16,1); RETURN; END;

SELECT @bad = COUNT(*) FROM #Cert WHERE ExpirationDate < EffectiveDate;
IF @bad > 0 BEGIN
    SELECT 'EXPIRATION BEFORE EFFECTIVE' AS Problem, Number, EffectiveDate, ExpirationDate FROM #Cert
      WHERE ExpirationDate < EffectiveDate;
    SET @errmsg = CAST(@bad AS nvarchar(10)) + ' row(s) expire before they take effect.';
    RAISERROR(@errmsg,16,1); RETURN; END;

-- Every country staged for a CertificateCountries row must be a real CountryList
-- entry as far as the target is concerned, i.e. it must already be spelled the same
-- way somewhere. A new spelling is allowed but is reported so it cannot pass unseen.
IF EXISTS (SELECT 1 FROM #CertCountry cc
           WHERE NOT EXISTS (SELECT 1 FROM dbo.CertificateCountries x WHERE x.Country = cc.Country))
BEGIN
    SELECT DISTINCT 'NEW COUNTRY SPELLING (allowed, review it)' AS Note, cc.Country
    FROM #CertCountry cc
    WHERE NOT EXISTS (SELECT 1 FROM dbo.CertificateCountries x WHERE x.Country = cc.Country);
END;

-- Product IDs may only attach to a certificate this load creates or one already
-- present. An orphan would be silently dropped by the INSERT's JOIN.
SELECT @bad = COUNT(*) FROM #Pid d
  WHERE NOT EXISTS (SELECT 1 FROM #Cert s WHERE s.Number = d.Number)
    AND NOT EXISTS (SELECT 1 FROM #Renewal r WHERE r.Number = d.Number)
    AND NOT EXISTS (SELECT 1 FROM dbo.Certificates c WHERE c.Number = d.Number);
IF @bad > 0 BEGIN
    SELECT DISTINCT 'ORPHAN PRODUCT ID' AS Problem, d.Number AS Value FROM #Pid d
      WHERE NOT EXISTS (SELECT 1 FROM #Cert s WHERE s.Number = d.Number)
        AND NOT EXISTS (SELECT 1 FROM #Renewal r WHERE r.Number = d.Number)
        AND NOT EXISTS (SELECT 1 FROM dbo.Certificates c WHERE c.Number = d.Number);
    SET @errmsg = CAST(@bad AS nvarchar(10)) + ' product ID(s) name a certificate that will not exist.';
    RAISERROR(@errmsg,16,1); RETURN; END;

PRINT '  preflight OK';

-- What is about to be created, before anything is written.
SELECT 'NEW company' AS WillCreate, s.Company AS Name, COUNT(*) AS Certs
FROM   #Cert s
WHERE  s.Company IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.Companies c WHERE c.Name = s.Company)
GROUP BY s.Company ORDER BY s.Company;

--------------------------------------------------------------------- load ---
BEGIN TRANSACTION;

-- 1. Companies
INSERT INTO dbo.Companies (Name, IsActive, IsDeleted, CreatedAt, UpdatedAt)
SELECT DISTINCT s.Company, 1, 0, @now, @now
FROM   #Cert s
WHERE  s.Company IS NOT NULL
  AND  NOT EXISTS (SELECT 1 FROM dbo.Companies c WHERE c.Name = s.Company);
PRINT '  companies inserted   : ' + CAST(@@ROWCOUNT AS varchar(10));

-- 2. Products — CR-186 identity is (CompanyId, Name, CategoryId, ProductTypeId,
--    ProductSubcategoryId). Same name + different type = SEPARATE product, so the
--    subcategory is part of the key, not decoration.
INSERT INTO dbo.Products (Name, CompanyId, CategoryId, ProductTypeId, ProductSubcategoryId,
                          IsActive, IsDeleted, CreatedAt, UpdatedAt)
SELECT DISTINCT s.ProductName, co.Id, ISNULL(pc.Id, @defaultCategory), pt.Id, ps.Id, 1, 0, @now, @now
FROM   #Cert s
JOIN   dbo.Companies co ON co.Name = s.Company
LEFT JOIN #PC pc ON pc.Name = s.Category
LEFT JOIN #PT pt ON pt.Name = s.ProductType
LEFT JOIN #PS ps ON ps.Name = s.Subcategory AND ps.ParentName = s.ProductType
WHERE  s.ProductName IS NOT NULL
  AND  NOT EXISTS (
        SELECT 1 FROM dbo.Products p
        WHERE  p.Name = s.ProductName
          AND  p.CompanyId = co.Id
          AND  p.CategoryId = ISNULL(pc.Id, @defaultCategory)
          AND  ISNULL(p.ProductTypeId, -1) = ISNULL(pt.Id, -1)
          AND  ISNULL(p.ProductSubcategoryId, -1) = ISNULL(ps.Id, -1));
PRINT '  products inserted    : ' + CAST(@@ROWCOUNT AS varchar(10));

-- 3. Certificates
INSERT INTO dbo.Certificates
    (Number, CompanyId, ProductId, CertificationTypeId, ProductCategoryId, ProductTypeId,
     ProductSubcategoryId, Status, EffectiveDate, ExpirationDate, Location, Country, Partner,
     CertificationResults, PathwayId, AllowsDuplicateNumber, CreatedAt, UpdatedAt)
SELECT s.Number, co.Id, p.Id, ct.Id, pc.Id, pt.Id, ps.Id,
       s.Status, s.EffectiveDate, s.ExpirationDate, s.Location, s.Country,
       ISNULL((SELECT TOP 1 cp.PartnerName FROM #CertPartner cp WHERE cp.Number = s.Number
               ORDER BY cp.PartnerName), 'None'),
       s.CertResults, pw.Id,
       0,                        -- no duplicate numbers in this drop; verified in the build
       @now, @now
FROM   #Cert s
JOIN   dbo.Companies co ON co.Name = s.Company
JOIN   #CT ct ON ct.Name = s.CertType
LEFT JOIN #PC pc ON pc.Name = s.Category
LEFT JOIN #PT pt ON pt.Name = s.ProductType
LEFT JOIN #PS ps ON ps.Name = s.Subcategory AND ps.ParentName = s.ProductType
LEFT JOIN #PW pw ON pw.Name = s.Pathway
JOIN   dbo.Products p
       ON  p.Name = s.ProductName
       AND p.CompanyId = co.Id
       AND p.CategoryId = ISNULL(pc.Id, @defaultCategory)
       AND ISNULL(p.ProductTypeId, -1) = ISNULL(pt.Id, -1)
       AND ISNULL(p.ProductSubcategoryId, -1) = ISNULL(ps.Id, -1);
PRINT '  certificates inserted: ' + CAST(@@ROWCOUNT AS varchar(10));

-- 4. Renewals — dates and status only. Company, product, certification type,
--    PDF and thumbnail links are all left exactly as they are.
UPDATE c
SET    c.Status         = r.Status,
       c.EffectiveDate  = r.EffectiveDate,
       c.ExpirationDate = r.ExpirationDate,
       c.Location       = ISNULL(r.Location, c.Location),
       c.UpdatedAt      = @now
FROM   dbo.Certificates c
JOIN   #Renewal r ON r.Number = c.Number;
PRINT '  renewals updated     : ' + CAST(@@ROWCOUNT AS varchar(10));

-- 5. Countries (CR-037). Certificate.Country already holds the PRIMARY country;
--    this is the full set, one row per country, for filters and search.
INSERT INTO dbo.CertificateCountries (CertificateId, Country)
SELECT DISTINCT c.Id, cc.Country
FROM   #CertCountry cc
JOIN   dbo.Certificates c ON c.Number = cc.Number
WHERE  NOT EXISTS (SELECT 1 FROM dbo.CertificateCountries x
                   WHERE x.CertificateId = c.Id AND x.Country = cc.Country);
PRINT '  country rows         : ' + CAST(@@ROWCOUNT AS varchar(10));

-- 6. Tag links (external + internal share one M2M)
INSERT INTO dbo.CertificateTag (CertificatesId, TagsId)
SELECT DISTINCT c.Id, g.Id
FROM   #CertTag t
JOIN   dbo.Certificates c ON c.Number = t.Number
JOIN   dbo.Tags g ON g.Name = t.TagName
WHERE  NOT EXISTS (SELECT 1 FROM dbo.CertificateTag x WHERE x.CertificatesId = c.Id AND x.TagsId = g.Id);
PRINT '  cert-tag links       : ' + CAST(@@ROWCOUNT AS varchar(10));

-- 7. Partner links (CR-171). Certificate-level association is the FIRST of the two
--    filters a partner request passes; without it the product IDs below reach nobody.
INSERT INTO dbo.CertificatePartners (CertificatesId, PartnersId)
SELECT DISTINCT c.Id, r.Id
FROM   #CertPartner p
JOIN   dbo.Certificates c ON c.Number = p.Number
JOIN   dbo.Partners r ON r.Name = p.PartnerName
WHERE  NOT EXISTS (SELECT 1 FROM dbo.CertificatePartners x WHERE x.CertificatesId = c.Id AND x.PartnersId = r.Id);
PRINT '  cert-partner links   : ' + CAST(@@ROWCOUNT AS varchar(10));

-- 8. Product IDs. CR-015: Status derives from the parent certificate; it is never
--    carried in the source and never chosen by a user.
INSERT INTO dbo.CertificateProductIds
    (CertificateId, ProductIdType, ProductIdValue, Status, Partner, ProductName,
     IsActive, IsPublic, DisplayOrder, CreatedAt, UpdatedAt)
SELECT c.Id, d.IdType, d.IdValue,
       CASE WHEN c.Status IN ('Client Approved - Paid', 'Republished') THEN 'Active' ELSE 'Inactive' END,
       d.PartnerName, d.ProductName,
       1, 1,
       ISNULL((SELECT MAX(x.DisplayOrder) FROM dbo.CertificateProductIds x WHERE x.CertificateId = c.Id), 0)
         + ROW_NUMBER() OVER (PARTITION BY c.Id ORDER BY d.Seq),
       @now, @now
FROM   #Pid d
JOIN   dbo.Certificates c ON c.Number = d.Number
WHERE  NOT EXISTS (SELECT 1 FROM dbo.CertificateProductIds x
                   WHERE x.CertificateId = c.Id AND x.ProductIdType = d.IdType
                     AND x.ProductIdValue = d.IdValue);
PRINT '  product ids          : ' + CAST(@@ROWCOUNT AS varchar(10));

-- 9. Per-product-ID partner links (CR-171 Model A). An UNTAGGED product ID is
--    treated as SHARED and returned to EVERY partner (DEF-352) — a missing link
--    here is not "hidden", it is over-exposed.
INSERT INTO dbo.CertificateProductIdPartners (CertificateProductIdId, PartnersId)
SELECT DISTINCT pid.Id, r.Id
FROM   #Pid d
JOIN   dbo.Certificates c ON c.Number = d.Number
JOIN   dbo.CertificateProductIds pid
       ON pid.CertificateId = c.Id AND pid.ProductIdType = d.IdType AND pid.ProductIdValue = d.IdValue
JOIN   dbo.Partners r ON r.Name = d.PartnerName
WHERE  d.PartnerName IS NOT NULL
  AND  NOT EXISTS (SELECT 1 FROM dbo.CertificateProductIdPartners x
                   WHERE x.CertificateProductIdId = pid.Id AND x.PartnersId = r.Id);
PRINT '  product-id partners  : ' + CAST(@@ROWCOUNT AS varchar(10));

--------------------------------------------------------------- assertions ---
SET @errmsg = NULL;

IF (SELECT COUNT(*) FROM dbo.Certificates c WHERE c.Number IN (SELECT Number FROM #Cert))
   <> (SELECT COUNT(*) FROM #Cert)
    SET @errmsg = 'certificate count does not match staging';

IF EXISTS (SELECT 1 FROM dbo.Certificates WHERE Number IS NOT NULL AND NormalizedNumber IS NULL)
    SET @errmsg = 'NormalizedNumber is NULL — CR-187 computed column did not populate';

-- Every staged country must have landed, and Certificate.Country must be one of them.
IF (SELECT COUNT(*) FROM dbo.CertificateCountries cc
    JOIN dbo.Certificates c ON c.Id = cc.CertificateId
    WHERE c.Number IN (SELECT Number FROM #CertCountry))
   <> (SELECT COUNT(DISTINCT Number + '|' + Country) FROM #CertCountry)
    SET @errmsg = 'CertificateCountries row count does not match staging';

IF EXISTS (SELECT 1 FROM dbo.Certificates c
           WHERE c.Number IN (SELECT Number FROM #Cert)
             AND c.Country IS NOT NULL AND LTRIM(RTRIM(c.Country)) <> ''
             AND NOT EXISTS (SELECT 1 FROM dbo.CertificateCountries cc
                             WHERE cc.CertificateId = c.Id AND cc.Country = c.Country))
    SET @errmsg = 'a certificate has a primary Country with no matching CertificateCountries row';

-- The renewals must have moved, and nothing else about them may have.
IF EXISTS (SELECT 1 FROM dbo.Certificates c JOIN #Renewal r ON r.Number = c.Number
           WHERE c.Status <> r.Status OR c.EffectiveDate <> r.EffectiveDate
              OR c.ExpirationDate <> r.ExpirationDate)
    SET @errmsg = 'a renewal did not take the values it was given';

IF (SELECT COUNT(*) FROM dbo.CertificateProductIds pid
    JOIN dbo.Certificates c ON c.Id = pid.CertificateId
    JOIN #Pid d ON d.Number = c.Number AND d.IdType = pid.ProductIdType AND d.IdValue = pid.ProductIdValue)
   <> (SELECT COUNT(*) FROM #Pid)
    SET @errmsg = 'product-id count does not match staging';

-- Partner-scoped product IDs are useless unless the CERTIFICATE is also associated
-- (feedback_partner_scoping_two_filters). Catch the August Gate-3 shape here.
IF EXISTS (SELECT 1 FROM dbo.CertificateProductIds pid
           JOIN dbo.Certificates c ON c.Id = pid.CertificateId
           WHERE c.Number IN (SELECT Number FROM #Cert)
             AND pid.Partner IS NOT NULL
             AND NOT EXISTS (SELECT 1 FROM dbo.CertificatePartners cp WHERE cp.CertificatesId = c.Id))
    SET @errmsg = 'a certificate carries partner-tagged product IDs but has no CertificatePartners row — no partner would receive them';

IF @errmsg IS NOT NULL
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR('LOAD ROLLED BACK: %s', 16, 1, @errmsg);
    RETURN;
END;

COMMIT TRANSACTION;

PRINT '';
PRINT '=================== POST-LOAD ===================';
SELECT 'Certificates' AS TableName, COUNT(*) AS [Rows] FROM dbo.Certificates
UNION ALL SELECT 'Companies', COUNT(*) FROM dbo.Companies
UNION ALL SELECT 'Products', COUNT(*) FROM dbo.Products
UNION ALL SELECT 'CertificateCountries', COUNT(*) FROM dbo.CertificateCountries
UNION ALL SELECT 'CertificateProductIds', COUNT(*) FROM dbo.CertificateProductIds
UNION ALL SELECT 'CertificateProductIdPartners', COUNT(*) FROM dbo.CertificateProductIdPartners
UNION ALL SELECT 'CertificatePartners', COUNT(*) FROM dbo.CertificatePartners
UNION ALL SELECT 'CertificateTag', COUNT(*) FROM dbo.CertificateTag;

SELECT Status, COUNT(*) AS Certs FROM dbo.Certificates
WHERE Number IN (SELECT Number FROM #Cert) GROUP BY Status ORDER BY COUNT(*) DESC;

SELECT c.Number, c.Country AS PrimaryCountry,
       STUFF((SELECT ', ' + cc.Country FROM dbo.CertificateCountries cc
              WHERE cc.CertificateId = c.Id ORDER BY cc.Country FOR XML PATH('')),1,2,'') AS AllCountries
FROM   dbo.Certificates c
WHERE  c.Number IN (SELECT Number FROM #Cert)
  AND  (SELECT COUNT(*) FROM dbo.CertificateCountries cc WHERE cc.CertificateId = c.Id) > 1
ORDER BY c.Number;

PRINT 'LOAD COMPLETE. Run the reconciliation before accepting.';
""")

    path = os.path.join(outdir, "batch3-load.sql")
    # UTF-16 LE + BOM, deliberately. sqlcmd auto-detects the BOM; a UTF-8 file gets
    # decoded as the ANSI codepage and every ® and ™ is silently corrupted.
    with io.open(path, "w", encoding="utf-16-le", newline="\r\n") as fh:
        fh.write("﻿" + "\n".join(out))
    return path


def write_undo(outdir, stamp, certs, renewals):
    """Reverses this load: deletes what it inserted, restores the renewals."""
    numbers = sorted(r["Certificate #"] for r in certs)
    lit = ",\n    ".join(f"({sq(n)})" for n in numbers)

    lines = [f"""/* ============================================================================
   UNDO for batch3-load.sql  ({stamp})
   ----------------------------------------------------------------------------
   Deletes the {len(certs)} certificates this load inserted, together with their
   child rows, and puts the {len(renewals)} renewed certificates back to the values
   they held before. Companies, Products and the new ProductType row are LEFT IN
   PLACE: they are shared reference data and may have been used by other work by
   the time this runs. Delete them by hand if the load is being abandoned wholesale.

   The renewal restore values below are the pre-load state read from the target on
   2026-09-03 and are hard-coded here on purpose — a generated undo that re-reads
   the current row would restore whatever it finds, which after a second load is
   not the original.

   RUN WITH:  sqlcmd -S <server> -d <database> -I -b -i this-file.sql
============================================================================ */
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;

IF OBJECT_ID('tempdb..#Undo') IS NOT NULL DROP TABLE #Undo;
CREATE TABLE #Undo (Number nvarchar(100) NOT NULL);
INSERT INTO #Undo (Number) VALUES
    {lit};

BEGIN TRANSACTION;

DECLARE @ids TABLE (Id int PRIMARY KEY);
INSERT INTO @ids (Id) SELECT c.Id FROM dbo.Certificates c JOIN #Undo u ON u.Number = c.Number;
PRINT 'certificates to remove: ' + CAST((SELECT COUNT(*) FROM @ids) AS varchar(10));

DELETE FROM dbo.CertificateProductIdPartners
WHERE CertificateProductIdId IN (SELECT Id FROM dbo.CertificateProductIds WHERE CertificateId IN (SELECT Id FROM @ids));
PRINT '  product-id partner links: ' + CAST(@@ROWCOUNT AS varchar(10));

DELETE FROM dbo.CertificateProductIds WHERE CertificateId IN (SELECT Id FROM @ids);
PRINT '  product ids             : ' + CAST(@@ROWCOUNT AS varchar(10));

DELETE FROM dbo.CertificateCountries WHERE CertificateId IN (SELECT Id FROM @ids);
PRINT '  country rows            : ' + CAST(@@ROWCOUNT AS varchar(10));

DELETE FROM dbo.CertificatePartners WHERE CertificatesId IN (SELECT Id FROM @ids);
PRINT '  partner links           : ' + CAST(@@ROWCOUNT AS varchar(10));

DELETE FROM dbo.CertificateTag WHERE CertificatesId IN (SELECT Id FROM @ids);
PRINT '  tag links               : ' + CAST(@@ROWCOUNT AS varchar(10));

DELETE FROM dbo.CertificateCertificationTypeSubcategories WHERE CertificateId IN (SELECT Id FROM @ids);
PRINT '  cert-subcategory links  : ' + CAST(@@ROWCOUNT AS varchar(10));

DELETE FROM dbo.CertificateStatusHistory WHERE CertificateId IN (SELECT Id FROM @ids);
PRINT '  status history          : ' + CAST(@@ROWCOUNT AS varchar(10));

-- The remaining two FK children. Neither is written by the load, but a certificate
-- that has been open in the app since could have acquired them, and Certificates
-- cannot be deleted while they exist (DEF-337: audit the joins, do not assume).
DELETE FROM dbo.CustomFieldValues WHERE CertificateId IN (SELECT Id FROM @ids);
PRINT '  custom field values     : ' + CAST(@@ROWCOUNT AS varchar(10));

DELETE FROM dbo.CertificateDocuments WHERE CertificateId IN (SELECT Id FROM @ids);
PRINT '  documents               : ' + CAST(@@ROWCOUNT AS varchar(10));

DELETE FROM dbo.Certificates WHERE Id IN (SELECT Id FROM @ids);
PRINT '  certificates            : ' + CAST(@@ROWCOUNT AS varchar(10));
"""]

    lines.append("\n-- restore the renewals to their pre-load values ----------------------------")
    for r in renewals:
        num = r["Certificate #"]
        pre = RENEWAL_PRE_STATE.get(num)
        if not pre:
            sys.exit(f"ABORT: no RENEWAL_PRE_STATE recorded for {num}; the undo would be a guess.")
        status, eff, exp, loc = pre
        lines.append(f"""
UPDATE dbo.Certificates
SET    Status = {sq(status)}, EffectiveDate = '{eff}', ExpirationDate = '{exp}',
       Location = {sq(loc)}, UpdatedAt = SYSUTCDATETIME()
WHERE  Number = {sq(num)};
PRINT '  restored {num}: ' + CAST(@@ROWCOUNT AS varchar(10)) + ' row(s)';""")

    lines.append("""
COMMIT TRANSACTION;
PRINT '';
PRINT 'UNDO COMPLETE.';
SELECT COUNT(*) AS CertificatesRemaining FROM dbo.Certificates;
""")

    path = os.path.join(outdir, "batch3-UNDO.sql")
    with io.open(path, "w", encoding="utf-16-le", newline="\r\n") as fh:
        fh.write("﻿" + "\n".join(lines))
    return path


if __name__ == "__main__":
    main()
