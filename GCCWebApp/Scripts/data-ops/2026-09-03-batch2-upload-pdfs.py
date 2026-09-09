# -*- coding: utf-8 -*-
"""
BATCH 2 — ATTACH THE PULLED CERTIFICATE PDFs
============================================
Uploads the documents fetched by `2026-09-03-batch2-pull-pdfs.py` to the certificates loaded on
2026-09-03, through the application's own API:

    POST /api/certificates/{number}/upload-pdf        (CertificateController.cs:3624, AdminOnly)

    python 2026-09-03-batch2-upload-pdfs.py {dev|test}            # DRY RUN, uploads nothing
    python 2026-09-03-batch2-upload-pdfs.py {dev|test} --apply

WHY THE API AND NOT THE FILE SYSTEM
-----------------------------------
The August load placed files on the server by hand and wrote the two columns with SQL. That works,
but it reimplements four things this endpoint already does correctly:
  * resolves the physical path through `IUploadPathResolver` — DEF-286 exists because a second
    guess at where `App_Data` lives is how PDFs go missing;
  * names the file to the application's own convention;
  * renders the PNG preview — and passes the LOGICAL path when doing so, which is precisely the
    DEF-262 bug that once made this endpoint silently produce no thumbnail at all;
  * sets `CertificateFile` and `CertificateImagePath` together, in one transaction.
It also needs no Plesk access, which a hand-placed file does.

WHAT IT REFUSES TO DO
---------------------
It will not overwrite a certificate that already holds a document. A certificate PDF is client
evidence; replacing one silently is not something a migration script should be able to do.

VERIFICATION (after --apply)
----------------------------
Re-reads the database AND re-fetches documents over HTTP as a signed-in admin, asserting:
  * both columns are set for every certificate that reported success;
  * the served bytes start with `%PDF-` — a 200 proves the server answered, not what with, and a
    role-gated response is an HTML page at status 200
    ([[feedback_role_gated_404_is_not_absence]]);
  * the served sha256 equals what production served us, byte for byte;
  * previews are real PNGs;
  * no two certificates point at the same file or the same preview — the DEF-369 shape, which is
    correct in every single row and only visible when the set is compared with itself.
"""
from __future__ import annotations

import csv
import datetime
import hashlib
import io
import os
import re
import sys
from collections import defaultdict

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", errors="replace")

import requests
import pyodbc

HERE = os.path.dirname(os.path.abspath(__file__))
PDF_DIR = os.path.join(HERE, "_batch2_pdfs")
MANIFEST = os.path.join(PDF_DIR, "manifest.csv")

BASE = os.environ.get("BASE_URL", "https://gccwebapp.com").rstrip("/")
GATE = (os.environ.get("GATE_USER", "gcc"), os.environ.get("GATE_PASS", "Cert-Test-Access-2026!"))
# `admin` had its password reset on TEST on 2026-08-19 and only Shawn knows it. A failed login
# re-renders the login page as HTTP 200, which reads exactly like a broken API — use admin.uat.
USER = os.environ.get("LOGIN_USER", "admin.uat")
PASS = os.environ.get("LOGIN_PASS", "TestPass123!")

TARGETS = {
    "dev": ("Driver={ODBC Driver 17 for SQL Server};Server=ANUKI01;Database=GCCDatabase_Dev;"
            "Uid=GCC_DevUser;Pwd=DevP@ss2024!SQL;TrustServerCertificate=yes;Encrypt=no;"),
    "test": ("Driver={ODBC Driver 17 for SQL Server};Server=tcp:68.71.129.120,1533;"
             "Database=gccwebap_certificates;Uid=GCC_adminxyz123;Pwd=Q3Ez4e!gE_47KNi;"
             "Encrypt=yes;TrustServerCertificate=yes;"),
}
args = [a for a in sys.argv[1:] if not a.startswith("--")]
APPLY = "--apply" in sys.argv
# Re-verify an upload that already happened. Without this, a second --apply skips every
# certificate as "already holds a document" and the verification block then has nothing to
# check — so a failed CHECK could never be re-run after being fixed, only the upload could.
VERIFY_ONLY = "--verify-only" in sys.argv
if not args or args[0] not in TARGETS:
    sys.exit("usage: 2026-09-03-batch2-upload-pdfs.py {dev|test} [--apply | --verify-only]")
