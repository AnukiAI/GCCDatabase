# Draft reply — Amazon Integration Guide questions (client email, 2026-09-14)

Status: **DRAFT for Shawn's review. Not sent.**
Source: client email "Amazon Integration Guide Questions / Issues" + 15 Word comments on
`GCC_Partner_API_Integration_Guide_TEMPLATE_GCC_2.docx` (all by the client, 2026-08-28).

---

## Part 1 — The three email bullets

### Bullet 1: are those four the final AmazonCode variations?

**Yes — those four are correct, and they are the complete set of codes Amazon receives today.**
Verified from the live feed, not from the database: we pulled the actual Amazon payload from the test
environment with the Amazon API key on 2026-09-10 after CR-021 landed. All 212 certificates in it
carry a long-form code and **none carries a short one**.

| Code | Certificates in the Amazon feed |
|---|---|
| `greencircle_cef` | 78 |
| `greencircle_sustainable_energy_practices` | 67 |
| `greencircle_recycled_content` | 48 |
| `greencircle_energy_savings` | 19 |

**The caveat worth one minute of their time.** Those four are the only four of **seventeen**
certification types that hold the long format. The other thirteen still carry the original short
codes internally (`MIV`, `CSF`, `GCA`, `WDL`, `LCA`, `CLP`, `CWS`, `DEM`, `SMP`, `GHG`, plus three
with no certificates behind them).

They reach nobody today — the client's own 9/10 edits removed the last four short-coded types from
the feed, and CR-021 fixed the fifth. But the failure mode if one ever gets Amazon-tagged is
**silent**: it crosses over with a code Amazon does not recognise and simply is not badged, with no
error anywhere. Already asked on 9/10; re-asking here because it is the natural follow-on to their
own question. (~1 hr to set them all once they supply the mapping — one CR for the set.)

### Bullet 2: the GS1 link with no content

**That is not a link in the guide — it is a value inside the JSON response**, and the page they
landed on is GS1 US's own unfinished landing page, not something broken on our side.

- The guide's §5 sample response contains `"@context"` and `"$schema"`, which the GS1 Certification
  Data Model requires every document to carry. Our API emits them verbatim
  (`Models/Api/GS1CertificationReport.cs:15,21`). Word auto-linked them.
- **Both files resolve and return real content** — re-checked 2026-09-14:
  - `https://gs1us-technology.github.io/certification-data-model/cert_schema.json` → full JSON Schema
  - `https://gs1us-technology.github.io/certification-data-model/cert_context.json` → JSON-LD context
- The *directory* above them (`…/certification-data-model/`) is the stub page they saw. GS1 US
  published the two files to GitHub Pages and never wrote an index page for the folder.
- The human-readable home for the model is the repository:
  **`https://github.com/gs1us-technology/certification-data-model`** — README, the schema, the
  JSON-LD context, worked examples and a validation script.

**Recommended edit to the guide:** add one sentence under the §5 sample pointing to the repo, so a
reader who clicks the raw URL out of curiosity has somewhere sensible to go. No change to the payload
— those two URLs are part of the GS1 contract and must stay exactly as they are.

### Bullet 3: the Word comments

Answered individually below.

---

## Part 2 — The 15 Word comments

Eight of the fifteen are the same question — *"where would this info come from?"* — so they are
grouped.

### Group A — the "Your Connection" panel (comments 0, 3, 4, 5, 6, 8, 9)

> *"Where do we get API Key and this information? Does GCC have to request from Anuki?"*
> *"I don't know how I would fill in some of missing items when I take the template and apply to Novi"*

**Every one of those seven rows comes from the API key record itself**, which lives in the
application under **Admin → API Keys**. Nothing has to be invented, and nothing has to be looked up
in a second place.

| Panel row | Where the value comes from | Varies per partner? |
|---|---|---|
| Base URL | Fixed — the production host | **No** |
| Test base URL | Fixed — the test host | **No** |
| API key | Generated when the key is created; delivered by secure channel | Yes |
| Rate limit | `RateLimitPerHour` on the key record (default 1,000/hr) | Yes |
| Certificate status mode | Always *Presence*. Not a per-partner setting | **No** |
| Endpoints permitted | The key's scope — "All" unless deliberately narrowed | Yes |
| Product identifier scope | Fixed by CR-171 (untagged + tagged-to-you) | **No** |
| Key expiration | `ExpiresAt` on the key record — "None" unless agreed | Yes |

