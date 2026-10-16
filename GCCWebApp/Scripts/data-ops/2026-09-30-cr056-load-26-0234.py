# -*- coding: utf-8 -*-
"""
CR-056 | add the missing certificate 26-0234 (Sargent / ASSA ABLOY, 281 Series Closer)
=====================================================================================
The renewal of 25-8076, missed by the migration (see CR-056 in change-request-register.md).
The PDF is the record: Certification Period Aug 1 2026 - Jul 31 2029.

    python 2026-09-30-cr056-load-26-0234.py            # DRY RUN - preflight only, writes nothing
    python 2026-09-30-cr056-load-26-0234.py --apply    # insert + attach PDF + verify
    python 2026-09-30-cr056-load-26-0234.py --verify-only

1. INSERT in one transaction, guarded: aborts if 26-0234 already exists. Company, product,
   type, category and location are copied from 25-8076 (Id 4613); number/status/dates from the PDF.
2. PDF attached through the app's own POST /api/certificates/26-0234/upload-pdf (same path as
   batch 2), so path resolution, preview rendering and both columns are done by the app.
3. Verify: DB row + joins, anonymous detail page, served PDF sha256 == source sha256.

Rollback: 2026-09-30-cr056-ROLLBACK.sql
"""
from __future__ import annotations

import hashlib
import io
import os
import re
import sys

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", errors="replace")

import pyodbc
import requests

NUMBER = "26-0234"
TEMPLATE_ID = 4613                      # 25-8076, the cert this one renews
EFFECTIVE, EXPIRATION = "2026-08-01", "2029-07-31"
STATUS = "Client Approved - Paid"
PDF = r"C:\Users\swest\GCCPortalPull\certificates\26-0234_SARGENT_Manufacturing_Company,_an_ASSA_ABLOY_Group_5718.pdf"
PDF_SHA = "4c52b5793981af89fc342610ec6108f57b260d865784e92ea65cdc8645fa48c8"

BASE = os.environ.get("BASE_URL", "https://db.greencirclecertified.com").rstrip("/")
USER = os.environ.get("LOGIN_USER", "admin.uat")
PASS = os.environ.get("LOGIN_PASS", "TestPass123!")
CONN = ("Driver={ODBC Driver 17 for SQL Server};Server=tcp:68.71.129.120,1533;"
        "Database=gccwebap_certificates;Uid=GCC_adminxyz123;Pwd=Q3Ez4e!gE_47KNi;"
        "Encrypt=yes;TrustServerCertificate=yes;")

APPLY = "--apply" in sys.argv
VERIFY_ONLY = "--verify-only" in sys.argv
fails: list[str] = []


def check(name, ok, detail=""):
    print(f"  {'PASS' if ok else 'FAIL'}  {name}" + (f"   {detail}" if detail else ""))
    if not ok:
        fails.append(name)


cn = pyodbc.connect(CONN, autocommit=False)
cur = cn.cursor()
db = cur.execute("SELECT DB_NAME()").fetchone()[0]
print(f"### CR-056  {NUMBER}   db={db}   site={BASE}")
print(f"### mode: {'VERIFY ONLY' if VERIFY_ONLY else 'APPLY' if APPLY else 'DRY RUN - nothing written'}\n")

# ------------------------------------------------------------------ preflight --
print("=== preflight ===")
check("database is gccwebap_certificates", db == "gccwebap_certificates", db)
blob = open(PDF, "rb").read()
check("source PDF sha256 matches the portal pull", hashlib.sha256(blob).hexdigest() == PDF_SHA,
      f"{len(blob):,} bytes")
tpl = cur.execute("SELECT Number, CompanyId, ProductId, CertificationTypeId FROM Certificates WHERE Id=?",
                  TEMPLATE_ID).fetchone()
check("template row is 25-8076 / Sargent / 281 Series Closer",
      tpl is not None and tuple(tpl) == ("25-8076", 242, 137, 6), str(tpl and tuple(tpl)))
existing = cur.execute("SELECT COUNT(*) FROM Certificates WHERE Number=?", NUMBER).fetchone()[0]
if not VERIFY_ONLY:
    check(f"{NUMBER} not yet in the database", existing == 0, f"found {existing}")
total_before = cur.execute("SELECT COUNT(*) FROM Certificates").fetchone()[0]
print(f"  certificates before: {total_before:,}")

if fails:
    sys.exit(f"\nPREFLIGHT FAILED: {fails}")
if not APPLY and not VERIFY_ONLY:
    print("\nDRY RUN OK. Re-run with --apply.")
    sys.exit(0)

