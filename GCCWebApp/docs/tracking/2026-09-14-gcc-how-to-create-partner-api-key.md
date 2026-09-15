# How to create a Partner API key

**For GreenCircle Certified staff.** Not for partners — do not send this to Amazon.
Written against the live application, 2026-09-14.

---

## Before you start: creating a key is step 2, not step 1

A key on its own returns an **empty list**. Two things control what a partner sees, and they are
checked in this order:

1. **The certificate must be tagged to that partner** (Certificate → Partners). A certificate tagged
   to nobody reaches nobody — including Amazon.
2. **The key must belong to that same partner.**

So if Amazon reports "the feed is empty", the usual cause is untagged certificates, not a bad key.

---

## Creating the key

1. Sign in as an **Admin**.
2. Go to **Admin → Partner API Access** (`/Admin/PartnerAPI`).
3. Click **Generate Key** (top right).
4. Fill the form. It has **three** fields and only the first is required:

| Field | Required | What to enter |
|---|---|---|
| **Partner** | **Yes** | The partner this key belongs to — e.g. *Amazon*. This is what scopes the data. |
| **Description** | No | A label so you can tell keys apart later, e.g. *"Amazon — test"* or *"Amazon — production"*. |
| **Expiration Date** | No | Leave empty for a key that never expires. Set a date for a time-boxed test key. |

5. Click **Generate Key**. A confirmation panel shows the new key with a **Copy** button.
6. **Send the key to the partner by a secure channel** — not in the Integration Guide, not in the
   body of an email. The guide is forwarded internally by partners; a key printed in it is a key you
   will have to rotate.

**Copy the key before closing that panel — it is shown only once.** Afterwards the list displays
every key masked, and the only action on each row is Revoke (or Delete, once revoked). There is no
way to retrieve the full value again; a key you failed to copy has to be revoked and replaced.

*(Security note for Anuki, not for the client: the list API returns keys **unmasked** to the browser
and the value sits in each row's `data-key-value` attribute, where it feeds the revoke confirmation.
So the value is technically present in the page even though no UI exposes it. Worth revisiting — but
it is not a supported way to recover a key, and must not be documented as one.)*

---

## What you can and cannot set per key

This matters because the Integration Guide's *Your Connection* panel implies more is configurable
than actually is. **Verified in the application, 2026-09-14:**

| Guide row | Actually per-key? | The real value |
|---|---|---|
| API key | **Yes** | Generated here |
| Key expiration | **Yes** | The Expiration Date field; blank = never |
| Partner association | **Yes** | The Partner dropdown |
| **Rate limit** | **No** | **200 requests per hour**, system-wide for all partner keys. There is no field for it. |
| **Endpoints permitted** | **No** | **All endpoints.** Keys created through this screen are not scoped to a subset. |
| Base URL / Test base URL | No | Same for every partner |
| Certificate status mode | No | Always *Presence* — see Section 4 of the guide |
| Product identifier scope | No | Fixed by CR-171 |

**Consequence:** only **three** of the panel's rows genuinely vary per partner, not seven. The guide's
"how to use this template" box still says seven; that is now overstated. Rate limit and endpoint
scope have been filled in as the constants they are.

---

## Revoking a key

Use **Revoke** on the key's row. It takes effect immediately and **cannot be undone** — every request
using that key starts returning `401`. Revoked keys are retained for audit. Generate a new key if the
partner needs access again.

Revoke the **test** key when you issue the production key (Section 10, step 6 of the guide).

---

## Testing a key before you send it

Run this from a terminal, or ask Anuki to:

```bash
curl -i -H "X-API-Key: <the key>" \
  https://dbtest.greencirclecertified.com/api/v1/certificates
```

- **200 + JSON** — the key works.
- **401** — key wrong, inactive, or expired.
- **200 but an empty list** — the key is fine; **no certificates are tagged to that partner**.

**Do not test by pasting the URL into a browser tab where you are already signed in to the
application.** A signed-in session takes precedence over the key, so the result tells you nothing
about whether the key works. Use a private/incognito window, or `curl` as above.

The API caches responses for **five minutes**. If you tag a certificate and immediately re-test, wait
five minutes before concluding anything.

---

## Which host to use

| | Host | Status |
|---|---|---|
| **Test** | `https://dbtest.greencirclecertified.com/api/v1` | **Live now** — use for all partner integration work |
| **Production** | `https://database.greencirclecertified.com/api/v1` | **At go-live only** — currently still serves the existing certificate database |
