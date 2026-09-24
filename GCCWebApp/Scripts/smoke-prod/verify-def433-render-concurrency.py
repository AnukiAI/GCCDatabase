#!/usr/bin/env python3
"""
DEF-433 verification -- does the site stay up while the anonymous PDF routes are hammered?

WHAT THIS PROVES, AND WHY A TIMING RUN CANNOT
---------------------------------------------
DEF-433 is not "the preview is slow". It is: the two [AllowAnonymous] PDF routes serialise on a
process-wide lock, that lock was taken with a synchronous timeout-free Wait(), and nothing rate
limited them. Each waiter parked a thread-pool thread; injection is ~1-2 threads/sec; the queue
outran it and the WHOLE SITE stopped -- login, tile views and the Amazon partner API included.

So the assertion is NOT about how fast the PDF routes are. It is about whether an UNRELATED route
stays responsive while they are under load. A single-request timing run measures the one thing that
was never the problem.

    baseline   : control route latency, quiet
    storm      : N concurrent requests at the PDF route, released SIMULTANEOUSLY
    during     : control route latency, measured DURING the storm
    assertion  : `during` p95 must not collapse relative to `baseline` p95

PASS means a burst degrades previews and nothing else. FAIL means a burst takes the site with it.

=====================================================================================
A PASS FROM THIS SCRIPT IS WORTHLESS ON ITS OWN. READ THIS.
=====================================================================================
On 2026-09-21 this check was run twice: once against the fixed code (PASS) and once against the
pre-fix code with the blocking Wait() restored as a NEGATIVE CONTROL -- and the negative control
**also passed**. A check that passes on code known to be broken discriminates nothing, and the
first version of this script was therefore not a gate, only a reassurance.

Two defects in the harness itself were identified as the likely cause (2026-09-22):

  1. THE LOAD GENERATOR WAS THE BOTTLENECK. The storm threads were created and started in a
     Python loop, each building its own urllib Request after starting. Creating N threads and N
     requests takes long enough that the requests TRICKLE IN rather than arriving together, so
     the server drained them as they landed and true concurrency was never reached. Fixed by a
     threading.Barrier: every worker builds its request, then blocks, and all are released in one
     instant.
  2. THE CONTROL SAMPLER STARTED TOO LATE. It was started after the loop that starts all the storm
     threads, so on a fast box it could begin sampling after the peak had already passed -- and
     measure the recovery rather than the outage. It now starts first and samples continuously
     until the storm ends.

**Those two changes are a hypothesis about why the negative control passed, not a proven fix.**
Until this script FAILS against pre-fix code, DEF-433 remains fixed-in-principle only. Use --expect
to record the protocol explicitly:

    # 1. negative control -- restore the blocking Wait(), then:
    python ...verify-def433-render-concurrency.py --cert-id N --expect fail
    # 2. restore the fix, then:
    python ...verify-def433-render-concurrency.py --cert-id N --expect pass

Both must exit 0. Run 1 exiting non-zero means the harness STILL does not discriminate, and no
conclusion about the fix may be drawn from run 2.

IF IT STILL WILL NOT REPRODUCE
------------------------------
The box is too fast: a 4-page PDF opens in ~30 ms warm, so the queue drains faster than it builds.
Escalate in this order -- each models the shared Plesk host more closely than the last:

  a. --concurrency 400 (or higher). The fault is a function of queue depth vs thread injection.
  b. A bigger fixture. See docs/tracking/2026-09-18-multipage-certificate-pdfs.csv; the file SIZE
     matters more than the page count for how long one open holds the lock.
  c. Constrain the thread pool, which is the condition the production box actually has. Launch the
     app under a capped pool so parked threads cannot simply be replaced:
         $env:DOTNET_gcServer=0
         dotnet run --urls "http://localhost:5101"
     and set System.Threading.ThreadPool.MaxThreads low in the generated runtimeconfig.json in
     bin/. DO NOT add this to runtimeconfig.template.json -- that ships.

SAFETY
------
This generates load. It refuses to run against anything but localhost unless --i-know-what-im-doing
is passed, because pointing it at the test or live host is a self-inflicted outage -- which is
precisely the fault being tested for.

USAGE
-----
    dotnet run --urls "http://localhost:5101"      # in another terminal, and NOT while building

    python Scripts/smoke-prod/verify-def433-render-concurrency.py --cert-id 615
    python Scripts/smoke-prod/verify-def433-render-concurrency.py --cert-id 615 --concurrency 400

Exit 0 = the expectation set by --expect was met (default --expect pass).
"""

