# Shipped — August 2026

Snapshot of delivery for August 2026. Stakeholder-readable log.

Period: 2026-08-01 → ongoing

## August 2026 — at-a-glance

| | Count |
|---|---|
| **Deployed 2026-08-31** (single file, no database change and no application restart) | **DEF-387 — "Error saving user" now says what actually went wrong.** Reported by the client: creating a new user failed with **"Error saving user. Please try again."** and nothing else — no reason, nothing to act on, and no way to tell a genuine fault apart from something the person could simply have fixed themselves. **The message was not describing the failure; it was what the page said whenever it could not understand the server's answer.** Every refusal the server sends back for this screen carries a plain-English reason with it, so reaching that message at all proved the reply had not come from the part of the system that saves users. It had come from the sign-in page. When the security token that protects the form has gone stale — which happens quite normally if the page has been left open for a while, or if you have signed in again in another tab — the site is designed to answer with a short, specific refusal. On this one screen it was instead answering with the **whole sign-in page**, which the form could make no sense of, so it discarded the reply and showed its fallback wording. The screen now identifies itself correctly when it asks the server to save, which is what the rest of the site already did, and it reads the reply properly rather than throwing it away. **What you will see from now on:** if the security token has expired, it says so and tells you to reload the page; if the sign-in session has expired, it says that instead; and if something genuinely went wrong on the server, it reports that plainly so it can be looked up. **An important limitation, stated honestly:** this does not by itself prove *which* of those caused the original report — the reason was discarded at the time and cannot be recovered. The expired-token case is the most likely on the evidence. **The next attempt will name it on screen rather than leaving it to be guessed at**, which was the point of the change. **No data was at risk and nothing was deleted or altered.** User creation itself was never broken — the site was correctly refusing a request it could not accept, and only the explanation was missing. **This was predicted and should not have been left.** When the matching fault on the *edit* screen was fixed on 20 August, we recorded in writing that this same fallback was a second way for a real reason to be swallowed, and chose not to change it because it had not yet happened. It happened eleven days later and cost a support round-trip. **Confirmed live rather than assumed:** the updated file on the server was checked byte-for-byte against the one we built, and every other file on the site was confirmed unchanged in the same pass. |
| **Deployed 2026-08-31** (**bundle 20260830-2245**, which superseded 20260830-2132; includes a database change, applied to the test database by hand on 2026-08-30 ahead of the upload) | **Three fixes from the client's 30 August notes.** **(1) Certification results recorded as `1` were being shown as "1%" when they mean 100%.** The results field is free text, so the site has to work out whether a number is a proportion (`0.86`) or an already-whole percentage (`100`). The rule was "below 1 is a proportion", which is right for every value in the data except exactly `1` — and `1` is now the most common entry in that field. **This was never right; it was simply never visible until the client's own data was loaded**, because the earlier examples used to check the rule were `0.17`, `100`, `17%` and `Pass`. We confirmed the meaning rather than assuming it, by listing every distinct value on record: the scale runs from `0.01` (which is how 1% is already written) through `0.99` and then `1`, with **nothing at all between 1 and 100**. So `1` can only mean 100%, and **86 certificates were reporting the best possible result as the worst**. Both the displayed percentage and the hidden value the "Cert Results" column sorts by were corrected together — fixing only the display would have left those certificates showing "100%" while sorting to the bottom. **Please note: this changes what Amazon sees.** Cert Results is included in the partner feed at the client's earlier request, and **11 of the affected certificates reach Amazon**, who use this figure against a pass threshold. The format of the feed is unchanged, so there is nothing for Amazon to re-integrate — but the values for those 11 will change from 1% to 100%, which is a correction in their favour and worth telling them before it goes live. **(2) The 2FA recovery-code "Copy All" and "Download as Text" buttons were only giving out half of each code.** The codes appeared correctly on screen — `A1B2-C3D4` — but anything copied or downloaded came out as `A1B2-`. The screen and the copy button were built with two commands that look identical but count differently, so one produced the whole code and the other silently stopped halfway. These codes are the way back into an account when the authenticator is unavailable, so **anyone who saved the downloaded file as their backup has an unusable one**. The stored codes were always correct and nothing needs reissuing — but everyone who has already saved theirs should copy them again after this update. **(3) "Trust this device for 30 days" now actually works.** The tick box was connected, but to the wrong thing: it kept you signed in for longer without ever removing the two-factor step, so a fresh sign-in asked for a code every time. Ticking it now genuinely skips the code on that browser for 30 days. **The password is still required every single time** — only the second step is skipped, and only on the browser where the box was ticked. The trust is granted only after a code has actually been verified, is cancelled automatically if the password is changed or two-factor is switched off or reset by an administrator, and **cannot carry across to a different account signing in from the same browser** — all of which we tested rather than assumed. There is not yet a screen for viewing or individually removing trusted devices; if that is wanted, it is a small addition. |
| **Deployed 2026-08-31** (delivered inside **bundle 20260830-2245**; 20260828-1258 and 20260827-0636 were superseded and never uploaded) | **The public certificate list no longer repeats the same certificates as you scroll.** Reported by the client: scrolling down the public database appeared to restart from the beginning, showing the same certificate numbers over and over, so it was impossible to read past the first page or to check the records against their own list. It happened whether the list was sorted or not, whether signed in or not, and clearing the browser cache made no difference. **The certificates themselves were never wrong, and neither was the database.** We checked this before changing anything: the list of certificates the page requests was walked from the first record to the last, on three different sort orders, and it returned **963 certificates, all 963 different, with no repeats at all**. The fault was purely in how the page drew them on screen. Each time you reached the bottom and the next batch loaded, the page re-drew **every certificate it had loaded so far** underneath the ones already showing, instead of adding just the new ones. So the second batch repeated the first, the third repeated both, and so on — which is exactly what "it repeats the prior" describes. It also meant the page was building up thousands of duplicate entries as you scrolled, which is why it felt like it was reloading itself. This is now corrected, and the count shown at the top of the list matches what is actually on screen. **Clearing the cache could never have helped**, which is worth saying because it was reasonably assumed to be a browser problem: the page already loads the newest version of that file automatically. **A second, related fault was found and fixed at the same time.** When the list asked for records starting from a position that was not an exact multiple of the batch size, it quietly rounded *down* — re-sending records already shown and making the last few records unreachable. It was not being triggered yet, but it is the fault that would have turned the above from an annoyance into a permanent block: the page counts how many *new* records it received, so a single repeat would have moved it onto one of these positions and it could never have reached the end of the list. Both are fixed and both now have automated checks that were confirmed to fail against the old code before being trusted. **Separately, on the record counts the client is reconciling:** of the certificates in a publicly-approved status, **979** exist and **963** are visible. The **16** difference is entirely certificates whose start date has not yet arrived — the behaviour introduced in the DEF-358 work below, working as intended. **None** are hidden because of expiry. The individual certificate numbers have been listed for the client. |
| **Prepared 2026-08-27 — superseded by 20260828-1258** (code only, no database change; **bundle 20260827-0636**) | **Four fixes found by a full regression test of the site, run across every type of login.** The test covered all six account types, every page each one can reach, the spreadsheet downloads, the import, creating and editing a certificate, and page speed — 324 individual checks in total. **308 passed.** Access control was clean: 24 pages were requested by all seven kinds of caller, including a signed-out visitor, and every single one behaved correctly — no page let someone in who should not have been, and none blocked someone who should have been. Company separation was proved by pointing two different company logins at each other's data: neither could see a single one of the other's certificates. Page speed was comfortable throughout, with the full spreadsheet of all 5,702 certificates downloading in under four seconds. What the test found, and what has now been fixed: **(1) Amazon was being shown two certificates about two months before their start date.** Earlier work had stopped certificates appearing publicly before their start date, and that was correctly applied to the Amazon data feed and to the downloadable spreadsheet — but not to the certificate pages an Amazon login sees when signed in to the site. The result was that the signed-in view listed 210 certificates while the same account's spreadsheet download listed 208, the two extras being certificates that do not come into force until late October. The signed-in view now matches, and the certificate document cannot be downloaded early either. **The certifying company's own view is deliberately unchanged** — a company still sees its own upcoming certificates, which was the client's earlier direction. **(2) A partner with no certificates got an error instead of an empty spreadsheet.** Downloading the certificate list as a partner account that has no certificates attached to it returned an error message rather than a file. It now returns a normal spreadsheet containing just the column headings, which is what a spreadsheet user expects. A genuine failure is now also reported as a genuine failure rather than being disguised as a problem with the request. **(3) The import now creates certificate numbers automatically, the same way the on-screen form does.** Adding a certificate through the form and leaving the number blank has always generated the next number automatically. Importing the same information from a spreadsheet did not — it refused the whole file and said a certificate number was required, without the template ever indicating the column was mandatory. The two now behave the same way. **A number supplied in the file is still used exactly as given, which is how the main data migration keeps the client's existing certificate numbers.** While fixing this we found a second, more serious problem in the same area: because the import groups rows by certificate number, several rows left blank would have been merged into a *single* certificate rather than creating one each. That is fixed, and the import preview now shows "(auto-assign)" against any row whose number will be generated. **(4) A spreadsheet template was offered to accounts that cannot use it.** The product import template could be downloaded by company and partner logins, even though only staff can import products. It contained no data — just column headings — so nothing was disclosed, but it offered a function those accounts do not have. It is now staff-only. **Also found and recorded, not yet addressed:** duplicate certificate numbers in the migrated data (already logged separately), the Novi partner having no certificates attached to it anywhere in the data, and one internal page that returns an error but is not reachable from anywhere in the site. **One question for the client, raised by the numbers rather than by a fault:** a signed-in company currently sees only its current certificates, not its expired ones. For one company that means 30 visible out of 574 on file, because 82% of the migrated data is expired. That is how the site was built and it may well be correct — but it is a decision worth confirming rather than discovering later. **No data was at risk.** The test created its own certificates and removed them afterwards; no existing record was changed or deleted. |
| **Prepared 2026-08-17 — not yet deployed** (code only, no database change) | **DEF-358 — Certificates no longer appear on the public database before their effective date begins.** Reported by the client: the public directory was showing certificates that had not yet reached their effective date. Putting a certificate into an approved status ahead of its start date is normal practice for the team — the work is finished and everything is staged so it is ready the moment the date arrives — but the site was treating "approved" as "publish now" and listing it immediately. A certificate should only be publicly visible **while it is inside its effective-to-expiration period**, and it now is. **The same question was raised about the information passed to Amazon, and it was right.** Those certificates were also going out over the Amazon partner feed, reported as active, so a certificate could be presented as a current certification weeks before it came into force. **What we found.** Everywhere the site decides whether a certificate may be shown publicly, it was checking two things — the certificate's status, and that it had not expired. It never checked that the certificate had *started*. There was no such check anywhere in the system; the start date was recorded, and used for sorting the lists, but was never used to decide visibility. The Amazon feed was the weaker of the two: it checked only the status, with no date check at either end. **Scale.** On the test environment, 9 of the 67 publicly visible certificates were affected, 6 of them also going to Amazon. It matters more than those numbers suggest, because the data prepared for go-live carries the same pattern — of the 981 certificates that would be publicly visible, **16 have future start dates** — so without this change the behaviour would have gone live with real certificates. **What changes.** A certificate appears on the public directory, and in the Amazon feed, only from its effective date through its expiration date, inclusive of both days. Nothing needs to be done to release it: a certificate staged today for a date next month **publishes itself on the morning of that date**, with no action from the team and no status change required. Certificates that pre-date this and have no start date recorded are unaffected. For the Amazon feed, a certificate that has not started is simply **left out** until its date arrives, rather than being sent with a new "pending" label — chosen deliberately so the agreed format Amazon receives is completely unchanged. **This also covers the downloadable spreadsheet.** The certificate list can be downloaded as a CSV without signing in, and that download was assembled by a different piece of code from the one behind the web pages — so on first pass it would still have contained the not-yet-started certificates the site had just stopped showing. It now matches: the public download went from 981 rows to 965, the difference being exactly the certificates whose start date has not arrived. Worth stating plainly because it was caught by our own pre-release checks rather than by review, and had it shipped, the site and the spreadsheet would have disagreed with each other. **Deliberately unchanged, at the client's direction:** the certifying company still sees its own upcoming certificates when signed in — a company needs to know its certificate is queued and ready — and staff continue to see everything, exactly as before. Only the public directory and the partner feed are affected. **One thing to expect at deployment:** if Amazon has already taken in the certificates currently showing early, they will see those drop out of the feed and then return on each certificate's effective date. **No data was at risk and nothing was deleted or altered** — the certificates themselves are untouched; this only changes when they become visible. |
| **Deployed 2026-08-04** (code only, no database change; **bundle 20260804-2230**) | **DEF-340 — Setting up the authenticator app now tells you when a code is rejected.** When switching on two-factor sign-in, you scan a QR code with your authenticator app and type in the six-digit code it shows. If that code was wrong, expired, or mistyped, the page simply reloaded and said nothing at all — no error, no explanation, the same screen you were already looking at. There was no way to tell a rejected code apart from a page that had just refreshed, so the only reasonable conclusion was that the setup screen was broken. It was not: the code was being correctly rejected, and the explanation for why was being generated every time. It just never made it onto the screen. The message was being handed off in the wrong way for the kind of page change involved, so it was dropped in transit. It now appears on the setup page, immediately, saying exactly what went wrong — the code was not six digits, or the code was not accepted, or the setup could not be completed. **There was a second effect worth mentioning, because it may explain something odd that was seen elsewhere.** Because that message was never picked up where it was meant to be, it was not discarded either — it stayed with the session and waited. The next time the **two-factor preferences** page was opened, it would appear there instead, as a red error about an authenticator code, attached to nothing the user had just done. That could be minutes or much longer after the fact. Both effects are resolved by the same change: the message is now delivered and used on the setup page itself, so it cannot resurface later out of context. **How this was found:** not by a tester and not reported by the client — it was surfaced by our automated pre-release checks while preparing this release, in a flow that is only reached when someone enrolls in two-factor sign-in *and* enters a bad code. No data was ever at risk and sign-in itself was never affected; two-factor protection was working correctly throughout, and this was purely about what the setup screen told you while you were switching it on. |
| **Deployed 2026-08-04** (code only, no database change; **bundle 20260804-1705**, superseded by 20260804-2230) | **DEF-336 — "Download Selected" no longer stalls when you select a large batch of certificates.** Selecting a large number of certificates and using **Download Selected** could leave the browser waiting with nothing happening, and on a big selection the download would never arrive at all. Nothing was ever downloaded incorrectly and no data was at risk — the request simply took longer than the browser was willing to wait. Cause: the page was fetching the selected certificates **one at a time**, and each certificate is assembled from a dozen related pieces of information (company, product, tags, partners, identifiers and so on). For a 60-certificate selection that worked out to several hundred separate trips to the database, one after another, before the ZIP file could even begin. The certificates are now fetched **in a single pass** instead, which is what the underlying database is built to do well. The permission check that decides whether you are allowed each individual certificate is **unchanged and still runs certificate-by-certificate** — only the fetching was made faster, never the security. Verified on the test environment: the download now completes promptly and returns the correct result, and the automated security check that covers this screen — which had been unable to finish at all while the stall was happening — is running green again. **Worth flagging:** this was not reported by a tester. It was found by our own automated regression suite, and the reason it matters beyond the slow download is that the stall was **preventing a security check from completing** — the check that confirms one company's user can never pull down another company's certificate files. That protection was working correctly the whole time; what we had lost was the automatic proof of it, which is now restored. |

