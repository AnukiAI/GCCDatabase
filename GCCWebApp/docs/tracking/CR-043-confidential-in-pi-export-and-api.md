# CR-043 — Confidential certificates must reach the PI export and the partner/Amazon API

**Status:** **PROPOSED — NOT BUILT.** No code changed. Awaiting Shawn's go and the client's answer
to §7.
**Raised:** 2026-09-02 (client, via Shawn, from the QC log)
**Answers:** the open Confidential question in `docs/tracking/2026-08-24-client-questions-partner-api.md` (CR-035)
**Register row:** `docs/tracking/change-request-register.md` → CR-043
**QC log reply:** `docs/tracking/qc-log-update-2026-09-02-CR043-confidential-pi-export.md`
**Environment for every figure below:** the client's TEST environment (gccwebapp.com), read live
2026-09-02 — [[feedback_diagnose_on_clients_environment]]

> **QC # not yet known.** Shawn: paste the QC item number and Reporter into the register row and the
> QC reply doc before sending — [[feedback_def_entries_note_qc_id]]. Everything else is filled in.

---

## 1. What the client said

> *"It appears any certs marked 'Client Paid - Confidential' are not included in the PI export file
> (which I assume would also mean they would not go to Amazon). This is not accurate. They must be in
> export and go to Amazon when they are active since that is how they get badged. The Confidential
> piece is just for the public DB side to not show there. Make sure Client Paid - Approved, Client
> Paid - Confidential, and Republished statuses would all be in PI export and passed in API assuming
> within eff/exp date period.*
>
> *Example certs not in PI export due to this: 23-0758, 24-5426, 24-5427, 24-5428, 24-5712, 24-5713,
> 24-5714, 24-5773, 25-0479, 25-0480, 25-0481, 25-0482"*

**Naming note (no ambiguity, correct it once):** the stored values are `Client Approved - Confidential`
and `Client Approved - Paid`. The client writes "Client Paid - …". Same statuses.

---

## 2. Why this is a CR and not a defect

**We asked this exact question nine days ago and were told the opposite.**

On **2026-08-24**, `docs/tracking/2026-08-24-client-questions-partner-api.md` (CR-035) put it in
writing:

> *"Which certificate states become partner-visible, and under what label? **(Recommendation:
> Confidential stays hidden.)**"*

and, more directly:

> *"**Client Approved – Confidential is paid-but-embargoed** — you deliberately chose to keep those
> private. Our recommendation is that Confidential stays hidden regardless of what else changes, but
> we need that stated rather than assumed."*

The current behaviour is therefore the **documented, deliberate position**, flagged as an open
decision and explicitly awaiting this answer. The client is now giving it — the other way.

**The export side matches its own spec too.** CR-184 defined an "active product identifier" as one
whose parent certificate is *publicly live*, inheriting CR-015. `ExportController.DownloadActiveProductIds`
implements that faithfully.

Correct code + a requirement the client wants changed = CR ([[feedback_data_issue_vs_defect]]).

**The requirement we never captured.** We read "Confidential" as *keep it private, full stop.* The
client means **"absent from the public database, but still commercially active."** Confidential is
paid, in-force work that still needs its badge; only the public listing is suppressed. That
distinction is nowhere in the requirements baseline and is the substance of this CR.
Recorded as [[feedback_confidential_is_embargo_not_inactive]].

**This is the second time.** `docs/qa/gap-report.md` **G12** already records that DEF-223 (2026-05-26)
moved Confidential scope and was *"tracked as a DEF, not a CR — docs never updated."* Four months
apart, same status, same misclassification. If CR-043 is approved, close G12 in the same pass so the
docs stop disagreeing with the code.

---

## 3. Root cause — one list doing two jobs

`CertificateStatusHelper.PublicStatuses` = `{Client Approved - Paid, Republished}`
(`Models/CertificateStatus.cs:65`) is a single array answering **two different questions**:

| Surface | Confidential should be | Today |
|---|---|---|
| Anonymous public database | **excluded** | excluded ✅ |
| PI export + partner/Amazon API (badging) | **included** | excluded ❌ |
| Logged-in company (External) view | *undecided* — §7 Q1 | excluded |
| Internal dashboard "Active" counts | *undecided* — §7 Q2 | excluded (DEF-223) |

Because the public site and the badge feed read the same array, embargoing a certificate from the
public listing silently pulls it out of Amazon's feed as well.

