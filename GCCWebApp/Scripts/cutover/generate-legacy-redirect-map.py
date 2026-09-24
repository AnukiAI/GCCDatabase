#!/usr/bin/env python3
"""
Generate the IIS URL Rewrite map that keeps legacy db.greencirclecertified.com deep links
working after the go-live cutover.

WHY THIS EXISTS
---------------
At cutover the `db.` A record moves from Vercel (the legacy public certificate database) to
the GCC application. DNS has no partial state: the instant that record moves, every link in
circulation to the legacy site stops working unless a redirect map ships with it.

The legacy URLs are keyed by an opaque INTEGER record id, which the new system does not know
about -- it keys on certificate NUMBER. The mapping is therefore data, not an algorithm, and
it comes from the 2026-08-13 credentialed portal pull.

    legacy:  https://db.greencirclecertified.com/api/certificates/1/download
    new:     https://db.greencirclecertified.com/Certificate/CertificateSingleView/20-0245

SOURCE
------
C:\\Users\\swest\\GCCPortalPull\\manifest.csv -- 6,000 rows from the credentialed portal pull,
NOT the 1,021-row public mirror. The portal manifest is the only source that covers records
which were never public.

OUTPUT
------
rewritemaps.config at the repo root, referenced from web.config as:

    <rewriteMaps configSource="rewritemaps.config" />

It MUST end up in the publish output. If IIS cannot find a file named by configSource it
raises a configuration error and returns 500 on EVERY path -- the same total-outage shape as
DEF-363. `Scripts/cutover/verify-redirect-map.py` asserts it is in the bundle; run that before
any deploy that carries this.

USAGE
-----
    python Scripts/cutover/generate-legacy-redirect-map.py
    python Scripts/cutover/generate-legacy-redirect-map.py --manifest <path> --out <path>

Prints a validation report and exits non-zero if the source cannot support a redirect map.
"""

import argparse
import collections
import csv
import io
import os
import re
import sys
from xml.sax.saxutils import quoteattr

DEFAULT_MANIFEST = r"C:\Users\swest\GCCPortalPull\manifest.csv"
DEFAULT_OUT = "rewritemaps.config"
MAP_NAME = "LegacyCertIds"

# The legacy download URL shape, used to prove the manifest describes the URLs we think it does.
DOWNLOAD_URL_RE = re.compile(
    r"^https://db\.greencirclecertified\.com/api/certificates/(\d+)/download$"
)
# What the NEW app can actually route, not what certificate numbers usually look like.
#
# `CertificateSingleView(string id, ...)` takes a bare string off the default
# {controller}/{action}/{id?} route -- it carries NO route constraint. (The constraint
# ^[0-9]+-[0-9A-Za-z]+$ in Program.cs applies to Certificate/Edit and Certificate/Delete,
# which this map never targets.)
#
# So the only thing to guard against here is a value that would break the URL, not one that
# breaks a house style. An earlier version of this file used ^[0-9]{2}-[0-9A-Za-z]+$ and
# silently dropped three REAL certificates -- 6H3-5692, 6H3-7120 and 6H3-6301, all ASSA ABLOY,
# all Expired -- which the app serves perfectly well. Validate against the router, not against
# the pattern you expect the data to follow.
CERT_NUMBER_RE = re.compile(r"^[0-9A-Za-z][0-9A-Za-z._\-]*$")


def load(manifest_path):
    with io.open(manifest_path, encoding="utf-8-sig", newline="") as fh:
        return list(csv.DictReader(fh))