target = args[0]

fails = []


def check(name, ok, detail=""):
    print(f"  {'PASS' if ok else 'FAIL'}  {name}" + (f"   {detail}" if detail else ""))
    if not ok:
        fails.append(name)


# --------------------------------------------------------------------- manifest --
if not os.path.exists(MANIFEST):
    sys.exit(f"No manifest at {MANIFEST}. Run 2026-09-03-batch2-pull-pdfs.py first.")
with io.open(MANIFEST, encoding="utf-8-sig") as fh:
    rows = [r for r in csv.DictReader(fh) if r["sha256"]]      # skip FAILED rows from the pull

print(f"### batch-2 PDF attach   target={target.upper()}  {BASE}")
print(f"### {len(rows)} documents in the manifest")
_mode = ("VERIFY ONLY — re-checking an upload that already happened" if VERIFY_ONLY
         else "APPLY — documents will be uploaded" if APPLY
         else "DRY RUN — nothing will be uploaded")
print(f"### mode: {_mode}\n")

# ------------------------------------------------------------------- sign in --
s = requests.Session()
# The site gate accepts Basic as well as its cookie (TestEnvironmentGateMiddleware.cs:98). Basic
# is used because it is stateless: a stale gate COOKIE redirects to the gate page, which follows
# through to a 200 and would be graded as a successful upload (DEF-341).
s.auth = requests.auth.HTTPBasicAuth(*GATE) if "localhost" not in BASE else None
s.headers["User-Agent"] = "GCC-migration-tooling/1.0"

r = s.get(f"{BASE}/Account/Login", timeout=60)
m = re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"', r.text)
if not m:
    sys.exit("Could not find an antiforgery token on the login page.")
# `Email` accepts a username OR an email (AccountController.cs:1159); posting `Username` silently
# re-renders the login page at HTTP 200. `RememberMe` must be present — the form posts a hidden
# `false` alongside the checkbox, and omitting it fails model validation the same silent way.
r = s.post(f"{BASE}/Account/Login", timeout=120, data={
    "Email": USER, "Password": PASS, "RememberMe": "false",
    "__RequestVerificationToken": m.group(1)})
who = s.get(f"{BASE}/Dashboard", timeout=60, allow_redirects=True)
signed_in = "/Account/Login" not in who.url and who.status_code == 200
print(f"login: {'OK as ' + USER if signed_in else 'FAILED — bounced to ' + who.url}\n")
if not signed_in:
    sys.exit(1)

# ---------------------------------------------------------------- current state --
cn = pyodbc.connect(TARGETS[target])
cur = cn.cursor()
numbers = [r["Certificate #"] for r in rows]
inlist = ",".join("'" + n.replace("'", "''") + "'" for n in numbers)
cur.execute(f"SELECT Number, CertificateFile, CertificateImagePath FROM Certificates "
            f"WHERE Number IN ({inlist})")
before = {r[0]: (r[1], r[2]) for r in cur.fetchall()}

todo, skipped = [], []
for r in rows:
    n = r["Certificate #"]
    if r.get("Over 10MB"):
        skipped.append((n, "over the API's 10MB limit"))
    elif n not in before:
        skipped.append((n, "certificate not in this database"))
    elif before[n][0] and not VERIFY_ONLY:
        skipped.append((n, f"already holds a document ({before[n][0]})"))
    else:
        todo.append(r)

if VERIFY_ONLY:
    # Rebuild the "uploaded" set from what is already in the database, then fall through to the
    # verification block unchanged — so the checks are the same checks, not a second copy of them.
    uploaded = [{**r, "filePath": before[r["Certificate #"]][0],
                 "thumbnailPath": before[r["Certificate #"]][1]}
                for r in rows if r["Certificate #"] in before and before[r["Certificate #"]][0]]
    failed = []
    print(f"VERIFY ONLY — re-checking {len(uploaded)} certificate(s) already holding a document.\n")

