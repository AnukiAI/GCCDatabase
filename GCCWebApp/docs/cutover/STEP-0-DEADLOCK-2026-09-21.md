# ⛔ Cutover step 0 is a DEADLOCK — correction to the runbook

**Raised:** 2026-09-21 · **Applies to:** `database.greencirclecertified.com-runbook.md` § "Step 0"

> **MERGE ME.** This belongs inside the runbook's Step 0, immediately before
> *"### The fix — two edits, TTL only"*. It could not be written there directly — the runbook file
> was locked by another process (OneDrive on-demand placeholder, open in an editor) at the time.
> Once merged, retitle the original heading to
> *"### The fix — two edits, TTL only (valid only if 0-probe below SAVES)"* and delete this file.

---

## 🔴 UPDATE 2026-09-22 — DEADLOCK CONFIRMED. Probe rejected.

**0-probe was run and returned the identical error:**

> greencirclecertified.com's "TXT" records have mismatched TTL values (14400 and 3600).
> Records of the same name and type must always have the same TTL value.

**The inference below is now an observation.** A TTL-only edit — changing nothing but a number, on a
record whose own name holds exactly one TXT entry — was refused. cPanel is validating the whole zone
by record type and ignoring the name, and it will not accept any save that leaves two TTLs present.
Since no single-record save can reach a consistent state, **the zone is unmodifiable through the UI.**

**Consequence: step 0 cannot be cleared by us.** Every DNS step in both phases sits behind it, so
this is now the top item on the go-live critical path — ahead of DEF-433.

### The escalation path, in order

| # | Step | Cost | Note |
|---|---|---|---|
| **E1** | **Filter the zone editor to TXT and read the TTL column.** Look for a single *name* showing two different TTLs. | 30 sec | The last cheap out. Externally every name is internally consistent, but a zone cannot be fully enumerated from outside — a duplicate record invisible to DNS queries would be a genuine RRset violation and a one-edit fix. **Do this before opening a ticket.** |
| **E2** | Find a **whole-zone / raw zone-file editor** — *Advanced Zone Editor*, *Edit DNS Zone*. Commits every record in one transaction, which sidesteps the deadlock entirely. | 5 min | Many HostMonster shared plans have retired this. If it is not in the menu, it is not available. |
| **E2b** | **Try cPanel's *Email Deliverability* page** (Email → Email Deliverability). It manages SPF/DKIM/DMARC through a **different code path** from the zone editor and may write the record without hitting the same validator. | 5 min | Cheap, worth trying before the ticket. It will not touch `_cpanel-dcv`, so it is at best half the fix — but if it can move `_dmarc` to 3600, the zone becomes one legal single-record edit away from consistent. |
| **E3** | **HostMonster support ticket** — text below. They edit the zone file beneath the broken validator. | **days** | ✅ **Unblocked — Shawn confirmed full DNS access 2026-09-22.** She can raise this herself; no client dependency. |
| **E4** | **Fallback: move the zone to Cloudflare DNS.** | ~0.5 day + registrar propagation | Strategic, not just a workaround — see below. |

### ✅ OWNERSHIP RESOLVED — 2026-09-22

**Shawn has full access to the DNS.** An earlier revision of this document recorded E3 as blocked on
the client holding the HostMonster account, and warned that finding out who could file the ticket
would cost days on the critical path. **That is no longer true and the warning is withdrawn.**

Consequences:

- **E3 can be raised tonight, by us, with no client involvement.** It is the long pole — a support
  queue is measured in days — so it should go in **before** E1/E2b are exhausted, not after. A ticket
  that turns out to be unnecessary costs nothing; a ticket raised three days late costs three days.
- **Go-live plan §5 item 1** (*"who edits DNS at HostMonster?"*), listed as the top client blocker
  with a due date of Fri 26 Sep, is **answered**. It should come off the client letter.
- **E4 (Cloudflare) becomes practical rather than theoretical** — the migration is executable by us.
  It remains the client's zone and therefore their commercial decision, but it is no longer gated on
  obtaining access.

### ❌ "Delete them tonight and re-enter them" — the content is right, the mechanism is blocked

**The idea is sound and it is exactly what belongs in the ticket.** Delete both 14400 records, leaving
the four apex records uniform at 3600 — a legal zone — then re-add `_dmarc` at 3600. The end state is
correct.

**It cannot be performed through the UI, because the first delete is itself a save that must pass the
validator, and it cannot.** The zone holds **four TXT at 3600 and two at 14400**. Check every single
operation against the state it leaves behind:

| Operation | Leaves | Uniform? |
|---|---|---|
| Delete `_dmarc` | `3600×4` + `_dcv 14400` | ✗ |
| Delete `_cpanel-dcv` | `3600×4` + `_dmarc 14400` | ✗ |
| Delete any one apex record | `3600×3` + `14400×2` | ✗ |
| Edit any one `_` record → 3600 | `3600×5` + one at `14400` | ✗ |
| Edit any one apex → 14400 | `3600×3` + `14400×3` | ✗ |

