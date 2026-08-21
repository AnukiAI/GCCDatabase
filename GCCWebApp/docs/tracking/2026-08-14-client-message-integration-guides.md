# Message to client — integration guides updated for subdomain

**Status:** Draft for Shawn to send
**Date drafted:** 2026-08-14
**Attach:** the two PDFs and the Word template listed under *What's attached*

---

**Subject:** Updated API integration guides — production moves to a subdomain

Hi [NAME],

Both API integration guides are updated and attached, ready to go out.

**The production API will run on its own subdomain: `database.greencirclecertified.com`.** The base
URL becomes `https://database.greencirclecertified.com/api/v1`, replacing the
`greencirclecertified.com/database` path in the guides you have now. The driver is hosting — the
database application can't sit on the same server as the WordPress site, so it needs its own host.
That also gives it its own certificate and its own scaling path, independent of the website.

Nothing about the API itself changes. Paths, parameters, headers, field names and response shapes are
all identical, so for Amazon this is one configuration value, not an integration change.

**What's attached**

| File | What it is |
|---|---|
| `GCC Database Partner API - Amazon Developer Guide v1.2.pdf` | For Amazon. Updated throughout, and it opens with the endpoint change so it's the first thing they read |
| `GCC Database Partner API - Partner Integration Guide v1.1 TEMPLATE.pdf` | The generic partner guide, for your reference |
| `GCC_Partner_API_Integration_Guide_TEMPLATE.docx` | The same generic guide in Word, so you can fill it in for future partners without coming back to us |

The Word template has every partner-specific value marked as a highlighted placeholder — partner
name, rate limit, key scope, expiry. The base URL is no longer a placeholder, because it's the same
for every partner.

**Two things worth flagging**

First, **the previous version of the Amazon guide had a live API key printed in it.** It's the
production key currently in use, and it went out in the circulated PDFs. I've replaced it with a
placeholder in v1.2, but the key itself still needs rotating — a document change doesn't undo the
exposure. I'd suggest doing that after the endpoint cutover rather than alongside it, so Amazon isn't
handling two changes at once. Because a partner can hold two active keys at the same time, the
rotation itself is seamless for them: we issue the new key, both work during the changeover, and we
retire the old one once they confirm. I have that note drafted and ready when you want it.

Second, **the `/database` path wouldn't have worked as documented anyway.** The API builds some of its
response identifiers from the address it's called on, and behind a path-based setup those would have
come back without the `/database` segment — contradicting the guide. The subdomain removes that
mismatch rather than introducing one, and it needs no code change.

**What I need from you**

1. Confirm the subdomain and let me know the DNS cutover date.
2. Approve the Amazon note so it reaches them ahead of cutover — an unannounced host change would look
   like an outage from their side. Draft is ready.
3. Confirm you're happy to rotate the key after cutover, and I'll send that note through.

One item still open on our side: a fix to how the API applies partner rate limits is complete but not
yet deployed to production. It needs to go live before the guides are issued, otherwise the 200
requests/hour figure in them isn't what the server actually enforces. That's tracked and will go out
with the next deployment.

Happy to walk through any of it on a call.

Best regards,
[SIGN-OFF]
