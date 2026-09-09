"""
2026-09-09 - Export exactly what Amazon receives, and prove CR-045 did not change it.

WHY THIS EXISTS
CR-045 made Certification Results publish verbatim: whatever is stored is what is sent,
with no display-time conversion. The client asked for two things:

  1. "have someone check all the Amazon certs again once you change the Cert Results
     field to ensure those didn't break"
  2. "create file of what is sent to amazon. json and csv"

So this script pulls the LIVE partner payload through the same API key + endpoints Amazon
uses, writes it as JSON and CSV for a human to review, and reconciles the delivered
Certification Results against the database three ways.

WHAT IT DOES *NOT* DO
It never writes to the database and never mutates a certificate. The only write is a new
Amazon API key minted for the pull (the endpoint issues additional keys; existing keys keep
working), and files under _amazon_payload/.

THE RECONCILIATION IS THE POINT
Counting rows cannot show that nothing broke - a payload of the right SIZE can still carry
a converted value. So for every certificate delivered we compare four strings:

    DB now  vs  DB before CR-045 (CertResults_bak_CR045_20260908)
            vs  standard-list  certificationResults
            vs  GS1            certificationInfo.value

All four must be byte-identical. Any single mismatch is a real finding, and the row is
written to the exceptions file rather than folded into a total.

The pre-change snapshot is what makes the "nothing broke" claim falsifiable. Without it the
run only shows that the API agrees with the database TODAY, which would be equally true if
both had changed together.

USAGE
    python Scripts/data-ops/2026-09-09-amazon-payload-export.py test
    python Scripts/data-ops/2026-09-09-amazon-payload-export.py dev

Output lands in Scripts/data-ops/_amazon_payload/<timestamp>/.
"""
import os
import re
import sys
import csv
import json
import time
import datetime as dt
import urllib.request
import urllib.parse
import urllib.error
import http.cookiejar

HERE = os.path.dirname(os.path.abspath(__file__))
OUTBASE = os.path.join(HERE, "_amazon_payload")

TARGETS = {
    "test": {
        "base": "https://gccwebapp.com",
        "admin": ("admin.uat", "TestPass123!"),
        "conn": ("Driver={ODBC Driver 17 for SQL Server};Server=tcp:68.71.129.120,1533;"
                 "Database=gccwebap_certificates;Uid=GCC_adminxyz123;Pwd=Q3Ez4e!gE_47KNi;"
                 "Encrypt=yes;TrustServerCertificate=yes;"),
        # Snapshot of every certificate's Certification Results taken immediately before the
        # CR-045 deploy on 2026-09-08. Absent on dev; the run degrades to a 3-way compare and
        # says so rather than silently dropping the strongest control.
        "snapshot": "CertResults_bak_CR045_20260908",
    },
    "dev": {
        "base": "http://localhost:5101",
        # `admin` still has its original password on dev; on TEST it was reset 2026-08-19
        # and only Shawn knows the new value - hence admin.uat above.
        "admin": ("admin", "TestPass123!"),
        "conn": ("Driver={ODBC Driver 17 for SQL Server};Server=ANUKI01;"
                 "Database=GCCDatabase_Dev;Uid=GCC_DevUser;Pwd=DevP@ss2024!SQL;"
                 "TrustServerCertificate=yes;Encrypt=no;"),
        "snapshot": None,
    },
}

if len(sys.argv) < 2 or sys.argv[1] not in TARGETS:
    sys.exit("usage: 2026-09-09-amazon-payload-export.py {test|dev}")

TARGET = sys.argv[1]
CFG = TARGETS[TARGET]
BASE = CFG["base"].rstrip("/")

# TestEnvironmentGate.Mode is "Cookie", not Basic auth. Without the gate cookie every path
# answers 200 with a ~2.5KB gate page, JSON parsing fails, and the run reports an API fault
# that is really a gate fault (DEF-341).
GATE_PASSWORD = os.environ.get("GATE_PASS", "Cert-Test-Access-2026!")