import argparse
import json
import sys
import threading
import time
import urllib.error
import urllib.request
from collections import Counter
from urllib.parse import urlparse

CONTROL_PATH = "/Account/Login"   # cheap, anonymous, touches nothing PDF-related


def fetch(url, timeout, cookie=None, prebuilt=None):
    """Return (status, seconds). status is an int, or a string for transport failures."""
    req = prebuilt if prebuilt is not None else _build(url, cookie)
    t0 = time.perf_counter()
    try:
        with urllib.request.urlopen(req, timeout=timeout) as r:
            r.read()
            return r.getcode(), time.perf_counter() - t0
    except urllib.error.HTTPError as e:
        try:
            e.read()
        except Exception:
            pass
        return e.code, time.perf_counter() - t0
    except Exception as e:
        return type(e).__name__, time.perf_counter() - t0


def _build(url, cookie):
    req = urllib.request.Request(url)
    if cookie:
        req.add_header("Cookie", cookie)
    return req


def probe_pagecount(url, timeout, cookie=None):
    """Return (status, seconds, pages|None). Reads the body, unlike fetch()."""
    req = _build(url, cookie)
    t0 = time.perf_counter()
    try:
        with urllib.request.urlopen(req, timeout=timeout) as r:
            body = r.read().decode("utf-8", "replace")
            secs = time.perf_counter() - t0
            try:
                return r.getcode(), secs, int(json.loads(body).get("pages"))
            except Exception:
                return r.getcode(), secs, None
    except urllib.error.HTTPError as e:
        return e.code, time.perf_counter() - t0, None
    except Exception as e:
        return type(e).__name__, time.perf_counter() - t0, None


def percentile(xs, p):
    if not xs:
        return float("nan")
    xs = sorted(xs)
    k = max(0, min(len(xs) - 1, int(round((p / 100.0) * (len(xs) - 1)))))
    return xs[k]


