# QC #218 — Certificate Number Format & Year Rollover

**Anuki Consulting response — 4 August 2026**
Re: certificate numbers appearing as `26-XXXXX` (7 characters) instead of `26-XXXX` (6)

---

## Short answer

You were right on both counts.

1. **You did not run out of numbers.** The 5-digit numbers were caused by a defect in how we picked the
   next number, not by exhausting the 2026 range. We have fixed it.
2. **The year does roll over correctly.** `27-0001` in January 2027 — and we have now proven this with
   automated tests rather than simply asserting it. Results are below.

We also want to correct our earlier reply. On 23 June we told you *"nothing is broken in the code — it's
behaving as designed"* and attributed `26-10095` to imported test data. The data observation was right, but
the conclusion was wrong. That imported number did not just sit there looking odd; it changed the behavior
of the system for every certificate created afterwards. We should have caught that in June.

---

## What actually went wrong

Certificate numbers are issued by taking the highest number already used this year and adding one.

The flaw: that calculation looked at **every** number beginning with `26-`, including numbers that had
arrived through CSV import or the legacy data migration rather than being issued by the application. The
4-digit format is a *minimum* width, not a maximum — so nothing stopped a number from growing wider.

One imported record, `26-10095`, was enough to break the sequence permanently:

| | |
|---|---|
| Highest `26-` number found | `26-10095` (imported, not issued by the system) |
| Next number issued | `26-10096` — 5 digits |
| Next number after that | `26-10097` — and so on, for the rest of the year |

Each new number re-seeded the next, so the sequence could never return to 4 digits on its own. That is why
you kept seeing 7-character numbers on a database holding only ~265 certificates. Your instinct — *"I don't
think we ran out of numbers with 6 digits"* — was exactly correct.

## What we changed

The number allocator now only considers numbers within the standard 1–9999 range when deciding what to
issue next. Imported or out-of-band numbers are left alone — they remain valid and unique — but they can no
longer influence what the next certificate is called.

This implements your stated rule precisely:

> *"I am ok for it to be 7 digits IF WE RUN OUT OF numbers for the current year (so after 9999 certs)."*

So there is **no hard cap**. If you genuinely issue 9,999 certificates in a single year, the next one
becomes `26-10000` and continues from there. The number widens only when the year is truly full — which is
the behavior you asked for.

---

## Year rollover — you asked us to confirm we had tested it

> *"Anuki please confirm you have tested that when we hit a new calendar year it changes the first 2
> numbers (e.g., 2027 should have 27-XXXX)."*

**Confirmed — and we want to be transparent about how we got there.**

When we reviewed our own evidence, our original rollover test did not truly answer your question. It showed
that older years' numbers are ignored, but it could not actually move the calendar forward, because the
system read the real clock directly. In other words, we were describing what the code does rather than
demonstrating it. Given we had already given you one incorrect answer on this item, that was not good
enough.

We made a small internal change so tests can supply a specific date. This does not alter how the
application behaves in any way — in normal operation it still reads the real system clock — it simply
allows us to fast-forward the calendar in testing and observe the result.

### Test results — 16 of 16 passing

| What was tested | Result |
|---|---|
| **The direct answer:** 2026 filled with `26-0001`, `26-9999` **and** the problem number `26-10095`, then the clock moved to 1 January 2027 | **`27-0001`** ✔ |
| Rollover happens at the calendar boundary: 31 Dec 2026, 23:59:59 → then 1 Jan 2027, 00:00:00 | `26-0501`, then **`27-0001`** ✔ |
| Prefix in 2027 / 2028 / 2030 / 2099 | `27-`, `28-`, `30-`, `99-` ✔ |
| Each year counts independently (2026 volume does not affect 2027) | ✔ |
| **The original defect:** an imported `26-10095` present alongside `26-0265` | Issues **`26-0266`**, not `26-10096` ✔ |
| Empty year starts at 0001 | `26-0001` ✔ |
| Normal sequence stays 4 digits | `26-0266` ✔ |
| Year genuinely full at `26-9999` — your accepted condition for widening | `26-10000` ✔ |

Crucially, in the rollover test we deliberately left the contaminated `26-10095` in the 2026 data. The 2027
sequence still starts cleanly at `27-0001`. Nothing carries across the year boundary.

We also re-ran the wider automated test suite to confirm this change broke nothing else. It did not.

---

## Two things you should know

**1. This is not live yet.** The fix is complete and tested but is part of the next deployment bundle. Until
that deployment happens, gccwebapp.com will continue to issue 7-character numbers. Your 30 July note
(*"No change – still creating XX-XXXXX"*) was testing the currently deployed build, which predates the fix —
so that observation was expected, not a sign the fix failed.

**2. Certificates that already have a 5-digit number will keep it.** This fix governs newly issued numbers
only. Records such as `26-10095` retain their current numbers.

We have deliberately **not** renumbered them. A certificate number is your primary business identifier, and
rewriting existing ones is a data change rather than a defect fix — it would need a formal Change Request
and a decision from you. Two options:

- **Leave them.** They are valid and unique; they simply look inconsistent.
- **Remove or renumber them.** Straightforward while the environment is still pre-launch test data. Once
  real certificates are in play, this becomes a much more sensitive change.

Please let us know which you would prefer, and whether any of the affected records represent real
certificates rather than test data.

---

## Summary

| Item | Status |
|---|---|
| 5-digit numbers caused by running out of numbers? | **No** — caused by a defect in number selection |
| Defect fixed? | **Yes** — issues 4-digit numbers, widening only after 9,999 in a year |
| Year rollover to `27-XXXX` confirmed? | **Yes** — verified by automated test, 16/16 passing |
| Deployed? | **Not yet** — in the next deployment bundle |
| Existing 5-digit records? | Unchanged — **awaiting your decision** (see above) |

Reference: DEF-322, QC log item #218.
