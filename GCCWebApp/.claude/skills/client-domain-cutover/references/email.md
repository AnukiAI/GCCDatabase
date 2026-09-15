# Email under the client domain — SPF, DKIM, DMARC, PTR

Goal: transactional mail from the app (`noreply@sub.client.com`) lands in the inbox
at Gmail and Microsoft 365, with `spf=pass dkim=pass dmarc=pass` in the headers.

## Choose the sender domain first

| Option | Pros | Cons |
|---|---|---|
| Subdomain sender `noreply@sub.client.com` | Independent of the client's real mail; you control all records | Slightly less "official" looking |
| Root sender `noreply@client.com` | Looks like it comes from the company | Requires editing the root SPF (owned by whoever runs their mail) and signing DKIM for the root from your server; easy to break their real mail |

Default to the subdomain. Only go root if the client asks and their mail admin is
in the loop.

## Records at the DNS provider (subdomain sender)

Replace `sub`, `client.com`, `1.2.3.4`, and the DKIM values.

| Type | Name | Value |
|---|---|---|
| TXT | `sub` | `v=spf1 ip4:1.2.3.4 -all` |
| TXT | `default._domainkey.sub` | `v=DKIM1; k=rsa; p=<public key from Plesk>` |
| TXT | `_domainkey.sub` | `o=-` (Plesk emits this; harmless) |
| TXT | `_dmarc.sub` | `v=DMARC1; p=quarantine; rua=mailto:dmarc@client.com; fo=1` |

Notes:
- If `sub` is already a **CNAME** (web hostname), you cannot add TXT on the same
  name. Either make the web record an **A** record, or send from a different
  subdomain (`mail.client.com`) than the web hostname.
- Start DMARC at `p=none` if you want a week of reports before enforcing; go to
  `quarantine` before go-live.
- Use `ip4:` for the server IP rather than `a:`/`include:` so a future hostname
  change doesn't silently alter SPF.

## Root-domain sender (only if chosen)

- Root SPF: append `ip4:1.2.3.4` **inside** the existing record — one SPF record
  per name, ≤10 DNS lookups. Never add a second `v=spf1` TXT.
- DKIM: add the root domain in Plesk with mail service on, generate DKIM, publish the
  selector at the DNS provider. Do **not** change MX. Plesk will accept the domain
  without hosting its mailboxes.
- Tell the client's mail admin before and after. If they use Google Workspace or
  M365 with strict DMARC (`p=reject`), an SPF/DKIM mistake bounces the app's mail.

## PTR (reverse DNS)

`dig -x 1.2.3.4 +short` should return the Plesk server hostname, and that hostname
should resolve back to `1.2.3.4`. Set at the VPS/hosting provider (AccuWebHosting
support ticket or panel), not in the domain's zone. Microsoft in particular scores
generic PTRs (`ip-1-2-3-4.provider.net`) poorly.

## Verify

1. Send a test from the app to a Gmail address and a Microsoft address.
2. Gmail: *Show original* → look for `spf=pass`, `dkim=pass`, `dmarc=pass`.
   Microsoft: view message source → `Authentication-Results`.
3. Command line: `dig +short TXT sub.client.com`, `dig +short TXT default._domainkey.sub.client.com`.
4. Optional: send to a mail-tester service and record the score in the runbook.

## Common failures

- `dkim=fail` with a correct key → the record was pasted with quotes or line breaks
  from the Plesk UI. Re-paste as a single string.
- `spf=softfail` → the mail left from an IP not in the record (a second outbound IP,
  or the server's IPv6). Add it or disable IPv6 for outbound mail.
- Mail delivered but From shows the Plesk hostname → the app's `From` header isn't
  set; check the SMTP options binding.
- Works locally, fails in production → production is using the placeholder hostname
  in the `From`/links; check environment configuration, not code.
