# Client email — migration complete, workbook guide, and decisions needed

**Status:** Draft for Shawn to send
**Date drafted:** 2026-08-18
**Attach:** `2026-08-18-migration-verification-workbook.xlsx`
**Supersedes:** the CR-038-only draft (`2026-08-18-client-email-CR038-decisions.md`) — the decisions
from that draft are folded in here as section 4, so send this one instead of both.

> ## ⚠️ READ BEFORE SENDING — two of the draft bullets are not true on the test site
>
> **1. 2FA by email does not work on gccwebapp.com today, and enrolling in it would lock a user
> out.** `SmtpSettings.Host` and `Username` are **empty** in the test environment's configuration.
> `Program.cs:541` falls back to `MockEmailService` when they are, and that class only writes the
> message to the log (`Services/EmailService.cs:182`) — **nothing is delivered.** So a user who
> switches on email 2FA will be asked for a code that never arrives. This is DEF-263.
>
> **2. Password reset has the same problem.** `ForgotPassword` generates a valid reset link and
> hands it to the same mock service (`AccountController.cs:441`), so the email never arrives.
>
> Worse: `MockEmailService.TestConnectionAsync` **always returns true**, so any "test email
> connection" button reports success. Do not treat a green test there as evidence.
>
> The bullets below are written truthfully. If you would rather not raise it in this email, delete
> those two bullets entirely rather than softening them — but the client will find it the first time
> someone forgets a password, and SMTP needs configuring before go-live regardless.

---

**Subject:** Database migration complete — your data is on the test site for review

Hi [NAME],

Thank you for your patience. **The database migration has been completed.** Your full certificate
history is now on the test site and ready for you to review.

I have attached a workbook that lays out every figure we checked, so you can verify the numbers
yourself rather than take our word for them. There is a guide to it below, and four decisions we
need from you at the end.

## 1. Status

**Deployment** — included the changes that allow historic certificates sharing the same certificate
number to display in the application. **137 of your certificate numbers are each used by more than
one certificate**, 312 records in total, and they could not have been loaded without this.

**User logins** — the test accounts we created during build and testing have been removed. **Five
logins remain:**

| Login | Purpose |
|---|---|
| `carly@greencirclecertified.com` | your administrator |
| `analyst@greencirclecertified.com` | analyst |
| `admin@greencirclecertified.com` | administrator |
| `partner@amazon.com` | Amazon partner portal login |
| `swest@anukiconsulting.com` | required by the system to record changes |

If anyone else on your side had a login to the test site, please tell us now and we will recreate
it. **Everything else we removed was our own test scaffolding.**

**Data migration** — certificates have been loaded as follows:

| | |
|---|---|
| Certificates | **5,702** across **5,527** certificate numbers |
| Companies | **301** |
| Products | **3,439** |
| Product identifiers (ASINs) | **16,234** |
| Certificate documents attached | **5,474** |
| Records that could not be loaded | **29** — explained in section 3 |

Every certificate and every ASIN in your workbooks is accounted for **in both directions** — nothing
missing that should be there, and nothing in the system that is not in your files. We also checked
the detail carried on each ASIN, not just that the right ASINs are attached to the right
certificates: **all 16,234 product names and all 16,234 partner tags match your files exactly.**

**Data retained in the system** — everything you asked us to keep survived untouched, and we
re-checked each one after the load: certification types, product categories and types, tags,
pathways, partners, **all API keys including Amazon's**, roles and permissions, and your Google
Analytics settings. **The Amazon feed was tested specifically after the migration and is unchanged**,
so there is nothing for Amazon to re-test.

**Two-factor authentication** — the application supports **email-based 2FA only**. SMS is not set up,
and Duo is not integrated. One thing to be aware of before anyone switches it on: **the test site
cannot send email yet** — the mail server settings are not configured on that environment, so a
verification code would not arrive. We need to configure that before 2FA can be used or tested, and
in the meantime please do not enable it on an account, as it would lock that account out.

**Password reset** — the self-service "forgot password" flow sends a reset link by email, so it is
affected by the same mail-server gap above. Until that is configured, **contact us and we will set a
password for you directly.** Configuring mail is a small piece of work and we would suggest doing it
before go-live so both of these work normally.

**Certificate history** — one thing worth knowing rather than discovering: the migrated certificates
arrive **with no change history**. The data was loaded directly into the database rather than typed
through the application, so there is no "imported by / imported on" entry against each record.
History builds up from your first edit onwards. That is normal for a migration of this size.

## 2. The attached workbook — what you are looking at

Start on the **Read Me** tab. The two worth your time are **Summary** and **Not Loaded**.

| Tab | What it shows |
|---|---|
| **Summary** | Every check we ran, one per row |
| **Source vs Database** | For each worksheet you sent us: how many rows it held, and how many loaded |
| **Field Reconciliation** | Each data field, and how many values matched |
| **Mismatches** | Every single difference we found, with the reason |
| **Not Loaded (29)** | The 29 records that could not load, and what is missing from each |
| **Duplicate Numbers** | All 137 numbers used by more than one certificate |
| **ASINs** | Product identifiers checked in both directions |
| **Documents** | Certificate documents and previews |
| **Product ID Fields** | The product name and partner tag on each ASIN, checked against your files |
| **Name Conflicts** | ASINs your list holds more than once with a different product name each time |
| **Blank Product Names** | Product identifiers with no product name |
| **Status Breakdown** | How many certificates sit at each status |
| **Audit History** | Anything the system has changed since the load |
| **Certificates** | All 5,702 records, if you want to spot-check individual entries |

