#!/usr/bin/env python3
"""
MIGRATION SQL GENERATOR — transform output -> one transactional T-SQL load script
=================================================================================
Emits a direct-SQL loader for the ONE-TIME go-live migration, bypassing the
certificate import pipeline. It does NOT replace the importer for ongoing
analyst use — different job, different tool.

WHY A SEPARATE PATH AT ALL
--------------------------
The importer is the tested path, and normally the right one. For this load it is
not, for reasons measured on dev:
  * DEF-350 — previewing a ONE-ROW file against 5,405 certificates takes 148-266s
    and, at the stock 30s CommandTimeout, simply fails. Cause still unknown.
  * DEF-351 — the product-ID CSV parser splits on every comma, corrupting 50.7%
    of product names.
This script sidesteps both. It is only defensible because the value-level
reconciliation (`2026-08-13-migration-reconciliation.*`) verifies the result
independently of how the rows got there.

WHAT IT DELIBERATELY DOES NOT DO
--------------------------------
  * No AuditHistory rows and no ImportBatches row (Shawn, 2026-08-13). There is
    therefore NO UndoImport for this load — rollback is "restore the backup",
    which for a one-time load onto an empty database is stronger anyway.
  * ProductSubcategoryId is left NULL. `ProductSubcategories.ProductTypeId` is
    NOT NULL, so creating the 12 source subcategories means inventing a parent
    product type for each — a design decision inside DEF-348, not something to
    smuggle into a loader. The values are reported instead.

RULES REPLICATED FROM THE IMPORTER — each verified, not assumed
---------------------------------------------------------------
  * Lookups resolve BY NAME AT RUN TIME, never by baked-in Id. Dev and test do
    not share Ids; baking them is the DEF-323 mistake.
  * CertificationType: MIN(Id) per Name. The importer uses FirstOrDefault over an
    unordered fetch; VERIFIED against the loaded data — all 5,405 certificates
    use MIN(Id) for their type name (the hierarchy means one name can exist at
    several Levels, see [[feedback_certtypes_hierarchy]]).
  * Product identity = CR-186 tuple (CompanyId, Name, CategoryId, ProductTypeId,
    ProductSubcategoryId). Same-name products with different types are SEPARATE
    records; get this wrong and certificates point at the wrong product.
  * Products.CategoryId is NOT NULL; a blank source category falls back to the
    lowest active ProductCategory, replicating `categories.FirstOrDefault()`.
    VERIFIED: 1,766 loaded products carry that default.
  * CertificateProductId.Status derives from the parent certificate
    (CR-015): "Active" iff the cert status is in PublicStatuses
    (Client Approved - Paid, Republished), else "Inactive".
  * Country is canonicalised through Models/CountryList.cs — parsed from that
    file at generation time so it cannot drift from the app's own list.

Usage
  python 2026-08-13-migration-sql-generator.py [transform_dir]

Output: ./_sql_output/<timestamp>/migration-load.sql
Run it with:  sqlcmd -S <srv> -d <db> -I -i migration-load.sql
(-I is required: CR-187 persisted computed columns.)
"""

from __future__ import annotations

import csv
import datetime
import glob
import io
import os
import re
import sys
import unicodedata
from collections import OrderedDict

HERE = os.path.dirname(os.path.abspath(__file__))
APP_ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
COUNTRY_LIST_CS = os.path.join(APP_ROOT, "Models", "CountryList.cs")

CERT_FILES = ["certs-active.csv", "certs-expired2.csv", "certs-expired1.csv"]
BATCH = 900  # SQL Server caps a VALUES clause at 1000 rows; leave headroom.

PUBLIC_STATUSES = {"Client Approved - Paid", "Republished"}


# ----------------------------------------------------------------- country --
def cs_normalize(v: str) -> str:
    """Mirror CountryList.Normalize: lowercase, strip accents + punctuation,
    collapse whitespace. 'U.S.A. ' -> 'usa'."""
    if not v:
        return ""
    s = unicodedata.normalize("NFD", v)
    s = "".join(c for c in s if unicodedata.category(c) != "Mn")
    s = s.lower()
    s = "".join(c for c in s if c.isalnum() or c.isspace())
    return re.sub(r"\s+", " ", s).strip()