# --------------------------------------------------------------------- insert --
if APPLY:
    print("\n=== insert ===")
    cur.execute("""
SET XACT_ABORT ON;
IF EXISTS (SELECT 1 FROM Certificates WHERE Number = ?) THROW 50001, 'CR-056: 26-0234 already exists - aborting', 1;
DECLARE @new TABLE (Id int);
INSERT INTO Certificates (Number, Status, EffectiveDate, ExpirationDate, Location, Country,
    Partner, CompanyId, ProductId, CertificationTypeId, CreatedBy, UpdatedBy, CreatedAt, UpdatedAt,
    ProductCategoryId, ProductSubcategoryId, ProductTypeId, AllowsDuplicateNumber)
OUTPUT INSERTED.Id INTO @new
SELECT ?, ?, ?, ?, Location, Country,
    Partner, CompanyId, ProductId, CertificationTypeId, NULL, NULL, GETUTCDATE(), GETUTCDATE(),
    ProductCategoryId, ProductSubcategoryId, ProductTypeId, 0
FROM Certificates WHERE Id = ?;   -- Normalized* columns are computed
IF @@ROWCOUNT <> 1 THROW 50002, 'CR-056: template row not found', 1;
INSERT INTO CertificateCountries (CertificateId, Country)
    SELECT n.Id, c.Country FROM @new n CROSS JOIN CertificateCountries c WHERE c.CertificateId = ?;
INSERT INTO CertificateTag (CertificatesId, TagsId)
    SELECT n.Id, t.TagsId FROM @new n CROSS JOIN CertificateTag t WHERE t.CertificatesId = ?;
SELECT Id FROM @new;
""", NUMBER, NUMBER, STATUS, EFFECTIVE, EXPIRATION, TEMPLATE_ID, TEMPLATE_ID, TEMPLATE_ID)
    while cur.description is None:
        cur.nextset()
    new_id = cur.fetchone()[0]
    cn.commit()
    print(f"  inserted {NUMBER} as Id {new_id} (committed)")

new_id = cur.execute("SELECT Id FROM Certificates WHERE Number=?", NUMBER).fetchone()[0]

# --------------------------------------------------------------------- upload --
s = requests.Session()
s.headers["User-Agent"] = "GCC-migration-tooling/1.0"
if APPLY:
    print("\n=== attach PDF ===")
    r = s.get(f"{BASE}/Account/Login", timeout=60)
    m = re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"', r.text)
    if not m:
        sys.exit("No antiforgery token on the login page - PDF NOT attached (row is inserted).")
    s.post(f"{BASE}/Account/Login", timeout=120, data={
        "Email": USER, "Password": PASS, "RememberMe": "false", "__RequestVerificationToken": m.group(1)})
    who = s.get(f"{BASE}/Dashboard", timeout=60)
    if "/Account/Login" in who.url or who.status_code != 200:
        sys.exit(f"Login FAILED as {USER} - PDF NOT attached (row is inserted).")
    resp = s.post(f"{BASE}/api/certificates/{NUMBER}/upload-pdf", timeout=180,
                  headers={"X-Requested-With": "XMLHttpRequest"},
                  files={"pdfFile": (f"{NUMBER}.pdf", blob, "application/pdf")})
    try:
        j = resp.json()
    except Exception:
        j = {"success": False, "message": f"non-JSON body: {resp.text[:120]!r}"}
    check("upload-pdf returned success", resp.status_code == 200 and j.get("success"),
          f"HTTP {resp.status_code} {j.get('filePath') or j.get('message')}")
    check("preview thumbnail rendered", bool(j.get("thumbnailPath")), str(j.get("thumbnailPath")))

# --------------------------------------------------------------------- verify --
print("\n=== verify ===")
cn.rollback()   # fresh snapshot
row = cur.execute("""SELECT Number, Status, CONVERT(char(10),EffectiveDate,23), CONVERT(char(10),ExpirationDate,23),
    CompanyId, ProductId, CertificationTypeId, Location, Country, CertificateFile, CertificateImagePath
    FROM Certificates WHERE Id=?""", new_id).fetchone()
check("exactly one 26-0234", cur.execute("SELECT COUNT(*) FROM Certificates WHERE Number=?", NUMBER).fetchone()[0] == 1)
check("status / dates", tuple(row[1:4]) == (STATUS, EFFECTIVE, EXPIRATION), str(tuple(row[1:4])))
check("company / product / type", tuple(row[4:7]) == (242, 137, 6), str(tuple(row[4:7])))
check("location", tuple(row[7:9]) == ("Monroe, NC", "United States"), str(tuple(row[7:9])))
check("CertificateFile + preview set", bool(row[9]) and bool(row[10]), f"{row[9]} | {row[10]}")
check("country row", cur.execute("SELECT COUNT(*) FROM CertificateCountries WHERE CertificateId=?", new_id).fetchone()[0] == 1)
check("LEED tag", cur.execute("SELECT COUNT(*) FROM CertificateTag WHERE CertificatesId=? AND TagsId=2", new_id).fetchone()[0] == 1)
check("25-8076 untouched", tuple(cur.execute(
    "SELECT Status, CONVERT(char(10),ExpirationDate,23) FROM Certificates WHERE Id=?", TEMPLATE_ID).fetchone())
    == ("Expired", "2026-07-31"))
total_after = cur.execute("SELECT COUNT(*) FROM Certificates").fetchone()[0]
print(f"  certificates after: {total_after:,}")

anon = requests.Session()
anon.headers["User-Agent"] = "GCC-migration-tooling/1.0"
page = anon.get(f"{BASE}/Certificate/CertificateSingleView/{NUMBER}", timeout=60)
check("public detail page (anonymous) shows 26-0234",
      page.status_code == 200 and NUMBER in page.text and "/Account/Login" not in page.url,
      f"HTTP {page.status_code} {page.url}")
dl = anon.get(f"{BASE}/Certificate/Download/{NUMBER}", params={"cid": new_id}, timeout=120)
got = hashlib.sha256(dl.content).hexdigest()
check("served PDF is byte-identical to source", dl.status_code == 200 and got == PDF_SHA,
      f"HTTP {dl.status_code} {len(dl.content):,} bytes sha {got[:12]}")

print(f"\n{'ALL CHECKS PASSED' if not fails else 'FAILED: ' + ', '.join(fails)}   (Id {new_id})")
sys.exit(1 if fails else 0)
