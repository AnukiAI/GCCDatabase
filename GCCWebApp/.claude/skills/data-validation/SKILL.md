---
name: data-validation
description: Validate a set of data records against their source — migration loads, CSV imports, spreadsheet-to-database reconciliation, export contents, partner API payloads, batch loads, and any "did all the rows land correctly" question. Use when asked to reconcile, verify a load, check an import, compare two datasets or environments, confirm a count, prove an export is complete, or before/after running UPDATE or DELETE on business records. Catches the failure this project keeps hitting: the counts match and the contents are wrong.
---

# Data Validation

Every data incident here had the same shape: **a tally agreed, so nobody looked at the rows.**
Equal counts are the single most convincing wrong answer this project produces.

So the rule underneath all of this: **a validation must be able to fail.** If the check you are
about to run would produce the same output whether the data were right or wrong, it is not a
check. Delete it and design a different one.

---

## 0. Before you touch anything

**Name the environment and prove it.** Three corrections in one session came from diagnosing on
the wrong database. Verification scripts in `Scripts/data-ops/` default inconsistently — some to
dev, some to localhost. Never trust the default.

```sql
SELECT DB_NAME() AS db, @@SERVERNAME AS srv;   -- print this INTO the log, every run
```

There are two GCC databases on the test server (`gccwebap_certificates` is the live one;
`gccwebap_test` is unreferenced). Naming the catalog explicitly is not optional.

**If the run will UPDATE or DELETE business records, it needs a CR first** — number, estimate,
logged value — even if the fix is absorbed. And separate the two cases honestly: correct code with
bad rows is a **data issue**, not a defect. Filing it as a defect sends someone hunting for a code
bug that does not exist.

**Back up first** when the operation is destructive, and state where the backup is.

---

## 1. Design the check before you run it

Answer these four in writing, in the script header, before the first query:

1. **Source of truth** — which file, sheet, table, environment, and as of when. A checked-in
   artifact is a photograph: read its timestamp before citing it as current.
2. **Population** — the full set being validated, and how you enumerate it. Not a sample.
3. **Key** — the field that joins source to destination. See §2; this is where it goes wrong.
4. **What a failure looks like** — the specific row-level disagreement you expect to catch.

If you cannot answer 4, stop. You are about to write a check that always passes.

---

## 2. Choosing the key (the trap)

**`Certificate.Number` is NOT unique** — 137 numbers have more than one row. A `{number: value}`
dictionary silently keeps the last row and buries every disagreement. Any dict keyed on a
non-unique field is a data-loss step disguised as a lookup.

Before keying on anything, prove uniqueness on both sides:

```sql
SELECT Number, COUNT(*) FROM Certificates GROUP BY Number HAVING COUNT(*) > 1;
```

If it is not unique, key on the composite that is (`Number` + `Id`, or `Number` + company), or
carry a list per key and assert its length.

Related keying traps already paid for:
- Certificate **routes** take the number; certificate **asset** routes take the integer `Id`.
- `ProductSubcategoryId` FKs to `ProductTypes` — resolve by name **and** `ParentId`, or you match
  the wrong level of the hierarchy.
- A denormalized copy must not be narrower than its source column, or it truncates silently.

---

## 3. The comparison itself

**Enumerate, don't sample.** Every population, all rows. A tally once hid 6 of 15 mismatches.

**Compare key SETS in both directions.** `len(src) == len(dst)` proves nothing about membership.

```python
missing   = src_keys - dst_keys     # in source, never landed
unexpected = dst_keys - src_keys    # in destination, came from nowhere
```

Both must be reported, both must be empty, and **both must be printed even when empty** — the
printed empty set is the evidence; an unprinted one is an assumption.

**Then compare values for the intersection, field by field.** Matching key sets with mismatched
field values is the common real defect.

**Log the rows, not the summary.** `"1,247 of 1,247 matched"` is not a deliverable and cannot be
audited. Write the mismatching rows to a CSV under `Scripts/data-ops/`, dated, and cite the file.
A count in prose is not evidence.

**Check the fixture actually exercises the risk.** Dev `CertificateProductIds` is 100% ASIN, so 19
identical rows proved exactly nothing about ISBN, SKU or MPN handling. Before declaring a check
green, confirm the data it ran against contains the case you are worried about — and say which
cases were absent.

**An absence claim needs a stated scope.** "Not recorded anywhere" from a 26-line grep was simply
false. If you report that something is missing, state exactly what you searched: which paths, which
pattern, how many files.

---

## 4. Values that lie

