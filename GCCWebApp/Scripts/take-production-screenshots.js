// Production Screenshot Script using Playwright
// Usage: npx playwright test take-production-screenshots.js

const { chromium } = require('playwright');
const path = require('path');
const fs = require('fs');

const BASE_URL = 'https://gccwebapp.com';
const SCREENSHOT_DIR = path.join(__dirname, 'screenshots', new Date().toISOString().split('T')[0]);

// Test credentials
const ADMIN_CREDS = { username: 'admin', password: 'TestPass123!' };
const ANALYST_CREDS = { username: 'analyst', password: 'TestPass123!' };
const EXTERNAL_CREDS = { username: 'external', password: 'TestPass123!' };

async function ensureDir(dir) {
    if (!fs.existsSync(dir)) {
        fs.mkdirSync(dir, { recursive: true });
    }
}

async function login(page, credentials, role) {
    console.log(`Logging in as ${role}...`);
    await page.goto(`${BASE_URL}/Account/Login`);
    await page.waitForLoadState('networkidle');

    // Fill login form
    await page.fill('input[name="Username"], input[name="Email"], #Username, #Email', credentials.username);
    await page.fill('input[name="Password"], #Password', credentials.password);

    // Click login button
    await page.click('button[type="submit"], input[type="submit"]');
    await page.waitForLoadState('networkidle');

    // Check if login was successful
    const url = page.url();
    if (url.includes('/Account/Login')) {
        console.log(`  WARNING: May still be on login page`);
    } else {
        console.log(`  Logged in successfully, redirected to: ${url}`);
    }
}

async function takeScreenshot(page, name, description) {
    const filename = `${name}.png`;
    const filepath = path.join(SCREENSHOT_DIR, filename);
    await page.screenshot({ path: filepath, fullPage: true });
    console.log(`  Screenshot saved: ${filename} - ${description}`);
    return filepath;
}

async function main() {
    await ensureDir(SCREENSHOT_DIR);
    console.log(`\n========================================`);
    console.log(`Production Screenshot Capture`);
    console.log(`Target: ${BASE_URL}`);
    console.log(`Output: ${SCREENSHOT_DIR}`);
    console.log(`========================================\n`);

    const browser = await chromium.launch({ headless: true });
    const context = await browser.newContext({
        viewport: { width: 1920, height: 1080 }
    });
    const page = await context.newPage();

    try {
        // 1. Public Pages (no login required)
        console.log('--- PUBLIC PAGES ---');

        await page.goto(`${BASE_URL}/Account/Login`);
        await page.waitForLoadState('networkidle');
        await takeScreenshot(page, '01_login_page', 'Login page');

        await page.goto(`${BASE_URL}/Certificate/CertificateTileView`);
        await page.waitForLoadState('networkidle');
        await page.waitForTimeout(2000); // Wait for tiles to load
        await takeScreenshot(page, '02_public_certificate_tiles', 'Public certificate tile view');

        // 2. Admin Pages
        console.log('\n--- ADMIN PAGES ---');
        await login(page, ADMIN_CREDS, 'Admin');

        await page.goto(`${BASE_URL}/Dashboard`);
        await page.waitForLoadState('networkidle');
        await takeScreenshot(page, '03_admin_dashboard', 'Admin dashboard');

        await page.goto(`${BASE_URL}/Admin/UserManagement`);
        await page.waitForLoadState('networkidle');
        await takeScreenshot(page, '04_user_management', 'User management');

        await page.goto(`${BASE_URL}/Admin/CertificateTypes`);
        await page.waitForLoadState('networkidle');
        await takeScreenshot(page, '05_certification_types', 'Certification types (22 types)');

        await page.goto(`${BASE_URL}/Admin/Tags`);
        await page.waitForLoadState('networkidle');
        await takeScreenshot(page, '06_tags_management', 'Tags management (26 tags)');

        await page.goto(`${BASE_URL}/Admin/CertificateFields`);
        await page.waitForLoadState('networkidle');
        await takeScreenshot(page, '07_custom_fields', 'Custom fields (30 fields)');

        await page.goto(`${BASE_URL}/Admin/PartnerAPI`);
        await page.waitForLoadState('networkidle');
        await takeScreenshot(page, '08_partner_api', 'Partner API management');

        await page.goto(`${BASE_URL}/Certificate/CertificateTileViewInternal`);
        await page.waitForLoadState('networkidle');
        await page.waitForTimeout(2000);
        await takeScreenshot(page, '09_internal_certificate_view', 'Internal certificate tile view');

        // Logout
        await page.goto(`${BASE_URL}/Account/Logout`);
        await page.waitForLoadState('networkidle');

        // 3. External User Pages
        console.log('\n--- EXTERNAL USER PAGES ---');
        await login(page, EXTERNAL_CREDS, 'External');

        await page.goto(`${BASE_URL}/Certificate/CertificateTileViewExternal`);
        await page.waitForLoadState('networkidle');
        await page.waitForTimeout(2000);
        await takeScreenshot(page, '10_external_certificate_view', 'External certificate tile view');

        console.log('\n========================================');
        console.log('Screenshot capture complete!');
        console.log(`Files saved to: ${SCREENSHOT_DIR}`);
        console.log('========================================\n');

    } catch (error) {
        console.error('Error during screenshot capture:', error);
        await takeScreenshot(page, 'error_state', 'Error state');
    } finally {
        await browser.close();
    }
}

main().catch(console.error);
