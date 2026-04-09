using Microsoft.Playwright;

namespace GCCWebApp.UAT.Tests.Tests.UX
{
    [TestFixture]
    public abstract class UXUITestBase : PageTest
    {
        protected string BaseUrl { get; set; } = Environment.GetEnvironmentVariable("TEST_BASE_URL") ?? "https://gccwebapp.com";
        protected IPage TestPage { get; private set; } = null!;

        [SetUp]
        public async Task UXSetUp()
        {
            TestPage = await Browser.NewPageAsync(new BrowserNewPageOptions
            {
                IgnoreHTTPSErrors = true
            });
        }

        [TearDown]
        public async Task UXTearDown()
        {
            if (TestPage != null)
                await TestPage.CloseAsync();
        }

        /// <summary>
        /// Test page loading performance
        /// </summary>
        protected async Task<PerformanceResult> TestPagePerformance(string url)
        {
            var result = new PerformanceResult { Url = url };
            
            try
            {
                var startTime = DateTime.Now;
                var response = await TestPage.GotoAsync($"{BaseUrl}{url}", new PageGotoOptions 
                { 
                    WaitUntil = WaitUntilState.NetworkIdle 
                });
                
                result.LoadTime = DateTime.Now - startTime;
                result.StatusCode = response?.Status ?? 0;
                
                // Check for performance metrics
                var performanceMetrics = await TestPage.EvaluateAsync<object>(@"
                    () => {
                        const navigation = performance.getEntriesByType('navigation')[0];
                        return {
                            domContentLoaded: navigation.domContentLoadedEventEnd - navigation.domContentLoadedEventStart,
                            loadComplete: navigation.loadEventEnd - navigation.loadEventStart,
                            firstPaint: performance.getEntriesByName('first-paint')[0]?.startTime || 0,
                            firstContentfulPaint: performance.getEntriesByName('first-contentful-paint')[0]?.startTime || 0
                        };
                    }
                ");
                
                result.PerformanceMetrics = performanceMetrics;
                return result;
            }
            catch (Exception ex)
            {
                result.Error = ex.Message;
                return result;
            }
        }

        /// <summary>
        /// Test responsive navigation
        /// </summary>
        protected async Task<NavigationResult> TestNavigationUsability()
        {
            var result = new NavigationResult();
            
            try
            {
                // Test main navigation elements
                var navElements = await TestPage.QuerySelectorAllAsync("nav, .navbar, .navigation, .menu");
                result.HasMainNavigation = navElements.Any();

                if (result.HasMainNavigation)
                {
                    // Check for mobile menu toggle
                    var mobileToggles = await TestPage.QuerySelectorAllAsync(".navbar-toggler, .menu-toggle, .hamburger, [aria-label*='menu']");
                    result.HasMobileMenuToggle = mobileToggles.Any();

                    // Test navigation links
                    var navLinks = await TestPage.QuerySelectorAllAsync("nav a, .navbar a, .navigation a");
                    result.NavigationLinksCount = navLinks.Count();

                    // Test keyboard navigation
                    if (navLinks.Any())
                    {
                        await navLinks.First().FocusAsync();
                        await TestPage.Keyboard.PressAsync("Tab");
                        
                        var focusedElement = await TestPage.EvaluateAsync<string>("document.activeElement.tagName");
                        result.KeyboardNavigationWorks = !string.IsNullOrEmpty(focusedElement);
                    }
                }

                // Test breadcrumbs if present
                var breadcrumbs = await TestPage.QuerySelectorAllAsync(".breadcrumb, nav[aria-label*='breadcrumb']");
                result.HasBreadcrumbs = breadcrumbs.Any();

                return result;
            }
            catch (Exception ex)
            {
                result.Error = ex.Message;
                return result;
            }
        }

        /// <summary>
        /// Test form usability
        /// </summary>
        protected async Task<FormUsabilityResult> TestFormUsability(string formSelector)
        {
            var result = new FormUsabilityResult { FormSelector = formSelector };
            
            try
            {
                var form = await TestPage.QuerySelectorAsync(formSelector);
                if (form == null)
                {
                    result.Error = "Form not found";
                    return result;
                }

                // Check for proper labels
                var inputs = await form.QuerySelectorAllAsync("input, textarea, select");
                var inputsWithLabels = 0;
                var inputsWithPlaceholders = 0;
                var inputsWithAriaLabels = 0;

                foreach (var input in inputs)
                {
                    var id = await input.GetAttributeAsync("id");
                    var placeholder = await input.GetAttributeAsync("placeholder");
                    var ariaLabel = await input.GetAttributeAsync("aria-label");
                    
                    if (!string.IsNullOrEmpty(id))
                    {
                        var label = await TestPage.QuerySelectorAsync($"label[for='{id}']");
                        if (label != null) inputsWithLabels++;
                    }
                    
                    if (!string.IsNullOrEmpty(placeholder)) inputsWithPlaceholders++;
                    if (!string.IsNullOrEmpty(ariaLabel)) inputsWithAriaLabels++;
                }

                result.TotalInputs = inputs.Count();
                result.InputsWithLabels = inputsWithLabels;
                result.InputsWithPlaceholders = inputsWithPlaceholders;
                result.InputsWithAriaLabels = inputsWithAriaLabels;

                // Check for required field indicators
                var requiredInputs = await form.QuerySelectorAllAsync("input[required], textarea[required], select[required]");
                result.RequiredInputs = requiredInputs.Count();

                // Check for field grouping
                var fieldsets = await form.QuerySelectorAllAsync("fieldset");
                result.HasFieldGrouping = fieldsets.Any();

                // Test tab order
                if (inputs.Any())
                {
                    await inputs.First().FocusAsync();
                    var tabOrder = new List<string>();
                    
                    for (int i = 0; i < Math.Min(5, inputs.Count()); i++)
                    {
                        var activeElement = await TestPage.EvaluateAsync<string>("document.activeElement.name || document.activeElement.id || document.activeElement.tagName");
                        tabOrder.Add(activeElement);
                        await TestPage.Keyboard.PressAsync("Tab");
                    }
                    
                    result.TabOrderTest = string.Join(" -> ", tabOrder);
                }

                return result;
            }
            catch (Exception ex)
            {
                result.Error = ex.Message;
                return result;
            }
        }

        /// <summary>
        /// Test accessibility features
        /// </summary>
        protected async Task<AccessibilityResult> TestAccessibilityFeatures()
        {
            var result = new AccessibilityResult();
            
            try
            {
                // Check color contrast (basic test)
                result.BackgroundColor = await TestPage.EvaluateAsync<string>("getComputedStyle(document.body).backgroundColor");
                result.TextColor = await TestPage.EvaluateAsync<string>("getComputedStyle(document.body).color");

                // Check for skip links
                var skipLinks = await TestPage.QuerySelectorAllAsync("a[href='#main'], a[href='#content'], .skip-link");
                result.HasSkipLinks = skipLinks.Any();

                // Check for proper headings structure
                var headings = await TestPage.QuerySelectorAllAsync("h1, h2, h3, h4, h5, h6");
                var headingLevels = new List<int>();
                
                foreach (var heading in headings)
                {
                    var tagName = await heading.EvaluateAsync<string>("el => el.tagName");
                    if (int.TryParse(tagName.Substring(1), out int level))
                        headingLevels.Add(level);
                }
                
                result.HeadingStructure = string.Join(", ", headingLevels);
                result.HasProperHeadingHierarchy = headingLevels.Count > 0 && headingLevels[0] == 1;

                // Check for ARIA landmarks
                var landmarks = await TestPage.QuerySelectorAllAsync("[role='main'], [role='navigation'], [role='banner'], [role='contentinfo'], main, nav, header, footer");
                result.LandmarksCount = landmarks.Count();

                // Check for alt text on images
                var images = await TestPage.QuerySelectorAllAsync("img");
                var imagesWithAlt = 0;
                
                foreach (var img in images)
                {
                    var alt = await img.GetAttributeAsync("alt");
                    if (!string.IsNullOrEmpty(alt)) imagesWithAlt++;
                }
                
                result.TotalImages = images.Count();
                result.ImagesWithAltText = imagesWithAlt;

                return result;
            }
            catch (Exception ex)
            {
                result.Error = ex.Message;
                return result;
            }
        }

        /// <summary>
        /// Test mobile responsiveness
        /// </summary>
        protected async Task<MobileUsabilityResult> TestMobileUsability()
        {
            var result = new MobileUsabilityResult();
            
            try
            {
                // Set mobile viewport
                await TestPage.SetViewportSizeAsync(375, 667);
                await TestPage.WaitForTimeoutAsync(1000);

                // Check for viewport meta tag
                var viewportMeta = await TestPage.QuerySelectorAsync("meta[name='viewport']");
                result.HasViewportMeta = viewportMeta != null;

                if (result.HasViewportMeta)
                {
                    result.ViewportContent = await viewportMeta.GetAttributeAsync("content") ?? "";
                }

                // Check for horizontal scrolling
                result.HasHorizontalScroll = await TestPage.EvaluateAsync<bool>("document.body.scrollWidth > window.innerWidth");

                // Test touch targets size
                var clickableElements = await TestPage.QuerySelectorAllAsync("button, a, input[type='submit'], .btn");
                var touchFriendlyElements = 0;
                
                foreach (var element in clickableElements)
                {
                    var box = await element.BoundingBoxAsync();
                    if (box != null && box.Height >= 44 && box.Width >= 44) // 44px is recommended minimum
                        touchFriendlyElements++;
                }
                
                result.TotalClickableElements = clickableElements.Count();
                result.TouchFriendlyElements = touchFriendlyElements;

                // Check for mobile menu
                var mobileMenus = await TestPage.QuerySelectorAllAsync(".mobile-menu, .navbar-toggler, .menu-toggle");
                result.HasMobileMenu = mobileMenus.Any();

                // Test text readability
                result.FontSize = await TestPage.EvaluateAsync<string>("getComputedStyle(document.body).fontSize");

                return result;
            }
            catch (Exception ex)
            {
                result.Error = ex.Message;
                return result;
            }
        }
    }

