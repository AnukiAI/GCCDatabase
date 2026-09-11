# QC Log update — QC #261 + #262, Edit in a new tab / List as the default (in-cell, paste-ready)

**Source workbook:** `Database QC Log_9.9.26_sw_updates.xlsx`, sheet **`GCC DB QC Log2`**, header row 5.

| Item | ID | Row | Page | Image | Raised | Col O now |
|------|----|-----|------|-------|--------|-----------|
| Edit opens a new tab | **261** | **270** | `Edit - Open New Tab` | 118 | Carly, 2026-09-09 | `New` |
| List view as default | **262** | **271** | `Default - List View` | 119 | Carly, 2026-09-09 | `New` |

| Col | Header | Who fills it |
|-----|--------|--------------|
| **J** | `Sprint 3 6/23 Notes` | **Anuki** — the cells below go here |
| **O** | `Date Closed - GCC Confirmed Fixed` | **Client** — do not fill this in for them |
| **Q** | `Current Status - Anuki Marks Complete` | **Anuki** |

**Backing records:** CR-048 (#261) · CR-049 (#262) — `docs/tracking/change-request-register.md`.
**Bundle:** `GCCWebApp-deploy-20260910-2128.zip`, built and verified 2026-09-10.
**Status: NOT yet uploaded.** Do not mark either item live until the deploy lands — see the runbook
`docs/tracking/2026-09-10-deploy-runbook-CR048-CR049.md`. Once it is live, change the opening of each
Column J note from *"ready for the next deploy"* to *"live on gccwebapp.com (deployed M/D)"*.

---

# QC #261 — Edit opens a new tab

## Column Q

```
Complete - built and verified, pending deploy
```

## Column J — paste this

```
9/10: Done - built and verified, ready for the next deploy to the test site. And yes, it was an easy, low-risk change.

Edit now opens in a new tab, exactly the way Preview already did. Your results list stays put behind it, so working through a batch of certificates no longer costs you your search each time.

We applied it everywhere Edit appears, not only the screen in Image 118 - both certificate list views and both certificate detail pages - so it behaves the same wherever you start from.

One detail worth knowing: when you save, the new tab lands on that certificate's detail page rather than returning to the list. So you can simply close the tab and carry straight on from where you were. The row in the list behind you will still show its previous values until you refresh - Preview already worked this way, so it should feel familiar.

We'll confirm here once it is live on gccwebapp.com.
```

---

# QC #262 — List view as the default

## Column Q

```
Complete - built and verified, pending deploy
```

## Column J — paste this

```
9/10: Done - built and verified, ready for the next deploy. Also a low-risk change.

Signed-in users now open straight into the List view, so the team no longer has to toggle on every page load.

We added one thing beyond what was asked, and it is worth a moment: if someone does switch to Tile, that choice is now remembered for them next time. Without that, the new default would simply hand the same annoyance to anyone who prefers tiles. Everyone still starts on List; only a deliberate switch is remembered.

Your note said "all logged in users", so we applied this to the customer (external) views as well as the internal team views. If you would rather external customers keep opening on tiles, say so and we will limit it to the team - it is a small change either way.

The public certificate database is unchanged: it still opens as tiles.

One limitation to be aware of: the preference is stored in the browser, so signing in on a different computer starts on List again.

We'll confirm here once it is live on gccwebapp.com.
```

---

## Notes for us, not for the log

**Both items were phrased as questions** — *"is it an easy fix (with low risk of breaking things)"* —
so both Column J notes answer that directly rather than only reporting the change. The client is
asking us to price risk, not just to do the work.

**#262 carries an open question back to them.** Shawn chose the literal reading of *"all logged in
users"* (internal **and** external) over the internal-only recommendation. The note surfaces that
choice plainly and offers to reverse it, rather than letting the client discover a customer-facing
view changed without being asked. If they do want it reversed, it is roughly half an hour.

**#261's stale-row caveat is stated, not buried.** Preview already behaves this way, so it is a
known-and-accepted consequence rather than a new defect — but it is the kind of thing that comes
back as *"the list didn't update"* if nobody mentions it first.