**No single operation reaches uniformity** — the split is 4/2, so at minimum *two* records must change
before the zone is legal. Every path to the good end state runs through an illegal intermediate state,
and the editor rejects each one. This is the same wall the original 0a→0b sequence hit, and it is why
the very first delete failed. **Re-entering the records afterwards was never the problem; getting the
first delete accepted is.**

### ⚠️ And if it *had* worked — what the actual risk was

Worth stating plainly, because the fear driving the question was *"I will take their site down,"* and
that is not the exposure:

> **TXT records cannot take a website down.** Site availability rides on **A and CNAME** records, none
> of which this touches. What TXT records carry here is **mail authentication and service
> verification**. The exposure is the client's **email**, not their website.

| Record | If deleted | Risk |
|---|---|---|
| `_cpanel-dcv-test-record` | Nothing. A spent AutoSSL validation artifact cPanel wrote itself | **None** |
| `_dmarc` | DMARC policy stops being published. Mail is filtered *less* strictly, not more; `rua=` reports stop | **Low**, and briefly. It relaxes a control rather than breaking delivery |
| apex `v=spf1 …` | **Mail from the domain starts failing SPF.** Under the domain's own DMARC `p=quarantine`, legitimate mail is then quarantined — including the client's Microsoft 365 | 🛑 **Severe. Never touch this record.** |
| apex `MS=ms49326145` | Microsoft 365 domain-verification token removed | Medium — M365 re-verifies periodically |
| apex `google-site-verification` / `apple-domain-verification` | Loss of Search Console / Apple service verification | Low |

**So the two `_`-prefixed records are genuinely safe to remove and the four apex records are not** —
which is exactly the split the ticket asks for. The instinct to be careful was right; it was simply
pointed at the wrong record set.

> **Before any zone change, export or screenshot the complete zone — every record, with type, value
> and TTL.** Once a record is deleted the editor no longer shows what it was. This is the same lesson
> as the `db.` CNAME target: a rollback target that has never been recorded is not a rollback plan.

### E4 — why Cloudflare is worth raising now rather than later

It is worth putting in front of the client **in the same message as E3**, because it fixes a *second*
problem the runbook already flagged as possibly unsolvable:

> Runbook step 7b requires `db.`'s TTL to be lowered to **300** at least 24 h before go-live, so the
> cutover *and its rollback* propagate in minutes. Every A and CNAME in this zone sits uniformly at
> 14400. If the same by-type validator applies to A records — and we now know it applies to TXT —
> **step 7b is impossible too, and the five-minute rollback guarantee on go-live day does not exist.**

Moving the zone to Cloudflare gives unrestricted per-record TTL control and restores that guarantee.

| | |
|---|---|
| **Pro** | Fixes step 0 and step 7b permanently; free; fast propagation; rollback guarantee restored |
| **Con** | Every record must be replicated **exactly** — the apex Squarespace records, MX to Microsoft 365, the root SPF, the Squarespace verification CNAME. A missed record breaks the client's mail or their marketing site |
| **Risk control** | Export the full zone first and diff it record-by-record after the move. Do **not** attempt it inside the go-live window |
| **Decision** | Client's — it is their zone and their nameservers |

**Recommended sequence: E1 → E2 → E3 today, and raise E4 to the client in the same breath as a
decision to be made with lead time.** Do not wait for E3 to fail before mentioning E4.

---

## What happened

Shawn attempted **step 0b — delete `_cpanel-dcv-test-record`** in the HostMonster cPanel zone editor.
It was rejected. The natural next move — *"the delete is blocked, do the other edit first"* — is also
wrong. **Neither edit can be performed.**

## Why — the runbook's two-step fix was never executable

The validator refuses a save while two TTLs exist among the zone's TXT records, and cPanel's zone
editor commits **one record per transaction**. Enumerate every single-record save available, and look
at the state each one *leaves behind*:

| Attempt | Resulting TXT TTLs | Verdict |
|---|---|---|
| Delete `_cpanel-dcv` (step 0b) | `3600×4` + `_dmarc 14400` | **rejected — still two TTLs** |
| `_dmarc` → 3600 (step 0a) | `3600×5` + `_dcv 14400` | **rejected — still two TTLs** |
| `_dcv` → 3600 (the 0b alternative) | `3600×5` + `_dmarc 14400` | **rejected — still two TTLs** |
| Any one apex record → 14400 | `3600×3` + `14400×3` | **rejected** — and forbidden, one of them is the SPF |

**The zone needs two TXT changes to become consistent; the UI grants one at a time.** No ordering of
single-record saves ever reaches a legal state. Step 0a-then-0b fails in either order, which is
exactly the symptom that was hit.

### Measured vs inferred

**Measured** — `Resolve-DnsName -Type TXT -Server ns1.hostmonster.com`, 2026-09-21:

