# -*- coding: utf-8 -*-
"""
BATCH 3 POST-LOAD VERIFICATION
==============================
Compares the database against the audit CSVs 2026-09-08-batch3-load-BUILD.py wrote,
field by field.  It is deliberately independent of the loader: the loader's own
assertions prove it did what its staging tables said, this proves the staging tables
said what the client's spreadsheet said.

    python 2026-09-08-batch3-VERIFY.py dev
    python 2026-09-08-batch3-VERIFY.py test

Exit code is non-zero if any check fails, so it can gate a deploy step.

DIFFERENCES FROM THE BATCH-2 VERIFIER
-------------------------------------
* Batch 3 has no renewals, no product IDs and no multi-country certificates, so
  those three checks report NOT APPLICABLE instead of passing vacuously.  A check
  that cannot fail is not evidence, and batch 2's multi-country assertion would
  have FAILED here on an empty set rather than saying so.
* The encoding probe targets a company from THIS load.
* Two checks are new, both asserting things this load deliberately did NOT do, so
  the decisions are evidenced rather than remembered:
    - the 140 client-supplied certificate subcategories are still unloaded
    - the 26 Amazon certificates carry no product IDs (no ASIN file in the drop)
"""

from __future__ import annotations

import csv
import io
import os
import sys
from collections import defaultdict

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", errors="replace")

import pyodbc

HERE = os.path.dirname(os.path.abspath(__file__))
OUTBASE = os.path.join(HERE, "_batch3_output")

TARGETS = {
    "dev": ("Driver={ODBC Driver 17 for SQL Server};Server=ANUKI01;Database=GCCDatabase_Dev;"
            "Uid=GCC_DevUser;Pwd=DevP@ss2024!SQL;TrustServerCertificate=yes;Encrypt=no;"),
    "test": ("Driver={ODBC Driver 17 for SQL Server};Server=tcp:68.71.129.120,1533;"
             "Database=gccwebap_certificates;Uid=GCC_adminxyz123;Pwd=Q3Ez4e!gE_47KNi;"
             "Encrypt=yes;TrustServerCertificate=yes;"),
}

# There is no default target on purpose: two scripts in this folder silently
# defaulted to dev and produced findings about the wrong environment.
if len(sys.argv) < 2 or sys.argv[1] not in TARGETS:
    sys.exit("usage: 2026-09-08-batch3-VERIFY.py {dev|test} [output-dir]")
target = sys.argv[1]

outdir = sys.argv[2] if len(sys.argv) > 2 else os.path.join(
    OUTBASE, sorted(os.listdir(OUTBASE))[-1])

print(f"### BATCH 3 VERIFY  target={target.upper()}")
print(f"### audit set: {outdir}\n")


def rd(name):
    with io.open(os.path.join(outdir, name), encoding="utf-8-sig") as fh:
        return list(csv.DictReader(fh))


certs = rd("certs-loaded.csv")
pids = rd("product-ids.csv")
renewals = rd("renewals-updated.csv")

cn = pyodbc.connect(TARGETS[target])
cur = cn.cursor()

fails = []
skipped = []


def check(name, ok, detail=""):
    print(f"  {'PASS' if ok else 'FAIL'}  {name}" + (f"   {detail}" if detail else ""))
    if not ok:
        fails.append(name)


def na(name, why):
    """A check with nothing to measure. Reported as NOT APPLICABLE rather than
    PASS: batch 2 had renewals, product IDs and multi-country rows and batch 3
    has none, and a green tick against an empty set would hide that difference."""
    print(f"  n/a   {name}   ({why})")
    skipped.append(name)


numbers = [c["Certificate #"] for c in certs]
inlist = ",".join("'" + n.replace("'", "''") + "'" for n in numbers)

# 1 -------------------------------------------------- presence and uniqueness
cur.execute("SELECT Number, COUNT(*) FROM Certificates GROUP BY Number")
have = {r[0]: r[1] for r in cur.fetchall()}
missing = [n for n in numbers if n not in have]
dupes = [n for n in numbers if have.get(n, 0) > 1]
check(f"all {len(numbers)} certificates present exactly once",
      not missing and not dupes, f"missing={missing[:5]} dupes={dupes[:5]}")