TIMEOUT = int(os.environ.get("REQ_TIMEOUT", "300"))
UA = "amazon-payload-export"

stamp = dt.datetime.now().strftime("%Y%m%d-%H%M%S")
OUTDIR = os.path.join(OUTBASE, stamp)
os.makedirs(OUTDIR, exist_ok=True)

print("### WHAT AMAZON RECEIVES   target=%s (%s)   %s" % (TARGET.upper(), BASE, stamp))
print("### output: %s\n" % OUTDIR)


# --------------------------------------------------------------------- http --
def new_session():
    cj = http.cookiejar.CookieJar()
    op = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(cj))
    pass_gate(op)
    return op


def pass_gate(op):
    """GET the gate form for a fresh CSRF formToken, then POST the password. Posting the
    password without the token returns 400 'session expired', which reads like a wrong
    password and sends you looking in the wrong place."""
    code, html = raw_get(op, "/__gate?returnUrl=%2F")
    if code != 200 or "formToken" not in html:
        return False                      # no gate on this environment (localhost)
    m = re.search('name="formToken"[^>]*value="([^"]+)"', html)
    if not m:
        return False
    data = urllib.parse.urlencode(
        {"password": GATE_PASSWORD, "returnUrl": "/", "formToken": m.group(1)}).encode()
    try:
        op.open(urllib.request.Request(
            BASE + "/__gate", data=data,
            headers={"Content-Type": "application/x-www-form-urlencoded",
                     "User-Agent": UA}), timeout=TIMEOUT)
    except urllib.error.HTTPError:
        pass
    return True


def raw_get(op, path, headers=None):
    h = {"User-Agent": UA}
    if headers:
        h.update(headers)
    try:
        r = op.open(urllib.request.Request(BASE + path, headers=h), timeout=TIMEOUT)
        return r.getcode(), r.read().decode("utf-8", "replace")
    except urllib.error.HTTPError as e:
        return e.code, e.read().decode("utf-8", "replace")


def post(op, path, fields, headers=None):
    h = {"Content-Type": "application/x-www-form-urlencoded", "User-Agent": UA}
    if headers:
        h.update(headers)
    data = urllib.parse.urlencode(fields).encode()
    try:
        r = op.open(urllib.request.Request(BASE + path, data=data, headers=h), timeout=TIMEOUT)
        return r.getcode(), r.read().decode("utf-8", "replace")
    except urllib.error.HTTPError as e:
        return e.code, e.read().decode("utf-8", "replace")


def post_json(op, path, obj):
    body = json.dumps(obj).encode()
    h = {"Content-Type": "application/json", "Accept": "application/json",
         "X-Requested-With": "XMLHttpRequest", "User-Agent": UA}
    try:
        r = op.open(urllib.request.Request(BASE + path, data=body, headers=h), timeout=TIMEOUT)
        return r.getcode(), r.read().decode("utf-8", "replace")
    except urllib.error.HTTPError as e:
        return e.code, e.read().decode("utf-8", "replace")


def login(op, email, pw, landing="/Dashboard"):
    code, html = raw_get(op, "/Account/Login")
    m = re.search('name="__RequestVerificationToken"[^>]*value="([^"]+)"', html)
    if not m:
        return False
    post(op, "/Account/Login",
         {"Email": email, "Password": pw, "__RequestVerificationToken": m.group(1)})
    code, html = raw_get(op, landing)
    return code == 200 and "Account/Login" not in html


