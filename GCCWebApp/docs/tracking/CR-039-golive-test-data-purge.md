# CR-039 — Authorization to purge test-environment business records ahead of the go-live certificate load

**Raised:** 2026-08-17 · **Origin:** Gate 1 of `go-live-test-runbook-2026-08-14.md`; client instruction
in `ReadMe.txt` (2026-08-12) · **Status:** PROPOSED — awaiting client approval
**Blocks:** the entire go-live migration. Nothing loads until this is approved.
**Type:** authorization for a destructive data operation — **not a code change.** No application
behaviour changes and nothing ships. This CR exists because the operation deletes business records.

---

## 1. What is being asked for

Permission to run `Scripts/data-ops/2026-08-13-golive-testdata-purge.sql` against the **test
environment** (`gccwebap_certificates` @ `68.71.129.120,1533`), deleting the accumulated test
certificates, companies, products, audit history and test user accounts so the real 5,702-certificate
migration loads into a clean database.

The client already asked for this in writing:

> "All test certs/ASINs should be removed before uploading as we are preparing for go-live."
> Remove: certs, companies, products, fake users (except key test accounts).
> Do **not** remove: Cert Types, Product Cat/Type/Subcat, Tags, Pathways, Partners.
> — `ReadMe.txt`, 2026-08-12

**So the intent is not in question.** What this CR adds is the specific row counts, the specific
accounts, and the one decision the instruction does not answer (§5).

### Why it needs a CR rather than just doing it

Standing rule: any `UPDATE`/`DELETE` against business records requires a formal CR. Beyond the rule,
three things make this the wrong operation to run on a verbal go-ahead:

1. **It is not reversible from inside the application.** There is no soft-delete on `Certificates`
   (DEF-288 is still parked). Recovery is a database restore, nothing less.
2. **Test is live and in daily use by the client.** It is not an idle sandbox — the client's own
   admin logged in **three days ago** (§5).
3. **The instruction's phrase "fake users (except key test accounts)" does not define the keep-list**,
   and the current keep-list gets it wrong (§5).

---

## 2. Exactly what gets deleted — measured on TEST, 2026-08-17

Produced by the script's own report-only mode (`-v Execute=0`), which is read-only and returns before
the transaction opens:

| Table | Rows | Mode |
|---|---|---|
| **Certificates** | **229** | direct |
| CertificateProductIds | 3,202 | cascade |
| CertificateProductIdPartners | 2,644 | cascade |
| CertificatePartners | 144 | cascade |
| CertificateStatusHistory | 53 | cascade |
| CertificateTag (join) | 14 | cascade |
| CertificateCertificationTypeSubcategories (join) | 16 | cascade |
| CertificateDocuments | 0 | cascade |
| CustomFieldValues | 0 | cascade |
| **Products** | **312** | direct |
| ProductIdentifiers | 0 | cascade |
| ProductTag (join) | 0 | cascade |
| **Companies** | **173** | direct |
| CompanyUser (join) | 684 | cascade |
| **AuditHistory** | **930** | direct |
| ImportBatches | 6 | direct |
| **Users** | **83** | direct |
| UserRoles (join) | ≤87 | cascade |
| PartnerUser (join) | 12 | cascade |

> **Cascade counts are whole-table totals.** The script reports `UserRoles` / `CompanyUser` /
> `PartnerUser` as `COUNT(*)`, not as the subset that will actually cascade — so with 5 users now
> kept, the true cascade is slightly lower than shown. It over-states, never under-states, so it is
> safe for sign-off. Cosmetic; worth tightening at some point, not before this run.

> **These numbers are a snapshot and will drift.** Test held 228 certificates on 2026-08-13 and 229
> today. **Re-run the report-only mode at the moment of execution** and reconcile against the backup —
> do not execute against figures from an approval email written days earlier.

### What is explicitly preserved

Verified present on test today, and asserted by the script *inside the transaction* — if any of these
came back empty the purge rolls itself back rather than committing:

