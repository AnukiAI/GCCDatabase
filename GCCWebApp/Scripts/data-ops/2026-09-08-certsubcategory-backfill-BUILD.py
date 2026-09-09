# -*- coding: utf-8 -*-
"""
CERTIFICATE SUBCATEGORY BACKFILL  (DEF-411)  ->  two transactional T-SQL scripts
===============================================================================
`dbo.CertificateCertificationTypeSubcategories` holds 0 rows for all 6,012
certificates. The client has been supplying this data in the upload workbooks all
along. This reads those workbooks and emits the SQL to load it. READ-ONLY against
every source; the generator writes no database.

    python 2026-09-08-certsubcategory-backfill-BUILD.py
    sqlcmd -S <server> -d <db> -I -b -i partA-links.sql

WHAT IT EMITS  -  two files, deliberately separate
--------------------------------------------------
  partA-links.sql       381 links on 153 certificates. NO taxonomy change. Every
                        pair already resolves under the rule the application
                        enforces, so Part A is safe on its own and needs no
                        decision beyond "load it".

  partB-taxonomy.sql    6 new CertificationTypes child rows + the 75 links they
                        unlock. This CHANGES WHAT ANALYSTS SEE in the Create/Edit
                        subcategory picker, permanently and for every certificate
                        of those types - not just the backfilled ones. Run it only
                        with that understood.

Part B depends on Part A only in the sense that both write the same table; either
runs alone. Each emits its own UNDO.

WHY THE SPLIT EXISTS  -  the rule that decides it
-------------------------------------------------
CertificateController Create (:737) and Edit (:1443) both accept a subcategory ONLY
when `subcategory.ParentId == certificate.CertificationTypeId`, and Edit CLEARS the
collection before re-adding (:1433). The Edit picker is built the same way
(`Edit.cshtml:857`, `filter(ct => ct.parentId === typeId)`).

So a link whose subcategory is not a child of that certificate's own type is not
merely unconventional - it is DELETED THE FIRST TIME ANYONE SAVES THAT CERTIFICATE,
silently, because the picker never offered it and Edit clears what it does not see.
That is why the 75 are not simply inserted: without their parent rows they would
look loaded and then evaporate. Part B adds the parent rows first, so they stick.

WHAT PART B ADDS, AND WHY IT IS NOT INVENTING TAXONOMY
------------------------------------------------------
Every name Part B adds ALREADY EXISTS in CertificationTypes; what is missing is that
name under a SECOND parent. The table already does this 12 times over - each of
Certified Sustainability Facts' 12 children duplicates a Level-1 type name (Recycled
Content, Material Ingredient Reporting, Certified Energy Savings ...). Adding
"Waste Diversion from Landfill" under Waste Diversion from Landfill (Id 19) follows
the pattern the taxonomy is already built on.

  Zero Waste to Landfill               -> under Waste Diversion (19)    20 pairs
        the ONLY genuinely new name: it is in no CertificationTypes row today
  Carbon Footprint Reduction           -> under Certified Env. Facts (5) 25 pairs
  Waste Diversion from Landfill        -> under Waste Diversion (19)     27 pairs
  Waste Diversion from Landfill        -> under SMP (18)                  1 pair
  Sustainable Manufacturing Practices  -> under SMP (18)                  1 pair
  Waste Diversion from Landfill        -> under GreenCircle Audited (11)  1 pair

SOURCE DATA  -  audited 2026-09-08, header-identified columns only
------------------------------------------------------------------
  Certs_8.12.26V2  / Active List 7.31.26        1,026 rows -> 123 certs, 210 cells
  Certs_8.12.26V2  / Expired List 2 - 8.11.26     356 rows ->  54 certs, 106 cells
  Certs_9.4.26     / Missing to Add 9.4.26         88 rows ->  50 certs, 140 cells
  Certs_9.3.26     / Final Upload List 9.3.26     194 rows ->   0 (columns empty)
  Certs_8.12.26V2  / Expired List 1 - 4.9.26    4,349 rows -> no subcategory columns

  227 distinct certificates, 456 distinct (certificate, subcategory) pairs.
  No certificate appears in two source sheets, so nothing is double-counted.

  The CERTIFICATE_TYPE column (index 6) draws on the same vocabulary as the
  subcategory columns. Detecting columns by their VALUES therefore counts every
  certificate's own type as a subcategory and inflates 227 to 1,599. Columns are
  identified by HEADER TEXT ONLY here, and the header row is searched for rather
  than assumed to be row 1.
"""
from __future__ import annotations