# -------------------------------------------------------------- partner key --
def get_amazon_key(op):
    """Mint a key the way the contract-diff smoke does. Additive: existing Amazon keys are
    untouched, so this cannot disturb the client's real integration."""
    code, body = raw_get(op, "/api/admin/partner/list",
                         {"Accept": "application/json", "X-Requested-With": "XMLHttpRequest"})
    if code != 200:
        sys.exit("FATAL: /api/admin/partner/list -> HTTP %s. Admin login likely failed." % code)
    partners = json.loads(body).get("data") or []
    amazon = next((p for p in partners if p.get("name") == "Amazon"), None)
    if not amazon:
        sys.exit("FATAL: no partner named 'Amazon'. Found: %s"
                 % ", ".join(sorted(str(p.get("name")) for p in partners)))
    code, body = post_json(op, "/api/admin/partner/%s/apikey" % amazon["id"],
                           {"description": "amazon-payload-export %s" % stamp})
    if code not in (200, 201):
        sys.exit("FATAL: could not mint an Amazon API key -> HTTP %s" % code)
    data = json.loads(body).get("data") or {}
    key = data.get("apiKey")
    if not key or not key.startswith("gcc_partner_"):
        sys.exit("FATAL: key response did not carry a gcc_partner_ key: %s" % body[:200])
    print("  key: minted for partner Amazon (partner id %s, key id %s)"
          % (amazon["id"], data.get("id")))
    return key, data.get("id")


def revoke_key(op, key_id):
    """Revoke the key this run minted. Without it every run leaves a live Amazon credential
    behind - by 2026-09-09 the test environment had accumulated 15 active Amazon keys from
    verification scripts that all minted and none revoked. A key is a credential even on a
    pre-launch environment; the tidy-up belongs with the script that created it."""
    if key_id is None:
        print("  !! mint response carried no key id - cannot self-revoke. Revoke by hand at "
              "/Admin/Partners so this run does not leave a live Amazon key behind.")
        return
    h = {"Accept": "application/json", "X-Requested-With": "XMLHttpRequest", "User-Agent": UA}
    req = urllib.request.Request(BASE + "/api/admin/partner/apikey/%s" % key_id,
                                 headers=h, method="DELETE")
    try:
        r = op.open(req, timeout=TIMEOUT)
        print("  key %s revoked (HTTP %s)" % (key_id, r.getcode()))
    except urllib.error.HTTPError as e:
        print("  !! could not revoke key %s (HTTP %s) - revoke it by hand." % (key_id, e.code))


def api_get(op, key, path):
    """A partner call carries the API key and NO app cookie for auth - but on the deployed
    host it still needs the gate cookie to reach the app at all. The key auth is unaffected
    by that cookie.

    `op` MUST be a session that has NOT signed in. PartnerApiKeyAuthenticationMiddleware
    begins with "skip if already authenticated (cookie/JWT takes precedence)", so an admin
    cookie on the same session makes the middleware step aside, and the controller's
    [Authorize(AuthenticationSchemes = "ApiKey")] then rejects the cookie identity. The
    result is a flat HTTP 401 with an empty body, which reads exactly like a bad API key -
    it is not; it is the wrong session. Hence the split sessions below."""
    started = time.time()
    code, body = raw_get(op, path, {"X-API-Key": key, "Accept": "application/json"})
    return code, body, time.time() - started


def page_all(op, key, path_tmpl, label):
    """Walk every page. `hasNext` is authoritative; totalPages is a cross-check, and a
    disagreement between them is reported rather than trusted, because paging that quietly
    stops early produces a short file that still looks well-formed."""
    records, page, pages_seen, total_expected = [], 1, 0, None
    while True:
        url = path_tmpl % page
        code, body, secs = api_get(op, key, url)
        if code != 200:
            # One retry: the first execution of a given query shape has been reproducibly
            # slow on this dataset (120s+ observed 2026-08-24). A slow first call must not
            # be recorded as a short payload.
            print("  RETRY %s page %d -> HTTP %s after %.1fs" % (label, page, code, secs))
            code, body, secs = api_get(op, key, url)
        if code != 200:
            sys.exit("FATAL: %s page %d -> HTTP %s\n%s" % (label, page, code, body[:300]))
        env = json.loads(body)
        pg = env.get("pagination") or {}
        if total_expected is None:
            total_expected = pg.get("totalCount")
        batch = env.get("data") or []
        records.extend(batch)
        pages_seen += 1
        print("  %s page %d: %d records (%.1fs)" % (label, page, len(batch), secs))
        if not pg.get("hasNext"):
            break
        page += 1
        if page > 500:
            sys.exit("FATAL: %s exceeded 500 pages - refusing to loop." % label)
    if total_expected is not None and total_expected != len(records):
        print("  !! %s: pagination.totalCount=%s but %d records collected - "
              "paging disagreement, treat this file as suspect."
              % (label, total_expected, len(records)))
    return records, total_expected


