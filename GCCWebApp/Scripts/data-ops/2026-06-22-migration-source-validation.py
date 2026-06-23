#!/usr/bin/env python3
"""
Migration source-data validation gate  (DEV / TEST ONLY)
=========================================================

Profiles the customer's source spreadsheets BEFORE any import into the GCC
database and flags every row that will block or distort the migration.

It is read-only: it never connects to a database and never modifies the source
workbooks. Run it as a pre-migration gate -- it exits non-zero when BLOCKER
issues are present so it can fail a pipeline.

Source files (defaults point at the sample set; override with CLI args):
  1. Cert listing .xlsx  -- sheets "Active List ..." (NEW schema) and
                            "Expired List ..." (OLD schema)
  2. ASIN .xlsm          -- "GreenCircle Upload Template" -> CertificateProductId

Findings are categorised BLOCKER / WARNING / INFO and each flagged set is
written to CSV under ./_validation_results/<timestamp>/ for data cleanup.

Checks map to the target model constraints:
  - CertificateProductId.ProductName  MaxLength(200)
  - CertificateProductId.ProductIdValue MaxLength(50)
  - Certificate status enum (8 states, Models/CertificateStatus.cs)
  - Product category/type/subcategory + certification-type controlled vocab
    (Field Options sheet)

Usage:
  python 2026-06-22-migration-source-validation.py
  python 2026-06-22-migration-source-validation.py <cert.xlsx> <asin.xlsm>
  python 2026-06-22-migration-source-validation.py --no-csv

Requires: openpyxl  (pip install openpyxl)
"""

from __future__ import annotations

import csv
import datetime
import os
import re
import sys
import warnings
from collections import Counter, defaultdict

warnings.filterwarnings("ignore")

try:
    import openpyxl
except ImportError:
    sys.exit("ERROR: openpyxl is required.  Run:  pip install openpyxl")

# --------------------------------------------------------------------------- #
# Configuration
# --------------------------------------------------------------------------- #

DEFAULT_DIR = (
    r"C:\Users\swest\OneDrive - PointSolve Technology Inc\LocalWest"
    r"\Projects\GCC Database Solution\TestData\DBSampleLists"
)
DEFAULT_CERT_FILE = "GCC Database SAMPLE Cert Listing for Anuki Tesing_6.2.26.xlsx"
DEFAULT_ASIN_FILE = "SAMPLE ASIN List for Anuki Testing_6.1.26.xlsm"

# Model constraints (keep in sync with Models/CertificateProductId.cs)
MAX_PRODUCT_NAME = 200
MAX_PRODUCT_ID_VALUE = 50

# Certificate status enum -- Models/CertificateStatus.cs (8 states)
VALID_CERT_STATUSES = {
    "Draft",
    "Client Approved - Not Paid",
    "Client Approved - Confidential",
    "Client Approved - Paid",
    "Hold",
    "Expired",
    "Republished",
    "Expired - No Annual Verification",
}

# Controlled vocabulary -- source: "Field Options" sheet (FOR DROPDOWN section).
# Update these if the client's dropdown lists change.
VALID_CERTIFICATION_TYPES = {
    "Carbon Footprint Reduction",
    "Certified Energy Savings",
    "Certified Environmental Facts",
    "Certified Sustainability Facts",
    "Certified Water Savings",
    "Closed Loop Product",
    "Dematerialization",
    "GreenCircle Audited",
    "Greenhouse Gas (GHG) Verification",
    "Life Cycle Assessment (LCA) Optimized",
    "Material Ingredient Reporting",
    "Recyclable Material",
    "Recycled Content",
    "Sustainable Energy Practices",
    "Sustainable Manufacturing Practices",
    "Waste Diversion from Landfill",
}

# Canonical Level-1 category labels and their fixed numeric prefix.
CANONICAL_CATEGORIES = {
    "Chemicals and Raw Materials": "1",
    "Clothing, Apparel, and Fabrics": "2",
    "Construction / Building Products": "3",
    "Consumer Products": "4",
    "Electronics, Computers, and Accessories": "5",
    "Food and Beverage": "6",
    "Healthcare and Pharmaceutical": "7",
    "Packaging": "8",
}