def validate(rows):
    """Return (mapping, report). Raises SystemExit if the source cannot support a map."""
    report = collections.OrderedDict()
    report["rows parsed"] = len(rows)

    if not rows:
        sys.exit("FATAL: manifest is empty")

    for col in ("record_id", "certificate_number", "download_url"):
        if col not in rows[0]:
            sys.exit("FATAL: manifest has no '%s' column" % col)

    ids = [r["record_id"].strip() for r in rows]
    nums = [r["certificate_number"].strip() for r in rows]

    report["distinct record_id"] = len(set(ids))
    report["distinct cert numbers"] = len(set(nums))
    report["blank record_id"] = sum(1 for x in ids if not x)
    report["non-numeric record_id"] = sum(1 for x in ids if not x.isdigit())

    # A redirect needs record_id -> number to be a FUNCTION. Many numbers legitimately map to
    # several record ids (one certificate, several documents); the reverse would be fatal.
    fan = collections.defaultdict(set)
    for r in rows:
        fan[r["record_id"].strip()].add(r["certificate_number"].strip())
    ambiguous = {k: sorted(v) for k, v in fan.items() if len(v) > 1}
    report["AMBIGUOUS record_id"] = len(ambiguous)

    # Confirm the manifest really describes the URL shape we are mapping.
    off_shape = [r["download_url"].strip() for r in rows
                 if not DOWNLOAD_URL_RE.match(r["download_url"].strip())]
    report["download_url off-shape"] = len(off_shape)

    # Rows we cannot map, enumerated rather than counted -- a tally hides which ones.
    unmappable = []
    mapping = {}
    for r in rows:
        rid = r["record_id"].strip()
        num = r["certificate_number"].strip()
        if not rid.isdigit():
            unmappable.append((rid, num, "record_id not numeric"))
            continue
        if not num:
            unmappable.append((rid, num, "no certificate number"))
            continue
        if not CERT_NUMBER_RE.match(num):
            unmappable.append((rid, num, "certificate number fails %s" % CERT_NUMBER_RE.pattern))
            continue
        mapping[rid] = num

    report["MAPPABLE entries"] = len(mapping)
    report["unmappable rows"] = len(unmappable)

    fatal = []
    if report["blank record_id"]:
        fatal.append("blank record_id present")
    if report["AMBIGUOUS record_id"]:
        fatal.append("record_id -> certificate_number is not a function")
    if report["download_url off-shape"]:
        fatal.append("download_url does not match the shape this map redirects")

    return mapping, report, unmappable, ambiguous, fatal


def emit(mapping, out_path, manifest_path):
    lines = [
        '<?xml version="1.0" encoding="utf-8"?>',
        "<!--",
        "    GENERATED FILE - DO NOT EDIT BY HAND.",
        "    Regenerate:  python Scripts/cutover/generate-legacy-redirect-map.py",
        "",
        "    Legacy deep-link map for the go-live cutover. Maps the legacy public database's",
        "    opaque integer record_id to the certificate NUMBER the new application routes on,",
        "    so links printed, emailed and bookmarked against the old site keep working.",
        "",
        "    Source   : %s" % manifest_path,
        "    Entries  : %d" % len(mapping),
        "    Consumed : web.config -> <rewrite><rewriteMaps configSource=\"rewritemaps.config\" />",
        "",
        "    An id that is absent resolves to the map's empty defaultValue, the rule's condition",
        "    fails, and the request falls through to a normal 404. That is deliberate: a clean",
        "    404 beats a redirect to a certificate that is not the one the link meant.",
        "-->",
        "<rewriteMaps>",
        '  <rewriteMap name="%s" defaultValue="">' % MAP_NAME,
    ]
    for rid in sorted(mapping, key=int):
        lines.append("    <add key=%s value=%s />"
                     % (quoteattr(rid), quoteattr(mapping[rid])))
    lines += ["  </rewriteMap>", "</rewriteMaps>", ""]

    with io.open(out_path, "w", encoding="utf-8", newline="\r\n") as fh:
        fh.write("\n".join(lines))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--manifest", default=DEFAULT_MANIFEST)
    ap.add_argument("--out", default=DEFAULT_OUT)
    ap.add_argument("--list-unmappable", action="store_true",
                    help="print every unmappable row rather than the first few")
    args = ap.parse_args()

    if not os.path.exists(args.manifest):
        sys.exit("FATAL: manifest not found: %s" % args.manifest)

    rows = load(args.manifest)
    mapping, report, unmappable, ambiguous, fatal = validate(rows)

    print("Source: %s" % args.manifest)
    for k, v in report.items():
        print("  %-24s %s" % (k, v))

    if ambiguous:
        print("\nAMBIGUOUS record_ids (each would redirect to the wrong certificate):")
        for rid, nums in list(ambiguous.items())[:20]:
            print("  %-8s -> %s" % (rid, ", ".join(nums)))

    if unmappable:
        print("\nUnmappable rows -- these fall through to 404:")
        shown = unmappable if args.list_unmappable else unmappable[:10]
        for rid, num, why in shown:
            print("  record_id %-8s number %-12r %s" % (rid, num, why))
        if len(unmappable) > len(shown):
            print("  ... %d more (use --list-unmappable)" % (len(unmappable) - len(shown)))

    if fatal:
        print("\nFATAL -- refusing to emit a map:")
        for f in fatal:
            print("  - %s" % f)
        sys.exit(2)

    emit(mapping, args.out, args.manifest)
    size = os.path.getsize(args.out)
    print("\nWROTE %s  (%d entries, %.1f KB)" % (args.out, len(mapping), size / 1024.0))
    print("\nNEXT: the certificate numbers in this map have NOT been checked against the live")
    print("      system. Run Scripts/cutover/verify-redirect-map.py before relying on it --")
    print("      a map entry pointing at a number the new database does not hold sends the")
    print("      visitor to a 404 that looks like the cutover lost their certificate.")


if __name__ == "__main__":
    main()
