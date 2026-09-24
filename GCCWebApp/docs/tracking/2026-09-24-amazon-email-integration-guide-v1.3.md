# Cover email: Amazon, Integration Guide v1.3 (2026-09-24)

**Attachment:** `docs/tracking/api-samples/GCC_Partner_API_Integration_Guide_Amazon_2026-09-24.docx`
**Status:** drafted, not sent. After sending, add a row to `partner-api-guide-issuance-log.md`.

---

**Subject:** GCC Partner API: production address is db.greencirclecertified.com (Integration Guide v1.3)

Hi [Name],

Attached is version 1.3 of the GCC Partner API Integration Guide. The only thing that changes for you
is the production address.

**The production API is now live at:**

`https://db.greencirclecertified.com/api/v1`

Earlier versions of the guide (1.0 to 1.2) gave the production address as
`database.greencirclecertified.com`. We've since moved the new certificate database onto
`db.greencirclecertified.com`, the address it has always used, so that links to certificates already
in circulation keep working. That was our change, and the guide now reflects it.

**Please use `db.greencirclecertified.com` directly.** `database.greencirclecertified.com` still
forwards to it, but some HTTP clients drop the `X-API-Key` header when they follow a redirect to
another address, and the request then fails as unauthorised. If your integration is already set up
with `database.`, please switch it to `db.`. Could you also let us know which address you're calling
today, so we can confirm nothing is affected?

**Nothing else changes.** The paths, the `X-API-Key` header, the parameters (including
`includeProductIds=true` and paging with `pageNumber`/`pageSize` until `hasNext` is false) and the
response format are all exactly as before.

**Testing:** the test address is unchanged, at `https://dbtest.greencirclecertified.com/api/v1`, and
stays available for integration testing.

**Your new production API key.** With the move to production we're issuing Amazon a new key. It's
available at this one-time secure link, which expires on [EXPIRY DATE] and can be opened once:

[SECURE LINK TO API KEY]

The password to open it will come to you separately by [TEXT / PHONE]. Please let us know once you've
saved the key. The key shown in the earlier Developer Guide will be retired on [RETIREMENT DATE], so
please switch to the new key before then. API keys are never included in the guide itself.

Thanks,
Shawn West
Anuki Consulting, on behalf of GreenCircle Certified