    public class PerformanceResult
    {
        public string Url { get; set; } = "";
        public TimeSpan LoadTime { get; set; }
        public int StatusCode { get; set; }
        public object PerformanceMetrics { get; set; } = new object();
        public string Error { get; set; } = "";
    }

    public class NavigationResult
    {
        public bool HasMainNavigation { get; set; }
        public bool HasMobileMenuToggle { get; set; }
        public int NavigationLinksCount { get; set; }
        public bool KeyboardNavigationWorks { get; set; }
        public bool HasBreadcrumbs { get; set; }
        public string Error { get; set; } = "";
    }

    public class FormUsabilityResult
    {
        public string FormSelector { get; set; } = "";
        public int TotalInputs { get; set; }
        public int InputsWithLabels { get; set; }
        public int InputsWithPlaceholders { get; set; }
        public int InputsWithAriaLabels { get; set; }
        public int RequiredInputs { get; set; }
        public bool HasFieldGrouping { get; set; }
        public string TabOrderTest { get; set; } = "";
        public string Error { get; set; } = "";
    }

    public class AccessibilityResult
    {
        public string BackgroundColor { get; set; } = "";
        public string TextColor { get; set; } = "";
        public bool HasSkipLinks { get; set; }
        public string HeadingStructure { get; set; } = "";
        public bool HasProperHeadingHierarchy { get; set; }
        public int LandmarksCount { get; set; }
        public int TotalImages { get; set; }
        public int ImagesWithAltText { get; set; }
        public string Error { get; set; } = "";
    }

    public class MobileUsabilityResult
    {
        public bool HasViewportMeta { get; set; }
        public string ViewportContent { get; set; } = "";
        public bool HasHorizontalScroll { get; set; }
        public int TotalClickableElements { get; set; }
        public int TouchFriendlyElements { get; set; }
        public bool HasMobileMenu { get; set; }
        public string FontSize { get; set; } = "";
        public string Error { get; set; } = "";
    }
}