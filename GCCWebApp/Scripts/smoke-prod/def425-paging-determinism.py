"""DEF-425 verification - partner API paging must be deterministic.

Deliberately uses a SMALL pageSize. The defect needs a group of rows tied on the sort key to
straddle a page boundary; with pageSize=100 there are only 2 boundaries in the whole feed, which is
why it stayed latent for days. pageSize=3 manufactures dozens of boundaries, so ties are almost
guaranteed to straddle one.

Three assertions, in increasing strength:
  1. rows == distinct certificate numbers        (catches repeats within one walk)
  2. the walk agrees with the unpaged totalCount  (catches drops)
  3. two independent walks return the IDENTICAL ordered list (catches non-determinism itself -
     the defect is a race, so a single clean walk is weak evidence)

Run against localhost with the fix in place. Exit 0 = pass.
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
    # `admin`'s password was reset on TEST 2026-08-19 and only Shawn has it - admin.uat is the
    # working Admin account there. On dev, `admin` still carries the original.
    "test": {"base": "https://gccwebapp.com", "admin": ("admin.uat", "TestPass123!")},
    "dev": {"base": "http://localhost:5101", "admin": ("admin", "TestPass123!")},
}

TARGET = sys.argv[1] if len(sys.argv) > 1 and sys.argv[1] in TARGETS else "dev"
CFG = TARGETS[TARGET]
BASE = CFG["base"]
UA = "def425-verify"
PAGE_SIZE = int(sys.argv[2]) if len(sys.argv) > 2 else 3

# TestEnvironmentGate.Mode is "Cookie". Without the gate cookie every path answers 200 with a small
# gate page, JSON parsing fails, and a gate fault reports as an API fault (DEF-341).
GATE_PASSWORD = os.environ.get("GATE_PASS", "Cert-Test-Access-2026!")

print("### DEF-425 paging determinism   target=%s (%s)\n" % (TARGET.upper(), BASE))


def session():
    cj = http.cookiejar.CookieJar()
    op = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(cj))
    pass_gate(op)
    return op


def pass_gate(op):
    """GET the gate form for a fresh CSRF formToken, then POST the password. Posting without the
    token returns 400 'session expired', which reads like a wrong password."""
    code, html = get(op, "/__gate?returnUrl=%2F")
    if code != 200 or "formToken" not in html:
        return False                       # no gate here (localhost)
    m = re.search('name="formToken"[^>]*value="([^"]+)"', html)
    if not m:
        return False
    data = urllib.parse.urlencode(
        {"password": GATE_PASSWORD, "returnUrl": "/", "formToken": m.group(1)}).encode()
    try:
        op.open(urllib.request.Request(
            BASE + "/__gate", data=data,
            headers={"Content-Type": "application/x-www-form-urlencoded", "User-Agent": UA}),
            timeout=300)
    except urllib.error.HTTPError:
        pass
    return True


def get(op, path, headers=None):
    h = {"User-Agent": UA}
    if headers:
        h.update(headers)
    try:
        r = op.open(urllib.request.Request(BASE + path, headers=h), timeout=300)
        return r.getcode(), r.read().decode("utf-8", "replace")
    except urllib.error.HTTPError as e:
        return e.code, e.read().decode("utf-8", "replace")


def post(op, path, fields):
    data = urllib.parse.urlencode(fields).encode()
    try:
        r = op.open(urllib.request.Request(
            BASE + path, data=data,
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


admin = session()
code, html = get(admin, "/Account/Login")
m = re.search('name="__RequestVerificationToken"[^>]*value="([^"]+)"', html)
if not m:
    sys.exit("FATAL: no antiforgery token - is the app up on %s?" % BASE)
post(admin, "/Account/Login",
     {"Email": CFG["admin"][0], "Password": CFG["admin"][1],
      "__RequestVerificationToken": m.group(1)})
code, body = get(admin, "/api/admin/partner/list",
                 {"Accept": "application/json", "X-Requested-With": "XMLHttpRequest"})
if code != 200:
    sys.exit("FATAL: admin sign-in failed (/api/admin/partner/list -> %s)" % code)
partners = json.loads(body).get("data") or []
amazon = next((p for p in partners if p.get("name") == "Amazon"), None)
if not amazon:
    sys.exit("FATAL: no Amazon partner on this database")
code, body = post_json(admin, "/api/admin/partner/%s/apikey" % amazon["id"],
                       {"description": "def425-verify"})
keydata = json.loads(body).get("data") or {}
KEY, KEY_ID = keydata.get("apiKey"), keydata.get("id")
if not KEY:
    sys.exit("FATAL: could not mint a key: %s" % body[:200])
print("minted key id %s for partner Amazon\n" % KEY_ID)

# Partner calls MUST be on a session with no app cookie, or the middleware stands aside and the
# controller rejects the cookie identity with a bare 401 that reads like a bad key.
partner = session()


def walk(path_tmpl, label):
    rows, page, total = [], 1, None
    while True:
        code, body = get(partner, path_tmpl % page,
                         {"X-API-Key": KEY, "Accept": "application/json"})
        # A small pageSize means many requests, which trips the partner rate limiter (DEF-345).
        # That is the limiter working correctly, not a paging fault - back off and retry rather
        # than reporting it as a failure of the thing under test.
        tries = 0
        while code == 429 and tries < 12:
            tries += 1
            time.sleep(10)
            code, body = get(partner, path_tmpl % page,
                             {"X-API-Key": KEY, "Accept": "application/json"})
        if code != 200:
            sys.exit("FATAL: %s page %d -> HTTP %s\n%s" % (label, page, code, body[:300]))
        env = json.loads(body)
        pg = env.get("pagination") or {}
        if total is None:
            total = pg.get("totalCount")
        rows.extend(env.get("data") or [])
        if not pg.get("hasNext"):
            break
        page += 1
        if page > 2000:
            sys.exit("FATAL: %s exceeded 2000 pages" % label)
    return rows, total, page


failures = []

for label, tmpl, key in (
    ("standard", "/api/v1/certificates?includeProductIds=false&pageSize=%d&pageNumber=%%d"
                 % PAGE_SIZE, "certificateNumber"),
    ("gs1", "/api/v1/certificates/gs1?pageSize=%d&pageNumber=%%d" % PAGE_SIZE, "id"),
):
    print("=== %s  (pageSize=%d) ===" % (label, PAGE_SIZE))
    rows_a, total, pages = walk(tmpl, label + " walk A")
    keys_a = [r.get(key) for r in rows_a]
    distinct = len(set(keys_a))
    print("  walk A: %d pages, %d rows, %d distinct %s (totalCount=%s)"
          % (pages, len(rows_a), distinct, key, total))

    if len(rows_a) != distinct:
        dupes = sorted(k for k in set(keys_a) if keys_a.count(k) > 1)
        failures.append("%s: %d rows but %d distinct - repeated: %s"
                        % (label, len(rows_a), distinct, dupes))
        print("  FAIL rows != distinct - repeated: %s" % dupes)
    else:
        print("  PASS rows == distinct")

    if total is not None and total != len(rows_a):
        failures.append("%s: collected %d rows but totalCount=%s" % (label, len(rows_a), total))
        print("  FAIL collected %d but totalCount=%s" % (len(rows_a), total))
    else:
        print("  PASS walk matches totalCount")

    rows_b, _, _ = walk(tmpl, label + " walk B")
    keys_b = [r.get(key) for r in rows_b]
    if keys_a != keys_b:
        only_a = set(keys_a) - set(keys_b)
        only_b = set(keys_b) - set(keys_a)
        failures.append("%s: two walks disagree (order or membership). only in A: %s / only in B: %s"
                        % (label, sorted(only_a), sorted(only_b)))
        print("  FAIL two walks returned different lists")
    else:
        print("  PASS two independent walks are byte-identical (ordering is deterministic)")
    print()

# Tidy up: a key is a credential even on localhost.
req = urllib.request.Request(BASE + "/api/admin/partner/apikey/%s" % KEY_ID,
                             headers={"Accept": "application/json",
                                      "X-Requested-With": "XMLHttpRequest", "User-Agent": UA},
                             method="DELETE")
try:
    admin.open(req, timeout=60)
    print("key %s revoked" % KEY_ID)
except urllib.error.HTTPError as e:
    print("!! could not revoke key %s (HTTP %s) - revoke by hand" % (KEY_ID, e.code))

print("\n%s" % ("DEF-425 VERIFY: PASS" if not failures else "DEF-425 VERIFY: FAIL"))
for f in failures:
    print("  - %s" % f)
sys.exit(1 if failures else 0)
