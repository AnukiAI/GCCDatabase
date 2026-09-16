"""Cross-surface check: the product IDs a partner RECEIVES must equal the ones the app SHOWS.

WHY THIS EXISTS (DEF-426)
The partner API was checked against the database. The web UI was checked against the database.
Nobody ever compared the two with each other - so two different definitions of "which product IDs
count" coexisted for months:

    partner API : .Where(pi => pi.Status == "Active")        <- ignored IsActive
    web UI      : .Where(pid => ... && pid.IsActive)         <- honoured it

68 ASINs deactivated in the application on 2026-09-08 kept going to Amazon labelled "Active" for
eight days. Both surfaces looked correct against the database. Only comparing them to each other
could reveal it.

The deeper reason it survived: our reconciliation derived "expected" ASINs using the API's own
predicate, so it agreed with the API by construction. A verification written from the implementation
can only confirm that the code does what the code does. This script deliberately takes its
expectation from a DIFFERENT surface rather than from the database.

WHAT IT COMPARES
For every certificate the partner API delivers, the set of product-ID values must be identical to the
set the internal tile API shows for that certificate. Certificates the UI lists but the partner does
not receive are EXPECTED (status / effective-date scoping) and are reported as context, not failure.

SECOND DIVERGENCE THIS ALSO CATCHES
The UI falls back to the legacy `Certificate.Asin` column when a certificate has no ProductIds rows
(`CertificateTileInternalApiController.cs:399`); the partner API never reads that column. Any such
certificate surfaces here as UI-only values.

USAGE
    python Scripts/smoke-prod/cross-surface-productids.py test
    python Scripts/smoke-prod/cross-surface-productids.py dev --type ASIN

EXIT  0 = every delivered certificate's product IDs match across both surfaces
      1 = a mismatch, i.e. the partner sees something different from the application
"""
import os
import re
import sys
import json
import time
import urllib.request
import urllib.parse
import urllib.error
import http.cookiejar

TARGETS = {
    "test": {"base": "https://gccwebapp.com", "admin": ("admin.uat", "TestPass123!")},
    "dev": {"base": "http://localhost:5101", "admin": ("admin", "TestPass123!")},
}
TARGET = sys.argv[1] if len(sys.argv) > 1 and sys.argv[1] in TARGETS else "dev"
CFG = TARGETS[TARGET]
BASE = CFG["base"]
UA = "cross-surface-productids"
GATE_PASSWORD = os.environ.get("GATE_PASS", "Cert-Test-Access-2026!")

ID_TYPE = None
if "--type" in sys.argv:
    ID_TYPE = sys.argv[sys.argv.index("--type") + 1].upper()

print("### CROSS-SURFACE PRODUCT IDs   target=%s (%s)%s\n"
      % (TARGET.upper(), BASE, ("   type=%s" % ID_TYPE) if ID_TYPE else ""))


def get(op, path, headers=None):
    h = {"User-Agent": UA}
    if headers:
        h.update(headers)
    try:
        r = op.open(urllib.request.Request(BASE + path, headers=h), timeout=300)
        return r.getcode(), r.read().decode("utf-8", "replace")
    except urllib.error.HTTPError as e:
        return e.code, e.read().decode("utf-8", "replace")


def pass_gate(op):
    """TestEnvironmentGate is cookie-based; without it every path answers 200 with a gate page and
    JSON parsing fails, which reads as an API fault rather than a gate fault (DEF-341)."""
    code, html = get(op, "/__gate?returnUrl=%2F")
    if code != 200 or "formToken" not in html:
        return
    m = re.search('name="formToken"[^>]*value="([^"]+)"', html)
    if not m:
        return
    data = urllib.parse.urlencode(
        {"password": GATE_PASSWORD, "returnUrl": "/", "formToken": m.group(1)}).encode()
    try:
        op.open(urllib.request.Request(
            BASE + "/__gate", data=data,
            headers={"Content-Type": "application/x-www-form-urlencoded", "User-Agent": UA}),
            timeout=300)
    except urllib.error.HTTPError:
        pass


