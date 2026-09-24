# Cutover step — sending application email from the client's domain

> ### ✅ LIVE 2026-09-22 (go-live night) — sender is `noreply@db.greencirclecertified.com`, SPF only
>
> - `FromEmail` is `noreply@db.greencirclecertified.com`, set in **two** places in the repo:
>   `web.config` env var `SmtpSettings__FromEmail` (wins — env vars load last) and, since 2026-09-22,
>   `appsettings.Production.json` (+ `.template.json`). SMTP login stays `admin@gccwebapp.com` in the
>   server-only Secrets overlay. Until SPF existed this sent "successfully" per the log and was
>   dropped by Gmail under the parent's DMARC `p=quarantine`.
> - Blocker B2 cleared at go-live (`db` is now an A record). Published at HostMonster, Name `db`:
>   `TXT v=spf1 ip4:72.18.135.96 ip4:64.27.52.66 -all` (TTL 300) — verified on ns1, ns2 ×3 and 8.8.8.8.
> - The app sets no separate envelope sender (`EmailService.cs`), so MAIL FROM = From = `db.` →
>   SPF passes **and aligns**, which alone satisfies DMARC. Test reset 23:06 → **arrived in Inbox**.
> - **Still open:** DKIM (B3, unproven on `accumail3` — Steps 1a/1b), and no MX, so bounces/replies to
>   `noreply@` are lost. Neither blocks delivery. Header check (`Show original`) not yet recorded.
> - **Rollback:** set `SmtpSettings__FromEmail` in the server's `web.config` to `admin@gccwebapp.com`
>   and recycle. Deleting that line is **not** enough — `appsettings.Production.json` now also says
>   `noreply@db`. **Never delete the `db` SPF TXT while this sender is in use.**
> - The test site (`test.gccwebapp.com`) gets the same `web.config`, so it also sends as `noreply@db`
>   — covered by the same SPF record (same sending host).
>
> Everything below is the original plan, kept for the DKIM follow-up.

**Status (original, superseded above): PREPARED, NOT EXECUTED — BLOCKED until go-live.** Compiled 2026-09-15; every DNS value below resolved live.
**Revised 2026-09-22** after a first attempt stalled at Step 1 — see *"Blockers found 2026-09-22"* below.
Scheduled as **Phase 2 (late Oct – Nov)** in `go-live-plan-2026-09-21.md`; nothing here gates cutover.
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

## Blockers found 2026-09-22

The original Step 1 and Step 2 were written before `db.` became a Plesk **domain alias** (runbook
step 3b) and assumed DNS we do not yet control. A first attempt stalled at Step 1: `db` does not
appear under Plesk → Mail → Mail Settings. Three findings, all checked live that day:

| # | Finding | Evidence | Consequence |
|---|---|---|---|
| B1 | `db.greencirclecertified.com` is a **domain alias** of the `gccwebapp.com` subscription, not a domain | Plesk Mail Settings lists only `gccwebapp.com` and `test.gccwebapp.com`; Plesk's resolve warning names `db.` as a hosted name | Aliases are never listed in Mail Settings, and Plesk will not accept `db.` as a separate mail domain while it is an alias. The old Step 1 ("add the mail domain") **cannot be done**. |
| B2 | `db.greencirclecertified.com` is currently a **CNAME to Vercel** | `Resolve-DnsName db.greencirclecertified.com -Type TXT` returns a CNAME | DNS forbids any other record at a CNAME name, so the **SPF TXT and MX in Step 2 cannot be published until the go-live A-record flip** replaces the CNAME. The DKIM record is at a different name (`default._domainkey.db…`) and is not blocked by this. |
| B3 | **DKIM has never been live on this mail setup** — not even for `gccwebapp.com` | `default._domainkey.gccwebapp.com` → *does not exist*; only the Plesk placeholder `_domainkey.gccwebapp.com TXT "o=-"` | The app sends via `accumail3` (`72.18.135.96`), a different host from the web server (`64.27.52.66`). It is **unproven** that the Plesk DKIM toggle makes that host sign. Prove it on `gccwebapp.com` (Step 1a) before building on it. |

**Resume only after go-live**, when B2 clears itself. Do Step 0 and Step 1a first; if either fails,
fall back to *The cheap alternative* at the end of this document.

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

## Step 1a — prove Plesk DKIM signs at all (on `gccwebapp.com`)

Blocker B3. ~15 min, and it de-risks everything after it.

1. Plesk → **Mail** → **Mail Settings** → click **`gccwebapp.com`** → tick **"Use DKIM spam protection
   system to sign outgoing email messages"** → OK.
2. Plesk shows the DNS record it expects (name `default._domainkey.gccwebapp.com`, value
   `v=DKIM1; k=rsa; p=…`). Publish it wherever the **`gccwebapp.com`** zone is hosted.
3. Send a real password reset to `shawnmwest@gmail.com` → *Show original* → require
   `dkim=pass header.d=gccwebapp.com`.

