# Cutover runbook — `db.greencirclecertified.com` (canonical) + `database.` alias

> **The filename is historical.** It was written on 2026-09-14 when `database.` was the intended
> production hostname. That changed on 2026-09-21 — **`db.` is canonical now** — but the name is
> referenced from `README.md`, `go-live-plan-2026-09-21.md` and CR-050, so it stays.

> **✅ EXECUTED 2026-09-22 ~20:56 EDT — brought forward from 13 Oct by Shawn's decision.** Steps 1b,
> 4, 8, 9 done: `database` A and `db` A → `64.27.52.66` (CNAME `cname.vercel-dns.com` deleted — that
> is the rollback value); `www.database` deleted; `www.db` A added; Let's Encrypt (20:56, exp
> 2026-12-21) covers db, www.db, database, dbtest, gccwebapp.com, www.gccwebapp.com. Verified
> externally: pages 200, `/api/v1/certificates` 401, http→https 301, `database` → `db` 301 with path
> and query, legacy `/certificate` and `/api/certificates/{id}/download` 301. Pre-go-live items
> accepted as open: number seeding, DEF-433 under load, client sign-off, Amazon notice.
> **Gotcha:** Plesk's "www" box covers the domain AND every alias — `www.<alias>` must exist in DNS
> first or the whole reissue fails (the first reissue at 20:44 silently omitted both aliases).
>
> *Original status line below, superseded.*