**The badging intent was already half-written.** `Controllers/Api/v1/CertificatesController.cs:1693`
already maps `Client Approved - Confidential` → `ACTIVE` in the GS1 status mapper:

```csharp
var activeStatuses = new[] { "Active", "Client Approved - Paid",
    "Client Approved - Confidential", "Client Approved - Not Paid", "Republished" };
```

That line is **unreachable today** — the query filter at line 1509 drops the row before it ever
reaches the mapper. Someone understood the intent; the visibility gate overrode it.

---

## 4. Measured impact (TEST, 2026-09-02)

### 4.1 Headline

| Measure | Count |
|---|---:|
| `Client Approved - Confidential` certificates inside their eff/exp window | **37** |
| — of those, carrying **active product identifiers** | **11** |
| — of those, carrying **none** | 26 |
| **Product-identifier rows added to the PI export** | **110** |
| PI export rows today (Paid + Republished) | 16,081 |
| Confidential certs tagged to **Amazon** (become API-visible) | **17** |
| Confidential certs with **no partner tag** (reach no partner either way) | 20 |

### 4.2 The client's example list is exactly right — and exactly complete

Every one of the **11** PI-bearing Confidential certificates appears in their list, and there are
**zero** they missed. Verified by set comparison in both directions, not by count
([[feedback_equal_counts_are_not_the_same_rows]]).

| Certificate | Company | Effective | Expiration | Partner | Active PIs |
|---|---|---|---|---|---:|
| `24-5426` | First Quality Consumer Products | 2024-07-11 | 2027-07-10 | Amazon | 8 |
| `24-5427` | First Quality Consumer Products | 2024-07-11 | 2027-07-10 | Amazon | 2 |
| `24-5428` | First Quality Consumer Products | 2024-07-11 | 2027-07-10 | Amazon | 14 |
| `24-5712` | Domtar Corporation | 2024-11-25 | 2027-11-24 | Amazon | 8 |
| `24-5713` | Domtar Corporation | 2024-11-25 | 2027-11-24 | Amazon | 2 |
| `24-5714` | Domtar Corporation | 2024-11-25 | 2027-11-24 | Amazon | 2 |
| `24-5773` | First Quality Consumer Products | 2024-12-01 | 2027-07-10 | Amazon | 65 |
| `25-0479` | Block Inc. | 2023-11-01 | 2026-10-31 | Amazon | 3 |
| `25-0480` | Block Inc. | 2023-11-01 | 2026-10-31 | Amazon | 2 |
| `25-0481` | Block Inc. | 2023-11-01 | 2026-10-31 | Amazon | 1 |
| `25-0482` | Block Inc. | 2023-11-01 | 2026-10-31 | Amazon | 3 |
| | | | | | **110** |

### 4.3 ⚠️ `23-0758` is a different cause and this CR will NOT fix it

| Certificate | Company | Effective | Expiration | Status |
|---|---|---|---|---|
| `23-0758` | First Quality Consumer Products | 2023-09-01 | **2026-08-31** | **`Expired`** |

It is not Confidential. It expired on **31 August 2026** — two days before the client wrote — and
left the export on expiry, which is correct behaviour. **It must be called out before delivery**, or
it will still be absent afterwards and read as a failed fix
([[feedback_verify_through_client_click_path]]).

Note this is the *same certificate* already explained to the client on 2026-09-02 in
`qc-log-update-2026-09-02-certs-23-0358-23-0758-25-0351.md`. It has now been reported twice as
missing, from two different angles. The expiry explanation evidently did not land — worth stating
plainly rather than referring back.

### 4.4 The 26 Confidential certs with no product identifiers

They add **nothing** to the PI export (no identifiers to export). Six of them are Amazon-tagged
(`24-0001`–`24-0005`, `25-0478`) and will become visible through the certificate API without
contributing badge data. The remaining 20 carry **no partner tag at all**, so no partner sees them
regardless of status — partner delivery is a two-condition gate
([[feedback_partner_scoping_two_filters]]). They are mostly ASR Group / Domino Sugar entities.

If the client expects any of those badged, that is a **partner-tagging data task**, separate from
this CR.

---

## 5. Scope

Add a second status list and repoint the badging surfaces at it. **`PublicStatuses` is not modified**
— the anonymous public site keeps hiding Confidential, exactly as the client confirms.