| Outcome | Meaning |
|---|---|
| `dkim=pass` | `accumail3` signs with the Plesk key. Continue to Step 1b. |
| No `DKIM-Signature` header at all | The sending host does not use Plesk's key. **Stop** — raise an AccuWeb ticket or take the cheap alternative. |

This is also an improvement in its own right: `gccwebapp.com` mail currently goes out unsigned.

## Step 1b — Plesk: enable mail on the `db.` alias

The address has to exist before anything points at it. **Verified 2026-08-19 by SMTP RCPT probe:
`admin@gccwebapp.com` is the only mailbox on this server** — `noreply@`, `no-reply@`, `support@` and
`info@` all return `550 No such user here`, and an invented control address is rejected too, so the
server does real per-user validation. Setting `FromEmail` to an address that does not exist invites
rejection at the server and bounced replies.

`db.` is a domain alias (blocker B1), so it has **no mailboxes of its own** — an alias with mail
enabled maps `anything@db.greencirclecertified.com` onto the primary domain's mailbox of the same
name. Do **not** try to add `db.` as a separate mail domain or a second site; that would break the
alias the web cutover depends on.

1. Plesk → **Mail** → **Email Addresses** → create `noreply@gccwebapp.com` (a real mailbox, not a
   forward-only alias, so SMTP AUTH can use it if you later switch the app to authenticate as it).
   Via the alias, this is what receives `noreply@db.greencirclecertified.com`.
2. Plesk → **Websites & Domains** → `gccwebapp.com` → **Domain Aliases** → `db.greencirclecertified.com`
   → tick **Mail service** → OK. **Leave Web service ticked** — the cutover needs it.
3. Plesk → **Mail Settings** → confirm DKIM signing is on for `gccwebapp.com` (Step 1a). Find the DKIM
   record Plesk expects **for the alias** — `default._domainkey.db.greencirclecertified.com`.
   **Unverified:** whether this Plesk version signs alias mail with `d=db.greencirclecertified.com` or
   shows a separate record for it. If Plesk offers no DKIM record for the alias, stop and check with
   AccuWeb before publishing anything — a record for a key nothing signs with achieves nothing.

> **Decide now: can people reply?** A send-only subdomain with no MX means every reply hard-bounces.
> Either publish an MX and monitor the mailbox, or accept it — but note the application has **no
> `ReplyTo` support** (`Services/EmailService.cs` handles only `FromEmail`/`FromName`), so you cannot
> currently point replies elsewhere without a small code change.

---

## Step 2 — HostMonster: publish the DNS records

All records are on the **subdomain**. The root `greencirclecertified.com` zone is not modified.

> **Precondition — blocker B2.** `db.` must already be an **A record** (the go-live flip), not the
> current CNAME to Vercel. Check before starting:
> `Resolve-DnsName db.greencirclecertified.com -Type A -Server 8.8.8.8` → must return an A record,
> not a CNAME. While it is a CNAME the zone editor will reject the SPF TXT and the MX outright.

| Host | Type | Value | Why | Blocked by CNAME? |
|---|---|---|---|---|
| `db.greencirclecertified.com` | TXT | `v=spf1 ip4:72.18.135.96 -all` | Authorises the sending server. `ip4:` costs **zero** DNS lookups | **Yes** |
| `default._domainkey.db.greencirclecertified.com` | TXT | *(the public key from Step 1b)* | DKIM — required, because DMARC is `p=quarantine` | No |
| `db.greencirclecertified.com` | MX | `10 mail.gccwebapp.com` | **Only if replies must be received.** Omit for send-only | **Yes** |

**Entering them in the HostMonster zone editor** (cPanel → Zone Editor → Manage
`greencirclecertified.com` → + Add Record):

- **Name** is relative — type `default._domainkey.db`, and cPanel appends the zone. If you type the
  full name, end it with a dot (`default._domainkey.db.greencirclecertified.com.`).
- **TTL** `3600`.
- **Value** is `v=DKIM1; k=rsa; p=…` on **one line, no quotes**. If Plesk shows the key split into
  several quoted `"…" "…"` pieces, join them. A line break copied out of the browser breaks the key.
- The selector must be **`default`** — the one Plesk signs with.
- If the editor refuses to save with a TTL/validation error, that is the zone-editor deadlock in
  `STEP-0-DEADLOCK-2026-09-21.md`, not a DKIM problem.

Check each record from outside once saved:
`nslookup -type=TXT default._domainkey.db.greencirclecertified.com 8.8.8.8`

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
| 1a — prove DKIM on `gccwebapp.com` | ~15 min, and may end the exercise |
| 1b — mailbox + mail on the `db.` alias | ~15 min |
| 2 — HostMonster DNS (Shawn has DNS access as of 2026-09-22) | ~15 min, **after** the go-live A-record flip |
| 3 — config + deploy | within an existing deploy |
| 4 — header verification | ~15 min |

**Not started. No DNS, Plesk or config changes have been made for email.** (2026-09-22: Step 1 was
attempted in Plesk and stopped at blocker B1 before anything was changed.)