| Preserved | Rows | Why |
|---|---|---|
| CertificationTypes | 32 | client explicit |
| ProductCategories / ProductTypes | 9 / 54 | client explicit |
| ProductSubcategories | 0 | client explicit (already empty) |
| Tags | 4 | client explicit |
| Pathways | 6 | client explicit |
| Partners | 4 | client explicit |
| **PartnerApiKeys** | **9** (Amazon 8, Novi 1) | **Amazon's live integration** |
| ApiKeys | 3 | — |
| GoogleAnalyticsSettings | 1 | — |
| Roles / Permissions / PermissionRole | 5 / 22 / 41 | needed for the client's own step 3 |
| CustomFields | 0 | configuration, not test data |
| Certificates_bak_CR019 / Products_bak_CR019 | 4 / 2 | CR-019 backup tables |
| `__EFMigrationsHistory` | 34 | schema version — never touched |

**Amazon's API access is unaffected, and this is a checked fact rather than an assumption.**
`PartnerApiKeys` has **no foreign key to `Users`** — confirmed by enumerating every FK that references
`dbo.Users`; `CreatedBy`/`RevokedBy` are plain columns. Authentication is by key, not by user account.
All 8 Amazon keys survive, so deleting the `partner@amazon.com` *portal login* does not interrupt the
integration. Gate 4's contract diff still applies after the load.

---

## 3. The keep-list — was wrong, now FIXED in the script (2026-08-17)

> **RESOLVED.** The three client accounts have been added to
> `2026-08-13-golive-testdata-purge.sql` (Shawn, 2026-08-17) and the change is verified against the
> live test database: **all five entries resolve to real users, zero unmatched**, and users deleted
> drops **86 → 83**. The remainder of this section records why, and is the basis for the client
> confirmation still requested in §7.

**Every Admin account on test, and its fate — reviewed 2026-08-17:**

| Id | Email | Active | Last login | Fate |
|---|---|---|---|---|
| 1 | `swest@anukiconsulting.com` | ✔ | 2026-08-17 | **KEPT** — mandatory (`Audit:SystemUserEmail`) |
| 22 | `admin@greencirclecertified.com` | ✔ | 2026-08-12 | **KEPT** |
| 99 | **`carly@greencirclecertified.com`** | ✔ | **2026-08-14** | **NOW KEPT** — was being deleted |
| 12 | `robert.taylor@anukiconsulting.com` | ✔ | never | deleted — our seed |
| 38 | `carly@gcccc.com` | ✔ | never | deleted — throwaway domain |
| 42 | `cara.admin@example.com` | ✘ | never | deleted — QC fixture |
| 47 | `hank.badstatus@example.com` | ✘ | never | deleted — QC fixture |
| 78 | `carly@admin.com` | ✔ | never | deleted — throwaway domain |
| 82 | `carly@gcc5.com` | ✔ | never | deleted — throwaway domain |
| 83 | `carly@gcc6.com` | ✔ | never | deleted — throwaway domain |
| 84 | `carly@gcc7.com` | ✔ | never | deleted — throwaway domain |

**11 Admin accounts; 3 kept, 8 deleted — and of the 8, not one has ever been logged into.** The five
`carly@`-lookalikes on `gcccc.com` / `admin.com` / `gcc5-7.com` are ours, created during testing, and
are exactly why "keep the admin" needed the account list rather than the account *name*.

**The revised keep-list (now in the script):**

| Email | Role | Why |
|---|---|---|
| `swest@anukiconsulting.com` | Admin | **Mandatory** — `Audit:SystemUserEmail`; without it every audited write fails (DEF-337) |
| `admin@greencirclecertified.com` | Admin | GCC admin |
| **`carly@greencirclecertified.com`** | **Admin** | **client administrator, in active use** |
| `analyst@greencirclecertified.com` | Analyst | client analyst |
| `partner@amazon.com` | Partner | Amazon partner portal login |

**The principle written into the script:** a keep-list errs safe in one direction only — keeping an
account that could have been deleted is reversible in seconds; deleting one that was needed is not.
When in doubt, add the address.

