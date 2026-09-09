# -*- coding: utf-8 -*-
"""
AUGUST SOURCE -> TEST DATABASE: COUNTRY AUDIT
=============================================
Answers one question the batch-2 verification did NOT answer.

The batch-2 verify compares the SEPTEMBER drop against the database. It says nothing about the
5,702 certificates loaded in August, and the client's complaint was about *those*:

    "I noticed a couple certs that had multiple countries from last import that did not carry
     over (I fixed the 2 I saw)."

"The 2 I saw" is a sample, not a population ([[feedback_enumerate_dont_sample_populations]]). This
reads every Country cell in the August source workbook, compares it to what the test database holds
now — as a SET, both directions ([[feedback_equal_counts_are_not_the_same_rows]]) — and prints
every certificate where they disagree.

    python 2026-09-03-august-country-audit.py {dev|test}

READ-ONLY. Touches no database row and no source file.
"""
from __future__ import annotations

import csv
import io
import os
import re
import sys
import datetime
import importlib.util
from collections import Counter, defaultdict

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", errors="replace")

import openpyxl
import pyodbc

HERE = os.path.dirname(os.path.abspath(__file__))
APP_ROOT = os.path.abspath(os.path.join(HERE, "..", ".."))
REPO_ROOT = os.path.abspath(os.path.join(APP_ROOT, "..", ".."))
SRC = os.path.join(REPO_ROOT, "DataMigrationTestDev", "GCC Database Migration_Certs_8.12.26V2.xlsx")
GENERATOR = os.path.join(HERE, "2026-08-13-migration-sql-generator.py")

# The three sheets the August transform actually read (2026-08-13-migration-transform.py:152-154).
SHEETS = ["Active List 7.31.26", "Expired List 2 - 8.11.26", "Expired List 1 - 4.9.26"]

TARGETS = {
    "dev": ("Driver={ODBC Driver 17 for SQL Server};Server=ANUKI01;Database=GCCDatabase_Dev;"
            "Uid=GCC_DevUser;Pwd=DevP@ss2024!SQL;TrustServerCertificate=yes;Encrypt=no;"),
    "test": ("Driver={ODBC Driver 17 for SQL Server};Server=tcp:68.71.129.120,1533;"
             "Database=gccwebap_certificates;Uid=GCC_adminxyz123;Pwd=Q3Ez4e!gE_47KNi;"
             "Encrypt=yes;TrustServerCertificate=yes;"),
}
if len(sys.argv) < 2 or sys.argv[1] not in TARGETS:
    sys.exit("usage: 2026-09-03-august-country-audit.py {dev|test}")
target = sys.argv[1]


def open_workbook(path):
    try:
        return openpyxl.load_workbook(path, read_only=True, data_only=True)
    except PermissionError:
        import shutil, tempfile
        tmp = os.path.join(tempfile.gettempdir(), "gcc-audit-" + os.path.basename(path))
        shutil.copy2(path, tmp)
        return openpyxl.load_workbook(tmp, read_only=True, data_only=True)


# Country canonicalisation reused from the generator, so this audit cannot disagree with the
# loader about what "The Netherlands" is.
spec = importlib.util.spec_from_file_location("gen", GENERATOR)
gen = importlib.util.module_from_spec(spec)
spec.loader.exec_module(gen)
canon, n_c, n_a = gen.load_country_map()
cs_normalize = gen.cs_normalize
print(f"countries: {n_c} canonical + {n_a} aliases from CountryList.cs\n")


def split_countries(raw):
    out = []
    for part in re.split(r"[;,|]", str(raw or "")):
        part = part.strip()
        if not part or part.lower() in ("none", "n/a", "na"):
            continue
        c = canon.get(cs_normalize(part), part)
        if c not in out:
            out.append(c)
    return out


print(f"reading {SRC}")
wb = open_workbook(SRC)

source = {}            # cert number -> [countries]
raw_by_number = {}     # cert number -> the raw cell, for the report
compound_rows = []
per_sheet = Counter()

