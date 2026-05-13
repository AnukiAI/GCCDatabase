"""
DEF-191 verification: Product ID Import Preview, partial selection with
Replace-all OFF.

Bug (pre-fix): selecting a preview row whose CSV values exactly matched an
existing PID on the certificate (action='skip') caused the bulk POST to be
rejected by the server with "Product ID already exists on this certificate".
The user saw a generic "Import failed" toast even though the row they checked
represented a no-op by definition.

Fix: in wwwroot/js/product-id-manager.js applyImport(), the partition that
split selected preview rows for the bulk POST changed from
    r.action !== 'update'
to
    r.action === 'create'
plus a new `skippedExisting` partition and a friendly success branch for
skip-only selections.

Four scenarios on cert 26-0023 (analyst login), all with Replace-all OFF:
  A) skip-only           : check 1 unchanged row   -> "X row(s) already on this certificate — nothing to change"
  B) modified-only       : check 1 modified row    -> "Updated 1 product ID(s)"
  C) new-only            : check 1 new row         -> "Imported 1 product ID(s)"
  D) mixed update + skip : check 1 update + 1 skip -> "Updated 1 product ID(s); 1 already on file"

Captures a screenshot of scenario A (the previously-broken case) as visible
proof that the failure toast is gone and is replaced by a success modal.

Usage:
  py -3 verify_def191.py
Requires the local dev server on http://localhost:5101 and seeded analyst user.
"""
import sys
import time
from pathlib import Path
from playwright.sync_api import sync_playwright

BASE = "http://localhost:5101"
EMAIL = "jane.analyst@anukiconsulting.com"
PASSWORD = "TestPass123!"
CERT_NUMBER = "26-0023"
OUT = Path(__file__).parent


def login(page):
    page.goto(f"{BASE}/Account/Login", wait_until="domcontentloaded")
    page.locator('input[name="Email"]').fill(EMAIL)
    page.locator('input[name="Password"]').fill(PASSWORD)
    page.locator('button[type="submit"], input[type="submit"]').first.click()
    page.wait_for_load_state("domcontentloaded")


def get_existing_pid(page, cert_id, index=0):
    resp = page.request.get(f"{BASE}/api/productids/certificate/{cert_id}")
    body = resp.json()
    data = body.get("data") or []
    if len(data) <= index:
        return None
    p = data[index]
    return {
        "type": p.get("productIdType"),
        "value": p.get("productIdValue"),
        "name": p.get("productName") or "",
        "sku": p.get("productSku") or "",
        "partner": p.get("partner") or "",
    }


def find_unique_new_value(page, cert_id):
    """Return a 10-char ASIN that is NOT already on the cert."""
    resp = page.request.get(f"{BASE}/api/productids/certificate/{cert_id}")
    existing = {(p.get("productIdValue") or "").upper() for p in (resp.json().get("data") or [])}
    suffix_seed = int(time.time())
    for i in range(20):
        candidate = f"X{suffix_seed + i:09d}"[:10].ljust(10, "Z")
        if candidate.upper() not in existing:
            return candidate
    raise RuntimeError("could not find unique ASIN value for test")


def run_scenario(page, name, csv_text, keep_indexes, screenshot_after_toast=False):
    page.goto(f"{BASE}/Certificate/Edit/{CERT_NUMBER}", wait_until="domcontentloaded")
    page.wait_for_selector(".product-id-manager", timeout=10000)
    page.wait_for_function(
        "window.productIdManager && typeof window.productIdManager.parseAndPreviewImport === 'function'",
        timeout=10000,
    )

    page.evaluate("(csv) => window.productIdManager.parseAndPreviewImport(csv)", csv_text)
    page.wait_for_selector(
        "#import-preview-modal[style*='display: block'], #import-preview-modal[style*='display:block']",
        timeout=5000,
    )

    page.evaluate(
        """
        (keepIndexes) => {
            window.__capturedToasts = [];
            const errFn = window.productIdManager.showError.bind(window.productIdManager);
            const sucFn = window.productIdManager.showSuccess.bind(window.productIdManager);
            window.productIdManager.showError = (m) => { window.__capturedToasts.push({kind: 'error', text: m}); errFn(m); };
            window.productIdManager.showSuccess = (m) => { window.__capturedToasts.push({kind: 'success', text: m}); sucFn(m); };
            window.productIdManager.importPreviewData.forEach((r, idx) => {
                if (r.status === 'error') return;
                r.selected = keepIndexes.includes(idx);
                const cb = document.querySelector(`.import-row-checkbox[data-index='${idx}']`);
                if (cb) cb.checked = r.selected;
            });
            const t = document.getElementById('overwrite-existing-toggle');
            if (t) t.checked = false;
            window.productIdManager.updateApplyButtonState();
        }
        """,
        keep_indexes,
    )

    page.locator("#apply-import-btn").click()
    try:
        page.wait_for_function(
            "() => (window.__capturedToasts || []).length > 0",
            timeout=15000,
        )
    except Exception:
        pass

    toasts = page.evaluate("() => window.__capturedToasts || []")

    if screenshot_after_toast:
        page.wait_for_timeout(400)
        page.screenshot(path=str(OUT / f"{name}_toast.png"), full_page=False)

    return toasts