So only **four** rows are genuinely per-partner, and all four are readable off the key.

**Practical offer, and the real answer to the Novi problem:** rather than hand them a template with
blanks, **we fill it in at the point we issue the key**. When a partner key is created we send back a
completed copy of the guide with all four values populated. That removes the guesswork entirely and
means the document can never disagree with the key that was actually issued — which is the failure
this template was written to avoid. Suggest we do exactly this for Novi as the first one.

**On "does GCC have to request from Anuki?":** today, yes — key creation is an Admin function we run.
It is a screen in the app, so it can move to GCC whenever they want it; it is a training question,
not a technical one. See also comment 14.

### Comment 1 and 7 — "§2", "§8": *"I don't understand these throughout doc? What are they referencing?"*

`§` is the section symbol — `§2` means Section 2, `§8` means Section 8. It is standard in technical
specifications but it is genuinely opaque if you haven't met it.

**Agreed — change it.** Replace every `§N` with `Section N` throughout. Fifteen minutes, zero risk,
and the guide is read by integration engineers at partners who may have the same reaction.

### Comment 2 — Test base URL: *"After we go live I assume this would stay or is it just production at that point?"*

> **Updated 2026-09-14 (Shawn):** the test host is **`dbtest.greencirclecertified.com`**, not
> `gccwebapp.com` — `database.` and `db.` are both in use by the existing certificate database.
> The guide (v3) now states `https://dbtest.greencirclecertified.com/api/v1` everywhere the test
> host appears. **Verified live today:** resolves to the application server, TLS certificate
> explicitly covers the name, and `/api/v1/certificates` returns 401 to an anonymous caller — i.e.
> it is our app and the key check is enforced. All four checks green (`docs/cutover/scripts/verify-cutover.sh`).

Both, deliberately, and it depends on the partner's stage:

- **During onboarding** the row carries the test URL. §10 steps 3–6 are built on it: issue a test key,
  the partner validates end to end against sample data, then the production key is issued and the
  test key is revoked.
- **After that partner goes live**, their copy of the guide should have the row read
  *"Not applicable — production key issued [date]"*. Leaving a live test URL in a document that a
  partner forwards internally invites someone to build against the wrong environment.
- **The test environment itself stays**, because the next partner needs it and because it is where
  we regression-test before anything reaches production.

### Comments 11 and 13 — `amazonCode`: *"This would not be relevant for anyone but Amazon — remove?"*

**Reasonable instinct, but no — and the reason matters.**

`amazonCode` is returned in the payload to **every** partner, unconditionally. It is not gated on who
is calling (`Controllers/Api/v1/CertificatesController.cs:282, 348`). Novi's response will contain
the field too.

Removing it from the field table would mean a partner sees a field in the JSON that the guide never
mentions — which reads as an undocumented surprise and generates a support question. The current
wording already handles it: *"Present in the payload for all partners; may be blank where not
applicable to your program."*

The field could be *renamed* to something program-neutral, but that is a breaking change to a
contract Amazon already consumes, so it is not worth doing for cosmetics.

**Suggested compromise:** keep the row, and drop the `amazonCode` line from the §5 JSON sample in
non-Amazon copies only, since the sample is illustrative and the field table is the contract.

### Comments 10 and 12 — `[PARTNER NAME]` as the `partner` example: *"Can this be whatever we add, or does it need to match something in API key?"*

**It must match — it is not free text.**

The `partner` field returns the name of the Partner record the calling key is associated with,
intersected with the partners on that certificate. The same name drives the scoping: a key sees a
certificate only if that certificate is tagged to its partner. So on an Amazon key the field reads
`Amazon`; on a Novi key it reads `Novi`.

In the template, `[PARTNER NAME]` is a placeholder for **the Partner record's name exactly as it
appears in the GCC database** — not a free-form label. Worth spelling that out in the guide, because
getting it wrong would have the partner writing string comparisons against a value that never
appears.

