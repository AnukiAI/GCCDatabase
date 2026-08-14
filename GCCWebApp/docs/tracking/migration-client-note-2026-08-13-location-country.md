# Locations and countries on certificates — findings + one decision (2026-08-13)

## Internal context (not sent — for our records)

**Purpose.** The client observed that some certificates appear to carry more than one country and
asked whether the design supports it. It does not: `Certificate.Location` is free text (175) and
`Certificate.Country` is a single-value dropdown (`nvarchar(100)`), single-select in
`Views/Certificate/Create.cshtml:293`.

**What the data actually shows.** Three distinct situations were being conflated, and separating
them shrinks the problem substantially:

| Group | Test env | Example | Status |
|-------|----------|---------|--------|
| Several sites, one country | 3 of 4 | 24-0304 `Buffalo, NY; Cedar Rapids, IA` / United States | Works today. No issue. |
| Sites across >1 country | 1 of 4 | 23-0925 `Gyongyos, Hungary and Marktheidenfeld` / Germany | Real limitation. ~2%. |
| Country contradicts location | — | 26-9633 `Seattle, WA` / Mexico | Data entry error, not a design gap. |

Rates are consistent across both data sets: 4 of 228 on the test env, 19 of 1,021 in the public
GreenCircle mirror (35 distinct countries). The public mirror's worst case is 25-8767 — five sites
across the US and Canada; one record spans US + China + Poland.

**The migration transform rule is currently wrong and this is the blocking part.**
`migration-import-transform-plan-2026-06-22.md` §87 says "strip trailing country into Country" for
the expired/historical set, whose `LOCATION` column is combined. Against a multi-site string that
takes the *last* country — 25-8767 would load as **Canada** with its three US sites silently
dropped from the Country field. Recommended rule: **first-listed site sets Country**, full original
string preserved in Location (175 cap), cross-border rows written to a review CSV rather than
guessed. Deterministic, and it never discards the primary plant.

**Amazon contract is not in the blast radius.** `certificationCountries` is hardcoded to the literal
`"All"` for partner callers (`Controllers/Api/v1/CertificatesController.cs:254, 318, 1067, 1668`)
per Dev Guide §4/§8. Only internal callers project `c.Country`. So a multi-country expansion would
not require partner re-certification — worth knowing, but not a reason to build it.

**Flagged internally, not in the email:** CSV import writes `Country` as an unvalidated raw string
(`Services/Import/CertificateImportService.cs:1097`, `:1771`) — no check against the dropdown list.
That is how `Atlantis` is currently sitting in the test-env Country column. Needs a DEF number and a
small validation fix before the 5,556-row load. Ours to fix, not theirs to decide.

**Not yet profiled:** the multi-country row count in the go-live source files themselves. The email
promises the exception list rather than a number — do not add a figure before that profile runs.

**Deciding question for the CR** is whether they need to *filter/search* by every country or only
*display* them. Display is nearly free (already in Location text). Filtering is the expensive one:
join table, the persisted `NormalizedCountry` column, tile filters/sort on internal + external APIs,
CSV import/export columns, and the migration transform.

---

## Client-facing email (this is what gets sent)

**Subject:** Re: GCC database — certificates showing more than one country

Hi [name],

Thanks for flagging this — you're right that the current design holds one country per certificate.
Worth separating the two fields up front, because only one of them is limited: **Location is free
text and can list every site**, however many there are, while **Country is a dropdown that holds a
single selection**.

We looked at both your test site and the public GreenCircle database to see how often this actually
bites. The short version is that what looks like "multiple countries" is three different situations,
and only one of them is a real limitation.

**1. Several manufacturing sites, all in one country — this already works.**
For example, 24-0304 lists `Buffalo, NY; Cedar Rapids, IA` with the country set to United States,
and 25-8777 lists `Athens, GA; Chowchilla, CA`. Location is listing every site, as it's meant to,
and the country is correct. Nothing needs to change for these, and they're the majority of what
you're seeing.

**2. Sites in more than one country — this is the genuine gap, and it's about 2% of records.**
Certificate 23-0925 is the clearest example: the Location reads
`Gyongyos, Hungary and Marktheidenfeld`, but the country is set to Germany — so Hungary only exists
in the free-text field and can't be searched or filtered on. Four certificates on your test site are
in this position, and 19 of the 1,021 records in the public GreenCircle database, spanning 35
countries in total. The most complex is 25-8767, which covers five sites across the US and Canada.

**3. A few where the country simply doesn't match the location.**
26-9633 is the one you pointed to — the Location is `Seattle, WA` but the country is set to
**Mexico**. That's a single site with the wrong country selected, rather than a multi-location
certificate. We can't tell from the data which is right, so these need an analyst to look at them.

**What we'd suggest**

It's worth being clear about what is and isn't limited here. **Location is a free-text field, so it
can list every site** — all of them, in as much detail as you like, and they all stay visible on the
certificate record. The only single-value part is the **Country** dropdown, which holds one
selection.

So for the upcoming data load we need one rule from you: where a certificate lists sites in more
than one country, we propose the **first site listed** sets the Country dropdown, while the Location
field carries the complete list of places exactly as provided. Nothing is lost or shortened. We'll
also produce a list of every certificate this affects — along with the group 3 mismatches like
26-9633 — so your team can review and adjust the country selection after the load rather than us
guessing during it. **If that rule works for you, no action needed.**

On expanding the system to hold several countries per certificate: it's certainly possible, but it's
a larger change than it appears — it affects the certificate structure, the search and filter tools,
the import and export templates, and the migration itself. Given it applies to roughly 2% of
records, and that the Amazon feed is unaffected either way (it sends "All" for countries rather than
the specific list), we'd suggest not taking it on before go-live.

That said, one question decides it, and it's yours to answer:

> **Do you need to search and filter certificates by every country involved, or is it enough that
> all the sites are visible on the certificate record?**

If visibility is enough, today's design already does it and we'd close this out. If you need to
filter by them, we'll scope it as a change request for after launch and give you a proper estimate.

Best,
Shawn
