const { chromium } = require('playwright');

(async () => {
    const browser = await chromium.launch();
    const context = await browser.newContext({ viewport: { width: 1920, height: 1080 } });
    const page = await context.newPage();
    
    // Go to login page
    await page.goto('http://localhost:5101/Account/Login');
    await page.fill('#Email', 'admin@greencirclecertified.com');
    await page.fill('#Password', 'Admin123!');
    await page.click('button[type="submit"]');
    await page.waitForTimeout(2000);
    
    // Go to Create Certificate page
    await page.goto('http://localhost:5101/Certificate/Create');
    await page.waitForTimeout(1000);
    
    // Take screenshot
    await page.screenshot({ path: 'create-certificate-screenshot.png', fullPage: true });
    
    console.log('Screenshot saved to create-certificate-screenshot.png');
    
    await browser.close();
})();