import argparse
import collections
import csv
import datetime
import io
import os
import sys

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", errors="replace")

try:
    import openpyxl
except ImportError:
    sys.exit("openpyxl is required:  pip install openpyxl")
try:
    import pyodbc
except ImportError:
    sys.exit("pyodbc is required")

HERE = os.path.dirname(os.path.abspath(__file__))
APP_ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
REPO_ROOT = os.path.abspath(os.path.join(APP_ROOT, "..", ".."))
SRC = os.path.join(REPO_ROOT, "DataMigrationTestDev")

SHEETS = [
    (os.path.join(SRC, "GCC Database Migration_Certs_8.12.26V2.xlsx"), "Active List 7.31.26"),
    (os.path.join(SRC, "GCC Database Migration_Certs_8.12.26V2.xlsx"), "Expired List 2 - 8.11.26"),
    (os.path.join(SRC, "new", "GCC Database Migration_Certs_9.3.26.xlsx"), "Final Upload List 9.3.26"),
    (os.path.join(SRC, "new", "GCC Database Certs for Upload_9.4.26.xlsx"), "Missing to Add 9.4.26"),
]

# Classification is done against a live database so the split is measured, not
# assumed -- but the emitted SQL re-verifies everything at run time and aborts on a
# disagreement, so a stale classification cannot load the wrong thing.
TARGETS = {
    "dev": ("Driver={ODBC Driver 17 for SQL Server};Server=ANUKI01;Database=GCCDatabase_Dev;"
            "Uid=GCC_DevUser;Pwd=DevP@ss2024!SQL;TrustServerCertificate=yes;Encrypt=no;"),
    "test": ("Driver={ODBC Driver 17 for SQL Server};Server=tcp:68.71.129.120,1533;"
             "Database=gccwebap_certificates;Uid=GCC_adminxyz123;Pwd=Q3Ez4e!gE_47KNi;"
             "Encrypt=yes;TrustServerCertificate=yes;"),
}


def S(v) -> str:
    return "" if v is None else str(v).strip()


def sq(v) -> str:
    if v is None:
        return "NULL"
    s = str(v).strip()
    return "NULL" if s == "" else "N'" + s.replace("'", "''") + "'"


def open_wb(path):
    try:
        return openpyxl.load_workbook(path, read_only=True, data_only=True)
    except PermissionError:
        import shutil, tempfile
        t = os.path.join(tempfile.gettempdir(), "subcat-" + os.path.basename(path))
        shutil.copy2(path, t)
        print("  (locked - read from a copy at %s)" % t)
        return openpyxl.load_workbook(t, read_only=True, data_only=True)


def find_header(rows):
    for i, r in enumerate(rows[:8]):
        cells = [S(c).upper() for c in r]
        if any(c in ("CERTIFICATE_NUMBER", "CERTIFICATE #", "CERTIFICATE NUMBER") for c in cells):
            return i, [S(c) for c in r]
    return None, None


