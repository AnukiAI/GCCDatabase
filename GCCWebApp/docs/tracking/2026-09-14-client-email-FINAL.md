# Email to the client — 2026-09-14 (FINAL, paste-ready)

**Attach:**
1. `GCC_Partner_API_Integration_Guide_Amazon_2026-09-14.docx` — the copy for Amazon
2. `GCC_Partner_API_Integration_Guide_MASTER_2026-09-14.docx` — GCC's master (keeps the how-to box)
3. `2026-09-14-gcc-how-to-create-partner-api-key.md` — key setup steps

---

Hi Carly,

Updated guide attached, along with answers to everything you raised. Two versions, because they have
different jobs:

- **`..._Amazon_2026-09-14.docx`** — this is the one to send on. The "how to use this template" box
  has been removed, as that box itself instructs.
- **`..._MASTER_2026-09-14.docx`** — keep this one. It is identical except the how-to box is still in
  it, so it stays usable as the template for Novi and anyone after.

## The one thing Amazon needs to know before they start

**Have them build against the test host, and change one hostname at go-live.**

- **Test — live right now:** `https://dbtest.greencirclecertified.com/api/v1`
- **Production — at go-live:** `https://database.greencirclecertified.com/api/v1`

The guide previously pointed at the production hostname as though it were live. It is not — that
name still belongs to the existing certificate database until cutover. I have corrected it
throughout: the Quick Start example now uses the test host so it works if Amazon copy-pastes it
today, and the production row is marked *"available at go-live."*

I checked the test host this morning rather than assuming: it resolves to our server, its security
certificate is valid for that exact name, and an anonymous request correctly returns *401
Unauthorized* — so the endpoint is up and the key check is working. Amazon can start as soon as they
have a key.

**Nothing else changes at go-live.** Same paths, same `X-API-Key` header, same response. One
hostname.

## Creating the API key yourselves

You asked where keys come from, and the answer is that you can do this — it is a screen in the
application, not something you need us for. Full steps in the third attachment; the short version:

**Admin → Partner API Access → Generate Key.** Three fields, only the first required:

- **Partner** — *Amazon*. This is what scopes the data.
- **Description** — optional label, e.g. "Amazon — test".
- **Expiration Date** — optional; leave empty for a key that never expires. For a test key, set one.

The key appears with a copy button, and **that is the only time you can see it** — afterwards the
list shows it masked and there is no way to retrieve the full value, so a key you don't copy has to
be revoked and regenerated. Copy it straight into wherever you're sending it from. Send it to Amazon
by a secure channel rather than in the guide — the guide gets forwarded internally, and a key printed
in it is a key you have to rotate.

**The one thing that catches people out:** a key on its own returns an empty list. The certificates
also have to be tagged to that partner. If Amazon says "we're getting nothing back", it is almost
always untagged certificates rather than a bad key.

## Your comments on the document

**The "§" symbols — gone.** You were right that they were opaque. That is the section symbol (§8 =
Section 8), standard in technical specs but not worth the friction. All sixteen now read "Section 8",
"Section 2" and so on.

**"Where do we get the API key — does GCC have to request it from Anuki?"** — **No. You create it
yourselves**, and the master copy of the guide now says how, in the "how to use this template" box:

> Admin → Partner API Access → Generate Key. It asks only for **Partner** (required), an optional
> **Description** and an optional **Expiration Date**. Copy the key from the confirmation panel and
> send it to the partner by secure channel.

Checking this properly turned up something worth telling you: **fewer rows of the *Your Connection*
panel actually vary per partner than the guide claimed.** It said seven. It is really **three** — the
partner name, the key, and the key's expiry. Rate limit and permitted endpoints are **system-wide
settings with no per-key field at all**, so I have filled them in as the constants they are rather
than leave them as blanks you could never correctly fill:

- Rate limit: **200 requests per hour**
- Endpoints: **all of them**

The box previously told you to check those two against the key you issued. There is nothing on the
key to check them against — so that instruction has been corrected, not just the values. That closes
several of your "where would this come from?" comments permanently.

**One more thing the key setup catches people on**, now also in the box: a key returns an **empty
list** until certificates are tagged to that partner. Tag first, then generate.

**`amazonCode` — worth keeping even for non-Amazon partners.** The field is returned to every partner
in the payload, not just Amazon. If the guide didn't mention it, a partner would see a field in the
JSON that the documentation never explains. I'd rather keep the row and its "may be blank where not
applicable" note.

**`[PARTNER NAME]`** — good catch, and it does need to match. The `partner` field description in
Section 9 now says so in the document: it is the partner name exactly as GCC holds it, not a
free-text label, and it is the same name that determines which certificates the key can see.

**The onboarding checklist showing GCC as owner** — I think that is right as-is for the
partner-facing copy; GreenCircle is Amazon's counterparty and that is what they should see. Issuing
the keys is the part that was really ours, and as above, that is now yours whenever you want it.

## The AmazonCode variations — yes, those four are correct

They are the complete set Amazon receives today. I checked against the live feed rather than the
database: we pulled the actual payload using the Amazon key, and all 212 certificates carry a
long-form code. None carries a short one.

    greencircle_cef                              78 certificates
    greencircle_sustainable_energy_practices     67
    greencircle_recycled_content                 48
    greencircle_energy_savings                   19

Still open from 9/10, and I would like to close it: those four are the only four of seventeen
certification types using the long format. The other thirteen still hold short codes internally. They
reach Amazon today only because none of those types is currently tagged to Amazon — your edits on
9/10 removed the last four.

If a certificate of one of those types is ever tagged to Amazon, it goes across with a code Amazon
doesn't recognise and quietly isn't badged. No error, nowhere to see it. Tell us which types Amazon
should ever receive and the official string for each, and we'll set them in one go. About an hour
once we have the mapping.

## The link with no content

That one is a red herring, in a good way. Those aren't links we put in the guide — they are two
values inside the JSON response itself (`@context` and `$schema`) that the GS1 data model requires
every document to carry. Word turned them into clickable links automatically.

Both files are fine; I re-checked them and each returns its full content. What you clicked through to
is the *folder* above them — GS1 US published the two files and never wrote a landing page for the
folder, so you get their placeholder. Nothing is broken on our side and nothing needs to change in
the payload.

If you want to read about the model, the right place is the repository, which has the README, the
schema and worked examples: https://github.com/gs1us-technology/certification-data-model

Thanks,
Shawn

---

## Before sending — check these

- [x] **Amazon copy personalised** — `[PARTNER NAME]` ×6 → *Amazon*, `[MONTH YEAR]` ×2 →
      *September 2026*, `[YEAR]` → *2026*. The pale-yellow "fill me in" highlight was cleared from
      each, so they read as finished text rather than as blanks. The MASTER is untouched and stays
      generic for Novi.
- [ ] **Three brackets left in the Amazon copy, each one decision away:**
      - `[None — or the agreed expiry date]` — depends on whether Amazon's key expires. Decide when
        you generate it.
      - `[GCC support contact]` — the email address or person Amazon should contact. Yours to supply.
      - `[Presence — the feed contains only currently-certified records]` — this is already the
        *correct* value; it just needs the brackets removed. Say the word and I'll do it.
      (`[your key — delivered separately]` and `[64-character hash]` are deliberate — they stay.)
- [ ] Confirm the production hostname decision — the guide promises
      `database.greencirclecertified.com` at go-live. If the app ends up on a different name, that
      row changes and Amazon will have built against the promise.