**Status 2026-09-22: step 0 cleared · TTLs pre-staged · DNS value change HELD pending a deploy.**
The zone editor's mismatched-TTL refusal is fixed (apex `apple-domain-verification` TXT raised
3600 → 14400). `database` and `db` are both at **TTL 300**, verified authoritatively. **Next action:
the Plesk domain alias (step 3).** Step **1b is deliberately held** — the `Canonical host - database
subdomain` rewrite rule is not in any deployed build, so repointing now would land the hostname on
the test gate instead of redirecting. Phase 2 still waits on its own gates (§"Do not attempt Phase 2
until"). Fill the timestamp column as you go.

**Plan of record:** `go-live-plan-2026-09-21.md` · **Inventory:** `…-inventory.md` ·
**Audit:** `…-audit.md` · **Verify:** `scripts/verify-cutover.sh`

## The model (decided 2026-09-21)

> Shawn: *"retire rename database and db subdomains to point to gccwebapp. Make gccwebapp domain
> always display as `db.greencircle…`"*

| Hostname | Today (verified 2026-09-21) | After cutover | Phase |
|---|---|---|---|
| **`db.greencirclecertified.com`** | **CNAME → `cname.vercel-dns.com`** (TTL 14400) — **Vercel, the LIVE legacy database**, HTTP 200. *Corrected 2026-09-21: it is **not** an A record at `76.76.21.123`, which every earlier revision of these documents claimed* | `64.27.52.66` **A** — **the app. Canonical. Everything redirects here.** | **2** |
| `database.greencirclecertified.com` | `70.32.82.212` — nginx shim, lossy 302 to `db.` | `64.27.52.66` — alias, **301 → `db.`** | **1** |
| `gccwebapp.com` | `64.27.52.66` — the app, behind the test gate | `64.27.52.66` — **301 → `db.`** | **2** |
| `dbtest.greencirclecertified.com` | `64.27.52.66` — what Amazon integrates against | unchanged | — |
| `test.gccwebapp.com` | `64.27.52.66` — live, gated, on `gccwebap_test` | the permanent test environment | done |

---

## The two phases have completely different risk, and must not be merged

**Phase 1 — `database.` — near-zero risk.** That hostname serves no application today, only a 302
that discards the path. Moving it to our server turns a lossy redirect into a better one. The box it
points at (`70.32.82.212`) is never touched, so rollback is one DNS record.

**Phase 2 — `db.` — this is the actual cutover.** That hostname is the client's live public
certificate database, on Vercel, with ~1,021 public certificates and links in circulation. **DNS has
no partial state:** the moment the record moves, every existing link to the legacy site stops
working unless the redirect map ships with it. There is no soft launch of a DNS change.

**Do not attempt Phase 2 until:**

1. the legacy deep-link redirect map is built, validated and **in the deployed bundle** (audit §10,
   go-live plan §A7) — 5,999 mappable entries from `C:\Users\swest\GCCPortalPull\manifest.csv`;
2. DEF-433 is fixed — the anonymous PDF-render routes are unthrottled, and the test gate is the only
   thing currently mitigating them. It comes off in Phase 2;
3. the client has signed off UAT and Amazon has had its notice window.

---

## Hard blocker before anything below

**Who can edit DNS at HostMonster?** The `greencirclecertified.com` zone is served by
`ns1/ns2.hostmonster.com` — **the client's**, not ours. Every DNS step here is theirs to perform
unless we are given access. Confirm this before promising a date.

> **It is HostMonster, not Bluehost.** Same company, near-identical cPanel. Logged into the wrong
> one, the domain simply will not be listed. Zone editor: **Domains → (domain) → DNS**, older UI
> **Advanced → Zone Editor**. Do **not** use *Domains → Subdomains* — that creates a hosted folder
> and an A record pointing back at HostMonster. Check *Domains → Redirects* for a leftover entry on
> the same hostname; it keeps winning after the zone record is correct. Ignore AutoSSL prompts —
> TLS lives on the app server.

### What is actually in the zone

Observed in the zone editor **2026-09-21** (Shawn's screenshot of the record list — not a DNS probe,
and the list was scrolled, so treat it as partial). Everything below is on **TTL 14400**, which is
what makes step 1a and step 7b necessary.

| Record | Value | Origin | Action |
|---|---|---|---|
| `database.` **A** | `70.32.82.212` | The third-party nginx redirector (rDNS `pa-ala.org`) serving today's path-dropping 302 — inventory §3.1 | **Edit** — step 1b |
| `www.database.` **A** | `70.32.82.212` | Auto-created companion. cPanel adds a `www.` A record for every subdomain made through *Domains → Subdomains*, which is how `database.` was originally built | **Decide** — step 1c |
| `zk3j73e324zjcpmxedkc.` **CNAME** | `verify.squarespace.com` | Squarespace domain-ownership token for the **marketing site** | **Do not touch** |
| `_cpanel-dcv-test-record.` **TXT** | `6KiNgcwRUAopP0LHzQM_…` | cPanel AutoSSL's domain-control-validation probe. Cosmetic | **Do not touch** |

**`db.` was not visible in the captured portion — resolved separately, 2026-09-21, and it changed
step 8.** Authoritative answer from `1.1.1.1` and `8.8.8.8`:

| Record | Value | TTL |
|---|---|---|
| `db.` **CNAME** | `cname.vercel-dns.com` | 14400 |
| ↳ which resolves to | `66.33.60.67`, `76.76.21.98` — Vercel anycast, **rotating** | 300 |

**It is a CNAME, not an A record, and `76.76.21.123` was never its address.** Every revision of these
documents before 2026-09-21 said "`db` A → `76.76.21.123`". That was wrong twice over, and the
rollback row was the dangerous half — see step 8 and Rollback below.

**The Edit buttons render, so whoever is in that account holds zone rights** — which settles the
hard blocker above for that login. If an Edit greys out or silently reverts, the record is bound to
a cPanel-managed subdomain: remove it under *Domains → Subdomains* (that deletes these records) and
add a fresh bare A record.

> ⚠️ **Do not touch MX or the root SPF.** MX points at the client's live Microsoft 365
> (`greencirclecertified-com.mail.protection.outlook.com`) and nothing in this cutover needs it. The
> root SPF already carries five sources against a hard 10-lookup limit; another `include:` could
> break mail for the whole domain, Microsoft 365 included. Sender work is a separate job —
> `outbound-email-client-domain.md` — and the subdomain sender avoids both records entirely.

---

## Step 0 — BLOCKER: the zone editor refuses every save until the TXT TTLs are normalised

**Hit live on 2026-09-21 on the first attempted edit.** The cPanel zone editor rejects the save with:

> **Error:** greencirclecertified.com's "TXT" records have mismatched TTL values (14400 and 3600).
> Records of the same name and type must always have the same TTL value.

**This is not a permissions problem and has nothing to do with the record being edited.** It is a
pre-existing inconsistency, it blocks **every** DNS step in both phases, and until it is cleared
nothing below can be executed.

### What is actually mismatched — ONE record

Read from the **zone editor UI**, 2026-09-22, showing all 53 records. The zone is uniformly
**TTL 14400** with exactly one exception:

| TTL | Name | Type | Value |
|---|---|---|---|
| 14400 | `greencirclecertified.com` | TXT | `v=spf1 ip4:107.20.88.125 include:zcsend.net include:spf.constantcontact.com include:spf.protection.outlook.com include:one.zoho.com ~all` ← root SPF |
| 14400 | `greencirclecertified.com` | TXT | `MS=ms49326145` |
| 14400 | `greencirclecertified.com` | TXT | `google-site-verification=KJSg8Nn_MbGOKvt00Gfdw-WLchjosyWi8XxauSRkiIE` |
| **3600** | `greencirclecertified.com` | TXT | **`apple-domain-verification=OC6SuW8PHizJzPcP`** ← **the only 3600 record in the entire zone** |

**cPanel's error message is correct and literal.** The apex TXT RRset — same name, same type — holds
three records at 14400 and one at 3600. That is a real RFC 2181 §5.2 violation, and it is the whole
problem.

> ### ⚠️ Why external DNS queries cannot find this, and reported the opposite
>
> Resolving the apex TXT set from `ns1.hostmonster.com` returns **all four records at 3600**, which
> reads as "the apex is consistent at 3600 and the other names are the odd ones out." That is an
> artefact: **RFC 2181 §5.2 requires an authoritative server to serve every record in an RRset with
> a single TTL — the lowest one present.** BIND collapses the apex set to 3600 because `apple` is
> 3600, and the three 14400 records become invisible as such.
>
> **A within-RRset TTL mismatch is undetectable from outside the zone. Only the control panel shows
> it.** Two consecutive wrong diagnoses on 2026-09-21 came from trusting `Resolve-DnsName` output
> here — first "two stray records", then "eight records split 4/4", then a recommendation to raise a
> support ticket. All three were wrong, and the real answer is one edit. **For TTL questions, read
> the zone editor's TTL column. Do not infer it from a query.**

### The fix — one edit

| # | Step | Owner | Done |
|---|---|---|---|
| 0a | Zone Editor → apex row **`greencirclecertified.com.` TXT `apple-domain-verification=OC6SuW8PHizJzPcP`** → **Edit** → **TTL 3600 → 14400** → Save. **Change the TTL field only**; leave the value untouched. | whoever holds DNS | ✅ **2026-09-22** |
| 0b | Confirm the save is accepted — no mismatched-TTL error. | — | ✅ **2026-09-22** |
| 0c | Retry the `database` A-record edit and confirm it saves. | — | ☐ |

> **0a/0b verified externally 2026-09-22.** The apex TXT set now returns **all four records at
> 14400**, where it previously returned all four at 3600. Because a server serves an RRset at its
> *lowest* TTL, "all four at 14400" is only possible if no 3600 record remains — so this single
> query is a sound check that the fix landed, even though it could not have found the fault.

**This save is legal on its own**, unlike the multi-record plans that preceded it: the validator
checks the set being *submitted*, so raising `apple` to 14400 resolves the mismatch in the same
operation that makes it. Raising it (rather than lowering the other three to 3600) means the **root
SPF is never touched** — five lookups against a hard limit of ten, and the one record here that can
break mail for the whole domain, Microsoft 365 included — and it leaves the whole zone uniform at
14400, which is also what AutoSSL writes.

> ### Not every cPanel operation runs this validation — and that is a usable fallback
>
> **`dbtest` was added to this zone without any error**, while the `apple` fault was (almost
> certainly) already present. **Add Record** appends through a simpler endpoint; **Edit** submits
> through `mass_edit_dns_zone_records`, which validates RRset TTL consistency across the whole
> submitted set. Only the edit path looks. This is inferred from observed behaviour, not from
> cPanel's source — but it is the only explanation that does not require an unexplained change to
> the `apple` record between the two attempts.
>
> **So if step 0a is itself refused, do not escalate — switch paths.** Instead of editing
> `database`:
>
> 1. **Delete** `database.greencirclecertified.com` A `70.32.82.212`
> 2. **Add Record** → `database` A → `64.27.52.66`
>
> Back to back; the hostname is absent in between, which is harmless here because it serves nothing
> but a redirect today. **Do not use this trick on `db` at D-0 without fixing the zone first** — a
> gap on the live hostname is a real outage, however short.

### Two findings that reduce risk elsewhere

- **`www` carries a CNAME *and* a TXT record.** That is exactly the RFC 1034 "no other data at a
  CNAME node" case flagged as a risk against step 7a — and this zone already does it, tolerated by
  cPanel. That **downgrades** the concern that DNS-01 pre-issuance for `db.` would be refused.
- **`_acme-challenge` exists at the apex**, so DNS-01 validation has been performed on this domain
  before. Also encouraging for 7a. Neither is proof; both make 7a more likely to work than step 7a's
  warning assumes.

### ✅ What this means for the TTL steps below — they are fine

An earlier revision of this file warned that lowering a single A record to 300 might trip the same
validator. **It will not.** The rule is per **RRset** — same name *and* type — not per type across
the zone:

- `database` A is a **single-record RRset**. Changing its TTL cannot create a mismatch. Step 1a is
  legal. Same for `dbtest`, `www.database`, `mail`, `ftp`, `cpanel`.
- `db` is a **single CNAME**, so step 7b is legal too. **Phase 2's five-minute rollback guarantee
  holds.**
- The one multi-record RRset to leave alone is the **apex A set** — four Squarespace addresses
  (`198.185.159.144/.145`, `198.49.23.144/.145`), all at 14400. Nothing in this cutover touches it,
  and if it ever must be changed, all four move together.

Still worth doing step 1a early rather than at D-0 — but as ordinary sequencing, not as a probe of
whether the cutover is possible.

---

## Phase 1 — `database.` · pre-go-live, zero production risk

Can run any time once DNS ownership is settled **and step 0 is cleared**.

> ### 🔶 DECISION 2026-09-22 — alias now, DNS value change HELD
>
> Shawn: *"do the alias now, hold the DNS change."*
>
> **The `Canonical host - database subdomain` rewrite rule is not deployed.** It exists only in the
> working tree — `git show HEAD:web.config` does not contain it — so no built bundle carries it, and
> `20260917-1751` is held under the one-final-build policy.
>
> **Consequence if `database` were repointed today:** the alias would serve the app, steps 3–5 would
> pass, but the **301 to `db.` would not fire** and the hostname would land on the **test gate**.
> Today it does a lossy 302 to the live legacy site, so that is a small but real regression on a
> hostname whose circulation (printed certificates, email signatures, partner bookmarks) is unknown.
>
> **Therefore:**
>
> | Step | State |
> |---|---|
> | 0, 1a, 1d — TTLs pre-staged to 300 | ✅ **done 2026-09-22**, and they keep their value regardless |
> | **3 — Plesk domain alias** | ▶️ **do now.** Costs nothing, reversible, and is the only Phase 1 step not gated on either the TTL clock or a deploy |
> | **1b — `database` A → `64.27.52.66`** | ⏸ **HELD** until a deployed build carries the rewrite rule |
> | 1c — delete `www.database` | may be done any time |
> | 4, 5, 6 — TLS and verification | follow 1b |
>
> **The 4-hour TTL clock is not running and does not need to.** It starts when 1b is unheld.

| # | Step | Where | Owner | Done |
|---|---|---|---|---|
| 1a | **Lower the TTL first, on its own, and change nothing else.** `database` is on **TTL 14400 (4 h)** today. Edit **only** the TTL to **300**, leave the value at `70.32.82.212`, and **wait out the old 4-hour TTL** before step 1b. A TTL reduction does not take effect until the previously-cached TTL expires — flip the value on the same visit and you are committed for four hours, and any rollback costs four hours too. | HostMonster zone editor | whoever holds DNS | ✅ **2026-09-22** |
| 1b | ⏸ **HELD — see the decision box above.** Do not run until a deployed build carries the `Canonical host - database subdomain` rule. **A record** `database` → **`64.27.52.66`**. Confirmed an **A** record, not a CNAME (authoritative, 2026-09-21), so this is a plain **Edit of the existing record, not a new one** — do not layer a second A record, or traffic round-robins and half of it lands on the old host. *(Contrast step 8: `db` **is** a CNAME and needs delete-then-create.)* | HostMonster zone editor | whoever holds DNS | ⏸ HELD |
| 1c | **Decide `www.database.greencirclecertified.com`** — it exists, A → `70.32.82.212`, auto-created alongside the subdomain. **Recommended: delete it.** Nothing links to it, and repointing it means it must *also* be added as a Plesk alias (step 3) **and** carried on the Let's Encrypt certificate (step 4) or it serves a certificate error on a hostname nobody asked for. Repoint only if the client says something uses it. | HostMonster zone editor | whoever holds DNS | ☐ |
| 1d ✅ **2026-09-22** | **Lower `db`'s TTL to 300 NOW — this is Phase 1 work, not Phase 2.** Edit the TTL field only; leave the value `cname.vercel-dns.com` untouched. **This is not part of the cutover:** `db` goes on pointing at Vercel and the legacy site serves exactly as before. It only shortens how long resolvers cache that answer. Because a TTL cut is not honoured until the old 4-hour TTL expires, doing it weeks early removes it from the cutover's critical path entirely — doing it at D-1 races a 4-hour window against the go-live slot. Zero risk, do it in the same visit as 1a. | HostMonster zone editor | whoever holds DNS | ☐ |

> ### Verify every zone edit against `ns1.hostmonster.com` — never against a public resolver
>
> `Resolve-DnsName <host> -Server ns1.hostmonster.com` serves the zone-file value with no caching
> lag, so an edit is confirmed or disproved within seconds.
>
> **A public resolver cannot verify a TTL change, and will actively mislead you.** A caching
> resolver reports the *remaining* lifetime of its copy, counting down — so a record still
> configured at 14400, cached 4 hours ago, reports a TTL of ~300 and looks exactly like a
> successful change to 300. Observed live on 2026-09-22: `1.1.1.1` returned `database` at TTL 300
> while the zone file still held **14400**.
>
> **The `database` / `www.database` rows are adjacent in the editor and two edits intended for
> `database` landed on `www.database` instead** (2026-09-22). Read the **Name** column before
> saving, and confirm authoritatively after.
>
> ### Query BOTH nameservers, REPEATEDLY — the authoritative side is anycast too
>
> `ns2.hostmonster.com` is `162.159.25.186`, Cloudflare-hosted anycast with multiple backend nodes
> that do **not** update in lockstep. Observed 2026-09-22: three consecutive queries for the same
> record returned **14400, 300, 14400**, while ns1 was consistently 300 and **the SOA serials on ns1
> and ns2 matched** — so a serial comparison does not detect it either.
>
> **A single authoritative query can therefore report either the old or the new value for several
> minutes after a correct edit.** Poll until every answer agrees before believing a change has
> propagated:
>
> ```powershell
> 1..6 | %{ (Resolve-DnsName <host> -Server ns2.hostmonster.com | ?{$_.Type -in 'A','CNAME'}).TTL }
> ```
>
> **This sets when the TTL clock actually starts.** A resolver that hit a stale node cached the
> *old* TTL and holds it for its full duration — so the wait before a value change runs from
> **convergence**, not from the moment of saving. At D-0 the same applies to step 8: poll until
> consistent before declaring the cutover propagated.
| 2 | Confirm propagation: `nslookup database.greencirclecertified.com` → `64.27.52.66` | anywhere | — | ☐ |
| 3 | Add `database.greencirclecertified.com` as a **Domain Alias** of the existing `gccwebapp.com` site — see the settings table below. **Do NOT create a new site:** a second site gets its own data-protection key ring. *(Plesk accepts an alias before DNS points here, so 3 may be done ahead of 1b — it is the only Phase 1 step not gated on the TTL clock.)* | Plesk | Shawn | ✅ **2026-09-22** |
| 3b | **Add `db.greencirclecertified.com` as a SECOND Domain Alias of the same site, now.** This is step **7d pulled forward** — identical settings. It is preparation, not cutover: the server only answers to that Host header if DNS sends traffic here, and DNS still points at Vercel, so the public keeps reaching the legacy site untouched. Doing it today takes certificate and binding work off the go-live critical path. | Plesk | Shawn | ☐ |

**Verify an alias bound without touching DNS** — send the Host header straight at the IP:

```powershell
curl.exe -s -o NUL -w "%{http_code} %{redirect_url}`n" -H "Host: <hostname>" http://64.27.52.66/
```

A **bound** alias returns the site's own HTTP→HTTPS redirect (`301` → `https://<same hostname>/`).
An **unbound** hostname falls through to a default site and behaves differently — `db.` returned a
bare `200` with no redirect on 2026-09-22, which is how its absence was spotted. Verified this way:
`database.` **bound**, `gccwebapp.com` bound, `db.` **not yet**.

#### Step 3 in detail — Plesk Obsidian 18.x

**Websites & Domains** → the **`gccwebapp.com`** card → **Add Domain Alias**. (In some skins the
button sits at the top of the page beside *Add Domain* / *Add Subdomain*; if so, confirm the form's
**parent domain** reads `gccwebapp.com` before saving.)

| Field / option | Value | Why |
|---|---|---|
| Domain alias name | `database.greencirclecertified.com` | |
| Parent domain | `gccwebapp.com` | Same site, same document root, same app pool, same key ring |
| **Synchronize DNS zone with the primary domain** | ☐ **UNCHECK** | The zone is at HostMonster. Leaving it on has Plesk author DNS for a zone it does not control |
| **Mail service** | ☐ **UNCHECK** | Keeps Plesk away from mail routing on a hostname under the client's domain — their live Microsoft 365 and DMARC `p=quarantine` are not to be disturbed |
| **Web service** | ☑ **KEEP** | This is what makes the alias serve the site at all |
| **Redirect with the HTTP 301 code** | ☐ **UNCHECK — see below** | |

> ⚠️ **Do not tick "Redirect with the HTTP 301 code", however much it looks like the right option.**
> `database.` is indeed meant to 301 to `db.` — but **Plesk's alias redirect drops the path**, which
> is exactly the lossy behaviour of the nginx shim this cutover exists to replace. The redirect is
> the app's job, via the `Canonical host - database subdomain` rule already enabled in `web.config`,
> which preserves path **and** query. Redirecting at the web-server layer means the request never
> reaches the app and that rule never runs.
| 4 | Issue **Let's Encrypt** for the alias. Succeeds only **after** step 2 resolves here — HTTP-01 validation, so sequence matters. | Plesk | Shawn | ☐ |
| 5 | Confirm the app answers on the hostname over TLS, unauthenticated: `/api/v1/certificates` → **401**, not 200 and not an HTML page. | `scripts/verify-cutover.sh` | Shawn | ☐ |
| 6 | Confirm the **canonical 301** fires: `https://database.greencirclecertified.com/anything?q=1` → **301** to the same path **and query** on `db.` — served by the `Canonical host - database subdomain` rule, which **ships enabled** in `web.config`. Before Phase 2 this lands on the legacy Vercel site, which is exactly what the hostname does today. | curl | Shawn | ☐ |

---

## Phase 2 — `db.` · go-live day

Run in this order. Step 8 is the point of no return.

| # | Step | Owner | Done |
|---|---|---|---|
| 7a | **Pre-issue TLS for `db.` — recommended, at D-1.** Plesk's Let's Encrypt with a **DNS-01** challenge needs one TXT record at HostMonster and does **not** require DNS to point here yet. It takes certificate issuance off the cutover's critical path. *Fallback:* HTTP-01 immediately after step 8, budgeting ~10 min, during which the site serves an untrusted certificate to the public. **⚠️ Test this at D-2, do not assume it:** DNS-01 needs a TXT at `_acme-challenge.db.greencirclecertified.com`, and `db` is currently a **CNAME** — RFC 1034 forbids other data at a CNAME node, and cPanel enforces that inconsistently, so the zone editor may simply refuse to create the record. If it refuses, 7a is unavailable and you are on the HTTP-01 fallback: plan the ~10 min untrusted-certificate window into the cutover rather than discovering it on the day. | Shawn + DNS holder | ☐ |
| 7b | **Confirm `db` is already at TTL 300** — this should have been done weeks earlier at **step 1d**, not here. `Resolve-DnsName db.greencirclecertified.com -Server ns1.hostmonster.com` → expect **300**. If it still reads 14400, lower it **and wait a minimum of 4 hours** before step 8, because a TTL cut is not honoured until the previously-cached 4-hour TTL expires — otherwise the "300" is a fiction and the cutover *and its rollback* both take four hours to propagate, on the one day that matters. | DNS holder | ☐ |
| 7c | **Change freeze** on the legacy system. Final delta load. Certificate-number seed assertion: next auto-number **>** GCC's written last-issued number, run **after** the final load. | Both | ☐ |
| 7d | **Confirm** `db.greencirclecertified.com` is already a **second Domain Alias** of the `gccwebapp.com` site — this should have been done at **step 3b**, weeks earlier, not here. Check without DNS: `curl.exe -H "Host: db.greencirclecertified.com" http://64.27.52.66/` → expect the site's own `301` to `https://db.greencirclecertified.com/`, not a bare `200`. If it is missing, add it now with step 3's settings. | Plesk / Shawn | ☐ |
| 8 | **The cutover — and it is a delete-then-create, not an edit.** `db` is a **CNAME → `cname.vercel-dns.com`** (verified authoritative 2026-09-21). A CNAME cannot coexist with an A record on the same name, so: **delete the CNAME, then create `db` A → `64.27.52.66`, TTL 300.** Do both in one visit — between the two there is no record at all and the hostname NXDOMAINs. **Write the CNAME target down before deleting it** (`cname.vercel-dns.com`); it is the rollback, and once deleted the zone editor no longer shows you what it was. | DNS holder | ☐ |
| 9 | Issue or confirm TLS for the `db.` alias — a no-op if 7a was done. | Plesk / Shawn | ☐ |
| 10 | **Turn the test gate off:** `TestEnvironmentGate:Enabled` → `false`, then **recycle the app pool**. The middleware binds its options **once in the constructor** and is a singleton — config alone changes nothing. | Shawn | ☐ |
| 11 | **Enable the second rewrite rule:** set `enabled="true"` on `Canonical host - gccwebapp retirement` in `web.config`. `gccwebapp.com` then 301s to `db.` | Shawn | ☐ |
| 12 | Run the full verification set below. | Shawn | ☐ |
| 13 | Announce to **Amazon** (and any other integrated partner) that the base URL is live **and that the GS1 `id` URI changes with it** — audit §3. | GCC | ☐ |
| 14 | Update the Integration Guide: remove the "available at go-live" marker and the NOT-YET-LIVE note in `GCC_Partner_API_Integration_Guide_TEMPLATE_GCC_3.docx`, and confirm the hostname it promises is **`db.`**, not `database.` | Anuki | ☐ |

### Verification — run from outside the network

| Check | Expected |
|---|---|
| `https://db.greencirclecertified.com/api/v1/certificates` anonymous | **401** — not 200, not HTML |
| Same, with a **real Amazon key from an unauthenticated session** | JSON, and GS1 `id` values now read `https://db.greencirclecertified.com/...`. An admin cookie makes the partner middleware stand aside and the 401 then reads like a bad key |
| `https://gccwebapp.com/Certificate/CertificateSingleView/26-0224` | **301** to the same path on `db.` |
| `https://database.greencirclecertified.com/x?q=1` | **301**, path **and** query preserved |
| 20 sampled legacy integer-id URLs | **301** to the correct certificate number |
| `https://db.greencirclecertified.com/robots.txt` | no longer `Disallow: /` |
| `https://db.greencirclecertified.com/appsettings.Production.json` | **must NOT return 200** |
| `https://db.greencirclecertified.com/` anonymous | the public certificate directory — **no `/__gate` redirect** |
| `https://test.gccwebapp.com/` | **still gated.** It is the only test environment now |

---

## Post-go-live

| # | Step | Done |
|---|---|---|
| 15 | Watch IIS logs for 404s on `db.` — unmapped legacy deep links surface here first, and same-day fixes are the plan | ☐ |
| 16 | Ask the client to keep the legacy Vercel project alive ≥ 30 days, reachable by its direct Vercel URL, as a data fallback | ☐ |
| 17 | Raise TTLs back to 3600+ once stable | ☐ |
| 18 | Tighten `AllowedHosts` from `"*"` to an explicit list (audit §1 — deliberately deferred to here) | ☐ |
| 19 | Repoint the ~3,600 `gccwebapp.com` references in `Scripts/smoke-prod/` and `qc*-live-check/` to `test.gccwebapp.com` — **or they follow the 301 into production** | ☐ |
| 20 | Submit the deindex request for the retired hostname — see the SEO deindex playbook | ☐ |
| 21 | Record the issue in `docs/tracking/partner-api-guide-issuance-log.md` | ☐ |
| 22 | Fill in the timestamps above and change this file's status to EXECUTED | ☐ |

---

## Rollback

**Revert the A record.** That is the whole procedure, and it is why the DNS flip is last.

| Failure | Rollback | Recovery |
|---|---|---|
| Phase 2 is wrong | **Delete the `db` A record and recreate the CNAME → `cname.vercel-dns.com`**, TTL 300. **Do NOT roll back to an A record at a Vercel IP.** Vercel serves this host from rotating anycast addresses — `66.33.60.67` / `76.76.21.98` on 2026-09-21, TTL 300, and *not* the `76.76.21.123` these documents claimed for a week. Pinning a stale Vercel IP is a rollback that looks done and serves nothing | one TTL (~5 min) |
| Phase 1 is wrong | `database` A → **`70.32.82.212`**, TTL 300 | one TTL |
| Redirects are wrong | Set the rule's `enabled="false"` on the server **and** fix the repo's `web.config` — otherwise the next publish restores the broken rule | minutes |
| The gate should not have come off | `Enabled: true` **and recycle the app pool** | minutes |
| The data load is wrong | Logical restore — drilled 2026-08-13, 8,823 rows in 24–82 s. **Uploaded PDFs are a separate restore path:** zip `wwwroot/uploads/` first | under an hour |

Neither the Vercel project nor the nginx shim is ever reconfigured by us, so both resume the moment
DNS propagates back. The Plesk aliases and their certificates can be left in place — they do nothing
while DNS points elsewhere, and leaving them means a re-attempt skips the alias and TLS steps.

**Redirects live at the server layer, never in application code**, so a rollback is a config flip
rather than a redeploy.

> ⚠️ **`web.config` is tracked in git and republished on every deploy.** A rule enabled or disabled
> by hand on the server is silently lost at the next publish — the same mechanism that loses SMTP
> environment variables. Any lasting change belongs in the repo.

---

## What changed from the 2026-09-14 version

The original runbook moved **`database.`** to the app and treated `db.` as permanently out of scope.
It also carried a blocker — *"`database.` is NOT free, it belongs to the existing certificate
database"* — and an open question about which hostname the app would take.

Shawn's 2026-09-21 decision answered it: **both** hostnames move to the app, and **`db.` is
canonical**. Consequences, all reflected above:

- `db.` retirement is **in scope at the hostname layer**, so the legacy deep-link map became
  critical-path work instead of a deferred note (audit §10).
- The alias-vs-second-site tension in audit §7/§8 resolves itself: `gccwebapp.com`'s site becomes
  production with two aliases, and `test.gccwebapp.com` (CR-050) is the test environment. **CR-050
  must therefore be complete before step 10**, or there is no gated test environment anywhere.
- `dbtest.greencirclecertified.com` still needs no work — it resolves, its certificate lists the
  name, and `verify-cutover.sh` passed 4/4 on 2026-09-14.

## Explicitly out of scope

- **Decommissioning the legacy Vercel system** — its data export, ownership handover and vendor
  conversation are Phase 2 of the go-live plan. Only its *hostname* moves here.
- **Moving the email sender** to the client domain — `outbound-email-client-domain.md`. Independent
  of the web cutover and deliberately scheduled **after** it, so a mail problem cannot hold go-live.
  The client domain enforces DMARC `p=quarantine` (verified live: `rua=mailto:security@pointsolve.com`),
  so SPF *and* DKIM are mandatory and a misconfigured sender lands in junk while the app reports
  success.
- **Redirecting the legacy `/api/*` bulk endpoints.** `/api/certificates/{id}/download` **is**
  mapped; the unauthenticated `/api/certificates` list and `/api/trpc/*` are **not**. Redirecting
  them would convert a working unauthenticated integration into a 401, which is worse than a clean
  404 with a message.
