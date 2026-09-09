# QC #251 (row 260) — resolution summary, 2026-09-08

**Workbook:** `Database QC Log_9.3.26_sw_updates.xlsx` · sheet `GCC DB QC Log2` · **row 260**
**Page:** Cert Results Field · **Raised by:** Carly · opened 8/27/26 · Image 113

| Cell | Set it to |
|---|---|
| **Column J** | **append** the block below under the existing text |
| **Column C** (`Status`) | `Resolved` — currently `UAT Testing` |
| **Column Q** | `Complete` — the work is delivered and live |
| **Column O** | currently `Needs Revised` — leave it; the client clears it once they have reviewed |

> **Note on column Q, correcting what I said earlier on #252.** Column Q is headed *"Anuki Marks
> Complete"* — it records that **we** finished, and the client's own confirmation lives in column O.
> So Q should read `Complete` on #251, #252 and #257 now that all three are deployed. Waiting for
> their re-test before setting Q conflates the two columns, which is part of why 67 rows on this log
> read "Open" while carrying a close date.

**Tone:** resolution summary and guidance, consistent with #257 — what is true now and what to do
next, not a fault analysis.

---

## Column J — paste as one append (line breaks = Alt+Enter)

```
9/8: Resolved and live on gccwebapp.com. Both halves of this are done - the existing data and the behaviour of the field.

THE EXISTING DATA (9/2)
Every populated Certification Results value in the database is now in the explicit XX% form you used on the ASIN certificates - 803 records updated to match. Nothing is left in decimal form, so the imported records and your own corrections now read the same way.

THE FIELD ITSELF (9/8)
Certification Results is now a straightforward open text field. Whatever is typed is exactly what is stored and exactly what is published - on screen, in the CSV exports, and to Amazon. That covers your point about not always entering a percentage: "Pass", "Silver", ">50%", a plain number, or anything else will now come through unchanged.

"AND CAN'T HAVE THE UPDATE BREAK EITHER" - it did not, and we checked rather than assumed
We compared every certificate carrying a value before and after the change: 1,103 records, of which 1,072 are in XX% form and 31 hold non-numeric entries such as "Pass" and "Silver". Zero displayed values changed, zero export values changed, zero sort positions changed, and nothing sent to Amazon changed. Sequencing is why: the data was brought into XX% form first, then the field was changed, so by the time the behaviour changed there was nothing left for it to affect.

WHERE THE DECIMAL VALUES CAME FROM, AND WHAT IT MEANS FOR FUTURE SPREADSHEETS
In the source workbooks these cells are formatted as percentages, so a cell shows 23% while the underlying value Excel stores is 0.23. Loads read the underlying value, which is how the decimals arrived. Every load from here reads the displayed value instead, so a cell showing 23% will load as 23%. Nothing further is needed on your side - it is worth knowing when you are preparing a sheet.

ONE THING TO KEEP IN MIND GOING FORWARD
Because the field now publishes exactly what is entered, a value typed as 0.23 will publish as 0.23 rather than being converted to 23%. That is the intent of "whatever is inputted is the value we want". If you would like a gentle prompt when someone types a bare decimal - a reminder, not a block - say the word and we will scope it.

Ready for review whenever suits.
```

---

## Internal notes (do NOT paste)

- **Backing records:** CR-042 (data normalisation, 803 certificates, executed 2026-09-02) and CR-045
  (verbatim publication, built + deployed 2026-09-08). Both in
  `docs/tracking/change-request-register.md`.
- **Figures verified live on the target 2026-09-08**, not carried forward from dev: 1,103 populated /
  1,072 `%`-formatted / 31 non-numeric / **0 bare numerics**. The "zero changed" claim is measured
  against a pre-change snapshot of all 6,012 certificates (`CertResults_bak_CR045_20260908`), not
  inferred from the absence of bare decimals.
- **The "where the decimals came from" paragraph is deliberately neutral.** The client's note says
  *"all the imports Anuki added are in decimal form"*, which reads as an import fault on our side.
  It was not — the importer stored the true cell value, and the source cells carry Excel's `0%`
  number format, so the cell displays `23%` while holding `0.23`. Left uncorrected, they are left
  believing there is a live import bug and may re-report it. Stated as *how the values got there*
  rather than as a correction, which answers the concern without turning the row into an argument.
- **Not offered:** reverting the 803 rows to bare decimals. Their own source data displays `23%` and
  their ASIN corrections are in `XX%`, so the normalised values already match what they authored.
  Flagged to Shawn 2026-09-04; still the right call.
- **Not built:** the optional soft warning on import preview / Create / Edit for a bare numeric ≤ 1.
  Offered in the block above as a scoped extra rather than assumed.
