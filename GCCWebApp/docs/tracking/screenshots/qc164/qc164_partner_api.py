"""
QC #164 Partner API contract evidence:

1. Snapshot baseline Partner API list (only CAP/Republished Amazon certs, status='Active' literal)
2. Promote cert 26-0146 (Amazon, has 2 PIDs, currently legacy 'Active') to CAP via Edit form
3. Re-call API — cert should now appear with productIds[] array, each PID status='Active'
4. Restore cert 26-0146 to legacy 'Active' via Edit form
5. Re-call API — cert should drop out of the response
"""
import json
import subprocess
import time
from pathlib import Path
from playwright.sync_api import sync_playwright

BASE = "http://localhost:5101"
OUT = Path(__file__).parent
EMAIL = "jane.analyst@anukiconsulting.com"
PASSWORD = "TestPass123!"
API_KEY = "gcc_partner_95961557dde1e196be074634cef252caec32c4b08e1e58dcfa63e43bbdbb63c7"
TARGET_NUMBER = "26-0146"  # cert 47, Amazon, 2 PIDs, currently legacy Active

LOG = OUT / "partner_api_log.txt"
LOG.write_text("")

def log(msg):
    print(msg)
    with LOG.open("a", encoding="utf-8") as f:
        f.write(msg + "\n")

def call_api(label, save_as):
    r = subprocess.run(
        ["curl", "-s", "-H", f"X-API-Key: {API_KEY}", f"{BASE}/api/v1/certificates"],
        capture_output=True, text=True
    )
    (OUT / save_as).write_text(r.stdout, encoding="utf-8")
    d = json.loads(r.stdout)
    log(f"\n=== {label} ({save_as}) ===")
    log(f"totalCount: {d['pagination']['totalCount']}")
    target_seen = False
    for c in d['data']:
        pids = c.get('productIds') or []
        marker = "  <-- TARGET" if c['certificateNumber'] == TARGET_NUMBER else ""
        if c['certificateNumber'] == TARGET_NUMBER:
            target_seen = True
        log(f"  {c['certificateNumber']}: status='{c['status']}', productIds={len(pids)}{marker}")
        if c['certificateNumber'] == TARGET_NUMBER and pids:
            for p in pids:
                log(f"      pid type={p.get('type')} value={p.get('value')} status={p.get('status')}")
    log(f"Target '{TARGET_NUMBER}' present in response: {target_seen}")

def login(page):
    page.goto(f"{BASE}/Account/Login", wait_until="domcontentloaded")
    page.locator('input[name="Email"]').fill(EMAIL)
    page.locator('input[name="Password"]').fill(PASSWORD)
    page.locator('button[type="submit"], input[type="submit"]').first.click()
    page.wait_for_load_state("domcontentloaded")

def change_status(page, new_status):
    page.goto(f"{BASE}/Certificate/Edit/{TARGET_NUMBER}", wait_until="domcontentloaded")
    page.wait_for_selector("#Status", timeout=10000)
    page.select_option("#Status", label=new_status)
    time.sleep(0.3)
    page.locator('button:has-text("Save Changes")').first.click()
    page.wait_for_load_state("domcontentloaded")
    time.sleep(1.0)
    log(f"   -> cert {TARGET_NUMBER} Status set to '{new_status}'")

def sql_snap(label):
    cmd = ["sqlcmd", "-S", "ANUKI01", "-d", "GCCDatabase_Dev",
           "-U", "GCC_DevUser", "-P", "DevP@ss2024!SQL",
           "-Q", f"SET NOCOUNT ON; SELECT c.Number, c.Status AS CertStatus, p.ProductIdValue, p.Status AS PidStatus FROM Certificates c JOIN CertificateProductIds p ON p.CertificateId=c.Id WHERE c.Number='{TARGET_NUMBER}' ORDER BY p.Id;",
           "-W", "-s", "|"]
    out = subprocess.run(cmd, capture_output=True, text=True).stdout
    log(f"\n--- {label} (DB) ---")
    log(out.strip())

def run():
    # Step 1: baseline
    call_api("STEP 1: BASELINE (cert in legacy Active, should NOT appear)", "D_step1_baseline.json")
    sql_snap("STEP 1 DB")

    with sync_playwright() as p:
        b = p.chromium.launch(headless=True)
        ctx = b.new_context(viewport={"width": 1366, "height": 900}, ignore_https_errors=True)
        page = ctx.new_page()
        login(page)

        # Step 2: promote to CAP
        log("\n>> STEP 2: promote cert to Client Approved - Paid")
        change_status(page, "Client Approved - Paid")
        sql_snap("STEP 2 DB (after promote)")
        call_api("STEP 2 API (cert SHOULD appear with productIds[], all status=Active)", "D_step2_promoted.json")

        # Step 3: restore to legacy Active
        log("\n>> STEP 3: restore cert to legacy 'Active' status")
        change_status(page, "Active")
        sql_snap("STEP 3 DB (after restore)")
        call_api("STEP 3 API (cert SHOULD drop out — Active is not in PublicStatuses)", "D_step3_restored.json")

        ctx.close()
        b.close()

if __name__ == "__main__":
    try:
        run()
        log("\nDONE")
    except Exception as e:
        log(f"FAIL: {e}")
        raise
