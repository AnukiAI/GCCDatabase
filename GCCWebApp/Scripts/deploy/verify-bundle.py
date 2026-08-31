"""Deploy-bundle hygiene + content verification.

Two independent questions, both of which have been answered wrongly before:

  HYGIENE  — does the zip carry anything that must never reach the server?
             The 2026-08-17 bundle shipped 5 DataProtection key files, unencrypted. Dev config
             carries the dev DB password. Shipping wwwroot/uploads or App_Data/uploads clobbers
             the live PDF store (2026-04-23 upload wipe, and a contributor to the 2026-06-15
             all-PDFs-missing incident).

  CONTENT  — are the fixes actually inside this zip?
             "It built" is not evidence the change shipped. Markers are asserted against the
             in-zip bytes, with a control marker that MUST be present and a nonsense marker that
             MUST be absent, so a silently-broken check cannot read as a pass.

Usage:  python Scripts/deploy/verify-bundle.py <path-to-zip>
Exit:   0 = clean, 1 = a hygiene failure or a missing expected marker.
"""
import re
import sys
import zipfile

BACKSLASH = chr(92)


def norm(name):
    return name.lower().replace(BACKSLASH, "/")


# (label, predicate, is_fatal)
HYGIENE = [
    ("DataProtection keys", lambda n: "dataprotectionkeys" in n, True),
    ("loose key-*.xml", lambda n: re.search(r"key-[0-9a-f-]{8,}\.xml$", n) is not None, True),
    ("appsettings.Development.json", lambda n: n.endswith("appsettings.development.json"), True),
    ("appsettings.Secrets.json", lambda n: n.endswith("appsettings.secrets.json"), True),
    ("appsettings.Test.json", lambda n: n.endswith("appsettings.test.json"), True),
    ("test projects", lambda n: ".tests/" in n or n.endswith(".tests"), True),
    ("wwwroot/uploads", lambda n: "wwwroot/uploads/" in n, True),
    ("App_Data/uploads", lambda n: "app_data/uploads/" in n, True),
    ("Scripts/ tree", lambda n: n.startswith("scripts/"), True),
    ("docs/ tree", lambda n: n.startswith("docs/"), True),
    ("docs2/ tree", lambda n: "docs2/" in n, True),
    (".pdb symbols", lambda n: n.endswith(".pdb"), False),
]

REQUIRED = ["gccwebapp.dll", "web.config", "appsettings.json", "appsettings.production.json"]


