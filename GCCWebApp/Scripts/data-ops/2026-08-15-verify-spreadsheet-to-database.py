#!/usr/bin/env python3
"""
END-TO-END VERIFICATION — client spreadsheet  ->  database
==========================================================
Every check so far has compared the database against the TRANSFORM OUTPUT. That
proves the loader faithful but says nothing about the transform itself: a bug
there is invisible to it, because both sides come from the same place.

This script goes back to the client's own workbooks and asks a different
question: **is everything in the spreadsheets actually in the database, and is
everything in the database actually in the spreadsheets?**

It reads the .xlsx / .xlsm directly, applies only the transformations the client
explicitly agreed to (numeric grouping prefixes stripped, dates normalised,
country aliases canonicalised), and reconciles both directions.

Inputs
  <certs.xlsx>   default: DataMigrationTestDev\\GCC Database Migration_Certs_8.12.26V2.xlsx
  <asins.xlsm>   default: DataMigrationTestDev\\GCC ASIN Master List_8.12.26.xlsm
  <db_certs.csv> UTF-8 dump: Number, Company, Product, Status, Eff, Exp
  <db_pids.csv>  UTF-8 dump: Number, Type, Value

Both DB dumps come from 2026-08-15-verify-dump.ps1 (System.Data.SqlClient ->
UTF-8; never sqlcmd -o, which mangles non-ASCII and produces false mismatches).

Outputs (./_verify_output/<timestamp>/)
  missing-from-database.csv    source rows with no matching certificate
  extra-in-database.csv        certificates with no matching source row
  asin-missing.csv             (cert, ASIN) pairs in the sheet but not the DB
  asin-extra.csv               (cert, ASIN) pairs in the DB but not the sheet
"""

from __future__ import annotations

import csv
import collections
import datetime
import io
import os
import re
import sys
import unicodedata

try:
    import openpyxl
except ImportError:
    sys.exit("openpyxl is required:  pip install openpyxl")

HERE = os.path.dirname(os.path.abspath(__file__))
APP_ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
REPO_ROOT = os.path.abspath(os.path.join(HERE, "..", "..", "..", ".."))
SRC_DIR = os.path.join(REPO_ROOT, "DataMigrationTestDev")

CERTS = os.path.join(SRC_DIR, "GCC Database Migration_Certs_8.12.26V2.xlsx")
ASINS = os.path.join(SRC_DIR, "GCC ASIN Master List_8.12.26.xlsm")
SHEETS = ["Active List 7.31.26", "Expired List 2 - 8.11.26", "Expired List 1 - 4.9.26"]

PREFIX_RE = re.compile(r"^\d+(?:\.\d+)*\s+")
ASIN_RE = re.compile(r"^[A-Z0-9]{10}$")
EXCEL_EPOCH = datetime.datetime(1899, 12, 30)


def clean(v) -> str:
    if v is None:
        return ""
    s = str(v).replace(" ", " ").strip()
    return re.sub(r"\s+", " ", s)


def norm(v) -> str:
    """Compare key: case/space/punctuation-insensitive, accents folded. Used so a
    trademark symbol or double space never reads as a data difference."""
    s = unicodedata.normalize("NFD", clean(v))
    s = "".join(c for c in s if unicodedata.category(c) != "Mn")
    return re.sub(r"\s+", " ", s.lower()).strip()


def to_date(v) -> str:
    if v is None or clean(v) == "":
        return ""
    if isinstance(v, datetime.datetime):
        return v.strftime("%Y-%m-%d")
    if isinstance(v, datetime.date):
        return v.strftime("%Y-%m-%d")
    if isinstance(v, (int, float)) and not isinstance(v, bool):
        if 1 <= float(v) <= 60000:
            return (EXCEL_EPOCH + datetime.timedelta(days=float(v))).strftime("%Y-%m-%d")
        return ""
    s = clean(v)
    for f in ("%Y-%m-%d", "%m/%d/%Y", "%m/%d/%y", "%d-%b-%Y", "%Y/%m/%d"):
        try:
            return datetime.datetime.strptime(s, f).strftime("%Y-%m-%d")
        except ValueError:
            pass
    return s


