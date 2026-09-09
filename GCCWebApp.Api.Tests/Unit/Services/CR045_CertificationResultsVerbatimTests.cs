using FluentAssertions;
using GCCWebApp.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace GCCWebApp.Api.Tests.Unit.Services
{
    /// <summary>
    /// CR-045 (client, 2026-09-04): <c>CertificationResults</c> is a TRUE FREE-TEXT field. Whatever
    /// an analyst types is what publishes — screen, CSV exports, both tile APIs and the Amazon
    /// partner feed — with no interpretation.
    ///
    /// This file replaces DisplayFormattersTests, which guarded the opposite rule. The formatter it
    /// tested (<c>Extensions/DisplayFormatters.FormatCertificationResults</c>) was DELETED, not
    /// disabled, so there is no longer a C# function to unit test. What still needs guarding is the
    /// other half of the old DEF-383 pairing: the <c>CertificationResultsNumeric</c> computed column
    /// that drives the numeric sort.
    ///
    /// The invariant has flipped, and that is the whole point of these tests. It used to be
    /// "the column must multiply by 100 exactly like the display does". It is now "the column must
    /// NOT transform a value the display no longer transforms" — because if the guess survives in
    /// SQL after being removed from C#, a future <c>0.5</c> displays as "0.5" and sorts as 50. That
    /// is DEF-383 again, arrived at from the other side.
    /// </summary>
    public class CR045_CertificationResultsVerbatimTests
    {
        /// <summary>
        /// Builds the EF model against the SQL Server provider so the relational annotation is
        /// readable. No connection is opened — model building is offline, and the connection string
        /// is never used.
        /// </summary>
        private static string GetComputedColumnSql()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlServer("Server=CR045_ModelOnly;Database=CR045_ModelOnly;")
                .Options;

            using var context = new ApplicationDbContext(options);

            var sql = context.Model
                .FindEntityType(typeof(GCCWebApp.Models.Certificate))!
                .FindProperty(nameof(GCCWebApp.Models.Certificate.CertificationResultsNumeric))!
                .GetComputedColumnSql();

            sql.Should().NotBeNullOrWhiteSpace(
                "CertificationResultsNumeric is a persisted computed column and must stay one");

            return sql!;
        }

        [Fact]
        public void SortColumn_DoesNotMultiplyByOneHundred()
        {
            var sql = GetComputedColumnSql();

            // The precise shape of the branch that CR-045 removed. Matching on "* 100" rather than
            // on the whole CASE keeps this test from breaking on unrelated formatting changes while
            // still failing loudly if anyone reinstates the fraction guess.
            sql.Replace(" ", string.Empty)
               .Should().NotContain("*100",
                   "CR-045 made the field verbatim; a sort column that still scales fractions would "
                   + "make a future 0.5 display as \"0.5\" and sort as 50 (DEF-383 in reverse)");
        }

        [Fact]
        public void SortColumn_StillStripsThePercentSign()
        {
            var sql = GetComputedColumnSql();

            // "23%" is a formatted 23 and must sort as 23. Stripping '%' reads the value; it does
            // not reinterpret it, so CR-045 deliberately kept this branch. Every populated row on
            // TEST and DEV is an explicit XX% string after CR-042, which is exactly why removing
            // the x100 branch changed no existing sort position.
            sql.Should().Contain("REPLACE(",
                "values stored as \"23%\" must still sort as 23");
        }

        [Fact]
        public void SortColumn_StillReturnsNullForNonNumericValues()
        {
            var sql = GetComputedColumnSql();

            // "Pass", "Silver", "+Vantage Vinyl Verified" are real values in this column. They have
            // no numeric position and must fall out of the numeric sort dimension rather than
            // collapsing to zero.
            sql.Should().Contain("TRY_CAST",
                "non-numeric free text must yield NULL, not 0");
        }
    }
}
