#!/usr/bin/env python3
r"""
ASIN ROW MAPPING -- all 21,034 source rows, one-to-one, to the database
======================================================================

Why this exists
  Asked 2026-08-23: "there are 21034 ASINS listed in this spreadsheet but we only
  have 16000 or so in the database. why."

  The earlier artefacts could not answer that. 2026-08-18-asin-reconciliation.py
  and 2026-08-23-asin-record-log.py both key on (certificate, type, value), so
  they emit ~16,237 rows and are silent about the other ~4,800 spreadsheet rows.
  Proving "nothing is missing" against a 16,237-row list does not tell anyone
  what happened to rows 16,238..21,034 -- and that gap is exactly the question.

  This script inverts the axis. It emits ONE ROW PER SOURCE SPREADSHEET ROW, all
  21,034 of them, each carrying its own disposition and, where it was collapsed,
  the spreadsheet row that carries its value in the database instead. Every
  source row is individually accounted for, by number, on screen.

The answer the output demonstrates
  21,034 spreadsheet rows
  -  4,797 rows repeat a (certificate, ASIN) pair already listed on an earlier row
  =  16,237 distinct (certificate, ASIN) pairs
  -      3 pairs whose value is not a valid ASIN ("To be Checked" x2, "08FDWPMP6")
  =  16,234 loaded  <-- exactly the row count in CertificateProductIds

  The database has a UNIQUE index on (CertificateId, ProductIdType, ProductIdValue)
  -- Migrations/20260420171817_FixProductIdUniqueIndex_PerCertificate.cs -- so one
  ASIN can appear only once per certificate. The spreadsheet lists the same ASIN on
  the same certificate several times because each row carries a different marketing
  name for the same product. 21,034 is a count of NAME rows, not of ASINs.

  Cross-check from the other direction: the sheet holds 14,301 distinct ASIN values
  in total. So 21,034 was never the number of ASINs under any definition.

Which row "wins"
  Determined by EVIDENCE, not assumption: among the rows sharing a key, the one
  whose product name matches what the database actually stored is marked LOADED.
  Only where the candidates' names are indistinguishable does it fall back to the
  first row by spreadsheet order. The fallback is labelled in the output so the two
  cases are never confused.

Usage
  python 2026-08-23-asin-row-mapping.py \
      --master "<GCC ASIN Master List_8.12.26.xlsm>" \
      --loaded _reconciliation/loaded-productids-TEST-20260823.csv \
      --certs  _reconciliation/loaded-certs-companies-TEST.csv \
      --out    _reconciliation/_asin_record_log/asin-row-mapping.xlsx

  If the workbook is open in Excel it will be locked; copy it first.
  Exit 0 only if every source row is accounted for and loaded rows == DB rows.
"""

import argparse
import collections
import csv
import re
import sys

import openpyxl
from openpyxl.styles import Alignment, Font, PatternFill
from openpyxl.utils import get_column_letter

WS = re.compile(r"\s+")
ASIN_RE = re.compile(r"^[A-Z0-9]{10}$")
MASTER_SHEET = "ACTIVE LIST"

HDR_FILL = PatternFill("solid", fgColor="1F4D38")
HDR_FONT = Font(color="FFFFFF", bold=True)
FILLS = {
    "LOADED": PatternFill("solid", fgColor="E8F5E9"),
    "DUPLICATE": PatternFill("solid", fgColor="FFF8E1"),
    "NOT LOADED": PatternFill("solid", fgColor="FFEBEE"),
}


def norm(v):
    return "" if v is None else str(v).strip()


def nws(s):
    return WS.sub(" ", s).strip()


def read_master(path):
    wb = openpyxl.load_workbook(path, read_only=True, data_only=True)
    ws = wb[MASTER_SHEET]
    it = ws.iter_rows(values_only=True)
    hdr = next(it)
    ix = {h: i for i, h in enumerate(hdr)}
    rows = []
    rn = 1
    for r in it:
        rn += 1
        if r[ix["company"]] is None and r[ix["value"]] is None:
            continue
        rows.append({
            "ExcelRow": rn,
            "Company": norm(r[ix["company"]]),
            "Certificate": norm(r[ix["certificateNumber"]]),
            "Type": norm(r[ix["type"]]) or "ASIN",
            "Value": norm(r[ix["value"]]),
            "ProductName": norm(r[ix["productName"]]),
        })
    return rows


