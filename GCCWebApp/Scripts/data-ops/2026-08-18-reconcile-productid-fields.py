#!/usr/bin/env python3
"""
Field-level reconciliation for certificate product identifiers (ASINs).
=======================================================================

The existing check (`2026-08-15-verify-spreadsheet-to-database.py`) compares the PAIRING only --
the 3-tuple (certificate number, id type, id value) -- as sets in both directions. It proves the
right ASINs hang off the right certificates. It says nothing about the fields carried ON each
product identifier, and those were never verified. This closes that gap for the two that matter:

  productName   sourced from the ASIN worksheet's own `productName` column
  partner tag   NOT in the ASIN worksheet at all -- it is INHERITED from the certificate's
                `Partner (Amazon)` column in the certificate workbook
                (`2026-08-13-migration-transform.py:519`). So "correct" means: every ASIN on a
                certificate whose source Partner is Amazon must be tagged Amazon, and every ASIN
                on a certificate with a blank Partner must carry no tag.

It also audits the collapse. 4,791 source rows were deduplicated away by the 3-tuple key. If two
collapsed rows disagreed on productName, one silently won and no existing check would notice --
so disagreements among collapsed rows are reported separately from genuine mismatches.

Compares against the CLIENT'S WORKBOOKS, not our transform output, so a transform bug cannot hide.

Outputs (./_productid_recon/<timestamp>/)
  summary.csv                    one row per field, with counts
  productname-mismatches.csv     per-ASIN productName differences
  partner-mismatches.csv         per-ASIN partner tag differences
  collapsed-disagreements.csv    duplicate source rows that disagreed before dedup
  blank-productname.csv          product IDs with no product name in the database

Usage:  python 2026-08-18-reconcile-productid-fields.py
"""
import csv
import datetime
import os
import re
import sys
import unicodedata

try:
    import openpyxl
except ImportError:
    sys.exit("openpyxl required")

HERE = os.path.dirname(os.path.abspath(__file__))
PROJ = os.path.dirname(os.path.dirname(HERE))
# PROJ = ...\GCCWebApp\GCCWebApp ; the source workbooks sit beside the outer GCCWebApp folder.
SRC = os.path.join(os.path.dirname(os.path.dirname(PROJ)), "DataMigrationTestDev")
CERTS = os.path.join(SRC, "GCC Database Migration_Certs_8.12.26V2.xlsx")
ASINS = os.path.join(SRC, "GCC ASIN Master List_8.12.26.xlsm")
ASIN_RE = re.compile(r"^[A-Z0-9]{10}$")


def clean(v) -> str:
    if v is None:
        return ""
    return re.sub(r"\s+", " ", str(v).replace(" ", " ").strip())


def norm(v) -> str:
    """Comparison key: case/space/accent-insensitive, so a trademark symbol or a double
    space never reads as a data difference."""
    s = unicodedata.normalize("NFD", clean(v))
    s = "".join(c for c in s if unicodedata.category(c) != "Mn")
    return re.sub(r"\s+", " ", s.lower()).strip()


def hdr_index(ws):
    idx = {}
    for row in ws.iter_rows(min_row=1, max_row=1, values_only=True):
        for i, h in enumerate(row):
            if h is not None:
                idx[norm(h)] = i
        break
    return idx


def col(idx, *names):
    for n in names:
        if norm(n) in idx:
            return idx[norm(n)]
    for n in names:
        for k, v in idx.items():
            if norm(n) in k:
                return v
    return None


def get(row, i):
    return row[i] if i is not None and i < len(row) else None


# ------------------------------------------------------------------ source: ASIN worksheet
print(f"reading {os.path.basename(ASINS)}")
wb = openpyxl.load_workbook(ASINS, read_only=True, data_only=True)
ws = wb[wb.sheetnames[0]]
idx = hdr_index(ws)
c_num, c_type, c_val = col(idx, "certificateNumber"), col(idx, "type"), col(idx, "value")
c_pname = col(idx, "productName")
if None in (c_num, c_type, c_val, c_pname):
    sys.exit(f"ASIN sheet: could not locate required columns (got {c_num},{c_type},{c_val},{c_pname})")