if skipped and not VERIFY_ONLY:
    print(f"skipping {len(skipped)}:")
    for n, why in skipped:
        print(f"   {n}  {why}")
    print()

if not APPLY and not VERIFY_ONLY:
    print(f"DRY RUN — would upload {len(todo)} document(s):")
    for r in todo[:8]:
        print(f"   {r['Certificate #']}  {int(r['Bytes'])/1024/1024:.2f} MB  {r['sha256'][:12]}")
    if len(todo) > 8:
        print(f"   ... and {len(todo)-8} more")
    print("\nRe-run with --apply to upload.")
    sys.exit(0)

# --------------------------------------------------------------------- upload --
if not VERIFY_ONLY:
    uploaded, failed = [], []
for i, r in enumerate([] if VERIFY_ONLY else todo, 1):
    n = r["Certificate #"]
    with open(r["Local path"], "rb") as fh:
        blob = fh.read()
    try:
        resp = s.post(f"{BASE}/api/certificates/{n}/upload-pdf", timeout=180,
                      headers={"X-Requested-With": "XMLHttpRequest"},
                      files={"pdfFile": (f"{n}.pdf", blob, "application/pdf")})
    except Exception as e:
        failed.append((n, f"{type(e).__name__}: {e}"))
        print(f"  [{i:>3}/{len(todo)}] {n}  FAILED  {type(e).__name__}")
        continue
    try:
        j = resp.json()
    except Exception:
        # An HTML body here means the request was answered by something other than the API —
        # the gate, or a login redirect. Never grade that as a success.
        j = {"success": False, "message": f"non-JSON body: {resp.text[:120]!r}"}
    if resp.status_code != 200 or not j.get("success"):
        failed.append((n, f"HTTP {resp.status_code} {j.get('message','')}"))
        print(f"  [{i:>3}/{len(todo)}] {n}  FAILED  HTTP {resp.status_code}  {j.get('message','')}")
        continue
    uploaded.append({**r, "filePath": j.get("filePath"), "thumbnailPath": j.get("thumbnailPath")})
    print(f"  [{i:>3}/{len(todo)}] {n}  {j.get('filePath')}"
          + ("" if j.get("thumbnailPath") else "   (NO PREVIEW)"))

if not VERIFY_ONLY:
    print(f"\nuploaded: {len(uploaded)}   failed: {len(failed)}")
    for n, why in failed:
        print(f"   {n}: {why}")

# --------------------------------------------------------------------- verify --
print("\n=== verification ===")
cur.execute(f"SELECT Number, CertificateFile, CertificateImagePath FROM Certificates "
            f"WHERE Number IN ({inlist})")
after = {r[0]: (r[1], r[2]) for r in cur.fetchall()}

missing_file = [u["Certificate #"] for u in uploaded if not after.get(u["Certificate #"], (None,))[0]]
check("every uploaded certificate has CertificateFile set in the database",
      not missing_file, f"{len(missing_file)} without {missing_file[:5]}")
missing_thumb = [u["Certificate #"] for u in uploaded if not after.get(u["Certificate #"], (None, None))[1]]
check("every uploaded certificate has a preview image",
      not missing_thumb, f"{len(missing_thumb)} without {missing_thumb[:5]}")

# Compared across the WHOLE table, not just this batch: sharing a path with a certificate loaded
# in August is exactly as wrong as sharing with one loaded today.
cur.execute("""SELECT CertificateFile, COUNT(*) FROM Certificates
               WHERE CertificateFile IS NOT NULL GROUP BY CertificateFile HAVING COUNT(*) > 1""")
shared_file = cur.fetchall()
cur.execute("""SELECT CertificateImagePath, COUNT(*) FROM Certificates
               WHERE CertificateImagePath IS NOT NULL GROUP BY CertificateImagePath HAVING COUNT(*) > 1""")