def read_db(loaded, certs):
    db = {}
    for r in csv.DictReader(open(loaded, encoding="utf-8-sig")):
        db[(norm(r["Certificate #"]), norm(r["Type"]), norm(r["Value"]))] = {
            "CertificateId": norm(r["CertificateId"]),
            "ProductName": norm(r["ProductName"]),
        }
    co = {}
    for r in csv.DictReader(open(certs, encoding="utf-8-sig")):
        co.setdefault(norm(r["Certificate #"]), norm(r["Company"]))
    return db, co


def autosize(ws, widths):
    for i, w in enumerate(widths, start=1):
        ws.column_dimensions[get_column_letter(i)].width = w
    ws.freeze_panes = "A2"
    for c in ws[1]:
        c.fill = HDR_FILL
        c.font = HDR_FONT
        c.alignment = Alignment(vertical="center", wrap_text=True)


CERT_SHEETS = ["Active List 7.31.26", "Expired List 1 - 4.9.26", "Expired List 2 - 8.11.26"]


def add_certificate_sheets(wb, workbook_path, db_certs_csv, quarantine_csv, autosize_fn):
    """Row-by-row mapping for the CERTIFICATE import sheets.

    Separate from the ASIN mapping because they are different source files with
    different keys, but written into the same workbook so one attachment answers
    "how does each import sheet map to the database".

    Only the three sheets the load actually consumed are mapped. The workbook also
    holds 'Full List', 'Hold List' and planning tabs, which were never inputs --
    mapping those would invent a discrepancy that does not exist.
    """
    why = {}
    if quarantine_csv:
        for r in csv.DictReader(open(quarantine_csv, encoding="utf-8-sig")):
            why[(norm(r["Sheet"]), norm(r["ExcelRow"]))] = norm(r["MissingFields"])

    db = collections.Counter()
    for r in csv.DictReader(open(db_certs_csv, encoding="utf-8-sig")):
        db[norm(r["Certificate #"])] += 1

    wbk = openpyxl.load_workbook(workbook_path, read_only=True, data_only=True)
    rows = []
    for sname in CERT_SHEETS:
        if sname not in wbk.sheetnames:
            continue
        ws = wbk[sname]
        it = ws.iter_rows(values_only=True)
        hdr = next(it)
        ix = {norm(h): i for i, h in enumerate(hdr) if h}
        rn = 1
        for r in it:
            rn += 1
            num = norm(r[ix["CERTIFICATE_NUMBER"]]) if "CERTIFICATE_NUMBER" in ix else ""
            if not num:
                continue
            missing = why.get((sname, str(rn)), "")
            in_db = db.get(num, 0) > 0
            if in_db:
                disp, note = "LOADED", ""
            elif missing:
                disp = "NOT LOADED"
                note = (f"quarantined by the transform: required field(s) empty in the "
                        f"source -- {missing}")
            else:
                disp = "NOT LOADED"
                note = "not in the database and not in the transform's quarantine list -- INVESTIGATE"
            rows.append({
                "Sheet": sname,
                "Excel Row": rn,
                "Certificate": num,
                "Company": norm(r[ix["COMPANY"]]) if "COMPANY" in ix else "",
                "Product": norm(r[ix["PRODUCT"]]) if "PRODUCT" in ix else "",
                "Certificate Type": norm(r[ix["CERTIFICATE_TYPE"]]) if "CERTIFICATE_TYPE" in ix else "",
                "Status": norm(r[ix["STATUS"]]) if "STATUS" in ix else "",
                "Disposition": disp,
                "Rows In Database": db.get(num, 0),
                "Note": note,
            })

    cols = list(rows[0].keys())
    widths = [24, 10, 13, 32, 40, 30, 22, 13, 15, 70]

    for name, subset in (("Certificates", rows),
                         ("Certificates Not Loaded",
                          [r for r in rows if r["Disposition"] == "NOT LOADED"])):
        s = wb.create_sheet(name)
        s.append(cols)
        for r in subset:
            s.append([r[c] for c in cols])
        autosize_fn(s, widths)
        di = cols.index("Disposition")
        for i, r in enumerate(subset, start=2):
            f = FILLS.get(r["Disposition"])
            if f:
                s.cell(row=i, column=di + 1).fill = f
        s.auto_filter.ref = f"A1:{get_column_letter(len(cols))}{len(subset) + 1}"

    loaded = sum(1 for r in rows if r["Disposition"] == "LOADED")
    notloaded = [r for r in rows if r["Disposition"] == "NOT LOADED"]
    unexplained = [r for r in notloaded if "INVESTIGATE" in r["Note"]]
    return {
        "source_rows": len(rows),
        "loaded": loaded,
        "not_loaded": len(notloaded),
        "unexplained": len(unexplained),
        "db_rows": sum(db.values()),
        "publishable_not_loaded": [r for r in notloaded
                                   if r["Status"].lower() not in ("expired", "")],
    }


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--master", required=True)
    ap.add_argument("--loaded", required=True)
    ap.add_argument("--certs", required=True)
    ap.add_argument("--certs-workbook",
                    help="GCC Database Migration_Certs_8.12.26V2.xlsx. When given, the "
                         "certificate sheets are mapped row-by-row too, so one workbook "
                         "covers BOTH import sheets.")
    ap.add_argument("--quarantine",
                    help="quarantine-incomplete-rows.csv from the transform run, used to "
                         "state WHY a certificate row did not load rather than only that it "
                         "did not.")
    ap.add_argument("--out", required=True)
    a = ap.parse_args()

    src = read_master(a.master)
    db, db_co = read_db(a.loaded, a.certs)

    by_key = collections.defaultdict(list)
    for r in src:
        by_key[(r["Certificate"], r["Type"], r["Value"])].append(r)

    out = []
    counts = collections.Counter()
    fallback = 0

    for key, group in by_key.items():
        group.sort(key=lambda r: r["ExcelRow"])
        hit = db.get(key)
        invalid = key[1] == "ASIN" and not ASIN_RE.match(key[2])

        winner = None
        by_name = False
        if hit:
            for r in group:
                if nws(r["ProductName"]) == nws(hit["ProductName"]):
                    winner, by_name = r, True
                    break
        if winner is None:
            winner = group[0]
            if hit:
                fallback += 1

        for r in group:
            if invalid:
                disp, note = "NOT LOADED", f"value {key[2]!r} is not a valid ASIN (must be 10 characters, A-Z/0-9)"
            elif r is winner:
                disp = "LOADED"
                note = ("this row's product name is the one stored in the database"
                        if by_name else
                        "first row for this ASIN on this certificate; the other rows' names are identical")
            else:
                disp = "DUPLICATE"
                note = (f"same ASIN already listed on this certificate at row {winner['ExcelRow']}; "
                        f"the database stores one entry per ASIN per certificate")
            counts[disp] += 1
            out.append({
                "Excel Row": r["ExcelRow"],
                "Company": r["Company"],
                "Certificate": r["Certificate"],
                "Type": r["Type"],
                "ASIN": r["Value"],
                "Product Name (spreadsheet)": r["ProductName"],
                "Disposition": disp,
                "In Database": "YES" if (hit and not invalid) else "NO",
                "Stored At Excel Row": winner["ExcelRow"] if (hit and not invalid) else "",
                "Product Name (database)": hit["ProductName"] if (hit and not invalid) else "",
                "DB Certificate Id": hit["CertificateId"] if (hit and not invalid) else "",
                "Company (database)": db_co.get(r["Certificate"], ""),
                "Note": note,
            })

    out.sort(key=lambda r: r["Excel Row"])

    loaded = counts["LOADED"]
    dup = counts["DUPLICATE"]
    notl = counts["NOT LOADED"]
    total = len(out)
    distinct_values = len({r["Value"] for r in src})

    wb = openpyxl.Workbook()

    ws = wb.active
    ws.title = "Summary"
    summary = [
        ("Question", "The spreadsheet has 21,034 rows; the database holds ~16,234. Why?"),
        ("", ""),
        ("Spreadsheet rows", total),
        ("  minus rows repeating an ASIN already on the same certificate", -dup),
        ("  minus rows whose value is not a valid ASIN", -notl),
        ("Rows loaded into the database", loaded),
        ("", ""),
        ("Rows in CertificateProductIds (measured)", len(db)),
        ("Match?", "YES" if loaded == len(db) else "NO"),
        ("", ""),
        ("Distinct ASIN values in the sheet (any certificate)", distinct_values),
        ("Distinct (certificate, ASIN) pairs in the sheet", len(by_key)),
        ("", ""),
        ("Why", "The database allows one entry per ASIN per certificate (unique index)."),
        ("", "The spreadsheet lists the same ASIN on the same certificate several"),
        ("", "times, once per marketing name. 21,034 counts NAME rows, not ASINs."),
        ("", ""),
        ("Where to look", "'All Rows' lists every one of the 21,034 rows and what happened to it."),
        ("", "'Duplicates' and 'Not Loaded' are the two non-loaded subsets on their own."),
    ]
    for r in summary:
        ws.append(list(r))
    ws.column_dimensions["A"].width = 58
    ws.column_dimensions["B"].width = 78
    for row in ws.iter_rows():
        for c in row:
            c.alignment = Alignment(vertical="center", wrap_text=True)
    ws["A1"].font = Font(bold=True, size=12)

    cols = list(out[0].keys())
    widths = [10, 30, 13, 8, 14, 46, 13, 12, 16, 46, 15, 26, 62]

    def sheet(name, rows):
        s = wb.create_sheet(name)
        s.append(cols)
        for r in rows:
            s.append([r[c] for c in cols])
        autosize(s, widths)
        di = cols.index("Disposition")
        for i, r in enumerate(rows, start=2):
            f = FILLS.get(r["Disposition"])
            if f:
                s.cell(row=i, column=di + 1).fill = f
        s.auto_filter.ref = f"A1:{get_column_letter(len(cols))}{len(rows) + 1}"
        return s

    sheet("All Rows", out)
    sheet("Duplicates", [r for r in out if r["Disposition"] == "DUPLICATE"])
    sheet("Not Loaded", [r for r in out if r["Disposition"] == "NOT LOADED"])

    cert_stats = None
    if a.certs_workbook:
        cert_stats = add_certificate_sheets(wb, a.certs_workbook, a.certs, a.quarantine, autosize)

    wb.save(a.out)

    print(f"spreadsheet rows      : {total}")
    print(f"  LOADED              : {loaded}")
    print(f"  DUPLICATE           : {dup}")
    print(f"  NOT LOADED          : {notl}")
    print(f"  ---------------------------")
    print(f"  accounted for       : {loaded + dup + notl}")
    print()
    print(f"rows in database      : {len(db)}")
    print(f"distinct ASIN values  : {distinct_values}")
    print(f"winner chosen by name : {len(by_key) - fallback} of {len(by_key)} keys "
          f"({fallback} fell back to first-row order, names indistinguishable)")
    print()
    print(f"wrote {a.out}")

    cert_ok = True
    if cert_stats:
        c = cert_stats
        print()
        print("CERTIFICATE SHEETS (the other import file)")
        print(f"  source rows         : {c['source_rows']}")
        print(f"  loaded              : {c['loaded']}")
        print(f"  NOT loaded          : {c['not_loaded']}")
        print(f"  of those unexplained: {c['unexplained']}")
        print(f"  rows in database    : {c['db_rows']}")
        if c["publishable_not_loaded"]:
            print()
            print(f"  *** {len(c['publishable_not_loaded'])} NOT-LOADED certificate(s) are NOT expired "
                  f"-- these are the ones that matter:")
            for r in c["publishable_not_loaded"]:
                print(f"      {r['Certificate']}  {r['Status']}  {r['Company'][:34]}")
                print(f"        {r['Note']}")
        # An unexplained absence is a genuine failure; a quarantined one is a
        # source-data gap the transform recorded on purpose. Only the first fails.
        cert_ok = c["unexplained"] == 0

    ok = (loaded + dup + notl == total) and (loaded == len(db)) and cert_ok
    if not ok:
        print("\nFAIL: rows do not account for exactly, loaded != database count, "
              "or a certificate is absent with no recorded reason.")
        return 1
    print("\nPASS: every one of the "
          f"{total} spreadsheet rows is accounted for, and loaded == database rows.")
    if cert_stats:
        print("      Every certificate row is either loaded or quarantined with a stated reason.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
