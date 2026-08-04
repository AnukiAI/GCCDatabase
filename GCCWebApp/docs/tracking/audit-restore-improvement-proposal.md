# Audit History → Record Restore: current state and improvement options

**Date:** 2026-08-04 · **Author:** Anuki · **Status:** Proposal — nothing built
**Trigger:** Shawn, 2026-08-04 — *"how do we improve the audit history. There may be an instance
when the client needs to restore a record."*
**Related:** [[DEF-337]] · [[DEF-288]] (certificate soft-delete, parked) · CR-023 (import undo,
built — already restores from audit `OldValue`)

---

## 1. What already works — do not rebuild this

The audit foundation is **better than it first appears**, and one early reading of it was wrong.

`AuditService.LogEntityChangeAsync` writes a full-entity JSON snapshot to `OldValue` on DELETE.
It serializes with `DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull`
(`Services/Audit/AuditService.cs:491`), so **NULL fields are omitted — an absent key means NULL,
not "not captured."** A snapshot showing 8 keys for a 16-column table is complete; the other 8
were null.

This was verified in practice on 2026-08-04: products 100/101 were deleted and restored from their
snapshots, and the restore was **faithful**. Scalar-field capture on delete is sound.

CR-023 already proves the pattern end to end — `UndoImportBatchAsync` reconstructs certificates
from audit `OldValue`, with 15/15 validation. **A restore capability is not a greenfield build.
It is an extension of something that already works.**

---

## 2. The four real gaps

### Gap 1 — `Certificate` has no delete audit at all *(most serious)*

`Services/CertificateService.cs` never references `_auditService`. Deleting a certificate leaves
**no snapshot**, and takes its `CertificateProductIds`, `CertificatePartners` and status history
with it. This is the system's core entity and the one a client is most likely to ask to recover.

### Gap 2 — cascade children are destroyed with no record

`ProductIdentifiers.ProductId` and `ProductTag.ProductsId` are `ON DELETE CASCADE`. Their rows
disappear at the SQL layer, unaudited. The parent's snapshot cannot help either: navigation
**collections** are deliberately stripped from the blob (`AuditService.cs:521-525`). Nothing,
anywhere, records what the children were.

### Gap 3 — delete-audit coverage is inconsistent

| Entity | Delete audited? | Delete style |
|---|---|---|
| User | Yes (`AdminService.cs:659`) | **Soft** — genuinely recoverable today |
| Product | Yes (`ProductController.cs:532`) | Hard |
| Company | Yes (`CompanyService.cs:174`) | Hard |
| Certificate (import undo only) | Yes (`CertificateImportService.cs:1004`) | Hard |
| **Certificate (normal delete)** | **No** | Hard |
| **ProductCategories / ProductTypes** | **No** | Hard |
| **CertificationTypes / Tags** | **No** | Hard |

Practical consequence, hit on 2026-08-04: when we needed to know whether a *category* had been
deleted, the audit log could not answer. Only row counts could.

### Gap 4 — `UpdatedAt` is never captured

Excluded via `AuditSkipFields` (`AuditService.cs:414`). A restored row cannot reproduce its
original modification time. Minor, but it means no restore is ever bit-identical.

---

## 3. Options

### Option 1 — Close the coverage gaps (no new UI) · **≈1–1.5 days · RECOMMENDED FIRST**

Make every delete leave a complete, recoverable record. No user-facing change.

- Add DELETE audit to `CertificateService` and to the category/type/tag delete paths.
- Capture cascade children before the parent is removed — either snapshot them into the parent's
  audit `Metadata`, or write a child audit row each.
- Add a **coverage test** asserting every deletable entity writes a DELETE snapshot, so a new
  entity cannot silently join the gap list.

**Why first:** every later option is worthless without it. You cannot restore what was never
recorded, and today's deletes are *still* losing data while we deliberate. This is also the only
option that improves the situation retroactively-in-spirit — it stops the bleeding now.

### Option 2 — Soft delete where the column already exists · **≈1–2 days**

`Products.IsDeleted` exists and is unused; `User` already soft-deletes correctly. Extend the
existing pattern rather than inventing one. Nothing cascades, so Gap 2 largely dissolves.

**Caution:** every read path needs a global query filter, and missing one leaks deleted rows into
lists, exports and the Partner API. That is the risk that parked DEF-288. Should be decided
*together with* DEF-288, not separately — same decision, several tables.

### Option 3 — Admin "Restore deleted record" UI · **≈2–3 days, after Options 1–2**

A Deleted Items view (filter `AuditHistory` for `Action='DELETE'`), showing what was deleted, by
whom, when — with a Restore action rebuilding the row from `OldValue`, behind
`AdminCore.showConfirmation()`. Reuses CR-023's restore logic rather than duplicating it.

**This is the thing that actually answers "the client needs to restore a record"** — but it is
only safe once Options 1–2 guarantee the snapshot is there and complete. Built first, it would
offer a Restore button that silently fails for certificates and categories.

### Option 4 — Do nothing further

Defensible for a pre-launch system where deletes are rare and mostly test data. **The bet:** no
client ever hard-deletes a real certificate. If they do, it is unrecoverable and there is no trace
of what it was.

---

## 4. Recommendation

**Option 1 now; decide 2 and 3 later.**

Option 1 is the cheapest, carries the least regression risk (additive — no read path changes), and
is a prerequisite for everything else. It converts a silent, permanent data loss into a recorded
one, which is the difference between "we can recover this" and "we cannot know what was there."

Option 3 is what Shawn's question is really asking for, and it is genuinely reachable — but only
worth building on top of Option 1. Sequencing it first would produce a restore feature that fails
on the entity clients care most about.

**Explicitly not recommended:** building the Restore UI alone. It would look like a safety net
while leaving certificates and categories entirely uncovered.

---

## 5. Scope note

Nothing here changes `/api/v1/certificates*` ([[feedback_amazon_api_immutable]]). Option 2 is the
only one that touches read paths, and that is precisely why it should ride with DEF-288's decision
rather than be slipped in alongside audit work.
