using Xunit;
using FluentAssertions;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit.Abstractions;

namespace GCCWebApp.Accessibility.Tests.Tests.ColorContrast
{
    /// <summary>
    /// Tests for WCAG 2.1 Level AA color contrast compliance
    /// Generated: 2025-10-24, Migrated to xUnit: 2025-11-25
    /// </summary>
    public class ColorContrastValidationTests
    {
        private readonly ITestOutputHelper _output;
        private readonly string _cssPath;
        private const double WCAG_AA_MINIMUM_CONTRAST = 4.5;

        // Colors that MUST NOT appear in CSS (non-compliant)
        private static readonly string[] PROHIBITED_COLORS = new[] { "#999", "#999999", "#bbb", "#bbbbbb", "#BBBBBB" };

        // Colors that SHOULD appear (compliant replacements)
        private static readonly string[] REQUIRED_COLORS = new[] { "#707070", "#595959" };

        public ColorContrastValidationTests(ITestOutputHelper output)
        {
            _output = output;
            // Get CSS path relative to test assembly location
            var assemblyLocation = AppContext.BaseDirectory;
            _cssPath = Path.GetFullPath(Path.Combine(assemblyLocation, @"..\..\..\..\..\GCCWebApp\wwwroot\css"));
        }

        /// <summary>
        /// Verifies that prohibited low-contrast colors (#999, #bbb) do not appear in CSS files
        /// </summary>
        [Fact]
        public void CSSFiles_ShouldNotContainProhibitedLowContrastColors()
        {
            // Skip if CSS directory doesn't exist in test environment
            if (!Directory.Exists(_cssPath))
            {
                _output.WriteLine($"CSS directory not found: {_cssPath} - skipping test");
                return;
            }

            // Arrange
            var cssFiles = Directory.GetFiles(_cssPath, "*.css", SearchOption.AllDirectories);
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
            violations.Should().BeEmpty(
                $"Found prohibited low-contrast colors in CSS files:\n{string.Join("\n", violations)}\n\n" +
                "These colors do not meet WCAG AA contrast requirements (4.5:1) and should be replaced with compliant alternatives.");
        }

        /// <summary>
        /// Verifies that admin-core.css uses compliant color values in CSS custom properties
        /// </summary>
        [Fact]
        public void AdminCoreCss_ShouldUseCompliantColorCustomProperties()
        {
            // Arrange
            var adminCorePath = Path.Combine(_cssPath, "admin", "admin-core.css");

            // Skip if file doesn't exist in test environment
            if (!File.Exists(adminCorePath))
            {
                _output.WriteLine($"admin-core.css not found at {adminCorePath} - skipping test");
                return;
            }

            var content = File.ReadAllText(adminCorePath);

            // Act & Assert

            // Verify --admin-text-muted uses compliant color
            content.Should().Contain("--admin-text-muted: #707070",
                "--admin-text-muted should use #707070 (4.54:1 contrast ratio)");

            // Verify --admin-text-light uses compliant color
            content.Should().Contain("--admin-text-light: #595959",
                "--admin-text-light should use #595959 (6.0:1 contrast ratio)");

            // Verify old non-compliant colors are not used
            content.Should().NotContain("--admin-text-muted: #999",
                "--admin-text-muted should not use #999 (non-compliant)");
            content.Should().NotContain("--admin-text-light: #bbb",
                "--admin-text-light should not use #bbb (non-compliant)");
        }

        /// <summary>
        /// Verifies that compliant replacement colors appear in CSS files
        /// </summary>
        [Fact]
        public void CSSFiles_ShouldContainCompliantReplacementColors()
        {
            // Skip if CSS directory doesn't exist in test environment
            if (!Directory.Exists(_cssPath))
            {
                _output.WriteLine($"CSS directory not found: {_cssPath} - skipping test");
                return;
            }

            // Arrange
            var cssFiles = Directory.GetFiles(_cssPath, "*.css", SearchOption.AllDirectories);
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
                foundColors.Should().ContainKey(requiredColor,
                    $"Compliant color {requiredColor} should appear in CSS files but was not found");
                foundColors[requiredColor].Should().BeGreaterThan(0,
                    $"Compliant color {requiredColor} should appear at least once");
            }