```csharp
/// Statuses that are commercially in force and therefore badgeable — the partner/Amazon
/// surface and the product-identifier export. Differs from PublicStatuses by Confidential:
/// paid-but-embargoed work is hidden from the PUBLIC database only (CR-043, client 2026-09-02).
public static readonly string[] BadgeableStatuses = new[]
{
    StatusClientApprovedPaid,
    StatusClientApprovedConfidential,
    StatusRepublished
};
```

**Repoint (9 partner branches + 1 export):**

| File | Lines |
|---|---|
| `Controllers/ExportController.cs` | 293 (PI export) |
| `Controllers/Api/v1/CertificatesController.cs` | 183, 401, 458, 781, 882, 1094, 1352, 1509, 1636 |

**Deliberately unchanged — public surfaces:** `Controllers/Api/CertificateTilesApiController.cs`
(111, 269, 459), the public gates in `Controllers/CertificateController.cs`, and
`Models/CertificateVisibility.PubliclyLiveExpression`.

**Unchanged pending §7 Q1 — External/company branches:** `CertificatesController.cs` 212, 1128, 1544.

**Effective/expiration window: no change.** The client's *"assuming within eff/exp date period"*
confirms the existing DEF-358 rule. `WhereEffectiveDateReached()` already enforces the lower bound
and the daily expiration sweep the upper. Both stay as they are.

---

## 6. Risk and test impact

- **Partner API response *shape* is unchanged** — same fields, more rows — so this stays inside
  [[feedback_amazon_api_immutable]]. No contract re-baseline, no partner sign-off needed on shape.
  Amazon does begin receiving 17 certificates it has never seen, which is the client's explicit
  instruction and should be stated in the delivery note.
- **`Scripts/smoke-prod/_security/partner-isolation.js` and `rbac-matrix.js` will go red on purpose.**
  Both assert the current narrower status set. Re-baseline them in the same change (the CR-036
  precedent). **Keep every isolation assertion intact** — a wider *status* set must not widen
  *partner* scope; Amazon must still never see a Novi-only certificate
  ([[feedback_partner_data_isolation]]).
- **Adjacent-endpoint sweep required.** Status filtering has leaked between list / detail / single /
  download endpoints repeatedly (DEF-103/164/198/204/205). All nine partner branches must move
  together or a Confidential cert will list but 404 on its detail or PDF
  ([[feedback_scoping_check_adjacent_endpoints]]).
- **Public-site regression check is the safety net:** after the change, confirm all 37 Confidential
  certificates still return 404 anonymously on `/Certificate/CertificateSingleView/{number}`. That is
  the assertion that proves the embargo survived.

**Effort:** ≈ 0.5 day. The code change is small and mechanical; the bulk is re-baselining the
security suite and re-verifying isolation with the wider set.

---

## 7. Open questions — both raised by the client's own wording, neither answered

**Q1 — Should a company see its own Confidential certificates when signed in?**
*"The Confidential piece is just for the public DB side"* implies yes. But that is a third surface
beyond "PI export and API" (`CertificatesController.cs` 212, 1128, 1544 and the external tile API), so
we are not assuming it. **For the client.**

**Q2 — Should Confidential count as "Active" on the internal dashboard?**
The client says *"when they are active"* — treating Confidential as active. **DEF-223 (Shawn,
2026-05-26) deliberately narrowed `DashboardStatuses` to exclude it** so that "Active" meant the same
thing internally and publicly. This CR's rationale reverses that premise. Reverting DEF-223 touches
every dashboard tile and its exports and is **not** in this CR's scope.
**This one is Shawn's call, not the client's** — it was her decision in May.

---

## 8. Notes for Shawn (not client-facing)

- **Don't bundle with the ASIN master questions** (`2026-09-01-client-email-asin-master-questions.md`).
  Separate decision, separate reply.
- **Bill it.** The client opened with *"This is not accurate"* — they believe it is a defect. The
  CR-035 record is the evidence that we raised it first and were told the opposite. Framing the reply
  as "we fixed a bug" gives that away and sets the precedent that scope reversals are warranty work.
- **`23-0758` has now been reported missing twice** (see §4.3). Consider whether the client's copy of
  the QC log actually carried our 9/2 expiry explanation, or whether it was closed before they read it
  — [[feedback_qc_log_open_status_unreliable]].
- **Don't build before the go.** Two open questions in §7 change the surface count; Q1 in particular
  could add three more call sites.
