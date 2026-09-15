---
org: GreenCircle Certified Database
title: Setting Up a Partner with API Access
slug: GCC_Partner_Setup_Guide_v1.0
lede: How to create a partner, tag the certificates they should receive, generate their API key, and prove it works before you hand it over.
version: 1.0
issued: September 2026
applies_to: Test environment
prepared_by: Anuki Consulting
audience: Administrators
colophon: GreenCircle Certified Database — Setting Up a Partner with API Access, version 1.0. Prepared by Anuki Consulting, September 2026. Every behaviour described here was verified against the running application in September 2026. For anything not covered here, contact your system administrator.
---

> [!NOTE] About the screenshots
> Every screenshot was taken from the current build on the test environment. **API key values are
> deliberately blurred** — no real key appears anywhere in this guide, and the confirmation panel
> that displays a newly generated key is described rather than shown, so that producing this guide
> never required creating a live credential.

> [!WARNING] Tag the certificates first, or the partner sees nothing
> An API key on its own returns an **empty list**. What a partner receives is decided by two
> separate checks, and the certificate tagging is the one people forget. Work through the steps in
> the order given below.

# 1. What you are setting up

A partner is an external organisation — Amazon, Novi, or another — that pulls certificate data from
GCC over the Partner API. Setting one up has four parts, and all four are yours to do. You do not
need to raise a request with Anuki for any of them.

| Step | What it does | Where |
|---|---|---|
| 1. Create the partner record | Gives the organisation an identity in the system | Administration → Partners |
| 2. Tag certificates to the partner | Decides which certificates they may receive | On each certificate |
| 3. Generate an API key | Gives them a credential tied to that partner | Administration → Partner API Access |
| 4. Test, then deliver the key | Proves it works before the partner tries it | Terminal, then secure channel |

## 1.1 The two checks that decide what a partner sees

This is the single most useful thing to understand, because almost every "the feed is empty" or
"why can't they see this certificate" question comes back to it.

1. **Is the certificate tagged to that partner?** If not, it reaches nobody — regardless of status.
2. **Is the certificate commercially in force?** Its status must be Client Approved – Paid, Client
   Approved – Confidential, or Republished, and today's date must fall inside its effective and
   expiration dates.

Both must be true. The checks are independent, so a perfectly valid certificate that nobody tagged
is invisible, and a tagged certificate that has expired is equally invisible.

> [!NOTE] Confidential certificates DO go to partners
> Confidential is an embargo on the **public** website only. Those certificates are paid, in-force
> work and they are still sent to partners and still badged. Do not assume Confidential means
> withheld from everyone.

# 2. Create the partner record

1. Sign in as an Administrator.
2. Go to **Administration → Partners**.

![The Partners page. Export and Add are the only actions — there is no import, because partner records are core entities rather than bulk data.](20-partners-list)

3. Click **Add**. A new editable row appears at the top of the table — this is an inline row, not a
   pop-up dialog.
4. Enter the **Partner Name**. It must be unique and is limited to 100 characters. This exact name
   is what the partner sees returned in the `partner` field of their API responses, so spell it the
   way you want it to appear.
5. Add a **Description** if useful — free text, up to 500 characters.
6. Save the row. New partners are **Active** by default.

![Clicking Add opens an editable row at the top of the table rather than a dialog.](21-add-partner)

> [!CAUTION] Deactivating a partner does NOT cut off their API access
> This is worth knowing before you rely on it. Setting a partner to inactive does **not** stop their
> API keys from working — the key check does not consult the partner's Active flag. **The only way
> to stop a partner calling the API is to revoke each of their keys** (section 7). Deactivating
> affects where the partner appears in the application, not their API access.

# 3. Tag the certificates

A partner receives only the certificates explicitly associated with them. Open each certificate that
should be included and add the partner in its **Partners** field.

For certificates that carry product identifiers — ASINs, UPCs, GTINs and so on — identifiers behave
slightly differently from the certificate itself:

| Identifier is tagged to | Does this partner receive it? |
|---|---|
| Nobody | **Yes** — untagged identifiers are treated as shared |
| This partner | **Yes** |
| A different partner only | **No** — it is withheld |

That last row is deliberate. Two partners can share a certificate without either of them seeing the
other's product identifiers.

# 4. Generate the API key

1. Go to **Administration → Partner API Access**.

![The Partner API Access page. The counts at the top show how many partners and keys exist, and how many of those keys are still active.](22-partner-api-access)

2. Click **Generate API Key**. A dialog opens with three fields.

![The Generate API Key dialog. Only Partner is required.](23-generate-key)
3. Choose the **Partner**. This is the only required field, and it is what scopes the key.
4. Enter a **Description**. Optional, and internal only — something like "Amazon production" or
   "Amazon test" so you can tell keys apart later.