---

## 2026-08-18 — Certificate data migration loaded into the test environment

**What was delivered:** the client's complete certificate dataset is now live in the test site,
replacing the test records that had accumulated during build and QA.

| Loaded into the test site | |
|---|---|
| Certificates | **5,702** across **5,527** certificate numbers |
| Companies | **301** |
| Products | **3,439** |
| Product identifiers (ASINs) | **16,234** |
| Publicly visible certificates | **965** |
| Records from the source workbooks that did not load | **29** — the incomplete ones, listed for the client |

**Verified, not assumed.** Every certificate and every ASIN in the client's own workbooks was
reconciled against the database **in both directions**: nothing missing that should be there,
nothing present that should not be, and no unexplained differences. Fourteen of fifteen data fields
matched exactly; the fifteenth is nine records where `The Netherlands` is stored as `Netherlands`,
which is the system's country standardisation working as intended.

**The Amazon integration was checked specifically and is unchanged.** The partner feed was called on
the live test site and compared against the frozen April reference responses — identical structure,
no change for Amazon to re-test. All nine partner API keys, including Amazon's, are intact.

**Nothing the client asked us to keep was touched.** Certification types, product categories and
types, tags, pathways, partners, API keys, roles and permissions, and the Google Analytics settings
all survived, and were checked again after the load. The client's own administrator account and the
analyst and Amazon partner logins were preserved.

