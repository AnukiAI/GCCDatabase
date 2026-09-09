# -*- coding: utf-8 -*-
"""
EXPORT AUDIT — every download surface, signed in and anonymous
==============================================================
Exercises every export the application offers and checks each one actually returns the file it
claims to, rather than an HTML error page or a login redirect at HTTP 200.

    python 2026-09-04-export-audit.py                # against gccwebapp.com
    BASE_URL=http://localhost:5101 python ...        # against a local run

WHY THIS EXISTS
---------------
The client reported that certificate exports list only one country per certificate. The source
already contains the CR-037 fix (`CertificateService.BuildCountryList`), so the open question is
whether it is DEPLOYED — a question no amount of reading the repository can answer
([[feedback_verify_deployed_asset_not_symptom]]). This downloads the real file from the real site
and looks at the real bytes.

While it is here it checks every other export too, because fixing one export and shipping three
broken ones is the standing failure mode ([[feedback_scoping_check_adjacent_endpoints]]).

WHAT COUNTS AS A PASS
---------------------
  * HTTP 200 AND a body that is actually the claimed type — CSV starts with a header row, XLSX
    starts with the ZIP magic `PK`. An HTML body at 200 is a FAIL, never a pass: that is the shape
    a gate page or a login redirect takes (DEF-341).
  * Anonymous callers must NOT receive internal-only columns (Internal Tag, Pathway,
    Cert Subcategories) — DEF-128/DEF-295.
"""
from __future__ import annotations

import io
import os
import re
import sys
import csv as csvmod

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", errors="replace")

import requests

BASE = os.environ.get("BASE_URL", "https://gccwebapp.com").rstrip("/")
GATE = (os.environ.get("GATE_USER", "gcc"), os.environ.get("GATE_PASS", "Cert-Test-Access-2026!"))
USER = os.environ.get("LOGIN_USER", "admin.uat")
PASS = os.environ.get("LOGIN_PASS", "TestPass123!")
LOCAL = "localhost" in BASE

# A certificate known to carry three countries, for the CR-037 check.
MULTI = os.environ.get("MULTI_CERT", "26-0636")
MULTI_EXPECT = {"United States", "China", "Poland"}

fails, warns = [], []


def check(name, ok, detail=""):
    print(f"  {'PASS' if ok else 'FAIL'}  {name}" + (f"   {detail}" if detail else ""))
    if not ok:
        fails.append(name)


def warn(name, detail=""):
    print(f"  WARN  {name}" + (f"   {detail}" if detail else ""))
    warns.append(name)


def session(sign_in):
    s = requests.Session()
    if not LOCAL:
        s.auth = requests.auth.HTTPBasicAuth(*GATE)
    s.headers["User-Agent"] = "GCC-export-audit/1.0"
    if not sign_in:
        return s
    r = s.get(f"{BASE}/Account/Login", timeout=60)
    m = re.search(r'name="__RequestVerificationToken"[^>]*value="([^"]+)"', r.text)
    if not m:
        sys.exit("no antiforgery token on the login page")
    s.post(f"{BASE}/Account/Login", timeout=120, data={
        "Email": USER, "Password": PASS, "RememberMe": "false",
        "__RequestVerificationToken": m.group(1)})
    who = s.get(f"{BASE}/Dashboard", timeout=60)
    if "/Account/Login" in who.url:
        sys.exit(f"login FAILED as {USER}")
    return s


def kind(body: bytes) -> str:
    if body[:2] == b"PK":
        return "xlsx"
    if body[:4] == b"%PDF":
        return "pdf"
    head = body[:400].lstrip(b"\xef\xbb\xbf").lstrip()
    if head[:1] == b"<" or b"<!doctype" in head[:200].lower() or b"<html" in head[:200].lower():
        return "html"
    return "csv" if body else "empty"


def fetch(s, path, label, expect, required=True):
    """Returns (body, detected_kind) or (None, kind) on failure."""
    try:
        r = s.get(f"{BASE}{path}", timeout=300)
    except Exception as e:
        (check if required else warn)(f"{label}", False, f"{type(e).__name__}")
        return None, "error"
    k = kind(r.content)
    ok = r.status_code == 200 and k == expect
    detail = f"HTTP {r.status_code}, {len(r.content):,} bytes, looks like {k}"
    if required:
        check(f"{label}", ok, detail)
    elif not ok:
        warn(f"{label}", detail)
    else:
        print(f"  PASS  {label}   {detail}")
    return (r.content if ok else None), k


print(f"### EXPORT AUDIT   {BASE}\n")

# ---------------------------------------------------------------- signed in --
print(f"=== signed in as {USER} (Admin) ===")
adm = session(True)

cert_csv, _ = fetch(adm, "/Certificate/ExportCertificates?format=csv",
                    "Certificate export (CSV, all)", "csv")
fetch(adm, f"/Certificate/ExportCertificates?format=csv&searchTerm={MULTI}",
      "Certificate export (CSV, filtered by search)", "csv")