def main(path):
    z = zipfile.ZipFile(path)
    names = z.namelist()
    normed = [norm(n) for n in names]
    print("bundle: %s" % path)
    print("entries: %d" % len(names))

    print("\n=== HYGIENE ===")
    failures = 0
    for label, pred, fatal in HYGIENE:
        hits = [n for n, nn in zip(names, normed) if pred(nn)]
        if hits and fatal:
            failures += 1
            tag = "FAIL"
        elif hits:
            tag = "note"
        else:
            tag = "PASS"
        line = "  [%s] %s: %d" % (tag, label, len(hits))
        if hits:
            line += "  e.g. " + ", ".join(hits[:3])
        print(line)

    print("\n=== REQUIRED FILES ===")
    for req in REQUIRED:
        hits = [n for n, nn in zip(names, normed) if nn.rsplit("/", 1)[-1] == req]
        tag = "PASS" if hits else "FAIL"
        if not hits:
            failures += 1
        print("  [%s] %s%s" % (tag, req, ("  -> " + hits[0]) if hits else ""))

    # --- content markers -------------------------------------------------
    # Read the in-zip bytes, not the working tree.
    def blob(suffix):
        for n, nn in zip(names, normed):
            if nn.endswith(suffix):
                return z.read(n)
        return None

    dll = blob("/gccwebapp.dll") or blob("gccwebapp.dll")
    preview = blob("views/certificateimport/preview.cshtml")
    tilejs = blob("wwwroot/js/certificate-tile-view.js")

    print("\n=== CONTENT MARKERS (in-zip bytes) ===")
    # A .NET assembly stores these in TWO different heaps with TWO different encodings:
    #   - type and member names  -> #Strings heap, UTF-8
    #   - string literals        -> #US heap, UTF-16LE
    # Searching a literal as ASCII therefore reports ABSENT for a change that did ship. Every
    # marker below is tested in both encodings, so the check cannot produce that false negative.
    def contains(data, marker):
        return marker.encode("utf-8") in data or marker.encode("utf-16-le") in data

    checks = [
        ("control: type name present", dll, "CertificateController", True),
        ("control: nonsense marker absent", dll, "zzz_not_a_real_marker_zzz", False),
        ("DEF-376 partner effective-date gate", dll, "WhereEffectiveDateReached", True),
        ("DEF-376 download gate", dll, "IsNotYetEffective", True),
        ("DEF-377 new export failure message (literal)", dll, "Export failed. Please try again", True),
        ("DEF-377 old 400 message is gone (literal)", dll, "No data to export or export failed", False),
        ("DEF-378 auto-number flag", dll, "WillAutoAssignNumber", True),
        ("DEF-378 preview placeholder", dll, "DisplayCertificateNumber", True),
        ("DEF-378 auto-assign label (literal)", dll, "(auto-assign)", True),
        ("DEF-378 old rejection is gone (literal)", dll, "Certificate Number is required", False),
        # DEF-382 adds an auto-property `Skip` to CertificateSearchFilter. `Skip` alone is useless
        # as a marker (System.Linq.Skip is everywhere), so assert on the compiler-generated backing
        # field, which only exists if the property was actually compiled in.
        ("DEF-382 filter Skip property", dll, "<Skip>k__BackingField", True),
        # DEF-383 changes `<` to `<=` in an expression — it adds NO string literal, so a literal
        # grep is structurally blind to it. Assert on the migration TYPE NAME instead, which only
        # exists if the migration compiled in, and on the SQL script further down.
        ("DEF-383 percent-boundary migration", dll, "DEF383FixCertResultsPercentBoundary", True),
        # DEF-384: Razor views compile into the DLL, and their static script text is emitted as
        # string literals, so both the fix and the defect are greppable in the assembly.
        ("DEF-384 recovery code copies in full", dll, "code.substring(4);", True),
        ("DEF-384 half-code expression is gone", dll, "code.substring(4, 4);", False),
        ("DEF-385 trusted-device service", dll, "TrustedDeviceService", True),
        ("DEF-385 trusted-device migration", dll, "DEF385AddTrustedDevices", True),
        ("DEF-385 cookie name (literal)", dll, "GCC.TrustedDevice", True),
    ]
    for label, data, marker, want in checks:
        if data is None:
            print("  [FAIL] %s: source file not found in zip" % label)
            failures += 1
            continue
        found = contains(data, marker)
        ok = (found == want)
        if not ok:
            failures += 1
        print("  [%s] %s: %s (expected %s)" % (
            "PASS" if ok else "FAIL", label,
            "PRESENT" if found else "ABSENT",
            "present" if want else "absent"))

    # Razor views are compiled into the DLL, but the .cshtml also ships; assert on it directly
    # so the auto-assign placeholder is proven on the file the server actually renders from.
    if preview is not None:
        found = b"DisplayCertificateNumber" in preview
        if not found:
            failures += 1
        print("  [%s] DEF-378 Preview.cshtml renders DisplayCertificateNumber: %s"
              % ("PASS" if found else "FAIL", "PRESENT" if found else "ABSENT"))
    else:
        print("  [note] Preview.cshtml not shipped separately (compiled into the DLL)")

    # DEF-381 is a static asset, not compiled into the DLL — assert on the shipped .js itself,
    # including the negative: the old whole-array append must be gone.
    if tilejs is not None:
        for marker, want, label in [
            (b"pendingAppend", True, "DEF-381 renders only the newest batch"),
            (b"const html = state.certificates.map(cert => createCertificateCard(cert)).join('');\n\n        if (append)",
             False, "DEF-381 old whole-array append is gone"),
        ]:
            found = marker in tilejs
            ok = (found == want)
            if not ok:
                failures += 1
            print("  [%s] %s: %s (expected %s)" % (
                "PASS" if ok else "FAIL", label,
                "PRESENT" if found else "ABSENT",
                "present" if want else "absent"))
    else:
        print("  [FAIL] certificate-tile-view.js not found in zip")
        failures += 1

    # DEF-383's other half is a persisted computed column. The deploy carries it as SQL, so assert
    # the shipped migration script actually rewrites the boundary — the display fix alone would make
    # these certificates sort as 1% while displaying 100%.
    migration_sql = blob("migrations/apply-migrations.sql")
    if migration_sql is not None:
        sql = migration_sql.decode("utf-8", "replace")

        # DEF-386, ALWAYS fatal: EF's --idempotent output wraps a computed-column DDL in
        # EXEC(CONCAT(...)), which SQL Server cannot parse — EXECUTE takes a string or variable,
        # never a function call. Because it is a PARSE error it kills the whole script before any
        # guard can skip it. deploy.ps1 rewrites these into DECLARE @v = CONCAT(...); EXEC(@v);
        n_concat = sql.count("EXEC(CONCAT(")
        ok = (n_concat == 0)
        if not ok:
            failures += 1
        print("  [%s] DEF-386 no unparseable EXEC(CONCAT(...)): %d found (expected 0)"
              % ("PASS" if ok else "FAIL", n_concat))

        # The schema portion is scoped by deploy.ps1 -FromMigration to what the TARGET still needs,
        # so an empty one is correct when the target is already at the repo's high-water — it is not
        # evidence the migrations are missing. What proves they SHIPPED is the migration type name
        # in the assembly (asserted above), which is also what the runtime auto-migrate uses.
        has_schema = "__EFMigrationsHistory" in sql
        if has_schema:
            for label, marker in [
                ("DEF-383 migration in SQL", "DEF383FixCertResultsPercentBoundary"),
                ("DEF-383 new boundary", "AS DECIMAL(18,4)) <= 1"),
                ("DEF-385 TrustedDevices table", "CREATE TABLE [TrustedDevices]"),
                ("DEF-385 migration in SQL", "DEF385AddTrustedDevices"),
            ]:
                found = marker in sql
                if not found:
                    failures += 1
                print("  [%s] %s: %s" % ("PASS" if found else "FAIL", label,
                                         "PRESENT" if found else "ABSENT"))
        else:
            print("  [note] schema script is empty — built with -FromMigration, so the target is "
                  "already at the repo high-water. Migrations are proven present via the assembly "
                  "markers above.")
    else:
        print("  [FAIL] migrations/apply-migrations.sql not found in zip")
        failures += 1

    print("\nfailures: %d" % failures)
    return 1 if failures else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1]))
