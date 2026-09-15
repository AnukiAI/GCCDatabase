# Cutover inventory — `database.greencirclecertified.com`

**Compiled:** 2026-09-14. All values resolved live from this machine; evidence inline.
**Target:** point `database.greencirclecertified.com` at the GCC application on Plesk/AccuWebHosting.
**Trigger:** the Partner Integration Guide states this host as the production API base URL, and it
does not serve the API today.

---

## Step 0 answers

| Question | Answer | Evidence |
|---|---|---|
| Client domain nameservers | **HostMonster** — `ns1.hostmonster.com`, `ns2.hostmonster.com` | `Resolve-DnsName greencirclecertified.com -Type NS` |
| What resolves for the target hostname | **`70.32.82.212`** — an **nginx redirector**, not an app | A record; `Server: nginx` |
| What that host actually does | **302 on every path → `https://db.greencirclecertified.com`**, **path NOT preserved** | traced `/`, `/api/v1/certificates`, `/certificate/1234` — all 302 to the bare root |
| TLS on the target hostname today | Valid Let's Encrypt, `CN=database.greencirclecertified.com`, 2026-08-21 → 2026-11-19 | direct `SslStream` probe |
| Who runs `70.32.82.212` | **Unknown / third party.** rDNS = `pa-ala.org` — a shared box, not GCC-specific | `[Net.Dns]::GetHostEntry` |
| The real legacy site | **`db.greencirclecertified.com`** → CNAME `cname.vercel-dns.com` (**Vercel**) | separate hostname, separate host |
| App server | **`64.27.52.66`** = `ACCU22.accuwebhosting.com`, Plesk, **IIS 10** | rDNS + `Server:` header |
| App hostname today | `gccwebapp.com` (+ `www`), LE cert covering both, 2026-08-05 → 2026-11-03 | cert probe |
| Main marketing site | **Squarespace** — `198.185.159.x` / `198.49.23.x`; `www` CNAMEs `ext-cust.squarespace.com` | A/CNAME |
| Mail | **Microsoft 365** — MX `greencirclecertified-com.mail.protection.outlook.com` | MX lookup |
| Root SPF | `v=spf1 ip4:107.20.88.125 include:zcsend.net include:spf.constantcontact.com include:spf.protection.outlook.com include:one.zoho.com ~all` | TXT |
| Go-live date / who calls it | **UNKNOWN — must be confirmed by the client** | — |

---

## The important correction

An earlier note in this project recorded that `database.greencirclecertified.com` "returns HTTP 200
serving the legacy Next.js site." **That was following a redirect.** The hostname itself is a bare
nginx 302 shim; the Next.js content came from `db.greencirclecertified.com`, the redirect target.

This matters, and in our favour:

- **Nothing of substance lives on the target hostname.** We are not displacing an application, only
  a redirect that already throws away the path it was given.
- **We do not need the operator of `70.32.82.212` to cooperate.** Repointing the A record at
  HostMonster moves the hostname away from that box entirely. That box is never reconfigured, so
  rollback is one DNS record — exactly the shape Step 6 of the skill wants.
- **`db.greencirclecertified.com` is untouched by this cutover.** It is a different hostname on a
  different host (Vercel) and stays exactly as it is.

## Two hostnames, do not conflate them

| Hostname | Host | What it is | In scope today? |
|---|---|---|---|
| `database.greencirclecertified.com` | nginx @ 70.32.82.212 | Path-dropping 302 shim → `db.` | **YES — this is the cutover** |
| `db.greencirclecertified.com` | Vercel | **Live legacy public database** — 1,021 public certificates, `/certificate/{id}`, `/api/certificates/{id}/download`, plus an authenticated `/portal` on tRPC | **NO — separate decision** |

Retiring `db.` is a larger piece of work (deep-link mapping for 1,021 certificates, the signed-in
portal, S3-backed PDF downloads) and is **not** part of today's change. See the open questions.

---

## Application readiness — already good

Audited in `database.greencirclecertified.com-audit.md`. Summary: **no code change is required to
accept the new hostname.**

- `AllowedHosts` is `"*"` in every environment file — nothing to add.
- CORS already lists `https://database.greencirclecertified.com` (added 2026-08-24, marked defensive).
- `UseForwardedHeaders()` is wired (`Extensions/SecurityServicesExtensions.cs:221`).
- Partner-facing URLs are built from the **request host**
  (`CertificatesController.cs:1670` — `$"{request.Scheme}://{request.Host}"`), so they follow the
  hostname the caller used. No hardcoded base URL to change.

---

## Open questions — these gate go-live

1. **Who holds the HostMonster login?** The A record must change there. If it is the client's, they
   perform this step and we cannot schedule it unilaterally. **This is the only hard blocker.**
2. **Does the client accept that `database.` stops redirecting to `db.`?** Low impact — the redirect
   already discards the path and lands everyone on the legacy home page — but it is their domain and
   their call.
3. **Is `db.greencirclecertified.com` staying?** If the legacy database is also being retired, that is
   a separate cutover with real deep-link work. Do not bundle it into today.
4. **Does anything published point at `database.…`?** Printed certificates, QR codes, partner
   documentation, the Squarespace site nav. Worth one pass before the redirect changes meaning.
5. **Sender address.** The app currently sends as `admin@gccwebapp.com`. Moving the sender to the
   client domain is a separate decision with SPF/DKIM work at HostMonster and coordination with the
   M365 admin — **out of scope for today** unless asked.
