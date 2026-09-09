# -*- coding: utf-8 -*-
"""
BATCH 2 — PULL THE MISSING CERTIFICATE PDFs FROM THE CLIENT'S PRODUCTION SITE
============================================================================
The 2026-09-03 drop contained no PDFs, so all 193 loaded certificates have an empty
`CertificateFile`. 96 of them — exactly the publishable-status set — are downloadable
from the client's own public production database. This fetches those and reports what
it got. It does NOT upload anything; that is the -UPLOAD step.

    python 2026-09-03-batch2-pull-pdfs.py {dev|test}

Downloads to `_batch2_pdfs/` next to this script (git-ignored by size, not by rule —
do not commit it). Read-only against every database.

WHY DOWNLOAD RATHER THAN ASK THE CLIENT
---------------------------------------
These are the client's own published certificates, served anonymously from their own
system at db.greencirclecertified.com. Round-tripping a request for 96 files the client
has already published costs days and the drop was already late.

WHAT IS VERIFIED ON THE WAY IN
------------------------------
  * HTTP 200 and a non-empty body.
  * REAL `%PDF-` magic bytes, not an HTML error page with a .pdf name. A role-gated or
    rate-limited response is a 200-with-HTML, and a sweep that trusts the status code
    "finds" 96 perfect PDFs that are all login pages (feedback_role_gated_404_is_not_absence).
  * sha256 per file, so the upload step can prove the byte-for-byte round trip.
  * Size against the app's 10 MB limit (`CertificateController.cs:3637`, DEF-368) —
    anything larger CANNOT be uploaded through the API and is reported, not silently
    skipped.
  * That no two certificates were handed the same document.
"""
from __future__ import annotations

import csv
import hashlib
import io
import json
import os
import sys
import time
import urllib.request
import urllib.error
from collections import Counter, defaultdict

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", errors="replace")

import pyodbc

HERE = os.path.dirname(os.path.abspath(__file__))
OUTDIR = os.path.join(HERE, "_batch2_pdfs")
PROD_API = "https://db.greencirclecertified.com/api/certificates"
PROD_DL = "https://db.greencirclecertified.com/api/certificates/{id}/download"
MAX_UPLOAD = 10 * 1024 * 1024      # CertificateController.cs:3637

TARGETS = {
    "dev": ("Driver={ODBC Driver 17 for SQL Server};Server=ANUKI01;Database=GCCDatabase_Dev;"
            "Uid=GCC_DevUser;Pwd=DevP@ss2024!SQL;TrustServerCertificate=yes;Encrypt=no;"),
    "test": ("Driver={ODBC Driver 17 for SQL Server};Server=tcp:68.71.129.120,1533;"
             "Database=gccwebap_certificates;Uid=GCC_adminxyz123;Pwd=Q3Ez4e!gE_47KNi;"
             "Encrypt=yes;TrustServerCertificate=yes;"),
}
if len(sys.argv) < 2 or sys.argv[1] not in TARGETS:
    sys.exit("usage: 2026-09-03-batch2-pull-pdfs.py {dev|test}")
target = sys.argv[1]

# The certificates this load created, read from the build's own audit CSV so the two
# cannot drift.
OUTBASE = os.path.join(HERE, "_batch2_output")
auditdir = os.path.join(OUTBASE, sorted(os.listdir(OUTBASE))[-1])
with io.open(os.path.join(auditdir, "certs-loaded.csv"), encoding="utf-8-sig") as fh:
    loaded = [r["Certificate #"] for r in csv.DictReader(fh)]
print(f"### batch-2 certificates: {len(loaded)}   (audit set {os.path.basename(auditdir)})")

os.makedirs(OUTDIR, exist_ok=True)


def fetch(url, timeout=120):
    req = urllib.request.Request(url, headers={"User-Agent": "GCC-migration-tooling/1.0"})
    with urllib.request.urlopen(req, timeout=timeout) as r:
        return r.status, r.read(), dict(r.headers)


# ------------------------------------------------- which ones can we even get? --
print(f"\nfetching the production certificate index …")
status, body, _ = fetch(PROD_API)
prod = json.loads(body.decode("utf-8"))
prod_by_number = {c["certificateNumber"]: c for c in prod}
print(f"  production index: {len(prod)} published certificates")

cn = pyodbc.connect(TARGETS[target])
cur = cn.cursor()
inlist = ",".join("'" + n.replace("'", "''") + "'" for n in loaded)
cur.execute(f"""SELECT Number, Status, CertificateFile FROM Certificates WHERE Number IN ({inlist})""")
db_state = {r[0]: (r[1], r[2]) for r in cur.fetchall()}

