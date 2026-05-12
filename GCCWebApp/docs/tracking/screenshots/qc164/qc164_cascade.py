"""
QC #164 cascade evidence: change cert Status via the Edit form,
observe child product-ID statuses follow.

Sequence on cert 26-0002 (id=3) which has 2 product IDs (baseline: CAP / Inactive):
  T0  snapshot: CAP / Inactive (stale dev data)
  T1  Edit → Status='Hold'         → SAVE → expect: Hold / Inactive (cascade no-op, both inactive)
  T2  Edit → Status='Client Approved - Paid' → SAVE → expect: CAP / Active (UP-cascade)
  T3  Edit → Status='Hold'         → SAVE → expect: Hold / Inactive (DOWN-cascade)
  T4  Edit → Status='Client Approved - Paid' → SAVE → expect: CAP / Active (final restored state)

Writes the run log to cascade_log.txt with each transition's before/after rows.
"""
import subprocess
import time
from pathlib import Path
from playwright.sync_api import sync_playwright

BASE = "http://localhost:5101"
OUT = Path(__file__).parent
CERT_NUMBER = "26-0002"
EMAIL = "jane.analyst@anukiconsulting.com"
PASSWORD = "TestPass123!"
LOG = OUT / "cascade_log.txt"
LOG.write_text("")  # reset

def log(msg):
    print(msg)
    with LOG.open("a", encoding="utf-8") as f:
        f.write(msg + "\n")

def sql_snapshot(label):
    cmd = [
        "sqlcmd", "-S", "ANUKI01", "-d", "GCCDatabase_Dev",
        "-U", "GCC_DevUser", "-P", "DevP@ss2024!SQL",
        "-Q", "SET NOCOUNT ON; SELECT c.Status AS CertStatus, p.Id AS PidId, p.ProductIdValue, p.Status AS PidStatus FROM Certificates c JOIN CertificateProductIds p ON p.CertificateId=c.Id WHERE c.Id=3 ORDER BY p.Id;",
        "-W", "-s", "|"
    ]
    out = subprocess.run(cmd, capture_output=True, text=True).stdout
    log(f"\n--- {label} ---")
    log(out.strip())
    return out

def login(page):
    page.goto(f"{BASE}/Account/Login", wait_until="domcontentloaded")
    page.locator('input[name="Email"]').fill(EMAIL)
    page.locator('input[name="Password"]').fill(PASSWORD)
    page.locator('button[type="submit"], input[type="submit"]').first.click()
    page.wait_for_load_state("domcontentloaded")

def change_status(page, new_status):
    page.goto(f"{BASE}/Certificate/Edit/{CERT_NUMBER}", wait_until="domcontentloaded")
    page.wait_for_selector("#Status", timeout=10000)
    # Use select_option which works for native <select>
    page.select_option("#Status", label=new_status)
    time.sleep(0.3)
    # Find Save button — should be a submit button on the form
    save = page.locator('button:has-text("Save Changes")').first
    save.click()
    # Wait for navigation/redirect or success notification
    page.wait_for_load_state("domcontentloaded")
    time.sleep(1.0)  # let any post-save processing finish
    log(f"   -> Status set to '{new_status}', save clicked, landed on: {page.url}")

def run():
    sql_snapshot("T0 baseline")
    with sync_playwright() as p:
        b = p.chromium.launch(headless=True)
        ctx = b.new_context(viewport={"width": 1366, "height": 900}, ignore_https_errors=True)
        page = ctx.new_page()
        login(page)

        log("\n>> T1: change Status to Hold (cascade should set PIDs Inactive — no visible change, both Inactive)")
        change_status(page, "Hold")
        sql_snapshot("T1 after Hold")

        log("\n>> T2: change Status to Client Approved - Paid (UP-cascade: PIDs should flip to Active)")
        change_status(page, "Client Approved - Paid")
        sql_snapshot("T2 after CAP — EXPECTED Active")

        log("\n>> T3: change Status to Hold (DOWN-cascade: PIDs should flip to Inactive)")
        change_status(page, "Hold")
        sql_snapshot("T3 after Hold — EXPECTED Inactive")

        log("\n>> T4: restore Status to Client Approved - Paid (final: PIDs Active)")
        change_status(page, "Client Approved - Paid")
        sql_snapshot("T4 final restore — EXPECTED Active")

        ctx.close()
        b.close()

if __name__ == "__main__":
    try:
        run()
        log("\nDONE")
    except Exception as e:
        log(f"FAIL: {e}")
        raise
