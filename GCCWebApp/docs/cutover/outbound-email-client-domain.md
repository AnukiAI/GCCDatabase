# Cutover step — sending application email from the client's domain

**Status: PREPARED, NOT EXECUTED.** Compiled 2026-09-15; every DNS value below resolved live.
**Goal:** move the app's outbound mail from `admin@gccwebapp.com` to an address on
`greencirclecertified.com`, without touching the client's production Microsoft 365 mail.
**Companion to:** `database.greencirclecertified.com-runbook.md` (the web-hostname cutover).

---

## The two findings that shape this

### 1. The client domain enforces DMARC at `p=quarantine`

```
_dmarc.greencirclecertified.com  →  v=DMARC1; p=quarantine; rua=mailto:security@pointsolve.com
```

There is **no `sp=` tag**, so subdomains inherit that policy. This is the binding constraint and it
turns SPF/DKIM from good practice into a hard requirement: **mail sent as
`@db.greencirclecertified.com` that does not pass DMARC goes to the junk folder**, silently, with no
error visible to the app. Password resets and 2FA codes would simply "not arrive".

Alignment is *relaxed* (no `adkim`/`aspf` tags), so a subdomain sender aligns with the
organisational domain — `db.greencirclecertified.com` satisfies alignment against
`greencirclecertified.com`. That is what makes the subdomain approach workable.

Failure reports already go to an address the client monitors, so a misconfiguration will be visible
to them, not just to us.

### 2. The root SPF record has only three DNS lookups of headroom

```
greencirclecertified.com TXT  →  v=spf1 ip4:107.20.88.125 include:zcsend.net
                                 include:spf.constantcontact.com
                                 include:spf.protection.outlook.com
                                 include:one.zoho.com ~all
```

| Mechanism | Lookups |
|---|---|
| `include:zcsend.net` | 1 |
| `include:spf.constantcontact.com` | 1 |
| `include:spf.protection.outlook.com` | 1 |
| `include:one.zoho.com` | **4** (it nests three further includes) |
| **Total** | **7 of the 10 permitted** |

**Exceeding 10 is a `permerror`, which fails SPF for the entire domain — Microsoft 365 mail
included.** So if anything is ever added to the *root* record, add it as **`ip4:72.18.135.96`**
(zero lookups), never as an `include:`. The plan below avoids touching the root record at all.

---

## Decision: a subdomain sender

`noreply@db.greencirclecertified.com` (use `dbtest.` while pre-launch).

Its SPF and DKIM live on the subdomain, so the root SPF record, the client's M365 configuration and
their live mail are **untouched**. Nothing in this procedure can break the client's real email, which
is not true of the root-domain alternative. Root-domain sending needs their M365 admin, consumes root
SPF headroom, and buys only cosmetics.

---

## Step 0 — the ten-minute test that may end this whole exercise

**Do this before touching DNS or Plesk.**

The app authenticates to AccuWeb's SMTP as `admin@gccwebapp.com`. Many SMTP servers refuse to send a
`From:` outside the authenticated mailbox's domain, or silently rewrite it. If `accumail3` does
either, no amount of DNS work will help and the answer is "keep the gccwebapp sender, change
`FromName` only".

1. On the **test** site, set `SmtpSettings:FromEmail` to `noreply@db.greencirclecertified.com`.
2. Trigger one password reset to **`shawnmwest@gmail.com`** — the plain address, no `+tag`. It is the
   only mailbox that actually receives (see the Verify step).
3. Read the result:

| Outcome | Meaning |
|---|---|
| Message arrives | The server permits a cross-domain `From`. Continue to Step 1. |
| SMTP rejects (5xx on send) | The server enforces sender identity. **Stop** — this approach is closed. |
| Arrives but `From` has been rewritten | The server rewrites. **Stop** — same conclusion. |

4. Put `FromEmail` back to `admin@gccwebapp.com` afterwards either way.

---

## Step 1 — Plesk: create the mailbox

The address has to exist before anything points at it. **Verified 2026-08-19 by SMTP RCPT probe:
`admin@gccwebapp.com` is the only mailbox on this server** — `noreply@`, `no-reply@`, `support@` and
`info@` all return `550 No such user here`, and an invented control address is rejected too, so the
server does real per-user validation. Setting `FromEmail` to an address that does not exist invites
rejection at the server and bounced replies.

1. Plesk → **Mail** → add the mail domain for `db.greencirclecertified.com`.
   **Uncheck DNS zone management** — the zone is at HostMonster, not Plesk.
2. Create the mailbox `noreply@db.greencirclecertified.com` (a real mailbox, not a forward-only
   alias, so SMTP AUTH can use it if you later switch the app to authenticate as it).
3. Plesk → **Mail Settings** → enable **DKIM signing** for that mail domain. Plesk generates the key
   pair and shows the public key to publish in Step 2.

