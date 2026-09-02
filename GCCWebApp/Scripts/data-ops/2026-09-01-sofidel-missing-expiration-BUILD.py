# -*- coding: utf-8 -*-
"""Rebuild the 2 withheld Sofidel rows through the ACTUAL migration transform,
so the emitted CSV is byte-identical in shape to what the 8/18 loader consumed.

Only difference: Expiration Date is supplied (it is blank at source, which is why
withhold_incomplete() quarantined both rows).
"""
import importlib.util, os, sys, io, csv
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')

REPO = r"C:\Users\swest\OneDrive - PointSolve Technology Inc\LocalWest\Projects\GCC Database Solution\GCCWebApp\GCCWebApp"
XFORM = os.path.join(REPO, "Scripts", "data-ops", "2026-08-13-migration-transform.py")
SRC = r"C:\Users\swest\OneDrive - PointSolve Technology Inc\LocalWest\Projects\GCC Database Solution\DataMigrationTestDev\GCC Database Migration_Certs_8.12.26V2.xlsx"

EXPIRY = sys.argv[1] if len(sys.argv) > 1 else "2040-12-31"
OUT = sys.argv[2] if len(sys.argv) > 2 else os.path.join(os.path.dirname(os.path.abspath(__file__)), "sofidel-2-certs.csv")
TARGETS = {"23-0358", "25-0351"}

spec = importlib.util.spec_from_file_location("xform", XFORM)
m = importlib.util.module_from_spec(spec)
spec.loader.exec_module(m)

import openpyxl
wb = openpyxl.load_workbook(SRC, data_only=True)
ws = wb[m.SHEET_ACTIVE]

unmappable, email_review = [], []
pathway_fix = {
    "renewable electricity usage": "Renewable electricity usage",
    "energy usage reduction": "Energy usage reduction",
    "onsite fossil fuel usage reduction": "Onsite fossil fuel usage reduction",
}
rows = list(m.transform_sheet(ws, m.SHEET_ACTIVE, True, unmappable, pathway_fix, email_review))
print("transform_sheet produced %d rows from '%s'" % (len(rows), m.SHEET_ACTIVE))

picked = [(r, rec) for r, rec in rows if rec["Certificate #"] in TARGETS]
print("matched %d target rows\n" % len(picked))

for excel_row, rec in picked:
    missing_before = [f for f in m.REQUIRED_FIELDS if not rec[f].strip()]
    print("Excel row %d  %s" % (excel_row, rec["Certificate #"]))
    print("   missing at source : %s" % (", ".join(missing_before) or "(none)"))
    rec["Expiration Date"] = EXPIRY
    still = [f for f in m.REQUIRED_FIELDS if not rec[f].strip()]
    print("   missing after fix : %s" % (", ".join(still) or "(none)"))
    for h in m.CERT_HEADERS:
        v = rec.get(h, "")
        if v:
            print("      %-22s = %s" % (h, v))
    print()

with open(OUT, "w", newline="", encoding="utf-8-sig") as f:
    w = csv.DictWriter(f, fieldnames=m.CERT_HEADERS)
    w.writeheader()
    for _, rec in picked:
        w.writerow({h: rec.get(h, "") for h in m.CERT_HEADERS})
print("WROTE %s  (%d data rows, expiration=%s)" % (OUT, len(picked), EXPIRY))