def read_pairs():
    """number -> {subcategory}. Header text only; see the module docstring."""
    pairs = collections.defaultdict(set)
    origin = {}
    for path, sheet in SHEETS:
        wb = open_wb(path)
        if sheet not in wb.sheetnames:
            sys.exit("ABORT: sheet %r not in %s" % (sheet, os.path.basename(path)))
        rows = list(wb[sheet].iter_rows(values_only=True))
        wb.close()
        hi, hdr = find_header(rows)
        if hi is None:
            sys.exit("ABORT: no certificate-number header row in %s" % sheet)
        cols = [i for i, h in enumerate(hdr)
                if "subcategor" in h.lower() and "product" not in h.lower()]
        if not cols:
            print("  %-30s no subcategory columns" % sheet)
            continue
        # trailing blank-header columns belong to the same repeated block
        body = [r for r in rows[hi + 1:] if r and S(r[0])]
        ncols = max(len(r) for r in body) if body else len(hdr)
        j = max(cols) + 1
        while j < ncols and (j >= len(hdr) or not S(hdr[j])):
            cols.append(j)
            j += 1
        n = 0
        for r in body:
            num = S(r[0])
            subs = {S(r[i]) for i in cols if i < len(r) and S(r[i])}
            subs.discard("")
            if subs:
                pairs[num].update(subs)
                origin.setdefault(num, "%s :: %s" % (os.path.basename(path), sheet))
                n += 1
        print("  %-30s %4d rows -> %3d with subcategories (cols %s)"
              % (sheet, len(body), n, ",".join(str(c) for c in sorted(cols))))
    return pairs, origin


HEADER_TMPL = """/* ============================================================================
   {title}
   ----------------------------------------------------------------------------
   DEF-411 certificate-subcategory backfill, generated {stamp}
   Generated by Scripts/data-ops/2026-09-08-certsubcategory-backfill-BUILD.py
   Do not hand-edit; change the generator and re-run, or the audit CSVs stop
   describing this file.

{summary}

   RUN WITH:  sqlcmd -S <server> -d <database> -I -b -i this-file.sql
   -I (QUOTED_IDENTIFIER ON) is REQUIRED - CR-187 persisted computed columns.
   -b makes a failure a non-zero exit.
============================================================================ */
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;

DECLARE @now datetime2(7) = SYSUTCDATETIME();
DECLARE @errmsg nvarchar(400);
DECLARE @bad int;

-- Encoding canary. A UTF-8 file decoded by sqlcmd as the ANSI codepage silently
-- mangles every non-ASCII character; the August load lost 1,674 product names that
-- way. These literals equal the corresponding NCHAR only if this file decoded right.
IF N'®' <> NCHAR(174) OR N'™' <> NCHAR(8482)
BEGIN
    RAISERROR('ABORT: this script was decoded with the wrong codepage. Re-run against the UTF-16 file as generated (sqlcmd auto-detects the BOM), or pass -f 65001.', 16, 1);
    RETURN;
END;

PRINT '=== {title} ({stamp}) ===';
"""

STAGING = """
-------------------------------------------------------------------- staging --
IF OBJECT_ID('tempdb..#Sub') IS NOT NULL DROP TABLE #Sub;
CREATE TABLE #Sub (
    Number      nvarchar(100) NOT NULL,
    SubcatName  nvarchar(200) NOT NULL
);
"""

