#!/usr/bin/env python3
"""
ASIN / PRODUCT-ID RECONCILIATION -- value-level, both directions
===============================================================

Why this exists
  reconciliation-summary.csv reconciles 15 CERTIFICATE fields and not one
  product ID. The ASIN half of the migration -- 21,034 source rows, the single
  largest table in the load -- was never checked against the database at all.
  The first question anyone asked about it ("23-0753 shows 16 in the sheet and
  11 in the app") had no artefact to answer it.

What it compares
  Per certificate number, the SET of (Type, Value) pairs on each side, in BOTH
  directions. Counts are deliberately not the test: two sides can agree on 11
  and 11 while holding different ASINs, and a count survives exactly the failure
  it is supposed to detect. Every differing pair is printed individually --
  never a sample, never a tally.

  For pairs present on both sides it also compares ProductName, because DEF-351
  truncated 50.7% of product names while every count matched.

Expected, not drift
  The source lists the same ASIN more than once per certificate under different
  marketing names (4,791 rows across 47 certs). The DB has a UNIQUE index on
  (CertificateId, ProductIdType, ProductIdValue) --
  Migrations/20260420171817_FixProductIdUniqueIndex_PerCertificate.cs -- so the
  transform collapses them at load. Those are reported as COLLAPSED, separately
  from drift, with the product name that was dropped. 23-0753: 16 rows, 11
  distinct ASINs, 11 in the DB. Correct.

Usage
  # 1. dump the target DB (Target is mandatory -- no silent default)
  powershell -File 2026-08-18-asin-reconciliation-dump.ps1 -Target test

  # 2. compare
  python 2026-08-18-asin-reconciliation.py \
      --loaded _reconciliation/loaded-productids-TEST.csv \
      [--asins  "<path to GCC ASIN Master List_8.12.26.xlsm>"] \
      [--transform-dir _transform_output/<ts>/] \
      [--include-inactive]

Output (./_reconciliation/)
  asin-reconciliation-summary.csv      one row per bucket, with the target named
  asin-reconciliation-percert.csv      per cert: source distinct / loaded / drift
  asin-reconciliation-differences.csv  EVERY differing pair, both directions
  asin-reconciliation-collapsed.csv    source duplicates dropped by the unique index

Exit code
  0 = no drift. 1 = drift found (or the dump is missing/unreadable).
"""

from __future__ import annotations

import argparse
import csv
import glob
import io
import os
import re
import sys
from collections import Counter, defaultdict

HERE = os.path.dirname(os.path.abspath(__file__))
# Same anchor the transform uses (2026-08-13-migration-transform.py:75-76).
REPO_ROOT = os.path.abspath(os.path.join(HERE, "..", "..", "..", ".."))
SRC_DIR = os.path.join(REPO_ROOT, "DataMigrationTestDev")
DEFAULT_ASINS = os.path.join(SRC_DIR, "GCC ASIN Master List_8.12.26.xlsm")

# ProductIdService.cs:96 -- the same pattern the importer validates against.
ASIN_RE = re.compile(r"^[A-Z0-9]{10}$")

CERT_FILES = ["certs-active.csv", "certs-expired2.csv", "certs-expired1.csv"]


def clean(v) -> str:
    if v is None:
        return ""
    return re.sub(r"\s+", " ", str(v).replace(" ", " ")).strip()


def norm_name(v: str) -> str:
    """Product-name compare key: whitespace- and case-insensitive."""
    return re.sub(r"\s+", " ", (v or "")).strip().casefold()


def newest_transform_dir() -> str | None:
    dirs = sorted(glob.glob(os.path.join(HERE, "_transform_output", "*/")))
    return dirs[-1] if dirs else None


