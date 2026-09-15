# Client communications during a cutover

Two rules. First, say only what has been tested — "will redirect" is a promise, and
the client repeats it to their own customers. Second, translate every mechanism into
one of two outcomes the client cares about: *nothing breaks before*, *old links keep
working after*. Words that mean nothing to them: DNS, flip, propagate, redirect
(as a noun), A record, CNAME, binding, alias, TLS, certificate (they'll think you
mean their product certificates).

## Confirming the plan (before anything is built)

> Yes, that's the plan. Once we go live, anyone using the old link
> (`old.client.com`, including links to specific records) will automatically land on
> the new site at `new.client.com`. They won't need to update anything.
>
> Until go-live, nothing changes — the old link keeps working exactly as it does
> today. We'll handle the switchover on our side as part of the launch and test it
> first.

## Confirming it's done (after verification script passes)

> The switchover is complete. `new.client.com` is live, and the old link now brings
> people to the new site automatically — including any saved links to individual
> records. We've tested both from outside our network. The old site stays available
> in the background for the next 30 days as a safety net; you won't see it unless
> something goes wrong on our side.

## Explaining a sender-address decision

> Emails from the system will come from `noreply@new.client.com`. We set it up this
> way so it doesn't interfere with your regular company email. If you'd prefer they
> come from `@client.com`, that's possible but needs a small change from whoever
> manages your email — let me know and I'll coordinate it.

## What the client asks you when something's off

| They say | It usually means | Check |
|---|---|---|
| "The new link still shows the old site" | Their browser or DNS cache hasn't refreshed | `dig` from outside; TTL; ask them to try incognito |
| "The old link says not secure" | Old hostname bound without a cert, or cert issuance failed | `curl -sIv https://old...`; Plesk certificate status |
| "Some links go to the home page instead of the record" | Deep-link mapping missed a URL shape | Check 404 logs on new host; add mapping |
| "Emails are going to spam" | SPF/DKIM/PTR gap | Header check per `email.md` |

## The vendor handover request (client sends this, you draft it)

When the old site lives with an outside vendor, the client — as their customer —
makes the request. Draft it for them so nothing is missed:

> Subject: `old.client.com` — transition to new platform
>
> Hi <vendor>,
>
> We're launching a new version of our database on <date>, hosted with
> <PointSolve/Anuki>. To make the transition smooth, we need the following from you:
>
> 1. **Data export** — a full export of the current database (all records, including
>    inactive/hidden ones, and any uploaded files) in CSV/JSON plus the original
>    files, by <date>.
> 2. **URL structure** — a list of the public URL patterns the site uses today
>    (for example, how a single record's link is formed), so existing links can be
>    forwarded correctly.
> 3. **Change freeze** — no data or configuration changes to the current site after
>    <date/time> until we confirm the switch is complete.
> 4. **Keep the site available** for 30 days after <go-live date> so we can fall
>    back if needed. We'll confirm when it can be taken down.
> 5. **Domain handover** — on <go-live date> we will change where
>    `old.client.com` points. Please confirm there's nothing on your side that would
>    interfere, and that we own the domain registration and DNS.
> 6. **Decommission** — after the 30 days, please confirm the site and its data are
>    removed from your systems, and let us know of any final invoices.
>
> Thanks,
> <client name>

Adjust items 2 and 5 depending on whether the old hostname's DNS is already under
the client's control (it usually is — the vendor just hosts). If the vendor controls
the DNS zone, item 5 becomes a transfer request and moves to the top of the list.
