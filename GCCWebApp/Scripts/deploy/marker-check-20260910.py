# -*- coding: utf-8 -*-
"""Prove THIS bundle carries the 2026-09-10 work.

verify-bundle.py checks a curated list of older markers; it cannot know about work added
after it was last edited. Two bundles can match on entry count and size and still differ by
a whole fix (the 2026-08-30 lesson), so each item below is asserted against the bundle's
actual bytes:

  - static JS is compared md5-for-md5 against the working-tree file
  - .cshtml work is compiled into GCCWebApp.dll, so it is asserted as a literal byte string
  - new C# types are asserted by type name in the DLL

Usage:  python Scripts/deploy/marker-check-20260910.py <bundle.zip>
"""
import hashlib
import io
import os
import sys
import zipfile

BUNDLE = sys.argv[1] if len(sys.argv) > 1 else None
if not BUNDLE or not os.path.exists(BUNDLE):
    print('usage: python Scripts/deploy/marker-check-20260910.py <bundle.zip>')
    sys.exit(2)

# (label, in-zip path, working-tree path)
STATIC_FILES = [
    ('CR-048/049 internal tile+list bundle', 'wwwroot/js/certificate-tile-view-internal.js',
     'wwwroot/js/certificate-tile-view-internal.js'),
    ('CR-048/049 external tile+list bundle', 'wwwroot/js/certificate-tile-view-external.js',
     'wwwroot/js/certificate-tile-view-external.js'),
    ('CR-048 public tile bundle', 'wwwroot/js/certificate-tile-view.js',
     'wwwroot/js/certificate-tile-view.js'),
    ('DEF-419/420 shared CSV helper', 'wwwroot/js/csv-parse.js', 'wwwroot/js/csv-parse.js'),
    ('DEF-420 csfAttributes export escaping', 'wwwroot/js/admin/csfAttributes.js',
     'wwwroot/js/admin/csfAttributes.js'),
    ('DEF-342 product-id template copy', 'wwwroot/js/product-id-manager.js',
     'wwwroot/js/product-id-manager.js'),
]

# (label, literal that MUST be in GCCWebApp.dll, expected present?)
DLL_MARKERS = [
    ('CR-049 List button opens as the active/pressed one',
     b'data-view="list" aria-pressed="true"', True),
    ('CR-049 Tile button is no longer the pressed one',
     b'data-view="tile" aria-pressed="true"', False),
    ('CR-048 Edit opens a new tab (compiled cert detail views)',
     b'/Certificate/Edit/', True),
    ('DEF-419 RFC 4180 record reader type is compiled in',
     b'CsvRecordReader', True),
    ('DEF-419 shared CSV helper is referenced by the layout',
     b'js/csv-parse.js', True),
]


def md5(data):
    return hashlib.md5(data).hexdigest()


failures = 0
zf = zipfile.ZipFile(BUNDLE)
names = set(zf.namelist())


def resolve(path):
    """Bundles have been built with and without a leading folder; accept either."""
    if path in names:
        return path
    for n in names:
        if n.endswith('/' + path) or n == path:
            return n
    return None


print('bundle: %s' % os.path.basename(BUNDLE))
print('        %d entries, %.1f MB\n' % (len(names), os.path.getsize(BUNDLE) / 1048576.0))

print('--- static assets: in-zip bytes vs working tree ---')
for label, zpath, wpath in STATIC_FILES:
    z = resolve(zpath)
    if z is None:
        print('  [FAIL] %-42s NOT IN BUNDLE (%s)' % (label, zpath))
        failures += 1
        continue
    if not os.path.exists(wpath):
        print('  [FAIL] %-42s working-tree file missing (%s)' % (label, wpath))
        failures += 1
        continue
    zmd5 = md5(zf.read(z))
    wmd5 = md5(io.open(wpath, 'rb').read())
    ok = zmd5 == wmd5
    failures += 0 if ok else 1
    print('  [%s] %-42s %s' % ('PASS' if ok else 'FAIL', label,
                               zmd5[:16] if ok else 'zip=%s tree=%s' % (zmd5[:16], wmd5[:16])))

print('\n--- compiled views / types: literals inside GCCWebApp.dll ---')
dll = resolve('GCCWebApp.dll')
if dll is None:
    print('  [FAIL] GCCWebApp.dll not found in bundle')
    failures += 1
else:
    blob = zf.read(dll)
    print('  GCCWebApp.dll md5 = %s  (%.1f MB)' % (md5(blob)[:16], len(blob) / 1048576.0))
    # .NET keeps type/member names in the #Strings heap as UTF-8, but *string literals* -- which
    # is what compiled Razor markup becomes -- live in the #US heap as UTF-16LE. Searching only
    # for ASCII bytes reports a literal ABSENT when it is plainly there, which reads exactly like
    # a fix missing from the bundle. Check both encodings.
    for label, literal, expect_present in DLL_MARKERS:
        text = literal.decode('utf-8')
        present = (literal in blob) or (text.encode('utf-16-le') in blob)
        ok = (present == expect_present)
        failures += 0 if ok else 1
        print('  [%s] %-52s %s (expected %s)' % (
            'PASS' if ok else 'FAIL', label,
            'PRESENT' if present else 'ABSENT',
            'present' if expect_present else 'absent'))

print('\n--- hygiene: things that must NEVER ship ---')
for bad, why in [('DataProtectionKeys', 'dev keys ship unencrypted'),
                 ('appsettings.Development.json', 'dev config points at the dev DB')]:
    hits = [n for n in names if bad.lower() in n.lower()]
    ok = not hits
    failures += 0 if ok else 1
    print('  [%s] %-42s %s' % ('PASS' if ok else 'FAIL', bad,
                               '(none) - ' + why if ok else 'FOUND: %s' % hits[:3]))

print('\nfailures: %d' % failures)
sys.exit(0 if failures == 0 else 1)
