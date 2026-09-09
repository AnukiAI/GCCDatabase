"""
Live verification of CR-043 and CR-045 against localhost:5101 (GCCDatabase_Dev).

Enumerates rows rather than reporting counts: a total row count cannot show WHICH
certificates moved, and "the number went up" is exactly the evidence that would hide
a partial fix.

Controls are deliberate throughout:
  - CR-043 widens the badging surfaces. The control is the PUBLIC surface, which must
    NOT widen. A run where everything returns more rows proves nothing.
  - CR-045 is expected to be a no-op on today's data. The control is that a stored
    "23%" still publishes as "23%" - unchanged - while the x100 rule is gone.
"""
import re, io, os, csv, sys, json, urllib.request, urllib.parse, urllib.error, http.cookiejar

# Point at the environment under test. Defaults to localhost; pass a base URL to run
# it against gccwebapp.com after the deploy (the test env gate takes HTTP Basic gcc/...).
BASE = sys.argv[1].rstrip("/") if len(sys.argv) > 1 else "http://localhost:5101"

# The 13 in-window Confidential certificates carrying product identifiers on dev.
CONFIDENTIAL = ["24-5426", "24-5427", "24-5428", "24-5712", "24-5713", "24-5714",
                "24-5773", "25-0479", "25-0480", "25-0481", "25-0482",
                "26-0473", "26-0474"]

# Accounts differ by environment, and getting this wrong reads as a broken app:
#   dev/localhost - `admin` still has TestPass123!, and the partner fixtures come from
#                   Scripts/ci/seed-e2e.sql (partner@amazon.com / perry.novi@example.com).
#   TEST          - `admin`'s password was changed 2026-08-19 and only Shawn knows it, so
#                   admin.uat is the Admin account; the partner accounts are the role-matrix
#                   fixtures test.amazon / test.novi on GccTest2026!.
IS_LOCAL = "localhost" in BASE or "127.0.0.1" in BASE

if IS_LOCAL:
    ADMINS = [("admin", "TestPass123!"), ("admin.uat", "TestPass123!")]
    AMAZON = ("partner@amazon.com", "TestPass123!")
    NOVI = ("perry.novi@example.com", "TestPass123!")
    OTHER_OWNER = None   # no equivalent company fixture seeded on dev
else:
    ADMINS = [("admin.uat", "TestPass123!")]
    AMAZON = ("test.amazon", "GccTest2026!")
    NOVI = ("test.novi", "GccTest2026!")
    # Sika Corporation, 574 certificates - none of them Confidential and none of them
    # owned by Block / Domtar / First Quality / Kenvue. That makes it a control that
    # actually returns data, which test.novi does not on this dataset.
    OTHER_OWNER = ("test.external", "GccTest2026!")

# TestEnvironmentGate.Mode is "Cookie", NOT Basic Auth. Sending httpCredentials and following
# redirects yields HTTP 200 and a ~2.5KB gate page for EVERY url, which then gets graded as the
# application - that is DEF-341, a false security PASS. Mirrors Scripts/smoke-prod/_lib/test-env-gate.js.
GATE_PASSWORD = os.environ.get("GATE_PASS", "Cert-Test-Access-2026!")

results = []


def rec(name, ok, detail):
    results.append((name, ok, detail))
    print(("[PASS] " if ok else "[FAIL] ") + name + ": " + detail)


def new_session():
    """A fresh cookie jar, already through the test-environment gate when there is one.

    Every session needs its own gate cookie - including the ANONYMOUS one. That is the
    subtle part: without it the "anonymous" checks below are talking to the gate page,
    every URL answers 200, and a 404 assertion fails for a reason that has nothing to do
    with the certificate embargo."""
    cj = http.cookiejar.CookieJar()
    op = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(cj))
    pass_gate(op)
    return op


def pass_gate(op):
    """Cookie gate: GET the form for a fresh CSRF formToken, then POST the password.
    Posting the password without the token returns 400 'session expired', not 401, which
    reads like a wrong password and sends you looking in the wrong place."""
    code, html = get(op, "/__gate?returnUrl=%2F")
    if code != 200 or "formToken" not in html:
        return False          # no gate on this environment (localhost)
    m = re.search('name="formToken"[^>]*value="([^"]+)"', html)
    if not m:
        return False
    data = urllib.parse.urlencode(
        {"password": GATE_PASSWORD, "returnUrl": "/", "formToken": m.group(1)}).encode()
    req = urllib.request.Request(
        BASE + "/__gate", data=data,
        headers={"Content-Type": "application/x-www-form-urlencoded",
                 "User-Agent": "cr-verify"})
    try:
        op.open(req, timeout=180)
    except urllib.error.HTTPError:
        pass
    return True


def get(op, path, headers=None):
    h = {"User-Agent": "cr-verify"}
    if headers:
        h.update(headers)
    req = urllib.request.Request(BASE + path, headers=h)
    try:
        r = op.open(req, timeout=180)
        return r.getcode(), r.read().decode("utf-8", "replace")
    except urllib.error.HTTPError as e:
        return e.code, e.read().decode("utf-8", "replace")


