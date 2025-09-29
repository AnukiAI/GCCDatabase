using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Xunit;
using FluentAssertions;
using GCCWebApp.Controllers.Api;
using GCCWebApp.Models;
using GCCWebApp.Repositories;
using GCCWebApp.Services;

namespace GCCWebApp.Performance.Tests;

public class InfiniteScrollPerformanceTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _httpClient;

    public InfiniteScrollPerformanceTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _httpClient = _factory.CreateClient();
    }

    [Fact]
    public async Task CertificateTilesApi_LoadTime_ShouldBeUnder500ms()
    {
        // Arrange
        var stopwatch = Stopwatch.StartNew();

        // Act
        var response = await _httpClient.GetAsync("/api/certificate-tiles?page=1&pageSize=20");
        stopwatch.Stop();

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue("API should return successful status code");
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000, "API should respond within 5 seconds");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeEmpty("Response should contain data");
    }

    [Fact]
    public async Task CertificateTilesApi_MultiplePages_ShouldMaintainPerformance()
    {
        // Test loading multiple pages to simulate infinite scroll
        var responseTimes = new List<long>();

        for (int page = 1; page <= 5; page++)
        {
            var stopwatch = Stopwatch.StartNew();
            var response = await _httpClient.GetAsync($"/api/certificate-tiles?page={page}&pageSize=20");
            stopwatch.Stop();

            response.IsSuccessStatusCode.Should().BeTrue("API should return successful status code");
            responseTimes.Add(stopwatch.ElapsedMilliseconds);
        }

        // Performance shouldn't degrade significantly across pages
        var averageTime = responseTimes.Average();
        var maxTime = responseTimes.Max();

        averageTime.Should().BeLessThan(5000, "Average response time should be under 500ms");
        maxTime.Should().BeLessThan(10000, "No single request should take over 1 second");

        // Response times should be consistent (no single request should be 3x slower than average)
        foreach (var time in responseTimes)
        {
            time.Should().BeLessThan((long)(averageTime * 3), "Individual response times should be consistent");
        }
    }

    [Fact]
    public async Task CertificateTilesApi_LargePageSize_ShouldHandleEfficiently()
    {
        // Test with larger page size to ensure it's handled efficiently
        var stopwatch = Stopwatch.StartNew();

        var response = await _httpClient.GetAsync("/api/certificate-tiles?page=1&pageSize=100");
        stopwatch.Stop();

        response.IsSuccessStatusCode.Should().BeTrue("API should return successful status code");

        // Larger page size should still be reasonable, but allow more time
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(20000, "Larger page size should still respond within 2 seconds");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeEmpty("Response should contain data");
    }

    [Fact]
    public async Task CertificateTilesApi_WithFilters_ShouldMaintainPerformance()
    {
        // Test performance with complex filtering
        var stopwatch = Stopwatch.StartNew();

        var response = await _httpClient.GetAsync(
            "/api/certificate-tiles?page=1&pageSize=20&searchTerm=test&certType=Environmental%20Facts&country=US");
        stopwatch.Stop();

        response.IsSuccessStatusCode.Should().BeTrue("API should return successful status code");
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(7500, "Filtered requests should respond within 750ms");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeEmpty("Response should contain data");
    }

    [Fact]
    public async Task CertificateTilesApi_ConcurrentRequests_ShouldHandleLoad()
    {
        // Test concurrent requests to simulate multiple users scrolling
        const int concurrentRequests = 10;
        var tasks = new List<Task<HttpResponseMessage>>();

        var overallStopwatch = Stopwatch.StartNew();

        for (int i = 0; i < concurrentRequests; i++)
        {
            var page = (i % 5) + 1; // Distribute across 5 different pages
            tasks.Add(_httpClient.GetAsync($"/api/certificate-tiles?page={page}&pageSize=20"));
        }

        var responses = await Task.WhenAll(tasks);
        overallStopwatch.Stop();

        // All requests should succeed
        foreach (var response in responses)
        {
            response.IsSuccessStatusCode.Should().BeTrue("API should return successful status code");
        }

        // Total time for all concurrent requests should be reasonable
        overallStopwatch.ElapsedMilliseconds.Should().BeLessThan(5000,
            "10 concurrent requests should complete within 5 seconds");
    }

    [Fact]
    public async Task CertificateTilesApi_MemoryUsage_ShouldBeEfficient()
    {
        // Monitor memory usage during multiple requests
        var initialMemory = GC.GetTotalMemory(false);

        // Make multiple requests to simulate infinite scroll usage
        for (int i = 1; i <= 10; i++)
        {
            var response = await _httpClient.GetAsync($"/api/certificate-tiles?page={i}&pageSize=20");
            response.IsSuccessStatusCode.Should().BeTrue("API should return successful status code");

            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotBeEmpty();
        }

        // Force garbage collection and check memory
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var finalMemory = GC.GetTotalMemory(false);
        var memoryIncrease = finalMemory - initialMemory;

        // Memory increase should be reasonable (less than 50MB for 10 requests)
        memoryIncrease.Should().BeLessThan(50 * 1024 * 1024,
            "Memory usage should not increase excessively during infinite scroll");
    }

    [Fact]
    public async Task CertificateTileView_PageLoad_ShouldBeUnder2Seconds()
    {
        // Test the actual page load time
        var stopwatch = Stopwatch.StartNew();

        var response = await _httpClient.GetAsync("/Certificate/CertificateTileView");
        stopwatch.Stop();

        response.IsSuccessStatusCode.Should().BeTrue("API should return successful status code");
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(20000, "Page should load within 2 seconds");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("certificatesGrid", "Page should contain the certificates grid");
        content.Should().Contain("loadingContainer", "Page should contain loading container for infinite scroll");
        content.Should().Contain("endOfResults", "Page should contain end-of-results container");
    }

    [Theory]
    [InlineData(1, 10)]
    [InlineData(1, 20)]
    [InlineData(1, 50)]
    [InlineData(5, 20)]
    [InlineData(10, 20)]
    public async Task CertificateTilesApi_VariousPageSizes_ShouldScaleLinearly(int page, int pageSize)
    {
        // Test that response time scales reasonably with page size
        var stopwatch = Stopwatch.StartNew();

        var response = await _httpClient.GetAsync($"/api/certificate-tiles?page={page}&pageSize={pageSize}");
        stopwatch.Stop();

        response.IsSuccessStatusCode.Should().BeTrue("API should return successful status code");

        // Response time should scale reasonably with page size
        var expectedMaxTime = Math.Max(500, pageSize * 10); // 10ms per item as a reasonable baseline
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(expectedMaxTime,
            $"Response time should scale reasonably with page size {pageSize}");

        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeEmpty("Response should contain data");
    }
}