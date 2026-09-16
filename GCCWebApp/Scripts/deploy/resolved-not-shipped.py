"""Gate: every issue that is RESOLVED must be in the build. Nothing finished sits behind.

WHY THIS EXISTS
Shawn, 2026-09-15: "we seem to miss defects in deploy. If I wanted it separate, it would be in a
separate build."

A full-overwrite bundle ships the whole working tree, so the risk is not shipping too much - it is a
fix that is DONE sitting unshipped for weeks because nobody reconciled the registers against the
bundle. On 2026-09-15 the deploy log's own "what is live" section was wrong by an entire bundle, and
register entries marked fixed-locally had never reached the test environment.

WHY IT IS NOT A ONE-LINE GREP
The registers carry 25+ distinct Status phrasings for the same two states - "Fixed locally (pending
deploy)", "**FIXED - deployed 2026-09-04**", "Fixed (pending UAT / deploy)", "**FIXED 2026-08-30 -
built and verified on dev, NOT deployed.**". A naive search for "deployed" marks the NOT-deployed
ones as shipped, which is the exact failure this gate is meant to catch. So: negative phrases are
tested FIRST, and anything that cannot be classified confidently is reported as UNCLASSIFIED rather
than assumed shipped.

An UNCLASSIFIED entry is a finding, not noise. A fix whose status cannot be read is invisible to
every future reconciliation, which is how work goes missing in the first place.

THE BASELINE ("line in the sand")
The registers are not a deploy ledger - status lines are written at fix-time and frequently never
updated, so on 2026-09-15 three quarters of all entries could not be confirmed as shipped. Auditing
that backlog entry-by-entry is archaeology on defects nobody will revisit. Instead
`docs/tracking/deploy-baseline.json` records the ids accepted as shipped at a named, verified bundle.
Those ids drop out of the report, so from that point forward the gate surfaces only NEW work.

The baseline is an ACCEPTED ASSUMPTION, not a verification, and the file says so in its own text. It
is sound because a bundle is a full overwrite of the working tree: anything fixed in the repo before
the cut necessarily shipped with it. It is not sound for a fix that was never merged, or was
reverted - if one of those surfaces later, re-raise it as a new finding rather than quietly trusting
the baseline.

USAGE
    python Scripts/deploy/resolved-not-shipped.py
    python Scripts/deploy/resolved-not-shipped.py --full             # list deployed + open too
    python Scripts/deploy/resolved-not-shipped.py --show-baselined   # list what the baseline absorbed
    python Scripts/deploy/resolved-not-shipped.py --write-baseline "<bundle>" "<note>"

EXIT
    0 = nothing resolved is waiting, and every entry was classifiable
    1 = something is resolved-but-unshipped, or an entry could not be classified
"""
import io
import os
import re
import sys
import json
import datetime as dt

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))

REGISTERS = [
    ("DEF", os.path.join(ROOT, "docs", "tracking", "defect-register.md")),
    ("CR", os.path.join(ROOT, "docs", "tracking", "change-request-register.md")),
]

FULL = "--full" in sys.argv
SHOW_BASELINED = "--show-baselined" in sys.argv
WRITE_BASELINE = "--write-baseline" in sys.argv
BASELINE_PATH = os.path.join(ROOT, "docs", "tracking", "deploy-baseline.json")

baseline = {"ids": [], "bundle": None, "date": None, "note": None}
if os.path.exists(BASELINE_PATH):
    try:
        baseline = json.load(io.open(BASELINE_PATH, encoding="utf-8"))
    except ValueError as exc:
        sys.exit("FATAL: %s is not valid JSON (%s). Fix it - a broken baseline silently "
                 "un-baselines every entry and floods the report." % (BASELINE_PATH, exc))
BASELINED = set(x.upper() for x in baseline.get("ids", []))

# Headings are inconsistent too - some entries are ###, some ####. Matching a single level
# UNDERCOUNTS, which is how DEF-419 was proposed when the register was already at DEF-424.
HEADING = re.compile(r"^#{2,5}\s+((?:DEF|CR)-\d+)\b(.*)$", re.I)
STATUS = re.compile(r"^\s*[-*]\s*\*\*Status:?\*\*:?\s*(.+)$", re.I)

