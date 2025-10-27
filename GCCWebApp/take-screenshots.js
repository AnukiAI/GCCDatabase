const { chromium } = require('playwright');
const path = require('path');
const fs = require('fs');

(async () => {
  // Create screenshots directory
  const screenshotsDir = path.join(__dirname, 'screenshots', 'accessibility-color-changes');
  if (!fs.existsSync(screenshotsDir)) {
    fs.mkdirSync(screenshotsDir, { recursive: true });
  }

  const browser = await chromium.launch({ headless: true });
  const context = await browser.newContext({
    viewport: { width: 1920, height: 1080 }
  });
  const page = await context.newPage();

  const baseUrl = 'http://localhost:5101';

  // Pages to screenshot
  const pages = [
    { url: '/Account/Login', name: 'Account-Login' },
    { url: '/Certificate/CertificateTileViewExternal', name: 'Certificate-TileView-External' },
    { url: '/Certificate/CertificateTileViewInternal', name: 'Certificate-TileView-Internal' },
    { url: '/Certificate/Create', name: 'Certificate-Create' },
    { url: '/Certificate/Edit/55', name: 'Certificate-Edit' },
    { url: '/Admin/UserManagement', name: 'Admin-UserManagement' },
    { url: '/Admin/Tags', name: 'Admin-Tags' },
    { url: '/Admin/RoleManagement', name: 'Admin-RoleManagement' },
  ];

  console.log('Starting screenshot capture...\n');

  for (const pageInfo of pages) {
    try {
      console.log(`Capturing: ${pageInfo.name}...`);
      await page.goto(`${baseUrl}${pageInfo.url}`, { waitUntil: 'networkidle' });

      // Wait a bit for any dynamic content
      await page.waitForTimeout(2000);

      const screenshotPath = path.join(screenshotsDir, `${pageInfo.name}.png`);
      await page.screenshot({
        path: screenshotPath,
        fullPage: true
      });

      console.log(`✓ Saved: ${pageInfo.name}.png`);
    } catch (error) {
      console.error(`✗ Error capturing ${pageInfo.name}:`, error.message);
    }
  }

  await browser.close();

  console.log(`\n✅ Screenshots saved to: ${screenshotsDir}`);
})();