# ------------------------------------------------------------------ source --
def read_source(path):
    """-> (per_cert {num: {(type,value): name}}, collapsed[], invalid[], row_total)

    First row wins, which is exactly what transform C does (2026-08-13-migration-
    transform.py:503). Recording the LOSER's name too is the point: which of two
    names survives is decided by spreadsheet row order, i.e. arbitrarily, and
    that is a client-visible choice nobody has signed off on.
    """
    import openpyxl

    wb = openpyxl.load_workbook(path, read_only=True, data_only=True)
    ws = wb[wb.sheetnames[0]]
    rows = ws.iter_rows(values_only=True)
    hdr = next(rows)

    def key(h):
        return re.sub(r"[^a-z0-9]", "", str(h).lower()) if h is not None else ""

    nh = [key(h) for h in hdr]

    def col(*cands):
        for c in cands:
            if c in nh:
                return nh.index(c)
        raise SystemExit(f"ASIN sheet is missing a column for {cands!r}; headers = {hdr}")

    c_cert = col("certificatenumber", "certnumber", "certificate")
    c_type = col("type", "idtype")
    c_val = col("value", "productid", "asin")
    c_name = col("productname", "product")

    per_cert = defaultdict(dict)
    collapsed, invalid = [], []
    total = 0

    for excel_row, r in enumerate(rows, start=2):
        if r is None or all(c is None or clean(c) == "" for c in r):
            continue
        total += 1
        cert = clean(r[c_cert] if c_cert < len(r) else None)
        t = clean(r[c_type] if c_type < len(r) else None).upper()
        v = clean(r[c_val] if c_val < len(r) else None).upper()
        n = clean(r[c_name] if c_name < len(r) else None)

        if t == "ASIN" and not ASIN_RE.match(v):
            invalid.append({"ExcelRow": excel_row, "CertificateNumber": cert,
                            "Type": t, "Value": v,
                            "Why": "fails ProductIdService ASIN pattern ^[A-Z0-9]{10}$"})
            continue

        pair = (t, v)
        if pair in per_cert[cert]:
            kept_row, kept_name = per_cert[cert][pair]
            collapsed.append({
                "CertificateNumber": cert, "Type": t, "Value": v,
                "KeptExcelRow": kept_row, "DroppedExcelRow": excel_row,
                "KeptProductName": kept_name, "DroppedProductName": n,
                "NamesDiffer": "YES" if norm_name(kept_name) != norm_name(n) else "no",
            })
        else:
            per_cert[cert][pair] = (excel_row, n)

    return per_cert, collapsed, invalid, total


# ------------------------------------------------------------------ loaded --
def read_loaded(path, include_inactive):
    """-> (per_cert {num: {(type,value): name}}, cert_ids {num: set}, inactive_skipped)"""
    per_cert = defaultdict(dict)
    cert_ids = defaultdict(set)
    inactive = 0
    dup_in_db = []

    with io.open(path, encoding="utf-8-sig") as fh:
        for r in csv.DictReader(fh):
            num = clean(r.get("Certificate #"))
            if not num:
                continue
            if not include_inactive and clean(r.get("IsActive")) not in ("1", "True", "true"):
                inactive += 1
                continue
            t = clean(r.get("Type")).upper()
            v = clean(r.get("Value")).upper()
            n = clean(r.get("ProductName"))
            cert_ids[num].add(clean(r.get("CertificateId")))
            pair = (t, v)
            # CR-038 lets several certificates share a number, so the same pair can
            # legitimately appear twice under one number. Recorded, not merged away.
            if pair in per_cert[num]:
                dup_in_db.append({"CertificateNumber": num, "Type": t, "Value": v})
            per_cert[num][pair] = n

    return per_cert, cert_ids, inactive, dup_in_db