| Looks like | Actually is |
|---|---|
| Excel cell showing `23%` | stored `0.23` — read the **displayed** value; normalise storage before removing any formatter |
| A blank cell in a client spreadsheet | **their answer**, not a gap to fill in |
| A date with Unspecified Kind | shifted by `UtcDateTimeConverter` — pre-format calendar dates as `yyyy-MM-dd` |
| A blank dropdown value on a form | the stored value was outside a filtered subset and gets rewritten on save |
| An empty column in an API response | often the field is never projected, not never populated — check the projection |
| HTTP 200 on a login POST | the field is `Email` (it accepts a username too); posting `Username=` re-renders the login page as 200 |
| A 404 on a certificate asset | may be role-gating, not a missing file — sign in, then assert magic bytes |

**Certification Results is verbatim free text** (CR-045 deleted `DisplayFormatters`). Never
re-introduce a display-time conversion on it, and never "normalise" it during a validation.

---

## 5. Scoped data — validate the denial, not just the view

For anything partner- or company-scoped, a passing check on one account proves only that it *sees*
its own rows. It can never prove another party's rows are *withheld*. Always validate in pairs:

- `test.external` (Sika) against `test.external2` (MAPEI)
- `test.amazon` against `test.novi` — but **Novi has 0 tagged certs on TEST**, so that pair is
  vacuous; use `test.external` as the control instead.
- `pru.multi@example.com` holds Amazon **and** Novi — the only fixture that can expose a
  co-partner leak.

Partner scoping applies **two** filters, cert-level association first. A validation that checks
only the company filter passes on data that leaks.

And when you fix scoping anywhere, validate the adjacent endpoints in the same pass: the list
endpoint gets filtered and the detail, single-view and download endpoints get missed.

---

## 6. Asserting correctly

- **Assert the positive terminal state.** `!/Loading/` passed on the text "Retrying... (1/3)".
  Assert what success looks like, not the absence of one failure string.
- **An async assertion must wait, and must record the wait.** Measuring immediately records
  absence; a blanket sleep hides regressions. Poll for the condition and log how long it took.
- **A page turn must assert same size, not just difference.** A byte comparison passed while page 2
  rendered 23% more rows.
- **Never fabricate a status code.** A timeout is a timeout; it is not "HTTP 300".
- **Verify redaction by literal-grep from the source values.** Scanning your own output forward is
  unfalsifiable. Take each secret's literal string and grep the artifact for it — including
  comments, which are a value slot.
- **A test below the defect's layer cannot see it.** Nine green unit tests with the bug still live.
  Validate at the layer the user meets.

---

## 7. Performance measurements are data too

If the validation is about speed: **count logical reads, not seconds.** The same SQL on this box
timed 2.83s and then 142.99s. Never build while a gate measures — serialise, and re-run any timeout
on its own. And stale statistics look exactly like a missing index; compare the healthy
environment's indexes **and** its statistics before designing a new one.

---

## 8. Prior art — reuse before writing

`Scripts/data-ops/` already holds working reconciliations. Read the closest one first:

| Pattern | Script |
|---|---|
| Source file validated before load | `2026-06-22-migration-source-validation.py` |
| Full migration reconciliation | `2026-08-13-migration-reconciliation.py` |
| Spreadsheet to database, both directions | `2026-08-15-verify-spreadsheet-to-database.py` |
| Child-collection (ASIN) reconciliation | `2026-08-18-asin-reconciliation.py`, `2026-08-21-asin-per-company-reconciliation.py` |
| Field-level reconciliation | `2026-08-18-reconcile-productid-fields.py` |
| Batch load verification | `2026-09-03-batch2-VERIFY.py`, `2026-09-08-batch3-VERIFY.py` |
| Change-request verification | `2026-09-08-CR043-CR045-verify.py` |

Name new scripts `YYYY-MM-DD-<what>-VERIFY.py` and make them idempotent. Seeds and fixtures must be
transactional and additive, with DELETEs scoped to their own markers — `Scripts/ci/seed-e2e.sql` is
the model: its only deletes match `Number LIKE '98-9%'` and `ZZ_CI_%`, and the real certificate
count was identical before and after.

Scripts that write data go in `post-deploy/`.

---

## 9. Reporting the result

State, in this order:

1. Environment (`DB_NAME()` output, verbatim) and timestamp of the run.
2. Population size on each side, and how it was enumerated.
3. `missing` and `unexpected` key sets — printed, even when empty.
4. Field-level mismatch count **and the path to the CSV of those rows**.
5. What the fixture could **not** exercise.
6. A plain verdict. If it failed, say so with the output.

Then run the `gate-check` skill before the number leaves your hands. A figure quoted to a client or
a partner is exactly its trigger — and "the counts reconcile while the contents are wrong" is the
specific failure it exists to catch.
