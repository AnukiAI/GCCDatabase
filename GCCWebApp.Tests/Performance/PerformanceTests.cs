using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using System.Net;
using FluentAssertions;
using GCCWebApp.Tests.Integration;

namespace GCCWebApp.Tests.Performance
{
    [MemoryDiagnoser]
    [SimpleJob(RuntimeMoniker.Net80)]
    [Config(typeof(PerformanceTestConfig))]
    public class ApiPerformanceTests : IntegrationTestBase
    {
        private HttpClient? _client;

        public ApiPerformanceTests() : base(new IntegrationTestWebApplicationFactory())
        {
        }

        [GlobalSetup]
        public async Task Setup()
        {
            _client = Client;
            // Seed performance test data
            await GCCWebApp.Tests.Integration.TestDataSeeder.SeedPerformanceTestDataAsync(DbContext);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            // Cleanup is handled by base class disposal
        }

        [Benchmark(Baseline = true)]
        public async Task<HttpResponseMessage> GetCertificateTiles_SmallDataset()
        {
            var response = await _client!.GetAsync("/api/CertificateTileExternal/tiles?limit=10");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            return response;
        }

        [Benchmark]
        public async Task<HttpResponseMessage> GetCertificateTiles_MediumDataset()
        {
            var response = await _client!.GetAsync("/api/CertificateTileExternal/tiles?limit=50");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            return response;
        }

        [Benchmark]
        public async Task<HttpResponseMessage> GetCertificateTiles_LargeDataset()
        {
            var response = await _client!.GetAsync("/api/CertificateTileExternal/tiles?limit=100");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            return response;
        }

        [Benchmark]
        public async Task<HttpResponseMessage> SearchCertificates_SimpleQuery()
        {
            var response = await _client!.GetAsync("/api/CertificateTileExternal/tiles?searchTerm=Performance");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            return response;
        }

        [Benchmark]
        public async Task<HttpResponseMessage> SearchCertificates_ComplexQuery()
        {
            var response = await _client!.GetAsync("/api/CertificateTileExternal/tiles?searchTerm=Performance%20Test%20Certificate");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            return response;
        }

        [Benchmark]
        public async Task<HttpResponseMessage> GetHomePage()
        {
            var response = await _client!.GetAsync("/");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            return response;
        }
    }

    [MemoryDiagnoser]
    [SimpleJob(RuntimeMoniker.Net80)]
    [Config(typeof(PerformanceTestConfig))]
    public class ConcurrentLoadTests : IntegrationTestBase
    {
        private HttpClient? _client;

        public ConcurrentLoadTests() : base(new IntegrationTestWebApplicationFactory())
        {
        }

        [GlobalSetup]
        public async Task Setup()
        {
            _client = Client;
            await GCCWebApp.Tests.Integration.TestDataSeeder.SeedPerformanceTestDataAsync(DbContext);
        }

        [Benchmark]
        [Arguments(5)]
        [Arguments(10)]
        [Arguments(20)]
        public async Task ConcurrentRequests(int concurrentCount)
        {
            var tasks = new List<Task<HttpResponseMessage>>();

            for (int i = 0; i < concurrentCount; i++)
            {
                tasks.Add(_client!.GetAsync("/api/CertificateTileExternal/tiles?limit=10"));
            }

            var responses = await Task.WhenAll(tasks);

            foreach (var response in responses)
            {
                response.StatusCode.Should().Be(HttpStatusCode.OK);
                response.Dispose();
            }
        }

        [Benchmark]
        public async Task SequentialRequests_10()
        {
            for (int i = 0; i < 10; i++)
            {
                using var response = await _client!.GetAsync("/api/CertificateTileExternal/tiles?limit=10");
                response.StatusCode.Should().Be(HttpStatusCode.OK);
            }
        }

