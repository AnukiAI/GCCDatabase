# Client-capture guide — "white screen after sorting Certificates"

**Origin:** client report (2026-07-16): *"Sorting Certificates by oldest effective date made the screen go white but results were still there — clicked on screen and it opened one of the results. Refreshed the blank results page and the certificates showed up."*

## Why we need a capture

The symptom — the results area goes **white**, but the rows are still in the page (a click lands on a certificate) and a **refresh fixes it** — is the classic signature of a **stalled GPU/compositor paint**: the browser updated the page but didn't repaint that region until something forced a reflow (click / scroll / refresh).

We could **not** reproduce it in automation, and we tried hard:
- desktop width (1440×900), the real sticky-header table
- CPU throttled 4× to widen timing-race windows
- four mechanisms (repeat re-sort, deep-scroll then sort, sort while tab hidden, rapid double-sort)
- **both** headless **and** a real-GPU headed browser
- 20+ checks, screenshots inspected — all rendered fully, no blank

That points to something **specific to the machine/GPU/browser** it happened on, which only a capture from that environment can pin down. (We also shipped two safety changes anyway — a defensive repaint nudge after each sort, and a fix for sorts being silently dropped — see DEF-309.)

## What to capture *if it happens again*

Grab these **at the moment the screen is white** (before refreshing, if possible):

1. **A screenshot of the white screen** (full window).
2. **Browser + version** (e.g. Chrome 126, Edge, Safari, Firefox) and **OS** (Windows 11 / macOS).
3. **GPU info** — in Chrome/Edge open a new tab, go to `chrome://gpu`, and screenshot the top "Graphics Feature Status" section. (This is the single most useful item for a compositor bug.)
4. **Console errors** — press **F12** → **Console** tab → screenshot anything red.
5. **Which view** it was in — **Tile** (cards) or **List** (table).
6. **Screen size / zoom** — roughly how wide the window was, and browser zoom level (Ctrl+0 resets to 100%).
7. **How many certificates were loaded / how far scrolled** — had you scrolled down a lot before sorting?
8. **Does it repeat?** — same sort again on the same machine: does it go white every time, sometimes, or was it a one-off?

## Quick things the client can try if it recurs

- **Scroll or click once** — if the rows reappear, that confirms it's a paint-flush (harmless data-wise; the results were always there).
- **Hard refresh** (Ctrl+F5).
- If it's frequent on one machine: in Chrome/Edge, `Settings → System → "Use graphics acceleration when available"` — toggling this **off** is a known workaround for GPU-compositor glitches (useful as a diagnostic; not a fix we'd ship).

Send items 1–8 to the team and we can target the exact cause instead of guessing.
