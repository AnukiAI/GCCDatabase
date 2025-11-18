// GCC Database - Playwright Screenshot Test Suite with Authentication
// Captures screenshots for all user roles with proper login
// Created: 2025-10-05
//
// SETUP:
// npm install -D @playwright/test
// npx playwright install
//
// RUN:
// node playwright-screenshot-tests.js

const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');

// Configuration
const config = {
    baseUrl: 'http://localhost:5101',
    outputDir: 'screenshots',
    viewportSizes: {
        desktop: { width: 1920, height: 1080 },
        tablet: { width: 1024, height: 768 },
        mobile: { width: 375, height: 667 }
    },
    credentials: {
        admin: {
            email: 'admin@greencirclecertified.com',
            password: 'Admin123!'
        },
        analyst: {
            email: 'analyst@greencirclecertified.com',
            password: 'Analyst123!'
        },
        external: {
            email: 'partner@amazon.com',
            password: 'Partner123!'
        }
    }
};

// Create output directory
const timestamp = new Date().toISOString().replace(/:/g, '-').substring(0, 19);
const outputPath = path.join(config.outputDir, timestamp);

function ensureDir(dir) {
    if (!fs.existsSync(dir)) {
        fs.mkdirSync(dir, { recursive: true });
    }
}

ensureDir(outputPath);
ensureDir(path.join(outputPath, 'public'));
ensureDir(path.join(outputPath, 'external'));
ensureDir(path.join(outputPath, 'analyst'));
ensureDir(path.join(outputPath, 'admin'));

// Helper function to login
async function login(page, role) {
    console.log(`  Logging in as ${role}...`);
    const cred = config.credentials[role];

    await page.goto(`${config.baseUrl}/Account/Login`);
    await page.fill('input[name="email"], input[type="email"], input#email', cred.email);
    await page.fill('input[name="password"], input[type="password"], input#password', cred.password);

    // Click login button (try multiple selectors)
    const loginSelectors = [
        'button[type="submit"]',
        'button:has-text("Login")',
        'button:has-text("Sign In")',
        'input[type="submit"]'
    ];

    for (const selector of loginSelectors) {
        try {
            await page.click(selector, { timeout: 2000 });
            break;
        } catch (e) {
            // Try next selector
        }
    }

    // Wait for redirect after login
    await page.waitForLoadState('networkidle');

    // Verify login success
    const url = page.url();
    if (url.includes('/Login')) {
        throw new Error(`Login failed for ${role}`);
    }

    console.log(`  ✓ Logged in as ${role}`);
}

// Helper function to capture screenshot
async function captureScreenshot(page, url, filename, description) {
    console.log(`  Capturing: ${filename} - ${description}`);

    await page.goto(url);
    await page.waitForLoadState('networkidle');

    // Wait for any animations to complete
    await page.waitForTimeout(1000);

    await page.screenshot({
        path: filename,
        fullPage: true
    });

    console.log(`    ✓ Saved: ${filename}`);
}