def login(op, email, pw, landing="/Dashboard"):
    """`landing` matters: a Partner-role user is not permitted on /Dashboard, so probing
    there reports a working sign-in as a failed one. Check a page the role can actually
    reach - a probe that fails on a known-good account is testing the probe."""
    code, html = get(op, "/Account/Login")
    m = re.search('name="__RequestVerificationToken"[^>]*value="([^"]+)"', html)
    if not m:
        return False
    data = urllib.parse.urlencode(
        {"Email": email, "Password": pw, "__RequestVerificationToken": m.group(1)}
    ).encode()
    req = urllib.request.Request(
        BASE + "/Account/Login", data=data,
        headers={"Content-Type": "application/x-www-form-urlencoded",
                 "User-Agent": "cr-verify"})
    try:
        op.open(req, timeout=180)
    except urllib.error.HTTPError:
        pass
    code, html = get(op, landing)
    return code == 200 and "Account/Login" not in html


def parse_report_csv(text):
    """The admin CSV reports carry a title/date preamble before the real header row.
    Find the header by looking for the row that names the certificate column."""
    lines = text.lstrip("﻿").splitlines()
    start = next((i for i, l in enumerate(lines)
                  if "Certificate Number" in l or "CertificateNumber" in l), None)
    if start is None:
        return [], None
    rows = list(csv.DictReader(io.StringIO("\n".join(lines[start:]))))
    col = next((c for c in rows[0].keys()
                if c and "Certificate" in c and "Number" in c), None) if rows else None
    return rows, col


# ---------------------------------------------------------------------------
# CR-043 A - the CONTROL. The public database must be untouched.
# ---------------------------------------------------------------------------
anon = new_session()
leaked = []
for n in CONFIDENTIAL:
    code, _ = get(anon, "/Certificate/CertificateSingleView/" + n)
    if code != 404:
        leaked.append(n + "=" + str(code))

rec("CR043.A1  public database still hides every Confidential cert",
    not leaked,
    "all 13 return 404 anonymously" if not leaked
    else "EMBARGO BROKEN: " + ", ".join(leaked))

# A known-good control: a publicly live cert must still return 200, or A1 would pass
# simply because the whole public surface is broken.
code, _ = get(anon, "/Certificate/CertificateSingleView/23-0753")
rec("CR043.A2  control - a public cert still loads anonymously",
    code == 200, "23-0753 returns HTTP " + str(code) + " (expect 200)")

# ---------------------------------------------------------------------------
# CR-043 B - the PI export must now carry the Confidential identifiers.
# ---------------------------------------------------------------------------
admin = new_session()
signed_in = any(login(admin, u, pw) for u, pw in ADMINS)
rec("CR043.B0  admin signed in", signed_in, "session established" if signed_in else "login failed")

if signed_in:
    code, body = get(admin, "/Export/DownloadActiveProductIds")
    rows, col = parse_report_csv(body)
    if code != 200 or not col:
        rec("CR043.B1  Confidential certs present in the PI export", False,
            "HTTP " + str(code) + ", header row not found")
    else:
        nums = set(r[col] for r in rows)
        found = [n for n in CONFIDENTIAL if n in nums]
        missing = [n for n in CONFIDENTIAL if n not in nums]
        rec("CR043.B1  every Confidential cert is now in the PI export",
            len(found) == len(CONFIDENTIAL),
            str(len(found)) + "/13 present"
            + ("" if not missing else " | MISSING: " + ", ".join(missing)))

        conf_rows = [r for r in rows if r[col] in CONFIDENTIAL]
        rec("CR043.B2  identifier rows added by the change",
            len(conf_rows) > 0,
            str(len(conf_rows)) + " identifier rows across "
            + str(len(set(r[col] for r in conf_rows))) + " certificates; "
            + str(len(rows)) + " rows in the file")

        scol = next((c for c in rows[0].keys() if c and c.strip() == "Certificate Status"), None)
        if scol:
            statuses = sorted(set(r[scol] for r in conf_rows))
            rec("CR043.B3  they are labelled Confidential, not relabelled",
                statuses == ["Client Approved - Confidential"],
                "status column = " + ", ".join(statuses))

        # CONTROL: an EXPIRED cert must still be absent. 23-0758 is the certificate the
        # client reported twice; CR-043 does not bring it back and must not.
        rec("CR043.B4  control - expired 23-0758 is still absent (a different cause)",
            "23-0758" not in nums,
            "absent, as expected" if "23-0758" not in nums
            else "PRESENT - the window filter has been widened by mistake")

# ---------------------------------------------------------------------------
# CR-043 C - the signed-in partner surface, and the isolation control.
#
# /api/v1/certificates is API-KEY authenticated, so a cookie session gets 401 there;
# the equivalent cookie surface is the external tile API, which serves Partner and
# External roles and is one of the controllers CR-043 repointed.
#
# All 13 Confidential certs are Amazon-tagged on dev (verified in the database), so
# Amazon SHOULD now see them and Novi should NOT. Widening a STATUS set must never
# widen a PARTNER set - that pairing is the whole test, because a run that only shows
# Amazon seeing more rows cannot tell "correct" from "isolation broken".
# ---------------------------------------------------------------------------
TILES = "/api/CertificateTileExternal?page=1&pageSize=500"

