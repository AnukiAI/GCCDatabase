# Plesk — alias, TLS, mail

Assumes the app already exists in Plesk as a site (e.g. `gccwebapp.com`) and the
client hostname (e.g. `database.greencirclecertified.com`) has a DNS record pointing
at this server. Plesk menu names drift between versions; the concepts don't.

## 1. Add the client hostname as a domain alias

**Websites & Domains → (the app's domain) → Add Domain Alias**

- Domain alias name: the client hostname.
- **Uncheck "Synchronize DNS zone with the primary domain"** / "DNS service" —
  Plesk is not authoritative for the client's zone. If left on, Plesk creates a
  local zone that does nothing except confuse the next person.
- Check "Web service" (required) and "Mail service" only if the app will send mail
  under this hostname (see §3).
- Redirect with HTTP 301: **off**. An alias with 301 on forwards the alias to the
  primary domain — the opposite of what you want. Off means both hostnames serve
  the site, and generated links follow whichever hostname the request came in on.

Why alias and not a new domain: one site, one document root, one deploy target. A
second domain in Plesk means a second web root to keep in sync.

## 2. Issue TLS for the alias

**Websites & Domains → (domain) → SSL/TLS Certificates → Let's Encrypt (or "Install
a free basic certificate")**

- Tick the alias in the list of names to include. If the alias isn't offered, DNS
  isn't resolving here yet — wait, then retry.
- Enable "Keep websites secured with the certificate" so renewals re-bind
  automatically.
- Turn on **Permanent SEO-safe 301 redirect from HTTP to HTTPS** on the Hosting
  Settings page.
- Verify from outside: `curl -sIv https://<alias> 2>&1 | grep -E "subject:|HTTP/"`.

## 3. Mail service for the client hostname (only if the app sends email)

**Websites & Domains → (domain or alias) → Mail → Mail Settings**

- Activate mail service on the domain.
- Create the sending mailbox: **Mail → Create Email Address** → e.g.
  `noreply@<hostname>`. This is the SMTP login the app uses; give it a real
  password and store it in the app's secret store, not `appsettings.json`.
- **DKIM:** Mail Settings → "Use DKIM spam protection system to sign outgoing
  email" → Apply. Plesk then shows two TXT records (`default._domainkey.<host>` and
  `_domainkey.<host>`). Copy them to the client's DNS provider — Plesk can't publish
  them because it doesn't own the zone.
- **Outgoing mail mode:** Tools & Settings → Mail Server Settings → make sure
  outgoing mail is not limited to the server's primary hostname.
- SPF and DMARC records are created at the DNS provider, not in Plesk. See
  `email.md`.

## 4. Old-hostname redirect on this server (retiring a legacy site)

When the plan is "point the old hostname at this server and 301 it":

1. Add the old hostname as **another domain alias** of the same site (steps in §1).
2. Issue Let's Encrypt for it — after its DNS points here.
3. Add the redirect at the web-server layer, not in app code:
   - **Apache & nginx → Additional nginx directives** (Plesk on Linux):
     ```nginx
     if ($host = old.clientdomain.com) {
         return 301 https://new.clientdomain.com$request_uri;
     }
     ```
   - **IIS (Plesk on Windows)** → `web.config` URL Rewrite rule, or Websites &
     Domains → (alias) → Hosting Settings → *Forwarding* with 301 — but only if the
     forwarding preserves the path; test with a deep link before relying on it.
4. Verify: `curl -sI https://old.clientdomain.com/some/deep/path?x=1` returns
   `301` with `Location: https://new.clientdomain.com/some/deep/path?x=1`.

Keep this alias in place indefinitely.

## 5. Canonical redirect for the placeholder hostname

Once the client hostname is verified, add the same kind of 301 for the placeholder
(e.g. `gccwebapp.com` → client hostname). Do this last; it's the moment the
placeholder stops being a fallback.

## 6. Things Plesk does that bite

- Plesk "Domain aliases" with *Synchronize DNS* on will overwrite records you
  intended to manage elsewhere if the server is ever made authoritative.
- Let's Encrypt renewals fail silently if the DNS record is later moved away; check
  the certificate expiry in the runbook 60 days after cutover.
- The Plesk firewall / Fail2Ban can block the ACME challenge from some networks;
  if issuance fails, check `/var/log/plesk/panel.log` or the Windows Plesk log.
- Plesk's own hostname (Tools & Settings → Server Settings) is what the mail server
  uses in HELO. It should resolve and have a matching PTR.