| Name | Type | TTL |
|---|---|---|
| `greencirclecertified.com` | TXT ×4 — root SPF, `MS=ms49326145`, `apple-domain-verification`, `google-site-verification` | **3600** |
| `_dmarc.greencirclecertified.com` | TXT | **14400** |
| `_cpanel-dcv-test-record.greencirclecertified.com` | TXT | **14400** |

Both `_`-prefixed records are **still present**, which independently confirms the rejected delete
changed nothing in the zone.

**Inferred** — that cPanel validates the *resulting* whole-zone state by type. This is deduced from
the error text plus the fact that a *delete* was refused. It fits every observation, but it is not
proven: the zone cannot be enumerated from outside and we do not hold the login.

### Also confirmed unchanged (2026-09-21, authoritative from `ns1.hostmonster.com`)

| Record | Value |
|---|---|
| `db.greencirclecertified.com` | **CNAME → `cname.vercel-dns.com`** — the legacy Vercel site, untouched |
| `database.greencirclecertified.com` | **A → `70.32.82.212`** — the nginx shim, untouched |
| `dbtest.greencirclecertified.com` | **A → `64.27.52.66`** — untouched |

**Nothing in the zone has been damaged.** No rollback is required.

---

## Do this instead — in this order

| # | Step | Cost | Purpose |
|---|---|---|---|
| **0-probe** | In the zone editor, edit **`_dmarc` TTL 14400 → 3600**. Change the **TTL field only** — leave the value byte-identical (`v=DMARC1; p=quarantine; rua=mailto:security@pointsolve.com`). Save. | 2 min | **Decides which world we are in.** If it **saves**, the validator is looser than feared and the original two-step fix works — go straight to 0b. If it is **rejected with the same error**, the deadlock is confirmed and single-record edits are dead. Use the *edit*, not the delete: equally decisive, and it cannot lose a record. |
| **0-bulk** | Look for a **whole-zone** editor — cPanel *Advanced Zone Editor*, *Edit DNS Zone*, or any raw zone-file view. These commit the entire zone in one transaction and sidestep the deadlock outright. | 5 min | The clean escape, if HostMonster still exposes it on this account. |
| **0-ticket** | If 0-probe is rejected and 0-bulk does not exist, **open a HostMonster support ticket** (text below). They correct TTLs at the zone-file level, beneath the broken validator. | days | **Raise it the same day.** It is the only remaining route and it is the one with lead time. |

> 🛑 **Schedule consequence.** Step 0 blocks **every** DNS step in **both** phases. If it needs a
> support ticket, step 0 is on the critical path for go-live, and the ticket must be raised
> immediately — not when Phase 1 is otherwise ready. A support queue measured in days does not fit
> inside the D-7 pre-stage window.

> ⚠️ **Do not touch the four apex TXT records.** One is the root SPF, already at five of ten
> permitted lookups, and it is the single record that can break mail for the whole domain —
> the client's Microsoft 365 included.

---

## Ready-to-send support ticket

```
Subject: Zone editor rejects all saves — TXT TTL validation error on greencirclecertified.com

The cPanel DNS zone editor for greencirclecertified.com refuses every save with:

  "greencirclecertified.com's 'TXT' records have mismatched TTL values (14400 and 3600).
   Records of the same name and type must always have the same TTL value."

The zone's TXT records are:

  greencirclecertified.com                          TXT x4   TTL 3600
  _dmarc.greencirclecertified.com                   TXT x1   TTL 14400
  _cpanel-dcv-test-record.greencirclecertified.com  TXT x1   TTL 14400

These are three different names and each name is internally consistent, so there is no
RFC 2181 section 5.2 RRset violation. The validator appears to be grouping TXT records by
type across the whole zone and ignoring the name.

This leaves the zone unmodifiable. Correcting it requires two TTL changes, but the editor
only saves one record at a time, and every possible single-record change still leaves two
TTL values present and is therefore rejected. There is no order of operations that clears
this from the UI.

Please set the TTL to 3600 on both _dmarc and _cpanel-dcv-test-record at the zone-file level.
(Alternatively, remove _cpanel-dcv-test-record entirely — it is a spent AutoSSL DCV artifact
with no ongoing function.)

Please do NOT modify the four apex TXT records. One of them is our SPF record and it must
not change.
```

---

## Carry forward

**When a control panel rejects an edit, enumerate every alternative edit and check the state each one
*leaves behind* before recommending an order.** The runbook prescribed "two TTL-only edits" and named
both correctly — but never asked whether the *intermediate* state after the first edit was legal. It
was not, in any order, so a procedure that reads as a simple two-step fix had a zero percent chance of
working. The cost of that omission was a failed attempt on a client-owned production zone and the
false impression that something had been broken.

Companion to the existing carry-forward from the same day: *an error message from a hosting control
panel is a claim, not a diagnosis.* The extension is that **a fix derived from that claim is a
hypothesis too, and it needs its own feasibility check** — walk the state machine, not just the
endpoint.