# ------------------------------------------------------------------ csv out --
def write_csv(path, fieldnames, rows):
    # utf-8-sig: Excel opens these directly, and the client does open them in Excel.
    with open(path, "w", newline="", encoding="utf-8-sig") as fh:
        w = csv.DictWriter(fh, fieldnames=fieldnames, extrasaction="ignore")
        w.writeheader()
        for r in rows:
            w.writerow(r)
    print("  wrote %-42s %d rows" % (os.path.basename(path), len(rows)))


def write_json(path, obj):
    with open(path, "w", encoding="utf-8") as fh:
        json.dump(obj, fh, indent=2, ensure_ascii=False)
    size = os.path.getsize(path)
    print("  wrote %-42s %.1f KB" % (os.path.basename(path), size / 1024.0))


# ===========================================================================
print("[1] sessions + gate")
admin_op = new_session()     # signs in; used only to mint the key
partner_op = new_session()   # gate cookie ONLY - this is how Amazon calls

print("[2] admin sign-in (%s)" % CFG["admin"][0])
if not login(admin_op, CFG["admin"][0], CFG["admin"][1]):
    sys.exit("FATAL: admin sign-in failed. On TEST use admin.uat - `admin`'s password was "
             "reset 2026-08-19 and only Shawn has it.")

print("[3] Amazon partner API key")
KEY, KEY_ID = get_amazon_key(admin_op)

print("\n[4] standard list  /api/v1/certificates?includeProductIds=true")
std, std_total = page_all(
    partner_op, KEY,
    "/api/v1/certificates?includeProductIds=true&pageSize=100&pageNumber=%d",
    "standard")

print("\n[5] GS1 list  /api/v1/certificates/gs1")
gs1, gs1_total = page_all(
    partner_op, KEY, "/api/v1/certificates/gs1?pageSize=100&pageNumber=%d", "gs1")

# Both pulls are done - the key has no further job. Revoke it here rather than at the end, so
# a failure in the file-writing steps below still cannot leave a live credential behind.
revoke_key(admin_op, KEY_ID)

# --------------------------------------------------------------- JSON files --
print("\n[6] JSON - the payload as delivered, unmodified")
write_json(os.path.join(OUTDIR, "amazon-certificates.json"),
           {"source": BASE + "/api/v1/certificates?includeProductIds=true",
            "retrieved": stamp, "totalCount": std_total, "data": std})
write_json(os.path.join(OUTDIR, "amazon-gs1.json"),
           {"source": BASE + "/api/v1/certificates/gs1",
            "retrieved": stamp, "totalCount": gs1_total, "data": gs1})

# ---------------------------------------------------------------- CSV files --
print("\n[7] CSV - the same payload, flattened for review")

CERT_COLS = ["certificateNumber", "status", "issueDate", "expirationDate",
             "certificationCountries", "partner", "certificationResults", "pathway",
             "company", "productName", "certificationType", "amazonCode", "issuer",
             "productIdCount", "pdfDownloadUrl"]
cert_rows = []
for c in std:
    row = {k: c.get(k) for k in CERT_COLS}
    row["productIdCount"] = len(c.get("productIds") or [])
    cert_rows.append(row)
