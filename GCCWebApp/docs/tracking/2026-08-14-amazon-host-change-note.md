# Note to Amazon — Production API host change

**Status:** Draft for review — not sent
**Date drafted:** 2026-08-14
**Attach:** `GCC Database Partner API - Amazon Developer Guide v1.2.pdf`
**Before sending, fill:** the cutover date in the *Timing* line, and the sender sign-off.

---

## Email draft

**Subject:** GCC Partner API — production endpoint change ahead of go-live (action required)

Hi [NAME],

Ahead of go-live I need to flag one change on our side that affects your configuration.

**The production API will be served from a dedicated subdomain rather than a path under our main
website.** The base URL is:

> `https://database.greencirclecertified.com/api/v1`

This replaces the `https://greencirclecertified.com/database/api/v1` address in the developer guide
you have. That address was never activated and will not serve the API, so it should be removed from
your configuration rather than kept as a fallback.

**Only the host changes.** Every path, query parameter, header, field name and response shape is
identical to what you have already built and tested — `/certificates/gs1`, `/certificates`,
`/certificates/search/productid`, the `X-API-Key` header, pagination, and both the Standard JSON and
GS1 JSON-LD payloads are all unchanged. For your integration this should be a single configuration
value.

One detail worth noting for the GS1 endpoint: the `id` fields in the JSON-LD response are generated
from the host you call, so they will change from `gccwebapp.com` to
`database.greencirclecertified.com` when you point at production. They are identifiers rather than
links to fetch, but if you store or compare them, that is the one place the change is visible in the
payload.

**The reason** is infrastructure rather than anything in the API contract: the certificate database
runs as a separate application and cannot be hosted on the same server as our main website. Giving it
its own subdomain also gives it its own certificate and its own scaling path, independent of the
website.

**The test environment is unaffected.** `https://gccwebapp.com/api/v1` continues to work exactly as
it does today, so you can keep validating against it while you make the production change.

**Timing:** the subdomain goes live with DNS cutover on **[DATE]**. Your production key and its
permissions are unchanged — nothing needs to be reissued.

I have attached the updated developer guide (v1.2), which reflects the new endpoint throughout. Happy
to jump on a call if it is easier to walk through.

Best regards,
[SIGN-OFF]

---

## Internal notes — not part of the email

- **Sequencing.** This should reach Amazon *before* DNS cutover, not with it. If they are polling
  production on a schedule, an unannounced host change reads to them as an outage.
- **Not a contract change.** The frozen `/api/v1/certificates*` shape is untouched, so this does not
  need partner sign-off in the way a payload change would. It does need their acknowledgement that
  they have reconfigured, since the old address will not resolve.
- **Deliberately not in this note:** the exposed production API key
  (`gcc_partner_fc8498…`, printed in the v1.0/v1.1 guide PDFs and still live). Rotation is a separate
  conversation and a separate note — bundling it here would muddle a routine config change with a
  credential incident.
  **Decided (Shawn, 2026-08-14): rotate after cutover.** Draft and runbook in
  `2026-08-14-amazon-key-rotation-note.md`. Because a partner can hold two active keys at once, that
  rotation is zero-downtime and can be scheduled at Amazon's convenience — so nothing about it needs
  to compete with the cutover.
- **Suggested ask back:** a one-line confirmation from Amazon once their configuration is updated, so
  the cutover has a known-good state on both sides.