def partner_sees(email, pw):
    op = new_session()
    if not login(op, email, pw,
                 landing="/Certificate/CertificateTileViewExternal"):
        return None, "sign-in failed"
    code, body = get(op, TILES, headers={"X-Requested-With": "XMLHttpRequest"})
    if code != 200:
        return None, "HTTP " + str(code)
    # The field is certificateNumber, not number. My first probe matched "number" and
    # returned 0 for BOTH partners - and a control that "passes" because nothing came
    # back proves nothing. Assert the payload is non-empty before reading it.
    seen = set(re.findall(r'"certificateNumber"\s*:\s*"([^"]+)"', body))
    total = re.search(r'"totalCount"\s*:\s*(\d+)', body)
    return seen, ("HTTP 200, totalCount=" + (total.group(1) if total else "?")
                  + ", " + str(len(seen)) + " numbers parsed")

amazon_seen, amazon_note = partner_sees(*AMAZON)
if amazon_seen is None:
    rec("CR043.C1  Amazon partner sees in-force Confidential certs", False, amazon_note)
else:
    hit = [n for n in CONFIDENTIAL if n in amazon_seen]
    rec("CR043.C1  Amazon partner now sees the Confidential certs tagged to it",
        len(hit) == len(CONFIDENTIAL),
        str(len(hit)) + "/13 visible (" + amazon_note + ")"
        + ("" if len(hit) == len(CONFIDENTIAL)
           else " | MISSING: " + ", ".join(n for n in CONFIDENTIAL if n not in amazon_seen)))

# --- C2: the co-partner control -------------------------------------------------
# A control that returns NOTHING cannot distinguish "correctly denied" from "broken".
# On the TEST dataset Novi has 0 tagged certificates (verified in the database), so this
# check is INCONCLUSIVE there and is reported as such rather than as a pass. On dev the
# seed gives Novi its own certificate and the control is live.
novi_seen, novi_note = partner_sees(*NOVI)
if novi_seen is None:
    rec("CR043.C2  co-partner control (Novi)", False, novi_note)
elif not novi_seen:
    rec("CR043.C2  co-partner control (Novi)", False,
        "INCONCLUSIVE - Novi sees 0 certificates in total (" + novi_note + "), so a zero here "
        "proves nothing about isolation. Novi has no tagged certificates on this dataset. "
        "See C3 for the control that does return data.")
else:
    leak = [n for n in CONFIDENTIAL if n in novi_seen]
    rec("CR043.C2  co-partner control - Novi cannot see Amazon-only Confidential certs",
        not leak,
        "0/13 leaked to Novi (" + novi_note + ") - status widened, partner scope did not"
        if not leak else "PARTNER LEAK: " + ", ".join(leak))

# --- C3: the owning-company control ---------------------------------------------
# The 13 Confidential certificates belong to Block Inc., Domtar, First Quality and Kenvue.
# A DIFFERENT company's account must see its own certificates (non-empty, so the control is
# live) and none of those 13. This is the assertion C2 cannot make on the test dataset.
if OTHER_OWNER:
    other_seen, other_note = partner_sees(*OTHER_OWNER)
    if other_seen is None:
        rec("CR043.C3  control - another company sees its own certs but none of the 13",
            False, other_note)
    else:
        leak = [n for n in CONFIDENTIAL if n in other_seen]
        rec("CR043.C3  control - another company sees its own certs but none of the 13",
            bool(other_seen) and not leak,
            (str(len(other_seen)) + " certificates visible to this company (" + other_note
             + "), 0 of the 13 Confidential certs among them")
            if (other_seen and not leak) else
            ("EMPTY - control proves nothing (" + other_note + ")" if not other_seen
             else "CROSS-COMPANY LEAK: " + ", ".join(leak)))

# ---------------------------------------------------------------------------
# CR-045 - the stored value publishes verbatim.
# ---------------------------------------------------------------------------
if signed_in:
    code, body = get(admin, "/api/certificate-tiles-internal?search=23-0758&pageSize=5",
                     headers={"X-Requested-With": "XMLHttpRequest"})
    m = re.search('"certificationResults"\\s*:\\s*"([^"]*)"', body, re.I)
    rec("CR045.D1  internal tile API publishes the stored value verbatim",
        bool(m) and m.group(1) == "23%",
        "certificationResults = " + (repr(m.group(1)) if m else "<absent>")
        + '  (stored value is "23%")')

    code, body = get(admin, "/Certificate/CertificateInternalView/23-0758")
    rec("CR045.D2  the detail view shows the stored value verbatim",
        code == 200 and "23%" in body and ">2300%" not in body,
        "HTTP " + str(code) + ", page contains \"23%\"" if code == 200 else "HTTP " + str(code))

print("")
passed = sum(1 for _, ok, _ in results if ok)
failed = sum(1 for _, ok, _ in results if not ok)
print("SUMMARY: " + str(passed) + " passed, " + str(failed) + " failed")
sys.exit(1 if failed else 0)