PREFLIGHT = """
------------------------------------------------------------------ preflight --
-- Nothing is written until every one of these passes. Resolving a subcategory to
-- the WRONG row is worse than failing, so anything ambiguous aborts the run.

-- 1. every staged certificate number exists, exactly once.
SELECT @bad = COUNT(*) FROM (
    SELECT s.Number FROM #Sub s
    GROUP BY s.Number
    HAVING (SELECT COUNT(*) FROM dbo.Certificates c WHERE c.Number = s.Number) <> 1) x;
IF @bad > 0 BEGIN
    SELECT DISTINCT 'CERTIFICATE NOT FOUND (or duplicated)' AS Problem, s.Number AS Value,
           (SELECT COUNT(*) FROM dbo.Certificates c WHERE c.Number = s.Number) AS Matches
    FROM #Sub s WHERE (SELECT COUNT(*) FROM dbo.Certificates c WHERE c.Number = s.Number) <> 1;
    SET @errmsg = CAST(@bad AS nvarchar(10)) + ' staged certificate number(s) do not resolve to exactly one record.';
    RAISERROR(@errmsg,16,1); RETURN; END;

-- 2. THE RULE. Every (certificate, subcategory) must resolve to exactly one
--    CertificationTypes row that is a CHILD OF THAT CERTIFICATE'S OWN TYPE. This is
--    the condition CertificateController enforces on Create (:737) and Edit (:1443);
--    a link that fails it is deleted the next time anyone saves the certificate.
SELECT @bad = COUNT(*) FROM #Sub s
JOIN   dbo.Certificates c ON c.Number = s.Number
WHERE  (SELECT COUNT(*) FROM dbo.CertificationTypes t
        WHERE t.ParentId = c.CertificationTypeId AND t.Name = s.SubcatName) <> 1;
IF @bad > 0 BEGIN
    SELECT DISTINCT 'SUBCATEGORY NOT A CHILD OF THIS CERT TYPE' AS Problem,
           s.Number AS Certificate, ct.Name AS CertificateType, s.SubcatName AS Subcategory,
           (SELECT COUNT(*) FROM dbo.CertificationTypes t
            WHERE t.ParentId = c.CertificationTypeId AND t.Name = s.SubcatName) AS Matches
    FROM   #Sub s
    JOIN   dbo.Certificates c ON c.Number = s.Number
    LEFT JOIN dbo.CertificationTypes ct ON ct.Id = c.CertificationTypeId
    WHERE (SELECT COUNT(*) FROM dbo.CertificationTypes t
           WHERE t.ParentId = c.CertificationTypeId AND t.Name = s.SubcatName) <> 1;
    SET @errmsg = CAST(@bad AS nvarchar(10)) + ' staged pair(s) name a subcategory that is not a child of that certificate''s type. They would be silently deleted on the next edit - run the taxonomy script first, or drop them.';
    RAISERROR(@errmsg,16,1); RETURN; END;

-- 3. this is additive: a link that already exists means a re-run.
SELECT @bad = COUNT(*)
FROM   #Sub s
JOIN   dbo.Certificates c ON c.Number = s.Number
JOIN   dbo.CertificationTypes t ON t.ParentId = c.CertificationTypeId AND t.Name = s.SubcatName
JOIN   dbo.CertificateCertificationTypeSubcategories x
       ON x.CertificateId = c.Id AND x.CertificationTypeSubcategoriesId = t.Id;
IF @bad > 0 BEGIN
    SET @errmsg = CAST(@bad AS nvarchar(10)) + ' of these links already exist. This is a re-run; nothing was written.';
    RAISERROR(@errmsg,16,1); RETURN; END;

PRINT '  preflight OK';
"""

LOAD = """
--------------------------------------------------------------------- load ---
BEGIN TRANSACTION;

INSERT INTO dbo.CertificateCertificationTypeSubcategories (CertificateId, CertificationTypeSubcategoriesId)
SELECT DISTINCT c.Id, t.Id
FROM   #Sub s
JOIN   dbo.Certificates c ON c.Number = s.Number
JOIN   dbo.CertificationTypes t ON t.ParentId = c.CertificationTypeId AND t.Name = s.SubcatName;
PRINT '  subcategory links inserted : ' + CAST(@@ROWCOUNT AS varchar(10));

COMMIT TRANSACTION;

PRINT '';
PRINT '=================== POST-LOAD ===================';
SELECT 'CertificateCertificationTypeSubcategories' AS TableName, COUNT(*) AS Rows
FROM dbo.CertificateCertificationTypeSubcategories;

SELECT ct.Name AS CertificateType, sub.Name AS Subcategory, COUNT(*) AS Links
FROM   dbo.CertificateCertificationTypeSubcategories x
JOIN   dbo.Certificates c ON c.Id = x.CertificateId
JOIN   dbo.CertificationTypes ct ON ct.Id = c.CertificationTypeId
JOIN   dbo.CertificationTypes sub ON sub.Id = x.CertificationTypeSubcategoriesId
GROUP BY ct.Name, sub.Name ORDER BY COUNT(*) DESC;

PRINT 'LOAD COMPLETE. Run the verification before accepting.';
"""


