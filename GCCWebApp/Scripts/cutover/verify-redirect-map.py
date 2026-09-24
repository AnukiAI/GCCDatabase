#!/usr/bin/env python3
r"""
Verify the legacy deep-link redirect map. Three independent checks, because each one can
pass while another fails, and each catches a different way this goes wrong.

  --bundle <zip>        Is rewritemaps.config actually IN the deploy zip, and does web.config
                        reference it? If the file is missing, IIS cannot resolve the
                        configSource and returns 500 on EVERY path -- not just redirects.
                        This is the pre-deploy gate.

  --live <base-url>     Do the rules actually redirect? Samples real legacy URLs and asserts
                        a 301 to the expected target. Rewrite rules run at the IIS layer
                        BEFORE the application, so this works even while the test gate is on.

  --targets <csv>       Do the map's certificate numbers EXIST in the new system? A map entry
                        pointing at a number the database does not hold sends the visitor to
                        a 404 that reads like the cutover lost their certificate. Compares
                        key SETS in BOTH directions -- equal counts are not the same rows.

Examples
--------
  python Scripts/cutover/verify-redirect-map.py --bundle deployment-drops/GCCWebApp-deploy-*.zip
  python Scripts/cutover/verify-redirect-map.py --live https://gccwebapp.com --sample 40
  python Scripts/cutover/verify-redirect-map.py --targets live-cert-numbers.csv

Exit code is 0 only if every requested check passed.
"""

import argparse
import csv
import glob
import io
import os
import random
import re
import sys
import urllib.request
import xml.etree.ElementTree as ET
import zipfile

MAP_FILE = "rewritemaps.config"
CANONICAL = "https://db.greencirclecertified.com"


def load_map(path=MAP_FILE):
    root = ET.parse(path).getroot()
    out = {}
    for add in root.iter("add"):
        out[add.get("key")] = add.get("value")
    if not out:
        sys.exit("FATAL: %s contains no entries" % path)
    return out


# ---------------------------------------------------------------- bundle
def check_bundle(zip_path, mapping):
    print("\n=== BUNDLE: %s ===" % os.path.basename(zip_path))
    ok = True
    with zipfile.ZipFile(zip_path) as z:
        names = set(z.namelist())

        if MAP_FILE not in names:
            print("  FAIL  %s is NOT in the bundle." % MAP_FILE)
            print("        IIS will fail to resolve the configSource and return 500 on EVERY")
            print("        path. Check the <Content Update> in GCCWebApp.csproj.")
            return False
        print("  pass  %s present" % MAP_FILE)

        shipped = z.read(MAP_FILE).decode("utf-8")
        n = shipped.count("<add key=")
        if n != len(mapping):
            print("  FAIL  bundle has %d entries, working copy has %d -- the bundle was cut "
                  "from a different map" % (n, len(mapping)))
            ok = False
        else:
            print("  pass  %d entries, matches the working copy" % n)

        if "web.config" not in names:
            print("  FAIL  no web.config in the bundle")
            return False
        wc = z.read("web.config").decode("utf-8")

        for needle, label in (
            ('configSource="rewritemaps.config"', "web.config references the map"),
            ("LegacyCertIds", "the rewrite rule reads the map"),
            ("Legacy deep link - certificate download by record id", "download rule present"),
            ("Legacy deep link - certificate listing", "listing rule present"),
        ):
            if needle in wc:
                print("  pass  %s" % label)
            else:
                print("  FAIL  %s -- %r absent from the shipped web.config" % (label, needle))
                ok = False
    return ok


# ---------------------------------------------------------------- live
class NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, *a, **k):
        return None


