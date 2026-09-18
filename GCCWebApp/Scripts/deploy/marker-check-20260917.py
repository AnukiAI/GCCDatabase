"""Marker check for GCCWebApp-deploy-20260917-1545.zip (CR-051, CR-052 reworked, DEF-418).

Razor views compile INTO GCCWebApp.dll, so view copy is asserted against the DLL bytes, never
against files on disk. A .NET assembly keeps type/member names in the UTF-8 #Strings heap and
string literals in the UTF-16LE #US heap, so every marker is tested in both encodings -- an
ASCII-only search reports ABSENT for a change that did ship.

Includes a control that must be PRESENT and a nonsense marker that must be ABSENT, so a
silently-broken search cannot read as a pass.

Usage: python Scripts/deploy/marker-check-20260917.py deployment-drops/GCCWebApp-deploy-20260917-1545.zip
"""
import sys
import zipfile

ZIP = sys.argv[1] if len(sys.argv) > 1 else "deployment-drops/GCCWebApp-deploy-20260917-1545.zip"

# (ref, label, in-zip file, marker, must_be_present)
CHECKS = [
    ("control", "type name compiled in", "GCCWebApp.dll", "CertificateController", True),
    ("control", "nonsense marker", "GCCWebApp.dll", "ZZ_NOT_IN_THIS_BUILD_MARKER", False),

    ("CR-051", "status panel id", "GCCWebApp.dll", 'id="status-content"', True),
    ("CR-051", "panel header binding", "GCCWebApp.dll", 'data-filter="status"', True),
    ("CR-051", "stored display values, not enum names", "GCCWebApp.dll", "GetAllStatuses", True),

    ("CR-052", "route is wired", "GCCWebApp.dll", "ViewCertificateFile", True),
    ("CR-052", "popup control markup", "GCCWebApp.dll", "zoom-allpages-link", True),
    ("CR-052", "popup control label", "GCCWebApp.dll", "View all pages", True),
    ("CR-052", "caption corrected", "GCCWebApp.dll", "Click outside to close", True),
    # The Edit green-bar duplicate was REMOVED 2026-09-17. Its title string was unique to it,
    # so its absence is the assertion that the removal shipped.
    ("CR-052", "removed Edit green-bar button", "GCCWebApp.dll", "Open the PDF to view all pages", False),
    ("CR-052", "shared popup styling", "wwwroot/css/main.css", ".zoom-allpages-link", True),
    ("CR-052", "Create blob link", "wwwroot/js/certificate-create.js", "createObjectURL", True),
    ("CR-052", "Create blob link is revoked", "wwwroot/js/certificate-create.js", "revokeObjectURL", True),

    ("DEF-418", "modal copy corrected", "GCCWebApp.dll", "Certificate # is not required", True),
    ("DEF-418", "modal edit instruction", "GCCWebApp.dll", "To edit an existing certificate", True),
    ("DEF-418", "documented template column", "GCCWebApp.dll", "Cert Subcategories", True),
    ("DEF-418", "old wrong requirement line", "GCCWebApp.dll", "Required:</strong> CERTIFICATE_NUMBER", False),
]

# NOT asserted, deliberately: "Click anywhere to close" still ships. Create.cshtml's popup keeps
# that wording and is correct -- it has no button in it. Asserting its absence would fail on a
# file that was never meant to change.


def main():
    z = zipfile.ZipFile(ZIP)
    cache = {}
    failures = []
    print(f"=== MARKER CHECK: {ZIP} ===\n")
    for ref, label, path, marker, want in CHECKS:
        if path not in cache:
            cache[path] = z.read(path)
        data = cache[path]
        found = marker.encode("utf-8") in data or marker.encode("utf-16-le") in data
        ok = found == want
        state = "PRESENT" if found else "ABSENT"
        expect = "expected present" if want else "expected absent"
        print(f"  [{'PASS' if ok else 'FAIL'}] {ref:<8} {label:<38} {state:<8} ({expect})")
        if not ok:
            failures.append(f"{ref} {label}")
    print(f"\n{len(CHECKS) - len(failures)}/{len(CHECKS)} PASS")
    if failures:
        print("FAILURES: " + "; ".join(failures))
    return 1 if failures else 0


if __name__ == "__main__":
    sys.exit(main())
