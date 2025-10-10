# Performance Tests - Getting Started

## Overview
The performance test project has been created with basic database performance tests. The project structure is set up and ready for expansion.

## Current Status
✅ Performance test project created  
✅ Project added to solution  
✅ Basic performance tests implemented  
✅ Performance test plan documented  
✅ Package references configured  

## What's Included

### 1. Performance Test Plan (`PERFORMANCE_TEST_PLAN.md`)
Comprehensive plan outlining testing strategy, objectives, and success criteria.

### 2. Basic Performance Tests (`BasicPerformanceTests.cs`)
Initial set of database performance tests including:
- Simple certificate queries
- Filtered queries with timing assertions
- Bulk insert operations
- Text search operations
- Aggregate queries

### 3. Test Script (`run-performance-tests.cmd`)
Windows batch script for running performance tests.

## Current Test Results Structure
```
BasicPerformanceTests
├── CertificateQuery_SimpleSelect_ShouldBeUnder100ms
├── CertificateQuery_WithFiltering_ShouldBeUnder200ms  
├── BulkInsert_100Records_ShouldBeUnder1Second
├── SearchQuery_TextSearch_ShouldBeEfficient
└── AggregateQuery_CountGroupBy_ShouldBeEfficient
```

## Running Tests

### Method 1: Direct Test Command
```bash
# Build the performance test project
dotnet build GCCWebApp.Performance.Tests

# Run performance tests (will show pass/fail with timing results)
dotnet test GCCWebApp.Performance.Tests --logger "console;verbosity=detailed"
```

### Method 2: Using Test Script
```bash
# Run the provided batch script
.\GCCWebApp.Performance.Tests\run-performance-tests.cmd
```

## Performance Targets
- **Simple queries**: < 100ms
- **Filtered queries**: < 200ms  
- **Bulk operations**: < 1-2 seconds
- **Search queries**: < 300ms
- **Aggregate queries**: < 200ms

## Next Steps for Expansion

### 1. Add Load Testing
Uncomment and fix the NBomber-based load tests for HTTP endpoints:
- API endpoint testing
- Concurrent user simulation
- Throughput measurement

### 2. Add Benchmarking
Implement BenchmarkDotNet tests for method-level performance:
- Database query benchmarks
- Service method benchmarks
- Memory allocation profiling

### 3. Integration with CI/CD
Add performance testing to build pipeline:
```yaml
- name: Run Performance Tests
  run: dotnet test GCCWebApp.Performance.Tests --logger "trx;LogFileName=performance-results.trx"
```

## Troubleshooting

### Build Issues
If you encounter build errors related to other test projects:
1. The performance tests are designed to build independently
2. Use `--no-dependencies` flag if needed
3. Check package version compatibility

### Test Data Setup
The tests use in-memory databases with seeded test data:
- 500 test certificates
- 10 test companies  
- 20 test products
- 5 certificate types
- 5 product categories

## Framework Versions
- Target Framework: .NET 8.0
- Entity Framework Core: 9.0.8
- xUnit: 2.9.2
- FluentAssertions: 8.5.0

The performance test infrastructure is ready for use and expansion as the application grows.