def session():
    cj = http.cookiejar.CookieJar()
    op = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(cj))
    pass_gate(op)
    return op


def post(op, path, fields):
    try:
        r = op.open(urllib.request.Request(
            BASE + path, data=urllib.parse.urlencode(fields).encode(),
            headers={"Content-Type": "application/x-www-form-urlencoded", "User-Agent": UA}),
            timeout=300)
        return r.getcode(), r.read().decode("utf-8", "replace")
    except urllib.error.HTTPError as e:
        return e.code, e.read().decode("utf-8", "replace")


def post_json(op, path, obj):
    try:
        r = op.open(urllib.request.Request(
            BASE + path, data=json.dumps(obj).encode(),
            headers={"Content-Type": "application/json", "Accept": "application/json",
                     "X-Requested-With": "XMLHttpRequest", "User-Agent": UA}), timeout=300)
        return r.getcode(), r.read().decode("utf-8", "replace")
    except urllib.error.HTTPError as e:
        return e.code, e.read().decode("utf-8", "replace")


def api_get(op, key, path):
    """Retries 429: paging generates many calls and the partner limiter is doing its job."""
    code, body = get(op, path, {"X-API-Key": key, "Accept": "application/json"})
    tries = 0
    while code == 429 and tries < 12:
        tries += 1
        time.sleep(10)
        code, body = get(op, path, {"X-API-Key": key, "Accept": "application/json"})
    return code, body


# --------------------------------------------------------------------- auth --
print("[1] admin sign-in (%s)" % CFG["admin"][0])
admin = session()
code, html = get(admin, "/Account/Login")
m = re.search('name="__RequestVerificationToken"[^>]*value="([^"]+)"', html)
if not m:
    sys.exit("FATAL: no antiforgery token - is %s up?" % BASE)
post(admin, "/Account/Login",
     {"Email": CFG["admin"][0], "Password": CFG["admin"][1],
      "__RequestVerificationToken": m.group(1)})
code, body = get(admin, "/api/admin/partner/list",
                 {"Accept": "application/json", "X-Requested-With": "XMLHttpRequest"})
if code != 200:
    sys.exit("FATAL: admin sign-in failed (/api/admin/partner/list -> %s)" % code)
amazon = next((p for p in (json.loads(body).get("data") or []) if p.get("name") == "Amazon"), None)
if not amazon:
    sys.exit("FATAL: no partner named 'Amazon' on this environment")

print("[2] mint a partner key")
code, body = post_json(admin, "/api/admin/partner/%s/apikey" % amazon["id"],
                       {"description": "cross-surface-productids"})
kd = json.loads(body).get("data") or {}
KEY, KEY_ID = kd.get("apiKey"), kd.get("id")
if not KEY:
    sys.exit("FATAL: could not mint a key: %s" % body[:200])
print("    key id %s" % KEY_ID)

# ------------------------------------------------------------- surface 1: API --
print("\n[3] PARTNER API - what Amazon receives")
partner = session()          # deliberately NOT signed in: an app cookie makes the middleware stand aside
api = {}
page = 1
while True:
    path = "/api/v1/certificates?includeProductIds=true&pageSize=100&pageNumber=%d" % page
    code, body = api_get(partner, KEY, path)
    if code != 200:
        sys.exit("FATAL: partner API page %d -> HTTP %s\n%s" % (page, code, body[:300]))
    env = json.loads(body)
    for c in (env.get("data") or []):
        vals = set()
        for p in (c.get("productIds") or []):
            if ID_TYPE is None or (p.get("type") or "").upper() == ID_TYPE:
                v = (p.get("value") or "").strip()
                if v:
                    vals.add(v)
        api.setdefault(c["certificateNumber"], set()).update(vals)
    pg = env.get("pagination") or {}
    if not pg.get("hasNext"):
        break
    page += 1