**A full backup was taken immediately beforehand** and is held separately; the load itself ran inside
a single transaction.

**Certificate documents: attached and confirmed opening on the test site.** All **5,474** certificate
PDFs are matched to their certificates with previews generated — every document verified against its
original by checksum, with no mismatches, and each certificate holding its own distinct document.
The files were uploaded to the server and then checked end to end through the application itself:
documents open as genuine PDFs, previews render, and where several certificates share a number each
one correctly serves its *own* document rather than a sibling's.

One certificate's preview image failed to render (a known issue with that particular file); it
displays a generic preview instead, and we confirmed it shows the generic image rather than another
certificate's.

**Two decisions still outstanding with the client:** written confirmation of how duplicate
certificate numbers are handled, and a direction on **four certificates carrying 1,960 ASINs that
have no partner set** — 12% of everything loaded. Because no partner is set on the certificate,
**Amazon does not receive those certificates or their ASINs at all.** The question is whether that
is correct, or whether they were meant to reach Amazon.

> **Correction (2026-08-21).** An earlier version of the line above said those ASINs were *"visible
> to every partner that calls the API"*. **That was the wrong way round.** Partner scoping applies
> two filters, and the first one settles it: the API scopes to certificates the caller's partner is
> associated with *before* it filters product identifiers. All four certificates have no partner
> association at all, so they reach **no** partner — measured as `untagged_reachable_by_a_partner`
> = **0 of 1,960**. The risk is not over-exposure but the opposite: silent omission. Corrected in
> the change-request register and the go-live runbook at the time; this client-facing log was
> missed until 2026-08-21.