# 2 --------------------------------------------------------- field-by-field
cur.execute("""
SELECT c.Number, co.Name, p.Name, ct.Name, c.Status,
       CONVERT(varchar(10), c.EffectiveDate, 120), CONVERT(varchar(10), c.ExpirationDate, 120),
       ISNULL(c.Location, ''), ISNULL(c.Country, ''), ISNULL(c.CertificationResults, ''),
       ISNULL(pc.Name, ''), ISNULL(pt.Name, ''), ISNULL(ps.Name, ''), ISNULL(pw.Name, '')
FROM   Certificates c
JOIN   Companies co ON co.Id = c.CompanyId
JOIN   Products p ON p.Id = c.ProductId
JOIN   CertificationTypes ct ON ct.Id = c.CertificationTypeId
LEFT JOIN ProductCategories pc ON pc.Id = c.ProductCategoryId
LEFT JOIN ProductTypes pt ON pt.Id = c.ProductTypeId
LEFT JOIN ProductTypes ps ON ps.Id = c.ProductSubcategoryId
LEFT JOIN Pathways pw ON pw.Id = c.PathwayId""")
db = {r[0]: list(r[1:]) for r in cur.fetchall()}

FIELDS = [("Company", 0), ("Product Name", 1), ("Certification Type", 2), ("Status", 3),
          ("Effective Date", 4), ("Expiration Date", 5), ("Location", 6),
          ("Country (primary)", 7), ("Cert Results", 8), ("Product Category", 9),
          ("Product Type", 10), ("Subcategory", 11), ("Pathway", 12)]
mismatch = defaultdict(list)
case_only = []
for c in certs:
    row = db.get(c["Certificate #"])
    if not row:
        continue
    for label, i in FIELDS:
        want = (c.get(label) or "").strip()
        got = (row[i] or "").strip()
        if want == got:
            continue
        # SQL Server's collation is case-INSENSITIVE, so a lookup by name matches a
        # record that differs only in case and the loader correctly reuses it rather
        # than creating a near-duplicate. That is not a load defect — it is the
        # client's own two spreadsheets disagreeing ("GPxx" in August, "GPXX" in
        # September). Reported, never failed: "fixing" it would mean two product
        # records the app itself cannot tell apart.
        if want.casefold() == got.casefold():
            case_only.append((c["Certificate #"], label, want, got))
            continue
        mismatch[label].append((c["Certificate #"], want, got))
for label, _ in FIELDS:
    m = mismatch[label]
    check(f"field '{label}'", not m, f"{len(m)} mismatch(es) {m[:3] if m else ''}")
if case_only:
    print(f"\n  INFO  {len(case_only)} field(s) differ from the sheet in LETTER CASE only, because "
          f"the value already existed in the database under the other casing and was reused:")
    for num, label, want, got in case_only:
        print(f"          {num}  {label}: sheet {want!r} -> reused existing {got!r}")
    print()

# 3 ------------------------------------------------------------- countries
cur.execute("""SELECT c.Number, cc.Country FROM CertificateCountries cc
               JOIN Certificates c ON c.Id = cc.CertificateId""")
dbc = defaultdict(set)
for n, co in cur.fetchall():
    dbc[n].add(co)
bad = []
for c in certs:
    want = {x.strip() for x in (c["All countries"] or "").split(";") if x.strip()}
    got = dbc.get(c["Certificate #"], set())
    if want != got:
        bad.append((c["Certificate #"], sorted(want), sorted(got)))
check("CertificateCountries matches the source, as a SET both ways", not bad,
      f"{len(bad)} {bad[:3]}")
multi = [c for c in certs if ";" in (c["All countries"] or "")]
if multi:
    check(f"the {len(multi)} multi-country certificates carry every country",
          all(len(dbc.get(c["Certificate #"], set())) > 1 for c in multi),
          f"{len(multi)} multi-country rows")
else:
    na("multi-country certificates carry every country",
       "this drop has none - every row names exactly one country")
# Every certificate must still have its ONE row: without it the country filter
# cannot see it at all, which is the CR-037 failure the client reported.
no_row = [c["Certificate #"] for c in certs if not dbc.get(c["Certificate #"])]
check(f"all {len(certs)} certificates have at least one CertificateCountries row",
      not no_row, f"{len(no_row)} without one {no_row[:5]}")

# 4 ----------------------------------------------------------- product IDs
cur.execute("""SELECT c.Number, pid.ProductIdValue, pid.Status, ISNULL(pid.Partner, '')
               FROM CertificateProductIds pid JOIN Certificates c ON c.Id = pid.CertificateId""")
dbp = defaultdict(dict)
for n, v, st, pa in cur.fetchall():
    dbp[n][v] = (st, pa)
missing_p = [(p["Certificate #"], p["Value"]) for p in pids
             if p["Value"] not in dbp.get(p["Certificate #"], {})]
if pids:
    check(f"all {len(pids)} product IDs present", not missing_p,
          f"{len(missing_p)} missing {missing_p[:3]}")
else:
    na("product IDs present", "no ASIN workbook in this drop - nothing was staged")

