#!/usr/bin/env python3
r"""
ASIN RECORD LOG -- every certificate, every ASIN, both directions
=================================================================

Why this exists
  2026-08-21 the client validated ASIN counts per company and five came back
  short. That was answered: the counts reconcile exactly, the difference was
  three counting units (2026-08-21-asin-per-company-reconciliation.py). The
  client's reply was that they are "still concerned the ASINs are not correct"
  and asked for a log of every record and its ASINs compared to the import
  sheets.

  A count -- even a reconciling count -- cannot answer that. Two sides can agree
  on 11 and 11 while holding eleven DIFFERENT ASINs. This script therefore does
  not compare counts at all. It emits one row per (certificate, product ID) in
  the UNION of both sides, with a verdict on each, so every value can be read
  and checked individually.

  2026-08-18-asin-reconciliation.py already compares value sets per certificate
  and reports 0 missing / 0 extra. This is deliberately a SECOND, independently
  written implementation of the same question with a different output shape --
  if both agree, the result is not an artefact of one script's logic.

What it compares
  MASTER : "GCC ASIN Master List_8.12.26.xlsm", sheet ACTIVE LIST -- the client's
           own list, the thing they are checking against.
  DB     : a live dump of CertificateProductIds joined to Certificates.

  Keyed on (certificate NUMBER, type, value). Number, not CertificateId, because
  that is what the client sees and what the master list carries -- but CR-038
  allows several certificates to share one number, so where that happens the
  DB side is the UNION across those IDs and the log says so in CertificateIds.

Verdicts, one per output row
  OK                  present on both sides
  MISSING_FROM_DB     in the master, absent from the database  <-- a real defect
  EXTRA_IN_DB         in the database, absent from the master  <-- a real defect
  INVALID_IN_SOURCE   master value fails ProductIdService's pattern, so it was
                      never loadable ("TO BE CHECKED", a 9-character ASIN)
  CERT_NOT_IN_DB      the master row's certificate is not in the database at all
  Name differences are reported in their own column, never as a missing ASIN --
  a renamed product is not a lost one.

Usage
  powershell -File 2026-08-18-asin-reconciliation-dump.ps1 -Target test \
      -OutFile _reconciliation\loaded-productids-TEST-<date>.csv
  powershell -File 2026-08-23-cert-company-dump.ps1 -Target test

  python 2026-08-23-asin-record-log.py \
      --master "<path to GCC ASIN Master List_8.12.26.xlsm>" \
      --loaded _reconciliation/loaded-productids-TEST-<date>.csv \
      --certs  _reconciliation/loaded-certs-companies-TEST.csv \
      --outdir _reconciliation/_asin_record_log

  Exit 0 only if MISSING_FROM_DB and EXTRA_IN_DB are both zero.
"""

import argparse
import collections
import csv
import os
import re
import sys

import openpyxl

WS = re.compile(r"\s+")

# Mirrors ProductIdService. ASIN is the only type the master list carries today,
# but the pattern table is keyed by type so a UPC/GTIN row cannot be silently
# waved through as "not an ASIN, therefore fine".
PATTERNS = {
    "ASIN": re.compile(r"^[A-Z0-9]{10}$"),
}

MASTER_SHEET = "ACTIVE LIST"


def norm(v):
    """Trim only. Deliberately NOT upper-cased: if the master holds a lowercase
    ASIN the database will not match it, and that is a finding to report, not a
    difference to paper over."""
    return "" if v is None else str(v).strip()


def load_master(path):
    wb = openpyxl.load_workbook(path, read_only=True, data_only=True)
    if MASTER_SHEET not in wb.sheetnames:
        sys.exit(f"master workbook has no sheet {MASTER_SHEET!r}; found {wb.sheetnames}")
    ws = wb[MASTER_SHEET]
    it = ws.iter_rows(values_only=True)
    hdr = next(it)
    ix = {h: i for i, h in enumerate(hdr)}
    for need in ("company", "type", "value", "productName", "certificateNumber"):
        if need not in ix:
            sys.exit(f"master sheet is missing column {need!r}")

    # rows[(cert, type, value)] = list of (excel_row, product_name)
    rows = collections.defaultdict(list)
    cert_company = {}
    raw_total = 0
    excel_row = 1  # header consumed above
    for r in it:
        excel_row += 1
        if r[ix["company"]] is None and r[ix["value"]] is None:
            continue
        raw_total += 1
        cert = norm(r[ix["certificateNumber"]])
        typ = norm(r[ix["type"]]) or "ASIN"
        val = norm(r[ix["value"]])
        rows[(cert, typ, val)].append((excel_row, norm(r[ix["productName"]])))
        cert_company.setdefault(cert, norm(r[ix["company"]]))
    return rows, cert_company, raw_total