def write_sql(path, title, summary, stamp, rows, taxonomy=None):
    out = [HEADER_TMPL.format(title=title, stamp=stamp, summary=summary)]
    if taxonomy:
        out.append("""
--------------------------------------------------- new taxonomy rows (pre) --
-- These must exist BEFORE the preflight resolves subcategories. Created OUTSIDE
-- the load transaction on purpose: a CertificationTypes row is reference data, it
-- is harmless if the link load later rolls back, and creating it inside would make
-- the preflight's own lookup order fragile.
--
-- NOTE: each of these becomes a selectable option in the Create/Edit subcategory
-- picker for EVERY certificate of the parent type, not only the backfilled ones
-- (Edit.cshtml:857 filters allCertificationTypes by parentId).""")
        for name, parent_name, parent_id, n in taxonomy:
            pname = name.replace("'", "''")
            out.append(f"""
IF NOT EXISTS (SELECT 1 FROM dbo.CertificationTypes WHERE Name = {sq(name)} AND ParentId = {parent_id})
BEGIN
    IF NOT EXISTS (SELECT 1 FROM dbo.CertificationTypes WHERE Id = {parent_id})
    BEGIN
        RAISERROR('ABORT: parent CertificationTypes Id={parent_id} ({parent_name}) does not exist.',16,1);
        RETURN;
    END;
    -- SortOrder is NOT NULL with no default (caught by the dev rehearsal, 2026-09-08).
    -- NormalizedName is a PERSISTED COMPUTED column (CR-187) and must NOT be inserted.
    INSERT INTO dbo.CertificationTypes (Name, ParentId, Level, IsActive, SortOrder)
    SELECT {sq(name)}, {parent_id}, 2, 1,
           ISNULL((SELECT MAX(SortOrder) FROM dbo.CertificationTypes WHERE ParentId = {parent_id}), 0) + 1;
    PRINT '  created subcategory "{pname}" under {parent_name} (Id={parent_id}) - unlocks {n} link(s)';
END
ELSE PRINT '  subcategory "{pname}" already present under Id={parent_id}';""")
    out.append(STAGING)
    out.append("-- staged pairs ------------------------------------------------------------")
    B = 900
    for i in range(0, len(rows), B):
        chunk = rows[i:i + B]
        out.append("INSERT INTO #Sub (Number, SubcatName) VALUES")
        out.append(",\n".join("  (%s, %s)" % (sq(a), sq(b)) for a, b in chunk) + ";")
    out.append(PREFLIGHT)
    out.append(LOAD)
    with io.open(path, "w", encoding="utf-16-le", newline="\r\n") as fh:
        fh.write("﻿" + "\n".join(out))
    return path