# Order matters. Every NEGATIVE pattern is tested before any positive one, because most of the
# not-deployed phrasings contain the word "deployed".
PENDING = [
    r"not\s+(?:yet\s+)?deployed", r"pending\s+deploy", r"pending\s+uat",
    r"awaiting\s+(?:deploy|upload)", r"not\s+uploaded", r"not\s+in\s+the\b.*\bdeploy",
    r"fixed\s+locally(?!\s*\((?:deployed|shipped))", r"built,?\s+(?:but\s+)?not\s+deployed",
    r"bundled\s+for\s+deploy", r"ready\s+to\s+deploy", r"needs\s+deploy",
]
DEPLOYED = [
    r"\bdeployed\b", r"\bshipped\b", r"\blive\b", r"verified\s+on\s+gccwebapp",
    r"in\s+production", r"released",
]
NOT_A_FIX = [
    r"\bopen\b", r"\braised\b", r"not\s+fixed", r"\bbacklog\b", r"\bdeferred\b",
    r"\bwad\b", r"working\s+as\s+designed", r"\bretracted\b", r"no\s+code\s+change",
    r"\bclosed\b.*no\s+(?:code|change)", r"\bduplicate\b", r"\bwon'?t\s+fix\b",
    r"\bparked\b", r"\bblocked\b", r"needs\s+a?\s*decision",
]


def classify(status):
    s = status.lower()
    for p in PENDING:
        if re.search(p, s):
            return "PENDING"
    for p in NOT_A_FIX:
        if re.search(p, s):
            return "NOT-A-FIX"
    for p in DEPLOYED:
        if re.search(p, s):
            return "DEPLOYED"
    # A bare "Fixed." with nothing else says the work is done and says nothing about where it is.
    # That is precisely the ambiguity this gate exists to surface - never resolve it optimistically.
    if re.search(r"\bfixed\b|\bresolved\b|\bcomplete\b|\bdone\b", s):
        return "UNCLASSIFIED"
    return "UNCLASSIFIED"


def strip_md(text):
    return re.sub(r"[*`~]", "", text).strip()


rows = []
for kind, path in REGISTERS:
    if not os.path.exists(path):
        print("  !! register missing: %s" % path)
        continue
    lines = io.open(path, encoding="utf-8", errors="replace").read().splitlines()
    current, title, seen_status = None, "", False
    for ln in lines:
        h = HEADING.match(ln)
        if h:
            if current and not seen_status:
                # An entry with no Status line at all is worse than an unreadable one.
                rows.append((current, strip_md(title)[:70], "NO-STATUS", ""))
            current = h.group(1).upper()
            title = h.group(2)
            seen_status = False
            continue
        if current and not seen_status:
            m = STATUS.match(ln)
            if m:
                st = strip_md(m.group(1))
                rows.append((current, strip_md(title)[:70], classify(st), st[:90]))
                seen_status = True
    if current and not seen_status:
        rows.append((current, strip_md(title)[:70], "NO-STATUS", ""))

# The same id can appear more than once (second-pass entries, status-sync notes). The LAST
# occurrence in the file is the current state; earlier ones are history.
latest = {}
for r in rows:
    latest[r[0]] = r
rows = sorted(latest.values(), key=lambda r: (r[0].split("-")[0], int(r[0].split("-")[1])))

BLOCKING_KINDS = ("PENDING", "UNCLASSIFIED", "NO-STATUS")

# Re-bucket anything the baseline absorbed. Done AFTER classification, never during, so the
# baseline can be deleted at any time and the raw picture comes straight back.
rows = [(i, t, ("BASELINED" if (i in BASELINED and k in BLOCKING_KINDS) else k), s)
        for (i, t, k, s) in rows]

buckets = {}
for r in rows:
    buckets.setdefault(r[2], []).append(r)