# A real ASIN is "B0" + 8 alphanumerics. Used only to surface obvious junk.
ASIN_PATTERN = re.compile(r"B0[A-Z0-9]{8}$")
CERT_NUMBER_PATTERN = re.compile(r"\d{2}-\d{3,5}$")

# --------------------------------------------------------------------------- #
# Findings collector
# --------------------------------------------------------------------------- #

BLOCKER, WARNING, INFO = "BLOCKER", "WARNING", "INFO"


class Report:
    def __init__(self):
        self.findings = []  # (severity, code, message, rows, fieldnames)

    def add(self, severity, code, message, rows=None, fieldnames=None):
        self.findings.append((severity, code, message, rows or [], fieldnames))

    def counts(self):
        c = Counter(sev for sev, *_ in self.findings)
        return c[BLOCKER], c[WARNING], c[INFO]


# --------------------------------------------------------------------------- #
# Workbook helpers
# --------------------------------------------------------------------------- #


def clean(v):
    return "" if v is None else str(v).strip()


def is_real_date(v):
    return isinstance(v, datetime.datetime)


def looks_like_excel_serial(v):
    """A bare integer in the plausible Excel-date range stored as text/number."""
    s = clean(v)
    return s.isdigit() and 30000 <= int(s) <= 60000


def load_sheet(path, sheet_match):
    """Return (sheet_title, header_list, data_rows). sheet_match is a predicate
    or substring used to pick the worksheet."""
    wb = openpyxl.load_workbook(path, read_only=True, data_only=True)
    title = None
    for ws in wb.worksheets:
        if (sheet_match(ws.title) if callable(sheet_match) else sheet_match in ws.title):
            title = ws.title
            break
    if title is None:
        wb.close()
        raise KeyError(f"No sheet matching {sheet_match!r} in {os.path.basename(path)}")
    ws = wb[title]
    it = ws.iter_rows(values_only=True)
    header = list(next(it))
    rows = [r for r in it if any(c is not None and str(c).strip() for c in r)]
    wb.close()
    return title, header, rows


def col(rows, idx):
    return [r[idx] if idx < len(r) else None for r in rows]


# --------------------------------------------------------------------------- #
# Validators
# --------------------------------------------------------------------------- #


