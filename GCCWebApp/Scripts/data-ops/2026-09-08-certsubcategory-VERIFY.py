# -*- coding: utf-8 -*-
"""
CERTIFICATE SUBCATEGORY BACKFILL - VERIFICATION  (DEF-411)
==========================================================
Independent of the loader: the loader's own preflight proves it wrote what its
staging table said, this proves the database now says what the client's WORKBOOKS
said. Re-reads the source workbooks rather than the generator's audit CSVs.

    python 2026-09-08-certsubcategory-VERIFY.py dev
    python 2026-09-08-certsubcategory-VERIFY.py test

Non-zero exit if any check fails, so it can gate a deploy step.

THE CHECK THAT MATTERS MOST is #3, durability. A link whose subcategory is not a
child of that certificate's own type inserts happily - the join table has no
constraint - and is then DELETED the first time anyone saves that certificate,
because CertificateController Edit (:1433) clears the collection and re-adds only
what passes `ParentId == CertificationTypeId`, and the picker never offered it.
Such a link would pass a naive row-count check and still be gone next week.
"""
from __future__ import annotations

import collections
import io
import os
import sys

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", errors="replace")

import pyodbc
import openpyxl

HERE = os.path.dirname(os.path.abspath(__file__))
REPO_ROOT = os.path.abspath(os.path.join(HERE, "..", "..", "..", ".."))
SRC = os.path.join(REPO_ROOT, "DataMigrationTestDev")

SHEETS = [
    (os.path.join(SRC, "GCC Database Migration_Certs_8.12.26V2.xlsx"), "Active List 7.31.26"),
    (os.path.join(SRC, "GCC Database Migration_Certs_8.12.26V2.xlsx"), "Expired List 2 - 8.11.26"),
    (os.path.join(SRC, "new", "GCC Database Migration_Certs_9.3.26.xlsx"), "Final Upload List 9.3.26"),
    (os.path.join(SRC, "new", "GCC Database Certs for Upload_9.4.26.xlsx"), "Missing to Add 9.4.26"),
]

TARGETS = {
    "dev": ("Driver={ODBC Driver 17 for SQL Server};Server=ANUKI01;Database=GCCDatabase_Dev;"
            "Uid=GCC_DevUser;Pwd=DevP@ss2024!SQL;TrustServerCertificate=yes;Encrypt=no;"),
    "test": ("Driver={ODBC Driver 17 for SQL Server};Server=tcp:68.71.129.120,1533;"
             "Database=gccwebap_certificates;Uid=GCC_adminxyz123;Pwd=Q3Ez4e!gE_47KNi;"
             "Encrypt=yes;TrustServerCertificate=yes;"),
}

if len(sys.argv) < 2 or sys.argv[1] not in TARGETS:
    sys.exit("usage: 2026-09-08-certsubcategory-VERIFY.py {dev|test}")
target = sys.argv[1]
print("### CERTIFICATE SUBCATEGORY VERIFY   target=%s\n" % target.upper())

fails = []


def check(name, ok, detail=""):
    print("  %s  %s%s" % ("PASS" if ok else "FAIL", name, ("   " + detail) if detail else ""))
    if not ok:
        fails.append(name)


def S(v):
    return "" if v is None else str(v).strip()


def find_header(rows):
    for i, r in enumerate(rows[:8]):
        cells = [S(c).upper() for c in r]
        if any(c in ("CERTIFICATE_NUMBER", "CERTIFICATE #", "CERTIFICATE NUMBER") for c in cells):
            return i, [S(c) for c in r]
    return None, None


# ------------------------------------------------------------------- source --
want = collections.defaultdict(set)
for path, sheet in SHEETS:
    wb = openpyxl.load_workbook(path, read_only=True, data_only=True)
    rows = list(wb[sheet].iter_rows(values_only=True))
    wb.close()
    hi, hdr = find_header(rows)
    cols = [i for i, h in enumerate(hdr)
            if "subcategor" in h.lower() and "product" not in h.lower()]
    body = [r for r in rows[hi + 1:] if r and S(r[0])]
    if not cols or not body:
        continue
    ncols = max(len(r) for r in body)
    j = max(cols) + 1
    while j < ncols and (j >= len(hdr) or not S(hdr[j])):
        cols.append(j)
        j += 1
    for r in body:
        subs = {S(r[i]) for i in cols if i < len(r) and S(r[i])}
        subs.discard("")
        if subs:
            want[S(r[0])].update(subs)

want_pairs = {(n, s) for n, subs in want.items() for s in subs}
print("source: %d certificates, %d (certificate, subcategory) pairs\n"
      % (len(want), len(want_pairs)))

cn = pyodbc.connect(TARGETS[target])
cur = cn.cursor()

# certificates that actually exist here - a cert absent from this environment cannot
# be linked, and that is a scope fact, not a load failure.
cur.execute("SELECT Number FROM dbo.Certificates")
present = {r[0] for r in cur.fetchall()}
missing_certs = sorted({n for n in want if n not in present})
expected = {(n, s) for (n, s) in want_pairs if n in present}
if missing_certs:
    print("  NOTE  %d source certificate(s) do not exist in this environment and are"
          " out of scope here:\n        %s\n" % (len(missing_certs), missing_certs[:10]))