def load_country_map():
    """Parse Countries + Aliases straight out of CountryList.cs so this script
    cannot drift from the list the application actually uses."""
    if not os.path.exists(COUNTRY_LIST_CS):
        sys.exit(f"CountryList.cs not found at {COUNTRY_LIST_CS}")
    src = io.open(COUNTRY_LIST_CS, encoding="utf-8-sig").read()

    m = re.search(r"Countries\s*=\s*new\(\)\s*\{(.*?)\};", src, re.S)
    if not m:
        sys.exit("Could not parse the Countries list from CountryList.cs")
    countries = re.findall(r'"([^"]+)"', m.group(1))

    a = re.search(r"Aliases\s*=\s*new\([^)]*\)\s*\{(.*?)\};", src, re.S)
    aliases = dict(re.findall(r'\["([^"]+)"\]\s*=\s*"([^"]+)"', a.group(1))) if a else {}

    canon = {cs_normalize(c): c for c in countries}
    for k, v in aliases.items():
        canon[cs_normalize(k)] = v
    return canon, len(countries), len(aliases)


# ------------------------------------------------------------------- utils --
def sq(v) -> str:
    """SQL literal. NULL for empty; N'' with doubled quotes otherwise."""
    if v is None:
        return "NULL"
    s = str(v).strip()
    if s == "":
        return "NULL"
    return "N'" + s.replace("'", "''") + "'"


def sdate(v) -> str:
    s = (v or "").strip()
    return f"'{s}'" if s else "NULL"


def split_multi(v):
    """Partner and tag cells are comma/pipe delimited; 'None' is not a value."""
    if not v:
        return []
    parts = re.split(r"[,|]", str(v))
    out = []
    for p in parts:
        p = p.strip()
        if p and p.lower() != "none" and p not in out:
            out.append(p)
    return out


def emit_batched_insert(out, table, columns, rows):
    """INSERT ... VALUES in chunks, so no single statement exceeds SQL's row cap."""
    if not rows:
        out.append(f"-- (no rows for {table})\n")
        return
    collist = ", ".join(columns)
    for i in range(0, len(rows), BATCH):
        chunk = rows[i:i + BATCH]
        out.append(f"INSERT INTO {table} ({collist}) VALUES")
        out.append(",\n".join("  (" + ", ".join(r) + ")" for r in chunk) + ";")
        out.append("")


