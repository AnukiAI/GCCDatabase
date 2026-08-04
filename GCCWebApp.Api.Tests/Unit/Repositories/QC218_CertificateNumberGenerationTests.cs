using Xunit;
using FluentAssertions;
using GCCWebApp.Data;
using GCCWebApp.Models;
using GCCWebApp.Repositories;
using GCCWebApp.Api.Tests.TestUtilities;

namespace GCCWebApp.Api.Tests.Unit.Repositories
{
    /// <summary>
    /// DEF-322 / QC #218 — certificate number allocation.
    ///
    /// Client report (7/30/26): new certificates were coming out 7 characters wide ("26-10095")
    /// on a database holding only a few hundred records. Client's rule, verbatim: "I am ok for it
    /// to be 7 digits IF WE RUN OUT OF numbers for the current year (so after 9999 certs)."
    /// They also asked us to confirm the year rolls over ("2027 should have 27-XXXX").
    ///
    /// Cause: GenerateNextCertificateNumberAsync took Max()+1 over EVERY sequence for the year,
    /// so one imported out-of-band record (26-10095) permanently dragged the counter above 9999.
    /// </summary>
    public class QC218_CertificateNumberGenerationTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly CertificateRepository _repository;
        private readonly string _yearPrefix = (DateTime.Now.Year % 100).ToString("D2");

        public QC218_CertificateNumberGenerationTests()
        {
            _context = TestDbContextFactory.CreateInMemoryContext();
            _repository = new CertificateRepository(_context);

            _context.Companies.Add(new Company { Id = 10, Name = "Acme Corp", Email = "acme@test.com" });
            _context.CertificationTypes.Add(new CertificationType { Id = 10, Name = "Recycled Content", IsActive = true });
            _context.Products.Add(new Product { Id = 10, Name = "Widget", CompanyId = 10 });
            _context.SaveChanges();
        }

        public void Dispose() => _context.Dispose();

        private void AddCert(int id, string number)
        {
            _context.Certificates.Add(new Certificate
            {
                Id = id,
                Number = number,
                Status = CertificateStatusHelper.StatusClientApprovedPaid,
                CompanyId = 10,
                ProductId = 10,
                CertificationTypeId = 10,
                EffectiveDate = DateTime.UtcNow.AddDays(-30),
                ExpirationDate = DateTime.UtcNow.AddDays(335),
                Location = "Philadelphia",
                Country = "United States",
                CreatedAt = DateTime.UtcNow
            });
            _context.SaveChanges();
        }

        [Fact]
        public async Task EmptyDatabase_StartsAtOne_AndIsSixCharacters()
        {
            var next = await _repository.GenerateNextCertificateNumberAsync();

            next.Should().Be($"{_yearPrefix}-0001");
            next.Should().HaveLength(7, "YY-XXXX is 2 + 1 + 4 characters");
        }

        [Fact]
        public async Task NormalSequence_IncrementsAndStaysFourDigits()
        {
            AddCert(1, $"{_yearPrefix}-0001");
            AddCert(2, $"{_yearPrefix}-0265");

            var next = await _repository.GenerateNextCertificateNumberAsync();

            next.Should().Be($"{_yearPrefix}-0266");
        }

        /// <summary>
        /// THE REGRESSION. Before DEF-322 this returned "{YY}-10096" — the client's exact symptom.
        /// </summary>
        [Fact]
        public async Task ImportedOutOfBandNumber_DoesNotPoisonTheSequence()
        {
            AddCert(1, $"{_yearPrefix}-0265");
            AddCert(2, $"{_yearPrefix}-10095"); // arrived via CSV import / legacy ETL

            var next = await _repository.GenerateNextCertificateNumberAsync();

            next.Should().Be($"{_yearPrefix}-0266",
                "an out-of-band imported number must not drag every subsequent certificate above 9999");
            next.Should().NotBe($"{_yearPrefix}-10096");
        }

        [Fact]
        public async Task OnlyOutOfBandNumbersExist_StillAllocatesInsideTheFourDigitSpace()
        {
            AddCert(1, $"{_yearPrefix}-10095");

            var next = await _repository.GenerateNextCertificateNumberAsync();

            next.Should().Be($"{_yearPrefix}-0001", "1..9999 are all still free");
        }

        /// <summary>
        /// The client's stated condition for accepting a wider number: the year is genuinely full.
        /// </summary>
        [Fact]
        public async Task FourDigitSpaceExhausted_WidensToFiveDigits()
        {
            AddCert(1, $"{_yearPrefix}-9999");

            var next = await _repository.GenerateNextCertificateNumberAsync();

            next.Should().Be($"{_yearPrefix}-10000",
                "only once 9,999 is reached may the number widen — this is the client's accepted condition");
        }