print("    %d certificates, %d product-ID values" % (len(api), sum(len(v) for v in api.values())))

# -------------------------------------------------------------- surface 2: UI --
print("\n[4] WEB UI (internal tile API) - what the application shows")
ui = {}
page = 1
while True:
    code, body = get(admin,
                     "/api/certificate-tiles-internal?partner=Amazon&page=%d&pageSize=100" % page,
                     {"Accept": "application/json", "X-Requested-With": "XMLHttpRequest"})
    if code != 200:
        sys.exit("FATAL: tile API page %d -> HTTP %s\n%s" % (page, code, body[:300]))
    env = json.loads(body)
    for c in (env.get("certificates") or []):
        num = c.get("certificateNumber") or c.get("CertificateNumber")
        if not num:
            continue
        # The UI renders each id type as one comma-joined cell.
        if ID_TYPE in (None, "ASIN"):
            cell = c.get("asin") or c.get("Asin") or ""
        else:
            cell = c.get(ID_TYPE.lower()) or c.get(ID_TYPE.capitalize()) or ""
        vals = set(v.strip() for v in cell.split(",") if v.strip())
        ui.setdefault(num, set()).update(vals)
    if not env.get("hasMore"):
        break
    page += 1
print("    %d certificates, %d product-ID values" % (len(ui), sum(len(v) for v in ui.values())))

# A key is a credential even on a pre-launch environment; revoke what this run created.
try:
    admin.open(urllib.request.Request(
        BASE + "/api/admin/partner/apikey/%s" % KEY_ID,
        headers={"Accept": "application/json", "X-Requested-With": "XMLHttpRequest",
                 "User-Agent": UA}, method="DELETE"), timeout=60)
    print("\n    key %s revoked" % KEY_ID)
except urllib.error.HTTPError as e:
    print("\n    !! could not revoke key %s (HTTP %s) - revoke it by hand" % (KEY_ID, e.code))

# ------------------------------------------------------------------ compare --
print("\n[5] COMPARE - for every certificate the partner receives")
mismatches = []
for num in sorted(api):
    sent = api[num]
    shown = ui.get(num)
    if shown is None:
        mismatches.append((num, "DELIVERED to the partner but ABSENT from the UI listing",
                           set(), set()))
        continue
    only_sent, only_shown = sent - shown, shown - sent
    if only_sent or only_shown:
        mismatches.append((num, "product-ID sets differ", only_sent, only_shown))

ui_only = sorted(set(ui) - set(api))
print("    certificates compared        : %d" % len(api))
print("    certificates with mismatches : %d" % len(mismatches))
print("    UI-only certificates         : %d  (expected - status / effective-date scoping)"
      % len(ui_only))

if mismatches:
    print("\n" + "-" * 100)
    print("  MISMATCH - the partner is not seeing what the application shows")
    print("-" * 100)
    for num, why, only_sent, only_shown in mismatches[:40]:
        print("  %-10s %s" % (num, why))
        if only_sent:
            s = sorted(only_sent)
            print("      sent to the partner but NOT shown in the app (%d): %s%s"
                  % (len(s), ", ".join(s[:8]), " ..." if len(s) > 8 else ""))
        if only_shown:
            s = sorted(only_shown)
            print("      shown in the app but NOT sent (%d): %s%s"
                  % (len(s), ", ".join(s[:8]), " ..." if len(s) > 8 else ""))
    if len(mismatches) > 40:
        print("  ... and %d more" % (len(mismatches) - 40))

print("\n" + "=" * 100)
if mismatches:
    print("  CROSS-SURFACE: FAIL - %d certificate(s) differ between the two surfaces"
          % len(mismatches))
else:
    print("  CROSS-SURFACE: PASS - the partner sees exactly what the application shows")
print("=" * 100)
sys.exit(1 if mismatches else 0)
