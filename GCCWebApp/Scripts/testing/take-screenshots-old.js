const { chromium } = require('playwright');
const path = require('path');
const fs = require('fs');

(async () => {
  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({
    viewport: { width: 1920, height: 1080 }
  });
  const page = await context.newPage();

  // Create screenshots directory
  const screenshotsDir = path.join(__dirname, 'ui-screenshots');
  if (!fs.existsSync(screenshotsDir)) {
    fs.mkdirSync(screenshotsDir);
  }

  const baseUrl = 'http://localhost:5101';

  // Login function
  async function login(email, password) {
    await page.goto(`${baseUrl}/Account/Login`);
    await page.fill('input[name="Email"]', email);
    await page.fill('input[name="Password"]', password);
    await page.click('button[type="submit"]');
    await page.waitForNavigation({ waitUntil: 'networkidle' });
  }

  console.log('📸 Starting UI screenshot capture...\n');

  // 1. PUBLIC VIEWS (No auth required)
  console.log('1️⃣ Capturing PUBLIC views...');

  await page.goto(`${baseUrl}/Certificate/CertificateTileView`);
  await page.waitForLoadState('networkidle');
  await page.screenshot({ path: path.join(screenshotsDir, '01-public-certificate-tile-view.png'), fullPage: true });
  console.log('  ✓ Public Certificate Tile View');

  await page.goto(`${baseUrl}/Account/Login`);
  await page.waitForLoadState('networkidle');
  await page.screenshot({ path: path.join(screenshotsDir, '02-login-page.png'), fullPage: true });
  console.log('  ✓ Login Page');

  // Click on a certificate to see details (if available)
  await page.goto(`${baseUrl}/Certificate/CertificateTileView`);
  await page.waitForLoadState('networkidle');
  await page.waitForTimeout(1000);
  const firstCert = await page.$('.certificate-tile');
  if (firstCert) {
    const certId = await firstCert.getAttribute('data-certificate-id');
    if (certId) {
      await page.goto(`${baseUrl}/Certificate/Details/${certId}`);
      await page.waitForLoadState('networkidle');
      await page.screenshot({ path: path.join(screenshotsDir, '03-public-certificate-details.png'), fullPage: true });
      console.log('  ✓ Public Certificate Details');
    }
  }

  // 2. ADMIN VIEWS
  console.log('\n2️⃣ Capturing ADMIN views...');
  await login('admin@greencirclecertified.com', 'Admin123!');

  await page.goto(`${baseUrl}/Dashboard/Index`);
  await page.waitForLoadState('networkidle');
  await page.screenshot({ path: path.join(screenshotsDir, '04-admin-dashboard.png'), fullPage: true });
  console.log('  ✓ Dashboard');

  await page.goto(`${baseUrl}/Certificate/CertificateTileViewInternal`);
  await page.waitForLoadState('networkidle');
  await page.screenshot({ path: path.join(screenshotsDir, '05-admin-certificate-tile-view-internal.png'), fullPage: true });
  console.log('  ✓ Admin Certificate Tile View (Internal)');

  await page.goto(`${baseUrl}/Certificate/Create`);
  await page.waitForLoadState('networkidle');
  await page.screenshot({ path: path.join(screenshotsDir, '06-admin-certificate-create.png'), fullPage: true });
  console.log('  ✓ Create Certificate Page');

  // Get first certificate for edit view
  await page.goto(`${baseUrl}/Certificate/CertificateTileViewInternal`);
  await page.waitForLoadState('networkidle');
  await page.waitForTimeout(1000);
  const adminFirstCert = await page.$('.certificate-tile');
  if (adminFirstCert) {
    const certId = await adminFirstCert.getAttribute('data-certificate-id');
    if (certId) {
      await page.goto(`${baseUrl}/Certificate/Edit/${certId}`);
      await page.waitForLoadState('networkidle');
      await page.screenshot({ path: path.join(screenshotsDir, '07-admin-certificate-edit.png'), fullPage: true });
      console.log('  ✓ Edit Certificate Page');

      await page.goto(`${baseUrl}/Certificate/CertificateSingleView/${certId}`);
      await page.waitForLoadState('networkidle');
      await page.screenshot({ path: path.join(screenshotsDir, '08-admin-certificate-single-view.png'), fullPage: true });
      console.log('  ✓ Single Certificate View');
    }
  }

  await page.goto(`${baseUrl}/Company/Index`);
  await page.waitForLoadState('networkidle');
  await page.screenshot({ path: path.join(screenshotsDir, '09-admin-companies-list.png'), fullPage: true });
  console.log('  ✓ Companies List');

  await page.goto(`${baseUrl}/Product/Management`);
  await page.waitForLoadState('networkidle');
  await page.screenshot({ path: path.join(screenshotsDir, '10-admin-products-management.png'), fullPage: true });
  console.log('  ✓ Products Management');

  // Admin-only pages
  await page.goto(`${baseUrl}/Admin/UserManagement`);
  await page.waitForLoadState('networkidle');
  await page.screenshot({ path: path.join(screenshotsDir, '11-admin-user-management.png'), fullPage: true });
  console.log('  ✓ User Management');

  await page.goto(`${baseUrl}/Admin/RoleManagement`);
  await page.waitForLoadState('networkidle');
  await page.screenshot({ path: path.join(screenshotsDir, '12-admin-role-management.png'), fullPage: true });
  console.log('  ✓ Role Management');

  await page.goto(`${baseUrl}/Admin/CertificateTypes`);
  await page.waitForLoadState('networkidle');
  await page.screenshot({ path: path.join(screenshotsDir, '13-admin-certificate-types.png'), fullPage: true });
  console.log('  ✓ Certificate Types');

  await page.goto(`${baseUrl}/Admin/ProductTypes`);
  await page.waitForLoadState('networkidle');
  await page.screenshot({ path: path.join(screenshotsDir, '14-admin-product-types.png'), fullPage: true });
  console.log('  ✓ Product Types');

  await page.goto(`${baseUrl}/Admin/CertificateFields`);
  await page.waitForLoadState('networkidle');
  await page.screenshot({ path: path.join(screenshotsDir, '15-admin-certificate-fields.png'), fullPage: true });
  console.log('  ✓ Certificate Fields');

  await page.goto(`${baseUrl}/Admin/Partners`);
  await page.waitForLoadState('networkidle');
  await page.screenshot({ path: path.join(screenshotsDir, '16-admin-partners.png'), fullPage: true });
  console.log('  ✓ Partners');

  // 3. ANALYST VIEWS
  console.log('\n3️⃣ Capturing ANALYST views...');
  await page.goto(`${baseUrl}/Account/Login`);
  await page.waitForLoadState('networkidle');
  await login('analyst@greencirclecertified.com', 'Analyst123!');

  await page.goto(`${baseUrl}/Dashboard/Index`);
  await page.waitForLoadState('networkidle');
  await page.screenshot({ path: path.join(screenshotsDir, '17-analyst-dashboard.png'), fullPage: true });
  console.log('  ✓ Analyst Dashboard');

  await page.goto(`${baseUrl}/Certificate/CertificateTileViewInternal`);
  await page.waitForLoadState('networkidle');
  await page.screenshot({ path: path.join(screenshotsDir, '18-analyst-certificate-tile-view.png'), fullPage: true });
  console.log('  ✓ Analyst Certificate Tile View');

  // 4. EXTERNAL VIEWS
  console.log('\n4️⃣ Capturing EXTERNAL views...');
  await page.goto(`${baseUrl}/Account/Login`);
  await page.waitForLoadState('networkidle');
  await login('partner@amazon.com', 'Partner123!');

  await page.goto(`${baseUrl}/Certificate/CertificateTileViewExternal`);
  await page.waitForLoadState('networkidle');
  await page.screenshot({ path: path.join(screenshotsDir, '19-external-certificate-tile-view.png'), fullPage: true });
  console.log('  ✓ External Certificate Tile View');

  await page.goto(`${baseUrl}/Product/Management`);
  await page.waitForLoadState('networkidle');
  await page.screenshot({ path: path.join(screenshotsDir, '20-external-products-management.png'), fullPage: true });
  console.log('  ✓ External Products Management');

  // 5. MODALS AND POPUPS (Admin context)
  console.log('\n5️⃣ Capturing MODALS and POPUPS...');
  await page.goto(`${baseUrl}/Account/Login`);
  await page.waitForLoadState('networkidle');
  await login('admin@greencirclecertified.com', 'Admin123!');

  // Try to trigger filter panel on certificate tile view
  await page.goto(`${baseUrl}/Certificate/CertificateTileViewInternal`);
  await page.waitForLoadState('networkidle');
  await page.waitForTimeout(500);

  const filterToggle = await page.$('#filter-toggle, .filter-toggle, button:has-text("Filters")');
  if (filterToggle) {
    await filterToggle.click();
    await page.waitForTimeout(500);
    await page.screenshot({ path: path.join(screenshotsDir, '21-filter-panel-open.png'), fullPage: true });
    console.log('  ✓ Filter Panel (Open)');
  }

  console.log('\n✅ Screenshot capture complete!');
  console.log(`📁 Screenshots saved to: ${screenshotsDir}`);

  await browser.close();
})();