# ------------------------------------------------------------------- main --
def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--loaded", required=True,
                    help="CSV from 2026-08-18-asin-reconciliation-dump.ps1")
    ap.add_argument("--asins", default=DEFAULT_ASINS)
    ap.add_argument("--transform-dir", default=None,
                    help="_transform_output/<ts>/ -- used to tell 'never loaded on "
                         "purpose' apart from 'lost'. Defaults to the newest.")
    ap.add_argument("--include-inactive", action="store_true",
                    help="compare soft-deactivated rows too (default: active only, "
                         "which is what the app shows)")
    args = ap.parse_args()

    if not os.path.exists(args.loaded):
        sys.exit(f"Loaded dump not found: {args.loaded}\n"
                 f"Run: powershell -File 2026-08-18-asin-reconciliation-dump.ps1 -Target test")
    if not os.path.exists(args.asins):
        sys.exit(f"ASIN master list not found: {args.asins}")

    target = "UNKNOWN"
    m = re.search(r"loaded-productids-([A-Za-z0-9_]+)\.csv$", os.path.basename(args.loaded))
    if m:
        target = m.group(1).upper()

    print(f"source : {args.asins}")
    print(f"loaded : {args.loaded}   (target: {target})")
    print(f"scope  : {'active + inactive' if args.include_inactive else 'ACTIVE rows only'}\n")

    src, collapsed, invalid, src_rows = read_source(args.asins)
    db, cert_ids, inactive_skipped, dup_in_db = read_loaded(args.loaded, args.include_inactive)

    # Which certificate numbers did the transform actually emit? A number absent from
    # the load set was quarantined on purpose (dup cert number, etc.) -- reporting that
    # as "ASINs lost" would be a false alarm, and reporting it as nothing would hide it.
    tdir = args.transform_dir or newest_transform_dir()
    load_set = None
    if tdir and os.path.isdir(tdir):
        load_set = set()
        for fn in CERT_FILES:
            p = os.path.join(tdir, fn)
            if os.path.exists(p):
                for r in csv.DictReader(io.open(p, encoding="utf-8-sig")):
                    n = clean(r.get("Certificate #"))
                    if n:
                        load_set.add(n)
        print(f"load set: {len(load_set)} certificate numbers from {tdir}\n")
    else:
        print("load set: (no _transform_output found -- 'not loaded' vs 'quarantined' "
              "cannot be told apart)\n")

    src_pairs = sum(len(v) for v in src.values())
    db_pairs = sum(len(v) for v in db.values())
    print(f"source : {src_rows} rows -> {src_pairs} distinct (cert,type,value) "
          f"across {len(src)} certificate numbers")
    print(f"loaded : {db_pairs} product IDs across {len(db)} certificate numbers")
    if inactive_skipped:
        print(f"         ({inactive_skipped} inactive rows skipped -- rerun with "
              f"--include-inactive to fold them in)")
    print()

    differences = []
    percert = []
    bucket = Counter()

    for num in sorted(set(src) | set(db)):
        s_pairs = src.get(num, {})
        d_pairs = db.get(num, {})

        missing = sorted(set(s_pairs) - set(d_pairs))
        extra = sorted(set(d_pairs) - set(s_pairs))
        shared = sorted(set(s_pairs) & set(d_pairs))

        name_diffs = []
        for pair in shared:
            s_name = s_pairs[pair][1]
            d_name = d_pairs[pair]
            if norm_name(s_name) != norm_name(d_name):
                name_diffs.append((pair, s_name, d_name))

        if num in src and num not in db:
            kind = ("CERT_QUARANTINED_NOT_LOADED"
                    if load_set is not None and num not in load_set
                    else "CERT_MISSING_FROM_DB")
            bucket[kind] += 1
            for pair in missing:
                differences.append({
                    "CertificateNumber": num, "Kind": kind, "Type": pair[0],
                    "Value": pair[1], "SourceProductName": s_pairs[pair][1],
                    "LoadedProductName": "", "SourceExcelRow": s_pairs[pair][0],
                })
        else:
            for pair in missing:
                bucket["PAIR_MISSING_FROM_DB"] += 1
                differences.append({
                    "CertificateNumber": num, "Kind": "PAIR_MISSING_FROM_DB",
                    "Type": pair[0], "Value": pair[1],
                    "SourceProductName": s_pairs[pair][1], "LoadedProductName": "",
                    "SourceExcelRow": s_pairs[pair][0],
                })
            for pair in extra:
                bucket["PAIR_EXTRA_IN_DB"] += 1
                differences.append({
                    "CertificateNumber": num, "Kind": "PAIR_EXTRA_IN_DB",
                    "Type": pair[0], "Value": pair[1], "SourceProductName": "",
                    "LoadedProductName": d_pairs[pair], "SourceExcelRow": "",
                })
            for pair, s_name, d_name in name_diffs:
                bucket["PRODUCT_NAME_MISMATCH"] += 1
                differences.append({
                    "CertificateNumber": num, "Kind": "PRODUCT_NAME_MISMATCH",
                    "Type": pair[0], "Value": pair[1], "SourceProductName": s_name,
                    "LoadedProductName": d_name,
                    "SourceExcelRow": s_pairs[pair][0],
                })

        percert.append({
            "CertificateNumber": num,
            "CertificateRowsInDb": len(cert_ids.get(num, set())),
            "SourceDistinctPairs": len(s_pairs),
            "LoadedPairs": len(d_pairs),
            "MissingFromDb": len(missing),
            "ExtraInDb": len(extra),
            "NameMismatches": len(name_diffs),
            "SourceRowsCollapsed": sum(1 for c in collapsed if c["CertificateNumber"] == num),
            "Verdict": "OK" if not (missing or extra or name_diffs) else "DRIFT",
        })

    # ---- report ------------------------------------------------------------
    print(f"{'bucket':32} {'count':>8}")
    print("-" * 42)
    order = ["PAIR_MISSING_FROM_DB", "PAIR_EXTRA_IN_DB", "PRODUCT_NAME_MISMATCH",
             "CERT_MISSING_FROM_DB", "CERT_QUARANTINED_NOT_LOADED"]
    drift = 0
    for k in order:
        n = bucket.get(k, 0)
        if k != "CERT_QUARANTINED_NOT_LOADED":
            drift += n
        flag = "" if n == 0 else "   <<<"
        print(f"{k:32} {n:8}{flag}")
    print()
    print(f"{'source rows collapsed by unique index':32} {len(collapsed):8}   (expected)")
    print(f"{'  ...where product NAME differed':32} "
          f"{sum(1 for c in collapsed if c['NamesDiffer'] == 'YES'):8}   (review)")
    print(f"{'invalid-pattern source rows':32} {len(invalid):8}")
    if dup_in_db:
        print(f"{'same pair twice under one number':32} {len(dup_in_db):8}   (CR-038 split)")

    bad = [p for p in percert if p["Verdict"] == "DRIFT"]
    if bad:
        print(f"\ncertificates with drift: {len(bad)}")
        for p in sorted(bad, key=lambda x: -(x["MissingFromDb"] + x["ExtraInDb"] + x["NameMismatches"])):
            print(f"  {p['CertificateNumber']:<12} source {p['SourceDistinctPairs']:>5} "
                  f"loaded {p['LoadedPairs']:>5}  missing {p['MissingFromDb']:>4} "
                  f"extra {p['ExtraInDb']:>4} names {p['NameMismatches']:>4}")

    # ---- artefacts ---------------------------------------------------------
    outdir = os.path.join(HERE, "_reconciliation")
    os.makedirs(outdir, exist_ok=True)

    def write(name, fields, rows):
        with io.open(os.path.join(outdir, name), "w", newline="", encoding="utf-8-sig") as fh:
            w = csv.DictWriter(fh, fieldnames=fields)
            w.writeheader()
            w.writerows(rows)

    write("asin-reconciliation-summary.csv",
          ["Target", "Bucket", "Count", "Note"],
          [{"Target": target, "Bucket": k, "Count": bucket.get(k, 0),
            "Note": "drift" if k != "CERT_QUARANTINED_NOT_LOADED" else "expected"}
           for k in order]
          + [{"Target": target, "Bucket": "SOURCE_ROWS", "Count": src_rows, "Note": ""},
             {"Target": target, "Bucket": "SOURCE_DISTINCT_PAIRS", "Count": src_pairs, "Note": ""},
             {"Target": target, "Bucket": "LOADED_PAIRS", "Count": db_pairs, "Note": ""},
             {"Target": target, "Bucket": "COLLAPSED_BY_UNIQUE_INDEX", "Count": len(collapsed),
              "Note": "expected -- DB is UNIQUE(CertificateId,Type,Value)"},
             {"Target": target, "Bucket": "COLLAPSED_WITH_DIFFERENT_NAME",
              "Count": sum(1 for c in collapsed if c["NamesDiffer"] == "YES"),
              "Note": "review -- kept name chosen by spreadsheet row order"},
             {"Target": target, "Bucket": "INVALID_PATTERN_ROWS", "Count": len(invalid), "Note": ""},
             {"Target": target, "Bucket": "INACTIVE_ROWS_SKIPPED", "Count": inactive_skipped,
              "Note": "" if args.include_inactive else "not compared"}])

    write("asin-reconciliation-percert.csv",
          ["CertificateNumber", "CertificateRowsInDb", "SourceDistinctPairs", "LoadedPairs",
           "MissingFromDb", "ExtraInDb", "NameMismatches", "SourceRowsCollapsed", "Verdict"],
          sorted(percert, key=lambda p: p["CertificateNumber"]))

    write("asin-reconciliation-differences.csv",
          ["CertificateNumber", "Kind", "Type", "Value", "SourceProductName",
           "LoadedProductName", "SourceExcelRow"],
          sorted(differences, key=lambda d: (d["Kind"], d["CertificateNumber"], d["Value"])))

    write("asin-reconciliation-collapsed.csv",
          ["CertificateNumber", "Type", "Value", "KeptExcelRow", "DroppedExcelRow",
           "KeptProductName", "DroppedProductName", "NamesDiffer"],
          sorted(collapsed, key=lambda c: (c["CertificateNumber"], c["Value"])))

    if invalid:
        write("asin-reconciliation-invalid.csv",
              ["ExcelRow", "CertificateNumber", "Type", "Value", "Why"], invalid)

    print(f"\nOutput: {outdir}")
    print(f"TOTAL DRIFT: {drift}")
    return 1 if drift else 0


if __name__ == "__main__":
    sys.exit(main())