# -------------------------------------------------------------------- main --
def main():
    tdir = sys.argv[1] if len(sys.argv) > 1 else None
    if not tdir:
        dirs = sorted(glob.glob(os.path.join(HERE, "_transform_output", "*/")))
        if not dirs:
            sys.exit("No _transform_output/<ts>/ found — run the transform first.")
        tdir = dirs[-1]
    if not os.path.isdir(tdir):
        sys.exit(f"Not a directory: {tdir}")

    canon, n_countries, n_aliases = load_country_map()
    print(f"transform : {tdir}")
    print(f"countries : {n_countries} canonical + {n_aliases} aliases from CountryList.cs\n")

    # CR-038: a certificate number may legitimately appear on SEVERAL rows now, so this is a
    # LIST, not a dict keyed by number. It used to dedupe by number, which silently kept only the
    # first of each duplicated set — exactly the data the client asked us to preserve.
    certs = []
    cert_numbers = set()
    cert_tags, cert_partners = [], []
    country_unmapped = {}
    subcat_skipped = 0

    for fn in CERT_FILES:
        p = os.path.join(tdir, fn)
        if not os.path.exists(p):
            print(f"!! missing {fn}")
            continue
        n = 0
        for r in csv.DictReader(io.open(p, encoding="utf-8-sig")):
            num = (r["Certificate #"] or "").strip()
            if not num:
                continue

            raw_country = (r.get("Country") or "").strip()
            country = canon.get(cs_normalize(raw_country), raw_country) if raw_country else ""
            if raw_country and cs_normalize(raw_country) not in canon:
                country_unmapped[raw_country] = country_unmapped.get(raw_country, 0) + 1

            if (r.get("Subcategory") or "").strip():
                subcat_skipped += 1

            certs.append(r | {"_country": country})
            # Tag and partner links are keyed by NUMBER, so a duplicated number would otherwise
            # emit the same link several times. The load INSERTs are DISTINCT + NOT EXISTS
            # guarded, but keep the staging clean rather than relying on that.
            if num not in cert_numbers:
                for t in split_multi(r.get("External Tag")) + split_multi(r.get("Internal Tag")):
                    cert_tags.append((num, t))
                for pn in split_multi(r.get("Partner")):
                    cert_partners.append((num, pn))
            cert_numbers.add(num)
            n += 1
        print(f"  {fn:22} {n} certificates")

    pid_path = os.path.join(tdir, "productids-all.csv")
    pids = []
    if os.path.exists(pid_path):
        for r in csv.DictReader(io.open(pid_path, encoding="utf-8-sig")):
            if (r["certificateNumber"] or "").strip() in cert_numbers:
                pids.append(r)
    print(f"  productids-all.csv     {len(pids)} product IDs")
    print(f"  cert-tag links {len(cert_tags)} | cert-partner links {len(cert_partners)}")
    if subcat_skipped:
        print(f"  NOTE {subcat_skipped} rows carry a Product Subcategory — left NULL (DEF-348)")
    if country_unmapped:
        print(f"  NOTE {len(country_unmapped)} country value(s) not in CountryList, passed through as-is: "
              + ", ".join(sorted(country_unmapped)[:5]))

    stamp = datetime.datetime.now().strftime("%Y%m%d-%H%M%S")
    outdir = os.path.join(HERE, "_sql_output", stamp)
    os.makedirs(outdir, exist_ok=True)
    out = []

    # Unknown countries are passed through verbatim, matching the importer: DEF-347
    # deliberately warns rather than rejects, so a migration spelling never costs a
    # certificate. But "passed through" must be counted, not silent — every value
    # lands here for review.
    if country_unmapped:
        with io.open(os.path.join(outdir, "review-unmapped-countries.csv"), "w",
                     newline="", encoding="utf-8-sig") as fh:
            w = csv.writer(fh)
            w.writerow(["CountryValue", "RowCount", "Note"])
            for val, n in sorted(country_unmapped.items(), key=lambda x: -x[1]):
                note = ("multi-country value — see CR-037" if re.search(r"[;,]", val)
                        else "not in CountryList.Countries")
                w.writerow([val, n, note])

    out.append(f"""/*==============================================================================
  GO-LIVE MIGRATION LOAD  —  generated {stamp}
  Source: {os.path.basename(os.path.normpath(tdir))}
  ------------------------------------------------------------------------------
  {len(certs)} certificate records ({len(cert_numbers)} distinct numbers), {len(pids)} product IDs, {len(cert_tags)} tag links,
  {len(cert_partners)} partner links.

  RUN WITH:  sqlcmd -S <server> -d <database> -I -i migration-load.sql
  -I (QUOTED_IDENTIFIER ON) is REQUIRED — CR-187 persisted computed columns.

  One transaction. Preflight checks run BEFORE any write and abort on any
  unresolved lookup or pre-existing certificate number, so a bad run leaves
  nothing behind. Post-load assertions roll back on any count mismatch.

  NO AuditHistory and NO ImportBatches rows are written (decision: Shawn,
  2026-08-13) — so there is no UndoImport for this load. Rollback = restore the
  database backup taken immediately beforehand.

  Lookups resolve BY NAME at run time — no Ids are baked into this file, so it
  is safe to run against dev, test or prod without regeneration.
==============================================================================*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

DECLARE @now datetime2(7) = SYSUTCDATETIME();
DECLARE @errmsg nvarchar(400);

------------------------------------------------------------- encoding guard --
-- This file is UTF-16 LE with a BOM. If it is ever re-saved as UTF-8, or piped
-- through a tool that decodes it with the ANSI codepage, every non-ASCII literal
-- below silently becomes mojibake and the load still reports success — that is
-- exactly what happened on the first run of this generator ("EcoFlex(R)" landed
-- as two characters, corrupting 1,674 product names with no error anywhere).
-- These literals are canaries: if the file was decoded correctly they equal the
-- corresponding NCHAR, and if it was not, they do not.
IF N'®' <> NCHAR(174) OR N'™' <> NCHAR(8482)
BEGIN
    RAISERROR('ABORT: this script was decoded with the wrong codepage - non-ASCII text would be corrupted. Re-run with the UTF-16 file as generated (sqlcmd auto-detects the BOM), or pass -f 65001 for UTF-8.', 16, 1);
    RETURN;
END;

-------------------------------------------------------------------- staging --
CREATE TABLE #Cert (
    Number        nvarchar(50)  NOT NULL,   -- CR-038: no longer unique in staging
    CertType      nvarchar(200) NULL,
    Status        nvarchar(50)  NULL,
    Company       nvarchar(255) NULL,
    ProductName   nvarchar(1000) NULL,
    Category      nvarchar(200) NULL,
    ProductType   nvarchar(200) NULL,
    Pathway       nvarchar(200) NULL,
    CertResults   nvarchar(1000) NULL,
    EffectiveDate date NULL,
    ExpirationDate date NULL,
    Location      nvarchar(175) NULL,
    Country       nvarchar(100) NULL,
    CompanyEmail  nvarchar(255) NULL,
    CompanyUrl    nvarchar(500) NULL
);
CREATE TABLE #CertTag     (Number nvarchar(50) NOT NULL, TagName nvarchar(200) NOT NULL);
CREATE TABLE #CertPartner (Number nvarchar(50) NOT NULL, PartnerName nvarchar(100) NOT NULL);
CREATE TABLE #Pid (
    Seq         int identity(1,1),
    Number      nvarchar(50)  NOT NULL,
    IdType      nvarchar(20)  NOT NULL,
    IdValue     nvarchar(100) NOT NULL,
    PartnerName nvarchar(100) NULL,
    ProductName nvarchar(1000) NULL
);
""")

    cert_rows = []
    for r in certs:
        num = (r["Certificate #"] or "").strip()
        cert_rows.append([
            sq(num), sq(r.get("Certification Type")), sq(r.get("Status")),
            sq(r.get("Company")), sq(r.get("Product Name")),
            sq(r.get("Product Category")), sq(r.get("Product Type")),
            sq(r.get("Pathway")), sq(r.get("Cert Results")),
            sdate(r.get("Effective Date")), sdate(r.get("Expiration Date")),
            sq(r.get("Location")), sq(r["_country"]),
            sq(r.get("Company Email")), sq(r.get("Company URL")),
        ])
    out.append("-- certificates ------------------------------------------------------------")
    emit_batched_insert(out, "#Cert",
        ["Number", "CertType", "Status", "Company", "ProductName", "Category", "ProductType",
         "Pathway", "CertResults", "EffectiveDate", "ExpirationDate", "Location", "Country",
         "CompanyEmail", "CompanyUrl"], cert_rows)

    out.append("-- cert -> tag -------------------------------------------------------------")
    emit_batched_insert(out, "#CertTag", ["Number", "TagName"],
                        [[sq(a), sq(b)] for a, b in cert_tags])
    out.append("-- cert -> partner ---------------------------------------------------------")
    emit_batched_insert(out, "#CertPartner", ["Number", "PartnerName"],
                        [[sq(a), sq(b)] for a, b in cert_partners])
    out.append("-- product ids -------------------------------------------------------------")
    emit_batched_insert(out, "#Pid", ["Number", "IdType", "IdValue", "PartnerName", "ProductName"],
                        [[sq(r["certificateNumber"]), sq(r["type"]), sq(r["value"]),
                          sq(r.get("Partner")), sq(r.get("productName"))] for r in pids])

    out.append(r"""
------------------------------------------------------------------ preflight --
-- Nothing is written until every one of these passes. Resolving a lookup to the
-- wrong row is worse than failing, so unresolved names abort the run.

-- lowest-Id-per-name, matching the importer's FirstOrDefault (verified against
-- all 5,405 rows of the rehearsal load).
IF OBJECT_ID('tempdb..#CT') IS NOT NULL DROP TABLE #CT;
SELECT Name, MIN(Id) AS Id INTO #CT FROM dbo.CertificationTypes GROUP BY Name;
IF OBJECT_ID('tempdb..#PC') IS NOT NULL DROP TABLE #PC;
SELECT Name, MIN(Id) AS Id INTO #PC FROM dbo.ProductCategories GROUP BY Name;
IF OBJECT_ID('tempdb..#PT') IS NOT NULL DROP TABLE #PT;
SELECT Name, MIN(Id) AS Id INTO #PT FROM dbo.ProductTypes GROUP BY Name;
IF OBJECT_ID('tempdb..#PW') IS NOT NULL DROP TABLE #PW;
SELECT Name, MIN(Id) AS Id INTO #PW FROM dbo.Pathways GROUP BY Name;

DECLARE @defaultCategory int = (SELECT MIN(Id) FROM dbo.ProductCategories WHERE IsActive = 1);
IF @defaultCategory IS NULL
BEGIN RAISERROR('ABORT: no active ProductCategory exists; Products.CategoryId is NOT NULL.',16,1); RETURN; END;

PRINT '--- preflight ---';

DECLARE @bad int;

SELECT @bad = COUNT(*) FROM #Cert s WHERE s.CertType IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM #CT x WHERE x.Name = s.CertType);
IF @bad > 0 BEGIN
    SELECT DISTINCT 'UNRESOLVED CertificationType' AS Problem, s.CertType AS Value FROM #Cert s
      WHERE s.CertType IS NOT NULL AND NOT EXISTS (SELECT 1 FROM #CT x WHERE x.Name = s.CertType);
    SET @errmsg = CAST(@bad AS nvarchar(10)) + ' row(s) reference an unknown Certification Type.';
    RAISERROR(@errmsg,16,1); RETURN; END;

SELECT @bad = COUNT(*) FROM #Cert s WHERE s.Category IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM #PC x WHERE x.Name = s.Category);
IF @bad > 0 BEGIN
    SELECT DISTINCT 'UNRESOLVED ProductCategory' AS Problem, s.Category AS Value FROM #Cert s
      WHERE s.Category IS NOT NULL AND NOT EXISTS (SELECT 1 FROM #PC x WHERE x.Name = s.Category);
    SET @errmsg = CAST(@bad AS nvarchar(10)) + ' row(s) reference an unknown Product Category.';
    RAISERROR(@errmsg,16,1); RETURN; END;

SELECT @bad = COUNT(*) FROM #Cert s WHERE s.ProductType IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM #PT x WHERE x.Name = s.ProductType);
IF @bad > 0 BEGIN
    SELECT DISTINCT 'UNRESOLVED ProductType' AS Problem, s.ProductType AS Value FROM #Cert s
      WHERE s.ProductType IS NOT NULL AND NOT EXISTS (SELECT 1 FROM #PT x WHERE x.Name = s.ProductType);
    SET @errmsg = CAST(@bad AS nvarchar(10)) + ' row(s) reference an unknown Product Type.';
    RAISERROR(@errmsg,16,1); RETURN; END;

SELECT @bad = COUNT(*) FROM #Cert s WHERE s.Pathway IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM #PW x WHERE x.Name = s.Pathway);
IF @bad > 0 BEGIN
    SELECT DISTINCT 'UNRESOLVED Pathway' AS Problem, s.Pathway AS Value FROM #Cert s
      WHERE s.Pathway IS NOT NULL AND NOT EXISTS (SELECT 1 FROM #PW x WHERE x.Name = s.Pathway);
    SET @errmsg = CAST(@bad AS nvarchar(10)) + ' row(s) reference an unknown Pathway.';
    RAISERROR(@errmsg,16,1); RETURN; END;

SELECT @bad = COUNT(*) FROM #CertTag t WHERE NOT EXISTS (SELECT 1 FROM dbo.Tags g WHERE g.Name = t.TagName);
IF @bad > 0 BEGIN
    SELECT DISTINCT 'UNRESOLVED Tag' AS Problem, t.TagName AS Value FROM #CertTag t
      WHERE NOT EXISTS (SELECT 1 FROM dbo.Tags g WHERE g.Name = t.TagName);
    SET @errmsg = CAST(@bad AS nvarchar(10)) + ' tag link(s) reference an unknown Tag. Tags are on the preserve list and are not auto-created here.';
    RAISERROR(@errmsg,16,1); RETURN; END;

SELECT @bad = COUNT(*) FROM #CertPartner p WHERE NOT EXISTS (SELECT 1 FROM dbo.Partners r WHERE r.Name = p.PartnerName);
IF @bad > 0 BEGIN
    SELECT DISTINCT 'UNRESOLVED Partner' AS Problem, p.PartnerName AS Value FROM #CertPartner p
      WHERE NOT EXISTS (SELECT 1 FROM dbo.Partners r WHERE r.Name = p.PartnerName);
    SET @errmsg = CAST(@bad AS nvarchar(10)) + ' partner link(s) reference an unknown Partner.';
    RAISERROR(@errmsg,16,1); RETURN; END;

-- Certificate.Number is UNIQUE. Re-running onto a populated database must fail
-- loudly rather than half-insert.
SELECT @bad = COUNT(DISTINCT s.Number) FROM #Cert s JOIN dbo.Certificates c ON c.Number = s.Number;
IF @bad > 0 BEGIN
    SET @errmsg = CAST(@bad AS nvarchar(10)) + ' certificate number(s) already exist in the target. Purge first, or this load is a re-run.';
    RAISERROR(@errmsg,16,1); RETURN; END;

-- Dates: DEF-353 says never invent one. The transform should have withheld such
-- rows; if any arrive, stop rather than substitute.
SELECT @bad = COUNT(*) FROM #Cert WHERE EffectiveDate IS NULL OR ExpirationDate IS NULL;
IF @bad > 0 BEGIN
    SET @errmsg = CAST(@bad AS nvarchar(10)) + ' row(s) have a missing Effective/Expiration date. DEF-353: dates are never defaulted.';
    RAISERROR(@errmsg,16,1); RETURN; END;

PRINT '  preflight OK';

--------------------------------------------------------------------- load ---
BEGIN TRANSACTION;

-- 1. Companies (target is empty after the purge, but NOT EXISTS keeps it safe)
INSERT INTO dbo.Companies (Name, IsActive, IsDeleted, CreatedAt, UpdatedAt)
SELECT DISTINCT s.Company, 1, 0, @now, @now
FROM   #Cert s
WHERE  s.Company IS NOT NULL
  AND  NOT EXISTS (SELECT 1 FROM dbo.Companies c WHERE c.Name = s.Company);
PRINT '  companies inserted: ' + CAST(@@ROWCOUNT AS varchar(10));

-- 2. Products — CR-186 identity is (CompanyId, Name, CategoryId, ProductTypeId,
--    ProductSubcategoryId). Same name + different type = SEPARATE product.
INSERT INTO dbo.Products (Name, CompanyId, CategoryId, ProductTypeId, IsActive, IsDeleted, CreatedAt, UpdatedAt)
SELECT DISTINCT s.ProductName, co.Id, ISNULL(pc.Id, @defaultCategory), pt.Id, 1, 0, @now, @now
FROM   #Cert s
JOIN   dbo.Companies co ON co.Name = s.Company
LEFT JOIN #PC pc ON pc.Name = s.Category
LEFT JOIN #PT pt ON pt.Name = s.ProductType
WHERE  s.ProductName IS NOT NULL
  AND  NOT EXISTS (
        SELECT 1 FROM dbo.Products p
        WHERE  p.Name = s.ProductName
          AND  p.CompanyId = co.Id
          AND  p.CategoryId = ISNULL(pc.Id, @defaultCategory)
          AND  ISNULL(p.ProductTypeId, -1) = ISNULL(pt.Id, -1)
          AND  p.ProductSubcategoryId IS NULL);
PRINT '  products  inserted: ' + CAST(@@ROWCOUNT AS varchar(10));

-- 3. Certificates
INSERT INTO dbo.Certificates
    (Number, CompanyId, ProductId, CertificationTypeId, ProductCategoryId, ProductTypeId,
     Status, EffectiveDate, ExpirationDate, Location, Country, Partner,
     CertificationResults, PathwayId, AllowsDuplicateNumber, CreatedAt, UpdatedAt)
SELECT s.Number, co.Id, p.Id, ct.Id, pc.Id, pt.Id,
       s.Status, s.EffectiveDate, s.ExpirationDate, s.Location, s.Country,
       ISNULL((SELECT TOP 1 cp.PartnerName FROM #CertPartner cp WHERE cp.Number = s.Number ORDER BY cp.PartnerName), 'None'),
       s.CertResults, pw.Id,
       -- CR-038: exempt ONLY the rows that actually share a number, derived from the staged
       -- data rather than a hand-maintained list. Flagging the whole migration would silently
       -- drop the uniqueness guarantee for every other certificate.
       CASE WHEN d.Number IS NULL THEN 0 ELSE 1 END,
       @now, @now
FROM   #Cert s
JOIN   dbo.Companies co ON co.Name = s.Company
JOIN   #CT ct ON ct.Name = s.CertType
LEFT JOIN #PC pc ON pc.Name = s.Category
LEFT JOIN #PT pt ON pt.Name = s.ProductType
LEFT JOIN #PW pw ON pw.Name = s.Pathway
LEFT JOIN (SELECT Number FROM #Cert GROUP BY Number HAVING COUNT(*) > 1) d ON d.Number = s.Number
JOIN   dbo.Products p
       ON  p.Name = s.ProductName
       AND p.CompanyId = co.Id
       AND p.CategoryId = ISNULL(pc.Id, @defaultCategory)
       AND ISNULL(p.ProductTypeId, -1) = ISNULL(pt.Id, -1)
       AND p.ProductSubcategoryId IS NULL;
PRINT '  certificates inserted: ' + CAST(@@ROWCOUNT AS varchar(10));

-- 4. Tag links (external + internal both land in the same M2M)
INSERT INTO dbo.CertificateTag (CertificatesId, TagsId)
SELECT DISTINCT c.Id, g.Id
FROM   #CertTag t
JOIN   dbo.Certificates c ON c.Number = t.Number
JOIN   dbo.Tags g ON g.Name = t.TagName
WHERE  NOT EXISTS (SELECT 1 FROM dbo.CertificateTag x WHERE x.CertificatesId = c.Id AND x.TagsId = g.Id);
PRINT '  cert-tag links: ' + CAST(@@ROWCOUNT AS varchar(10));

-- 5. Partner links (CR-171)
INSERT INTO dbo.CertificatePartners (CertificatesId, PartnersId)
SELECT DISTINCT c.Id, r.Id
FROM   #CertPartner p
JOIN   dbo.Certificates c ON c.Number = p.Number
JOIN   dbo.Partners r ON r.Name = p.PartnerName
WHERE  NOT EXISTS (SELECT 1 FROM dbo.CertificatePartners x WHERE x.CertificatesId = c.Id AND x.PartnersId = r.Id);
PRINT '  cert-partner links: ' + CAST(@@ROWCOUNT AS varchar(10));

-- 6. Product IDs. CR-015: Status derives from the parent certificate, it is not
--    carried in the source.
INSERT INTO dbo.CertificateProductIds
    (CertificateId, ProductIdType, ProductIdValue, Status, Partner, ProductName,
     IsActive, IsPublic, DisplayOrder, CreatedAt, UpdatedAt)
SELECT c.Id, d.IdType, d.IdValue,
       CASE WHEN c.Status IN ('Client Approved - Paid', 'Republished') THEN 'Active' ELSE 'Inactive' END,
       d.PartnerName, d.ProductName,
       1, 1, ROW_NUMBER() OVER (PARTITION BY c.Id ORDER BY d.Seq), @now, @now
FROM   #Pid d
JOIN   dbo.Certificates c ON c.Number = d.Number
WHERE  NOT EXISTS (SELECT 1 FROM dbo.CertificateProductIds x
                   WHERE x.CertificateId = c.Id AND x.ProductIdType = d.IdType AND x.ProductIdValue = d.IdValue);
PRINT '  product ids: ' + CAST(@@ROWCOUNT AS varchar(10));

-- 7. Per-product-ID partner links (CR-171 Model A). An UNTAGGED product ID is
--    treated as SHARED and returned to EVERY partner (DEF-352) — so a missing
--    link here is not "hidden", it is over-exposed.
INSERT INTO dbo.CertificateProductIdPartners (CertificateProductIdId, PartnersId)
SELECT DISTINCT pid.Id, r.Id
FROM   #Pid d
JOIN   dbo.Certificates c ON c.Number = d.Number
JOIN   dbo.CertificateProductIds pid
       ON pid.CertificateId = c.Id AND pid.ProductIdType = d.IdType AND pid.ProductIdValue = d.IdValue
JOIN   dbo.Partners r ON r.Name = d.PartnerName
WHERE  d.PartnerName IS NOT NULL
  AND  NOT EXISTS (SELECT 1 FROM dbo.CertificateProductIdPartners x
                   WHERE x.CertificateProductIdId = pid.Id AND x.PartnersId = r.Id);
PRINT '  product-id partner links: ' + CAST(@@ROWCOUNT AS varchar(10));

--------------------------------------------------------------- assertions ---
SET @errmsg = NULL;

-- CR-038: joining on Number is many-to-many now, so compare ROW COUNTS, not a join cardinality.
IF (SELECT COUNT(*) FROM dbo.Certificates c WHERE c.Number IN (SELECT Number FROM #Cert))
   <> (SELECT COUNT(*) FROM #Cert)
    SET @errmsg = 'certificate count does not match staging';
-- CR-038: the exemption must cover exactly the duplicated rows and nothing else.
IF (SELECT COUNT(*) FROM dbo.Certificates WHERE AllowsDuplicateNumber = 1)
   <> (SELECT COUNT(*) FROM #Cert s WHERE s.Number IN (SELECT Number FROM #Cert GROUP BY Number HAVING COUNT(*) > 1))
    SET @errmsg = 'AllowsDuplicateNumber was not set on exactly the duplicated rows';
IF EXISTS (SELECT 1 FROM dbo.Certificates WHERE Number IS NOT NULL AND NormalizedNumber IS NULL)
    SET @errmsg = 'NormalizedNumber is NULL — CR-187 computed column did not populate';
-- (removed) "a staging row matched more than one certificate" — that assertion assumed one
-- certificate per number. Under CR-038 several certificates legitimately share a number, so the
-- join is many-to-many by design and the check fired on correct data. The row-count assertion
-- above covers what it was actually protecting.
--
-- CR-038: every duplicated number must end up with ALL its records present.
IF EXISTS (
    SELECT s.Number FROM #Cert s GROUP BY s.Number
    HAVING COUNT(*) <> (SELECT COUNT(*) FROM dbo.Certificates c WHERE c.Number = s.Number))
    SET @errmsg = 'a certificate number has a different number of records than were staged';
IF (SELECT COUNT(*) FROM dbo.CertificateProductIds pid JOIN dbo.Certificates c ON c.Id = pid.CertificateId
    JOIN #Cert s ON s.Number = c.Number) <> (SELECT COUNT(*) FROM #Pid)
    SET @errmsg = 'product-id count does not match staging';

IF @errmsg IS NOT NULL
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR('LOAD ROLLED BACK: %s', 16, 1, @errmsg);
    RETURN;
END;

COMMIT TRANSACTION;

PRINT '';
PRINT '=================== POST-LOAD ===================';
SELECT 'Certificates' AS TableName, COUNT(*) AS Rows FROM dbo.Certificates
UNION ALL SELECT 'Companies', COUNT(*) FROM dbo.Companies
UNION ALL SELECT 'Products', COUNT(*) FROM dbo.Products
UNION ALL SELECT 'CertificateProductIds', COUNT(*) FROM dbo.CertificateProductIds
UNION ALL SELECT 'CertificateProductIdPartners', COUNT(*) FROM dbo.CertificateProductIdPartners
UNION ALL SELECT 'CertificatePartners', COUNT(*) FROM dbo.CertificatePartners
UNION ALL SELECT 'CertificateTag', COUNT(*) FROM dbo.CertificateTag;
SELECT Status, COUNT(*) AS Certs FROM dbo.Certificates GROUP BY Status ORDER BY COUNT(*) DESC;
PRINT 'LOAD COMPLETE. Now run the value-level reconciliation before accepting.';
""")

    path = os.path.join(outdir, "migration-load.sql")
    # ENCODING: UTF-16 LE with BOM, deliberately — NOT UTF-8.
    #
    # The first run of this generator emitted UTF-8 and sqlcmd decoded it as the
    # ANSI codepage, so every non-ASCII character was corrupted ON THE WAY IN:
    # "SARGENT EcoFlex®" (U+00AE) landed as "EcoFlex®" (U+00C2 U+00AE). 1,674
    # product names and 360 company names were mangled, and nothing failed — the
    # load reported complete success. Only the value-level reconciliation caught it.
    #
    # sqlcmd reliably auto-detects a UTF-16 LE BOM, which removes the need for
    # anyone to remember `-f 65001`. The script also carries its own canary (see
    # the encoding guard near the top) so a mis-decoded run aborts instead of
    # silently writing mojibake.
    with io.open(path, "w", encoding="utf-16-le", newline="\r\n") as fh:
        fh.write("﻿" + "\n".join(out))

    size_mb = os.path.getsize(path) / 1024 / 1024
    print(f"\nWrote {path}  ({size_mb:.1f} MB, UTF-16 LE + BOM)")
    print(f"Run: sqlcmd -S <server> -d <database> -I -i \"{path}\"")


if __name__ == "__main__":
    main()