shared_thumb = cur.fetchall()
check("no two certificates in the whole table share a stored document",
      not shared_file, f"{len(shared_file)} shared {[r[0] for r in shared_file[:3]]}")
check("no two certificates in the whole table share a preview",
      not shared_thumb, f"{len(shared_thumb)} shared {[r[0] for r in shared_thumb[:3]]}")

# Round-trip a spread of them over HTTP.
#
# NOTE THE KEY. Almost every certificate route is keyed by certificate NUMBER, but these two
# asset routes are the exception: `ViewCertificateFile(int id)` / `ViewCertificateImage(int id)`
# (CertificateController.cs:2332 / :2416). Passing the number gets a 404 — indistinguishable
# from "the file is not being served", which is exactly how this check first reported 0/12
# against a load that was in fact perfect.
cur.execute(f"SELECT Number, Id FROM Certificates WHERE Number IN ({inlist})")
ids = {r[0]: r[1] for r in cur.fetchall()}
step = max(1, len(uploaded) // 12)
sample = [u for u in uploaded[::step][:12] if u["Certificate #"] in ids]
served = magic = identical = png = 0
for u in sample:
    cid = ids[u["Certificate #"]]
    r1 = s.get(f"{BASE}/Certificate/ViewCertificateFile/{cid}", timeout=120)
    if r1.status_code == 200:
        served += 1
        if r1.content[:5] == b"%PDF-":
            magic += 1
        if hashlib.sha256(r1.content).hexdigest() == u["sha256"]:
            identical += 1
    r2 = s.get(f"{BASE}/Certificate/ViewCertificateImage/{cid}", timeout=120)
    if r2.status_code == 200 and r2.content[:8] == b"\x89PNG\r\n\x1a\n":
        png += 1
check(f"sampled documents are served over HTTP", served == len(sample), f"{served}/{len(sample)}")
check("sampled documents are real PDFs, not an error page at status 200",
      magic == len(sample), f"{magic}/{len(sample)}")
check("sampled documents are byte-identical to what production served",
      identical == len(sample), f"{identical}/{len(sample)}")
check("sampled previews are real PNGs", png == len(sample), f"{png}/{len(sample)}")

# ----------------------------------------------------------------------- undo --
if VERIFY_ONLY:
    # No undo file on a verify-only run: it uploaded nothing, and writing a second undo would
    # overwrite the real one's timestamp ordering with a file that reverses somebody else's work.
    print("\n" + ("ALL CHECKS PASSED" if not fails else "FAILURES: " + str(fails)))
    sys.exit(1 if fails else 0)
stamp = datetime.datetime.now().strftime("%Y%m%d-%H%M%S")
undo = os.path.join(PDF_DIR, f"undo-{stamp}.sql")
with io.open(undo, "w", encoding="utf-16-le", newline="\r\n") as fh:
    fh.write("﻿")
    fh.write("/* Reverses 2026-09-03-batch2-upload-pdfs.py — clears the two columns it set.\n"
             "   The FILES are left on the server: they are harmless once unreferenced, and\n"
             "   deleting them needs Plesk access this did not use.\n"
             "   RUN WITH: sqlcmd -S <server> -d <database> -I -b -i this-file.sql */\n")
    fh.write("SET NOCOUNT ON;\nSET XACT_ABORT ON;\nBEGIN TRANSACTION;\n")
    for u in uploaded:
        fh.write(f"UPDATE dbo.Certificates SET CertificateFile = NULL, CertificateImagePath = NULL, "
                 f"UpdatedAt = SYSUTCDATETIME() WHERE Number = N'{u['Certificate #']}';\n")
    fh.write("COMMIT TRANSACTION;\nPRINT 'reverted';\n")
print(f"\nundo: {undo}")

print(f"\n{'ALL CHECKS PASSED' if not fails else 'FAILURES: ' + str(fails)}")
sys.exit(1 if fails else 0)