src_name = {}            # (cert,type,value) -> productName (first seen)
collapsed_conflict = []  # duplicates that disagreed on productName
rows_seen = 0
for row in ws.iter_rows(min_row=2, values_only=True):
    if not row or all(c is None or clean(c) == "" for c in row):
        continue
    rows_seen += 1
    num, typ, val = clean(get(row, c_num)), clean(get(row, c_type)).upper(), clean(get(row, c_val)).upper()
    if not num or not val:
        continue
    if typ == "ASIN" and not ASIN_RE.match(val):
        continue                                   # the 9 known-invalid values
    key = (norm(num), typ, val)
    pname = clean(get(row, c_pname))
    if key in src_name:
        if norm(src_name[key]) != norm(pname):
            collapsed_conflict.append({
                "CertificateNumber": num, "Type": typ, "Value": val,
                "FirstProductName": src_name[key], "ConflictingProductName": pname})
    else:
        src_name[key] = pname
print(f"  {rows_seen} rows -> {len(src_name)} distinct (cert,type,value)")
print(f"  duplicate rows disagreeing on productName: {len(collapsed_conflict)}")

# ------------------------------------------------------------- source: certificate workbook
print(f"reading {os.path.basename(CERTS)}  (cert-level Partner)")
# ONLY the three sheets the migration loads. The workbook also holds Plan / Field Mapping /
# Full List / Hold List tabs; reading those would overwrite a certificate's partner with a value
# from a sheet that was never loaded.
LOAD_SHEETS = ["Active List 7.31.26", "Expired List 2 - 8.11.26", "Expired List 1 - 4.9.26"]
# Of those three, only two actually carry the column. "Expired List 1 - 4.9.26" has 8 columns and
# no Partner at all, so every certificate from it legitimately has no source partner.
wb2 = openpyxl.load_workbook(CERTS, read_only=True, data_only=True)
cert_partner = {}
for sname in LOAD_SHEETS:
    if sname not in wb2.sheetnames:
        print(f"  !! sheet not found: {sname}")
        continue
    ws2 = wb2[sname]
    idx2 = hdr_index(ws2)
    cn = col(idx2, "CERTIFICATE_NUMBER")
    cp = col(idx2, "Partner (Amazon)")
    if cn is None:
        print(f"  !! {sname}: no CERTIFICATE_NUMBER column — skipped")
        continue
    n = tagged = 0
    for row in ws2.iter_rows(min_row=2, values_only=True):
        if not row:
            continue
        num = clean(get(row, cn))
        if not num:
            continue
        val = clean(get(row, cp)) if cp is not None else ""
        # First non-empty value wins; never let a partner-less sheet blank out a real one.
        if val or norm(num) not in cert_partner:
            cert_partner[norm(num)] = val
        n += 1
        if val:
            tagged += 1
    print(f"  {sname}: {n} rows, partner column {'present' if cp is not None else 'ABSENT'}, {tagged} with a partner")

# ------------------------------------------------------------------------------ database
cfg = os.path.join(PROJ, "appsettings.Production.json")
conn_str = re.search(r'"DefaultConnection"\s*:\s*"([^"]+)"', open(cfg, encoding="utf-8").read()).group(1)
import pyodbc
parts, mars = {}, False
for kv in conn_str.split(";"):
    if "=" not in kv:
        continue
    k, v = kv.split("=", 1)
    k, v, kl = k.strip(), v.strip(), k.strip().lower()
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
drivers = [d for d in pyodbc.drivers() if "SQL Server" in d]
cn = pyodbc.connect("DRIVER={%s};%s" % (drivers[-1], ";".join(f"{k}={v}" for k, v in parts.items())), timeout=60)
cur = cn.cursor()
cur.execute("""
    SELECT c.Number, pi.ProductIdType, pi.ProductIdValue, pi.ProductName, pi.Status,
           tags = STUFF((SELECT ', ' + p.Name
                         FROM dbo.CertificateProductIdPartners pp
                         JOIN dbo.Partners p ON p.Id = pp.PartnersId
                         WHERE pp.CertificateProductIdId = pi.Id
                         ORDER BY p.Name FOR XML PATH('')), 1, 2, '')
    FROM dbo.CertificateProductIds pi
    JOIN dbo.Certificates c ON c.Id = pi.CertificateId
""")
db_rows = cur.fetchall()
cn.close()
print(f"database: {len(db_rows)} product identifiers")

# ------------------------------------------------------------------------------- compare
name_mismatch, partner_mismatch, blank_name, not_in_source = [], [], [], []
name_compared = name_match = 0
partner_compared = partner_match = 0