for sheet in SHEETS:
    if sheet not in wb.sheetnames:
        sys.exit(f"ABORT: sheet {sheet!r} missing. Workbook has {wb.sheetnames}")
    ws = wb[sheet]
    rows = list(ws.iter_rows(values_only=True))
    header = [("" if h is None else str(h)).strip() for h in rows[0]]

    def find(name):
        for i, h in enumerate(header):
            if h.strip().lower() == name.lower():
                return i
        return None

    c_num = find("Certificate Number") or find("CERTIFICATE_NUMBER") or find("Certificate #")
    c_country = find("Country") if find("Country") is not None else find("COUNTRY")
    if c_num is None or c_country is None:
        # Fall back to a fuzzy match rather than guessing a position — DEF-347 was a header
        # mismatch that imported blanks and raised nothing.
        for i, h in enumerate(header):
            if c_num is None and "certificate" in h.lower() and ("number" in h.lower() or "#" in h):
                c_num = i
            if c_country is None and h.lower().startswith("countr"):
                c_country = i
    if c_num is None:
        sys.exit(f"ABORT: {sheet!r} — no certificate-number column. headers: {header}")
    if c_country is None:
        # "Expired List 1 - 4.9.26" is the pre-uplift sheet: 8 columns, no Country at all. The
        # August transform read it with has_new=False for exactly this reason. Not a
        # missing-data finding — say so and move on rather than aborting the whole audit.
        print(f"  {sheet!r}: {len(rows)-1} rows — NO Country column in this sheet "
              f"(headers: {[h for h in header if h]}). Skipped; nothing to compare.")
        continue
    print(f"  {sheet!r}: {len(rows)-1} rows, number=col{c_num} {header[c_num]!r}, "
          f"country=col{c_country} {header[c_country]!r}")

    for r in rows[1:]:
        if not r or c_num >= len(r) or not r[c_num]:
            continue
        num = str(r[c_num]).strip()
        raw = r[c_country] if c_country < len(r) else None
        countries = split_countries(raw)
        if not countries:
            continue
        per_sheet[sheet] += 1
        # A number can legitimately appear on more than one row (CR-038); union the countries.
        source.setdefault(num, [])
        for c in countries:
            if c not in source[num]:
                source[num].append(c)
        raw_by_number.setdefault(num, set()).add(str(raw).strip())
        if len(countries) > 1:
            compound_rows.append((num, str(raw).strip(), countries, sheet))

wb.close()
print(f"\nsource certificates carrying at least one country : {len(source)}")
print(f"source rows whose Country cell names SEVERAL countries: {len(compound_rows)}")

# ------------------------------------------------------------------ database --
cn = pyodbc.connect(TARGETS[target])
cur = cn.cursor()
cur.execute("SELECT Number, ISNULL(Country,'') FROM Certificates")
db_primary = {}
for n, c in cur.fetchall():
    db_primary[n] = c.strip()
cur.execute("""SELECT c.Number, cc.Country FROM CertificateCountries cc
               JOIN Certificates c ON c.Id = cc.CertificateId""")
db_set = defaultdict(set)
for n, c in cur.fetchall():
    db_set[n].add(c.strip())

cur.execute("SELECT COUNT(*) FROM Certificates")
cert_rows = cur.fetchone()[0]
# DISTINCT NUMBERS, not rows: CR-038 lets several certificates share a number, and both dicts
# above are keyed by number, so they collapse. Printing len(db_primary) as "certificates" makes
# a healthy database look like it just lost 175 rows.
print(f"\ntarget = {target.upper()}: {cert_rows} certificate rows across "
      f"{len(db_primary)} distinct numbers (CR-038 — a number may carry several records); "
      f"{sum(len(v) for v in db_set.values())} CertificateCountries rows "
      f"on {len(db_set)} of those numbers")