def sample_control_until(base, timeout, cookie, out, stop, max_samples):
    """Sample the control route continuously until `stop` is set, or max_samples is reached.

    DEF-433 harness fix (2026-09-22): the old version took a fixed number of samples starting
    AFTER every storm thread had been started, which on a fast box could measure the recovery
    instead of the outage. This starts before the storm is released and keeps sampling through it.
    """
    while not stop.is_set() and len(out) < max_samples:
        out.append(fetch(base + CONTROL_PATH, timeout, cookie))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--base", default="http://localhost:5101")
    ap.add_argument("--cert-id", type=int, required=True,
                    help="integer Id of a certificate that HAS a PDF (these routes take Id, not number)")
    ap.add_argument("--concurrency", type=int, default=200)
    ap.add_argument("--timeout", type=float, default=20.0)
    ap.add_argument("--control-samples", type=int, default=20)
    ap.add_argument("--cookie", help="gate cookie, if the target is gated")
    ap.add_argument("--max-slowdown", type=float, default=8.0,
                    help="how many times baseline p95 the control p95 may reach before this fails")
    ap.add_argument("--expect", choices=("pass", "fail"), default="pass",
                    help="'fail' asserts the site DOES collapse -- use it for the negative-control "
                         "run against pre-fix code. Exit 0 means the expectation was met.")
    ap.add_argument("--routes", choices=("both", "count", "image"), default="both",
                    help="which anonymous PDF route to storm. 'count' is the one that carried the "
                         "BLOCKING Wait() and is therefore the defect's actual path; 'image' renders "
                         "and is far slower per request, so a mixed storm saturates the renderer and "
                         "never builds a queue on the lock. Use 'count' to reproduce DEF-433.")
    ap.add_argument("--i-know-what-im-doing", action="store_true")
    args = ap.parse_args()

    host = (urlparse(args.base).hostname or "").lower()
    if host not in ("localhost", "127.0.0.1", "::1") and not args.i_know_what_im_doing:
        sys.exit("REFUSING: %s is not localhost. This script generates the exact load DEF-433 is "
                 "about. Pass --i-know-what-im-doing only if you mean it." % args.base)

    base = args.base.rstrip("/")
    count_url = "%s/Certificate/CertificatePageCount/%d" % (base, args.cert_id)
    image_url = "%s/Certificate/ViewCertificateImage/%d?page=1" % (base, args.cert_id)

    print("DEF-433 concurrency check")
    print("  target      : %s" % base)
    print("  certificate : Id %d" % args.cert_id)
    print("  concurrency : %d  (released simultaneously via barrier)" % args.concurrency)
    print("  routes      : %s" % args.routes)
    print("  control     : %s" % CONTROL_PATH)
    print("  expecting   : %s" % args.expect.upper())
    if args.expect == "fail":
        print("  NOTE: this is a NEGATIVE-CONTROL run. It asserts the site DOES collapse.")
        print("        It is meaningful only against pre-fix code (blocking Wait() restored).")

    # -- 0. the target must actually exercise the fault -------------------------------------
    #
    # A 200 is NOT enough. GetPdfPageCountAsync returns 0 and never touches the lock when the
    # file is absent from disk -- and most seeded dev certificates have no PDF, so the obvious
    # fixture yields `200 {"pages":0}` and a storm that proves precisely nothing. Require a real,
    # multi-page PDF, so the requests actually reach the contended path.
    status, secs, pages = probe_pagecount(count_url, args.timeout, args.cookie)
    print("\n[0] page-count route: %s in %.3fs -> pages=%s" % (status, secs, pages))
    if status != 200:
        sys.exit("FATAL: page count returned %s. Sign-in gate, wrong Id, or the app is not running.\n"
                 "       A storm against a 404 proves nothing -- it never reaches the lock." % status)
    # pages==0 is always fatal: it means no readable PDF on disk, so the route returns early and
    # NEVER takes the lock -- the storm would pass without exercising the fault at all.
    #
    # pages==1 is fatal only when the storm drives the RENDER route, which needs a second page to
    # page to. CertificatePageCount opens the PDF and takes the lock regardless of page count, so a
    # single-page document is a perfectly good --routes count fixture -- and on 2026-09-22 the best
    # one available was exactly that: Id 672 (26-0147), 2.34 MB, one page, 0.617s per count against
    # ~0.030s for the 4-page cert 615. Lock HOLD TIME is what builds the queue, and hold time tracks
    # file size and complexity, not page count. Rejecting it cost three failed reproduction attempts.
    min_pages = 1 if args.routes == "count" else 2
    if not pages or pages < min_pages:
        sys.exit("FATAL: certificate Id %d reports pages=%s (need >= %d for --routes %s).\n"
                 "       0 means there is no readable PDF on disk for it -- the route returns\n"
                 "       early and NEVER takes the render lock, so this test would pass without\n"
                 "       exercising the fault at all. 1 means there is nothing to page to, which\n"
                 "       only matters when the render route is in the mix.\n"
                 "       See docs/tracking/2026-09-18-multipage-certificate-pdfs.csv, and prefer\n"
                 "       the LARGEST file available -- hold time, not page count, builds the queue."
                 % (args.cert_id, pages, min_pages, args.routes))

    # -- 1. baseline ------------------------------------------------------------------------
    quiet = []
    for _ in range(args.control_samples):
        quiet.append(fetch(base + CONTROL_PATH, args.timeout, args.cookie))
    quiet_ok = [s for st, s in quiet if st == 200]
    if not quiet_ok:
        sys.exit("FATAL: control route %s never returned 200 while quiet: %s"
                 % (CONTROL_PATH, Counter(st for st, _ in quiet)))
    base_p50, base_p95 = percentile(quiet_ok, 50), percentile(quiet_ok, 95)
    print("\n[1] baseline control latency  p50 %.3fs   p95 %.3fs   (n=%d)"
          % (base_p50, base_p95, len(quiet_ok)))

    # -- 2. storm, released simultaneously, with the control measured DURING it ---------------
    #
    # The barrier is the point. Previously the threads were started in a loop and each built its
    # request after starting, so on a fast box the requests trickled in and the server drained
    # them as they arrived -- true concurrency was never achieved and the negative control passed.
    storm, during = [], []
    lock = threading.Lock()
    stop = threading.Event()
    gate = threading.Barrier(args.concurrency + 1)

    def worker(url):
        req = _build(url, args.cookie)        # build BEFORE the barrier, not after
        try:
            gate.wait(timeout=60)
        except threading.BrokenBarrierError:
            return
        r = fetch(url, args.timeout, args.cookie, prebuilt=req)
        with lock:
            storm.append(r)

    # Which route the storm drives is not cosmetic -- it decides whether the fault can appear.
    #
    # A 50/50 mix looks like the fairer test and is the reason three reproduction attempts failed
    # on 2026-09-22. ViewCertificateImage RENDERS: it is an order of magnitude slower per request,
    # so it monopolises the storm, 60-100 of 200 requests hit the client timeout, and the cheap
    # CertificatePageCount requests sail through in between. The lock queue never builds -- proven
    # by `PDF lock not acquired` appearing ZERO times in the fixed build's log, where it fires on
    # every 5s bound hit. CertificatePageCount is the route that carried the blocking Wait(), so
    # --routes count points the storm at the defect instead of at the renderer.
    def pick(i):
        if args.routes == "count":
            return count_url
        if args.routes == "image":
            return image_url
        return count_url if i % 2 else image_url

    threads = []
    for i in range(args.concurrency):
        t = threading.Thread(target=worker, args=(pick(i),))
        t.daemon = True
        threads.append(t)
        t.start()

    # Control sampling starts BEFORE the storm is released, so it spans the peak.
    ctl = threading.Thread(target=sample_control_until,
                           args=(base, args.timeout, args.cookie, during, stop,
                                 args.control_samples))
    ctl.daemon = True
    ctl.start()

    print("\n[2] %d workers staged; releasing..." % args.concurrency)
    t0 = time.perf_counter()
    try:
        gate.wait(timeout=60)
    except threading.BrokenBarrierError:
        sys.exit("FATAL: could not stage %d workers within 60s -- the load generator itself "
                 "failed, not the server." % args.concurrency)
    for t in threads:
        t.join()
    elapsed = time.perf_counter() - t0
    stop.set()
    ctl.join(timeout=args.timeout + 5)

    print("    storm finished in %.2fs" % elapsed)
    print("    PDF-route responses: %s"
          % ", ".join("%s=%d" % (k, v) for k, v in sorted(Counter(st for st, _ in storm).items(),
                                                          key=lambda kv: str(kv[0]))))

    during_ok = [s for st, s in during if st == 200]
    during_bad = [st for st, _ in during if st != 200]

    collapsed = False
    ratio = float("nan")

    if not during_ok:
        print("    control during storm: NO SUCCESSFUL RESPONSES -- %s" % Counter(during_bad))
        print("    => the site stopped answering an unrelated route. That is DEF-433 exactly.")
        collapsed = True
    else:
        d_p50, d_p95 = percentile(during_ok, 50), percentile(during_ok, 95)
        print("    control during storm   p50 %.3fs   p95 %.3fs   (n=%d, %d non-200)"
              % (d_p50, d_p95, len(during_ok), len(during_bad)))

        # -- 3. verdict ----------------------------------------------------------------------
        # A floor stops a very fast baseline making any load look catastrophic.
        allowed = max(base_p95 * args.max_slowdown, 1.0)
        ratio = d_p95 / base_p95 if base_p95 > 0 else float("inf")

        print("\n[3] control p95 %.3fs vs allowed %.3fs  (%.1fx baseline, limit %.1fx)"
              % (d_p95, allowed, ratio, args.max_slowdown))
        collapsed = d_p95 > allowed

    throttled = sum(1 for st, _ in storm if st == 429)
    if throttled:
        print("    %d of %d PDF requests were rate limited (429) -- the DEF-433 rules are active"
              % (throttled, len(storm)))
    else:
        print("    NOTE: no 429s. Either the limit was not reached, or IpRateLimiting is not")
        print("          loaded in this environment -- the rules live in appsettings.Production.json,")
        print("          so a Development run will not have them. The lock fix is still exercised.")

    if during_bad:
        print("    NOTE: %d control requests did not return 200: %s" % (len(during_bad), Counter(during_bad)))

    # -- 4. did reality match the expectation? -------------------------------------------------
    observed = "fail" if collapsed else "pass"
    print("\nobserved: site %s under load  |  expected: %s"
          % ("COLLAPSED" if collapsed else "STAYED UP", args.expect.upper()))

    if observed == args.expect:
        if args.expect == "pass":
            print("\nPASS -- %d concurrent PDF requests did not take the rest of the site with them."
                  % args.concurrency)
            print("        Valid ONLY if a --expect fail run against pre-fix code also exited 0.")
        else:
            print("\nPASS (negative control) -- the harness reproduces DEF-433 on broken code.")
            print("        It can therefore discriminate, and a subsequent --expect pass run means "
                  "something.")
        sys.exit(0)

    if args.expect == "fail":
        print("\nFAIL -- the harness did NOT reproduce the fault on code that is known to be broken.")
        print("        It still does not discriminate. Do not draw any conclusion about the fix.")
        print("        Escalate: --concurrency 400+, a larger PDF fixture, or a capped thread pool")
        print("        (see 'IF IT STILL WILL NOT REPRODUCE' at the top of this file).")
        sys.exit(1)

    print("\nFAIL -- an unrelated route degraded %.1fx under PDF load." % ratio)
    sys.exit(1)


if __name__ == "__main__":
    main()