def validate_active(rows, rpt):
    """Active List -- NEW schema. Column indices per the sample header."""
    C_CERT, C_COMPANY, C_PRODUCT = 0, 1, 2
    C_CAT, C_TYPE, C_SUBCAT = 3, 4, 5
    C_CTYPE, C_EFF, C_EXP = 6, 7, 8
    C_COUNTRY, C_STATUS, C_PARTNER = 10, 11, 12

    n = len(rows)
    rpt.add(INFO, "ACT-COUNT", f"Active List: {n} rows (NEW schema -> Certificate)")

    # cert# uniqueness / format
    cn = [clean(x) for x in col(rows, C_CERT)]
    dupes = {k for k, v in Counter(x for x in cn if x).items() if v > 1}
    blanks = [{"row": i + 2} for i, x in enumerate(cn) if not x]
    badfmt = [{"row": i + 2, "cert#": x}
              for i, x in enumerate(cn) if x and not CERT_NUMBER_PATTERN.match(x)]
    if dupes:
        rpt.add(BLOCKER, "ACT-CERT-DUP",
                f"{len(dupes)} duplicate certificate numbers in Active List",
                [{"cert#": d} for d in sorted(dupes)], ["cert#"])
    if blanks:
        rpt.add(BLOCKER, "ACT-CERT-BLANK",
                f"{len(blanks)} Active rows with blank certificate number",
                blanks, ["row"])
    if badfmt:
        rpt.add(WARNING, "ACT-CERT-FMT",
                f"{len(badfmt)} Active cert numbers not in NN-NNNN format",
                badfmt, ["row", "cert#"])

    # status mapping
    bad_status = [{"row": i + 2, "cert#": cn[i], "status": s}
                  for i, s in enumerate(clean(x) for x in col(rows, C_STATUS))
                  if s and s not in VALID_CERT_STATUSES]
    if bad_status:
        rpt.add(BLOCKER, "ACT-STATUS",
                f"{len(bad_status)} Active rows with status not in the 8-state enum",
                bad_status, ["row", "cert#", "status"])

    # certification type vocab
    bad_ctype = [{"row": i + 2, "cert#": cn[i], "type": t}
                 for i, t in enumerate(clean(x) for x in col(rows, C_CTYPE))
                 if t and t not in VALID_CERTIFICATION_TYPES]
    if bad_ctype:
        rpt.add(BLOCKER, "ACT-CTYPE",
                f"{len(bad_ctype)} Active rows with certification type outside the controlled vocab",
                bad_ctype, ["row", "cert#", "type"])

    # product category integrity (number prefix must match canonical for that label)
    cat_issues = []
    for i, raw in enumerate(clean(x) for x in col(rows, C_CAT)):
        if not raw:
            cat_issues.append({"row": i + 2, "cert#": cn[i], "category": "", "problem": "blank"})
            continue
        if raw.upper() == "N/A":
            cat_issues.append({"row": i + 2, "cert#": cn[i], "category": raw, "problem": "N/A"})
            continue
        m = re.match(r"^(\d+)\s+(.*)$", raw)
        if not m:
            cat_issues.append({"row": i + 2, "cert#": cn[i], "category": raw, "problem": "no-number-prefix"})
            continue
        num, label = m.group(1), m.group(2).strip()
        expected = CANONICAL_CATEGORIES.get(label)
        if expected is None:
            cat_issues.append({"row": i + 2, "cert#": cn[i], "category": raw, "problem": "unknown-label"})
        elif expected != num:
            cat_issues.append({"row": i + 2, "cert#": cn[i], "category": raw,
                               "problem": f"prefix {num} should be {expected}"})
    if cat_issues:
        # Mismatched prefixes are a BLOCKER (ambiguous mapping); blank/N/A are WARNING.
        hard = [c for c in cat_issues if c["problem"].startswith("prefix") or c["problem"] in ("unknown-label", "no-number-prefix")]
        soft = [c for c in cat_issues if c not in hard]
        if hard:
            rpt.add(BLOCKER, "ACT-CAT",
                    f"{len(hard)} Active rows with inconsistent/unknown product-category code",
                    hard, ["row", "cert#", "category", "problem"])
        if soft:
            rpt.add(WARNING, "ACT-CAT-MISSING",
                    f"{len(soft)} Active rows with blank or N/A product category",
                    soft, ["row", "cert#", "category", "problem"])

    # dates
    date_issues = []
    for i, r in enumerate(rows):
        eff, exp = r[C_EFF], r[C_EXP]
        if clean(eff) and not is_real_date(eff):
            date_issues.append({"row": i + 2, "cert#": cn[i], "field": "EffectiveDate",
                                "value": clean(eff), "problem": "serial/text" if looks_like_excel_serial(eff) else "not-a-date"})
        if clean(exp) and not is_real_date(exp):
            date_issues.append({"row": i + 2, "cert#": cn[i], "field": "ExpirationDate",
                                "value": clean(exp), "problem": "serial/text" if looks_like_excel_serial(exp) else "not-a-date"})
        if is_real_date(eff) and is_real_date(exp) and exp < eff:
            date_issues.append({"row": i + 2, "cert#": cn[i], "field": "range",
                                "value": f"{eff.date()}..{exp.date()}", "problem": "expiration<effective"})
    if date_issues:
        rpt.add(WARNING, "ACT-DATES",
                f"{len(date_issues)} Active date anomalies (unparsed serials / bad ranges)",
                date_issues, ["row", "cert#", "field", "value", "problem"])

    # informational: Amazon partner coverage
    amazon = sum(1 for x in col(rows, C_PARTNER) if clean(x) == "Amazon")
    rpt.add(INFO, "ACT-AMAZON", f"{amazon} Active certs flagged Partner=Amazon")