fetch(adm, "/Export/DownloadActiveProductIds", "Active product identifiers (CSV)", "csv")
# The `type` values are NOT free text — they are a closed switch in ExportController, and an
# unknown one returns 400 "Invalid stat type". An earlier run of this script invented values
# ("total", "certificates", "all") and reported four defects that did not exist. Every value
# below is transcribed from the switch statements, so a 400 here is a real finding.
STAT_TYPES = ["total-active-certs", "certs-expiring-this-month", "certs-expiring-next-month",
              "certs-expired-last-month", "certs-published-this-year", "certs-published-last-year",
              "certs-published-last-month", "companies-with-active-certs",
              "products-with-active-certs", "product-categories-with-active-certs",
              "product-types-with-active-certs", "product-subcategories-with-active-certs"]
TABLE_TYPES = ["cert-types", "cert-status", "top-companies", "product-categories", "product-types"]
REPORT_TYPES = ["active-certificates", "expiring-30-days", "expiring-3-months"]

for t in STAT_TYPES:
    fetch(adm, f"/Export/DownloadStat?type={t}&format=csv", f"Stat CSV: {t}", "csv")
for t in STAT_TYPES[:3]:
    fetch(adm, f"/Export/DownloadStat?type={t}&format=excel", f"Stat Excel: {t}", "xlsx")
for t in TABLE_TYPES:
    fetch(adm, f"/Export/DownloadTable?type={t}&format=csv", f"Table CSV: {t}", "csv")
for t in TABLE_TYPES[:2]:
    fetch(adm, f"/Export/DownloadTable?type={t}&format=excel", f"Table Excel: {t}", "xlsx")
for t in REPORT_TYPES:
    fetch(adm, f"/Export/DownloadReport?type={t}&format=excel", f"Report Excel: {t}", "xlsx")
    fetch(adm, f"/Export/DownloadReport?type={t}&format=csv", f"Report CSV: {t}", "csv")
fetch(adm, "/CertificateImport/DownloadTemplate", "Certificate import template (CSV)", "csv", required=False)

# ------------------------------------------------- the CR-037 country check --
print("\n=== CR-037: does the export list EVERY country? ===")
if not cert_csv:
    check("multi-country certificate present in the export", False, "no CSV to inspect")
else:
    text = cert_csv.decode("utf-8-sig", errors="replace")
    rows = list(csvmod.reader(io.StringIO(text)))
    header = rows[0] if rows else []
    try:
        ci = header.index("Country")
    except ValueError:
        ci = -1
    check("export has a Country column", ci >= 0, f"header: {header[:6]}…")
    numcol = 0  # Certificate Number is the first column
    hit = [r for r in rows[1:] if len(r) > max(ci, 0) and r[numcol].strip() == MULTI]
    check(f"{MULTI} is present in the export", bool(hit), f"{len(rows)-1} data rows")
    if hit and ci >= 0:
        cell = hit[0][ci].strip()
        got = {p.strip() for p in cell.split(",") if p.strip()}
        check(f"{MULTI} exports ALL its countries",
              got == MULTI_EXPECT,
              f"cell={cell!r} expected={sorted(MULTI_EXPECT)}")
        # The separator has to be one CountryList.ParseMany splits on, or the file cannot be
        # re-imported. ParseMany splits ',' and '|' — NOT ';'.
        if ";" in cell:
            check(f"{MULTI} country separator round-trips through the importer", False,
                  f"cell uses ';' which ParseMany does not split: {cell!r}")

# ------------------------------------------------------------- role gating --
print("\n=== anonymous caller must not receive internal-only columns ===")
anon = session(False)
anon_csv, k = fetch(anon, "/Certificate/ExportCertificates?format=csv",
                    "Certificate export (CSV, anonymous)", "csv")
if anon_csv:
    ahdr = next(csvmod.reader(io.StringIO(anon_csv.decode("utf-8-sig", errors="replace"))), [])
    for col in ("Internal Tag", "Internal Tags", "Pathway", "Cert Subcategories"):
        check(f"anonymous export omits '{col}' (DEF-128/295)", col not in ahdr)
    check("anonymous export still has Cert Results (QC #201)",
          any("Result" in h for h in ahdr), f"header: {ahdr}")

print("\n=== admin-only exports must refuse an anonymous caller ===")
for path, label in [("/Export/DownloadActiveProductIds", "Active product identifiers"),
                    ("/Export/DownloadStat?type=total&format=csv", "Dashboard stat")]:
    try:
        r = anon.get(f"{BASE}{path}", timeout=120, allow_redirects=False)
        refused = r.status_code in (301, 302, 401, 403) or "/Account/Login" in (r.headers.get("location") or "")
        check(f"{label} refuses anonymous", refused, f"HTTP {r.status_code}")
    except Exception as e:
        check(f"{label} refuses anonymous", False, str(e)[:60])

print("\n" + ("ALL CHECKS PASSED" if not fails else f"FAILURES ({len(fails)}): {fails}"))
if warns:
    print(f"warnings ({len(warns)}): {warns}")
sys.exit(1 if fails else 0)