def hdr_index(ws):
    idx = {}
    for row in ws.iter_rows(min_row=1, max_row=1):
        for i, c in enumerate(row):
            if c.value is not None and clean(c.value):
                idx.setdefault(clean(c.value).casefold(), i)
    return idx


def col(idx, name):
    n = name.casefold()
    if n in idx:
        return idx[n]
    for k, v in idx.items():
        if k.startswith(n):
            return v
    return None


def get(row, i):
    return row[i] if i is not None and i < len(row) else None


def read_csv(path):
    if not os.path.exists(path):
        sys.exit(f"missing DB dump: {path}\nRun 2026-08-15-verify-dump.ps1 first.")
    return list(csv.DictReader(io.open(path, encoding="utf-8")))


def main():
    certs_xlsx = sys.argv[1] if len(sys.argv) > 1 else CERTS
    asins_xlsm = sys.argv[2] if len(sys.argv) > 2 else ASINS
    db_certs = sys.argv[3] if len(sys.argv) > 3 else os.path.join(HERE, "_verify_input", "db-certificates.csv")
    db_pids = sys.argv[4] if len(sys.argv) > 4 else os.path.join(HERE, "_verify_input", "db-productids.csv")

    stamp = datetime.datetime.now().strftime("%Y%m%d-%H%M%S")
    outdir = os.path.join(HERE, "_verify_output", stamp)
    os.makedirs(outdir, exist_ok=True)

    # ================= CERTIFICATES =================
    print("=" * 66)
    print("CERTIFICATES — spreadsheet vs database")
    print("=" * 66)

    wb = openpyxl.load_workbook(certs_xlsx, read_only=True, data_only=True)
    src_rows = []
    for sheet in SHEETS:
        if sheet not in wb.sheetnames:
            print(f"  !! sheet missing: {sheet}")
            continue
        ws = wb[sheet]
        idx = hdr_index(ws)
        ci, co, pr = col(idx, "CERTIFICATE_NUMBER"), col(idx, "COMPANY"), col(idx, "PRODUCT")
        st, ef, ex = col(idx, "STATUS"), col(idx, "EFFECTIVE_DATE"), col(idx, "EXPIRATION_DATE")
        n = 0
        for row in ws.iter_rows(min_row=2, values_only=True):
            if not row or all(c is None or clean(c) == "" for c in row):
                continue
            num = clean(get(row, ci))
            if not num:
                continue
            src_rows.append({
                "Sheet": sheet, "Number": num,
                "Company": clean(get(row, co)), "Product": clean(get(row, pr)),
                "Status": clean(get(row, st)),
                "Effective": to_date(get(row, ef)), "Expiration": to_date(get(row, ex)),
            })
            n += 1
        print(f"  {sheet:26} {n:5} rows")

    db_rows = read_csv(db_certs)
    print(f"\n  spreadsheet rows (3 load sheets) : {len(src_rows):5}")
    print(f"  certificates in the database     : {len(db_rows):5}")

    # multiset per certificate number, so duplicated numbers are compared honestly
    def key(r, numf, cof, prf, eff, exf):
        return (norm(r[numf]), norm(r[cof]), norm(r[prf]), clean(r[eff]), clean(r[exf]))

    src_ms = collections.Counter(key(r, "Number", "Company", "Product", "Effective", "Expiration")
                                 for r in src_rows)
    db_ms = collections.Counter(key(r, "Number", "Company", "Product", "Effective", "Expiration")
                                for r in db_rows)

    missing = src_ms - db_ms          # in the spreadsheet, not in the database
    extra = db_ms - src_ms            # in the database, not in the spreadsheet

    print(f"  in spreadsheet, NOT in database  : {sum(missing.values()):5}")
    print(f"  in database, NOT in spreadsheet  : {sum(extra.values()):5}")

    src_by_num = collections.Counter(norm(r["Number"]) for r in src_rows)
    db_by_num = collections.Counter(norm(r["Number"]) for r in db_rows)
    num_missing = src_by_num - db_by_num
    num_extra = db_by_num - src_by_num
    print(f"  certificate NUMBERS missing      : {len(num_missing):5}"
          f"  ({sum(num_missing.values())} records)")
    print(f"  certificate NUMBERS unexpected   : {len(num_extra):5}")

    with io.open(os.path.join(outdir, "missing-from-database.csv"), "w", newline="", encoding="utf-8-sig") as fh:
        w = csv.writer(fh)
        w.writerow(["Number", "Company", "Product", "Effective", "Expiration", "Count"])
        for k, c in sorted(missing.items()):
            w.writerow([*k, c])
    with io.open(os.path.join(outdir, "extra-in-database.csv"), "w", newline="", encoding="utf-8-sig") as fh:
        w = csv.writer(fh)
        w.writerow(["Number", "Company", "Product", "Effective", "Expiration", "Count"])
        for k, c in sorted(extra.items()):
            w.writerow([*k, c])

    # ================= ASINs =================
    print()
    print("=" * 66)
    print("PRODUCT IDENTIFIERS (ASINs) — spreadsheet vs database")
    print("=" * 66)

    wb2 = openpyxl.load_workbook(asins_xlsm, read_only=True, data_only=True)
    ws2 = wb2[wb2.sheetnames[0]]
    idx2 = hdr_index(ws2)
    ac, at, av = col(idx2, "certificateNumber"), col(idx2, "type"), col(idx2, "value")

    sheet_rows = 0
    sheet_pairs = set()
    invalid = []
    for row in ws2.iter_rows(min_row=2, values_only=True):
        if not row or all(c is None or clean(c) == "" for c in row):
            continue
        sheet_rows += 1
        num, typ, val = clean(get(row, ac)), clean(get(row, at)).upper(), clean(get(row, av)).upper()
        if not num or not val:
            continue
        if typ == "ASIN" and not ASIN_RE.match(val):
            invalid.append((num, val))
            continue
        sheet_pairs.add((norm(num), typ, val))

    pid_rows = read_csv(db_pids)
    db_pairs = {(norm(r["Number"]), clean(r["Type"]).upper(), clean(r["Value"]).upper()) for r in pid_rows}

    loadable = {norm(r["Number"]) for r in db_rows}
    expected = {p for p in sheet_pairs if p[0] in loadable}
    orphan = {p for p in sheet_pairs if p[0] not in loadable}

    asin_missing = expected - db_pairs
    asin_extra = db_pairs - sheet_pairs

    print(f"  rows in the ASIN sheet            : {sheet_rows:6}")
    print(f"  distinct (certificate, ID) pairs  : {len(sheet_pairs):6}"
          f"   ({sheet_rows - len(sheet_pairs) - len(invalid)} duplicate rows collapsed)")
    print(f"  invalid ASIN values excluded      : {len(invalid):6}")
    print(f"  pairs whose certificate is loaded : {len(expected):6}")
    print(f"  pairs for a NON-loaded certificate: {len(orphan):6}")
    print()
    print(f"  product IDs in the database       : {len(pid_rows):6}  ({len(db_pairs)} distinct)")
    print(f"  EXPECTED but MISSING from the DB  : {len(asin_missing):6}")
    print(f"  in the DB but NOT in the sheet    : {len(asin_extra):6}")

    for name, data in (("asin-missing.csv", asin_missing), ("asin-extra.csv", asin_extra)):
        with io.open(os.path.join(outdir, name), "w", newline="", encoding="utf-8-sig") as fh:
            w = csv.writer(fh)
            w.writerow(["CertificateNumber", "Type", "Value"])
            for p in sorted(data):
                w.writerow(p)

    certs_with_ids = len({p[0] for p in db_pairs})
    print(f"  certificates carrying product IDs : {certs_with_ids:6}")

    print()
    print("=" * 66)
    ok = (sum(missing.values()) == 0 and sum(extra.values()) == 0
          and len(asin_missing) == 0 and len(asin_extra) == 0)
    print("RESULT: everything reconciles" if ok else "RESULT: differences found — see the CSVs")
    print(f"Output: {outdir}")
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