5. Set an **Expiration Date**. Optional. Leave it blank for a key that never expires; set a date for
   a time-boxed test key.
6. Click **Generate Key**. The new key is shown with a copy button. **Copy it now** — see the
   warning below.

Keys are 76 characters: the prefix `gcc_partner_` followed by 64 hexadecimal characters.

> [!CAUTION] Copy the key before you close that dialog — it is shown only once
> This is the only moment the full key is visible. Afterwards the list shows every key **masked**,
> and the only action on each row is Revoke. There is no way to retrieve the value later. If you
> close the dialog without copying it, the key is unusable: revoke it and generate a replacement.
> Paste it somewhere safe before you click away.

## 4.1 What you cannot set per key

Two values that look configurable are not. They are system-wide, and there is no field for either on
the key:

| Setting | Value | Per key? |
|---|---|---|
| Rate limit | 200 requests per hour | No — the same for every partner |
| Endpoints permitted | All endpoints | No — keys are not scoped to a subset |

So only three things genuinely vary between partners: the partner name, the key itself, and the
key's expiry date.

# 5. Which address the partner uses

| Environment | Base URL | Status |
|---|---|---|
| Test | `https://dbtest.greencirclecertified.com/api/v1` | Live now — use for all integration work |
| Production | `https://database.greencirclecertified.com/api/v1` | At go-live only |

Partners should build against the test address and change the hostname at go-live. Nothing else
changes — the paths, the `X-API-Key` header and the response are identical.

> [!WARNING] The production address is not live yet
> `database.greencirclecertified.com` still serves the previous certificate database. Do not give it
> to a partner as a working address before cutover is confirmed.

# 6. Test the key before you hand it over

Always prove the key works yourself first. A partner hitting a problem on day one costs far more
time than this check.

1. Open a terminal and run the request below, substituting the key.
2. Read the response code before anything else.

```
curl -i -H "X-API-Key: gcc_partner_..." https://dbtest.greencirclecertified.com/api/v1/certificates
```

| Response | What it means | What to do |
|---|---|---|
| `200` with certificates | The key works | Deliver it |
| `200` with an empty list | The key is fine; nothing is tagged to that partner | Go back to section 3 |
| `401` | Key wrong, revoked, or expired | Check the key row is still active |
| `429` | Rate limit hit — 200 requests in the last hour | Wait, then retry |

> [!CAUTION] Do not test by pasting the URL into a browser where you are already signed in
> A signed-in session takes priority over the API key, so the page may load perfectly while the key
> itself is invalid. The result tells you nothing. Use a terminal, or a private browsing window.

Responses are cached for five minutes. If you tag a certificate and immediately re-test, wait five
minutes before concluding that something is wrong.

## 6.1 Delivering the key

Send the key by a secure channel — a password manager share, or another route your organisation
already trusts for credentials. Never paste it into the Integration Guide, an email body, or a chat
message. The Integration Guide gets forwarded inside partner organisations, and a key printed in it
is a key you will have to rotate.

# 7. Revoking and rotating a key

Use **Revoke** on the key's row. It takes effect immediately and cannot be undone — every request
using that key starts returning `401`. Revoked keys are retained for audit.

![The key list. Each row shows its partner, when it was created, whether it expires, how many requests it has served, and its status. Key values are masked by the application and blurred again for this guide. The single button in the Actions column is Revoke — or Delete, once a key has been revoked.](24-key-list)

Revoke a key when the partner no longer needs access, when a key may have been exposed, when you
issue the production key and the test key is no longer needed, or as part of routine rotation.

To rotate without an outage, generate the new key first, give the partner time to switch, then
revoke the old one. Both keys work while the changeover happens.

# 8. What the partner receives

| | |
|---|---|
| Certificates | Only those tagged to them, and only while in force |
| Status field | Always the literal string "Active" — internal status names are never exposed |
| Product identifiers | Untagged ones, plus those tagged to them |
| Internal tags | Never returned |
| External tags | Returned |
| Analyst notes, audit history | Never returned |

The API expresses validity by **presence**, not by a status value. A certificate that is no longer
in force does not appear with an "expired" status — it stops appearing at all. Partners should treat
disappearance as authoritative, and should not wait for a status change that will never arrive.

## 8.1 The endpoints

| Endpoint | Purpose |
|---|---|
| `GET /certificates` | List everything the calling partner may see |
| `GET /certificates/{id}` | One certificate by its internal numeric ID |
| `GET /certificates/number/{number}` | One certificate by its certificate number |
| `GET /certificates/search` | Search by term, company, type or date range |
| `GET /certificates/search/productid` | Find certificates by product identifier such as an ASIN |
| `GET /certificates/{id}/download` | Download the certificate PDF |
| `GET /certificates/gs1` | GS1 JSON-LD format, used by Amazon |
| `GET /certificates/gs1/{number}` | One certificate in GS1 format |