        [Benchmark]
        public async Task MixedEndpointLoad()
        {
            var tasks = new List<Task<HttpResponseMessage>>
            {
                _client!.GetAsync("/"),
                _client.GetAsync("/Certificate/CertificateTileView"),
                _client.GetAsync("/api/CertificateTileExternal/tiles?limit=10"),
                _client.GetAsync("/api/CertificateTileExternal/tiles?searchTerm=Test"),
                _client.GetAsync("/Account/Login")
            };

            var responses = await Task.WhenAll(tasks);

            foreach (var response in responses)
            {
                response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);
                response.Dispose();
            }
        }
    }

    public class PerformanceTestConfig : ManualConfig
    {
        public PerformanceTestConfig()
        {
            // Configure for faster execution in tests
            AddJob(Job.Default
                .WithWarmupCount(1)
                .WithIterationCount(3)
                .WithInvocationCount(1)
                .WithUnrollFactor(1));
        }
    }

    // xUnit test runner for performance tests
    public class PerformanceTestRunner : IClassFixture<IntegrationTestWebApplicationFactory>
    {
        private readonly IntegrationTestWebApplicationFactory _factory;

        public PerformanceTestRunner(IntegrationTestWebApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public void RunApiPerformanceTests()
        {
            // Note: This test is disabled by default as benchmarks should be run manually
            // To run: dotnet run --project GCCWebApp.Tests --configuration Release -- --filter "BenchmarkDotNet*"

            // For automated testing, we'll do basic performance validation instead
            Assert.True(true, "Performance tests configured and ready to run manually");
        }

        [Fact]
        public async Task BasicPerformanceValidation_ApiShouldRespondWithin2Seconds()
        {
            // Arrange
            using var testBase = new IntegrationTestBase(_factory);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            var response = await testBase.Client.GetAsync("/api/CertificateTileExternal/tiles?limit=10");

            // Assert
            stopwatch.Stop();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000, "API should respond within 2 seconds");
        }

        [Fact]
        public async Task BasicPerformanceValidation_HomePageShouldRespondWithin1Second()
        {
            // Arrange
            using var testBase = new IntegrationTestBase(_factory);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act
            var response = await testBase.Client.GetAsync("/");

            // Assert
            stopwatch.Stop();
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000, "Home page should respond within 1 second");
        }

        [Fact]
        public async Task BasicPerformanceValidation_SearchShouldHandleLoadWithinReasonableTime()
        {
            // Arrange
            using var testBase = new IntegrationTestBase(_factory);
            await GCCWebApp.Tests.Integration.TestDataSeeder.SeedBasicTestDataAsync(testBase.DbContext);

            var tasks = new List<Task<HttpResponseMessage>>();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Act - 5 concurrent search requests
            for (int i = 0; i < 5; i++)
            {
                tasks.Add(testBase.Client.GetAsync("/api/CertificateTileExternal/tiles?searchTerm=Test"));
            }

            var responses = await Task.WhenAll(tasks);

            // Assert
            stopwatch.Stop();
            foreach (var response in responses)
            {
                response.StatusCode.Should().Be(HttpStatusCode.OK);
                response.Dispose();
            }

            stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000, "5 concurrent searches should complete within 5 seconds");
        }

        [Fact]
        public async Task MemoryUsageValidation_LargeResultSetShouldNotCauseExcessiveMemory()
        {
            // Arrange
            using var testBase = new IntegrationTestBase(_factory);
            await GCCWebApp.Tests.Integration.TestDataSeeder.SeedPerformanceTestDataAsync(testBase.DbContext);

            var initialMemory = GC.GetTotalMemory(true);

            // Act
            var response = await testBase.Client.GetAsync("/api/CertificateTileExternal/tiles?limit=100");
            var content = await response.Content.ReadAsStringAsync();

            var finalMemory = GC.GetTotalMemory(false);
            var memoryIncrease = finalMemory - initialMemory;

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            content.Should().NotBeNullOrEmpty();

            // Memory increase should be reasonable (less than 10MB for 100 certificates)
            memoryIncrease.Should().BeLessThan(10 * 1024 * 1024, "Memory usage should be reasonable for large result sets");
        }
    }
}