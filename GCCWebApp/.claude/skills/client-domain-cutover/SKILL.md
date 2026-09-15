---
name: client-domain-cutover
description: Move a client-facing web app from a placeholder hostname (e.g. gccwebapp.com) onto the client's own domain/subdomain (e.g. database.greencirclecertified.com) when DNS lives at one provider (Bluehost, GoDaddy, Cloudflare) and the app is hosted elsewhere (Plesk/AccuWebHosting, IIS, Nginx), including retiring an old site on a legacy host (Vercel, a prior vendor) with a permanent redirect. Use whenever Shawn mentions pointing DNS, domain aliases, subdomain setup, A/CNAME records, Plesk domain or mail config, Let's Encrypt for a new hostname, AllowedHosts, hardcoded URLs, go-live redirects, "make X the link instead of Y", "what hosting documentation do we have", or SPF/DKIM for the app's sender address — even if he names only one piece ("how do I do this in Plesk", "will db. redirect"). Also use it to write the client-facing cutover explanation and the Claude Code audit/verification prompts.
---

# Client Domain Cutover

Moving an app from a hostname you control to a hostname the client controls is a
coordination problem more than a technical one. The technical steps are small; the
risk is in ordering, in who owns each layer, and in the deep links, emails, and
certificates that silently embed the old hostname. This skill exists so every cutover
follows the same shape, produces the same documentation, and can be rolled back with
a config flip rather than a redeploy.

## Step 0 — Inventory before touching anything

Answer these before proposing any change. If the conversation or memory doesn't have
the answer, ask — never guess who owns a DNS zone.

| Question | Why it matters |
|---|---|
| Where are the client domain's nameservers? | Records go where the NS points, not where the WordPress site lives. |
| What currently resolves for the target hostname? | An existing record/redirect must be removed, not layered on. |
| What currently resolves for any *old* hostname being retired? | Tells you whether the old site is on a third-party host (Vercel, Netlify, a vendor) and who has to cooperate. |
| Who runs the app server, and what panel (Plesk, cPanel, raw IIS/Nginx)? | Determines where bindings and TLS live. |
| What does the app hardcode? | Base URLs in config, email templates, PDFs/QR codes, OAuth redirect URIs, cookie domains. |
| Does the app send email? From what address? | Sender domain needs SPF/DKIM at the DNS provider, not just SMTP creds. |
| What's the go-live date and who calls go/no-go? | Nothing on the old host changes before this. |

Resolve DNS yourself when you can (`dig +short <host>`, `dig NS <domain>`, `curl -sI <url>`)
rather than asking. Record answers in `docs/cutover/<hostname>-inventory.md`.

When the ask is "what documentation do we have" — search the repo and any shared
drive for `docs/`, `README`, `HANDOVER`, `hosting`, `DNS`, `Plesk`, `deploy`, and
the provider names before answering. Report what exists, what's stale, and what's
missing, then fill the gaps with the inventory above. Don't answer "we have nothing"
without looking.

## Step 1 — DNS at the client's provider

The client's provider (often Bluehost/cPanel because that's where WordPress lives)
will try to create a *hosted* subdomain — a folder on its own server. That's the
wrong tool. You want a bare DNS record pointing away from that provider.

- **A record** `sub` → app server public IP. Preferred when you own a static IP.
- **CNAME** `sub` → app server hostname. Fine when the target hostname is stable and
  it's not the zone apex.
- TTL 300 during cutover; raise to 3600+ once stable.
- Do **not** request SSL from the DNS provider for the record. TLS lives on the
  server that answers.

Provider-specific steps: `references/dns-providers.md`.

## Step 2 — Host binding and TLS on the app server

Add the client hostname to the existing site, don't create a second site.

