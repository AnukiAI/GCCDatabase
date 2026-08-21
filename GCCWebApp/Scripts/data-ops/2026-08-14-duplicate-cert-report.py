#!/usr/bin/env python3
"""
DUPLICATE CERTIFICATE NUMBERS — full list + rollup for client review (CR-038)
=============================================================================
Produces the reviewable artefacts behind CR-038:

  duplicate-certificates-FULL-LIST.csv   every affected row, with the evidence
  duplicate-certificates-ROLLUP.csv      the summary tables
  (both under ./_cr038_report/<timestamp>/)

Evidence sources — all measured, none assumed:
  * the transform's quarantine file  = which rows share a certificate number
  * the client's own portal manifest = whether the siblings are genuinely
    DIFFERENT documents (distinct sha256) or byte-identical
  * the raw source workbook          = status, partner, source list

The document check is the one that matters. Plan §11.2 originally judged most of
these "restatements, safe to collapse" from spreadsheet fields alone; the
documents contradict that, and collapsing would destroy real signed certificates.
"""

from __future__ import annotations

import csv
import collections
import datetime
import glob
import io
import os
import re
import sys

try:
    import openpyxl
except ImportError:
    sys.exit("openpyxl is required:  pip install openpyxl")

HERE = os.path.dirname(os.path.abspath(__file__))
REPO_ROOT = os.path.abspath(os.path.join(HERE, "..", "..", "..", ".."))
SRC = os.path.join(REPO_ROOT, "DataMigrationTestDev", "GCC Database Migration_Certs_8.12.26V2.xlsx")
PORTAL = r"C:\Users\swest\GCCPortalPull\manifest.csv"
SHEETS = ["Active List 7.31.26", "Expired List 2 - 8.11.26", "Expired List 1 - 4.9.26"]
COMPARE = ["Company", "ProductName", "CertificationType", "EffectiveDate", "ExpirationDate"]


def newest_transform():
    dirs = sorted(glob.glob(os.path.join(HERE, "_transform_output", "*/")))
    if not dirs:
        sys.exit("run the transform first")
    return dirs[-1]