for number, ptype, pvalue, pname, status, tags in db_rows:
    key = (norm(number), clean(ptype).upper(), clean(pvalue).upper())
    db_name = clean(pname)
    db_tags = clean(tags)

    if not db_name:
        blank_name.append({"CertificateNumber": number, "Type": ptype, "Value": pvalue,
                           "SourceProductName": src_name.get(key, "(key not in source)")})

    # --- productName ---
    if key in src_name:
        name_compared += 1
        if norm(src_name[key]) == norm(db_name):
            name_match += 1
        else:
            name_mismatch.append({"CertificateNumber": number, "Type": ptype, "Value": pvalue,
                                  "Spreadsheet": src_name[key], "Database": db_name})
    else:
        not_in_source.append({"CertificateNumber": number, "Type": ptype, "Value": pvalue})

    # --- partner tag: inherited from the certificate's source Partner ---
    expected_partner = clean(cert_partner.get(norm(number), ""))
    partner_compared += 1
    if norm(expected_partner) == norm(db_tags):
        partner_match += 1
    else:
        partner_mismatch.append({"CertificateNumber": number, "Type": ptype, "Value": pvalue,
                                 "ExpectedFromCertificate": expected_partner or "(none)",
                                 "DatabaseTag": db_tags or "(none)"})

# -------------------------------------------------------------------------------- output
stamp = datetime.datetime.now().strftime("%Y%m%d-%H%M%S") if False else "run"
outdir = os.path.join(HERE, "_productid_recon",
                      __import__("time").strftime("%Y%m%d-%H%M%S"))
os.makedirs(outdir, exist_ok=True)


def write(name, rows, headers):
    p = os.path.join(outdir, name)
    with open(p, "w", newline="", encoding="utf-8-sig") as f:
        w = csv.DictWriter(f, fieldnames=headers)
        w.writeheader()
        w.writerows(rows)
    return p


print()
print("=" * 70)
print("FIELD-LEVEL RECONCILIATION — product identifiers")
print("=" * 70)
print(f"  {'field':16} {'compared':>10} {'match':>10} {'MISMATCH':>10}")
print(f"  {'-'*16} {'-'*10} {'-'*10} {'-'*10}")
print(f"  {'productName':16} {name_compared:>10} {name_match:>10} {len(name_mismatch):>10}"
      f"{'   <<<' if name_mismatch else ''}")
print(f"  {'partner tag':16} {partner_compared:>10} {partner_match:>10} {len(partner_mismatch):>10}"
      f"{'   <<<' if partner_mismatch else ''}")
print()
print(f"  product IDs in DB with a blank product name : {len(blank_name)}")
print(f"  product IDs in DB whose key is not in source: {len(not_in_source)}")
print(f"  duplicate source rows that disagreed        : {len(collapsed_conflict)}")

write("summary.csv",
      [{"Field": "productName", "Compared": name_compared, "Match": name_match,
        "Mismatch": len(name_mismatch)},
       {"Field": "partner tag", "Compared": partner_compared, "Match": partner_match,
        "Mismatch": len(partner_mismatch)},
       {"Field": "blank productName in DB", "Compared": len(db_rows), "Match": "",
        "Mismatch": len(blank_name)},
       {"Field": "collapsed rows disagreeing", "Compared": rows_seen, "Match": "",
        "Mismatch": len(collapsed_conflict)}],
      ["Field", "Compared", "Match", "Mismatch"])
write("productname-mismatches.csv", name_mismatch,
      ["CertificateNumber", "Type", "Value", "Spreadsheet", "Database"])
write("partner-mismatches.csv", partner_mismatch,
      ["CertificateNumber", "Type", "Value", "ExpectedFromCertificate", "DatabaseTag"])
write("collapsed-disagreements.csv", collapsed_conflict,
      ["CertificateNumber", "Type", "Value", "FirstProductName", "ConflictingProductName"])
write("blank-productname.csv", blank_name,
      ["CertificateNumber", "Type", "Value", "SourceProductName"])

if name_mismatch:
    print("\n  productName sample:")
    for r in name_mismatch[:5]:
        print(f"    {r['CertificateNumber']} {r['Value']}")
        print(f"      src: {r['Spreadsheet'][:80]!r}")
        print(f"      db : {r['Database'][:80]!r}")
if partner_mismatch:
    print("\n  partner sample:")
    for r in partner_mismatch[:5]:
        print(f"    {r['CertificateNumber']} {r['Value']}  expected={r['ExpectedFromCertificate']!r} db={r['DatabaseTag']!r}")

print(f"\nOutput: {outdir}")
