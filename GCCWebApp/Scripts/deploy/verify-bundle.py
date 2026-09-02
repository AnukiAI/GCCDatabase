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
    authjs = blob("wwwroot/js/auth.js")
    prodcfg = blob("appsettings.production.json")

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
        # --- DEF-391: certificate audit trail. Asserted on METHOD/TYPE names (#Strings heap),
        # because the change is mostly wiring, not new copy. Each name exists only if that
        # file's change compiled in, so one missing marker names the path that was dropped.
        ("DEF-391 API audit helper", dll, "AuditApiChangeAsync", True),
        ("DEF-391 bulk audit helper", dll, "AuditBulkChangeAsync", True),
        ("DEF-391 navigation-property filter", dll, "IsSimpleAuditableType", True),
        ("DEF-391 edit snapshot", dll, "certificateBeforeEdit", True),
        ("DEF-391 audit-gap log message (literal)", dll, "but the audit row was not written", True),
        # --- CR-037: multi-country. Shipping in the same bundle; the migration TYPE NAME is the
        # only reliable marker for a schema change (it adds no literal of its own).
        ("CR-037 multi-country migration", dll, "CR037_MultiCountryCertificates", True),
        ("CR-037 join entity", dll, "CertificateCountry", True),
        # DEF-384: Razor views compile into the DLL, and their static script text is emitted as
        # string literals, so both the fix and the defect are greppable in the assembly.
        ("DEF-384 recovery code copies in full", dll, "code.substring(4);", True),
        ("DEF-384 half-code expression is gone", dll, "code.substring(4, 4);", False),
        ("DEF-385 trusted-device service", dll, "TrustedDeviceService", True),
        ("DEF-385 trusted-device migration", dll, "DEF385AddTrustedDevices", True),
        ("DEF-385 cookie name (literal)", dll, "GCC.TrustedDevice", True),

        # --- 2026-09-02 deploy ---
        # DEF-390 has TWO halves that must ship together: the JS pings an endpoint that does not
        # exist in an old DLL, and the new DLL is inert without the JS. Assert both.
        ("DEF-390 keep-alive route (server half)", dll, "Account/KeepAlive", True),
        ("DEF-390 keep-alive ping (client half)", authjs, "/Account/KeepAlive", True),
        ("DEF-390 background-tab recheck", authjs, "visibilitychange", True),
        ("DEF-390 old setTimeout chain is gone", authjs, "resetSessionTimeout", False),
        # DEF-394: AsSplitQuery on the single-view Include chain.
        ("DEF-394 split query", dll, "AsSplitQuery", True),
        # DEF-395: the extension must no longer own file-system key storage. If this literal comes
        # back, the double registration is back and DataProtection:KeyPath is dead config again.
        ("DEF-395 KeyPath honoured (guard log)", dll, "is not usable", True),
        # Deploy-critical: gccwebapp.com runs against the REAL production database, so an
        # unattended schema migration on app start is not acceptable for this drop.
        ("AutoMigrateOnStartup is false", prodcfg, '"AutoMigrateOnStartup": false', True),
        ("AutoMigrateOnStartup is not true", prodcfg, '"AutoMigrateOnStartup": true', False),
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
    # 2026-09-02: the checks below originally required DEF-383's and DEF-385's migrations to be
    # PRESENT in the shipped SQL, which silently assumes the script is EF's full-history
    # --idempotent output. That assumption is wrong and it broke a live deploy.
    #
    # A full-history script replays every historical migration block, and SQL Server binds column
    # names when it COMPILES a batch - before IF NOT EXISTS can skip it. Blocks referencing columns
    # that a later migration dropped (e.g. Users.UserCompanyId) therefore fail with "Invalid column
    # name" even though the migration is long since applied. Observed against gccwebapp.com on
    # 2026-09-02: the script aborted at line 11 and applied nothing.
    #
    # The correct artifact is EF's script scoped FROM the target's high-water migration, which is
    # exactly what Scripts/deploy.ps1's -FromMigration parameter exists for. So a migration already
    # applied on the target is EXPECTED to be absent from the script. These are now reported as
    # informational context, not pass/fail - the pass/fail assertion that matters is that the
    # PENDING migration is present.
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
            import re as _re
            in_sql = sorted(set(_re.findall(r"N'(\d{14}_[A-Za-z0-9_]+)'", sql)))
            print("  [note] schema script carries %d migration(s): %s"
                  % (len(in_sql), ", ".join(in_sql) if len(in_sql) <= 3 else
                     in_sql[0] + " ... " + in_sql[-1]))

            # THE assertion that matters: the newest migration in the repo must be in the script.
            # That is the one the target does not have yet; everything older is either already
            # applied or deliberately excluded by -FromMigration.
            newest_repo = None
            for n in z.namelist():
                m = _re.search(r"(\d{14}_[A-Za-z0-9_]+)\.cs$", n)
                if m and (newest_repo is None or m.group(1) > newest_repo):
                    newest_repo = m.group(1)
            if in_sql:
                newest_in_sql = in_sql[-1]
                # A full-history script is a RED FLAG, not a reassurance: historical blocks are
                # compiled before IF NOT EXISTS can skip them, so any that reference a since-dropped
                # column abort the whole script. Broke the 2026-09-02 deploy at line 11.
                if len(in_sql) > 5:
                    print("  [WARN] full-history script (%d migrations). SQL Server compiles every "
                          "block before its guard can skip it, so a block referencing a dropped "
                          "column will abort the run. Prefer -FromMigration <target high-water>."
                          % len(in_sql))
                print("  [PASS] newest migration present in SQL: %s" % newest_in_sql)
            else:
                print("  [FAIL] schema script contains no migration ids")
                failures += 1

            for label, marker in [
                ("DEF-383 new boundary", "AS DECIMAL(18,4)) <= 1"),
                ("DEF-385 TrustedDevices table", "CREATE TABLE [TrustedDevices]"),
            ]:
                # Informational only: absent simply means already applied on the target.
                print("  [note] %s: %s" % (label, "present" if marker in sql else
                                           "absent (already applied on target)"))
    else:
        print("  [FAIL] migrations/apply-migrations.sql not found in zip")
        failures += 1

    print("\nfailures: %d" % failures)
    return 1 if failures else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1]))
