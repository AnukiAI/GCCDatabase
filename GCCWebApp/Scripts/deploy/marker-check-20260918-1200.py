"""Content markers for GCCWebApp-deploy-20260918-1200.zip — the FINAL bundle, re-cut.

verify-bundle.py carries markers up to DEF-409/CR-045. This bundle's five items are newer than
that, so nothing in the standing gate would notice if one of them were missing. Two bundles have
already been observed with identical entry counts and sizes while one lacked a whole fix, so
"the zip looks right" is not evidence.

Method, copied from verify-bundle.py deliberately: assert against IN-ZIP BYTES, and include a
CONTROL marker that must be present and a NONSENSE marker that must be absent, so a broken
harness fails loudly instead of passing everything.

Usage: python Scripts/deploy/marker-check-20260918.py <path-to-zip>
"""
import sys, zipfile

if len(sys.argv) < 2:
    sys.exit("usage: marker-check-20260918.py <zip>")
zp = sys.argv[1]
z = zipfile.ZipFile(zp)
names = set(z.namelist())

def blob(path):
    return z.read(path) if path in names else None


def contains(hay, needle):
    """Razor VIEW MARKUP is stored in the compiled DLL as UTF-16LE, not UTF-8.

    Found the hard way: `status-content` and `zoom-allpages` both reported ABSENT against a bundle
    that demonstrably contains them, because the marker was compared as UTF-8 bytes. A marker check
    that cannot see view markup silently passes every view-only change. Search both encodings.
    """
    return needle in hay or needle.decode().encode("utf-16-le") in hay

DLL = "GCCWebApp.dll"

# (label, file, marker, must_be_present)
CHECKS = [
    # --- harness self-test -------------------------------------------------
    ("CONTROL  the DLL is readable at all",            DLL, b"GCCWebApp", True),
    ("CONTROL  a nonsense marker is NOT present",      DLL, b"ZZ_this_string_should_never_exist_42", False),

    # --- CR-051 status filter (internal listing only) ----------------------
    ("CR-051  status filter panel in the internal view", DLL, b"status-content", True),
    ("CR-051  status filter wired in the internal JS",
     "wwwroot/js/certificate-tile-view-internal.js", b"'status': 'status'", True),
    # The external view's ABSENCE of a status panel cannot be asserted from the DLL: both tile
    # views compile into the same assembly, so a grep cannot tell which one a marker came from.
    # It is covered at runtime instead — verify-def430-load-failure.js drives the external view as
    # an External-scoped user and has to fall back to the cert-type filter because no status panel
    # exists there. Asserting it here would be theatre.

    # --- CR-052 / CR-053 preview popup + pager -----------------------------
    ("CR-053  shared pager script ships, with real content",
     "wwwroot/js/certificate-preview-pager.js", b"zoom-pager", True),
    ("CR-053  page-count route compiled in",           DLL, b"CertificatePageCount", True),
    ("CR-053  pager styles ship",                      "wwwroot/css/main.css", b"zoom-pager", True),
    ("CR-052  'View all pages' link still in the popup", DLL, b"zoom-allpages", True),

    # --- DEF-430 tile-view load failure handling ---------------------------
    ("DEF-430 failure panel class (internal)",
     "wwwroot/js/certificate-tile-view-internal.js", b"tile-load-error", True),
    ("DEF-430 failure panel class (external)",
     "wwwroot/js/certificate-tile-view-external.js", b"tile-load-error", True),
    ("DEF-430 fetch timeout (internal)",
     "wwwroot/js/certificate-tile-view-internal.js", b"AbortSignal.timeout(30000)", True),
    ("DEF-430 fetch timeout (external)",
     "wwwroot/js/certificate-tile-view-external.js", b"AbortSignal.timeout(30000)", True),
    ("DEF-430 failure panel styles ship",
     "wwwroot/css/certificatetileview.css", b".tile-load-error", True),
    ("DEF-430 no modal-per-retry (internal)",
     "wwwroot/js/certificate-tile-view-internal.js", b"Connection failed. Retrying in", False),

    # --- DEF-418 import help copy (QC #263) --------------------------------
    # The client rejected the 2026-09-10 fix because it landed only on the standalone import PAGE,
    # which nothing links to. The surface she uses is the MODAL on the internal listing. Both
    # compile into the same DLL, so assert a phrase UNIQUE TO EACH — otherwise one marker would
    # pass on the strength of the other file and we would re-ship the same miss.
    ("DEF-418 modal copy (unique to the listing MODAL)",
     DLL, b"the column names the importer expects", True),
    ("DEF-418 page copy (unique to the import PAGE)",
     DLL, b"the column names described on this page", True),
    ("DEF-418 'Certificate # is not required' ships",
     DLL, b"Certificate # is not required.", True),


    # --- re-cut 2026-09-18 12:00: DEF-436 / DEF-434 / DEF-432 --------------
    ("DEF-436 page sizes capped to the thumbnail box",
     "wwwroot/css/main.css", b"min(85vh, 620px)", True),
    ("DEF-436 old flat 85vh cap is gone", "wwwroot/css/main.css", b"max-height: 85vh;", False),
    ("DEF-434 arrow keys skip text-entry targets",
     "wwwroot/js/certificate-preview-pager.js", b"isTextEntry", True),
    # DEF-432 is a C# METHOD CALL — it compiles to IL with no string literal, so there is nothing to
    # grep for in the DLL. Deliberately NOT asserted here rather than asserted weakly: it was
    # verified at runtime instead (Cache-Control "private, no-cache, must-revalidate" observed on
    # /Certificate/CertificatePageCount, matching its sibling). Recorded so the absence of a marker
    # is not mistaken for an oversight.
    # --- DEF-431 hydrate change --------------------------------------------
    # No string literal changed, so there is nothing to grep for. Asserting the migration that was
    # REVERTED is absent is the check that actually matters here: if it came back, this bundle would
    # carry a schema change we have declared it does not have.
    ("DEF-431 reverted covering index is NOT in the build",
     DLL, b"IX_CertificateProductIds_CertificateId_ProductIdType_IsActive", False),
]

failures, missing_files = 0, 0
print("=== CONTENT MARKERS — 2026-09-18 final bundle ===")
print("  zip: %s\n" % zp)
for label, path, marker, want in CHECKS:
    b = blob(path)
    if b is None:
        print("  [FAIL] %-52s file not in zip: %s" % (label, path))
        failures += 1; missing_files += 1
        continue
    found = contains(b, marker)
    ok = (found == want)
    print("  [%s] %-52s %s (expected %s)" % (
        "PASS" if ok else "FAIL", label,
        "PRESENT" if found else "ABSENT", "present" if want else "absent"))
    if not ok:
        failures += 1

print("\n  files checked: %d  |  failures: %d" % (len(CHECKS) - missing_files, failures))
sys.exit(1 if failures else 0)