- **Plesk:** add the hostname as a **domain alias** of the existing site (Websites &
  Domains → the app's domain → Add Domain Alias), uncheck DNS management, then issue
  Let's Encrypt for the alias. Details and the mail steps: `references/plesk.md`.
- **Raw IIS / Nginx / Caddy:** add the hostname to the site binding / `server_name`,
  then win-acme or Certbot. Caddy handles both automatically.

TLS issuance only works once the DNS record resolves to this server — sequence
matters. Keep the old placeholder hostname bound during testing; retire it in Step 6.

## Step 3 — Application configuration

Framework-specific checklist in `references/aspnet-core.md`. The shape is the same
everywhere:

1. Host allow-list (`AllowedHosts` in ASP.NET Core; `ALLOWED_HOSTS` in Django; etc.).
2. Forwarded headers if a proxy sits in front — otherwise generated links carry the
   internal host.
3. Hardcoded base URLs — grep for the old hostname, `http://`, `https://`, and any
   `BaseUrl`/`SiteUrl`/`AppUrl`/`PublicUrl` config key. Email templates and PDF/QR
   generation are the usual hiding places.
4. Auth cookies (`Domain` set explicitly?) and OAuth/OIDC redirect URIs registered
   with external providers.
5. CORS origins if a separate front end or partner API calls in.

Run the audit as a read-only Claude Code task before changing anything. Template in
`assets/audit-prompt.md`.

## Step 4 — Outbound email under the client domain

If the app sends mail, decide the sender address with the client before go-live:

- **Subdomain sender** (`noreply@sub.client.com`) — default. Doesn't touch the
  client's real mail. Needs SPF, DKIM, DMARC records for the *subdomain* at the DNS
  provider; DKIM key generated on the mail server (Plesk does this).
- **Root-domain sender** (`noreply@client.com`) — only if the client's mail admin
  will add your server IP to the root SPF and you can sign DKIM for the root domain
  from your server. More trustworthy-looking, more coordination.

Also confirm reverse DNS (PTR) on the server IP. Full setup: `references/email.md`.

## Step 5 — Retiring an old site (if there is one)

When a previous site lives on another host and its hostname must keep working:

- **Never touch the old hostname before go-live.** It's production until the moment
  you cut over.
- Preferred pattern: at go-live, point the old hostname's DNS at the **new** server,
  bind it there with its own TLS cert, and add a server-layer **301** to the new
  hostname with path and query preserved. Rollback is one DNS record. The old host
  never needs to be reconfigured.
- Alternative when the old host must stay authoritative for the hostname: a redirect
  rule on the old host. Weaker — rollback depends on the vendor.
- Deep links matter more than the home page. Map every old URL shape (certificate
  IDs, downloads, query strings) to a new URL before go-live; pull the old site's
  logs if you can get them. A root-only redirect reads as a regression to anyone
  holding a bookmarked or printed link.
- The client, not you, sends the request to their outgoing vendor (data export,
  URL structure, change freeze, keep site up ~30 days, ownership handover,
  decommission). Draft it for them: `references/client-comms.md`.

## Step 6 — Cutover order and rollback

```
Pre-go-live (any time, zero prod risk):
  1. DNS record for NEW hostname → new server
  2. Alias + TLS on new server
  3. AllowedHosts / forwarded headers / hardcoded URL fixes deployed
  4. Email sender domain set up and header-checked (dkim=pass, spf=pass)
  5. UAT on the NEW hostname, sign-off recorded

Go-live day, in this order:
  6. Change freeze on old site; final data migration; verify counts
  7. (If retiring an old hostname) bind OLD hostname + TLS on new server with 301 →
     new hostname, THEN flip OLD hostname's DNS to the new server
  8. Verify with scripts/verify-cutover.sh from outside the network
  9. Canonical redirect: placeholder hostname (e.g. gccwebapp.com) → new hostname

Post-go-live:
  10. Leave old site running behind the redirect ≥ 30 days
  11. Watch 404s on the new host for unmapped deep links
  12. Update public nav links so the redirect becomes fallback, not primary path
  13. Raise TTLs; write docs/cutover/<hostname>-runbook.md with what was actually done
```

Rollback at any point = revert the DNS record(s). Because the old site is never
modified, it comes back as soon as DNS propagates. Put redirects at the proxy/host
layer, never inside app code, so a revert is a config flip, not a redeploy.

## Step 7 — Communicating with the client

Non-technical clients hear "DNS flip" and "redirect" as noise. Tell them the two
things they care about: old links keep working after, nothing breaks before. Wording
patterns in `references/client-comms.md`. Never promise a redirect exists until the
rule has been tested on staging.

## Outputs this skill produces

Every cutover should leave behind, in the app repo under `docs/cutover/`:

- `<hostname>-inventory.md` — Step 0 answers, with the `dig`/`curl` evidence
- `<hostname>-audit.md` — hardcoded URL findings (from the audit prompt)
- `<hostname>-runbook.md` — the exact steps taken, in order, with timestamps
- `scripts/verify-cutover.sh` — parameterised; committed so it can be rerun

Plus a Claude Code prompt for each read-only audit and each verification step,
following Shawn's standing read-first / verify-before-modify pattern.

## Reference map

- `references/dns-providers.md` — Bluehost/cPanel, GoDaddy, Cloudflare, Namecheap:
  where the zone editor is and what *not* to click
- `references/plesk.md` — domain alias, Let's Encrypt, mail service, DKIM, SMTP
- `references/aspnet-core.md` — AllowedHosts, forwarded headers, URL generation,
  cookies, canonical redirect middleware
- `references/email.md` — SPF/DKIM/DMARC/PTR and header verification
- `references/client-comms.md` — plain-language confirmations and the vendor
  handover request the client sends
- `assets/audit-prompt.md` — Claude Code read-only hostname audit template
- `scripts/verify-cutover.sh` — external verification of DNS, TLS, redirects