**Reading the Summary tab:**

| Column | Meaning |
|---|---|
| **Group** | Which part of the migration the check belongs to |
| **Check** | What was being verified |
| **Expected** | What the figure should be, taken from your source files |
| **Actual** | What is genuinely in the system now |
| **Verdict** | **PASS** = matched exactly · **REVIEW** = needs your decision, not a fault |
| **Note** | Any explanation the number needs |
| **Source of truth** | Where the figure came from, so any number can be traced back |

**Everything reads PASS except two items marked REVIEW**, and neither is an error — they are the
untagged product identifiers and the automatic status updates, both covered below.

**Reading the Mismatches tab** — this is where we would expect you to be most sceptical, so it lists
every difference individually rather than summarising:

- **Country, 9 records** — your file says `The Netherlands`, the system stores `Netherlands`. The
  system standardises country names so filtering and reporting work consistently.
- **Status, 3 records** — your file says `Client Approved - Paid`, the system says `Expired`. These
  three certificates **expired on 14–15 August**, after your spreadsheet was produced. The system
  re-checks expiry dates daily and updated them. **The system is more current than the file, not
  wrong** — you can confirm this yourself by checking those certificates' expiration dates on the
  Certificates tab.

That second point will grow over time as more certificates reach their expiry date. It is the
system working correctly, and the **Audit History** tab records every such change with a reason.

## 3. The 29 records that could not be loaded

These are **not** a migration failure — they are records in your source files that are missing
information the system requires. We did not want to invent values, so we held them back and listed
them for you. The full list is on the **Not Loaded (29)** tab, with the certificate number, company,
product and the specific field missing from each.

| What is missing | Records |
|---|---|
| No expiration date | **13** |
| No product name | **11** |
| No product name, no effective date and no expiration date | **3** |
| No effective date | **2** |

**27 came from `Expired List 1 - 4.9.26` and 2 from `Active List 7.31.26`.**

Every certificate needs a product name and a validity period — without an expiration date the system
cannot tell whether a certificate is current, and it drives whether the certificate appears publicly.

**Two of the 29 are currently approved and public** — `23-0358` and `25-0351`, both Sofidel America.
Those two are worth attention first, since they are live records rather than history.

## 4. Decisions we need from you

| # | Decision | Why it matters |
|---|---|---|
| **1** | **The 29 records above** — can you supply the missing values, or should they be left out permanently? | They are the only records from your files not on the test site. |
| **2** | **Four certificates carry no partner at all, and so are not shared with any partner.** They are 23-0814 (P&G) and Kohler 25-8535, 26-0053 and 26-0101, and between them they hold **1,960 product identifiers (ASINs)** — 12% of everything loaded. Because no partner is set on the certificate, **Amazon does not receive these certificates or their ASINs.** Is that correct, or should they be associated with Amazon? | If these were meant to reach Amazon, they are not doing so today. |
| **3** | **Certificate `24-1634`** — your files give two different certification results for it, 0.63 and 0.23. We loaded **0.63**, which matches the certificate listing itself, but the certificate is publicly visible so we would like it confirmed. | A public record either way. |
| **4** | **Duplicate certificate numbers — please confirm our approach.** We loaded them as separate certificates, keeping every record and every document exactly as issued. There is one consequence you should know: where several certificates share a number, **a link built from that number opens only one of them**. For example `26-0182` is held by four Regupol America certificates — different products, each with its own signed document — and only one is reachable by that number. All four appear in the certificate listing. **8 shared numbers are currently public, covering 20 certificates.** Is that acceptable, or would you like the public site to indicate when a number is shared? | Affects what a visitor sees and what a shared link opens. |

## 5. Two smaller points on the ASIN list

Neither is a fault, and neither blocks anything — but both are your call rather than ours.

**a) 1,096 ASINs are listed more than once with a different product name each time.** 1,564 rows in
total, across 30 certificates. They look like the same product captured in different pulls — a long
Amazon-style title in one row and a shorter internal name in another, or one with a
"(Packaging May Vary)" prefix and one without. For example the same ASIN appears as both
*"DUDE Wipes Flushable Wipes - 1 Pack, 48 Wipes - Unscented Extra-Large…"* and
*"DUDE Wipes 48ct Fragrance Free"*.

We kept the **first** name for each, which is why the count above reconciles exactly. But since the
product name is one of the fields passed to Amazon, **which of the two Amazon sees is currently
decided by the row order in your spreadsheet**, and that is arbitrary. Is there a rule you would
like applied — always the longer Amazon-style title, always the most recent, or something else?
Every pair is listed on the **Name Conflicts** tab so you can see exactly what would change.

**b) 11 product identifiers have no product name.** Ten sit on `25-9301` and one on `23-0812`. They
are **blank in your source file as well**, so nothing was lost in the migration — but the ASINs will
show without a product name until values are supplied. Listed on the **Blank Product Names** tab.

## 6. Two remaining points on duplicate numbers

We can settle these whenever suits: **`21-0179` and `21-0180`** each carry two unrelated companies
and look like errors in the source rather than genuine shared numbers; and **9 numbers could safely
be merged** (8 where the same document was filed twice, 1 identical on every field) if you would
prefer fewer duplicate records.

Happy to walk through the workbook on a call if that is easier than email — decisions 1 and 2 in
particular may be quicker to resolve live.

Best,
[SIGNATURE]