# --------------------------------------------------------------------- diff --
missing_entirely, partial, extra, absent_cert, ok = [], [], [], [], 0
for num, want in source.items():
    if num not in db_primary:
        absent_cert.append(num)
        continue
    got = db_set.get(num, set())
    w = set(want)
    if w == got:
        ok += 1
    elif not got:
        missing_entirely.append((num, want, sorted(got)))
    elif w - got:
        partial.append((num, want, sorted(got), sorted(w - got)))
    else:
        extra.append((num, want, sorted(got), sorted(got - w)))

print("\n" + "=" * 78)
print("RESULT — source Country cell vs CertificateCountries in the database")
print("=" * 78)
print(f"  exact set match ....................... {ok}")
print(f"  certificate has NO country rows at all   {len(missing_entirely)}")
print(f"  certificate has SOME but not all ...... {len(partial)}")
print(f"  database has countries the source did not {len(extra)}")
print(f"  certificate number not in the database    {len(absent_cert)}")

if partial:
    print("\n--- COUNTRIES LOST (the client's reported defect) ---")
    for num, want, got, miss in sorted(partial):
        print(f"  {num}: source {want} | database {got} | MISSING {miss}")
if missing_entirely:
    print(f"\n--- NO COUNTRY ROWS AT ALL ({len(missing_entirely)}) ---")
    for num, want, got in sorted(missing_entirely)[:40]:
        print(f"  {num}: source {want}")
    if len(missing_entirely) > 40:
        print(f"  ... and {len(missing_entirely)-40} more (full list in the CSV)")
if extra:
    print(f"\n--- DATABASE HAS MORE THAN THE SOURCE ({len(extra)}) ---")
    for num, want, got, ex in sorted(extra)[:20]:
        print(f"  {num}: source {want} | database {got} | EXTRA {ex}")

# Every compound source row, and what the database made of it — this is the population the
# client sampled two of.
print("\n" + "=" * 78)
print(f"EVERY multi-country row in the August source ({len(compound_rows)}) and its fate")
print("=" * 78)
fixed = broken = 0
for num, raw, want, sheet in sorted(compound_rows):
    got = sorted(db_set.get(num, set()))
    state = "OK" if set(want) == set(got) else ("SPLIT NOT APPLIED" if len(got) <= 1 else "PARTIAL")
    if state == "OK":
        fixed += 1
    else:
        broken += 1
    print(f"  [{state:17s}] {num}  primary={db_primary.get(num,'(absent)')!r}")
    print(f"                      source {raw!r} -> {want}")
    print(f"                      database {got}")
print(f"\n  correct: {fixed}   still wrong: {broken}")

# ----------------------------------------------------------------- CSV out --
outdir = os.path.join(HERE, "_country_audit")
os.makedirs(outdir, exist_ok=True)
stamp = datetime.datetime.now().strftime("%Y%m%d-%H%M%S")
path = os.path.join(outdir, f"august-country-audit-{target}-{stamp}.csv")
with io.open(path, "w", encoding="utf-8-sig", newline="") as fh:
    w = csv.writer(fh)
    w.writerow(["Certificate #", "Verdict", "Source cell", "Source countries",
                "DB primary Country", "DB CertificateCountries", "Missing from DB", "Extra in DB"])
    for num in sorted(source):
        want = source[num]
        got = sorted(db_set.get(num, set()))
        if num not in db_primary:
            verdict = "certificate not in database"
        elif set(want) == set(got):
            verdict = "match"
        elif not got:
            verdict = "no country rows"
        elif set(want) - set(got):
            verdict = "COUNTRIES LOST"
        else:
            verdict = "extra in database"
        w.writerow([num, verdict, " | ".join(sorted(raw_by_number.get(num, []))),
                    "; ".join(want), db_primary.get(num, ""), "; ".join(got),
                    "; ".join(sorted(set(want) - set(got))),
                    "; ".join(sorted(set(got) - set(want)))])
print(f"\nwrote {path}")