---

### Why it was wrong — kept for the record

**This is the one thing in this CR that must not be waved through.**

The script keeps exactly **two** accounts: `swest@anukiconsulting.com` and
`admin@greencirclecertified.com`. Every other user is deleted. Enumerating all 86 — not sampling
them — shows they are **not** uniformly "fake users":

| | Count |
|---|---|
| Total deleted | **86** |
| Already soft-deleted (email mangled `__deleted__…`) — genuinely dead | 22 |
| **Live accounts** | **64** |
| …of which have **ever logged in** | **6** |

The six live accounts with real login history:

| Account | Role | Last login | Assessment |
|---|---|---|---|
| **`carly@greencirclecertified.com`** | **Admin** | **2026-08-14** | **the client's own admin — active three days ago** |
| **`partner@amazon.com`** | **Partner** | **2026-08-12** | partner portal login |
| **`analyst@greencirclecertified.com`** | **Analyst** | **2026-08-07** | client analyst account |
| `jane.analyst@anukiconsulting.com` | Analyst | 2026-08-04 | ours — seeded test account |
| `external.user@anukiconsulting.com` | External | 2026-06-24 | ours — seeded test account |
| `partner.amazon@anukiconsulting.com` | External | 2025-12-01 | ours — stale |

**As written, the purge deletes `carly@greencirclecertified.com`.** The client's administrator would
find herself locked out of the test environment immediately after go-live, on an account she used
three days ago — and the recovery is us recreating it with a new password, not a self-service reset.
`analyst@greencirclecertified.com` and `partner@amazon.com` are the same problem one rung down.

**Recommendation: add all three to the keep-list before running.** They are client-side accounts, not
our test scaffolding, and nothing about the migration requires their removal.

The remaining **58 live-but-never-used accounts are safe to delete** and are unambiguously ours: 13
`@anukiconsulting.com` seeds (`sarah.johnson`, `mike.chen`, …), 37 `@example.com` edge-case fixtures
built for QC (`zoe.emoji`, `formula.name`, `taylor.quotes`, `riley.comma` — import-validation test
rows), 4 `qc108-*@gcc-test.local` rows from the QC #108 run, and 5 duplicate `carly@` accounts on
throwaway domains (`gcc5.com`, `gcc6.com`, `gcc7.com`, `admin.com`, `gcccc.com`) that were never
logged into.

> `swest@anukiconsulting.com` is **mandatory**, not a preference — it is `Audit:SystemUserEmail`.
> Without it every audited write fails (the Gate 10 / DEF-337 failure mode). The script already
> pre-flight-aborts if it is missing.

---

## 4. Rollback — proven, not theoretical

A logical restore was drilled on 2026-08-13 into **both** an empty and an already-populated target:
8,823 rows restored in 24–82 seconds. `Scripts/data-ops/backup-testenv-db.ps1 -Mode Export` before,
`-Mode Restore` to reverse.

**A valid backup already exists: `db-backups/gccwebap_certificates-2026-08-13-090039`** — 43 tables,
8,823 rows, plus a schema snapshot.

**What the rollback target actually has to protect is the *preserved* set, not the certificates.**
Everything the purge deletes is test data that the migration replaces, so a stale count there costs
nothing. The tables that survive the purge — API keys, partners, cert types, categories, tags,
pathways, roles, users — are **not** reloaded by the migration, so those are what a restore has to
bring back intact. Checked against the backup on 2026-08-17:

| Preserved table | Backup 08-13 | Live 08-17 |
|---|---|---|
| Users | 88 | **88 — identical by (Id, Email)** |
| PartnerApiKeys | 9 | **9 — identical by (Id, PartnerId, ApiKey, IsActive)** |
| Partners | 4 | **4 — identical by (Id, Name)** |
| CertificationTypes / ProductCategories / ProductTypes | 32 / 9 / 54 | 32 / 9 / 54 |
| Tags / Pathways | 4 / 6 | 4 / 6 |
| Roles / Permissions / PermissionRole | 5 / 22 / 41 | 5 / 22 / 41 |
| ApiKeys / GoogleAnalyticsSettings | 3 / 1 | 3 / 1 |