statmap = {c["Certificate #"]: c["Status"] for c in certs}
wrong = []
for p in pids:
    n = p["Certificate #"]
    if n not in statmap or p["Value"] not in dbp.get(n, {}):
        continue          # ID on a pre-existing certificate; CR-015 already settled there
    expect = "Active" if statmap[n] in ("Client Approved - Paid", "Republished") else "Inactive"
    got = dbp[n][p["Value"]][0]
    if got != expect:
        wrong.append((n, p["Value"], got, expect))
if pids:
    check("CR-015: product-ID status derives from its certificate", not wrong,
          f"{len(wrong)} {wrong[:3]}")
else:
    na("CR-015 product-ID status derives from its certificate", "no product IDs in this load")

# 5 ------------------------------------------------ partner scoping, both filters
cur.execute("""SELECT c.Number FROM CertificatePartners cp
               JOIN Certificates c ON c.Id = cp.CertificatesId
               JOIN Partners p ON p.Id = cp.PartnersId WHERE p.Name = 'Amazon'""")
amazon = {r[0] for r in cur.fetchall()}
want_amz = {c["Certificate #"] for c in certs if (c["Partner"] or "").strip() == "Amazon"}
check(f"all {len(want_amz)} Amazon certificates have a CertificatePartners row",
      want_amz <= amazon, f"{len(want_amz - amazon)} missing {sorted(want_amz - amazon)[:5]}")

cur.execute(f"""SELECT COUNT(*) FROM CertificateProductIds pid
                JOIN Certificates c ON c.Id = pid.CertificateId
                WHERE c.Number IN ({inlist}) AND pid.Partner IS NOT NULL
                  AND NOT EXISTS (SELECT 1 FROM CertificatePartners cp WHERE cp.CertificatesId = c.Id)""")
orphan = cur.fetchone()[0]
check("no partner-tagged product ID sits on an unassociated certificate "
      "(the August Gate-3 shape)", orphan == 0, f"{orphan} orphan(s)")

# Scoped to the product IDs THIS LOAD staged, not to the 193 new certificates: some
# of the drop's ASINs attach to certificates that already existed (24-5714), and
# scoping by certificate silently excused those from the check.
pid_keys = {(p["Certificate #"], p["Value"]) for p in pids}
cur.execute("""SELECT c.Number, pid.ProductIdValue,
                      (SELECT COUNT(*) FROM CertificateProductIdPartners x
                       WHERE x.CertificateProductIdId = pid.Id)
               FROM CertificateProductIds pid JOIN Certificates c ON c.Id = pid.CertificateId""")
unlinked = [(n, v) for n, v, k in cur.fetchall() if (n, v) in pid_keys and k == 0]
if pids:
    check(f"every one of the {len(pids)} loaded product IDs has a partner link",
          not unlinked, f"{len(unlinked)} unlinked {unlinked[:3]}")
else:
    na("loaded product IDs have a partner link", "no product IDs in this load")

# 6 -------------------------------------------------------------- encoding
cur.execute("SELECT COUNT(*) FROM Companies WHERE Name LIKE N'%' + NCHAR(174) + N'%'")
reg = cur.fetchone()[0]
cur.execute("SELECT COUNT(*) FROM Companies WHERE CHARINDEX(NCHAR(194), Name) > 0 "
            "OR CHARINDEX(NCHAR(195), Name) > 0")
moji = cur.fetchone()[0]
check("no mojibake in company names", moji == 0,
      f"{reg} names hold a real U+00AE; {moji} hold a stray C2/C3")
# Two companies in this drop carry non-ASCII: a registered-trademark sign and an
# umlaut. Both are attached to certificates in THIS load, so a codepage failure
# on this run shows up here rather than in a pre-existing row.
cur.execute("SELECT Name FROM Companies WHERE Name LIKE 'Malarkey Roofing%'")
row = cur.fetchone()
check("company name round-tripped its registered-trademark sign",
      bool(row) and chr(174) in row[0], repr(row[0]) if row else "not found")
cur.execute("SELECT Name FROM Companies WHERE Name LIKE 'Alfred K%rcher%'")
row = cur.fetchone()
check("company name round-tripped its umlaut",
      bool(row) and chr(228) in row[0], repr(row[0]) if row else "not found")

# The two companies this load was authorised to create, and only those two.
EXPECTED_NEW = {"McKinney, a brand associated with ASSA ABLOY Accessories Hardware Group, Inc.",
                "Sylvamo North America, LLC"}
for name in sorted(EXPECTED_NEW):
    cur.execute("SELECT COUNT(*) FROM Companies WHERE Name = ?", name)
    check(f"new company created exactly once: {name[:44]}", cur.fetchone()[0] == 1)