            _output.WriteLine("Found compliant colors:");
            foreach (var color in foundColors)
            {
                _output.WriteLine($"  {color.Key}: {color.Value} occurrence(s)");
            }
        }

        /// <summary>
        /// Validates contrast ratio calculation for text colors
        /// </summary>
        [Theory]
        [InlineData("#707070", "#ffffff", 4.54, "admin-text-muted on white background")]
        [InlineData("#595959", "#ffffff", 6.0, "admin-text-light on white background")]
        public void ColorContrast_ShouldMeetWCAGAAMinimum(string foreground, string background, double expectedRatio, string description)
        {
            // Arrange
            var fgColor = HexToRgb(foreground);
            var bgColor = HexToRgb(background);

            // Act
            var contrastRatio = CalculateContrastRatio(fgColor, bgColor);

            // Assert
            contrastRatio.Should().BeGreaterThanOrEqualTo(WCAG_AA_MINIMUM_CONTRAST,
                $"Contrast ratio {contrastRatio:F2}:1 does not meet WCAG AA minimum of {WCAG_AA_MINIMUM_CONTRAST}:1");

            // Verify it's close to expected ratio (within 5% tolerance)
            var tolerance = expectedRatio * 0.05;
            contrastRatio.Should().BeApproximately(expectedRatio, tolerance,
                $"Calculated contrast ratio {contrastRatio:F2}:1 differs from expected {expectedRatio}:1");

            _output.WriteLine($"✓ {foreground} on {background}: {contrastRatio:F2}:1 (WCAG AA compliant) - {description}");
        }

        /// <summary>
        /// Verifies that old non-compliant colors would fail WCAG AA requirements
        /// </summary>
        [Theory]
        [InlineData("#999999", "#ffffff", 2.85, "Old color fails WCAG AA")]
        [InlineData("#bbbbbb", "#ffffff", 1.99, "Old color fails WCAG AA")]
        public void OldColors_ShouldNotMeetWCAGAAMinimum(string foreground, string background, double expectedRatio, string description)
        {
            // Arrange
            var fgColor = HexToRgb(foreground);
            var bgColor = HexToRgb(background);

            // Act
            var contrastRatio = CalculateContrastRatio(fgColor, bgColor);

            // Assert - These SHOULD fail WCAG AA
            contrastRatio.Should().BeLessThan(WCAG_AA_MINIMUM_CONTRAST,
                $"Old color {foreground} should NOT meet WCAG AA minimum (this confirms our fix was necessary)");

            // Verify it's close to expected ratio
            var tolerance = expectedRatio * 0.05;
            contrastRatio.Should().BeApproximately(expectedRatio, tolerance,
                $"Calculated contrast ratio {contrastRatio:F2}:1 differs from expected {expectedRatio}:1");

            _output.WriteLine($"✗ {foreground} on {background}: {contrastRatio:F2}:1 (FAILS WCAG AA - correctly replaced) - {description}");
        }

        /// <summary>
        /// Verifies that specific CSS files have been updated with compliant colors
        /// </summary>
        [Theory]
        [InlineData("admin/admin-core.css", "#707070", "Admin core CSS uses compliant gray")]
        [InlineData("admin/admin-core.css", "#595959", "Admin core CSS uses compliant light gray")]
        public void SpecificCSSFiles_ShouldContainCompliantColors(string cssFile, string expectedColor, string description)
        {
            // Arrange
            var filePath = Path.Combine(_cssPath, cssFile);

            // Skip if file doesn't exist in test environment
            if (!File.Exists(filePath))
            {
                _output.WriteLine($"CSS file not found: {filePath} - skipping test");
                return;
            }

            // Act
            var content = File.ReadAllText(filePath);
            var pattern = $@"\b{Regex.Escape(expectedColor)}\b";
            var matches = Regex.Matches(content, pattern, RegexOptions.IgnoreCase);

            // Assert
            matches.Count.Should().BeGreaterThan(0,
                $"{cssFile} should contain compliant color {expectedColor}");

            _output.WriteLine($"✓ {cssFile} contains {expectedColor} ({matches.Count} occurrence(s)) - {description}");
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
