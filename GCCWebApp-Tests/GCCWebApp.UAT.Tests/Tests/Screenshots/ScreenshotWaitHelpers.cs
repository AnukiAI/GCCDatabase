using Microsoft.Playwright;
using System;
using System.Threading.Tasks;

namespace GCCWebApp.UAT.Tests.Tests.Screenshots
{
    public static class ScreenshotWaitHelpers
    {
        /// <summary>
        /// Wait for all images on the page to load
        /// </summary>
        public static async Task WaitForImagesAsync(IPage page, int timeoutMs = 10000)
        {
            try
            {
                await page.WaitForFunctionAsync(@"() => {
                    const images = Array.from(document.querySelectorAll('img'));
                    return images.length === 0 || images.every(img => img.complete && img.naturalHeight !== 0);
                }", new PageWaitForFunctionOptions { Timeout = timeoutMs });
            }
            catch (TimeoutException)
            {
                Console.WriteLine("Warning: Some images may not have fully loaded");
            }
        }

        /// <summary>
        /// Wait for lazy-loaded content to appear
        /// </summary>
        public static async Task WaitForLazyLoadAsync(IPage page, int scrollPauseMs = 1000)
        {
            await page.EvaluateAsync(@"async () => {
                const distance = 100;
                const delay = 100;
                const height = document.body.scrollHeight;
                
                for (let i = 0; i < height; i += distance) {
                    window.scrollBy(0, distance);
                    await new Promise(resolve => setTimeout(resolve, delay));
                }
                
                // Scroll back to top
                window.scrollTo(0, 0);
            }");
            
            await page.WaitForTimeoutAsync(scrollPauseMs);
        }

        /// <summary>
        /// Wait for AJAX requests to complete
        /// </summary>
        public static async Task WaitForAjaxAsync(IPage page, int timeoutMs = 5000)
        {
            try
            {
                // First, inject a script to track AJAX requests
                await page.EvaluateAsync(@"() => {
                    if (!window.__ajaxCount) {
                        window.__ajaxCount = 0;
                        const originalOpen = XMLHttpRequest.prototype.open;
                        const originalSend = XMLHttpRequest.prototype.send;
                        
                        XMLHttpRequest.prototype.open = function() {
                            this.addEventListener('loadend', () => {
                                window.__ajaxCount--;
                            });
                            return originalOpen.apply(this, arguments);
                        };
                        
                        XMLHttpRequest.prototype.send = function() {
                            window.__ajaxCount++;
                            return originalSend.apply(this, arguments);
                        };
                    }
                }");

                // Wait for all AJAX requests to complete
                await page.WaitForFunctionAsync(
                    "() => window.__ajaxCount === 0", 
                    new PageWaitForFunctionOptions { Timeout = timeoutMs }
                );
            }
            catch (TimeoutException)
            {
                Console.WriteLine("Warning: Some AJAX requests may still be pending");
            }
        }

        /// <summary>
        /// Wait for specific element to be visible and stable
        /// </summary>
        public static async Task WaitForElementStableAsync(IPage page, string selector, int stabilityMs = 500)
        {
            try
            {
                // Wait for element to exist
                await page.WaitForSelectorAsync(selector, new PageWaitForSelectorOptions 
                { 
                    State = WaitForSelectorState.Visible,
                    Timeout = 10000 
                });

                // Wait for element position to stabilize (no animations)
                await page.EvaluateAsync($@"async (selector) => {{
                    const element = document.querySelector(selector);
                    if (!element) return;
                    
                    const getPosition = () => {{
                        const rect = element.getBoundingClientRect();
                        return JSON.stringify({{x: rect.x, y: rect.y, width: rect.width, height: rect.height}});
                    }};
                    
                    let lastPosition = getPosition();
                    let stableCount = 0;
                    
                    while (stableCount < 5) {{
                        await new Promise(resolve => setTimeout(resolve, 100));
                        const currentPosition = getPosition();
                        if (currentPosition === lastPosition) {{
                            stableCount++;
                        }} else {{
                            stableCount = 0;
                            lastPosition = currentPosition;
                        }}
                    }}
                }}", selector);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Element {selector} may not be stable: {ex.Message}");
            }
        }

        /// <summary>
        /// Hide elements that might cause screenshot inconsistencies
        /// </summary>
        public static async Task HideDynamicElementsAsync(IPage page)
        {
            await page.EvaluateAsync(@"() => {
                // Hide cookie banners
                const cookieBanners = document.querySelectorAll('[class*=""cookie""], [id*=""cookie""], [class*=""consent""], [id*=""consent""]');
                cookieBanners.forEach(el => el.style.display = 'none');
                
                // Hide chat widgets
                const chatWidgets = document.querySelectorAll('[class*=""chat""], [id*=""chat""], .intercom-launcher, #hubspot-messages-iframe-container');
                chatWidgets.forEach(el => el.style.display = 'none');
                
                // Pause videos
                const videos = document.querySelectorAll('video');
                videos.forEach(video => {
                    video.pause();
                    video.currentTime = 0;
                });
                
                // Pause animations
                const style = document.createElement('style');
                style.textContent = '*, *::before, *::after { animation-play-state: paused !important; }';
                document.head.appendChild(style);
            }");
        }

        /// <summary>
        /// Comprehensive wait for page to be ready for screenshot
        /// </summary>
        public static async Task WaitForPageReadyAsync(IPage page)
        {
            // Wait for network idle
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            // Wait for DOM content
            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            
            // Wait for images
            await WaitForImagesAsync(page);
            
            // Wait for any AJAX requests
            await WaitForAjaxAsync(page);
            
            // Hide dynamic elements
            await HideDynamicElementsAsync(page);
            
            // Final stabilization pause
            await page.WaitForTimeoutAsync(500);
        }
    }
}