---

## 2026-08-20 — Email, two-factor sign-in and password reset

**What was delivered:** the site can now send email, so password reset and two-factor sign-in work
for the first time on the test environment. Four defects, deployed across bundles **20260819-1436**,
**20260820-1640** and **20260820-1813**. Code and configuration only — no database change.

**Why this ran to four fixes rather than one.** Turning email on was expected to be a configuration
change. It was not: the moment mail started flowing, it exposed three further faults in two-factor
sign-in that had never been reachable before, because nobody had been able to complete a two-factor
sign-in to find them. Each was found by the client testing the working feature.

### DEF-263 — Email now sends: password reset and two-factor codes arrive

Outbound email had never worked on the test site. Password-reset links and two-factor codes were
being generated correctly and then **silently discarded** — the system was configured to log
messages instead of sending them, and that fallback reports success, so every screen said the email
was on its way. From 12 June to 19 August, every reset link and every verification code was created,
recorded and thrown away, with nothing on screen to indicate it.

**Filling in the blank mail settings would not have fixed it.** The mail server was tested directly
before anything was changed, and it does not support the connection method the software was written
to use. It also presents its security certificate under a different name from the one the site would
naturally have been pointed at, so the obvious configuration would have failed twice over and looked
like a wrong password both times. The email component was rebuilt to use the connection method the
server actually supports.