### Comment 14 — §10 owner column: *"Is this us or Anuki (ok if it says GCC for external purposes but wouldn't we need setup help behind the scenes?)"*

**Both, and the read is right.** The guide is partner-facing, so the Owner column should stay as
**GCC** — the partner's counterparty is GreenCircle, not a subcontractor, and that is the correct
thing for them to see.

Behind it, today:

| §10 step | Partner sees | Actually performed by |
|---|---|---|
| 1 — tag certificates to the partner | GCC | **GCC** (Admin screen) |
| 2 — confirm validity model + rate limit | GCC | GCC decides; Anuki confirms the technical value |
| 3 — issue test key | GCC | **Anuki** today |
| 6 — issue production key, revoke test key | GCC | **Anuki** today |

Steps 3 and 6 are an Admin screen in the application, so they can transfer to GCC with a short
walkthrough. Steps 1 and 2 are already theirs. Happy to add an **internal-only** RACI page —
separate from the partner-facing document, so the partner copy never shows a third party.

---

## Part 3 — Flags for Shawn only (NOT in the email)

1. **The production base URL in the guide does not serve the API yet — RESOLVED IN THE DOC, CUTOVER
   PREPARED.** `database.greencirclecertified.com` (70.32.82.212) is an **nginx redirector**: it
   returns **302 on every path** to `https://db.greencirclecertified.com`, discarding the path.
   *(Correcting my first reading: I reported it as "HTTP 200 serving the legacy Next.js site" — that
   was the redirect target, not the host. The hostname itself runs no application, which makes the
   cutover easier, not harder.)* `gccwebapp.com` (64.27.52.66) correctly returns 401.

   **Done on Shawn's instruction, 2026-09-14:** the guide now marks the Base URL row *"available at
   go-live"* — see `GCC_Partner_API_Integration_Guide_TEMPLATE_GCC_3.docx`. Cutover prepared in
   `docs/cutover/` (inventory, audit, runbook, verify script). **One hard blocker: who holds the
   HostMonster DNS login.**

2. **Bullet 1's caveat has now been raised twice** (9/10 in QC #264, again here). If they don't
   answer this round, consider raising the thirteen short codes as a formal CR with a stated
   assumption rather than leaving it open — it is a silent failure mode and an open loop on our side.

---

## Part 4 — Paste-ready email

Hi Carly,

Answers below, and thank you for the doc comments — a few of them caught real gaps.

**1. The AmazonCode variations — yes, those four are correct**, and they are the complete set Amazon
receives today. I checked this against the live feed rather than the database: we pulled the actual
Amazon payload from the test environment using the Amazon key, and every one of the 212 certificates
carries a long-form code. None carries a short one.

    greencircle_cef                              78 certificates
    greencircle_sustainable_energy_practices     67
    greencircle_recycled_content                 48
    greencircle_energy_savings                   19

One thing to flag, which is the same point I raised on 9/10 and I'd like to close out. Those four are
the only four of seventeen certification types that use the long format — the other thirteen still
hold the original short codes internally (MIV, CSF, GCA, WDL, LCA, CLP, CWS, DEM, SMP, GHG, plus
three with no certificates). They reach Amazon today only because none of those types is currently
tagged to Amazon; your edits on 9/10 removed the last four.

If a certificate of one of those types ever gets tagged to Amazon, it will go across with a code
Amazon doesn't recognise and quietly not be badged — no error, nowhere to see it. If you can tell us
which types Amazon should ever receive and the official string for each, we'll set them all in one
go so it can't catch anyone out later. About an hour of work once we have the mapping.

**One correction to the guide itself, and it's the most important thing in this email.** The base
URLs in the version you have are wrong, and I've fixed them in the attached copy.

- **Test base URL — use this now:** `https://dbtest.greencirclecertified.com/api/v1`
  This is live today. I verified it this morning: it resolves to our application server, the security
  certificate is valid for that exact name, and an anonymous request to `/api/v1/certificates`
  correctly returns *401 Unauthorized* — so the endpoint is up and the API key check is working.
  Amazon can point a test integration at it as soon as they have a test key.