def check_live(base, mapping, sample):
    print("\n=== LIVE: %s ===" % base)
    opener = urllib.request.build_opener(NoRedirect)
    ok = True

    keys = sorted(mapping, key=int)
    picked = keys[:3] + random.sample(keys, min(sample, len(keys))) + keys[-3:]
    picked = list(dict.fromkeys(picked))

    failures = []
    for rid in picked:
        url = "%s/api/certificates/%s/download" % (base.rstrip("/"), rid)
        want = "%s/Certificate/CertificateSingleView/%s" % (CANONICAL, mapping[rid])
        try:
            r = opener.open(url, timeout=25)
            code, loc = r.getcode(), r.headers.get("Location", "")
        except urllib.error.HTTPError as e:
            code, loc = e.code, e.headers.get("Location", "")
        except Exception as e:
            failures.append((rid, "ERROR", str(e)[:70])); continue
        if code != 301 or loc != want:
            failures.append((rid, code, loc or "(no Location)"))

    print("  sampled %d ids (first 3, last 3, %d random)" % (len(picked), sample))
    if failures:
        ok = False
        print("  FAIL  %d of %d did not 301 to the expected target:" % (len(failures), len(picked)))
        for rid, code, loc in failures[:15]:
            print("        id %-6s -> %s  %s" % (rid, code, loc))
            print("          expected: %s/Certificate/CertificateSingleView/%s" % (CANONICAL, mapping[rid]))
        if len(failures) > 15:
            print("        ... %d more" % (len(failures) - 15))
    else:
        print("  pass  every sampled id 301s to its certificate")

    # An id that is NOT in the map must fall through to a 404, never redirect somewhere wrong.
    absent = str(max(int(k) for k in mapping) + 10_000)
    url = "%s/api/certificates/%s/download" % (base.rstrip("/"), absent)
    try:
        r = opener.open(url, timeout=25); code = r.getcode()
    except urllib.error.HTTPError as e:
        code = e.code
    except Exception:
        code = None
    if code == 301:
        print("  FAIL  unmapped id %s returned a 301 -- the defaultValue guard is not working" % absent)
        ok = False
    else:
        print("  pass  unmapped id %s does not redirect (got %s)" % (absent, code))

    # /certificate -> the public tile view
    try:
        r = opener.open(base.rstrip("/") + "/certificate", timeout=25)
        code, loc = r.getcode(), r.headers.get("Location", "")
    except urllib.error.HTTPError as e:
        code, loc = e.code, e.headers.get("Location", "")
    want = CANONICAL + "/Certificate/CertificateTileView"
    if code == 301 and loc == want:
        print("  pass  /certificate 301s to the tile view")
    else:
        print("  FAIL  /certificate -> %s %s (expected 301 %s)" % (code, loc, want))
        ok = False
    return ok


# ---------------------------------------------------------------- targets
def check_targets(csv_path, mapping):
    """Compare the map's certificate numbers against the live system's, BOTH directions."""
    print("\n=== TARGETS vs %s ===" % os.path.basename(csv_path))
    with io.open(csv_path, encoding="utf-8-sig", newline="") as fh:
        sniff = fh.read(4096); fh.seek(0)
        rdr = csv.reader(fh)
        rows = list(rdr)
    if not rows:
        sys.exit("FATAL: %s is empty" % csv_path)

    # Accept either a bare one-column list or a file with a certificate-number column.
    header = [c.strip().lower() for c in rows[0]]
    col = 0
    start = 0
    for cand in ("number", "certificate_number", "certificatenumber", "cert number"):
        if cand in header:
            col = header.index(cand); start = 1; break
    else:
        if not re.match(r"^[0-9A-Za-z]", rows[0][0].strip()):
            start = 1
    live = {r[col].strip() for r in rows[start:] if r and r[col].strip()}

    mapped = set(mapping.values())
    missing = sorted(mapped - live)      # map points at a number the system does not have
    unreferenced = sorted(live - mapped)  # system has certs no legacy link points at (expected)

    print("  map distinct targets : %d" % len(mapped))
    print("  live certificate nums: %d" % len(live))
    print("  in map, NOT live     : %d   <-- each of these 404s a real legacy link" % len(missing))
    print("  live, not in map     : %d   (expected -- certificates created after the pull)"
          % len(unreferenced))

    if missing:
        print("\n  The %d unmatched targets, listed rather than counted:" % len(missing))
        for n in missing[:60]:
            ids = sorted((k for k, v in mapping.items() if v == n), key=int)
            print("    %-14s <- legacy record_id %s" % (n, ", ".join(ids[:6])))
        if len(missing) > 60:
            print("    ... %d more" % (len(missing) - 60))
        return False
    print("  pass  every map target exists in the live system")
    return True


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--map", default=MAP_FILE)
    ap.add_argument("--bundle")
    ap.add_argument("--live")
    ap.add_argument("--targets")
    ap.add_argument("--sample", type=int, default=25)
    args = ap.parse_args()

    if not (args.bundle or args.live or args.targets):
        ap.error("choose at least one of --bundle / --live / --targets")

    if not os.path.exists(args.map):
        sys.exit("FATAL: %s not found -- run generate-legacy-redirect-map.py first" % args.map)
    mapping = load_map(args.map)
    print("Working map: %s (%d entries)" % (args.map, len(mapping)))

    results = []
    if args.bundle:
        hits = sorted(glob.glob(args.bundle))
        if not hits:
            sys.exit("FATAL: no bundle matched %s" % args.bundle)
        results.append(check_bundle(hits[-1], mapping))
    if args.live:
        results.append(check_live(args.live, mapping, args.sample))
    if args.targets:
        results.append(check_targets(args.targets, mapping))

    print("\n%s" % ("ALL CHECKS PASSED" if all(results) else "*** FAILURES ABOVE ***"))
    sys.exit(0 if all(results) else 1)


if __name__ == "__main__":
    main()