// Main test suite
(async () => {
    console.log('=== GCC Screenshot Test Suite ===\n');

    const browser = await chromium.launch({ headless: true });

    try {
        // ============== PUBLIC USER TESTS ==============
        console.log('\n[PUBLIC USER TESTS]');
        const publicContext = await browser.newContext({
            viewport: config.viewportSizes.desktop
        });
        const publicPage = await publicContext.newPage();

        await captureScreenshot(
            publicPage,
            `${config.baseUrl}/Certificate/CertificateTileView`,
            path.join(outputPath, 'public', '01-tile-view-landing.png'),
            'Landing page - Certificate tile view'
        );

        // Test autocomplete by focusing on search
        await publicPage.goto(`${config.baseUrl}/Certificate/CertificateTileView`);
        await publicPage.waitForLoadState('networkidle');

        const searchInput = await publicPage.locator('input[type="text"]#searchInput, input.search-input').first();
        await searchInput.fill('Amazon');
        await publicPage.waitForTimeout(500); // Wait for autocomplete

        await publicPage.screenshot({
            path: path.join(outputPath, 'public', '02-autocomplete-dropdown.png'),
            fullPage: true
        });
        console.log(`    ✓ Saved: 02-autocomplete-dropdown.png`);

        await captureScreenshot(
            publicPage,
            `${config.baseUrl}/Certificate/Details/1`,
            path.join(outputPath, 'public', '03-certificate-details.png'),
            'Certificate details view'
        );

        await captureScreenshot(
            publicPage,
            `${config.baseUrl}/Account/Login`,
            path.join(outputPath, 'public', '04-login-page.png'),
            'Login page'
        );

        await publicContext.close();

        // ============== EXTERNAL USER TESTS ==============
        console.log('\n[EXTERNAL USER TESTS]');
        const externalContext = await browser.newContext({
            viewport: config.viewportSizes.desktop
        });
        const externalPage = await externalContext.newPage();

        await login(externalPage, 'external');

        await captureScreenshot(
            externalPage,
            `${config.baseUrl}/Certificate/CertificateTileView`,
            path.join(outputPath, 'external', '01-partner-filtered-view.png'),
            'Partner-filtered certificate view (Amazon only)'
        );

        await captureScreenshot(
            externalPage,
            `${config.baseUrl}/Certificate/Details/1`,
            path.join(outputPath, 'external', '02-certificate-details.png'),
            'Certificate details (partner access)'
        );

        // Try to access restricted page
        await externalPage.goto(`${config.baseUrl}/Certificate/Create`);
        await externalPage.waitForLoadState('networkidle');
        await externalPage.screenshot({
            path: path.join(outputPath, 'external', '03-create-access-denied.png'),
            fullPage: true
        });
        console.log(`    ✓ Saved: 03-create-access-denied.png`);

        // Try admin page
        await externalPage.goto(`${config.baseUrl}/Admin/UserManagement`);
        await externalPage.waitForLoadState('networkidle');
        await externalPage.screenshot({
            path: path.join(outputPath, 'external', '04-admin-access-denied.png'),
            fullPage: true
        });
        console.log(`    ✓ Saved: 04-admin-access-denied.png`);

        await externalContext.close();

        // ============== ANALYST USER TESTS ==============
        console.log('\n[ANALYST USER TESTS]');
        const analystContext = await browser.newContext({
            viewport: config.viewportSizes.desktop
        });
        const analystPage = await analystContext.newPage();

        await login(analystPage, 'analyst');

        await captureScreenshot(
            analystPage,
            `${config.baseUrl}/Dashboard`,
            path.join(outputPath, 'analyst', '01-dashboard.png'),
            'Analytics dashboard'
        );

        await captureScreenshot(
            analystPage,
            `${config.baseUrl}/Certificate/CertificateTileView`,
            path.join(outputPath, 'analyst', '02-certificate-list.png'),
            'Certificate list (all partners)'
        );

        await captureScreenshot(
            analystPage,
            `${config.baseUrl}/Certificate/Create`,
            path.join(outputPath, 'analyst', '03-create-form.png'),
            'Create certificate form'
        );

        await captureScreenshot(
            analystPage,
            `${config.baseUrl}/Certificate/Edit/1`,
            path.join(outputPath, 'analyst', '04-edit-form.png'),
            'Edit certificate form with Product ID manager'
        );

        await captureScreenshot(
            analystPage,
            `${config.baseUrl}/Certificate/Details/1`,
            path.join(outputPath, 'analyst', '05-certificate-details.png'),
            'Certificate details with edit button'
        );

        // Try admin page (should be denied)
        await analystPage.goto(`${config.baseUrl}/Admin/UserManagement`);
        await analystPage.waitForLoadState('networkidle');
        await analystPage.screenshot({
            path: path.join(outputPath, 'analyst', '06-admin-access-denied.png'),
            fullPage: true
        });
        console.log(`    ✓ Saved: 06-admin-access-denied.png`);

        await analystContext.close();

        // ============== ADMIN USER TESTS ==============
        console.log('\n[ADMIN USER TESTS]');
        const adminContext = await browser.newContext({
            viewport: config.viewportSizes.desktop
        });
        const adminPage = await adminContext.newPage();

        await login(adminPage, 'admin');

        await captureScreenshot(
            adminPage,
            `${config.baseUrl}/Dashboard`,
            path.join(outputPath, 'admin', '01-dashboard.png'),
            'Admin analytics dashboard'
        );

        await captureScreenshot(
            adminPage,
            `${config.baseUrl}/Certificate/Create`,
            path.join(outputPath, 'admin', '02-create-form.png'),
            'Create certificate form (admin)'
        );

        await captureScreenshot(
            adminPage,
            `${config.baseUrl}/Certificate/Edit/1`,
            path.join(outputPath, 'admin', '03-edit-form.png'),
            'Edit certificate form (admin)'
        );

        await captureScreenshot(
            adminPage,
            `${config.baseUrl}/Admin/UserManagement`,
            path.join(outputPath, 'admin', '04-user-management.png'),
            'User management page'
        );

        await captureScreenshot(
            adminPage,
            `${config.baseUrl}/Admin/RoleManagement`,
            path.join(outputPath, 'admin', '05-role-management.png'),
            'Role management page'
        );

        // Try to capture API keys page if it exists
        try {
            await captureScreenshot(
                adminPage,
                `${config.baseUrl}/Admin/ApiKeys`,
                path.join(outputPath, 'admin', '06-api-keys.png'),
                'API key management'
            );
        } catch (e) {
            console.log('    ⚠ API Keys page not available');
        }

        await adminContext.close();

        // ============== MOBILE SCREENSHOTS ==============
        console.log('\n[MOBILE SCREENSHOTS]');
        const mobileContext = await browser.newContext({
            viewport: config.viewportSizes.mobile,
            userAgent: 'Mozilla/5.0 (iPhone; CPU iPhone OS 14_0 like Mac OS X) AppleWebKit/605.1.15'
        });
        const mobilePage = await mobileContext.newPage();

        await captureScreenshot(
            mobilePage,
            `${config.baseUrl}/Certificate/CertificateTileView`,
            path.join(outputPath, 'public', 'mobile-01-tile-view.png'),
            'Mobile - Certificate tile view'
        );

        await captureScreenshot(
            mobilePage,
            `${config.baseUrl}/Certificate/Details/1`,
            path.join(outputPath, 'public', 'mobile-02-certificate-details.png'),
            'Mobile - Certificate details'
        );

        await mobileContext.close();

        // ============== TABLET SCREENSHOTS ==============
        console.log('\n[TABLET SCREENSHOTS]');
        const tabletContext = await browser.newContext({
            viewport: config.viewportSizes.tablet,
            userAgent: 'Mozilla/5.0 (iPad; CPU OS 14_0 like Mac OS X) AppleWebKit/605.1.15'
        });
        const tabletPage = await tabletContext.newPage();

        await captureScreenshot(
            tabletPage,
            `${config.baseUrl}/Certificate/CertificateTileView`,
            path.join(outputPath, 'public', 'tablet-01-tile-view.png'),
            'Tablet - Certificate tile view'
        );

        await captureScreenshot(
            tabletPage,
            `${config.baseUrl}/Certificate/Details/1`,
            path.join(outputPath, 'public', 'tablet-02-certificate-details.png'),
            'Tablet - Certificate details'
        );

        await tabletContext.close();

        console.log('\n=== Screenshots Completed Successfully ===');
        console.log(`Output directory: ${outputPath}`);
        console.log('\nScreenshot Summary:');
        console.log(`  Public:   ${fs.readdirSync(path.join(outputPath, 'public')).length} screenshots`);
        console.log(`  External: ${fs.readdirSync(path.join(outputPath, 'external')).length} screenshots`);
        console.log(`  Analyst:  ${fs.readdirSync(path.join(outputPath, 'analyst')).length} screenshots`);
        console.log(`  Admin:    ${fs.readdirSync(path.join(outputPath, 'admin')).length} screenshots`);

    } catch (error) {
        console.error('\n❌ Error during screenshot capture:', error);
        process.exit(1);
    } finally {
        await browser.close();
    }
})();