if WRITE_BASELINE:
    argv = [a for a in sys.argv[1:] if not a.startswith("--")]
    bundle = argv[0] if argv else "(unnamed bundle)"
    note = argv[1] if len(argv) > 1 else ""
    # Re-derive from the RAW classification, ignoring any existing baseline, so re-running this
    # extends the line rather than baselining an already-baselined set into nothing.
    raw = [r for r in rows if r[2] in BLOCKING_KINDS or r[2] == "BASELINED"]
    ids = sorted(set(r[0] for r in raw),
                 key=lambda x: (x.split("-")[0], int(x.split("-")[1])))
    payload = {
        "_what_this_is": (
            "Ids accepted as SHIPPED as of the bundle named below - a line in the sand, not an "
            "entry-by-entry verification. Sound because a bundle is a full overwrite of the working "
            "tree, so anything fixed in the repo before the cut shipped with it. NOT sound for a fix "
            "never merged or later reverted; if one surfaces, re-raise it as a new finding rather "
            "than trusting this file."),
        "_how_to_use": (
            "resolved-not-shipped.py drops these ids from its report so only NEW work surfaces. "
            "Delete this file to get the unfiltered picture back. Do not hand-edit to silence a "
            "finding - that is the failure this gate exists to prevent."),
        "baselined_at": dt.datetime.now().strftime("%Y-%m-%d %H:%M"),
        "bundle": bundle,
        "note": note,
        "count": len(ids),
        "ids": ids,
    }
    io.open(BASELINE_PATH, "w", encoding="utf-8").write(
        json.dumps(payload, indent=2) + "\n")
    print("wrote baseline: %s" % BASELINE_PATH)
    print("  bundle : %s" % bundle)
    print("  ids    : %d" % len(ids))
    print("\nRe-run without --write-baseline to see the post-baseline picture.")
    sys.exit(0)

print("=" * 100)
print("  RESOLVED-BUT-NOT-SHIPPED GATE")
print("  Registers: defect-register.md, change-request-register.md")
print("=" * 100)
print()
if BASELINED:
    print("  baseline: %s  (%s, %d ids)"
          % (baseline.get("bundle"), baseline.get("baselined_at"), len(BASELINED)))
    if baseline.get("note"):
        print("            %s" % baseline["note"])
    print()
for name in ("PENDING", "UNCLASSIFIED", "NO-STATUS", "BASELINED", "DEPLOYED", "NOT-A-FIX"):
    if name == "BASELINED" and not BASELINED:
        continue
    print("  %-14s %d" % (name, len(buckets.get(name, []))))
print("  %-14s %d" % ("TOTAL", len(rows)))
print()

blocking = buckets.get("PENDING", []) + buckets.get("UNCLASSIFIED", []) + buckets.get("NO-STATUS", [])

if SHOW_BASELINED:
    print("-" * 100)
    print("  ABSORBED BY THE BASELINE - accepted as shipped, not individually verified")
    print("-" * 100)
    for ident, title, _, st in buckets.get("BASELINED", []):
        print("  %-9s %s" % (ident, title))
    print()

if buckets.get("PENDING"):
    print("-" * 100)
    print("  RESOLVED AND WAITING - these MUST be in the next build")
    print("-" * 100)
    for ident, title, _, st in buckets["PENDING"]:
        print("  %-9s %s" % (ident, title))
        print("            status: %s" % st)
    print()

for label, key, note in (
    ("STATUS CANNOT BE READ - treat as unshipped until confirmed", "UNCLASSIFIED",
     "A fix whose status is unreadable is invisible to every future reconciliation."),
    ("NO STATUS LINE AT ALL", "NO-STATUS",
     "Cannot be reconciled by anything. Give it a status line."),
):
    if buckets.get(key):
        print("-" * 100)
        print("  %s" % label)
        print("  %s" % note)
        print("-" * 100)
        for ident, title, _, st in buckets[key]:
            print("  %-9s %s" % (ident, title))
            if st:
                print("            status: %s" % st)
        print()

if FULL:
    for key in ("DEPLOYED", "NOT-A-FIX"):
        print("-" * 100)
        print("  %s" % key)
        print("-" * 100)
        for ident, title, _, st in buckets.get(key, []):
            print("  %-9s %-70s %s" % (ident, title, st[:60]))
        print()

print("=" * 100)
if blocking:
    print("  GATE: ATTENTION - %d entr%s resolved-but-unshipped or unclassifiable."
          % (len(blocking), "y is" if len(blocking) == 1 else "ies are"))
    print("  Every one must be in the build, or named in the runbook with a reason it is not.")
    print("  \"If I wanted it separate, it would be in a separate build.\" (Shawn, 2026-09-15)")
else:
    print("  GATE: PASS - nothing resolved is waiting, and every entry was classifiable.")
print("=" * 100)
sys.exit(1 if blocking else 0)
