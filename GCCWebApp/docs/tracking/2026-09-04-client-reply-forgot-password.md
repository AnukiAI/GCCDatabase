# Client reply — Forgot Password QC (9/3)

*Draft for Shawn. Paste-ready. Internal notes are in the `<!-- -->` blocks and the "Not for the
client" section at the bottom — strip both before sending.*

---

Thanks for running through the forgot-password flow, and good to hear the reset itself worked
end to end.

All three items are fixed and verified. They are ready to go out with the next release to the test
site — I'll confirm here once they're live so you can re-check.

**1. "Expires in 24 hours" vs "expires in 1 hour"**

The email was correct. Reset links really do expire after **1 hour**, so the webpage was the one
giving the wrong number. I've corrected the page to say 1 hour and left the email and the actual
link lifetime as they were.

I want to flag the reasoning, because the fix could have gone the other way: making the page true by
extending links to 24 hours would have meant a live reset link sitting in an inbox all day. One hour
is the safer window, so I corrected the wording rather than loosening the timeout. Happy to change
the window if you'd prefer a longer one — just say so and we'll treat it as its own change.

**2. Background green on the reset screen**

Done — the Forgot Password and Reset Password screens now use the same lighter green as the sign-in
page, so the logo separates from the background properly.

Worth knowing why it happened: these pages were each carrying their own private copy of the
background colour instead of using the shared one. I removed those copies rather than just
re-typing a lighter green, so the reset screens now pull from the same single definition as sign-in
and can't quietly drift apart again.

**3. "Access your sustainability certification database."**

Removed from all three Forgot Password screens. Sign In has its own separate line under the logo,
which I've left as it is — let me know if you'd like that one looked at too.

---

**Your two go-live questions**

**Will the sender address change to the production domain?**

Yes. It's a configuration setting, so it changes without any code work — but there's a sequencing
point worth planning for: **the new mailbox has to exist on the production domain before we switch
the setting.** The mail server checks that the sending address is a real mailbox it owns, so if we
point it at an address that hasn't been created yet, password-reset emails stop sending altogether.
So the order at go-live is: create the mailbox, then flip the setting, then send a test reset.

**Will the reset links point to the production URL?**

Yes, automatically — no change needed. The links aren't hard-coded to any domain; they're built from
whatever address the user is actually visiting. Once the site is live on the production domain, the
links in the emails will use it. We'll still send a live test reset as part of go-live checks to
confirm it end to end.

**On the email landing in spam**

Thanks for calling it out — that's useful. It's expected behaviour for a brand-new sending domain
with no reputation built up yet, rather than a fault in the reset itself. Before go-live we'll get
the domain's mail authentication records (SPF, DKIM, DMARC) set up on the production domain, which
is what tells the receiving mail providers our messages are legitimate. That's the main lever for
inbox placement. It's worth setting expectations that reputation builds over the first few weeks of
real sending, so it may not be perfect on day one.

---

<!-- ============ NOT FOR THE CLIENT — strip before sending ============ -->

## Not for the client

**Tracking:** DEF-405 (expiry copy), DEF-406 (gradient), DEF-407 (tagline removal). All three
FIXED, **not yet deployed** — do not tell the client they are live until they ship.

**Verified on localhost:5101, 2026-09-04**, against the rendered DOM rather than the source:
computed `.login-left` background on Login / ForgotPassword / ResetPassword all resolve to
`rgb(146,200,62) -> rgb(68,118,56)`; `.login-brand p` count = 0 on both forgot screens; confirmation
page reads "Reset links expire after 1 hour for your security".

**`SignUp.cshtml` — parked, not orphaned.** Its gradient override was removed too, so it now matches
the other auth screens. `/Account/SignUp` returns 404 **by design**: the client asked for the screen
to be built ahead of a future self-registration feature, so it stays in the repo and stays
unreachable. No decision needed and nothing to raise with the client. The file now carries a
DO-NOT-DELETE header so a future cleanup sweep doesn't propose removing it (I nearly did).

**Spam/SPF:** I have **not** verified what SPF/DKIM/DMARC records exist on the production domain —
the reply commits us to setting them up, not to a current state. Confirm before go-live so the
promise is deliverable.

**Open question the client didn't ask but will hit:** the reply says the sender changes at go-live.
Nobody has decided *what* the production sender address should be (`noreply@`? `support@`?). Worth
settling before cutover, since it drives the mailbox that has to be created.
