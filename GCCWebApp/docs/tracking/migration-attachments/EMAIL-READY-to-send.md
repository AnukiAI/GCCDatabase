# SEND-READY — Migration client decisions email

> Copy the body below into the email. **One fill-in before sending:** replace `[Client name]`.
> **Attach:** `item4-orphan-asins.csv` and `item7-duplicate-expired-certs.csv` (both in this folder).
> Sender: Shawn.

---

**To:** [Client name]
**Subject:** Certificate data migration — the remaining decisions we need from you
**Attachments:** item4-orphan-asins.csv, item7-duplicate-expired-certs.csv

---

Hi [Client name],

We're close on prepping your certificate data for migration. A handful of items need your call before we load everything. I've put my recommendation next to each — if you're happy with them all, just reply **"go with your recommendations"** and we'll run with it. The first two affect the Amazon badge, so those are the priority if you're short on time.

**1. Amazon code for "Certified Environmental Facts"**
Your ASIN list uses the code **greencircle_cef** for these certificates. Our Amazon developer guide only documented examples for Recycled Content and Sustainable Energy Practices, so a code for Certified Environmental Facts was never formally listed with Amazon (and isn't set in our system yet).
→ *Can you confirm greencircle_cef is the code Amazon expects for CEF?* Once you confirm, we'll set it in the system and add it to the Amazon developer guide so it's documented going forward.

**2. A few very long product names**
About 88 of the product names in the ASIN list are longer than our current limit (the longest is ~950 characters). Since the product name is passed to Amazon, I don't want to trim them and change what Amazon sees.
→ *My recommendation:* keep the full names exactly as you provided them (we've widened the field to fit). **OK to store the full names?** (If they're only for internal display and a shorter version is fine, say so and we'll trim instead.)

**3. A few older certificate types not in the current system**
About 86 expired certificates use types that aren't in the current system: **Biodegradable, Biobased, Renewable Resource Content,** and **Verified Health Product Declaration** (all on expired records).
→ *What we'll do:* where a value in your data isn't in our current system list, we'll bring the certificate in with that field marked **"Unspecified"** rather than guessing at a substitute or dropping the record — every other detail comes in normally, and "Unspecified" is kept off the list of choices for new certificates. For these, their certification type will show as **"Unspecified"** on those expired records. **Please confirm you're OK with that.**

**4. Product IDs that point to a missing certificate**
Every product identifier (ASIN, etc.) needs to be tied to a certificate. In the ASIN list, **51 ASINs** reference certificates **25-0437** (29 ASINs) and **25-0438** (22 ASINs) — both X-Sense smoke/CO alarm products — but neither certificate appears in the Active or Expired listings you sent, so there's no certificate for those ASINs to attach to. (Full list of the 51 attached: *item4-orphan-asins.csv*.)
→ *My recommendation:* hold those 51 ASINs aside until you can send the two certificate records (they may just be newer than the 4/9 export). **Can you send certificates 25-0437 and 25-0438, or should we leave those ASINs out?**

**5. Two different results on one certificate**
Certificate **24-1634** (Philips) shows two different certification results in the ASIN list — **0.23** and **0.63**. We can only carry one. For context: most entries show **0.63** with a 2024 issue date, while a smaller set shows **0.23** with a 2023 date — which looks like the earlier certificate period, but I'd rather confirm than assume.
→ *Which is the correct result for this certificate?*

**6. "Block Inc." vs "Square"**
Certificates **25-0479** and **25-0482** are listed under both "Block Inc." and "Square" — the same company under its old and new names. Loading both as-is would create two separate company records.
→ *My recommendation:* use the current name **"Block, Inc."** for both. **Good with that, or would you prefer "Square"?**

**7. Some older certificates share a certificate number**
On the expired list, **114 certificate numbers** appear on more than one record (with slightly different details — mostly small spelling/casing differences in the product name, and a few with a slightly different expiration date). These are all expired/historical, and our new system needs each number to be unique.
→ *My recommendation:* for each, we keep the most complete record and set the duplicate aside — nothing is deleted without your sign-off. I've attached the full list so you can spot-check (*item7-duplicate-expired-certs.csv*). **OK to proceed that way, or would you rather review them first?**

Thanks!
Shawn