> **Decide now: can people reply?** A send-only subdomain with no MX means every reply hard-bounces.
> Either publish an MX and monitor the mailbox, or accept it — but note the application has **no
> `ReplyTo` support** (`Services/EmailService.cs` handles only `FromEmail`/`FromName`), so you cannot
> currently point replies elsewhere without a small code change.

---

## Step 2 — HostMonster: publish the DNS records

All records are on the **subdomain**. The root `greencirclecertified.com` zone is not modified.

| Host | Type | Value | Why |
|---|---|---|---|
| `db.greencirclecertified.com` | TXT | `v=spf1 ip4:72.18.135.96 -all` | Authorises the sending server. `ip4:` costs **zero** DNS lookups |
| `default._domainkey.db.greencirclecertified.com` | TXT | *(the public key Plesk generated)* | DKIM — required, because DMARC is `p=quarantine` |
| `db.greencirclecertified.com` | MX | `10 mail.gccwebapp.com` | **Only if replies must be received.** Omit for send-only |

`72.18.135.96` is the sending host: `accumail3.accuwebhosting.com` and `mail.gccwebapp.com` both
resolve to it (verified 2026-09-15).

**Do not add a `_dmarc.db...` record.** The subdomain inherits the root policy, which is what you
want — adding a weaker one at the subdomain would quietly loosen enforcement.

---

## Step 3 — Application configuration

`SmtpSettings:FromEmail` and `FromName` live in `appsettings.Production.json` (lines 157–158). They
are **not secret** and belong there, not in the secrets overlay.

```json
"FromEmail": "noreply@db.greencirclecertified.com",
"FromName":  "GreenCircle Certified Database"
```

Leave **Host, Port and Security alone** — both are load-bearing and easy to "correct" into a broken
state:

- Host must be `accumail3.accuwebhosting.com`, **not** `mail.gccwebapp.com`. Both resolve to the same
  IP, but the server's TLS certificate only covers the AccuWeb name, so the friendlier host fails
  certificate validation before it ever authenticates.
- Port must be **465** with `Security=SslOnConnect`. Port 587 advertises no STARTTLS and answers an
  explicit STARTTLS with `503 TLS is not allowed`.

`Username`/`Password` stay in the server-only `appsettings.Secrets.json` overlay and continue to
authenticate as `admin@gccwebapp.com` unless Step 0 showed you must switch.

> **This fails closed and silently.** Empty SMTP credentials make `Program.cs` fall back to
> `MockEmailService`, which logs the message and delivers nothing. After any deploy, read the
> `Email:` banner in stdout and send a real password reset — this is Gate 12 of the pre-deploy
> checklist, and it exists precisely because a broken mail config produces no error anywhere.

---

## Step 4 — Verify, on headers not on vibes

Send a real password reset to **`shawnmwest@gmail.com`** — the plain address.

> **`+tag` addresses do not receive mail.** Seven of the eight test accounts use Gmail subaddressing
> (`shawnmwest+analyst@gmail.com` and so on) purely to satisfy the app's uniqueness requirement;
> **none of them can receive anything.** `2fatest` / `shawnmwest@gmail.com` is the only account that
> can. Do not "fix" a failed mail test by pointing it at a `+tag` address.

Open the message, show original, and require **all three**:

```
spf=pass     ... smtp.mailfrom=db.greencirclecertified.com
dkim=pass    ... header.d=db.greencirclecertified.com
dmarc=pass   ... header.from=db.greencirclecertified.com
```

`dmarc=pass` is the one that matters. With `p=quarantine`, `dmarc=fail` means the junk folder — and
the app will report the send as successful.

Check the junk folder explicitly before declaring success. "It arrived" is not the same as "it
arrived in the inbox".

---

## Rollback

Set `FromEmail` back to `admin@gccwebapp.com` and recycle the app pool. That is the whole procedure —
the DNS records are additive and harmless once nothing sends from the subdomain, and the root zone
was never touched.

---

## The cheap alternative, if Step 0 closes the door

`FromName` is already `"GCC Database System"`, and most mail clients display the name rather than the
address. Changing it to `"GreenCircle Certified Database"` costs one config line, needs no DNS, no
Plesk work and no mail-admin coordination — and changes what almost every recipient actually sees.

If the objection to `admin@gccwebapp.com` is that recipients see the word "gccwebapp", this solves
most of it for a fraction of the effort. Worth confirming which problem is being solved before
spending the rest.

---

## Effort

| Step | Estimate |
|---|---|
| 0 — cross-domain `From` test | ~15 min, and may end the exercise |
| 1 — Plesk mail domain, mailbox, DKIM | ~30 min |
| 2 — HostMonster DNS (client-held login) | ~15 min plus their turnaround |
| 3 — config + deploy | within an existing deploy |
| 4 — header verification | ~15 min |

**Not started. No DNS, Plesk or config changes have been made.**