**A third fault was found the same way:** the sending address in the configuration did not exist as
a mailbox. Had the original settings shipped, sign-in codes would have been accepted by the mail
server and then bounced. The sending address is now a real mailbox.

**The credential is now stored where a deployment cannot destroy it.** Every other configuration file
is replaced when a new build is installed, so a mail password entered on the server would have been
wiped by the next deployment — silently, because email would simply revert to discarding messages.
It is now held in a file that deliberately does not travel with the build, and the deployment script
**refuses to run** if that file is ever bundled by mistake.

**And the failure can no longer be silent.** The site now states, in its start-up log, whether email
is live or disabled, and says plainly that two-factor sign-in and password reset will not work if it
is disabled. That check is now a standing item on the pre-deployment checklist. This was the real
lesson: the fault survived two months not because it was subtle but because **nothing ever said it
was broken.**

*Verified end to end on the test site: a real password reset was requested, the email arrived, the
link was used, and the old password stopped working.*

### DEF-365 — Two-factor authentication is now actually enforced per account

Switching two-factor authentication on for an individual account **had no effect on signing in**.
The setting was saved, the user list showed it as enabled, and the account was never once challenged
for a code. Sign-in consulted only a single system-wide switch and never looked at the individual
account's setting. That switch was off, so **two-factor protection was inactive for every account**,
including any account an administrator believed they had protected.