def validate_expired(rows, rpt):
    """Expired List -- OLD schema (single PRODUCT_TYPE, combined LOCATION)."""
    C_CERT, C_COMPANY, C_PRODUCT = 0, 1, 2
    C_PTYPE, C_CTYPE, C_EFF, C_EXP, C_LOC, C_STATUS = 3, 4, 5, 6, 7, 8

    n = len(rows)
    rpt.add(INFO, "EXP-COUNT",
            f"Expired List: {n} rows (OLD schema -> Certificate; needs schema transform)")
    rpt.add(INFO, "EXP-SCHEMA",
            "Expired List uses OLD schema: LOCATION must be split into Location+Country, "
            "and single PRODUCT_TYPE mapped into the 3-level hierarchy (detail not present in source).")

    cn = [clean(x) for x in col(rows, C_CERT)]
    dupes = {k: v for k, v in Counter(x for x in cn if x).items() if v > 1}
    if dupes:
        rpt.add(BLOCKER, "EXP-CERT-DUP",
                f"{len(dupes)} duplicate certificate numbers within Expired List "
                f"({sum(dupes.values())} rows) -- new schema requires uniqueness",
                [{"cert#": k, "count": v} for k, v in sorted(dupes.items())], ["cert#", "count"])
    blanks = [{"row": i + 2} for i, x in enumerate(cn) if not x]
    if blanks:
        rpt.add(BLOCKER, "EXP-CERT-BLANK",
                f"{len(blanks)} Expired rows with blank certificate number", blanks, ["row"])
    badfmt = [{"row": i + 2, "cert#": x}
              for i, x in enumerate(cn) if x and not CERT_NUMBER_PATTERN.match(x)]
    if badfmt:
        rpt.add(WARNING, "EXP-CERT-FMT",
                f"{len(badfmt)} Expired cert numbers not in NN-NNNN format", badfmt, ["row", "cert#"])

    # retired certification types not in new vocab
    bad_ctype = [{"row": i + 2, "cert#": cn[i], "type": t}
                 for i, t in enumerate(clean(x) for x in col(rows, C_CTYPE))
                 if t and t not in VALID_CERTIFICATION_TYPES]
    if bad_ctype:
        retired = sorted({c["type"] for c in bad_ctype})
        rpt.add(BLOCKER, "EXP-CTYPE",
                f"{len(bad_ctype)} Expired rows use retired certification types not in new vocab: "
                f"{', '.join(retired)}",
                bad_ctype, ["row", "cert#", "type"])

    # dates (serials / blanks / bad ranges)
    date_issues = []
    for i, r in enumerate(rows):
        eff, exp = r[C_EFF], r[C_EXP]
        for field, val in (("EffectiveDate", eff), ("ExpirationDate", exp)):
            s = clean(val)
            if s and not is_real_date(val):
                date_issues.append({"row": i + 2, "cert#": cn[i], "field": field, "value": s,
                                    "problem": "serial/text" if looks_like_excel_serial(val) else "not-a-date"})
        if is_real_date(eff) and is_real_date(exp) and exp < eff:
            date_issues.append({"row": i + 2, "cert#": cn[i], "field": "range",
                                "value": f"{eff.date()}..{exp.date()}", "problem": "expiration<effective"})
    if date_issues:
        rpt.add(WARNING, "EXP-DATES",
                f"{len(date_issues)} Expired date anomalies (unparsed Excel serials / blanks / bad ranges)",
                date_issues, ["row", "cert#", "field", "value", "problem"])

    return set(x for x in cn if x)


