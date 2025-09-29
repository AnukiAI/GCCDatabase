using Microsoft.Playwright;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace GCCWebApp.UAT.Tests.Tests.Validation
{
    [TestFixture]
    public abstract class ValidationTestBase : PageTest
    {
        protected string BaseUrl { get; set; } = "http://localhost:5101";
        protected IPage TestPage { get; private set; } = null!;

        [SetUp]
        public async Task ValidationSetUp()
        {
            TestPage = await Browser.NewPageAsync(new BrowserNewPageOptions
            {
                IgnoreHTTPSErrors = true
            });
        }

        [TearDown]
        public async Task ValidationTearDown()
        {
            if (TestPage != null)
                await TestPage.CloseAsync();
        }

        /// <summary>
        /// Test form field validation messages
        /// </summary>
        protected async Task<ValidationResult> TestFieldValidation(string fieldSelector, string invalidValue, string? expectedErrorMessage = null)
        {
            try
            {
                // Clear and fill field with invalid value
                await TestPage.FillAsync(fieldSelector, "");
                await TestPage.FillAsync(fieldSelector, invalidValue);
                
                // Trigger validation by clicking elsewhere or pressing tab
                await TestPage.PressAsync(fieldSelector, "Tab");
                await TestPage.WaitForTimeoutAsync(500);

                // Look for validation error
                var errorElements = await TestPage.QuerySelectorAllAsync(".field-validation-error, .invalid-feedback, .error-message, .validation-error");
                var validationMessages = new List<string>();
                
                foreach (var element in errorElements)
                {
                    var text = await element.TextContentAsync();
                    if (!string.IsNullOrWhiteSpace(text))
                        validationMessages.Add(text.Trim());
                }

                // Check if field has error styling
                var hasErrorClass = await TestPage.EvaluateAsync<bool>($@"
                    () => {{
                        const field = document.querySelector('{fieldSelector}');
                        return field && (
                            field.classList.contains('is-invalid') ||
                            field.classList.contains('error') ||
                            field.classList.contains('field-validation-error') ||
                            field.getAttribute('aria-invalid') === 'true'
                        );
                    }}
                ");

                return new ValidationResult
                {
                    HasValidationError = validationMessages.Any() || hasErrorClass,
                    ValidationMessages = validationMessages,
                    HasErrorStyling = hasErrorClass,
                    TestedValue = invalidValue,
                    FieldSelector = fieldSelector
                };
            }
            catch (Exception ex)
            {
                return new ValidationResult
                {
                    HasValidationError = false,
                    ValidationMessages = new List<string> { $"Test failed: {ex.Message}" },
                    HasErrorStyling = false,
                    TestedValue = invalidValue,
                    FieldSelector = fieldSelector
                };
            }
        }

        /// <summary>
        /// Test that form cannot be submitted with invalid data
        /// </summary>
        protected async Task<bool> TestFormSubmissionPrevention(string submitButtonSelector, Dictionary<string, string> invalidFormData)
        {
            try
            {
                // Fill form with invalid data
                foreach (var field in invalidFormData)
                {
                    await TestPage.FillAsync(field.Key, field.Value);
                }

                // Get current URL
                var currentUrl = TestPage.Url;

                // Try to submit form
                await TestPage.ClickAsync(submitButtonSelector);
                await TestPage.WaitForTimeoutAsync(2000);

                // Check if we're still on the same page (submission was prevented)
                var newUrl = TestPage.Url;
                return currentUrl == newUrl;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Test accessibility features
        /// </summary>
        protected async Task<AccessibilityResult> TestAccessibility()
        {
            var result = new AccessibilityResult();

            try
            {
                // Check for proper heading hierarchy
                var headings = await TestPage.QuerySelectorAllAsync("h1, h2, h3, h4, h5, h6");
                result.HasProperHeadings = headings.Any();

                // Check for alt text on images
                var images = await TestPage.QuerySelectorAllAsync("img");
                var imagesWithoutAlt = new List<string>();
                
                foreach (var img in images)
                {
                    var alt = await img.GetAttributeAsync("alt");
                    var src = await img.GetAttributeAsync("src");
                    if (string.IsNullOrEmpty(alt))
                        imagesWithoutAlt.Add(src ?? "unknown");
                }
                result.ImagesWithoutAlt = imagesWithoutAlt;

                // Check for form labels
                var inputs = await TestPage.QuerySelectorAllAsync("input[type='text'], input[type='email'], input[type='password'], textarea, select");
                var inputsWithoutLabels = new List<string>();
                
                foreach (var input in inputs)
                {
                    var id = await input.GetAttributeAsync("id");
                    var ariaLabel = await input.GetAttributeAsync("aria-label");
                    var ariaLabelledBy = await input.GetAttributeAsync("aria-labelledby");
                    
                    if (!string.IsNullOrEmpty(id))
                    {
                        var label = await TestPage.QuerySelectorAsync($"label[for='{id}']");
                        if (label == null && string.IsNullOrEmpty(ariaLabel) && string.IsNullOrEmpty(ariaLabelledBy))
                        {
                            inputsWithoutLabels.Add(id);
                        }
                    }
                    else if (string.IsNullOrEmpty(ariaLabel) && string.IsNullOrEmpty(ariaLabelledBy))
                    {
                        var name = await input.GetAttributeAsync("name");
                        inputsWithoutLabels.Add(name ?? "unnamed input");
                    }
                }
                result.InputsWithoutLabels = inputsWithoutLabels;

                // Check for focus indicators
                result.FocusIndicatorsPresent = await TestPage.EvaluateAsync<bool>(@"
                    () => {
                        const styles = getComputedStyle(document.body);
                        return styles.getPropertyValue('--focus-color') !== '' || 
                               document.querySelector('style')?.textContent?.includes(':focus') ||
                               document.querySelector('link[href*=""bootstrap""]') !== null;
                    }
                ");

                return result;
            }
            catch (Exception ex)
            {
                result.TestError = ex.Message;
                return result;
            }
        }

        /// <summary>
        /// Test responsive design breakpoints
        /// </summary>
        protected async Task<ResponsiveResult> TestResponsiveDesign(string? pageUrl = null)
        {
            if (pageUrl != null)
                await TestPage.GotoAsync($"{BaseUrl}{pageUrl}");

            var result = new ResponsiveResult();
            var breakpoints = new[]
            {
                new { Name = "Mobile", Width = 375, Height = 667 },
                new { Name = "Tablet", Width = 768, Height = 1024 },
                new { Name = "Desktop", Width = 1920, Height = 1080 }
            };

            foreach (var breakpoint in breakpoints)
            {
                await TestPage.SetViewportSizeAsync(breakpoint.Width, breakpoint.Height);
                await TestPage.WaitForTimeoutAsync(500);

                var breakpointResult = new BreakpointResult
                {
                    Name = breakpoint.Name,
                    Width = breakpoint.Width,
                    Height = breakpoint.Height
                };

                // Check for horizontal scrollbars
                breakpointResult.HasHorizontalScroll = await TestPage.EvaluateAsync<bool>(@"
                    () => document.body.scrollWidth > window.innerWidth
                ");

                // Check if navigation is accessible
                var navElements = await TestPage.QuerySelectorAllAsync("nav, .navbar, .navigation");
                breakpointResult.NavigationVisible = navElements.Any();

                // Check if buttons are appropriately sized for touch
                if (breakpoint.Name == "Mobile")
                {
                    var buttons = await TestPage.QuerySelectorAllAsync("button, .btn, input[type='submit']");
                    var touchFriendlyButtons = 0;
                    
                    foreach (var button in buttons)
                    {
                        var box = await button.BoundingBoxAsync();
                        if (box != null && box.Height >= 44 && box.Width >= 44)
                            touchFriendlyButtons++;
                    }
                    breakpointResult.TouchFriendlyElements = touchFriendlyButtons;
                }

                result.BreakpointResults.Add(breakpointResult);
            }

            return result;
        }
    }

    public class ValidationResult
    {
        public bool HasValidationError { get; set; }
        public List<string> ValidationMessages { get; set; } = new List<string>();
        public bool HasErrorStyling { get; set; }
        public string TestedValue { get; set; } = "";
        public string FieldSelector { get; set; } = "";
    }

    public class AccessibilityResult
    {
        public bool HasProperHeadings { get; set; }
        public List<string> ImagesWithoutAlt { get; set; } = new List<string>();
        public List<string> InputsWithoutLabels { get; set; } = new List<string>();
        public bool FocusIndicatorsPresent { get; set; }
        public string TestError { get; set; } = "";
    }

    public class ResponsiveResult
    {
        public List<BreakpointResult> BreakpointResults { get; set; } = new List<BreakpointResult>();
    }

    public class BreakpointResult
    {
        public string Name { get; set; } = "";
        public int Width { get; set; }
        public int Height { get; set; }
        public bool HasHorizontalScroll { get; set; }
        public bool NavigationVisible { get; set; }
        public int TouchFriendlyElements { get; set; }
    }
}