def write_undo(path, title, stamp, rows, taxonomy=None):
    out = [f"""/* ============================================================================
   UNDO for {title}  ({stamp})
   ----------------------------------------------------------------------------
   Deletes ONLY the links this run inserted, matched on the same
   (certificate, subcategory-under-its-own-type) rule that created them. The link
   table held 0 rows database-wide before this backfill, so there is nothing older
   to preserve - but the delete is scoped anyway rather than truncating.
============================================================================ */
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
PRINT '=== UNDO {title} ===';

IF OBJECT_ID('tempdb..#Undo') IS NOT NULL DROP TABLE #Undo;
CREATE TABLE #Undo (Number nvarchar(100) NOT NULL, SubcatName nvarchar(200) NOT NULL);
"""]
    B = 900
    for i in range(0, len(rows), B):
        chunk = rows[i:i + B]
        out.append("INSERT INTO #Undo (Number, SubcatName) VALUES")
        out.append(",\n".join("  (%s, %s)" % (sq(a), sq(b)) for a, b in chunk) + ";")
    out.append("""
BEGIN TRANSACTION;
DELETE x
FROM   dbo.CertificateCertificationTypeSubcategories x
JOIN   dbo.Certificates c ON c.Id = x.CertificateId
JOIN   #Undo u ON u.Number = c.Number
JOIN   dbo.CertificationTypes t ON t.Id = x.CertificationTypeSubcategoriesId
       AND t.ParentId = c.CertificationTypeId AND t.Name = u.SubcatName;
PRINT '  links deleted : ' + CAST(@@ROWCOUNT AS varchar(10));
COMMIT TRANSACTION;""")
    if taxonomy:
        out.append("""
-- The taxonomy rows are left in place on purpose: once an analyst has picked one on
-- some other certificate, deleting it would remove THEIR data too. Remove them by
-- hand only after confirming nothing references them. The guarded deletes below do
-- exactly that check, and are commented out so nothing runs by accident.""")
        for name, parent_name, parent_id, n in taxonomy:
            pname = name.replace("'", "''")
            out.append(f"""
-- IF NOT EXISTS (SELECT 1 FROM dbo.CertificateCertificationTypeSubcategories x
--                JOIN dbo.CertificationTypes t ON t.Id = x.CertificationTypeSubcategoriesId
--                WHERE t.Name = {sq(name)} AND t.ParentId = {parent_id})
--     DELETE FROM dbo.CertificationTypes WHERE Name = {sq(name)} AND ParentId = {parent_id};""")
    out.append("\nPRINT 'UNDO COMPLETE.';")
    with io.open(path, "w", encoding="utf-16-le", newline="\r\n") as fh:
        fh.write("﻿" + "\n".join(out))
    return path


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--classify-against", default="test", choices=sorted(TARGETS),
                    help="database read (READ-ONLY) to split loadable from blocked pairs")
    ap.add_argument("--out", default=os.path.join(HERE, "_subcat_backfill"))
    args = ap.parse_args()

    stamp = datetime.datetime.now().strftime("%Y%m%d-%H%M%S")
    outdir = os.path.join(args.out, stamp)
    os.makedirs(outdir, exist_ok=True)

    print("CERTIFICATE SUBCATEGORY BACKFILL BUILD  (DEF-411)")
    print("  classify against : %s  (READ-ONLY)" % args.classify_against)
    print("  out              : %s\n" % outdir)

    print("reading source workbooks:")
    pairs, origin = read_pairs()
    total = sum(len(v) for v in pairs.values())
    print("\n  %d certificates, %d distinct (certificate, subcategory) pairs" % (len(pairs), total))

    cn = pyodbc.connect(TARGETS[args.classify_against])
    cu = cn.cursor()
    cu.execute("""SELECT c.Number, c.CertificationTypeId, ISNULL(ct.Name,''), c.Status
                  FROM dbo.Certificates c
                  LEFT JOIN dbo.CertificationTypes ct ON ct.Id = c.CertificationTypeId""")
    certs = {r[0]: dict(ctid=r[1], ctname=r[2], status=r[3]) for r in cu.fetchall()}
    cu.execute("SELECT Id, Name, ParentId FROM dbo.CertificationTypes WHERE ParentId IS NOT NULL")
    child = {(r[2], r[1].strip().lower()) for r in cu.fetchall()}
    cu.execute("SELECT COUNT(*) FROM dbo.CertificateCertificationTypeSubcategories")
    existing = cu.fetchone()[0]
    cn.close()
    print("  link table currently holds %d row(s)" % existing)

    partA, partB, orphan = [], [], []
    for num in sorted(pairs):
        c = certs.get(num)
        if not c:
            for s in sorted(pairs[num]):
                orphan.append((num, s))
            continue
        for s in sorted(pairs[num]):
            (partA if (c["ctid"], s.strip().lower()) in child else partB).append((num, s))

    print("\n  Part A (loads today, no taxonomy change) : %4d pairs on %3d certificates"
          % (len(partA), len({p[0] for p in partA})))
    print("  Part B (needs new taxonomy rows)         : %4d pairs on %3d certificates"
          % (len(partB), len({p[0] for p in partB})))
    if orphan:
        print("  certificate number not in the database   : %4d  %s"
              % (len(orphan), sorted({o[0] for o in orphan})[:8]))

    # taxonomy rows Part B needs: (name, parent name, parent id, pairs unlocked)
    need = collections.Counter()
    for num, s in partB:
        c = certs[num]
        need[(s, c["ctname"], c["ctid"])] += 1
    taxonomy = [(n, pn, pid, cnt) for (n, pn, pid), cnt in
                sorted(need.items(), key=lambda x: -x[1])]
    if taxonomy:
        print("\n  Part B would create %d CertificationTypes row(s):" % len(taxonomy))
        for n, pn, pid, cnt in taxonomy:
            print("     %-38s under %-34s (Id=%s)  unlocks %d" % (n, pn, pid, cnt))

    sumA = ("   Inserts {n} subcategory links on {c} certificates.\n"
            "   NO taxonomy change - every pair already resolves under the rule the\n"
            "   application enforces, so nothing here alters what analysts can pick."
            ).format(n=len(partA), c=len({p[0] for p in partA}))
    sumB = ("   Creates {t} CertificationTypes child row(s), then inserts {n} links on {c}\n"
            "   certificates that depend on them.\n"
            "   THIS CHANGES THE CREATE/EDIT SUBCATEGORY PICKER for every certificate of\n"
            "   those parent types, permanently - not only the backfilled ones."
            ).format(t=len(taxonomy), n=len(partB), c=len({p[0] for p in partB}))

    a_sql = write_sql(os.path.join(outdir, "partA-links.sql"),
                      "PART A - certificate subcategory links", sumA, stamp, partA)
    a_undo = write_undo(os.path.join(outdir, "partA-UNDO.sql"),
                        "partA-links.sql", stamp, partA)
    print("\nWrote %s" % a_sql)
    print("Wrote %s" % a_undo)
    if partB:
        b_sql = write_sql(os.path.join(outdir, "partB-taxonomy.sql"),
                          "PART B - new taxonomy rows + the links they unlock",
                          sumB, stamp, partB, taxonomy=taxonomy)
        b_undo = write_undo(os.path.join(outdir, "partB-UNDO.sql"),
                            "partB-taxonomy.sql", stamp, partB, taxonomy=taxonomy)
        print("Wrote %s" % b_sql)
        print("Wrote %s" % b_undo)

    def write_csv(name, header, rows_):
        with io.open(os.path.join(outdir, name), "w", encoding="utf-8-sig", newline="") as fh:
            w = csv.writer(fh)
            w.writerow(header)
            w.writerows(rows_)
        print("  wrote %s (%d rows)" % (name, len(rows_)))

    print("\naudit files:")
    write_csv("partA-will-load.csv",
              ["Certificate #", "Status", "Certificate type", "Subcategory", "Source"],
              [[n, certs[n]["status"], certs[n]["ctname"], s, origin.get(n, "")] for n, s in partA])
    write_csv("partB-needs-taxonomy.csv",
              ["Certificate #", "Status", "Certificate type", "Subcategory",
               "New row needed", "Under parent", "Source"],
              [[n, certs[n]["status"], certs[n]["ctname"], s, s,
                "%s (Id=%s)" % (certs[n]["ctname"], certs[n]["ctid"]), origin.get(n, "")]
               for n, s in partB])
    write_csv("taxonomy-rows-to-create.csv",
              ["New subcategory", "Parent type", "Parent Id", "Links unlocked"],
              [[n, pn, pid, cnt] for n, pn, pid, cnt in taxonomy])

    print("\nNOTHING HAS BEEN WRITTEN TO ANY DATABASE. To apply:")
    print('  sqlcmd -S <server> -d <db> -I -b -i "%s"' % a_sql)
    if partB:
        print('  sqlcmd -S <server> -d <db> -I -b -i "%s"   # taxonomy change' % b_sql)


if __name__ == "__main__":
    main()