def validate_asin(rows, rpt, known_cert_numbers):
    """ASIN template -- collapses into CertificateProductId children."""
    C_COMPANY, C_TYPE, C_VALUE, C_CERTNAME, C_PNAME = 0, 1, 2, 3, 4
    C_AMZCODE, C_CTYPE, C_CERT, C_ISSUE, C_EXP = 5, 6, 7, 8, 9
    C_RESULTS, C_PATHWAY = 10, 11

    n = len(rows)
    rpt.add(INFO, "ASIN-COUNT", f"ASIN template: {n} rows (-> CertificateProductId)")

    cn = [clean(x) for x in col(rows, C_CERT)]
    val = [clean(x) for x in col(rows, C_VALUE)]
    pname = [clean(x) for x in col(rows, C_PNAME)]

    # productName length > model cap (BLOCKER -- will be truncated/rejected)
    long_names = [{"row": i + 2, "cert#": cn[i], "len": len(pn), "productName": pn[:120] + ("..." if len(pn) > 120 else "")}
                  for i, pn in enumerate(pname) if len(pn) > MAX_PRODUCT_NAME]
    if long_names:
        mx = max(c["len"] for c in long_names)
        rpt.add(BLOCKER, "ASIN-PNAME-LEN",
                f"{len(long_names)} ASIN productName values exceed ProductName MaxLength({MAX_PRODUCT_NAME}); "
                f"longest={mx}",
                long_names, ["row", "cert#", "len", "productName"])

    # productIdValue length
    long_vals = [{"row": i + 2, "cert#": cn[i], "len": len(v), "value": v}
                 for i, v in enumerate(val) if len(v) > MAX_PRODUCT_ID_VALUE]
    if long_vals:
        rpt.add(BLOCKER, "ASIN-VALUE-LEN",
                f"{len(long_vals)} ASIN product-id values exceed ProductIdValue MaxLength({MAX_PRODUCT_ID_VALUE})",
                long_vals, ["row", "cert#", "len", "value"])

    # orphan cert numbers (FK failure)
    orphan_rows = [{"row": i + 2, "cert#": cn[i], "value": val[i]}
                   for i in range(n) if cn[i] and cn[i] not in known_cert_numbers]
    if orphan_rows:
        orphan_certs = sorted({r["cert#"] for r in orphan_rows})
        rpt.add(BLOCKER, "ASIN-ORPHAN",
                f"{len(orphan_rows)} ASIN rows reference {len(orphan_certs)} cert numbers absent from "
                f"both cert lists (FK failure): {', '.join(orphan_certs)}",
                orphan_rows, ["row", "cert#", "value"])

    # junk product-id values
    junk = [{"row": i + 2, "cert#": cn[i], "value": v}
            for i, v in enumerate(val)
            if clean(col(rows, C_TYPE)[i]) == "ASIN" and not ASIN_PATTERN.match(v)]
    if junk:
        rpt.add(WARNING, "ASIN-JUNK",
                f"{len(junk)} ASIN rows have a value that is not a valid ASIN (placeholder/junk)",
                junk, ["row", "cert#", "value"])

    # exact duplicate rows
    seen = {}
    dup_rows = []
    for i, r in enumerate(rows):
        key = tuple(clean(c) for c in r[:16])
        if key in seen:
            dup_rows.append({"row": i + 2, "cert#": cn[i], "value": val[i], "first_seen_row": seen[key]})
        else:
            seen[key] = i + 2
    if dup_rows:
        rpt.add(WARNING, "ASIN-DUP",
                f"{len(dup_rows)} exact-duplicate ASIN rows (~{round(100*len(dup_rows)/n)}% redundancy) -- dedupe before insert",
                dup_rows, ["row", "cert#", "value", "first_seen_row"])

    # per-cert attribute conflicts (which value wins when collapsing?)
    bycert = defaultdict(lambda: defaultdict(set))
    fields = {"company": C_COMPANY, "certType": C_CTYPE, "amazonCode": C_AMZCODE,
              "results": C_RESULTS, "issueDate": C_ISSUE, "expirationDate": C_EXP}
    for r in rows:
        c = clean(r[C_CERT])
        if not c:
            continue
        for label, idx in fields.items():
            v = clean(r[idx])
            if v:
                bycert[c][label].add(v)
    conflicts = []
    for c, d in bycert.items():
        for label, vals in d.items():
            if len(vals) > 1:
                conflicts.append({"cert#": c, "field": label,
                                  "distinct_values": " | ".join(sorted(vals))[:200]})
    if conflicts:
        rpt.add(BLOCKER, "ASIN-CONFLICT",
                f"{len(conflicts)} cert-level attribute conflicts across a cert's ASIN rows "
                f"(non-deterministic collapse -- need a tie-break rule)",
                conflicts, ["cert#", "field", "distinct_values"])


