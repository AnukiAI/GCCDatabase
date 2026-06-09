# CR-171 — Client email draft (2026-06-04)

> Draft for Shawn to send/edit. Plain-language summary of CR-171 + the 5 sign-off questions. Detail/matrix lives in `CR-171-multipartner-visibility.md`.

---

**Subject:** Quick decisions needed — multi-partner certificates (QC #171)

Hi Carly,

Picking up QC #171 — *"Partner must be multi-select if used for partner login access."* We've scoped it and want your input before we build, because it's a small scope change with a few decisions that are really yours to make.

**What's changing**

Today a certificate can be tied to **one** partner. You've asked for a certificate to be able to carry **more than one** partner (e.g. both Amazon and Novi on the same cert). We can do that — and the important part is how visibility works once a cert has multiple partners:

- Each partner login will see **only the certs tied to them** (unchanged).
- When a cert is shared between, say, **Amazon and Novi**, each partner will see the cert but will **only ever see their own name on it** — Amazon will not see that Novi is also attached, and vice-versa. Nothing leaks between partners.

That last point is the heart of it: a shared certificate, but each partner's view stays private to them.

**Five questions before we build**

1. **Public view** — for anyone browsing the public certificate list (no login), should a partner name be shown at all? Our recommendation is **no** — keep partner associations out of the public view entirely.

2. **Company users** — when one of your customer/company users views *their own* certificate that happens to be tied to multiple partners, should they see all the partner names on it, or none? (They own the cert, but the partner tags may be commercially sensitive.)

3. **Is a partner required?** — should every certificate now require at least one partner, or is "no partner" still a valid option?

4. **Bulk upload** — do you need to assign multiple partners through the **spreadsheet/CSV import** as well, or is it enough for now to do it on the certificate **create/edit screen**? (Doing it on the screen first keeps this smaller; we can add import later if needed.)

5. **Amazon** — the data we send to Amazon through the API stays in exactly the same format; the only difference is Amazon will see only their own partner tag, never the others. We'd want Amazon (via Kyle) to confirm they're fine receiving a certificate that may be shared, while still only seeing their own association.

**Effort / next step**

This is a medium-sized change (roughly a day or two of work, including a one-time data update so existing certificates keep showing correctly to partner logins). It's logged as **CR-171** and is **on hold pending your answers** to the five questions above — once you confirm, we'll lock scope and come back with timing.

Happy to jump on a quick call if that's easier than email.

Thanks,
Shawn
Anuki Consulting