        /// <summary>
        /// Year rollover, which the client explicitly asked us to confirm. The allocator scopes its
        /// search to "{current 2-digit year}-", so certificates filed under any other year are
        /// invisible to it. A database full of prior-year numbers therefore starts the new year at
        /// 0001 rather than continuing the old run.
        /// </summary>
        [Fact]
        public async Task PriorYearNumbers_AreIgnored_SoANewYearRestartsAtOne()
        {
            var priorYearPrefix = ((DateTime.Now.Year - 1) % 100).ToString("D2");
            AddCert(1, $"{priorYearPrefix}-0001");
            AddCert(2, $"{priorYearPrefix}-4321");
            AddCert(3, $"{priorYearPrefix}-9999");

            var next = await _repository.GenerateNextCertificateNumberAsync();

            next.Should().Be($"{_yearPrefix}-0001",
                "crossing into a new calendar year restarts the sequence: 26-9999 must not produce 26-10000 in 2027");
            next.Should().StartWith($"{_yearPrefix}-");
        }

        [Fact]
        public async Task GeneratedNumberAlwaysCarriesTheCurrentTwoDigitYearPrefix()
        {
            AddCert(1, $"{_yearPrefix}-0007");

            var next = await _repository.GenerateNextCertificateNumberAsync();

            next.Should().MatchRegex(@"^\d{2}-\d{4,}$");
            next.Split('-')[0].Should().Be(_yearPrefix);
        }

        // ------------------------------------------------------------------
        // Year rollover — the client asked for explicit proof (QC #218, 7/30):
        // "please confirm you have tested that when we hit a new calendar year
        //  it changes the first 2 numbers (e.g., 2027 should have 27-XXXX)".
        //
        // These drive the allocator through the asOf clock seam, so the calendar
        // is genuinely advanced rather than inferred from prior-year data.
        // ------------------------------------------------------------------

        [Theory]
        [InlineData(2026, "26")]
        [InlineData(2027, "27")] // the client's stated example
        [InlineData(2028, "28")]
        [InlineData(2030, "30")]
        [InlineData(2099, "99")]
        public async Task NewCalendarYear_ChangesTheTwoDigitPrefix(int year, string expectedPrefix)
        {
            var next = await _repository.GenerateNextCertificateNumberAsync(new DateTime(year, 6, 15));

            next.Should().Be($"{expectedPrefix}-0001");
        }

        /// <summary>
        /// THE ANSWER TO THE CLIENT'S QUESTION. 2026 is filled right to the top — including the
        /// out-of-band 26-10095 that caused the original complaint — and the clock crosses into
        /// 2027. The next number must be 27-0001, not 26-10000 and not 27-10096.
        /// </summary>
        [Fact]
        public async Task CrossingIntoNewYear_WithPriorYearFull_RestartsAtOhOhOhOne()
        {
            AddCert(1, "26-0001");
            AddCert(2, "26-9999");
            AddCert(3, "26-10095");

            var next = await _repository.GenerateNextCertificateNumberAsync(new DateTime(2027, 1, 1));

            next.Should().Be("27-0001");
            next.Should().HaveLength(7, "YY-XXXX — back to the standard 6-digit-plus-dash shape");
        }

        /// <summary>
        /// The switch happens at the calendar boundary itself, not at some later point.
        /// </summary>
        [Fact]
        public async Task RolloverHappensAtMidnightOnJanuaryFirst()
        {
            AddCert(1, "26-0500");

            var lastMomentOf2026 = await _repository.GenerateNextCertificateNumberAsync(
                new DateTime(2026, 12, 31, 23, 59, 59));
            var firstMomentOf2027 = await _repository.GenerateNextCertificateNumberAsync(
                new DateTime(2027, 1, 1, 0, 0, 0));

            lastMomentOf2026.Should().Be("26-0501", "still 2026 — the run continues");
            firstMomentOf2027.Should().Be("27-0001", "new year — the sequence restarts");
        }

        /// <summary>
        /// Each year keeps its own independent counter; 2027 numbering is unaffected by how many
        /// certificates 2026 holds, and vice versa.
        /// </summary>
        [Fact]
        public async Task EachYearCountsIndependently()
        {
            AddCert(1, "26-0300");
            AddCert(2, "27-0042");

            var next2026 = await _repository.GenerateNextCertificateNumberAsync(new DateTime(2026, 8, 4));
            var next2027 = await _repository.GenerateNextCertificateNumberAsync(new DateTime(2027, 8, 4));
            var next2028 = await _repository.GenerateNextCertificateNumberAsync(new DateTime(2028, 8, 4));

            next2026.Should().Be("26-0301");
            next2027.Should().Be("27-0043");
            next2028.Should().Be("28-0001", "a year with no certificates yet starts fresh");
        }

        /// <summary>
        /// Documents a far-future edge rather than asserting it is desirable: the prefix is
        /// year % 100, so the year 2100 renders as "00-". Noted for the record; no action needed.
        /// </summary>
        [Fact]
        public async Task Year2100_RendersAsZeroZero_DocumentedEdgeCase()
        {
            var next = await _repository.GenerateNextCertificateNumberAsync(new DateTime(2100, 1, 1));

            next.Should().Be("00-0001");
        }
    }
}