write_csv(os.path.join(OUTDIR, "amazon-certificates.csv"), CERT_COLS, cert_rows)

# One row per identifier: this is the grain Amazon actually keys on, and a certificate-level
# file alone cannot show a missing ASIN.
PID_COLS = ["certificateNumber", "certStatus", "issueDate", "expirationDate", "company",
            "certificationType", "amazonCode", "certificationResults",
            "idType", "idValue", "idStatus", "idPartner", "idProductName", "idProductSku"]
pid_rows = []
for c in std:
    for p in (c.get("productIds") or []):
        pid_rows.append({
            "certificateNumber": c.get("certificateNumber"),
            "certStatus": c.get("status"),
            "issueDate": c.get("issueDate"),
            "expirationDate": c.get("expirationDate"),
            "company": c.get("company"),
            "certificationType": c.get("certificationType"),
            "amazonCode": c.get("amazonCode"),
            "certificationResults": c.get("certificationResults"),
            "idType": p.get("type"),
            "idValue": p.get("value"),
            "idStatus": p.get("status"),
            "idPartner": p.get("partner"),
            "idProductName": p.get("productName"),
            "idProductSku": p.get("productSku"),
        })
write_csv(os.path.join(OUTDIR, "amazon-product-identifiers.csv"), PID_COLS, pid_rows)

GS1_COLS = ["certificateId", "program", "status", "value", "startDate", "endDate",
            "amazonCode", "pathway", "agency", "targetCountries",
            "productName", "gtin", "additionalTradeItemIds", "documentId"]
gs1_rows = []
gs1_value_by_number = {}
for doc in gs1:
    for prod in (doc.get("products") or []):
        info = prod.get("certificationInfo") or {}
        ids = prod.get("additionalTradeItemId") or []
        num = info.get("certificateId")
        gs1_value_by_number[num] = info.get("value")
        gs1_rows.append({
            "certificateId": num,
            "program": info.get("program"),
            "status": info.get("status"),
            "value": info.get("value"),
            "startDate": info.get("startDate"),
            "endDate": info.get("endDate"),
            "amazonCode": info.get("amazonCode"),
            "pathway": info.get("pathway"),
            "agency": info.get("agency"),
            "targetCountries": ", ".join(info.get("targetCountries") or []),
            "productName": prod.get("name"),
            "gtin": prod.get("gtin"),
            "additionalTradeItemIds": "; ".join(
                "%s=%s" % (i.get("type"), i.get("value")) for i in ids),
            "documentId": doc.get("id"),
        })
write_csv(os.path.join(OUTDIR, "amazon-gs1.csv"), GS1_COLS, gs1_rows)

# ------------------------------------------------------- DB reconciliation --
print("\n[8] reconciliation - delivered value vs database vs pre-CR-045 snapshot")

db_now, db_before = {}, {}
dupe_disagreements = {}
snapshot_available = False
try:
    import pyodbc
    cn = pyodbc.connect(CFG["conn"], timeout=90)
    cu = cn.cursor()
    cu.execute("SELECT Number, CertificationResults, CertificationResultsNumeric "
               "FROM Certificates")
    # 137 certificate numbers carry more than one row on TEST (Certificates.AllowsDuplicateNumber
    # permits it), so a plain number->value dict silently keeps whichever row came last. That
    # would let a genuine disagreement disappear into the last-writer-wins slot. Collect every
    # value per number instead, and treat a number whose rows disagree as its own finding.
    db_rows = 0
    seen = {}
    for num, val, num_val in cu.fetchall():
        db_rows += 1
        seen.setdefault(num, set()).add((val, num_val))
    for num, vals in seen.items():
        distinct_text = set(v for v, _ in vals)
        if len(distinct_text) > 1:
            dupe_disagreements[num] = sorted(str(v) for v in distinct_text)
        db_now[num] = sorted(vals)[0]
    print("  database: %d rows over %d distinct certificate numbers"
          % (db_rows, len(db_now)))
    if dupe_disagreements:
        print("  !! %d certificate number(s) have multiple rows that DISAGREE on "
              "Certification Results - these are ambiguous and are reported as findings:"
              % len(dupe_disagreements))
        for num, vals in list(dupe_disagreements.items())[:20]:
            print("     %s -> %s" % (num, vals))

    snap = CFG["snapshot"]
    if snap:
        cu.execute("SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = ?", snap)
        if cu.fetchone()[0]:
            cu.execute("SELECT Number, CertificationResults FROM [%s]" % snap)
            snap_rows = 0
            for num, val in cu.fetchall():
                snap_rows += 1
                db_before[num] = val
            snapshot_available = True
            print("  snapshot %s: %d rows over %d distinct numbers"
                  % (snap, snap_rows, len(db_before)))
        else:
            print("  !! snapshot table %s is gone - the before/after control is UNAVAILABLE. "
                  "This run can only show API-vs-DB agreement today." % snap)
    else:
        print("  (no pre-change snapshot configured for %s - 3-way compare only)" % TARGET)
    cn.close()
