"""
QC #164 evidence capture: Status field removed from Product ID Manager surfaces
+ auto-cascade demonstration via Playwright.

Per feedback_playwright_screenshots.md:
- full_page screenshots
- 5 viewports (desktop x2, tablet x2, mobile x1)
- wait for content, not just networkidle

Targets:
- Local dev at http://localhost:5101
- Cert 3 (Number 26-0002, Status: Client Approved - Paid, 2 product IDs)
- Login: analyst@test-gcc.local / TestPass123!
"""
import sys
import time
from pathlib import Path
from playwright.sync_api import sync_playwright, expect

BASE = "http://localhost:5101"
OUT = Path(__file__).parent
CERT_ID = 3
CERT_NUMBER = "26-0002"
EMAIL = "jane.analyst@anukiconsulting.com"
PASSWORD = "TestPass123!"

VIEWPORTS = [
    ("desktop-1920", 1920, 1080),
    ("desktop-1366", 1366, 768),
    ("tablet-land-1024", 1024, 768),
    ("tablet-port-768", 768, 1024),
    ("mobile-375", 375, 667),
]

def login(page):
    page.goto(f"{BASE}/Account/Login", wait_until="domcontentloaded")
    page.locator('input[name="Email"]').fill(EMAIL)
    page.locator('input[name="Password"]').fill(PASSWORD)
    page.locator('button[type="submit"], input[type="submit"]').first.click()
    page.wait_for_load_state("domcontentloaded")
    # Cookie should now be set
    return page.context.cookies()

def shot(page, name, vp):
    path = OUT / f"{name}__{vp}.png"
    page.screenshot(path=str(path), full_page=True)
    print(f"  saved {path.name}")

def run():
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        # First login in a shared context (cookies reused across viewport contexts)
        ctx0 = browser.new_context(viewport={"width": 1920, "height": 1080}, ignore_https_errors=True)
        page0 = ctx0.new_page()
        print("logging in...")
        cookies = login(page0)
        # Sanity check: navigate to cert edit page
        page0.goto(f"{BASE}/Certificate/Edit/{CERT_NUMBER}", wait_until="domcontentloaded")
        page0.wait_for_selector(".product-id-manager", timeout=10000)
        print("on cert edit page, product-id-manager visible")
        ctx0.close()

        for vp_name, w, h in VIEWPORTS:
            print(f"viewport {vp_name} ({w}x{h})...")
            ctx = browser.new_context(viewport={"width": w, "height": h}, ignore_https_errors=True)
            ctx.add_cookies(cookies)
            page = ctx.new_page()

            # A1: cert edit, full page (includes Product ID Manager filter row + table)
            page.goto(f"{BASE}/Certificate/Edit/{CERT_NUMBER}", wait_until="domcontentloaded")
            page.wait_for_selector(".product-id-manager", timeout=10000)
            # Wait for table to populate (filter row visible)
            page.wait_for_selector("#filterByType", timeout=5000)
            time.sleep(0.5)  # let any animations settle
            page.locator(".product-id-manager").scroll_into_view_if_needed()
            time.sleep(0.3)
            shot(page, "A1_pid_manager", vp_name)

            # A2: Add Product ID modal (status field should NOT be present)
            # The Add button is only visible on edit (analyst+ role)
            add_btn = page.locator("#addProductId")
            if add_btn.count() > 0 and add_btn.is_visible():
                add_btn.click()
                page.wait_for_selector("#productIdModal.show", timeout=5000)
                time.sleep(0.5)
                shot(page, "A2_add_modal", vp_name)
                # Close modal so it doesn't bleed into the next capture
                page.locator("#productIdModal .btn-close").click()
                page.wait_for_selector("#productIdModal.show", state="detached", timeout=3000)
            else:
                print(f"  WARN: Add button not visible at {vp_name}")

            ctx.close()

        # B: template CSV download (one viewport is enough — file content is the artifact)
        print("downloading import template...")
        ctx = browser.new_context(viewport={"width": 1366, "height": 768}, ignore_https_errors=True)
        ctx.add_cookies(cookies)
        page = ctx.new_page()
        page.goto(f"{BASE}/Certificate/Edit/{CERT_NUMBER}", wait_until="domcontentloaded")
        page.wait_for_selector("#downloadSample", timeout=10000)
        with page.expect_download() as dl_info:
            page.locator("#downloadSample").click()
        download = dl_info.value
        template_path = OUT / "B_import_template.csv"
        download.save_as(str(template_path))
        print(f"  template saved to {template_path.name}")
        ctx.close()

        browser.close()

if __name__ == "__main__":
    try:
        run()
        print("DONE")
    except Exception as e:
        print(f"FAIL: {e}", file=sys.stderr)
        raise