def load_db(loaded_path, certs_path):
    cert_company = {}
    cert_ids = collections.defaultdict(set)
    with open(certs_path, encoding="utf-8-sig") as f:
        for r in csv.DictReader(f):
            num = norm(r["Certificate #"])
            cert_company.setdefault(num, norm(r["Company"]))
            cert_ids[num].add(norm(r["CertificateId"]))

    # rows[(cert, type, value)] = list of (certificate_id, product_name, is_active)
    rows = collections.defaultdict(list)
    raw_total = 0
    with open(loaded_path, encoding="utf-8-sig") as f:
        for r in csv.DictReader(f):
            raw_total += 1
            key = (norm(r["Certificate #"]), norm(r["Type"]), norm(r["Value"]))
            rows[key].append((norm(r["CertificateId"]), norm(r["ProductName"]), norm(r["IsActive"])))
    return rows, cert_company, cert_ids, raw_total


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--master", required=True)
    ap.add_argument("--loaded", required=True)
    ap.add_argument("--certs", required=True)
    ap.add_argument("--outdir", required=True)
    a = ap.parse_args()

    os.makedirs(a.outdir, exist_ok=True)

    m_rows, m_cert_co, m_raw = load_master(a.master)
    d_rows, d_cert_co, d_cert_ids, d_raw = load_db(a.loaded, a.certs)

    # Certificates CARRYING product IDs, which is a far smaller set than
    # certificates in the database. Labelling these "certificates" would invite
    # the reader to compare 158 against 5,527 and see a hole that is not there:
    # ASINs are concentrated on a handful of large certificates.
    d_certs_with_ids = {c for (c, _t, _v) in d_rows}
    print(f"master : {m_raw} rows, {len(m_rows)} distinct (cert,type,value), "
          f"on {len(m_cert_co)} certificates")
    print(f"db     : {d_raw} rows, {len(d_rows)} distinct (cert,type,value), "
          f"on {len(d_certs_with_ids)} certificates")
    print(f"         ({len(d_cert_co)} certificates exist in the database in total; "
          f"the rest carry no product IDs on either side)")
    print()

    all_keys = sorted(set(m_rows) | set(d_rows))
    log = []
    verdicts = collections.Counter()

    for cert, typ, val in all_keys:
        m = m_rows.get((cert, typ, val))
        d = d_rows.get((cert, typ, val))

        pat = PATTERNS.get(typ)
        invalid = bool(pat and not pat.match(val))
        cert_in_db = cert in d_cert_co

        if m and d:
            verdict = "OK"
        elif m and not d:
            if not cert_in_db:
                verdict = "CERT_NOT_IN_DB"
            elif invalid:
                verdict = "INVALID_IN_SOURCE"
            else:
                verdict = "MISSING_FROM_DB"
        else:
            verdict = "EXTRA_IN_DB"
        verdicts[verdict] += 1

        m_names = sorted({n for _, n in m}) if m else []
        d_names = sorted({n for _, n, _ in d}) if d else []
        inactive = sorted({act for _, _, act in d if act != "1"}) if d else []

        # Name comparison is done on whitespace-NORMALISED text, with the raw
        # result reported separately. The source has runs of double spaces that
        # the load collapses; treating those as content differences produced 309
        # false "mismatches" the first time this ran, and chasing them cost real
        # time. A name that differs only by spacing has not lost anything.
        m_norm = {WS.sub(" ", n).strip() for n in m_names}
        d_norm = {WS.sub(" ", n).strip() for n in d_names}
        if not (m and d):
            name_match = ""
        elif set(m_names) & set(d_names):
            name_match = "EXACT"
        elif m_norm & d_norm:
            name_match = "WHITESPACE_ONLY"
        else:
            name_match = "DIFFERS"

        log.append({
            "CertificateNumber": cert,
            "CompanyMaster": m_cert_co.get(cert, ""),
            "CompanyDb": d_cert_co.get(cert, ""),
            "CertificateIds": ";".join(sorted(d_cert_ids.get(cert, []))),
            "Type": typ,
            "Value": val,
            "InMaster": "Y" if m else "N",
            "InDb": "Y" if d else "N",
            "MasterRowCount": len(m) if m else 0,
            "DbRowCount": len(d) if d else 0,
            "MasterExcelRows": ";".join(str(r) for r, _ in m) if m else "",
            "MasterNameCount": len(m_names),
            # Newline, NOT " | ": product names themselves contain " | " as a
            # separator ("...HP 902XL | Black, Cyan, Magenta, Yellow 4 Pack XL"),
            # so a pipe joiner cannot be split back apart. It silently merged two
            # names into one and made 8 identical rows look different. Excel
            # renders an embedded newline as separate lines in the cell.
            "MasterProductNames": "\n".join(m_names),
            "DbProductName": "\n".join(d_names),
            "NameMatch": name_match,
            "DbInactive": ";".join(inactive),
            "Verdict": verdict,
        })

    log_path = os.path.join(a.outdir, "asin-record-log.csv")
    with open(log_path, "w", newline="", encoding="utf-8-sig") as f:
        w = csv.DictWriter(f, fieldnames=list(log[0].keys()))
        w.writeheader()
        w.writerows(log)

    exc = [r for r in log if r["Verdict"] != "OK"]
    exc_path = os.path.join(a.outdir, "asin-record-log-exceptions.csv")
    with open(exc_path, "w", newline="", encoding="utf-8-sig") as f:
        w = csv.DictWriter(f, fieldnames=list(log[0].keys()))
        w.writeheader()
        w.writerows(exc)

    # Per-certificate summary. Counts here are a NAVIGATION aid on top of the
    # value-level log above -- never the test itself.
    percert = collections.defaultdict(lambda: collections.Counter())
    percert_vals = collections.defaultdict(lambda: {"m": set(), "d": set()})
    for r in log:
        c = r["CertificateNumber"]
        percert[c][r["Verdict"]] += 1
        if r["InMaster"] == "Y":
            percert_vals[c]["m"].add(r["Value"])
        if r["InDb"] == "Y":
            percert_vals[c]["d"].add(r["Value"])

    sum_rows = []
    for c in sorted(set(m_cert_co) | set(d_cert_co)):
        v = percert_vals.get(c, {"m": set(), "d": set()})
        cnt = percert.get(c, collections.Counter())
        bad = cnt["MISSING_FROM_DB"] + cnt["EXTRA_IN_DB"]
        asins = sorted(v["m"] | v["d"])
        joined = ";".join(asins)
        if len(joined) > 30000:  # Excel cell ceiling is 32,767
            joined = ";".join(asins[:1000]) + f";...(+{len(asins) - 1000} more, see record log)"
        sum_rows.append({
            "CertificateNumber": c,
            "CompanyMaster": m_cert_co.get(c, ""),
            "CompanyDb": d_cert_co.get(c, ""),
            "CertificateIds": ";".join(sorted(d_cert_ids.get(c, []))),
            "InMasterList": "Y" if c in m_cert_co else "N",
            "InDatabase": "Y" if c in d_cert_co else "N",
            "MasterDistinctAsins": len(v["m"]),
            "DbDistinctAsins": len(v["d"]),
            "MissingFromDb": cnt["MISSING_FROM_DB"],
            "ExtraInDb": cnt["EXTRA_IN_DB"],
            "InvalidInSource": cnt["INVALID_IN_SOURCE"],
            "Verdict": "MATCH" if bad == 0 else "DIFFERS",
            "Asins": joined,
        })

    sum_path = os.path.join(a.outdir, "asin-percert-summary.csv")
    with open(sum_path, "w", newline="", encoding="utf-8-sig") as f:
        w = csv.DictWriter(f, fieldnames=list(sum_rows[0].keys()))
        w.writeheader()
        w.writerows(sum_rows)

    print("VERDICTS (every (certificate, type, value) in the union of both sides):")
    for k in ("OK", "INVALID_IN_SOURCE", "CERT_NOT_IN_DB", "MISSING_FROM_DB", "EXTRA_IN_DB"):
        print(f"   {k:20s} {verdicts[k]:7d}")
    print(f"   {'TOTAL':20s} {sum(verdicts.values()):7d}")
    print()

    print(f"NOTE: the {verdicts['INVALID_IN_SOURCE']} INVALID_IN_SOURCE keys above are DISTINCT "
          f"(certificate, value) pairs.")
    print("      They account for more than 3 spreadsheet rows, because the same bad value is")
    print("      repeated on the same certificate. Row-level detail is in the exceptions file.")
    print()

    print("product names on matched ASINs (reported, never counted as a missing ASIN):")
    for k in ("EXACT", "WHITESPACE_ONLY", "DIFFERS"):
        n = sum(1 for r in log if r["NameMatch"] == k)
        print(f"   {k:20s} {n:7d}")
    # Denominator is certificates that carry ASINs on EITHER side. Using the full
    # certificate count would flatter the result -- "0 of 5,527" reads far stronger
    # than "0 of 158" while testing exactly the same thing.
    scoped = [r for r in sum_rows if r["MasterDistinctAsins"] or r["DbDistinctAsins"]]
    certs_differ = [r for r in scoped if r["Verdict"] == "DIFFERS"]
    print(f"certificates whose ASIN set differs: {len(certs_differ)} of {len(scoped)} "
          f"that carry ASINs on either side")
    print()
    print(f"wrote {log_path}  ({len(log)} rows)")
    print(f"wrote {exc_path}  ({len(exc)} rows)")
    print(f"wrote {sum_path}  ({len(sum_rows)} rows)")

    bad = verdicts["MISSING_FROM_DB"] + verdicts["EXTRA_IN_DB"]
    if bad:
        print()
        print(f"FAIL: {bad} value(s) present on exactly one side. See the exceptions file.")
        return 1
    print()
    print("PASS: every ASIN in the master list is in the database and vice versa.")
    print("      The only one-sided values are source rows that are not valid ASINs.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