- **Production base URL — `https://database.greencirclecertified.com/api/v1`, available at go-live.**
  That hostname currently still belongs to the existing certificate database, so it does **not**
  serve this API yet. The attached guide now says *"available at go-live"* on that row rather than
  presenting it as live. Nothing about the URL shape changes — only the date it starts working.

Worth being explicit with Amazon about this, because it's the one thing that would waste their time:
**build against the test host, and switch the hostname at go-live.** Nothing else in the integration
changes — same paths, same key header, same response shape.

**2. The link with no content — that one is a bit of a red herring, in a good way.**

Those aren't links we put in the guide. They're two values inside the JSON response itself
(`@context` and `$schema`), which the GS1 Certification Data Model requires every document to carry.
Word turned them into clickable links automatically.

The files themselves are fine — I re-checked both this morning and each returns its full content:

    …/certification-data-model/cert_schema.json     the JSON Schema
    …/certification-data-model/cert_context.json    the JSON-LD context

What you clicked through to is the *folder* above them. GS1 US published the two files to GitHub and
never wrote a landing page for the folder, so you get their placeholder. Nothing is broken on our
side and nothing needs to change in the payload — those URLs are part of the GS1 contract.

If you want to read about the model, the right place is the repository itself, which has the README,
the schema, the context, worked examples and a validation script:
https://github.com/gs1us-technology/certification-data-model

I'll add a line under the sample response in the guide pointing there, so the next person who clicks
out of curiosity lands somewhere useful.

**3. The doc comments.** I've answered all fifteen in the attached copy. Four themes:

- **"Where does this information come from?"** — every value in the *Your Connection* panel comes off
  the API key record itself, and only four of the eight rows actually vary between partners (the key,
  the rate limit, the endpoint scope and the expiry). The rest are the same for everyone. **Better
  still: rather than hand you a template with blanks, we'll fill it in when we issue the key** and
  send you a completed copy. That way the document can never disagree with the key that was actually
  issued, which is the mistake this template exists to prevent. Happy to do Novi that way as the
  first one.

- **The "§" symbols** — you're right, and they're going. That's the section symbol (§2 = Section 2),
  standard in technical specs but not worth the friction. I'll spell them out throughout.

- **`amazonCode` — worth keeping, even for non-Amazon partners.** The field is returned to every
  partner in the payload, not just Amazon, so if the guide doesn't mention it a partner will see a
  field in the JSON that the documentation never explains. I'd rather keep the row and its "may be
  blank where not applicable" note. I will drop it from the sample response in non-Amazon copies,
  since that part is only illustrative.

- **`[PARTNER NAME]`** — good catch, and it does need to match. That value is the partner's name
  exactly as it appears in the database, and it's the same name that controls which certificates the
  key can see. I'll make that explicit in the guide rather than leaving it looking like free text.

On your point about the onboarding checklist showing GCC as owner — I think that's right as-is for
the partner-facing document; GreenCircle is their counterparty and that's what they should see. But
you're right that a couple of those steps are ours behind the scenes, specifically issuing the test
and production keys. Both are a screen in the application, so they can move to your team whenever you
want them — it's a short walkthrough, not a technical barrier. I can put together an internal-only
version of that checklist showing who actually does what, kept separate from the partner copy.

Thanks,
Shawn

---

## Before sending

- [x] **Production base URL** — marked *"available at go-live"* in the guide, 2026-09-14
- [x] **Test base URL** — corrected to `dbtest.greencirclecertified.com`, verified live (4/4 checks)
- [x] All `gccwebapp.com` references removed from the partner-facing guide (0 remain)
- [ ] Attach **`GCC_Partner_API_Integration_Guide_TEMPLATE_GCC_3.docx`**
- [ ] **Decide before issuing to a partner:** the guide's "How to use this template" box is marked
      *delete this box before issuing*. It is still in v3 and now carries the NOT-YET-LIVE note.
      Delete the box for a partner-facing copy; keep it for GCC's internal master.
- [ ] Confirm the production hostname decision — is `database.greencirclecertified.com` definitely
      the one the app takes over at go-live, or does the app get a different name?
- [ ] Confirm the rate limit actually set on the Amazon key before quoting a number anywhere
- [ ] Decide whether to raise the thirteen short codes as a formal CR this round
