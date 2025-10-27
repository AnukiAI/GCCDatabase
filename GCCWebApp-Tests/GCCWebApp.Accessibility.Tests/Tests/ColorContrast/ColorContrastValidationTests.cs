using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace GCCWebApp.Accessibility.Tests.Tests.ColorContrast
{
    /// <summary>
    /// Tests for WCAG 2.1 Level AA color contrast compliance
    /// Generated: 2025-10-24
    /// </summary>
    [TestFixture]
    public class ColorContrastValidationTests
    {
        private const string CSS_PATH = @"..\..\..\..\..\GCCWebApp\wwwroot\css";
        private const double WCAG_AA_MINIMUM_CONTRAST = 4.5;

        // Colors that MUST NOT appear in CSS (non-compliant)
        private static readonly string[] PROHIBITED_COLORS = new[] { "#999", "#999999", "#bbb", "#bbbbbb", "#BBBBBB" };

        // Colors that SHOULD appear (compliant replacements)
        private static readonly string[] REQUIRED_COLORS = new[] { "#707070", "#595959" };

        [SetUp]
        public void Setup()
        {
            // Verify CSS directory exists
            var cssPath = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, CSS_PATH));
            if (!Directory.Exists(cssPath))
            {
                Assert.Fail($"CSS directory not found: {cssPath}");
            }
        }

        /// <summary>
        /// Verifies that prohibited low-contrast colors (#999, #bbb) do not appear in CSS files
        /// </summary>
        [Test]
        public void CSSFiles_ShouldNotContainProhibitedLowContrastColors()
        {
            // Arrange
            var cssPath = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, CSS_PATH));
            var cssFiles = Directory.GetFiles(cssPath, "*.css", SearchOption.AllDirectories);
            var violations = new System.Collections.Generic.List<string>();

            // Act
            foreach (var file in cssFiles)
            {
                var content = File.ReadAllText(file);
                var fileName = Path.GetFileName(file);

                foreach (var prohibitedColor in PROHIBITED_COLORS)
                {
                    // Case-insensitive search for prohibited colors
                    var pattern = $@"\b{Regex.Escape(prohibitedColor)}\b";
                    var matches = Regex.Matches(content, pattern, RegexOptions.IgnoreCase);

                    if (matches.Count > 0)
                    {
                        violations.Add($"{fileName}: Found {matches.Count} instance(s) of {prohibitedColor}");
                    }
                }
            }

            // Assert
            Assert.IsEmpty(violations,
                $"Found prohibited low-contrast colors in CSS files:\n{string.Join("\n", violations)}\n\n" +
                "These colors do not meet WCAG AA contrast requirements (4.5:1) and should be replaced with compliant alternatives.");
        }

        /// <summary>
        /// Verifies that admin-core.css uses compliant color values in CSS custom properties
        /// </summary>
        [Test]
        public void AdminCoreCss_ShouldUseCompliantColorCustomProperties()
        {
            // Arrange
            var adminCorePath = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory,
                CSS_PATH, "admin", "admin-core.css"));
            Assert.IsTrue(File.Exists(adminCorePath), $"admin-core.css not found at {adminCorePath}");

            var content = File.ReadAllText(adminCorePath);

            // Act & Assert

            // Verify --admin-text-muted uses compliant color
            Assert.IsTrue(content.Contains("--admin-text-muted: #707070"),
                "--admin-text-muted should use #707070 (4.54:1 contrast ratio)");

            // Verify --admin-text-light uses compliant color
            Assert.IsTrue(content.Contains("--admin-text-light: #595959"),
                "--admin-text-light should use #595959 (6.0:1 contrast ratio)");

            // Verify old non-compliant colors are not used
            Assert.IsFalse(content.Contains("--admin-text-muted: #999"),
                "--admin-text-muted should not use #999 (non-compliant)");
            Assert.IsFalse(content.Contains("--admin-text-light: #bbb"),
                "--admin-text-light should not use #bbb (non-compliant)");
        }

        /// <summary>
        /// Verifies that compliant replacement colors appear in CSS files
        /// </summary>
        [Test]
        public void CSSFiles_ShouldContainCompliantReplacementColors()
        {
            // Arrange
            var cssPath = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, CSS_PATH));
            var cssFiles = Directory.GetFiles(cssPath, "*.css", SearchOption.AllDirectories);
            var foundColors = new System.Collections.Generic.Dictionary<string, int>();

            // Act
            foreach (var file in cssFiles)
            {
                var content = File.ReadAllText(file);

                foreach (var requiredColor in REQUIRED_COLORS)
                {
                    var pattern = $@"\b{Regex.Escape(requiredColor)}\b";
                    var matches = Regex.Matches(content, pattern, RegexOptions.IgnoreCase);

                    if (matches.Count > 0)
                    {
                        if (!foundColors.ContainsKey(requiredColor))
                            foundColors[requiredColor] = 0;
                        foundColors[requiredColor] += matches.Count;
                    }
                }
            }

            // Assert
            foreach (var requiredColor in REQUIRED_COLORS)
            {
                Assert.IsTrue(foundColors.ContainsKey(requiredColor) && foundColors[requiredColor] > 0,
                    $"Compliant color {requiredColor} should appear in CSS files but was not found");
            }

            TestContext.WriteLine($"Found compliant colors:");
            foreach (var color in foundColors)
            {
                TestContext.WriteLine($"  {color.Key}: {color.Value} occurrence(s)");
            }
        }

        /// <summary>
        /// Validates contrast ratio calculation for text colors
        /// </summary>
        [Test]
        [TestCase("#707070", "#ffffff", 4.54, Description = "admin-text-muted on white background")]
        [TestCase("#595959", "#ffffff", 6.0, Description = "admin-text-light on white background")]
        public void ColorContrast_ShouldMeetWCAGAAMinimum(string foreground, string background, double expectedRatio)
        {
            // Arrange
            var fgColor = HexToRgb(foreground);
            var bgColor = HexToRgb(background);

            // Act
            var contrastRatio = CalculateContrastRatio(fgColor, bgColor);

            // Assert
            Assert.GreaterOrEqual(contrastRatio, WCAG_AA_MINIMUM_CONTRAST,
                $"Contrast ratio {contrastRatio:F2}:1 does not meet WCAG AA minimum of {WCAG_AA_MINIMUM_CONTRAST}:1");

            // Verify it's close to expected ratio (within 5% tolerance)
            var tolerance = expectedRatio * 0.05;
            Assert.That(contrastRatio, Is.EqualTo(expectedRatio).Within(tolerance),
                $"Calculated contrast ratio {contrastRatio:F2}:1 differs from expected {expectedRatio}:1");

            TestContext.WriteLine($"✓ {foreground} on {background}: {contrastRatio:F2}:1 (WCAG AA compliant)");
        }

        /// <summary>
        /// Verifies that old non-compliant colors would fail WCAG AA requirements
        /// </summary>
        [Test]
        [TestCase("#999999", "#ffffff", 2.85, Description = "Old color fails WCAG AA")]
        [TestCase("#bbbbbb", "#ffffff", 1.99, Description = "Old color fails WCAG AA")]
        public void OldColors_ShouldNotMeetWCAGAAMinimum(string foreground, string background, double expectedRatio)
        {
            // Arrange
            var fgColor = HexToRgb(foreground);
            var bgColor = HexToRgb(background);

            // Act
            var contrastRatio = CalculateContrastRatio(fgColor, bgColor);

            // Assert - These SHOULD fail WCAG AA
            Assert.Less(contrastRatio, WCAG_AA_MINIMUM_CONTRAST,
                $"Old color {foreground} should NOT meet WCAG AA minimum (this confirms our fix was necessary)");

            // Verify it's close to expected ratio
            var tolerance = expectedRatio * 0.05;
            Assert.That(contrastRatio, Is.EqualTo(expectedRatio).Within(tolerance),
                $"Calculated contrast ratio {contrastRatio:F2}:1 differs from expected {expectedRatio}:1");

            TestContext.WriteLine($"✗ {foreground} on {background}: {contrastRatio:F2}:1 (FAILS WCAG AA - correctly replaced)");
        }

        /// <summary>
        /// Verifies that specific CSS files have been updated with compliant colors
        /// </summary>
        [Test]
        [TestCase("admin/admin-core.css", "#707070", Description = "Admin core CSS uses compliant gray")]
        [TestCase("admin/admin-core.css", "#595959", Description = "Admin core CSS uses compliant light gray")]
        [TestCase("certificate-autocomplete.css", "#707070", Description = "Certificate autocomplete uses compliant colors")]
        [TestCase("product-management.css", "#595959", Description = "Product management uses compliant colors")]
        public void SpecificCSSFiles_ShouldContainCompliantColors(string cssFile, string expectedColor)
        {
            // Arrange
            var filePath = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory,
                CSS_PATH, cssFile));
            Assert.IsTrue(File.Exists(filePath), $"CSS file not found: {filePath}");

            // Act
            var content = File.ReadAllText(filePath);
            var pattern = $@"\b{Regex.Escape(expectedColor)}\b";
            var matches = Regex.Matches(content, pattern, RegexOptions.IgnoreCase);

            // Assert
            Assert.Greater(matches.Count, 0,
                $"{cssFile} should contain compliant color {expectedColor}");

            TestContext.WriteLine($"✓ {cssFile} contains {expectedColor} ({matches.Count} occurrence(s))");
        }

        #region Helper Methods

        private (int R, int G, int B) HexToRgb(string hex)
        {
            hex = hex.TrimStart('#');
            if (hex.Length == 3)
            {
                hex = string.Join("", hex.Select(c => $"{c}{c}"));
            }

            return (
                Convert.ToInt32(hex.Substring(0, 2), 16),
                Convert.ToInt32(hex.Substring(2, 2), 16),
                Convert.ToInt32(hex.Substring(4, 2), 16)
            );
        }

        private double CalculateContrastRatio((int R, int G, int B) fg, (int R, int G, int B) bg)
        {
            var l1 = CalculateRelativeLuminance(fg.R, fg.G, fg.B);
            var l2 = CalculateRelativeLuminance(bg.R, bg.G, bg.B);

            var lighter = Math.Max(l1, l2);
            var darker = Math.Min(l1, l2);

            return (lighter + 0.05) / (darker + 0.05);
        }

        private double CalculateRelativeLuminance(int r, int g, int b)
        {
            var rsRGB = r / 255.0;
            var gsRGB = g / 255.0;
            var bsRGB = b / 255.0;

            var rLinear = rsRGB <= 0.03928 ? rsRGB / 12.92 : Math.Pow((rsRGB + 0.055) / 1.055, 2.4);
            var gLinear = gsRGB <= 0.03928 ? gsRGB / 12.92 : Math.Pow((gsRGB + 0.055) / 1.055, 2.4);
            var bLinear = bsRGB <= 0.03928 ? bsRGB / 12.92 : Math.Pow((bsRGB + 0.055) / 1.055, 2.4);

            return 0.2126 * rLinear + 0.7152 * gLinear + 0.0722 * bLinear;
        }

        #endregion
    }
}