# 7 -------------------------------------------------------------- renewals
if renewals:
    rlist = ",".join("'" + x["Certificate #"].replace("'", "''") + "'" for x in renewals)
    cur.execute(f"""SELECT Number, Status, CONVERT(varchar(10), EffectiveDate, 120),
                           CONVERT(varchar(10), ExpirationDate, 120)
                    FROM Certificates WHERE Number IN ({rlist})""")
    dr = {r[0]: tuple(r[1:]) for r in cur.fetchall()}
    bad_r = [(x["Certificate #"], dr.get(x["Certificate #"])) for x in renewals
             if dr.get(x["Certificate #"]) != (x["New Status"], x["New Effective"], x["New Expiration"])]
    check(f"the {len(renewals)} renewals took their new dates and status", not bad_r, str(bad_r))
else:
    na("renewals took their new dates and status",
       "this drop has none - all 88 numbers were absent from the target")

# 8 ------------------------------------------------------------ subcategory
want_sub = {c["Certificate #"] for c in certs if (c["Subcategory"] or "").strip()}
cur.execute(f"SELECT Number FROM Certificates WHERE Number IN ({inlist}) "
            "AND ProductSubcategoryId IS NOT NULL")
got_sub = {r[0] for r in cur.fetchall()}
check(f"ProductSubcategoryId populated on the {len(want_sub)} rows that name one",
      want_sub == got_sub, f"missing {sorted(want_sub - got_sub)[:5]} extra {sorted(got_sub - want_sub)[:5]}")

# 9 ------------------------------------------------------- CR-042 results shape
cur.execute(f"""SELECT COUNT(*) FROM Certificates WHERE Number IN ({inlist})
                AND CertificationResults IS NOT NULL AND LTRIM(RTRIM(CertificationResults)) <> ''
                AND CertificationResults NOT LIKE '%[%]%'
                AND TRY_CONVERT(decimal(18,6), CertificationResults) IS NOT NULL""")
bare = cur.fetchone()[0]
check("CR-042: no bare numeric certification result was stored", bare == 0,
      f"{bare} bare numeric")

# 10 ------------------------------------------------------- public visibility
cur.execute(f"""SELECT COUNT(*) FROM Certificates
                WHERE Number IN ({inlist})
                  AND Status IN ('Client Approved - Paid','Republished')
                  AND EffectiveDate <= CAST(GETUTCDATE() AS date)
                  AND ExpirationDate >= CAST(GETUTCDATE() AS date)""")
visible = cur.fetchone()[0]
paid = sum(1 for c in certs if c["Status"] in ("Client Approved - Paid", "Republished"))
print(f"\n  INFO  {visible} of this load's {len(certs)} certificates are publicly visible today "
      f"({paid} carry a publishable status; the rest are Draft/Confidential or outside their "
      f"validity window — DEF-358).")

cur.execute(f"SELECT COUNT(*) FROM Certificates WHERE Number IN ({inlist}) AND Status = 'Draft'")
print(f"  INFO  {cur.fetchone()[0]} are Draft and therefore invisible to the public and to partners.")

# 11 ------------------------------ the subcategories this load deliberately dropped
# Shawn's decision, 2026-09-08: match the rest of the database rather than make these
# 88 the only certificates with links. Asserted so the decision stays visible, and so
# a later backfill has a dated before-state to work from.
cur.execute(f"""SELECT COUNT(*) FROM CertificateCertificationTypeSubcategories s
                JOIN Certificates c ON c.Id = s.CertificateId
                WHERE c.Number IN ({inlist})""")
n_sub_links = cur.fetchone()[0]
check("certificate subcategories were NOT loaded, as decided", n_sub_links == 0,
      f"{n_sub_links} link(s) present; expected 0")
cur.execute("SELECT COUNT(*) FROM CertificateCertificationTypeSubcategories")
print(f"          the link table holds {cur.fetchone()[0]} row(s) database-wide - the 140 "
      "values from this sheet are recorded in cert-subcategories-NOT-loaded.csv")

# 12 ------------------------------------- the Amazon certificates have no ASINs yet
cur.execute(f"""SELECT COUNT(DISTINCT c.Number) FROM Certificates c
                JOIN CertificateProductIds pid ON pid.CertificateId = c.Id
                WHERE c.Number IN ({inlist})""")
with_pids = cur.fetchone()[0]
check("no certificate in this load carries a product ID (none were supplied)",
      with_pids == 0, f"{with_pids} certificate(s) unexpectedly have product IDs")
print(f"          {len(want_amz)} Amazon-partner certificates are loaded WITHOUT ASINs; they "
      "reach the Amazon API\n          as certificates but carry no product-level data until "
      "the client sends a covering ASIN list.")

if skipped:
    print(f"\n{len(skipped)} check(s) not applicable to this drop:")
    for n in skipped:
        print(f"    {n}")
print("\n" + ("ALL CHECKS PASSED" if not fails else f"FAILURES ({len(fails)}): {fails}"))
sys.exit(1 if fails else 0)
