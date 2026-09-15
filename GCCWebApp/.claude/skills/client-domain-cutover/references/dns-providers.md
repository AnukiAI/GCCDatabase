# DNS providers — where the record goes and what not to click

First, confirm the zone is actually served here:

```bash
dig NS clientdomain.com +short
dig +short sub.clientdomain.com          # A
dig +short CNAME sub.clientdomain.com    # CNAME
dig +short -t TXT sub.clientdomain.com   # SPF etc.
curl -sI https://sub.clientdomain.com | head -5   # current redirect?
```

If the NS records point somewhere other than the provider you're logged into, stop —
the record belongs at the nameserver host.

## Bluehost (cPanel)

- **Do not** use *Domains → Subdomains*. That creates a hosted folder on Bluehost and
  an A record to Bluehost's own server. Wrong direction.
- Go to **Domains → (domain) → DNS** (older UI: *Advanced → Zone Editor*).
- Look for an existing record for the target subdomain (A, CNAME, or a cPanel
  *Redirects* entry). Delete it. A leftover redirect entry under *Domains →
  Redirects* will keep winning even after you add the correct record.
- Add **A** `sub` → server IP, or **CNAME** `sub` → server hostname. TTL 300 (5 min)
  during cutover.
- Ignore Bluehost's SSL prompts for the subdomain. AutoSSL only covers hostnames
  hosted on Bluehost; the cert lives on the app server.
- Propagation from Bluehost is usually minutes; the old record's TTL governs how long
  cached answers survive elsewhere.

## GoDaddy

- **My Products → DNS → Manage Zones**. Add A/CNAME as above.
- GoDaddy's *Forwarding* feature is a hosted redirect on their server — don't use it
  for a hostname that should serve the app.

## Cloudflare

- Add the A/CNAME record. Set the proxy toggle to **DNS only (grey cloud)** until TLS
  is issued on the origin; you can turn the orange cloud on afterwards if you want
  Cloudflare in front.
- If proxied, TLS mode must be *Full (strict)* and the origin must present a valid
  cert for the hostname, otherwise you'll get 526 errors.
- Cloudflare Page Rules / Redirect Rules are a fine place for the old-hostname 301
  if the old hostname's zone is on Cloudflare.

## Namecheap

- **Domain List → Manage → Advanced DNS**. Add A/CNAME. Delete any *URL Redirect
  Record* for the same host first.

## General rules

- One record per hostname. Remove before you add.
- CNAME cannot coexist with other records on the same name (no CNAME + TXT on
  `sub`). If the subdomain also needs SPF/DKIM TXT records, use an **A record** for
  the web hostname, or put mail records on a different name.
- Lower TTL 24 h *before* a cutover so the flip propagates quickly; raise it after.
- Document the before/after records with the `dig` output in the inventory file.
