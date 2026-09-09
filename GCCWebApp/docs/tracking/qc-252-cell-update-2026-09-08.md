# QC #252 (row 261) — reply to the client's answers, 2026-09-08

**Workbook:** `Database QC Log_9.3.26_sw_updates.xlsx` · sheet `GCC DB QC Log2` · **row 261**

| Cell | Set it to |
|---|---|
| **Column J** | **append** the block below under the existing text — keep their green answers visible |
| **Column C** (`Status`) | `Resolved` — it still reads *"Change Request, Please Confirm Approach"*; the approach was confirmed 9/3 |
| **Column Q** | leave `Open` until they re-test |
| **Column O** | leave `See Responses` — theirs to fill |

The client answered eight points in green. The block replies to each, in their order.

---

## Column J — paste as one append (line breaks = Alt+Enter)

```
9/8: BUILT AND DEPLOYED - live on gccwebapp.com now and verified after the deploy. Taking your answers in order:

1. "(Yes when they have PI data)" - correct, and that is how it behaves. A certificate only contributes rows if it actually carries product identifiers. Of the 41 in-window Confidential certificates, 13 have identifiers and now appear; the other 28 have none and add nothing.

2. "Yes shared with partner so long as it's tagged for that partner and in active window/status" - built exactly that way. Both conditions are required, neither on its own is enough. Verified live: the Amazon account now sees all 13; another company's account sees none of them.

3. "Yes these changes are what we need implemented" - done and deployed.

4. "Correct, if now expired should not be included in PI export or API" - agreed, and 23-0758 is still absent after the deploy. It is Expired (8/31/26), not Confidential, so this change does not bring it back. If First Quality has renewed it, it needs a renewal certificate or corrected dates.

5. "There will be some marked confidential that don't go to Amazon (like ASR)... just need to make sure the ones tagged for a partner that are confidential still get sent to them and are in PI export" - that is precisely the behaviour now. Confidential + tagged to a partner + in window = goes to that partner and is in the PI export. Confidential with no partner tag reaches nobody and is untouched. Today that is 19 Amazon-tagged and 22 untagged.

6. "Correct" (the untagged ones add nothing) - still true. They hold no product identifiers, so they contribute no export rows regardless of status.

7. Q1 "Yes" - built. A company signed in to its own account now sees its own Confidential certificates. The public database still does not show them - we tested that specifically after the deploy: all 13 return "not found" to an anonymous visitor, while a normal public certificate loads correctly in the same run.

8. Q2 "Yes as long as they are in the eff-exp period window" - built with that window condition. One visible consequence: the internal "active certificates" report now returns 1,061 instead of 1,019. The difference is exactly the 42 Confidential certificates inside their window. No data changed; the report is counting what you asked it to count.

TWO FIGURES IN OUR NOTES ABOVE HAVE MOVED - they were measured 9/2 and two loads have landed since:
- "110 product identifiers added" is now 420, and "11 of your 12 examples" is now 13. The additions are 26-0473 and 26-0474 (Kenvue Brands), plus five identifiers added to 24-5714. Your original list of 12 is still correct - nothing dropped off it.
- "17 certificates become visible to Amazon" is now 19. In-window Confidential certificates: 37 on 9/2, 41 now. PI export: 16,081 rows then, 18,935 now, of which exactly 420 come from this change.

ONE THING WE FIXED THAT YOU HAD NOT SEEN: the product identifiers on those certificates were each marked "Inactive", because that status is copied from the parent certificate when it is saved rather than recalculated. They would have shipped inside a file titled "Active Product Identifiers" while the Product Identifiers screen showed them as Inactive. We corrected the 420 stored values in this release.

FOR AMAZON: they now begin receiving 19 Confidential certificates they have not seen before. Same fields, more records - nothing changes on their side, but their team may want notice.
```

---

## Internal notes (do NOT paste)

- **Backing record:** CR-043 → *CR-043 as built (2026-09-08)* in `docs/tracking/change-request-register.md`.
  `docs/tracking/CR-043-confidential-in-pi-export-and-api.md` §4 still holds the 9/2 figures — read it
  as a point-in-time measurement, not current state.
- **Every figure verified live 2026-09-08:** 42 Confidential total / 41 in-window / 13 with identifiers
  / 420 rows / 19 Amazon-tagged / 22 untagged / PI export 18,935 vs 18,515 without the change
  (difference exactly 420) / active-certificates 1,061 = 1,019 + 42.
- **Deliberately not offered here:** a way to reverse Q2. An earlier draft did; it invites a change
  they have not asked for. If the 1,061 count bothers them they will say so, and it is reversible.
- **Not mentioned, on purpose:** DEF-412 shipped in the same bundle but is not on this QC row.