# 9. Troubleshooting

| Symptom | Almost always |
|---|---|
| Partner sees an empty list | No certificates tagged to that partner — section 3 |
| A specific certificate is missing | Not tagged, expired, not yet effective, or still in draft |
| `401` on every request | Key revoked or expired; check the key row |
| `403` on one path | The endpoint is outside what that key may call |
| `429` | 200-per-hour rate limit reached |
| Partner sees a certificate you expected to be private | Confidential is embargoed from the public site only — it still reaches partners |
| Changes not showing | Five-minute response cache; wait it out |

If a partner asks **why** a particular certificate is not visible, tell them rather than letting them
infer it. The API deliberately makes "expired", "not tagged to you" and "never existed" look
identical, so that a key cannot be used to probe for certificates outside its scope. That is by
design, and it means the partner genuinely cannot work it out from the response.

# 10. Appendix — filling in a partner's "Your Connection" panel

The Partner Integration Guide — the document sent to the partner's engineering team — opens with a
panel headed **Your Connection**, introduced as *"values specific to the key issued to you."* Filling
it in is the last step before issuing that guide, and it is where nearly every question about partner
setup has come from.

**The panel overstates how much varies.** It reads as eight or nine values to look up per partner.
**Only three actually change between partners.** The rest are the same for everybody, and two of them
cannot be configured per key at all — there is no field for them anywhere in the application.

## 10.1 Row by row

| Row in the panel | Where the value comes from | Varies per partner? |
|---|---|---|
| Partner name | The Partner record you created in section 2 — spelled exactly as stored | **Yes** |
| API key | The Generate Key dialog, section 4 | **Yes** |
| Key expiration | The Expiration Date field in that dialog; blank means never | **Yes** |
| Base URL | Fixed. `https://database.greencirclecertified.com/api/v1`, at go-live | No |
| Test base URL | Fixed. `https://dbtest.greencirclecertified.com/api/v1`, live now | No |
| Rate limit | **System-wide: 200 requests per hour.** No per-key field exists | No |
| Endpoints permitted | **All endpoints.** Keys are not scoped to a subset | No |
| Certificate status mode | Always *Presence* — validity is expressed by whether a certificate is returned at all. See section 8 | No |
| Product identifier scope | Fixed: identifiers tagged to nobody, plus those tagged to this partner. See section 3 | No |

> [!WARNING] Two rows used to carry an instruction that could not be followed
> Earlier revisions of the Integration Guide told the issuer to *"check the rate limit and permitted
> endpoints against the API key actually issued to that partner."* **There is nothing on the key to
> check them against** — both are system-wide. If you are working from an older copy of that guide,
> ignore that instruction; the current template has the two values pre-filled as the constants they
> are.

## 10.2 The questions this panel keeps producing

1. **"Where do we get the API key — does this have to be requested from the developer?"** No. You
   generate it yourself in Administration → Partner API Access. Section 4 of this guide is the whole
   procedure.
2. **"Where would this information come from?"** — asked against the rate limit, the endpoint scope,
   the status mode and the key expiry. Of those four, only the expiry is something you choose; see
   the table above.
3. **"Can the partner name be whatever we enter, or does it have to match something in the key?"** It
   must match. The value the partner receives in the `partner` field is the Partner record's name as
   held in the database, and that same name decides which certificates the key can see. It is not a
   free-text label.
4. **"After go-live, does the test address stay or is it production only?"** Both, depending on the
   partner's stage. During onboarding the partner works against the test address. Once they are live,
   their copy of the guide should mark that row as no longer applicable — a live test address left in
   a document that gets forwarded internally invites somebody to build against the wrong
   environment. The test environment itself stays, because the next partner needs it.
5. **"Is `amazonCode` relevant to anyone but Amazon — should it be removed?"** Keep it. That field is
   returned in the payload to **every** partner, not only Amazon. Removing it from the field table
   would leave a partner looking at a field in the JSON that the documentation never mentions.
6. **"Who does the onboarding checklist's Owner column refer to?"** It should read GCC in the
   partner-facing copy — GreenCircle is the partner's counterparty. Internally, the steps that were
   historically done by the developer are issuing the test and production keys, and those are the
   screens documented in section 4.

> [!NOTE] Never paste a live key into the Integration Guide
> That document is circulated by email and forwarded inside partner organisations. A key printed in
> it is a key you will have to rotate. The API key row should describe the format and say the key is
> delivered separately — which is what the current template does.
