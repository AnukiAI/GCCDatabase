#!/usr/bin/env python3
"""
Add the partner-coverage tabs to the 2026-08-18 verification workbook.
======================================================================

WHY THIS EXISTS RATHER THAN A REBUILD
-------------------------------------
2026-08-18-build-verification-workbook.py now emits these tabs, so a full rebuild would include
them -- but a rebuild also re-reads the database and every other tab would move. 108 certificates
have expired since 18 August, so a rebuilt "Status Breakdown" or "Certificates" tab would no longer
match the numbers the client already has in writing. This script therefore takes the CLEAN 8/18
workbook and adds tabs to it, leaving every existing tab byte-for-byte as the client received it.

WHICH SOURCE COPY
-----------------
Two copies of the 8/18 workbook exist and they are NOT identical:

  docs/tracking/2026-08-18-...xlsx          520 KB, 23:23 -- clean generator output
  ../DataMigrationTestDev/2026-08-18-...xlsx 432 KB, 23:57 -- the same 15 tabs PLUS six hand-typed
                                             rows on the ASINs tab, one of which reads
                                             "DRAFT - still validating ASINS"

The DataMigrationTestDev copy is newer but is a hand-annotated working copy re-saved through Excel,
not a later build. We build from the repo copy so that stale DRAFT note cannot reach a client, and
write the result to BOTH locations so whichever copy was sent, the current file sits beside it.

The dated 8/18 files are never overwritten -- they are the record of what was actually sent.

THE FOUR TABS
-------------
  No Partner              the 4 certificates with product identifiers and no partner
  Partner Coverage        every certificate by partner x identifiers x status (the 2x2, itemised)
  Active - No Partner     all 683 active certificates with no partner, not only those with ASINs
  Amazon - No Identifiers the 54 active Amazon certificates carrying zero identifiers

"No Partner" answers a client question directly. The other three exist because that tab's predicate
is "has identifiers AND has no partner", so it can only ever surface certificates that carry
identifiers -- it reports 4 where the partner-coverage population is 683. Shipping the subset
without the population invites the reader to mistake one for the other.

Usage:  python 2026-09-01-add-partner-tabs-to-workbook.py
"""

import csv
import io
import os

import openpyxl
from openpyxl.styles import Alignment, Font, PatternFill
from openpyxl.utils import get_column_letter

HERE = os.path.dirname(os.path.abspath(__file__))
PROJ = os.path.abspath(os.path.join(HERE, "..", ".."))
SOLN = os.path.abspath(os.path.join(PROJ, "..", ".."))
RECON = os.path.join(HERE, "_reconciliation")

SRC = os.path.join(PROJ, "docs", "tracking", "2026-08-18-migration-verification-workbook.xlsx")
OUTS = [
    os.path.join(PROJ, "docs", "tracking", "2026-09-01-migration-verification-workbook.xlsx"),
    os.path.join(SOLN, "DataMigrationTestDev", "2026-09-01-migration-verification-workbook.xlsx"),
]

WARN_FILL = PatternFill("solid", fgColor="FFF6ECDD")
BAD_FILL = PatternFill("solid", fgColor="FFF7E5E4")

CERT_HEADERS = ["Certificate", "Company", "Status", "Effective", "Expiration",
                "Product identifiers", "Partner on certificate", "View on site"]
CERT_WIDTHS = [14, 34, 24, 12, 12, 18, 20, 62]


def read_csv(name):
    path = os.path.join(RECON, name)
    with io.open(path, encoding="utf-8-sig") as fh:
        return list(csv.DictReader(fh))


def cert_rows(rows):
    return [[r["CertificateNumber"], r["Company"], r["Status"], r["EffectiveDate"],
             r["ExpirationDate"], int(r["ProductIdentifiers"] or 0),
             r["PartnerOnCertificate"], r["ViewOnSite"]] for r in rows]


def sheet(wb, title, headers, rows, widths, head_fill, head_font, after=None):
    """Create a tab styled like the rest of the workbook, optionally positioned after `after`."""
    ws = wb.create_sheet(title)
    ws.append(headers)
    for c in range(1, len(headers) + 1):
        cell = ws.cell(row=1, column=c)
        cell.fill, cell.font = head_fill, head_font
        cell.alignment = Alignment(vertical="center", wrap_text=True)
    ws.row_dimensions[1].height = 26
    for r in rows:
        ws.append(r)
    for i, w in enumerate(widths, start=1):
        ws.column_dimensions[get_column_letter(i)].width = w
    ws.freeze_panes = "A2"
    if rows:
        ws.auto_filter.ref = "A1:%s%d" % (get_column_letter(len(headers)), len(rows) + 1)
    if after:
        names = wb.sheetnames
        wb.move_sheet(title, offset=names.index(after) + 1 - names.index(title))
    return ws