def main():
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        ctx = browser.new_context(viewport={"width": 1366, "height": 900}, ignore_https_errors=True)
        page = ctx.new_page()

        login(page)

        page.goto(f"{BASE}/Certificate/Edit/{CERT_NUMBER}", wait_until="domcontentloaded")
        cert_id = page.evaluate(
            "() => window.productIdManager && window.productIdManager.certificateId"
        )
        if not cert_id:
            print("FAIL: could not resolve certificate id")
            sys.exit(2)

        existing = get_existing_pid(page, cert_id, index=0)
        new_value = find_unique_new_value(page, cert_id)

        header = "Type,Product ID,Product Name,SKU,Partner,Description"
        unchanged_row = f"{existing['type']},{existing['value']},{existing['name']},{existing['sku']},{existing['partner']},"
        modified_row  = f"{existing['type']},{existing['value']},{existing['name']}-EDITED,{existing['sku']},{existing['partner']},"
        new_row       = f"ASIN,{new_value},NewWidget,NEW{new_value},Amazon,from-verify"

        results = []

        # A: skip-only -> previously broken
        toasts_a = run_scenario(
            page, "A_skip_only",
            "\n".join([header, unchanged_row]),
            keep_indexes=[0],
            screenshot_after_toast=True,
        )
        ok_a = (
            len(toasts_a) == 1
            and toasts_a[0]["kind"] == "success"
            and "already on this certificate" in toasts_a[0]["text"]
        )
        results.append(("A skip-only", ok_a, toasts_a))

        # B: modified-only -> Updated 1
        toasts_b = run_scenario(
            page, "B_modified_only",
            "\n".join([header, modified_row]),
            keep_indexes=[0],
        )
        ok_b = (
            len(toasts_b) == 1
            and toasts_b[0]["kind"] == "success"
            and "Updated 1" in toasts_b[0]["text"]
        )
        results.append(("B modified-only", ok_b, toasts_b))

        # C: new-only -> Imported 1
        toasts_c = run_scenario(
            page, "C_new_only",
            "\n".join([header, new_row]),
            keep_indexes=[0],
        )
        ok_c = (
            len(toasts_c) == 1
            and toasts_c[0]["kind"] == "success"
            and "Imported 1" in toasts_c[0]["text"]
        )
        results.append(("C new-only", ok_c, toasts_c))

        # D: mixed update + skip — use two DIFFERENT existing PIDs to keep the
        # classification stable regardless of prior scenarios mutating state.
        resp = page.request.get(f"{BASE}/api/productids/certificate/{cert_id}")
        all_pids = resp.json().get("data") or []
        if len(all_pids) < 11:
            print("FAIL: cert needs at least 11 existing PIDs for scenario D")
            sys.exit(2)
        d_update_pid = all_pids[5]
        d_skip_pid   = all_pids[10]
        suffix = str(int(time.time()))
        d_update_row = (f"{d_update_pid['productIdType']},{d_update_pid['productIdValue']},"
                        f"{(d_update_pid.get('productName') or '')}-VERIFY-{suffix},"
                        f"{(d_update_pid.get('productSku') or '')},{(d_update_pid.get('partner') or '')},")
        d_skip_row = (f"{d_skip_pid['productIdType']},{d_skip_pid['productIdValue']},"
                      f"{(d_skip_pid.get('productName') or '')},"
                      f"{(d_skip_pid.get('productSku') or '')},{(d_skip_pid.get('partner') or '')},")
        toasts_d = run_scenario(
            page, "D_mixed_update_plus_skip",
            "\n".join([header, d_update_row, d_skip_row]),
            keep_indexes=[0, 1],
        )
        ok_d = (
            len(toasts_d) >= 1
            and toasts_d[-1]["kind"] == "success"
            and "Updated" in toasts_d[-1]["text"]
            and "Import failed" not in toasts_d[-1]["text"]
        )
        results.append(("D mixed update+skip", ok_d, toasts_d))

        print("=" * 64)
        print("DEF-191 verification — Product ID Import Preview, Replace-all OFF")
        print("=" * 64)
        print(f"Cert: {CERT_NUMBER} (id={cert_id})")
        print()
        all_ok = True
        for name, ok, toasts in results:
            mark = "PASS" if ok else "FAIL"
            if not ok:
                all_ok = False
            text_summary = (toasts[-1]["text"] if toasts else "(no toast)")
            print(f"  [{mark}] {name}: {text_summary}")
        print()
        print(f"Saved scenario A toast screenshot: {OUT / 'A_skip_only_toast.png'}")
        print()
        print("RESULT:", "PASS" if all_ok else "FAIL")
        browser.close()
        sys.exit(0 if all_ok else 1)


if __name__ == "__main__":
    main()
