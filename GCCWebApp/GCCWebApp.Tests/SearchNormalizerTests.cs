using GCCWebApp.Search;
using Xunit;

namespace GCCWebApp.Tests
{
    /// <summary>
    /// CR-187: verifies the search-normalization rule (the C# / input side). The DB / stored side is
    /// generated from the SAME RemovableChars array via SearchNormalizer.ToSqlExpression, so these
    /// tests pin the canonical behavior both halves share. Each case maps to an acceptance criterion
    /// / testing scenario in the change request.
    /// </summary>
    public class SearchNormalizerTests
    {
        // The pairs the CR says must be treated as equivalent. The user's search term (left) must
        // normalize to the same value as the stored product name (right).
        [Theory]
        [InlineData("Product A", "Product™ A")]   // trademark symbol
        [InlineData("Product A", "Product® A")]   // registered symbol
        [InlineData("Product A", "Product© A")]   // copyright symbol
        [InlineData("product a", "Product A")]    // case difference
        [InlineData("Product-A", "Product A")]    // hyphen vs space
        [InlineData("Product   A", "Product A")]  // collapsed multiple spaces
        [InlineData("Product A", "Product: A")]   // colon punctuation
        [InlineData("  Product A  ", "Product A")]// leading/trailing whitespace
        [InlineData("Product A", "Product™® A")] // multiple symbols at once
        public void EquivalentInputs_NormalizeToSameValue(string userInput, string storedName)
        {
            Assert.Equal(SearchNormalizer.Normalize(storedName), SearchNormalizer.Normalize(userInput));
        }

        [Fact]
        public void TrademarkCase_NormalizesToPlainText()
        {
            // The canonical form is lowercase with symbols/whitespace stripped.
            Assert.Equal("producta", SearchNormalizer.Normalize("Product™ A"));
            Assert.Equal("producta", SearchNormalizer.Normalize("Product A"));
        }

        [Theory]
        [InlineData("prod", "Product A")]         // partial product name
        [InlineData("producta", "Product™ A")]    // full normalized term
        [InlineData("uct", "Product A")]          // partial, mid-word
        public void PartialTerm_IsSubstringOfStoredNormalizedValue(string userInput, string storedName)
        {
            var term = SearchNormalizer.Normalize(userInput);
            var stored = SearchNormalizer.Normalize(storedName);
            Assert.Contains(term, stored);
        }

        [Fact]
        public void ExactMatch_StillWorks()
        {
            Assert.Equal(SearchNormalizer.Normalize("Acme Widget 5000"),
                         SearchNormalizer.Normalize("Acme Widget 5000"));
        }

        [Fact]
        public void DistinctProducts_DoNotCollide()
        {
            // Normalization must not be so aggressive that genuinely different names collapse together.
            Assert.NotEqual(SearchNormalizer.Normalize("Product A"),
                            SearchNormalizer.Normalize("Product B"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("™®© ")]   // only removable characters
        public void OnlyRemovableOrEmpty_ReturnsEmpty(string? input)
        {
            Assert.Equal(string.Empty, SearchNormalizer.Normalize(input));
        }

        [Theory]
        [InlineData("Len-Tex", "LenTex")]         // existing hyphen behavior preserved
        [InlineData("3-Form", "3 Form")]
        [InlineData("St. Louis", "St Louis")]     // company/location punctuation
        public void LegacyHyphenAndPunctuation_StillCollapse(string a, string b)
        {
            Assert.Equal(SearchNormalizer.Normalize(b), SearchNormalizer.Normalize(a));
        }

        [Fact]
        public void DigitsAndLettersArePreserved()
        {
            Assert.Equal("abc123", SearchNormalizer.Normalize("ABC-123"));
        }

        // --- SQL parity surface: the stored side is generated from ToSqlExpression. These guard the
        // generator so it stays a faithful mirror of Normalize (deterministic LOWER/REPLACE only). ---

        [Fact]
        public void ToSqlExpression_WrapsColumnInLowerAndReplace()
        {
            var sql = SearchNormalizer.ToSqlExpression("[Name]");
            Assert.StartsWith("LOWER(", sql);
            Assert.Contains("[Name]", sql);
            Assert.Contains("REPLACE(", sql);
        }

        [Fact]
        public void ToSqlExpression_EscapesSingleQuoteAndUsesNcharForWhitespace()
        {
            var sql = SearchNormalizer.ToSqlExpression("[Name]");
            // Apostrophe must be doubled inside the N'...' literal.
            Assert.Contains("N''''", sql);
            // Tab/newline/carriage-return are emitted as NCHAR(...) rather than raw literals.
            Assert.Contains("NCHAR(9)", sql);
            Assert.Contains("NCHAR(10)", sql);
            Assert.Contains("NCHAR(13)", sql);
        }
    }
}
