using FluentAssertions;
using GCCWebApp.Extensions;
using Xunit;

namespace GCCWebApp.Api.Tests.Unit.Services
{
    /// <summary>
    /// DEF-383: CertificationResults is free text, so the display rule has to guess whether a number
    /// is a fraction (0.86) or an already-whole percentage (100). The boundary was `< 1`, which sent
    /// the single value `1` down the whole-percentage branch and rendered it as "1%".
    ///
    /// In the client's data `1` means 100%. That is not an assumption — every distinct stored value
    /// was enumerated: the scale runs 0.01 through 0.99 and then 1, with nothing at all between 1
    /// and 100. `0.01` is already how 1% is written, so `1` cannot also mean 1%.
    ///
    /// The boundary cases below are the whole point of this file. If someone "simplifies" the rule
    /// back to `< 1`, the `1 => 100%` case fails.
    /// </summary>
    public class DisplayFormattersTests
    {
        [Theory]
        // The regression: 1 is the top of the fraction scale, not one percent.
        [InlineData("1", "100%")]
        [InlineData("1.0", "100%")]
        // Fractions, including the value that proves 1% has its own representation.
        [InlineData("0.01", "1%")]
        [InlineData("0.17", "17%")]
        [InlineData("0.86", "86%")]
        [InlineData("0.99", "99%")]
        [InlineData("0", "0%")]
        // Above the boundary: already a whole percentage, so it is not multiplied.
        [InlineData("17", "17%")]
        [InlineData("100", "100%")]
        public void FormatCertificationResults_NumericValues_AreRenderedAsPercentages(
            string stored, string expected)
        {
            DisplayFormatters.FormatCertificationResults(stored).Should().Be(expected);
        }

        [Theory]
        // Already carries a '%' — passed through untouched, never re-scaled.
        [InlineData("100%")]
        [InlineData("17%")]
        [InlineData(">50%")]
        // Non-numeric grades really are in this column (17 "Silver", 7 "Gold" on the test data).
        [InlineData("Silver")]
        [InlineData("Gold")]
        [InlineData("+Vantage Vinyl Verified")]
        [InlineData("Pass")]
        public void FormatCertificationResults_NonNumericOrAlreadyFormatted_IsUnchanged(string stored)
        {
            DisplayFormatters.FormatCertificationResults(stored).Should().Be(stored);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void FormatCertificationResults_NullOrBlank_IsReturnedAsIs(string? stored)
        {
            DisplayFormatters.FormatCertificationResults(stored).Should().Be(stored);
        }

        /// <summary>
        /// The percentage shown on screen and the CertificationResultsNumeric computed column that
        /// drives the numeric sort share one boundary. They are written in two languages (C# and
        /// T-SQL) and can drift silently — if they do, these 106 certificates display "100%" while
        /// sorting to the bottom as though they were 1%. This mirrors the SQL CASE in
        /// ApplicationDbContext so a change to one side without the other fails here.
        /// </summary>
        [Theory]
        [InlineData("1", 100)]
        [InlineData("0.99", 99)]
        [InlineData("0.01", 1)]
        [InlineData("100", 100)]
        [InlineData("17", 17)]
        public void SortValue_MatchesTheDisplayedPercentage(string stored, decimal expectedSortValue)
        {
            // Mirror of the computed-column CASE in ApplicationDbContext.
            decimal ComputedColumnEquivalent(string raw)
            {
                var n = decimal.Parse(raw, System.Globalization.CultureInfo.InvariantCulture);
                return n <= 1 ? n * 100 : n;
            }

            var displayed = DisplayFormatters.FormatCertificationResults(stored);
            var sortValue = ComputedColumnEquivalent(stored);

            sortValue.Should().Be(expectedSortValue);
            displayed.Should().Be(
                expectedSortValue.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture) + "%",
                "the sort value and the displayed percentage must not disagree");
        }
    }
}