def main():
    nopartner = read_csv("certificates-no-partner.csv")
    covmatrix = read_csv("partner-coverage-matrix.csv")
    nopart_all = read_csv("active-certs-no-partner.csv")
    amzn_noids = read_csv("active-amazon-certs-no-identifiers.csv")
    print("rows: no-partner=%d  matrix=%d  active-no-partner=%d  amazon-no-ids=%d"
          % (len(nopartner), len(covmatrix), len(nopart_all), len(amzn_noids)))

    wb = openpyxl.load_workbook(SRC)

    # Match the header style this workbook actually uses rather than assuming it.
    probe = wb["ASINs"].cell(row=1, column=1)
    head_fill = PatternFill("solid", fgColor=probe.fill.fgColor.rgb)
    head_font = Font(color=probe.font.color.rgb if probe.font.color else "FFFFFFFF",
                     bold=probe.font.bold, size=probe.font.size)

    # ---- 1. the four, with the "why" column
    ws = sheet(wb, "No Partner",
               CERT_HEADERS[:7] + ["Reaches Amazon today", "Why", "View on site"],
               [[r["CertificateNumber"], r["Company"], r["Status"], r["EffectiveDate"],
                 r["ExpirationDate"], int(r["ProductIdentifiers"]), r["PartnerOnCertificate"],
                 r["ReachesAmazonToday"], r["Why"], r["ViewOnSite"]] for r in nopartner],
               [14, 30, 24, 12, 12, 18, 20, 20, 74, 62], head_fill, head_font, after="ASINs")
    for i in range(2, len(nopartner) + 2):
        ws.cell(row=i, column=7).fill = WARN_FILL
        ws.cell(row=i, column=8).fill = BAD_FILL
        ws.cell(row=i, column=9).alignment = Alignment(wrap_text=True, vertical="top")

    # ---- 2. the whole population in one grid
    ws = sheet(wb, "Partner Coverage",
               ["Partner on certificate", "Product identifiers", "Certificate status", "Certificates"],
               [[r["Partner on certificate"], r["Product identifiers"],
                 r["Certificate status"], int(r["Certificates"])] for r in covmatrix],
               [24, 20, 20, 14], head_fill, head_font, after="No Partner")
    for i in range(2, len(covmatrix) + 2):
        if ws.cell(row=i, column=1).value == "No partner" and ws.cell(row=i, column=3).value == "Active":
            for c in range(1, 5):
                ws.cell(row=i, column=c).fill = WARN_FILL

    # ---- 3 & 4. the two lists the "4" does not cover
    sheet(wb, "Active - No Partner", CERT_HEADERS, cert_rows(nopart_all), CERT_WIDTHS,
          head_fill, head_font, after="Partner Coverage")
    sheet(wb, "Amazon - No Identifiers", CERT_HEADERS, cert_rows(amzn_noids), CERT_WIDTHS,
          head_fill, head_font, after="Active - No Partner")

    # ---- ASINs note points at the tab
    for row in wb["ASINs"].iter_rows(min_row=1, max_row=wb["ASINs"].max_row):
        if row[0].value and "Untagged" in str(row[0].value):
            row[2].value = ("OPEN - on 4 certificates that carry no partner, so NO partner receives "
                            "them. The 4 are listed on the 'No Partner' tab; 'Partner Coverage' puts "
                            "them in context.")

    # ---- Read Me: index rows + provenance
    rm = wb["Read Me"]
    target = None
    for row in rm.iter_rows(min_row=1, max_row=rm.max_row):
        if row[0].value == "Documents" and target is None:
            target = row[0].row
        if row[1].value and str(row[1].value).startswith("Test environment - built 2026-08-18"):
            row[1].value = "Test environment - built 2026-08-18, partner tabs added 2026-09-01"
    index = [
        ("No Partner", "The 4 certificates that carry product identifiers but no partner."),
        ("Partner Coverage", "Every certificate by partner, identifiers and status - the whole picture in one grid."),
        ("Active - No Partner", "All 683 active certificates that carry no partner, not only the ones with identifiers."),
        ("Amazon - No Identifiers", "The 54 active Amazon certificates that carry no product identifiers."),
    ]
    for offset, (name, desc) in enumerate(index):
        rm.insert_rows(target + offset)
        rm.cell(row=target + offset, column=1).value = name
        rm.cell(row=target + offset, column=2).value = desc

    for out in OUTS:
        wb.save(out)
        print("WROTE %s" % out)
    wb.close()


if __name__ == "__main__":
    main()
