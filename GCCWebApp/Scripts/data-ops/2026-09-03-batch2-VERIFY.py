# -*- coding: utf-8 -*-
"""
BATCH 2 POST-LOAD VERIFICATION
==============================
Compares the database against the audit CSVs 2026-09-03-batch2-load-BUILD.py wrote,
field by field.  It is deliberately independent of the loader: the loader's own
assertions prove it did what its staging tables said, this proves the staging tables
said what the client's spreadsheet said.

    python 2026-09-03-batch2-VERIFY.py dev
    python 2026-09-03-batch2-VERIFY.py test

Exit code is non-zero if any check fails, so it can gate a deploy step.
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
OUTBASE = os.path.join(HERE, "_batch2_output")

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
    sys.exit("usage: 2026-09-03-batch2-VERIFY.py {dev|test} [output-dir]")
target = sys.argv[1]

outdir = sys.argv[2] if len(sys.argv) > 2 else os.path.join(
    OUTBASE, sorted(os.listdir(OUTBASE))[-1])

print(f"### BATCH 2 VERIFY  target={target.upper()}")
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


def check(name, ok, detail=""):
    print(f"  {'PASS' if ok else 'FAIL'}  {name}" + (f"   {detail}" if detail else ""))
    if not ok:
        fails.append(name)


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
check(f"the {len(multi)} multi-country certificates carry every country",
      bool(multi) and all(len(dbc.get(c["Certificate #"], set())) > 1 for c in multi),
      f"{len(multi)} multi-country rows")

# 4 ----------------------------------------------------------- product IDs
cur.execute("""SELECT c.Number, pid.ProductIdValue, pid.Status, ISNULL(pid.Partner, '')
               FROM CertificateProductIds pid JOIN Certificates c ON c.Id = pid.CertificateId""")
dbp = defaultdict(dict)
for n, v, st, pa in cur.fetchall():
    dbp[n][v] = (st, pa)
missing_p = [(p["Certificate #"], p["Value"]) for p in pids
             if p["Value"] not in dbp.get(p["Certificate #"], {})]
check(f"all {len(pids)} product IDs present", not missing_p,
      f"{len(missing_p)} missing {missing_p[:3]}")

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
check("CR-015: product-ID status derives from its certificate", not wrong,
      f"{len(wrong)} {wrong[:3]}")

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
check(f"every one of the {len(pids)} loaded product IDs has a partner link",
      not unlinked, f"{len(unlinked)} unlinked {unlinked[:3]}")

# 6 -------------------------------------------------------------- encoding
cur.execute("SELECT COUNT(*) FROM Companies WHERE Name LIKE N'%' + NCHAR(174) + N'%'")
reg = cur.fetchone()[0]
cur.execute("SELECT COUNT(*) FROM Companies WHERE CHARINDEX(NCHAR(194), Name) > 0 "
            "OR CHARINDEX(NCHAR(195), Name) > 0")
moji = cur.fetchone()[0]
check("no mojibake in company names", moji == 0,
      f"{reg} names hold a real U+00AE; {moji} hold a stray C2/C3")
cur.execute("SELECT Name FROM Companies WHERE Name LIKE 'ASR Group%Whitworth%'")
row = cur.fetchone()
check("the new company name round-tripped its non-ASCII characters",
      bool(row) and chr(174) in row[0], repr(row[0]) if row else "not found")

# 7 -------------------------------------------------------------- renewals
cur.execute("""SELECT Number, Status, CONVERT(varchar(10), EffectiveDate, 120),
                      CONVERT(varchar(10), ExpirationDate, 120)
               FROM Certificates WHERE Number IN ('25-0497','25-8163','25-8164')""")
dr = {r[0]: tuple(r[1:]) for r in cur.fetchall()}
bad_r = [(x["Certificate #"], dr.get(x["Certificate #"])) for x in renewals
         if dr.get(x["Certificate #"]) != (x["New Status"], x["New Effective"], x["New Expiration"])]
check(f"the {len(renewals)} renewals took their new dates and status", not bad_r, str(bad_r))

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

print("\n" + ("ALL CHECKS PASSED" if not fails else f"FAILURES ({len(fails)}): {fails}"))
sys.exit(1 if fails else 0)
