#!/usr/bin/env python3
"""
MIGRATION RECONCILIATION — value-level, not count-level
=======================================================

Plan §6 acceptance says "assert post-load counts match the transform output".
**That is not enough, and we proved it.** DEF-351 truncated 50.7% of product
names at the first comma while every count matched exactly — 16,234 in, 16,234
out — because the corruption was INSIDE fields, not row loss. A count check
cannot see that. This script compares VALUES, field by field, for every
certificate.

Inputs
  <transform_dir>  a ./_transform_output/<ts>/ directory (default: newest)
  <loaded_csv>     UTF-8 dump from 2026-08-13-migration-reconciliation.ps1
                   (default: ./_reconciliation/loaded-certificates.csv)

Encoding
  The loaded dump is produced via System.Data.SqlClient -> UTF-8, deliberately
  NOT `sqlcmd -o`, whose console-codepage rendering mangles en dashes and ®.
  That artefact produced 3,086 FALSE mismatches on the first attempt at this
  comparison. Never diff text across a transport that can alter it.

Known-not-comparable, by design
  Subcategory / Cert Subcategories  DEF-348 — no parser mapping, never stored.
                                    Reported separately, not counted as drift.

Output (./_reconciliation/)
  reconciliation-summary.csv    per-field: compared / matched / mismatched
  reconciliation-mismatches.csv every differing value, with source and loaded
"""

from __future__ import annotations

import csv
import glob
import io
import os
import re
import sys
from collections import Counter, defaultdict

HERE = os.path.dirname(os.path.abspath(__file__))

# Fields the importer is expected to persist verbatim.
COMPARE = [
    "Certification Type",
    "Status",
    "Company",
    "Product Name",
    "Product Category",
    "Product Type",
    "Pathway",
    "Cert Results",
    "Effective Date",
    "Expiration Date",
    "Location",
    "Country",
    "Partner",
    "External Tag",
    "Internal Tag",
]

# DEF-348: the template offers these, the parser has no mapping. Counted, not blamed.
NOT_STORED = ["Subcategory", "Cert Subcategories"]

CERT_FILES = ["certs-active.csv", "certs-expired2.csv", "certs-expired1.csv"]


def norm(v: str) -> str:
    """Whitespace/case-insensitive compare key. Multi-value fields (Partner,
    tags) are order-insensitive: the DB returns them sorted, the source does not."""
    s = (v or "").replace(" ", " ").strip()
    s = re.sub(r"\s+", " ", s)
    if "," in s:
        parts = sorted(p.strip().casefold() for p in s.split(",") if p.strip())
        return ", ".join(parts)
    return s.casefold()


def newest_transform_dir() -> str:
    dirs = sorted(glob.glob(os.path.join(HERE, "_transform_output", "*/")))
    if not dirs:
        sys.exit("No _transform_output/<ts>/ directory found.")
    return dirs[-1]