# --------------------------------------------------------------------------- #
# Output
# --------------------------------------------------------------------------- #


def write_csvs(rpt, outdir):
    os.makedirs(outdir, exist_ok=True)
    written = []
    for sev, code, _msg, rows, fieldnames in rpt.findings:
        if not rows or not fieldnames:
            continue
        path = os.path.join(outdir, f"{sev}_{code}.csv")
        with open(path, "w", newline="", encoding="utf-8-sig") as f:
            w = csv.DictWriter(f, fieldnames=fieldnames)
            w.writeheader()
            w.writerows(rows)
        written.append((path, len(rows)))
    return written


def print_report(rpt):
    order = {BLOCKER: 0, WARNING: 1, INFO: 2}
    icon = {BLOCKER: "[X]", WARNING: "[!]", INFO: "[i]"}
    print("\n" + "=" * 78)
    print("  GCC MIGRATION SOURCE-DATA VALIDATION")
    print("=" * 78)
    for sev in (BLOCKER, WARNING, INFO):
        block = [f for f in rpt.findings if f[0] == sev]
        if not block:
            continue
        print(f"\n{icon[sev]} {sev}  ({len(block)})")
        print("-" * 78)
        for _sev, code, msg, rows, _fn in sorted(block, key=lambda f: f[1]):
            tag = f"  {code:18s}"
            print(f"{tag} {msg}")
    b, w, i = rpt.counts()
    print("\n" + "=" * 78)
    print(f"  SUMMARY:  {b} blocker(s)   {w} warning(s)   {i} info")
    print("=" * 78)
    return b


def main():
    argv = [a for a in sys.argv[1:] if a != "--no-csv"]
    write = "--no-csv" not in sys.argv

    if len(argv) >= 2:
        cert_path, asin_path = argv[0], argv[1]
    else:
        cert_path = os.path.join(DEFAULT_DIR, DEFAULT_CERT_FILE)
        asin_path = os.path.join(DEFAULT_DIR, DEFAULT_ASIN_FILE)

    for p in (cert_path, asin_path):
        if not os.path.exists(p):
            sys.exit(f"ERROR: file not found: {p}")

    print(f"Cert file: {cert_path}")
    print(f"ASIN file: {asin_path}")

    rpt = Report()

    _, _, active = load_sheet(cert_path, lambda t: t.strip().lower().startswith("active list"))
    _, _, expired = load_sheet(cert_path, lambda t: t.strip().lower().startswith("expired list"))
    _, _, asin = load_sheet(asin_path, lambda t: "upload template" in t.strip().lower())

    validate_active(active, rpt)
    expired_certs = validate_expired(expired, rpt)

    active_certs = {clean(r[0]) for r in active if clean(r[0])}
    known = active_certs | expired_certs
    validate_asin(asin, rpt, known)

    blockers = print_report(rpt)

    if write:
        stamp = datetime.datetime.now().strftime("%Y%m%d-%H%M%S")
        outdir = os.path.join(os.path.dirname(os.path.abspath(__file__)),
                              "_validation_results", stamp)
        written = write_csvs(rpt, outdir)
        if written:
            print(f"\nFlagged rows written to:\n  {outdir}")
            for path, count in written:
                print(f"  - {os.path.basename(path)}  ({count} rows)")

    # Gate behaviour: non-zero exit when blockers exist.
    sys.exit(1 if blockers else 0)


if __name__ == "__main__":
    main()
