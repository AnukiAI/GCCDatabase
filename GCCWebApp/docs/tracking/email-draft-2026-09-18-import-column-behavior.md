# Email draft — 2026-09-18 — CSV edit-import: how "leave a column out" vs "leave a cell blank" behaves

**Status:** DRAFT — not sent. For Shawn's review.
**Purpose:** Answer the client's question about whether to delete unchanged columns, and give them a
chance to tell us the behaviour should be different before we lock the help text.

**Two decisions Shawn needs to make before this goes out:**

1. **Does the tag paragraph stay in?** It discloses DEF-437, which is not fixed and not shipped. I
   have left it in because it directly changes the instruction we are giving them — telling an
   analyst "just delete the columns you aren't changing" without it would walk them into the data
   loss. Cut it if you would rather fix first and tell them after, but then the rest of the email
   should wait too.
2. **Is the blank-clears-the-field rule what they actually want?** That is the real question in the
   email. It is currently a design decision we made, not one they asked for.

---

## Subject

Certificate import — what happens to columns you don't change (and one question for you)

## Body

Hi [name],

Following your question about the import help text — yes, your reading is right, and it's worth
being precise about it because the two options behave very differently.

**The rule is: leave the column out entirely.**

- **A column that isn't in your file at all** is left alone. The certificate keeps whatever it
  already had.
- **A column that *is* in your file but has an empty cell** is treated as an instruction to **clear
  that field**. The existing value is erased.

So for an edit, the safest file is a narrow one: only the columns you actually want to change.
Delete the rest of the columns rather than blanking out their cells.

**With one important exception — four columns must always be present**, even on an edit where you
aren't changing them:

| Always include | Why |
|---|---|
| **Certificate #** | identifies which certificate to edit |
| **Company** | required on every row |
| **Product Name** | required on every row |
| **Certification Type** | required on every row |

Leave any of those three out and the row is rejected with "… is required" rather than edited. So the
smallest possible edit file is those four columns plus whatever you're changing — for example, to
correct a Location: `Certificate #, Company, Product Name, Certification Type, Location`.

Two cases where a blank cell does something slightly more than clearing the field, worth knowing:

- **Status** — a blank Status doesn't clear the status, it sets the certificate back to **Draft**.
- **Partner** — a blank Partner removes the partner association.

We'll make the help text on the import screen say this explicitly, rather than leaving it implied.

**One thing we found while confirming this, which I'd rather tell you now.**

Tags are currently an exception to the rule above. At the moment, an edit-import will clear the tags
off a certificate **even if your file has no tag column at all** — which is not how the other fields
behave and is not what we told you it would do. It affects both external and internal tags. We've
raised it, and it will be fixed before this goes live; in the meantime, if you're editing
certificates by import, check the preview screen's Tags row before applying, and avoid import for
certificates whose tags matter until we've shipped the fix.

**And the question for you.**

The "blank cell clears the field" behaviour was our design decision, not something you specified. It
makes deliberate clearing possible — an analyst can empty out a Location or a Certification Results
value without editing the certificate by hand — but it also means a stray empty cell in an otherwise
good file will quietly erase data.

If you'd rather we changed it, the realistic options are:

1. **Leave it as is.** Blank clears. Most flexible, least forgiving.
2. **Blank never clears.** An empty cell is ignored exactly like a missing column, and clearing a
   field is only possible from the certificate's edit screen. Safest, but removes bulk-clearing.
3. **Require an explicit marker to clear** — a blank cell is ignored, and something like `[CLEAR]`
   in the cell means "empty this field". Keeps both behaviours and makes the destructive one
   deliberate.

Option 1 costs nothing since it's what's built. Options 2 and 3 are small pieces of work. My
recommendation is 3 if your analysts will regularly need to clear fields in bulk, and 2 if they
won't — but this is your call, since you know how the team will actually use it.

Happy to walk through it on a call if that's easier.

[sign-off]

---

## Notes for Shawn (not for sending)

- Option 3 is the one I'd put money on them picking, and it is the most work of the three — if they
  pick it, it needs a CR and an estimate before anything is built, not absorbed.
- The email deliberately does **not** list every field's blank behaviour. The full matrix is in
  DEF-437's entry in the defect register if they ask. Dates, Company, Product, Product Type and
  Pathway currently ignore a blank cell rather than clearing — which is inconsistent with the rule
  as stated, and would get tidied up under options 2 or 3 anyway.
- If they pick option 1 (leave as is), the inconsistency in the bullet above should still be raised
  as its own defect, because the help text will then be stating a rule the code doesn't follow for
  five fields.
