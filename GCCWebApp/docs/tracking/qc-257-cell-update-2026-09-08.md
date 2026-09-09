# QC #257 (row 266) — reply to the client, 2026-09-08

**Workbook:** `Database QC Log_9.3.26_sw_updates.xlsx` · sheet `GCC DB QC Log2` · **row 266**
**Page:** MS Auth Setup · **Raised by:** Carly · opened 8/28/26

> ⚠️ **This is #257, not #259.** #259 is the separate "Search Loading Time" row (row 268). Their text
> is easy to mix up because both were answered on 9/3.

| Cell | Set it to |
|---|---|
| **Column J** | **append** the block below under the existing text |
| **Column O** | currently `Need Further Clarity/Fix from Anuki` — leave it; the client clears it |
| **Column Q** | `Complete` — both halves are fixed and live |

Their ask was *"Once you confirm both have been resolved I can have someone review."* The block
confirms both and gives them the steps to review — **written as confirmation and instructions, not as
a post-mortem.** Root cause, blast radius and the internal defect history stay in the notes below and
in the defect register; none of it needs to reach the client to answer what they asked.

---

## Column J — paste as one append (line breaks = Alt+Enter)

```
9/8: Both items are resolved and live on gccwebapp.com. Ready for your reviewer - here is how to check each one.

1. RECOVERY CODES - COPY ALL AND DOWNLOAD AS TEXT
Both now capture the complete code, including everything after the dash.

To review it without undoing your setup: you do not need to disable and re-enrol. Go to your account's Two-Factor settings and choose "Regenerate Recovery Codes". That issues a fresh set and takes you straight to the recovery codes screen, where Copy All and Download as Text can both be checked against what is displayed. Note that regenerating replaces your previous codes, so save the new set and discard the old one.

Worth doing for anyone who is already set up: if a set of codes was saved from that screen before 8/31, regenerate to get a current copy on file.

2. "TRUST THIS DEVICE FOR 30 DAYS"
This now records the device for 30 days, and we have confirmed a device being remembered with the correct expiry date on the live site.

To review it:
- Sign in and tick the box during the two-factor step.
- It applies from the NEXT sign-in, not the one where you tick it - so sign out and sign back in to see it take effect.
- From then on that browser should not ask for a code again for 30 days.
- It is remembered per browser and per device, so a different browser, a different machine, or clearing cookies will ask for a code again. That is intended.

Because this is the first release where the setting is stored, everyone will be asked for a code once more on their next sign-in, and then not again for 30 days on that browser once they tick the box.

Happy to walk your reviewer through either one if that is easier.
```

---

## Internal notes (do NOT paste)

- **Tone is deliberate.** Shawn, 2026-09-09: *"update this so it doesn't sound like it was broke,
  just retesting and instructions."* The earlier draft led with what had been wrong and how long it
  had been wrong. Nothing in it was inaccurate, but the client asked for confirmation and a way to
  re-test, not a fault analysis — and this row is one they will read back later. The technical
  history is preserved in DEF-384 / DEF-410 and the register, which is where it belongs.
- **One sentence was kept on purpose**, softened: *"if a set of codes was saved from that screen
  before 8/31, regenerate."* Anyone holding codes saved before that date has an incomplete copy, and
  they will only discover it when locked out. That is a real user-safety item, so it stays — phrased
  as good practice rather than as a defect notice.
- **The "asked once more" line also stays.** Every 2FA user will be challenged on their next sign-in
  regardless of what they ticked previously. If it is not flagged, that reads as the fix not working
  and comes straight back as a new QC row.
- **Backing records:** DEF-384 (recovery codes, `Views/Account/ShowRecoveryCodes.cshtml`, shipped in
  `GCCWebApp-deploy-20260830-2245.zip`, confirmed live 8/31) and DEF-410 (`TrustDevice` checkbox had
  no `value`, so it posted `"on"` and could not bind to a bool — shipped in
  `GCCWebApp-deploy-20260908-1607.zip`).
- **The live evidence for point 2 was checked, not inferred:** `TrustedDevices` holds a row created
  `2026-09-08 20:46:26` expiring `2026-10-08` — after the deploy, with exactly the 30-day window.
  The table was empty beforehand.
- **The claim is deliberately scoped.** What is proven on the live site is that ticking the box now
  *records* a trusted device with the right expiry. That the next sign-in skips the challenge was
  proven on localhost, where TOTP codes could be computed directly. Their reviewer's next sign-in is
  the first real confirmation, which is why the block says "should not ask for a code again" rather
  than asserting it as tested.
- **`RegenerateRecoveryCodes` verified to exist** — `AccountController.cs:1862`, POST, button on
  `Views/Account/TwoFactorPreferences.cshtml:545`, redirects to `ShowRecoveryCodes`. The page itself
  warns that regenerating invalidates previous codes.
- **4 users have 2FA enabled** on the live site.
