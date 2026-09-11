"""Assert this session's fixes are actually inside the bundle, against the IN-ZIP bytes.

"It built" is not evidence the change shipped (DEF: two bundles, same size, different build).
A .NET assembly keeps type/member names in the #Strings heap (UTF-8) and string literals in
the #US heap (UTF-16LE), so every marker is tested in BOTH encodings — searching a literal as
ASCII alone reports ABSENT for a change that did ship.

Control markers prove the check itself works: one that MUST be present, one that MUST be absent.
"""
import sys, zipfile

zip_path = sys.argv[1]
z = zipfile.ZipFile(zip_path)

# Entry names in this bundle use backslashes (Compress-Archive on Windows).
norm = {n.replace("\\", "/").lower(): n for n in z.namelist()}

def blob(suffix):
    for k, original in norm.items():
        if k.endswith(suffix):
            return z.read(original), original
    return None, None

app_dll, app_name = blob("gccwebapp.dll")
views_dll, views_name = blob("gccwebapp.views.dll")
# .NET 8 compiles Razor views into the app assembly; fall back to it so the DEF-418
# markers are asserted against real bytes instead of passing vacuously against None.
if views_dll is None:
    views_dll, views_name = app_dll, (app_name or "") + " (views compiled in)"
csvjs, csvjs_name = blob("wwwroot/js/csv-parse.js")
pidjs, pidjs_name = blob("wwwroot/js/product-id-manager.js")
userjs, userjs_name = blob("wwwroot/js/usermanagement.js")

print("resolved in-zip files")
for label, name in (("app dll", app_name), ("views dll", views_name),
                    ("csv-parse.js", csvjs_name), ("product-id-manager.js", pidjs_name),
                    ("userManagement.js", userjs_name)):
    print("  %-22s %s" % (label, name or "*** NOT IN ZIP ***"))

def contains(data, marker):
    if data is None:
        return False
    return marker.encode("utf-8") in data or marker.encode("utf-16-le") in data

checks = [
    # --- controls: if these two misbehave, nothing below is trustworthy ---
    ("CONTROL  type name present",              app_dll,   "CertificateController",       True),
    ("CONTROL  nonsense marker absent",         app_dll,   "zzz_not_a_real_marker_zzz",   False),

    # --- DEF-418: import help text rewritten (Razor precompiled into Views.dll) ---
    ("DEF-418  'Certificate # is not required'", views_dll, "Certificate # is not required", True),
    ("DEF-418  Product Category must exist",     views_dll, "Product Category must already exist", True),
    ("DEF-418  old wrong line is GONE",          views_dll, "must exist in database",      False),
    ("DEF-418  old required-columns line GONE",  views_dll, "CertificateNumber, CompanyName", False),

    # --- DEF-421: the two dead template columns are now read ---
    ("DEF-421  ProductSubcategoryColumns",       app_dll,   "ProductSubcategoryColumns",   True),
    ("DEF-421  CertSubcategoriesColumns",        app_dll,   "CertSubcategoriesColumns",    True),
    ("DEF-421  row field ProductSubcategoryName",app_dll,   "ProductSubcategoryName",      True),
    ("DEF-421  row field CertSubcategoryIds",    app_dll,   "CertSubcategoryIds",          True),

    # --- DEF-422: prefix fallback is now opt-in ---
    ("DEF-422  allowPrefixFallback parameter",   app_dll,   "allowPrefixFallback",         True),

    # --- DEF-423: strip helper + guard, in the shipped JS ---
    ("DEF-423  GccCsv.stripExcelText defined",   csvjs,     "function stripExcelText",     True),
    ("DEF-423  stripExcelText exported",         csvjs,     "stripExcelText: stripExcelText", True),
    ("DEF-423  formula guard in escapeField",    csvjs,     "=+-@",                        True),
    ("DEF-423  product-id delegates to shared",  pidjs,     "GccCsv.stripExcelText(value, true)", True),
    ("DEF-423  user import strips",              userjs,    "GccCsv.stripExcelText(fields[index]", True),
]

print("\ncontent markers (in-zip bytes)")
failures = 0
for label, data, marker, want in checks:
    got = contains(data, marker)
    ok = (got == want)
    if not ok:
        failures += 1
    print("  [%s] %-42s %s" % ("PASS" if ok else "FAIL", label,
                               "" if ok else ("expected %s, got %s" % (want, got))))

print("\n%s: %d marker(s) failed" % ("CLEAN" if failures == 0 else "PROBLEM", failures))
sys.exit(1 if failures else 0)