except Exception as exc:                                     # noqa: BLE001
    print("  !! database unreachable (%s) - writing the payload files anyway, but the "
          "'nothing changed' claim is NOT established by this run." % exc)

REC_COLS = ["certificateNumber", "dbValue", "snapshotValue", "standardApiValue",
            "gs1ApiValue", "dbNumericSort", "allMatch", "note"]
rec_rows, mismatches = [], []
for c in std:
    num = c.get("certificateNumber")
    delivered = c.get("certificationResults")
    gs1_val = gs1_value_by_number.get(num, "<not in GS1 payload>")
    db_val, db_num = db_now.get(num, (None, None)) if db_now else ("<db unread>", None)
    snap_val = db_before.get(num) if snapshot_available else "<no snapshot>"

    compare = [delivered, db_val]
    if num in gs1_value_by_number:
        compare.append(gs1_val)
    if snapshot_available:
        compare.append(snap_val)
    # Normalise None vs "" - an empty field and an absent field are the same to Amazon,
    # and flagging that difference would bury a real one in noise.
    norm = set((x or "") for x in compare)
    ok = bool(db_now) and len(norm) == 1 and num not in dupe_disagreements

    note = ""
    if num in dupe_disagreements:
        note = ("AMBIGUOUS - %d database rows share this number and disagree on the value: %s"
                % (len(dupe_disagreements[num]), dupe_disagreements[num]))
    elif num not in gs1_value_by_number:
        note = "certificate not present in GS1 payload"
    elif not ok:
        note = "VALUE MISMATCH"

    row = {"certificateNumber": num, "dbValue": db_val, "snapshotValue": snap_val,
           "standardApiValue": delivered, "gs1ApiValue": gs1_val,
           "dbNumericSort": db_num, "allMatch": "YES" if ok else "NO", "note": note}
    rec_rows.append(row)
    if not ok:
        mismatches.append(row)

rec_rows.sort(key=lambda r: (r["allMatch"] == "YES", r["certificateNumber"] or ""))
write_csv(os.path.join(OUTDIR, "amazon-certresults-reconciliation.csv"), REC_COLS, rec_rows)

# --------------------------------------------------- membership reconcile --
# Value-level agreement is only half the question. "Did the Amazon certs break?" also asks
# whether the RIGHT certificates are in the payload - a file where every delivered value is
# correct can still be missing certificates entirely, and the value check above would pass.
#
# The API's partner filter is (Controllers/Api/v1/CertificatesController.cs:178):
#     Amazon-tagged  AND  status in BadgeableStatuses  AND  effective date reached
# There is deliberately no expiration predicate - Expired is a status, not a date test.
# So the delivered set is predictable from the database, and any certificate that is
# tagged-and-eligible but absent (or delivered but ineligible) is a genuine finding.
print("\n[9] membership - is the RIGHT set of Amazon certificates in the payload?")

