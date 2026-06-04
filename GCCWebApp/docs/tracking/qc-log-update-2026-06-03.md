# QC Log update — 2026-06-03 (paste-ready)

Status + response text for the items worked this session. Client-facing language; paste into the QC log status + notes columns.

---

## QC #103 — Product Management: remove Status from the popup and the table
**Status:** Fixed — verified on test environment, pending next deploy

**Response:**
You're right — Status doesn't belong on Product Management, and it's now removed. Specifically:
- The **Status column** is gone from the products table.
- The **Status field** is gone from the Add New Product / Edit Product popup.
- The **Status filter** above the table (which only existed to filter that column) has been removed too, so the page is clean and consistent.

The earlier rounds on this item had made the popup, template, and table *match each other* but kept Status on all three — this update takes Status off the page entirely, which is what you'd been asking for.

**One thing worth confirming on your side:** Status was the only place to mark a product "Inactive." Going forward, **retiring a product is done with the Delete (trash) button on its row** — that removes it from the active list and from the certificate form's product picker, without affecting the certificates already linked to it. You confirmed Delete is the intended way to retire a product, so no separate Active/Inactive control is needed.

Verified end-to-end on the test environment (no Status column, no Status filter, no Status field in the popup, all other fields intact). Ships with the next deploy.

---

## QC #106 / CR-020 — "User Company" (employer) field, separate from "Company Access"
**Status:** Built — verified on test environment, pending next deploy

**Response:**
Per your direction that there needs to be **both** fields, user records now carry two distinct things:
- **User Company** — the user's own company/employer (new). It's a dropdown of your existing companies, so it stays consistent with the company list.
- **Company Access** — which companies' data that user is allowed to see (unchanged).

Both now appear in the Add/Edit User popup, the user import template, and the export. The user table now displays **User Company** (the employer). This also settles the back-and-forth on the "User Company vs Company Access" naming — they're now two clearly separate fields rather than one label doing double duty.

This was handled as a change request (**CR-020**) since it adds a new field; built and verified on the test environment, ships with the next deploy.

**One thing to note for bulk import:** the **"User Company"** column in the import file now means the user's employer. If you have older import spreadsheets that used a "User Company" column to set *access*, those values should move to the **"Company Access"** column. The current template already shows both columns correctly.

---

## QC #109 — User CSV import (errors, Company column)
**Status:** Fixed — verified end-to-end on test environment, ready for UAT

**Response:**
Confirmed working end-to-end with a live import test:
- **Import errors are now visible.** When a row can't be imported, the on-screen message names the row and the reason (e.g. *"Import partial: 1 succeeded, 1 failed. Row 2 (…): Partner role requires the Partner column"*) instead of just saying "X errors" with the detail buried in the browser console.
- **The Company column is honored.** Company values in the CSV are matched to your companies by name (or ID), including multiple companies separated by semicolons, and saved as that user's Company Access.
- The underlying save error that could cause user create/update to fail intermittently is resolved.

We also confirmed this still works correctly alongside the new **User Company** field (CR-020): the import keeps **Company Access** (what the user can see) and **User Company** (the user's employer) as two separate columns — they don't get crossed.

---

## QC #185 / #77 — Login view inconsistencies (nav box behind search bar in Analyst view)
**Status:** Verified live on test site (gccwebapp.com) — resolved by the earlier consistency fix (DEF-227)

**Response:**
Confirmed resolved across **all four role types** on the test site. The gray separator bar between the top navigation menu and the page content now renders identically for Admin, Analyst, External, and Public:
- The **32px gray spacer** is present on every login (previously it was only landing on Admin, which is why the Analyst nav appeared to sit behind/overlap the search bar).
- **No overlap** between the navigation menu and the content/search bar on any role — verified by measurement, not just by eye.
- Admin still shows **breadcrumb text inside** that spacer; the other roles render the same spacer with no text. Same layout everywhere, just different content inside.

This was the exact concern you raised — a fix that had landed only in the Admin instance now applies to every instance. The earlier root cause was a role-restriction on the outer navigation container that hid the whole spacer for non-admins; that's been removed so the spacer always renders and only its *content* is admin-only.

Verified end-to-end on **gccwebapp.com** as the logged-in Admin, Analyst, and External user, plus the public view (screenshots on file).

---

## QC #157 — Cert List View column sorting (Location, ASIN, Cert Subcategories, Company URL)
**Status:** Fixed — **verified live on gccwebapp.com (2026-06-03)**

**Response:**
We dug into the columns you flagged on 5/29 (Location, ASIN, Cert Subcategories, Company URL). The sort itself was actually working correctly — the problem was *what showed up first*. When you sorted a column where many certificates are blank (a lot of certs have no Location or Company URL), the blanks were landing at the **top** in ascending order, so the first screen looked empty and the column looked like it "wasn't sorting." The populated values were there, just pushed below the blanks.

We've changed the rule across the **entire application**: **blank values now always sort to the bottom**, whether you're sorting ascending or descending. So the populated values you actually want to see lead the list every time, in correct A–Z (or Z–A) order, and the empties collect at the end. This is applied consistently everywhere certificates are listed — the logged-in Cert List view, the partner/external view, and the public directory — so they all behave the same way.

A couple of notes:
- **Cert Subcategories** sorts by its first value alphabetically when a certificate has more than one — as you noted, the order among multiples isn't critical.
- **UPC / GTIN / EAN** use the same logic as ASIN; there just wasn't data in those columns to see it during your test.

**Now live on gccwebapp.com** (deployed 2026-06-03 and verified after deploy: the same columns that previously showed blanks at the top now lead with populated values, blanks at the end, in both directions). Note: this is a display/ordering change only — no certificate data was changed.

---

## One-line summary for the call
Five items this round, all now on gccwebapp.com — **QC #103 and QC #157 deployed and verified live (2026-06-03)**; QC #185/#77 confirmed live; QC #106/CR-020 shipped in the 6/3 deploy; QC #109 verified end-to-end (ready for UAT):
- **QC #103** — Status is fully removed from Product Management (column, popup field, and filter). Retiring a product is now handled by the row's Delete button, as discussed.
- **QC #106 / CR-020** — added a separate **User Company** (employer) field alongside **Company Access**, which closes out the naming confusion between the two.
- **QC #109** — user CSV import verified end-to-end: import errors now show on screen, and the Company column is correctly applied as Company Access (and stays separate from User Company).
- **QC #157** — cert list sorting fixed so **blank values always sort to the bottom** (both directions), across all list views — populated values lead instead of a page of blanks. **Verified live on gccwebapp.com.** Sort logic itself was already correct.
- **QC #185 / #77** — login-view spacing inconsistency (Analyst nav box behind the search bar) confirmed resolved live across all four roles; the gray spacer now renders identically everywhere.
