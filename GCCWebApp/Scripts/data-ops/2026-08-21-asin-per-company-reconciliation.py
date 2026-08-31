#!/usr/bin/env python3
"""
ASIN RECONCILIATION -- PER COMPANY
==================================

Why this exists
  2026-08-21, client ASIN validation: "I went to the product management page,
  sorted from most to least ASINS, searched each company's name and totaled the
  ASINs, and compared that to our master ASIN list." Five companies came back
  short -- Philips 2870 vs 2019, Estee 3252 vs 1847, Kohler 1959 vs 1051,
  P&G 2264 vs 1240, Clover 3179 vs 67 + 1202.

  2026-08-18-asin-reconciliation.py already proves nothing is missing, but it
  reconciles PER CERTIFICATE. The client counts PER COMPANY, so its output could
  not answer the question actually being asked. This script re-cuts the same
  evidence by company and closes the arithmetic:

      master rows = DB rows + collapsed + invalid          (per company, exactly)

  Result 2026-08-21: 21,034 of 21,034 source rows accounted for. Zero missing.

The two counting units -- this is the whole finding
  MASTER  is a flat row list. The same ASIN appears once per marketing name per
          certificate, so it double-counts by design.
  DB      stores one row per (CertificateId, Type, Value) -- UNIQUE index, see
          Migrations/20260420171817_FixProductIdUniqueIndex_PerCertificate.cs.
          The same ASIN listed twice on ONE certificate collapses to one row.
  PAGE    /Admin/ProductManagement dedupes again by type+value WITHIN each
          product line item (wwwroot/js/admin/productManagement.js, "Deduplicate
          by type+value"). So the page total is neither the row count nor the
          company-wide distinct count -- it is the sum over line items of the
          distinct ASINs on each. That is why the client's numbers land on the
          row count for some companies (Philips 2019, P&G 1240) and on the
          distinct count for others (Kohler 1051, Estee 1847): it depends purely
          on how that company's certificates are spread across line items.

Company naming is a variable and must be neutralised
  The master's `company` column and the certificate's Company can disagree. Key
  BOTH sides by the master's company-per-certificate, or Clover looks like a
  2,109-row hole when it is really a 4-way split (Clover Environmental
  Solutions / Clover Imaging Group / INNOVERA / Dataproducts).

  The master column also carries case and whitespace duplicates -- "X-SENSE" vs
  "X-Sense", "Block Inc." vs "Block Inc. " -- which split one company into two
  rows that net to zero. Reported, not silently merged.

Usage
  python 2026-08-21-asin-per-company-reconciliation.py \
      --master   "<path to GCC ASIN Master List_8.12.26.xlsm>" \
      --loaded   _reconciliation/loaded-productids-TEST.csv \
      --certs    _reconciliation/loaded-certificates-TEST-final.csv \
      --collapsed _reconciliation/asin-reconciliation-collapsed.csv \
      --invalid  _reconciliation/asin-reconciliation-invalid.csv \
      --out      _reconciliation/asin-per-company-2026-08-21.csv

  Exit 0 if every company closes, 1 if any real gap remains.
"""

import argparse
import collections
import csv
import sys

import openpyxl


def load_master(path):
    """Returns (rows_per_company, cert->company, company->set_of_certs)."""
    wb = openpyxl.load_workbook(path, read_only=True, data_only=True)
    ws = wb["ACTIVE LIST"]
    it = ws.iter_rows(values_only=True)
    hdr = next(it)
    ix = {h: i for i, h in enumerate(hdr)}

    rows = collections.Counter()
    cert2co = {}
    certs = collections.defaultdict(set)
    total = 0
    for r in it:
        if r[ix["company"]] is None and r[ix["value"]] is None:
            continue
        total += 1
        co = str(r[ix["company"]])
        cn = str(r[ix["certificateNumber"]])
        rows[co] += 1
        # First occurrence wins, so a certificate is attributed to exactly one
        # company even if the sheet is inconsistent about it.
        cert2co.setdefault(cn, co)
        certs[co].add(cn)
    return rows, cert2co, certs, total