BADGEABLE = ("Client Approved - Paid", "Client Approved - Confidential", "Republished")
MEMBER_COLS = ["certificateNumber", "dbStatus", "effectiveDate", "expirationDate",
               "eligible", "delivered", "verdict"]
member_rows, member_findings = [], []
tagged_total = 0

try:
    import pyodbc
    cn = pyodbc.connect(CFG["conn"], timeout=90)
    cu = cn.cursor()
    cu.execute("""SELECT c.Number, c.Status, c.EffectiveDate, c.ExpirationDate
                  FROM Certificates c
                  JOIN CertificatePartners cp ON cp.CertificatesId = c.Id
                  JOIN Partners p ON p.Id = cp.PartnersId
                  WHERE p.Name = 'Amazon'""")
    tagged = cu.fetchall()
    cn.close()
    tagged_total = len(tagged)
    delivered_numbers = set(c.get("certificateNumber") for c in std)
    today = dt.date.today()

    for num, status, eff, exp in tagged:
        eff_reached = (eff is None) or (eff.date() if hasattr(eff, "date") else eff) <= today
        eligible = (status in BADGEABLE) and eff_reached
        delivered = num in delivered_numbers
        if eligible and delivered:
            verdict = "OK - eligible and delivered"
        elif not eligible and not delivered:
            reasons = []
            if status not in BADGEABLE:
                reasons.append("status '%s' is not badgeable" % status)
            if not eff_reached:
                reasons.append("effective date not yet reached")
            verdict = "OK - correctly withheld (%s)" % "; ".join(reasons)
        elif eligible and not delivered:
            verdict = "FINDING - eligible but MISSING from the Amazon payload"
        else:
            verdict = "FINDING - delivered although not eligible"

        row = {"certificateNumber": num, "dbStatus": status,
               "effectiveDate": eff.strftime("%Y-%m-%d") if eff else "",
               "expirationDate": exp.strftime("%Y-%m-%d") if exp else "",
               "eligible": "YES" if eligible else "NO",
               "delivered": "YES" if delivered else "NO", "verdict": verdict}
        member_rows.append(row)
        if verdict.startswith("FINDING"):
            member_findings.append(row)

    # The reverse direction matters too: a certificate in the payload that carries no Amazon
    # tag at all would never appear in the query above, so it cannot be caught by the loop.
    tagged_numbers = set(r[0] for r in tagged)
    for num in sorted(delivered_numbers - tagged_numbers):
        row = {"certificateNumber": num, "dbStatus": "", "effectiveDate": "",
               "expirationDate": "", "eligible": "NO", "delivered": "YES",
               "verdict": "FINDING - delivered to Amazon but carries no Amazon partner tag"}
        member_rows.append(row)
        member_findings.append(row)

    member_rows.sort(key=lambda r: (not r["verdict"].startswith("FINDING"),
                                    r["certificateNumber"] or ""))
    write_csv(os.path.join(OUTDIR, "amazon-membership-reconciliation.csv"),
              MEMBER_COLS, member_rows)
except Exception as exc:                                     # noqa: BLE001
    print("  !! membership check skipped - database unreachable (%s)" % exc)

# ------------------------------------------------------------------ summary --
delivered_values = {}
for c in std:
    v = c.get("certificationResults")
    delivered_values[v] = delivered_values.get(v, 0) + 1

populated = {v: n for v, n in delivered_values.items() if (v or "").strip()}
pct = {v: n for v, n in populated.items() if "%" in v}
# A bare decimal <= 1 is the shape the deleted formatter used to multiply by 100. If one is
# delivered now it publishes literally - correct under CR-045, but the client asked to be
# told about it, so it is surfaced rather than counted as an error.
bare_small = {}
for v, n in populated.items():
    try:
        if "%" not in v and 0 < float(v.strip()) <= 1:
            bare_small[v] = n
    except ValueError:
        pass
