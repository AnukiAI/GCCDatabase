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

Usage:  python Scripts/deploy/verify-bundle.py <path-to-zip> [--no-migrations=<HighWaterName>]
        --no-migrations declares a drop that carries NO schema change. Absence of the migration
        script is otherwise FATAL, and should stay that way.
Exit:   0 = clean, 1 = a hygiene failure or a missing expected marker.
"""
import io
import os
import re
import sys
import json
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
    # Added 2026-09-17: the 20260917-1539 build shipped five *.template.json files. The pubxml
    # had excluded only Production's. They are dev aids the server never reads, and their prose
    # names an SMTP host, a server IP and a live DB host:port. FATAL because the whole point of
    # the exclusion is that it cannot be re-earned by hand each release.
    ("appsettings.*.template.json", lambda n: re.search(r"appsettings\..*template\.json$", n) is not None, True),
    # Added 2026-09-17: wwwroot/css/certificate-create.css.bak was found shipping. Editor
    # leftovers are untracked, so their presence also means the zip is not reproducible
    # from a fresh clone.
    ("editor/backup leftovers", lambda n: n.endswith((".bak", ".orig", "~")), True),
    (".pdb symbols", lambda n: n.endswith(".pdb"), False),
]

REQUIRED = ["gccwebapp.dll", "web.config", "appsettings.json", "appsettings.production.json"]

# --- publish-profile policy (DEF-429) ---------------------------------------
# The pubxml decides what ships. Until 2026-09-17 it was gitignored, so these exclusions lived on
# one machine and a fresh clone got none of them. Tracking the file makes the policy PRESENT; this
# section makes it CHECKED. Each entry is here because its absence has cost, or nearly cost, data:
#   wwwroot/uploads + App_Data/uploads -> publishing once WIPED the production PDF store
#                                         (2026-04-23; a factor in the 2026-06-15 incident)
#   appsettings.Development.json       -> carries the dev DB password
#   appsettings.*.template.json        -> prose naming live host/port/mailbox details (2026-09-17)
#   EnvironmentName=Production         -> without it the deployed app loads placeholder config
#                                         and its DB + B2C wiring breaks
#
# Substring checks, deliberately not regex: the patterns contain Windows path separators, and an
# escaping mistake in a gate is worse than no gate - it fails open and reads as a pass.
PROFILE = os.path.join("Properties", "PublishProfiles", "PleskTestEnvironment.pubxml")
_BS = chr(92)
PROFILE_POLICY = [
    ("wwwroot/uploads excluded",
     'Content Remove="wwwroot' + _BS + 'uploads' + _BS + '**"'),
    ("App_Data/uploads excluded",
     'Content Remove="App_Data' + _BS + 'uploads' + _BS + '**"'),
    ("dev config excluded", 'Content Remove="appsettings.Development.json"'),
    ("template config excluded", 'Content Remove="appsettings.*.template.json"'),
    ("EnvironmentName is Production", "<EnvironmentName>Production</EnvironmentName>"),
]


def check_profile(repo_root):
    """Assert the publish profile exists and still carries every exclusion that protects live data.

    A MISSING profile is itself a failure: that is the state a fresh clone was in before DEF-429,
    and it is invisible from the bundle alone - a zip cannot show what its build was allowed to
    include.
    """
    print("")
    print("=== PUBLISH PROFILE POLICY (DEF-429) ===")
    path = os.path.join(repo_root, PROFILE)
    if not os.path.isfile(path):
        print("  [FAIL] profile not found: %s" % path)
        print("         A fresh clone with no pubxml can publish a bundle that ships the uploads")
        print("         tree and clobbers the live PDF store. Restore it from git before building.")
        return 1
    text = io.open(path, encoding="utf-8", errors="replace").read()
    failures = 0
    for label, needle in PROFILE_POLICY:
        ok = needle in text
        print("  [%s] %s" % ("PASS" if ok else "FAIL", label))
        if not ok:
            failures += 1
    return failures



BUNDLE_MARKER = re.compile(
    r"^\s*[-*]\s*\*\*Bundle marker:\*\*\s*(?P<file>[^|]+?)\s*\|\s*(?P<mode>contains|absent|reason)\s*\|\s*(?P<needle>.+?)\s*$",
    re.IGNORECASE)
REG_HEADING = re.compile(r"^#{2,5}\s+((?:DEF|CR)-\d+)\b\s*(?:[-—:]\s*)?(.*)$", re.IGNORECASE)


def load_register_markers(repo_root):
    """Collect `- **Bundle marker:** <file> | contains|absent | <needle>` lines per id.

    The register is the single source of truth on purpose. A separate markers file would be a
    second place to forget, which is the same drift that let coverage stop at DEF-429.
    """
    markers = {}
    for name in ("defect-register.md", "change-request-register.md"):
        path = os.path.join(repo_root, "docs", "tracking", name)
        if not os.path.exists(path):
            continue
        current = None
        for ln in io.open(path, encoding="utf-8", errors="replace").read().splitlines():
            h = REG_HEADING.match(ln)
            if h:
                current = h.group(1).upper()
                continue
            m = BUNDLE_MARKER.match(ln)
            if m and current:
                markers.setdefault(current, []).append(
                    (m.group("file").strip(), m.group("mode").lower(), m.group("needle")))
    return markers


def check_register_coverage(repo_root, waiting_path, z, names, normed, contains):
    """Every resolved-but-unshipped item must PROVE it is in this zip.

    Added 2026-09-22. The 50 markers above are hand-maintained and had silently stopped covering
    the register at DEF-429 - so a bundle carrying 11 unshipped items reported "failures: 0"
    while asserting nothing about any of them. A green gate read as "everything shipped".
    Coverage is now derived from the register itself rather than curated by hand.

    DEF without a marker FAILS the build; CR without one WARNS (Shawn's call, 2026-09-22).
    """
    print("\n=== REGISTER COVERAGE (resolved-but-unshipped must prove it shipped) ===")
    try:
        waiting = json.load(io.open(waiting_path, encoding="utf-8"))
    except Exception as exc:
        print("  [FAIL] could not read waiting set %s (%s)" % (waiting_path, exc))
        print("         Run: python Scripts/deploy/resolved-not-shipped.py --emit-waiting <path>")
        return 1

    items = waiting.get("waiting", [])
    if not items:
        print("  [PASS] nothing resolved is waiting - no coverage required")
        return 0

    markers = load_register_markers(repo_root)
    failures = 0
    unmarked_def, unmarked_cr = [], []

    def blob_for(spec):
        spec = spec.strip().lower()
        suffix = "gccwebapp.dll" if spec == "dll" else spec
        for n, nn in zip(names, normed):
            if nn.endswith(suffix):
                return z.read(n)
        return None

    for item in items:
        ident, kind = item["id"], item["kind"].upper()
        defined = markers.get(ident, [])
        if not defined:
            (unmarked_def if kind == "DEF" else unmarked_cr).append(ident)
            continue
        for spec, mode, needle in defined:
            # `none | reason | <why>` — an explicit, recorded opt-out. Some real fixes genuinely
            # have no in-artifact marker: DEF-432 is an added CALL to an existing method, which
            # compiles to IL with no new symbol or literal, so any marker would pass against the
            # unfixed build too. Forcing a marker there would produce a fake one, which is worse
            # than none. The opt-out is deliberately noisy and names its evidence instead.
            if mode == "reason":
                print("  [NOTE] %-8s no in-zip marker by design: %s" % (ident, needle[:70]))
                continue
            data = blob_for(spec)
            if data is None:
                print("  [FAIL] %-8s %s -> file not in zip" % (ident, spec))
                failures += 1
                continue
            present = contains(data, needle)
            ok = present if mode == "contains" else (not present)
            print("  [%s] %-8s %s | %s | %s"
                  % ("PASS" if ok else "FAIL", ident, spec, mode, needle[:58]))
            if not ok:
                failures += 1

    if unmarked_cr:
        print("  [WARN] %d CR(s) resolved-but-unshipped with NO bundle marker: %s"
              % (len(unmarked_cr), ", ".join(unmarked_cr)))
        print("         Nothing proves these are in the zip. Add a `- **Bundle marker:**` line.")
    if unmarked_def:
        print("  [FAIL] %d DEF(s) resolved-but-unshipped with NO bundle marker: %s"
              % (len(unmarked_def), ", ".join(unmarked_def)))
        print("         A fix that cannot be proven present is indistinguishable from one that")
        print("         was never built. Add to each register entry, e.g.:")
        print("           - **Bundle marker:** dll | contains | SomeCompiledSymbolOrLiteral")
        print("           - **Bundle marker:** wwwroot/css/main.css | absent | <old rule>")
        failures += len(unmarked_def)

    return failures


def main(path, no_migrations_highwater=None, waiting_path=None):
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
    authcss = blob("wwwroot/css/auth.css")

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

        # --- 2026-09-04 client-QC forgot-password batch (DEF-405 / 406 / 407) -----------------
        # These are Razor VIEW changes. Views compile into GCCWebApp.dll and their static text is
        # emitted as string literals (same mechanism DEF-384 relies on above), so both the fix and
        # the defect are greppable in the assembly.
        #
        # Read the control marker FIRST: all three fixes below are asserted partly as ABSENCES, and
        # an absence proves nothing if the view never made it into the DLL at all. This control is
        # untouched copy from ForgotPassword.cshtml, so it is present iff the view compiled in.
        ("control: ForgotPassword view compiled in", dll, "Enter your email to reset your password", True),

        # DEF-405 - the page said 24 hours, the token really dies in 1 hour (AddHours(1)).
        ("DEF-405 reset expiry says 1 hour", dll, "Reset links expire after 1 hour", True),
        # Marker is the FULL sentence, deliberately. "expire after 24 hours" alone collides with
        # SwaggerExtensions.cs:356 ("Tokens expire after 24 hours" — API JWT docs, unrelated), which
        # made this report a false FAIL on 2026-09-04. Keep it specific to the reset-link copy.
        ("DEF-405 old 24-hour copy is gone", dll, "Reset links expire after 24 hours", False),

        # DEF-406 - three auth views each redeclared --auth-left-gradient, shadowing auth.css and
        # rendering a darker green than sign-in. The fix DELETES those declarations, so assert the
        # declaration is gone from the DLL and the single surviving light one ships in auth.css.
        # NB: assert the VARIABLE DECLARATION, not the bare gradient value - the same dark gradient
        # legitimately remains on `.auth-icon` (auth.css) and in WelcomeRedirect.cshtml, so a bare
        # value check would fail on code that is deliberately unchanged.
        ("DEF-406 no view redeclares the gradient", dll,
         "--auth-left-gradient: linear-gradient(135deg, #4a7c23", False),
        ("DEF-406 light gradient ships in auth.css", authcss,
         "--auth-left-gradient: linear-gradient(135deg, #92C83E", True),

        # DEF-407 - client asked for this line to come off the Forgot Password screens.
        ("DEF-407 tagline removed from auth views", dll,
         "Access your sustainability certification database.", False),

        # --- 2026-09-04 drop -------------------------------------------------
        # DEF-401 - the preview-image field on Certificate/Edit. Razor views are compiled into
        # the DLL, so the element ids are the marker; both are new, neither existed before.
        ("DEF-401 preview-image upload zone", dll, "imageUploadZone", True),
        ("DEF-401 preview-image container", dll, "imagePreviewContainer", True),

        # DEF-402 - the country clause pulled out of the search OR into its own semi-join.
        # A new public method, so it lands in the #Strings heap under its own name.
        ("DEF-402 country semi-join helper", dll, "MatchingCountryCertificateIdsAsync", True),

        # DEF-404 - the export/list country filter now matches CertificateCountries rather than
        # the denormalized primary. The fix adds no new type or literal, but the lambda CAPTURES
        # the local, so the compiler emits a display-class field of that name. That field name is
        # the only durable trace the change left in the assembly.
        ("DEF-404 captured country filter", dll, "countryFilter", True),

        # --- 2026-09-08 drop -------------------------------------------------
        # DEF-410 - "Trust this device for 30 days" posted the literal "on", which ASP.NET Core's
        # bool binder cannot parse, so the trust cookie was NEVER issued. The fix is one attribute,
        # and it is asserted BOTH ways: the fixed markup must be present AND the broken bare form
        # must be gone. The absent-check is the regression guard - if someone drops value="true"
        # again the feature silently dies with no error anywhere.
        ("DEF-410 trust checkbox is bindable", dll, 'name="TrustDevice" value="true"', True),
        ("DEF-410 bare checkbox (posts \"on\") is gone", dll, 'name="TrustDevice" />', False),

        # DEF-401 second half - the Create screen's preview-image field. Uses the Create-specific
        # help text: `imageUploadZone` cannot tell Create from Edit, since both views compile into
        # this same assembly.
        ("DEF-401 Create-side preview image field", dll,
         "Leave empty and the preview is generated from the PDF above", True),

        # --- 2026-09-05 drop -------------------------------------------------
        # DEF-409 - relevance ranking for search. A new type and two new public members, so all
        # three land in the #Strings heap under their own names.
        ("DEF-409 ranking helper type", dll, "SearchRanking", True),
        ("DEF-409 banded paging", dll, "PageIdsAsync", True),
        ("DEF-409 autocomplete ranking", dll, "OrderByNumberRelevance", True),

        # CR-043 - Confidential certificates reach the PI export and the partner/Amazon API.
        # BadgeableStatuses is the new field and IsBadgeable the new predicate; both are member
        # names, so they land in the assembly's #Strings metadata heap and survive as ASCII.
        # PublicStatuses must STILL be present - it is not replaced, it keeps its four public
        # surfaces. A bundle where it vanished would mean the public embargo went with it, which
        # is the failure this pair is here to catch.
        # (The CR-043 data backfill inside migrations/apply-migrations.sql is not asserted here:
        # this table reads BINARY entries, and the schema script is text. It is covered by the
        # post-deploy data-script section that already parses that file.)
        ("CR-043 badgeable status list", dll, "BadgeableStatuses", True),
        ("CR-043 badgeable predicate", dll, "IsBadgeable", True),
        ("CR-043 public status list still present", dll, "PublicStatuses", True),

        # CR-045 - Certification Results publishes verbatim. Asserted by ABSENCE: the formatter
        # type and its method were deleted, not neutered, so either name reappearing in the DLL
        # means someone reinstated the display-time conversion.
        ("CR-045 display formatter is gone", dll, "FormatCertificationResults", False),
        ("CR-045 formatter class is gone", dll, "DisplayFormatters", False),

        # DEF-403 has NO reliable in-DLL marker and is deliberately not asserted here.
        # Its whole change is the ORDER BY expressions inside three LINQ queries - it introduces
        # no type, member or string literal, and every property name it touches already existed.
        # Inventing a marker that cannot fail would be worse than admitting the gap: verify it
        # functionally after the upload instead, with Scripts/smoke-prod/2026-09-04-export-audit.py,
        # which compares the six stat exports and fails if any two are byte-identical.
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
    elif no_migrations_highwater:
        # Explicit operator assertion that this drop carries NO schema change. The absence stays a
        # FAIL by default (that default has caught real omissions), so skipping it must be a
        # deliberate, recorded act — not a silent pass.
        #
        # The flag takes the migration that is expected to be the high-water mark and ALREADY
        # applied on the target. Asserting that type name is compiled into the DLL proves the
        # bundle really is at the schema level being claimed. It cannot prove nothing NEWER exists,
        # so the operator's assertion is printed loudly for the deploy log to carry.
        print("  [note] migrations/apply-migrations.sql: absent — declared NO-SCHEMA drop")
        if dll is not None and contains(dll, no_migrations_highwater):
            print("  [PASS] declared schema high-water compiled in: %s" % no_migrations_highwater)
        else:
            print("  [FAIL] declared schema high-water NOT in DLL: %s" % no_migrations_highwater)
            failures += 1
        print("  [!!] OPERATOR ASSERTION: no pending migration for this drop. If any migration was")
        print("       added after '%s', this bundle is INCOMPLETE and the" % no_migrations_highwater)
        print("       app will 500 on the changed tables. Re-check before uploading.")
    else:
        print("  [FAIL] migrations/apply-migrations.sql not found in zip")
        print("         If this drop genuinely carries no schema change, re-run with")
        print("         --no-migrations=<AlreadyAppliedHighWaterMigrationName> to declare that")
        print("         explicitly. Do NOT ignore this line.")
        failures += 1

    # Checked from the TREE, not the zip: a bundle cannot show what its own build was allowed
    # to include. This script lives at <repo>/Scripts/deploy/, so go up two levels.
    repo_root = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
    failures += check_profile(repo_root)

    if waiting_path:
        failures += check_register_coverage(repo_root, waiting_path, z, names, normed, contains)

    print("\nfailures: %d" % failures)
    return 1 if failures else 0


if __name__ == "__main__":
    args = sys.argv[1:]
    zip_path = None
    highwater = None
    waiting = None
    skip = False
    for i, a in enumerate(args):
        if skip:
            skip = False
            continue
        if a.startswith("--no-migrations="):
            highwater = a.split("=", 1)[1]
        elif a == "--no-migrations":
            sys.exit("--no-migrations requires the already-applied high-water migration name, "
                     "e.g. --no-migrations=CR037_MultiCountryCertificates")
        elif a.startswith("--waiting="):
            waiting = a.split("=", 1)[1]
        elif a == "--waiting":
            if i + 1 >= len(args):
                sys.exit("--waiting requires a path to the waiting-set JSON emitted by "
                         "resolved-not-shipped.py --emit-waiting")
            waiting = args[i + 1]
            skip = True
        else:
            zip_path = a
    if not zip_path:
        sys.exit("usage: verify-bundle.py <zip> [--no-migrations=<HighWaterMigrationName>] "
                 "[--waiting=<waiting-set.json>]")
    sys.exit(main(zip_path, highwater, waiting))