This is the most serious item in this release: a security control that appeared to be on and was not.

**The system-wide switch could not simply be turned on**, and it is worth being explicit about why.
Because sign-in ignored the per-account setting, enabling it would have demanded a code from
**every** account at once — including accounts that had never set up an authenticator and therefore
had no way to produce one. Turning it on before fixing the per-account check would have locked every
user out of the system.

A challenge now requires **both** the system switch and that specific account's own setting. The
switch has been turned on.

**Setting up an authenticator app was also impossible, for everyone.** The setup screen generated
its security key and never saved it, then checked the code you typed against a key that did not
exist. It rejected every code, always, and told the user to check their authenticator app — blaming
the user's phone for something the server had not stored. Setup now works. The key is also generated
by a properly secure method; the previous one was not suitable for a security credential.

**Two smaller faults fixed alongside:** an account set to use an authenticator was being emailed a
code as well, which quietly undermines the point of choosing an authenticator; and an account
switched on by an administrator was never told to finish setting it up. It now receives emailed
codes in the meantime — so nobody is locked out — and is taken to the setup screen automatically at
the next sign-in, with an explanation of why.

*Verified on the test site by an automated check that asserts both directions in one run: an account
without two-factor signs straight in, and an account with it is stopped and **not** signed in until
the code is answered.*

### DEF-366 — A duplicate email address now explains itself

Changing a user's email address to one already in use failed with **"Update failed. See the server
log for details."** — a message that names no cause and points at a file the reader cannot open.
The refusal was correct; the explanation was missing. Creating a user had always reported this
properly, so the two paths behaved differently for the same mistake. Editing now names the address
and gives the two ways forward.

### DEF-367 — The verification screen now says which code it wants

The two-factor screen read *"Enter the 6-digit code from your authenticator app or email"* for
everyone, every time, without saying which applied. A user with an authenticator set up on a
different account reasonably used it, and was told the code was invalid.

This is not cosmetic. **Five failed attempts lock an account for 15 minutes**, and a locked account
reports *"Invalid email or password"* — indistinguishable from a wrong password. An instruction
pointing at the wrong method manufactures exactly those failures and then disguises the result.
The screen now names the method in use, and where a code was emailed it shows the destination
partly masked, so the right inbox is obvious without exposing the address.

---

### Supporting material delivered with this release

- **User guide — Accounts & Sign-In** (`docs/user-guide/`): creating and managing accounts, roles
  and access scoping, two-factor setup and password reset, with ten screenshots taken from the
  current build. Supplied as Word and as a web page, both generated from one source so they cannot
  disagree.
- **Manual test cases** (`docs/qa/2026-08-20-auth-email-2fa-test-cases.md`): nine cases covering
  everything above plus role-based data scoping, written so each one can actually fail.
- **Test accounts** covering every role, including paired external and partner accounts so data
  isolation can be tested by comparison rather than assumption.

---

### DEF-389 — The product identifier count now updates as you add and remove them

On the certificate Edit page, adding a product identifier saved it straight away — but the
*"Showing 9 product ID(s)"* line beneath the table did not move. The new row was in the table and in
the database, while the only running total on the screen still said nine. The natural read is that
the addition had not taken, and the natural response is to save the certificate and go back in to
check — at which point the page reloads and the number is finally right, which makes the save look
like the step that committed it.

Nothing was ever lost. Product identifiers on the Edit page save the moment you add them and have
done since May; the certificate's own Save button has never been what commits them. The count was
simply not being redrawn after an add, an edit, a delete, or an import.

It now updates immediately on all of those, on both the Create and Edit pages. Two related
corrections came with it: on certificates with more than fifty identifiers the total is now taken
from the full list rather than the visible page, so filtering no longer makes the number appear to
drop to fifty; and on the Create page the count now appears as you build the list, where previously
it stayed blank until the certificate was saved.

**Delivered 2026-08-31** to the test environment (gccwebapp.com). A JavaScript file only — no application rebuild, no downtime, nothing else in the release. Confirmed live: the file served by the site now matches the fixed version exactly, and the count was checked on the deployed Edit page.