non_numeric = {v: n for v, n in populated.items()
               if "%" not in v and v not in bare_small and not v.strip()
               .replace(".", "", 1).lstrip("-").isdigit()}

lines = []
w = lines.append
w("WHAT AMAZON RECEIVES - %s (%s)" % (TARGET.upper(), BASE))
w("Retrieved %s via the Amazon partner API key." % stamp)
w("")
w("PAYLOAD")
w("  certificates delivered (standard list)  : %d" % len(std))
w("  product identifiers delivered           : %d" % len(pid_rows))
w("  GS1 documents delivered                 : %d" % len(gs1))
w("  GS1 product entries                     : %d" % len(gs1_rows))
w("")
w("CERTIFICATION RESULTS AS DELIVERED")
w("  certificates with a value               : %d" % sum(populated.values()))
w("  certificates with no value              : %d" % (len(std) - sum(populated.values())))
w("  values in XX%% form                      : %d" % sum(pct.values()))
w("  bare decimals <= 1 (publish literally)  : %d" % sum(bare_small.values()))
w("  non-numeric (Pass, Silver, etc.)        : %d" % sum(non_numeric.values()))
w("")
w("RECONCILIATION (DB now | pre-CR-045 snapshot | standard API | GS1 API)")
w("  snapshot control available              : %s"
  % ("yes - " + str(CFG["snapshot"]) if snapshot_available else "NO"))
w("  certificates compared                   : %d" % len(rec_rows))
w("  all sources identical                   : %d" % (len(rec_rows) - len(mismatches)))
w("  mismatches                              : %d" % len(mismatches))
w("  numbers with disagreeing duplicate rows : %d" % len(dupe_disagreements))
if mismatches:
    w("")
    w("  MISMATCHES - each of these is a real finding, not a rounding artefact:")
    for m in mismatches[:50]:
        w("    %-10s db=%r snap=%r std=%r gs1=%r  %s"
          % (m["certificateNumber"], m["dbValue"], m["snapshotValue"],
             m["standardApiValue"], m["gs1ApiValue"], m["note"]))
    if len(mismatches) > 50:
        w("    ... and %d more - see the reconciliation CSV." % (len(mismatches) - 50))
w("")
w("MEMBERSHIP (is the right SET of certificates in the payload?)")
w("  Amazon-tagged certificates in the DB    : %d" % tagged_total)
w("  delivered to Amazon                     : %d" % len(std))
w("  correctly withheld                      : %d"
  % sum(1 for r in member_rows if r["verdict"].startswith("OK - correctly withheld")))
withheld_by_status = {}
for r in member_rows:
    if r["verdict"].startswith("OK - correctly withheld"):
        k = r["dbStatus"] if "not badgeable" in r["verdict"] else "effective date not reached"
        withheld_by_status[k] = withheld_by_status.get(k, 0) + 1
for k, n in sorted(withheld_by_status.items(), key=lambda kv: -kv[1]):
    w("      %-34s %d" % (k, n))
w("  findings                                : %d" % len(member_findings))
if member_findings:
    w("")
    for m in member_findings[:50]:
        w("    %-10s %s" % (m["certificateNumber"], m["verdict"]))
    if len(member_findings) > 50:
        w("    ... and %d more - see the membership CSV." % (len(member_findings) - 50))
w("")
w("DISTINCT DELIVERED VALUES (every one, not a sample)")
for v, n in sorted(populated.items(), key=lambda kv: (-kv[1], str(kv[0]))):
    w("  %-14s %d" % (repr(v), n))

summary = "\n".join(lines)
with open(os.path.join(OUTDIR, "summary.txt"), "w", encoding="utf-8") as fh:
    fh.write(summary + "\n")

print("\n" + summary)
print("\nFiles written to: %s" % OUTDIR)
sys.exit(1 if (mismatches or member_findings) else 0)