def main():
    tdir = sys.argv[1] if len(sys.argv) > 1 else newest_transform_dir()
    loaded_path = (sys.argv[2] if len(sys.argv) > 2
                   else os.path.join(HERE, "_reconciliation", "loaded-certificates.csv"))

    if not os.path.isdir(tdir):
        sys.exit(f"Not a directory: {tdir}")
    if not os.path.exists(loaded_path):
        sys.exit(f"Loaded dump not found: {loaded_path}\nRun 2026-08-13-migration-reconciliation.ps1 first.")

    print(f"transform : {tdir}")
    print(f"loaded    : {loaded_path}\n")

    # ---- source: what we asked the importer to store -------------------------
    # CR-038: a certificate number can now belong to SEVERAL certificates, so both sides are
    # grouped as LISTS per number and compared as MULTISETS. Keying by number (as this script
    # originally did) silently kept one row per number — which would have skipped exactly the
    # duplicated records this migration exists to preserve. No natural key is unique either:
    # 13 source rows are identical on every compared field, so row-to-row pairing is impossible
    # and a per-number multiset is the only correct comparison.
    source = defaultdict(list)
    dup_src = 0
    for fn in CERT_FILES:
        p = os.path.join(tdir, fn)
        if not os.path.exists(p):
            print(f"!! missing {fn}")
            continue
        for r in csv.DictReader(io.open(p, encoding="utf-8-sig")):
            num = (r["Certificate #"] or "").strip()
            if not num:
                continue
            if num in source:
                dup_src += 1
            source[num].append(r)

    # ---- loaded: what the database actually holds ----------------------------
    loaded = defaultdict(list)
    for r in csv.DictReader(io.open(loaded_path, encoding="utf-8")):
        num = (r["Certificate #"] or "").strip()
        if num:
            loaded[num].append(r)

    src_rows = sum(len(v) for v in source.values())
    load_rows = sum(len(v) for v in loaded.values())
    print(f"source : {src_rows} records across {len(source)} numbers"
          + (f"  ({dup_src} share a number)" if dup_src else ""))
    print(f"loaded : {load_rows} records across {len(loaded)} numbers")

    # per-number record counts must agree before any field comparison means anything
    count_mismatch = [n for n in set(source) & set(loaded) if len(source[n]) != len(loaded[n])]
    print(f"numbers with a different record count: {len(count_mismatch)}"
          + (f"  e.g. {', '.join(count_mismatch[:5])}" if count_mismatch else ""))

    missing = sorted(set(source) - set(loaded))
    extra = sorted(set(loaded) - set(source))
    print(f"in source, NOT loaded: {len(missing)}")
    print(f"loaded, NOT in source: {len(extra)}")
    if missing[:5]:
        print(f"   e.g. {', '.join(missing[:5])}")
    if extra[:5]:
        print(f"   e.g. {', '.join(extra[:5])}")
    print()

    both = sorted(set(source) & set(loaded))

    stats = {f: Counter() for f in COMPARE}
    mismatches = []

    for num in both:
        srows, lrows = source[num], loaded[num]
        for f in COMPARE:
            # multiset compare: same values, same multiplicities, order irrelevant
            svals = sorted(norm((r.get(f) or "").strip()) for r in srows)
            lvals = sorted(norm((r.get(f) or "").strip()) for r in lrows)
            blanks = sum(1 for a, b in zip(svals, lvals) if not a and not b)
            stats[f]["both_blank"] += blanks
            n_compared = max(len(svals), len(lvals)) - blanks
            if n_compared <= 0:
                continue
            stats[f]["compared"] += n_compared
            if svals == lvals:
                stats[f]["match"] += n_compared
            else:
                stats[f]["mismatch"] += n_compared
                sset, lset = Counter(svals), Counter(lvals)
                only_src = list((sset - lset).elements())
                only_load = list((lset - sset).elements())
                raw_src = "; ".join(sorted({(r.get(f) or "").strip() for r in srows}))
                raw_load = "; ".join(sorted({(r.get(f) or "").strip() for r in lrows}))
                mismatches.append({
                    "CertificateNumber": num,
                    "Field": f,
                    "Source": raw_src,
                    "Loaded": raw_load,
                    "Kind": ("BLANK IN DB" if only_src and not only_load else
                             "ADDED BY DB" if only_load and not only_src else
                             "DIFFERENT"),
                })

    print(f"{'field':22} {'compared':>9} {'match':>8} {'MISMATCH':>9}  {'both blank':>10}")
    print("-" * 64)
    total_bad = 0
    for f in COMPARE:
        c = stats[f]
        total_bad += c["mismatch"]
        flag = "" if c["mismatch"] == 0 else "  <<<"
        print(f"{f:22} {c['compared']:9} {c['match']:8} {c['mismatch']:9}  {c['both_blank']:10}{flag}")

    # DEF-348 fields: quantify what never had anywhere to go.
    print()
    for f in NOT_STORED:
        n = sum(1 for num in both for r in source[num] if (r.get(f) or "").strip())
        print(f"NOT STORED (DEF-348)  {f:22} {n} source values with no import path")

    outdir = os.path.join(HERE, "_reconciliation")
    os.makedirs(outdir, exist_ok=True)

    with io.open(os.path.join(outdir, "reconciliation-summary.csv"), "w", newline="", encoding="utf-8-sig") as fh:
        w = csv.writer(fh)
        w.writerow(["Field", "Compared", "Match", "Mismatch", "BothBlank"])
        for f in COMPARE:
            c = stats[f]
            w.writerow([f, c["compared"], c["match"], c["mismatch"], c["both_blank"]])

    if mismatches:
        with io.open(os.path.join(outdir, "reconciliation-mismatches.csv"), "w", newline="", encoding="utf-8-sig") as fh:
            w = csv.DictWriter(fh, fieldnames=["CertificateNumber", "Field", "Kind", "Source", "Loaded"])
            w.writeheader()
            w.writerows(mismatches)
        print()
        print("mismatch kinds:", dict(Counter(m["Kind"] for m in mismatches)))
        byfield = Counter(m["Field"] for m in mismatches)
        for f, n in byfield.most_common():
            ex = next(m for m in mismatches if m["Field"] == f)
            print(f"  {f} x{n}")
            print(f"     src: {ex['Source'][:80]!r}")
            print(f"     db : {ex['Loaded'][:80]!r}")

    print()
    print(f"TOTAL field mismatches: {total_bad}")
    print(f"Output: {outdir}")
    return 1 if (total_bad or missing) else 0


if __name__ == "__main__":
    sys.exit(main())