The three that matter most were compared **by identity, not by count** — equal totals would look the
same if a row had been swapped ([[feedback_equal_counts_are_not_the_same_rows]]). **Nothing in the
preserved set has drifted**, so the 08-13 backup is a valid rollback target today.

The only database drift since it was taken is **two test certificates** (`26-9879` created 08-13
13:40, `26-9721` edited 08-13 18:55). Both are test data the purge deletes anyway — **no value, not a
gap in the backup.**

Two constraints that still apply:

- **Take a fresh backup in the same session anyway.** Not because the existing one is inadequate — it
  is — but because it is free, and it removes any need to reason about drift at the moment you are
  least able to. Standing runbook rule.
- **The purge runs in one transaction with post-purge assertions inside it.** A partial purge cannot
  commit; if `CertificationTypes`, `Partners`, `PartnerApiKeys` or `Pathways` were touched, or the
  audit user did not survive, it rolls back and raises. So the realistic failure mode is "nothing
  happened", not "half the database is gone".

**Uploaded PDFs are a separate restore path and are not covered by the database backup.** Zip
`wwwroot/uploads/` before the deploy that carries this.

---

## 5. Preconditions — none of these are optional

| # | Precondition | State |
|---|---|---|
| 1 | Keep-list corrected per §3 | ❌ **not done — decision required** |
| 2 | Backup taken in the same session, immediately before | procedure proven; **a valid 08-13 target already exists** (§4) |
| 3 | `sqlcmd -I` (QUOTED_IDENTIFIER ON) on every call — CR-187 computed columns fail otherwise | documented |
| 4 | `-v Execute=` supplied explicitly; there is deliberately no default | documented |
| 5 | Transaction-log headroom on test | ✅ **verified 2026-08-17 — fits, see §5A** |
| 6 | Schema prereqs applied first: DEF-349 widen, retired cert types, CR-038 filtered index | scripted |
| 7 | `wwwroot/uploads/` backed up | standing rule |
| 8 | Deploy window agreed — test is in daily client use | ❌ not scheduled |

---

## 5A. Transaction-log and file-size headroom — measured on test 2026-08-17

The dev load failed mid-purge with *"The transaction log for database … is full"*, so this was the
precondition most likely to bite. **It has now been checked directly and it fits — but the margin is
narrower than the data suggests, and the answer depends on timing.**

**Test environment, as configured:**

| | Current | Max | Growth |
|---|---|---|---|
| Data file | 72 MB (**11 MB actually used**) | **500 MB** | 64 MB |
| Log file | 72 MB | **1,024 MB** | 64 MB |

Recovery model **FULL**; `log_reuse_wait_desc` = **NOTHING** (nothing is currently blocking truncation).

**Data: ample, not a constraint.** Dev holds this exact load — 5,702 certificates, 3,439 products,
16,234 product IDs — in **39 MB allocated / 27 MB used.** Against a 500 MB cap that is better than
12× headroom. The dataset is small; it is the *number of rows*, not their size, that makes this
migration awkward.

**Log: fits, with roughly 2× margin.** Dev's log grew to **520 MB** doing this work against test's
1,024 MB ceiling. Two caveats keep that from being comfortable:

- Dev's 520 MB came from **repeated** purge/load cycles, so a single clean run should need materially
  less — but that is an inference, not a measurement.
- **The log can only truncate when a log backup runs, and that happens once a day.**

**The provider is running proper backups — this was not previously known.** `msdb` backup history
shows a regular nightly pair, unbroken from 08-08 to 08-15:

| Type | Time | Size |
|---|---|---|
| FULL | ~22:05 nightly | 20.2 MB |
| LOG | ~22:14 nightly | 0.1–0.4 MB |

**So there is a server-side `.bak` chain in addition to our logical backup**, which materially
improves the rollback story in §4 — a provider restore is available as well as ours.