def main():
    tdir = newest_transform()
    q = list(csv.DictReader(io.open(os.path.join(tdir, "quarantine-duplicate-cert-numbers.csv"),
                                    encoding="utf-8-sig")))
    groups = collections.defaultdict(list)
    for r in q:
        groups[r["CertificateNumber"]].append(r)
    nums = set(groups)

    # ---- portal documents: are the siblings actually different files? --------
    docs = collections.defaultdict(list)
    if os.path.exists(PORTAL):
        for r in csv.DictReader(io.open(PORTAL, encoding="utf-8-sig")):
            if r["certificate_number"] in nums and r["status"] == "downloaded":
                docs[r["certificate_number"]].append(r)

    # ---- status / partner straight from the client's workbook ---------------
    meta = {}
    wb = openpyxl.load_workbook(SRC, read_only=True, data_only=True)
    for sheet in SHEETS:
        if sheet not in wb.sheetnames:
            continue
        ws = wb[sheet]
        hdr = [str(c.value).strip() if c.value else "" for c in next(ws.iter_rows(min_row=1, max_row=1))]

        def col(name):
            for i, h in enumerate(hdr):
                if h.casefold().startswith(name.casefold()):
                    return i
            return None

        ci, si, pi = col("CERTIFICATE_NUMBER"), col("STATUS"), col("Partner")
        for row in ws.iter_rows(min_row=2, values_only=True):
            if ci is None or not row or not row[ci]:
                continue
            n = str(row[ci]).strip()
            if n in nums and n not in meta:
                meta[n] = {
                    "Status": str(row[si]).strip() if si is not None and row[si] else "",
                    "Partner": str(row[pi]).strip() if pi is not None and row[pi] else "",
                    "SourceList": sheet,
                }

    # ---- per-number analysis ------------------------------------------------
    def differs(rows):
        return [f for f in COMPARE if len({(r[f] or "").strip().casefold() for r in rows}) > 1]

    full = []
    per_num = {}
    for num, rows in sorted(groups.items()):
        d = docs.get(num, [])
        distinct_docs = len({r["sha256"] for r in d})
        diff = differs(rows)
        m = meta.get(num, {})

        if distinct_docs > 1:
            disp = "SEPARATE CERTIFICATES — siblings have different documents"
        elif distinct_docs == 1 and len(d) > 1:
            disp = "Same document filed twice — safe to merge"
        elif not diff:
            disp = "Identical on all compared fields — safe to merge"
        else:
            disp = "Differs, but no document evidence — needs client review"

        per_num[num] = {
            "CertificateNumber": num,
            "Rows": len(rows),
            "Company": rows[0]["Company"],
            "SourceList": m.get("SourceList", rows[0]["Sheet"]),
            "Status": m.get("Status", ""),
            "Partner": m.get("Partner", "") or "(none)",
            "PortalDocuments": len(d),
            "DistinctDocuments": distinct_docs,
            "WhatDiffers": ", ".join(diff) if diff else "(nothing on compared fields)",
            "Disposition": disp,
        }
        for r in rows:
            full.append({
                "CertificateNumber": num,
                "RowsForThisNumber": len(rows),
                "SourceList": r["Sheet"],
                "ExcelRow": r["ExcelRow"],
                "Company": r["Company"],
                "ProductName": r["ProductName"],
                "CertificationType": r["CertificationType"],
                "EffectiveDate": r["EffectiveDate"],
                "ExpirationDate": r["ExpirationDate"],
                "Status": m.get("Status", ""),
                "Partner": m.get("Partner", "") or "(none)",
                "DistinctDocumentsForNumber": distinct_docs,
                "WhatDiffersWithinNumber": ", ".join(diff) if diff else "(nothing on compared fields)",
                "Disposition": disp,
            })

    stamp = datetime.datetime.now().strftime("%Y%m%d-%H%M%S")
    out = os.path.join(HERE, "_cr038_report", stamp)
    os.makedirs(out, exist_ok=True)

    with io.open(os.path.join(out, "duplicate-certificates-FULL-LIST.csv"), "w",
                 newline="", encoding="utf-8-sig") as fh:
        w = csv.DictWriter(fh, fieldnames=list(full[0].keys()))
        w.writeheader()
        w.writerows(full)

    # ---- rollup -------------------------------------------------------------
    roll = []

    def add(section, label, value):
        roll.append({"Section": section, "Item": label, "Value": value})

    add("Totals", "Certificate numbers affected", len(groups))
    add("Totals", "Certificate rows affected", len(q))
    add("Totals", "Extra rows beyond one per number", len(q) - len(groups))

    for k, v in sorted(collections.Counter(len(v) for v in groups.values()).items()):
        add("Group size", f"{k} rows sharing one number", v)

    dd = collections.Counter()
    for n, info in per_num.items():
        if info["DistinctDocuments"] > 1:
            dd["Siblings have DIFFERENT documents"] += 1
        elif info["DistinctDocuments"] == 1:
            dd["Siblings share ONE document"] += 1
        else:
            dd["No document found in the portal"] += 1
    for k, v in dd.most_common():
        add("Document evidence", k, v)

    for k, v in collections.Counter(i["WhatDiffers"] for i in per_num.values()).most_common():
        add("What differs", k, v)
    for k, v in collections.Counter(i["Status"] or "(blank)" for i in per_num.values()).most_common():
        add("Status", k, v)
    for k, v in collections.Counter(i["Partner"] for i in per_num.values()).most_common():
        add("Partner (Amazon exposure)", k, v)
    for k, v in collections.Counter(i["SourceList"] for i in per_num.values()).most_common():
        add("Source list", k, v)
    for k, v in collections.Counter(i["Company"] for i in per_num.values()).most_common():
        add("Company", k, v)
    for k, v in collections.Counter(i["Disposition"] for i in per_num.values()).most_common():
        add("Suggested disposition", k, v)

    with io.open(os.path.join(out, "duplicate-certificates-ROLLUP.csv"), "w",
                 newline="", encoding="utf-8-sig") as fh:
        w = csv.DictWriter(fh, fieldnames=["Section", "Item", "Value"])
        w.writeheader()
        w.writerows(roll)

    cur = None
    for r in roll:
        if r["Section"] != cur:
            cur = r["Section"]
            print(f"\n{cur}")
        print(f"    {r['Value']:>6}  {r['Item']}")
    print(f"\nFULL LIST : {os.path.join(out, 'duplicate-certificates-FULL-LIST.csv')} ({len(full)} rows)")
    print(f"ROLLUP    : {os.path.join(out, 'duplicate-certificates-ROLLUP.csv')}")


if __name__ == "__main__":
    main()