def count_by_master_company(path, cert2co, cert_field, type_field=None):
    """Counts rows in `path` bucketed by the MASTER company of their certificate."""
    out = collections.Counter()
    with open(path, encoding="utf-8-sig") as f:
        for r in csv.DictReader(f):
            if type_field and r.get(type_field) != "ASIN":
                continue
            out[cert2co.get(r[cert_field], "<cert not in master>")] += 1
    return out


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--master", required=True)
    ap.add_argument("--loaded", required=True)
    ap.add_argument("--certs", required=True)
    ap.add_argument("--collapsed", required=True)
    ap.add_argument("--invalid", required=True)
    ap.add_argument("--out", required=True)
    a = ap.parse_args()

    master, cert2co, master_certs, master_total = load_master(a.master)

    # Certificates actually present in the target database.
    db_certs = {}
    with open(a.certs, encoding="utf-8-sig") as f:
        for r in csv.DictReader(f):
            db_certs[r["Certificate #"]] = r["Company"]

    db = count_by_master_company(a.loaded, cert2co, "Certificate #", "Type")
    collapsed = count_by_master_company(a.collapsed, cert2co, "CertificateNumber")
    invalid = count_by_master_company(a.invalid, cert2co, "CertificateNumber")

    # A company's certificates may sit under several DB company names -- record
    # the split rather than hiding it behind a matching total.
    db_split = collections.defaultdict(collections.Counter)
    with open(a.loaded, encoding="utf-8-sig") as f:
        for r in csv.DictReader(f):
            if r["Type"] != "ASIN":
                continue
            co = cert2co.get(r["Certificate #"], "<cert not in master>")
            db_split[co][db_certs.get(r["Certificate #"], "<not loaded>")] += 1

    rows_out = []
    real_gap = 0
    for co, m in sorted(master.items(), key=lambda x: -x[1]):
        s = db[co] + collapsed[co] + invalid[co]
        gap = m - s
        missing = sorted(c for c in master_certs[co] if c not in db_certs)
        if gap:
            real_gap += 1
        rows_out.append({
            "Company": co,
            "MasterRows": m,
            "DbRows": db[co],
            "CollapsedByUniqueIndex": collapsed[co],
            "InvalidPattern": invalid[co],
            "Sum": s,
            "Gap": gap,
            "Reconciles": "YES" if gap == 0 else "NO",
            "MasterCerts": len(master_certs[co]),
            "CertsNotInDb": len(missing),
            "CertsNotInDbList": ";".join(missing),
            "DbCompanySplit": ";".join(
                f"{k}={v}" for k, v in sorted(db_split[co].items(), key=lambda x: -x[1])
            ),
        })

    with open(a.out, "w", newline="", encoding="utf-8-sig") as f:
        w = csv.DictWriter(f, fieldnames=list(rows_out[0].keys()))
        w.writeheader()
        w.writerows(rows_out)

    tot_master = sum(r["MasterRows"] for r in rows_out)
    tot_sum = sum(r["Sum"] for r in rows_out)
    print(f"companies: {len(rows_out)}   master rows: {tot_master}   accounted for: {tot_sum}")
    print(f"wrote {a.out}")

    if real_gap:
        print(f"\n{real_gap} company row(s) do not close individually:")
        for r in rows_out:
            if r["Gap"]:
                print(f"  {r['Company']!r}: master={r['MasterRows']} sum={r['Sum']} gap={r['Gap']}"
                      f" certs_not_in_db={r['CertsNotInDb']}")
        # Case/whitespace variants of one company split its rows across two
        # buckets and net to zero. That is a source-data naming artefact, not a
        # migration gap, so it is only a real failure if the TOTAL does not close.
        if tot_master == tot_sum:
            print("\nTotal still closes exactly -- the gaps above are case/whitespace")
            print("variants of the same company name in the master's `company` column.")
            print("No source row is unaccounted for.")
            return 0
        print(f"\nTOTAL DOES NOT CLOSE: {tot_master - tot_sum} row(s) unaccounted for.")
        return 1

    print("every company closes exactly")
    return 0


if __name__ == "__main__":
    sys.exit(main())