> **⚠️ But the chain has stopped.** The most recent entry is **LOG 2026-08-15 22:20**. There is
> **nothing for 08-16 or 08-17.** Either the job has failed or the schedule changed. **Get this
> confirmed and restarted before the migration runs** — it is the difference between a two-day-old
> restore point and a same-night one, and it is also what keeps the log truncating.

**Operational consequence — run the migration shortly after the nightly log backup (~22:20).** That
is the moment the log is emptiest and the full 1,024 MB is available. Running mid-afternoon means
competing with whatever the day has already written and cannot yet truncate.

**Do NOT switch recovery model to SIMPLE**, which is how this was fixed on dev. Test is on a hosted
instance with an active backup chain; `SET RECOVERY SIMPLE` would **break that chain** and is the
provider's decision, not ours. It is also unnecessary — the headroom is there.

**If the log does fill anyway**, the load stops partway and rolls back — the transaction guarantees
no partial state. Recovery is to ask the provider for an on-demand log backup and re-run, not to
change any setting ourselves.

---

## 5B. Products — what is preserved, what is replaced, what is not populated

"Are products maintained?" splits into three different answers, and conflating them is how a
migration gets accepted on a wrong assumption.

**1. Product configuration (the taxonomy) — PRESERVED, untouched.**

| | Test today | Dev after the full load |
|---|---|---|
| ProductCategories | **9** | **9** |
| ProductTypes | **54** | **54** |
| ProductSubcategories | **0** | **0** |

Identical either side. **The migration maps into the existing lists rather than adding to them**, so
the purge preserving these is sufficient — nothing has to be recreated, and no category or type the
client configured is lost.

**2. Product records — DELETED and REPLACED, which is the intent.** The 312 products on test are test
data. They go, and the migration loads **3,439 real products** built on the CR-186 identity model
(one per Company + Name + Category + Type + Subcategory). Verified on dev:

| Check | Result |
|---|---|
| Products loaded | **3,439** |
| …with a Category set | **3,439 — all** |
| …with a Company set | **3,439 — all** |
| Orphan products (no certificate) | **0** |
| Certificates with no product | **0** |
| Product identifiers (ASINs) | **16,234** |

Every product is reachable from a certificate and every certificate has a product. Nothing is
stranded in either direction.

**3. Two fields that do NOT get populated — flagged, not fixed.**

| Field | Populated | Note |
|---|---|---|
| `ProductSubcategoryId` | **0 of 3,439** | **DEF-348** — no import path exists. 104 product-subcategory and 177 cert-subcategory values in the source are not stored. Known and previously reported. |
| `ProductTypeId` | **1,000 of 3,439 (29%)** | **Checked against the workbooks 2026-08-17 — this is the source, not a mapping gap. Closed, no defect.** |

**Product Type — checked and cleared.** The 29% looked like a mapping failure and is not one:

| Check | Result |
|---|---|
| Source rows carrying a Product Type | **1,254 of 5,702** |
| Certificates in the database whose product has a type | **1,254** — exact match, nothing dropped |
| Distinct Product Type values in the source | 33 |
| …that fail to resolve against the 54 configured types | **0 — every value resolves** |
| Product Type appearing in `unmappable-fields.csv` | **no** — that file holds only Subcategory (104) and Cert Subcategories (177) |

**The decisive check is the row-level one**: every source row that supplies a product type produced a
product carrying that type, 1,254 = 1,254. The workbooks simply do not supply a product type for the
other 78% of rows. *(A distinct-identity comparison gave 1,005 vs 1,000 — a 5-row discrepancy that is
an artefact of reconstructing the CR-186 identity key by hand rather than a real drop; the loader
fills Category where the source leaves it blank, so a hand-built key does not reproduce its grouping.
The row-level count is exact and is the meaningful test.)*

So `ProductTypeId` needs no action. `ProductSubcategoryId` remains genuinely unpopulated — DEF-348.

Neither is a purge concern — both are load-side, and both are the same on dev today. Documented here
because "products are maintained" would otherwise be read as "every product field is populated".