# A certificate number that carries several records (CR-038) would make the upload API
# ambiguous — it resolves by number and takes the newest. Catch it here rather than
# discover it as a PDF on the wrong record.
cur.execute(f"""SELECT Number, COUNT(*) FROM Certificates WHERE Number IN ({inlist})
                GROUP BY Number HAVING COUNT(*) > 1""")
ambiguous = {r[0]: r[1] for r in cur.fetchall()}
if ambiguous:
    print(f"\n  !! {len(ambiguous)} number(s) carry several certificate records: {ambiguous}")
    print("     The upload API resolves by number and takes the newest — these are SKIPPED.")

targets = []
skipped = defaultdict(list)
for num in loaded:
    if num in ambiguous:
        skipped["number is not unique in the database"].append(num)
    elif num not in prod_by_number:
        skipped["not published on the client's production site"].append(num)
    elif db_state.get(num, (None, None))[1]:
        skipped["already has a PDF attached"].append(num)
    else:
        targets.append(num)

print(f"\ndownloadable : {len(targets)}")
for reason, nums in sorted(skipped.items()):
    print(f"skipped ({len(nums):>3}) : {reason}")

# ------------------------------------------------------------------ download --
print(f"\ndownloading to {OUTDIR}\n")
rows, failures = [], []
for i, num in enumerate(targets, 1):
    pid = prod_by_number[num]["id"]
    url = PROD_DL.format(id=pid)
    path = os.path.join(OUTDIR, f"{num}.pdf")
    try:
        st, data, hdrs = fetch(url)
    except urllib.error.HTTPError as e:
        failures.append((num, pid, f"HTTP {e.code}"))
        print(f"  [{i:>3}/{len(targets)}] {num}  FAILED  HTTP {e.code}")
        continue
    except Exception as e:
        failures.append((num, pid, type(e).__name__ + ": " + str(e)[:80]))
        print(f"  [{i:>3}/{len(targets)}] {num}  FAILED  {type(e).__name__}")
        continue

    # A 200 proves the server answered, not that it answered with a PDF.
    if not data[:5] == b"%PDF-":
        head = data[:60].decode("utf-8", "replace").replace("\n", " ")
        failures.append((num, pid, f"not a PDF — starts {head!r}"))
        print(f"  [{i:>3}/{len(targets)}] {num}  FAILED  not a PDF ({len(data)} bytes)")
        continue

    with open(path, "wb") as fh:
        fh.write(data)
    sha = hashlib.sha256(data).hexdigest()
    over = len(data) > MAX_UPLOAD
    rows.append({"number": num, "prod_id": pid, "bytes": len(data), "sha256": sha,
                 "path": path, "over_limit": "YES" if over else ""})
    flag = "  >10MB, CANNOT UPLOAD" if over else ""
    print(f"  [{i:>3}/{len(targets)}] {num}  {len(data)/1024/1024:6.2f} MB  {sha[:12]}{flag}")
    time.sleep(0.15)          # gentle on the client's public server

# ------------------------------------------------------------------ verdicts --
print("\n" + "=" * 74)
print(f"downloaded : {len(rows)}")
print(f"failed     : {len(failures)}")
for num, pid, why in failures:
    print(f"    {num} (prod id {pid}): {why}")

over = [r for r in rows if r["over_limit"]]
if over:
    print(f"\nover the API's 10 MB limit ({len(over)}) — these need a different route:")
    for r in over:
        print(f"    {r['number']}  {r['bytes']/1024/1024:.2f} MB")

# Two certificates sharing a document is the DEF-369 shape: it looks fine per row and is
# only visible when the set is compared with itself.
dupe = {h: [r["number"] for r in rows if r["sha256"] == h]
        for h, n in Counter(r["sha256"] for r in rows).items() if n > 1}
if dupe:
    print(f"\ncertificates sharing an identical document ({len(dupe)} group(s)):")
    for h, nums in dupe.items():
        print(f"    {h[:12]}  {nums}")
else:
    print("\nno two certificates were handed the same document.")

manifest = os.path.join(OUTDIR, "manifest.csv")
with io.open(manifest, "w", encoding="utf-8-sig", newline="") as fh:
    w = csv.writer(fh)
    w.writerow(["Certificate #", "Production id", "Bytes", "sha256", "Local path", "Over 10MB"])
    for r in rows:
        w.writerow([r["number"], r["prod_id"], r["bytes"], r["sha256"], r["path"], r["over_limit"]])
    for num, pid, why in failures:
        w.writerow([num, pid, "", "", "", "FAILED: " + why])
print(f"\nwrote {manifest}")
print(f"\nNext: python 2026-09-03-batch2-upload-pdfs.py {target}")
