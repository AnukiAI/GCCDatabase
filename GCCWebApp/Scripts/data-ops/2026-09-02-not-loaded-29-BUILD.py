# -*- coding: utf-8 -*-
"""Rebuild all 29 withheld ("Not Loaded (29)") rows through the ACTUAL migration
transform, filling the missing fields with the values the client supplied in the
green "Missing Field Entry" column (screenshot, 2026-09-02).

Same shape as 2026-09-01-sofidel-missing-expiration-BUILD.py, which covered only the
2 Sofidel rows and used a placeholder expiry of 2040-12-31. The client has since given
per-certificate values, so that build is superseded.

Emits the three certs-*.csv + productids-all.csv that
2026-08-13-migration-sql-generator.py consumes.
"""
import importlib.util, os, sys, io, csv
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')

HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(HERE, "..", ".."))
XFORM = os.path.join(HERE, "2026-08-13-migration-transform.py")
SRC = r"C:\Users\swest\OneDrive - PointSolve Technology Inc\LocalWest\Projects\GCC Database Solution\DataMigrationTestDev\GCC Database Migration_Certs_8.12.26V2.xlsx"
OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(HERE, "_notloaded29_load")

# ---- client's green "Missing Field Entry" column, transcribed 2026-09-02 -------------
# NOTE: the screenshot's 3rd row reads "20-0588"; the source workbook AND the
# verification workbook both carry this record as 21-0588 (Climate Action Now, Expired,
# missing Product Name, Expired List 1 row 45). 20-0588 exists in neither. Loading as
# 21-0588 -- flagged to Shawn.
FILL = {
    # cert:      {field: value}
    "23-0358": {"Expiration Date": "2040-06-01"},
    "25-0351": {"Expiration Date": "2040-05-01"},

    "21-0588": {"Product Name": "Climate Action Now Audit"},
    "20-0397": {"Product Name": "Findlay, Ohio Facility"},
    "20-0464": {"Product Name": "Kinston, NC Facility"},
    "20-0462": {"Product Name": "Kinston, NC Facility"},
    "21-0704": {"Product Name": "Kinston, NC Facility"},
    "22-0348": {"Product Name": "GreenCircle Audited"},
    "21-0537": {"Product Name": "New Hamburg, Ontario, Canada Facility"},
    "23-0376": {"Product Name": "New Hamburg, Ontario, Canada Facility"},
    "23-0920": {"Product Name": "Waste Diversion System Audit"},
    "25-0271": {"Product Name": "Lititz, PA Facility"},
    "24-0143": {"Product Name": "Waste Diversion System Audit"},

    "23-0092": {"Effective Date": "2023-04-01"},
    "24-0401": {"Effective Date": "2023-08-01"},

    "13-0157": {"Expiration Date": "2026-08-31"},
    "14-0278": {"Expiration Date": "2026-08-31"},
    "15-0067": {"Expiration Date": "2026-08-31"},
    "16-0262": {"Expiration Date": "2026-08-31"},
    "18-0172": {"Expiration Date": "2026-08-31"},
    "19-0397": {"Expiration Date": "2026-08-31"},
    "21-0613": {"Expiration Date": "2026-08-31"},
    "21-0746": {"Expiration Date": "2026-08-31"},
    "21-0749": {"Expiration Date": "2026-08-31"},
    "21-0750": {"Expiration Date": "2026-08-31"},
    "21-0751": {"Expiration Date": "2026-08-31"},

    "24-0353": {"Product Name": "Brass Rod Mill Facility",
                "Effective Date": "2024-05-17", "Expiration Date": "2040-05-17"},
    "24-0354": {"Product Name": "Brass Rod Mill Facility",
                "Effective Date": "2024-05-17", "Expiration Date": "2040-05-17"},
    "24-0355": {"Product Name": "Brass Rod Mill Facility",
                "Effective Date": "2024-05-17", "Expiration Date": "2040-05-17"},
}

spec = importlib.util.spec_from_file_location("xform", XFORM)
m = importlib.util.module_from_spec(spec); spec.loader.exec_module(m)

import openpyxl
wb = openpyxl.load_workbook(SRC, data_only=True)
pathway_fix = {
    "renewable electricity usage": "Renewable electricity usage",
    "energy usage reduction": "Energy usage reduction",
    "onsite fossil fuel usage reduction": "Onsite fossil fuel usage reduction",
}

SHEETS = [(m.SHEET_ACTIVE, "certs-active.csv", True),
          (m.SHEET_EXPIRED2, "certs-expired2.csv", True),
          (m.SHEET_EXPIRED1, "certs-expired1.csv", False)]

os.makedirs(OUT, exist_ok=True)
targets = set(FILL)
seen, problems = set(), []

for sheet, fname, has_new in SHEETS:
    unmappable, email_review = [], []
    rows = list(m.transform_sheet(wb[sheet], sheet, has_new, unmappable, pathway_fix, email_review))
    picked = []
    for excel_row, rec in rows:
        num = rec["Certificate #"].strip()
        if num not in targets or num in seen:
            continue
        seen.add(num)
        before = [f for f in m.REQUIRED_FIELDS if not rec[f].strip()]
        for field, val in FILL[num].items():
            if rec.get(field, "").strip():
                problems.append(f"{num}: {field} already populated ('{rec[field]}') - fill skipped")
                continue
            rec[field] = val
        after = [f for f in m.REQUIRED_FIELDS if not rec[f].strip()]
        if after:
            problems.append(f"{num}: STILL missing after fill -> {', '.join(after)}")
        print(f"  {num}  [{sheet}, excel row {excel_row}]")
        print(f"      missing before : {', '.join(before) or '(none)'}")
        print(f"      filled         : {', '.join(f'{k}={v}' for k, v in FILL[num].items())}")
        print(f"      missing after  : {', '.join(after) or '(none)'}")
        eff, exp = rec.get("Effective Date", ""), rec.get("Expiration Date", "")
        if eff and exp and eff > exp:
            problems.append(f"{num}: effective {eff} is AFTER expiration {exp}")
        picked.append(rec)
    m.write_csv(os.path.join(OUT, fname), m.CERT_HEADERS, picked)
    print(f"--> {fname}: {len(picked)} rows\n")

m.write_csv(os.path.join(OUT, "productids-all.csv"), ["certificateNumber"] + m.PRODUCTID_HEADERS, [])

missing = targets - seen
print("=" * 70)
print(f"targets {len(targets)}  matched {len(seen)}  NOT FOUND IN SOURCE: {sorted(missing) or '(none)'}")
if problems:
    print("\nPROBLEMS:")
    for p in problems: print("  !! " + p)
else:
    print("\nno problems: every row complete, no date inversions, no overwrites")