---

## 6. Risks

| Risk | Severity | Mitigation |
|---|---|---|
| **Client admin locked out of test** | **High** | §3 — correct the keep-list. This is the CR's main ask. |
| Executed against stale approval figures | Medium | Re-run report-only at execution time; reconcile to the backup |
| Client working in test during the purge | Medium | Agreed window; announce it. Test is in daily use |
| Transaction log exhaustion mid-run | **Low** — §5A: 1,024 MB cap vs dev's 520 MB across *repeated* cycles | Run just after the ~22:20 nightly log backup; the transaction guarantees no partial state |
| **Provider backup chain stopped after 2026-08-15** | **Medium** | §5A — confirm and restart before the run. Affects both the restore point and log truncation |
| Uploaded PDFs lost | Medium | Zip `wwwroot/uploads/` — not covered by the DB backup |
| Audit trail gone (930 rows) | Low–Medium | Intended: it is test-activity history. **Confirm the client agrees** it has no retention value |
| Amazon integration interrupted | **None** | Verified: no FK from `PartnerApiKeys` to `Users`; 8 keys preserved |

**One consequence worth stating plainly rather than burying:** `AuditHistory` is emptied and the
migration itself writes **no** audit rows (by design — the SQL loader bypasses the application). The
migrated certificates therefore arrive with no history at all. That is expected and matches dev, but
if the client expects to see "imported on <date> by <user>" against each record, they will not.
Better raised now than discovered after go-live.

---

## 7. Decisions required from the client

1. **Approve the purge** as scoped in §2 — 229 certificates, 173 companies, 312 products, 930 audit
   rows, **83** user accounts.
2. **Confirm the keep-list — now five accounts** (§3): `swest@anukiconsulting.com`,
   `admin@greencirclecertified.com`, `carly@greencirclecertified.com`,
   `analyst@greencirclecertified.com`, `partner@amazon.com`. The script has been corrected, so this is
   now a confirmation rather than a fix. **The remaining question is whether anyone else on the client
   side holds a test-site login we cannot see as theirs** — every other account looks like our own
   scaffolding, but only they can confirm that.
3. **Confirm the audit history has no retention value** (930 rows, all test activity).
4. **Agree a window.** Test is in daily use; the purge and load are not instant.
5. **Confirm the nightly backup job is still running.** It ran unbroken 08-08 → 08-15 and there is no
   entry for 08-16 or 08-17 (§5A). We would like it restarted before the migration — it is both the
   restore point and what keeps the transaction log truncating. *Log headroom itself is confirmed
   sufficient; this is about the backup job, not the log size.*

---

## 8. Effort

**≈ 0.5 day of elapsed time for the purge step itself**, almost all of it backup, verification and
window management — the delete is seconds. No development: the script is written, and it has been run
end-to-end on dev as part of a rehearsal that is now verified clean against the client's own
spreadsheets in both directions.

**This CR is paperwork around an operation that is already built and proven.** The cost is in getting
the keep-list right and the window agreed, not in the work.

---

## 9. Related

| Document | Role |
|---|---|
| `docs/tracking/go-live-test-runbook-2026-08-14.md` | the procedure; this CR is its Gate 1 |
| `Scripts/data-ops/2026-08-13-golive-testdata-purge.sql` | the script being authorized |
| `Scripts/data-ops/backup-testenv-db.ps1` | backup / rollback |
| `docs/tracking/CR-038-duplicate-certificate-numbers.md` | Gate 2 — separate approval, also outstanding |
| `docs/tracking/CR-037-multi-country-certificates.md` | open, independent of this |
| DEF-288 | cert soft-delete — parked; its absence is why rollback means restore |
| DEF-337 / Gate 10 | why `swest@anukiconsulting.com` is mandatory in the keep-list |

**Note on numbering:** CR-038 exists as documents but was never entered in
`change-request-register.md`, whose highest entry is CR-037. CR-039 is the next free number; CR-038
still needs its register row.