# --------------------------------------------------------------- 1. presence --
cur.execute("""SELECT c.Number, sub.Name
               FROM dbo.CertificateCertificationTypeSubcategories x
               JOIN dbo.Certificates c ON c.Id = x.CertificateId
               JOIN dbo.CertificationTypes sub ON sub.Id = x.CertificationTypeSubcategoriesId""")
got = [(r[0], r[1]) for r in cur.fetchall()]
got_set = set(got)

check("link count matches the source", len(got_set) == len(expected),
      "database %d, source %d" % (len(got_set), len(expected)))

# Equal counts are not the same rows - compare the SETS, both directions.
only_db = got_set - expected
only_src = expected - got_set
check("the SAME pairs, compared both directions", not only_db and not only_src,
      "in DB not in source: %d %s | in source not in DB: %d %s"
      % (len(only_db), sorted(only_db)[:3], len(only_src), sorted(only_src)[:3]))

check("no duplicate link rows", len(got) == len(got_set),
      "%d rows, %d distinct" % (len(got), len(got_set)))

# ------------------------------------------------------------- 3. DURABILITY --
# The one that a row count cannot see. See the module docstring.
cur.execute("""SELECT c.Number, sub.Name, ct.Name
               FROM dbo.CertificateCertificationTypeSubcategories x
               JOIN dbo.Certificates c ON c.Id = x.CertificateId
               JOIN dbo.CertificationTypes sub ON sub.Id = x.CertificationTypeSubcategoriesId
               LEFT JOIN dbo.CertificationTypes ct ON ct.Id = c.CertificationTypeId
               WHERE sub.ParentId IS NULL OR sub.ParentId <> c.CertificationTypeId""")
fragile = cur.fetchall()
check("every link survives an edit (subcategory is a child of the cert's own type)",
      not fragile,
      "%d fragile link(s) %s" % (len(fragile), [(r[0], r[2], r[1]) for r in fragile[:3]]))

# ------------------------------------------------------- 4. nothing stray ----
cur.execute("""SELECT COUNT(*) FROM dbo.CertificateCertificationTypeSubcategories x
               JOIN dbo.Certificates c ON c.Id = x.CertificateId
               WHERE c.Number NOT IN (%s)"""
            % ",".join("'" + n.replace("'", "''") + "'" for n in want) if want else
            "SELECT 0")
stray = cur.fetchone()[0]
check("no links on certificates outside the source set", stray == 0, "%d stray" % stray)

# --------------------------------------------------- 5. the taxonomy rows ----
cur.execute("""SELECT t.Id, t.Name, p.Name, t.Level, t.SortOrder, t.IsActive,
                      ISNULL(t.NormalizedName,'<NULL>'),
                      (SELECT COUNT(*) FROM dbo.CertificateCertificationTypeSubcategories x
                       WHERE x.CertificationTypeSubcategoriesId = t.Id)
               FROM dbo.CertificationTypes t
               JOIN dbo.CertificationTypes p ON p.Id = t.ParentId
               WHERE t.ParentId <> 6
               ORDER BY p.Name, t.Name""")
tax = cur.fetchall()
print("\n  subcategory rows outside Certified Sustainability Facts (Part B's territory):")
for r in tax:
    print("     Id=%-5s %-36s under %-36s L%-2s sort=%-4s active=%-5s links=%s"
          % (r[0], r[1][:36], r[2][:36], r[3], r[4], r[5], r[7]))

bad_level = [r for r in tax if r[3] != 2 and r[7] > 0]
check("every backfilled subcategory row is Level 2", not bad_level,
      "%d wrong level %s" % (len(bad_level), [(r[0], r[1], r[3]) for r in bad_level[:3]]))
bad_norm = [r for r in tax if r[6] == "<NULL>"]
check("NormalizedName populated on the new rows (CR-187 computed column)",
      not bad_norm, "%d without %s" % (len(bad_norm), [r[1] for r in bad_norm[:3]]))
bad_active = [r for r in tax if not r[5] and r[7] > 0]
check("new subcategory rows are active", not bad_active,
      "%d inactive %s" % (len(bad_active), [r[1] for r in bad_active[:3]]))

# ------------------------------------------------- 6. what the UI will show --
cur.execute("""SELECT ct.Name, sub.Name, COUNT(*)
               FROM dbo.CertificateCertificationTypeSubcategories x
               JOIN dbo.Certificates c ON c.Id = x.CertificateId
               JOIN dbo.CertificationTypes ct ON ct.Id = c.CertificationTypeId
               JOIN dbo.CertificationTypes sub ON sub.Id = x.CertificationTypeSubcategoriesId
               GROUP BY ct.Name, sub.Name ORDER BY COUNT(*) DESC""")
print("\n  links by certificate type -> subcategory:")
for r in cur.fetchall():
    print("     %5d  %-38s -> %s" % (r[2], r[0][:38], r[1]))

cur.execute("""SELECT COUNT(DISTINCT x.CertificateId)
               FROM dbo.CertificateCertificationTypeSubcategories x""")
ncerts = cur.fetchone()[0]
print("\n  certificates now carrying at least one subcategory: %d" % ncerts)
cur.execute("SELECT COUNT(*) FROM dbo.Certificates")
print("  certificates in this database                     : %d" % cur.fetchone()[0])

cn.close()
print("\n" + ("ALL CHECKS PASSED" if not fails else "FAILURES (%d): %s" % (len(fails), fails)))
sys.exit(1 if fails else 0)
