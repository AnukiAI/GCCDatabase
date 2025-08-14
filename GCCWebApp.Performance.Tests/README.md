# GCC WebApp Performance Tests

This project contains performance tests for the GCC WebApp using multiple testing frameworks.

## Test Types

### 1. Benchmark Tests (BenchmarkDotNet)
- **Purpose**: Micro-benchmarks for critical code paths
- **File**: `CertificateBenchmarks.cs`
- **Measures**: Method execution time, memory allocation, throughput

### 2. Load Tests (NBomber)
- **Purpose**: HTTP endpoint load testing
- **File**: `ApiLoadTests.cs`
- **Measures**: Response times under load, concurrent user simulation

### 3. Database Performance Tests (xUnit)
- **Purpose**: Database query performance validation
- **File**: `DatabasePerformanceTests.cs`
- **Measures**: Query execution times, concurrent access performance

## Running Tests

### Prerequisites
1. Ensure the main GCCWebApp is buildable
2. Install dependencies: `dotnet restore`

### Run All Performance Tests
```bash
dotnet test GCCWebApp.Performance.Tests
```

### Run Specific Test Categories

#### Database Performance Tests Only
```bash
dotnet test GCCWebApp.Performance.Tests --filter "FullyQualifiedName~DatabasePerformanceTests"
```

#### Load Tests Only
```bash
dotnet test GCCWebApp.Performance.Tests --filter "FullyQualifiedName~ApiLoadTests"
```

### Run Benchmarks
```bash
# Run certificate benchmarks
dotnet run --project GCCWebApp.Performance.Tests --configuration Release certificates
```

## Test Environment Setup

### For Load Tests
1. Start the GCC WebApp in a separate terminal:
   ```bash
   dotnet run --project GCCWebApp
   ```
2. Ensure the application is running on the expected port
3. Run the load tests

### For Database Tests
- Uses in-memory database for isolated testing
- No external setup required

### For Benchmarks
- Runs in isolated environment
- Uses in-memory database for data operations

## Performance Targets

### Response Times
- **Simple Database Queries**: < 100ms
- **Complex Database Queries**: < 500ms
- **API Endpoints**: < 200ms for 95% of requests
- **Bulk Operations**: < 2s for 1000 records

### Load Targets
- **Concurrent Users**: 100 users
- **API Throughput**: 500 RPS (read), 100 RPS (write)
- **Database Connections**: < 50 concurrent

## Test Reports

### Benchmark Reports
- Generated in `BenchmarkDotNet.Artifacts` folder
- Includes HTML and Markdown reports
- Memory allocation diagrams

### Load Test Reports
- Generated in respective result folders:
  - `certificate-load-test-results/`
  - `product-load-test-results/`
  - `company-load-test-results/`
  - `auth-load-test-results/`
  - `mixed-workload-test-results/`

### Database Test Results
- Displayed in test output
- Includes execution time assertions
- Performance trend tracking

## Interpreting Results

### Benchmark Results
- **Mean**: Average execution time
- **Error**: Standard error
- **StdDev**: Standard deviation
- **Allocated**: Memory allocated per operation

### Load Test Results
- **RPS**: Requests per second
- **Response Time**: Latency percentiles (50th, 95th, 99th)
- **Error Rate**: Percentage of failed requests
- **Throughput**: Total successful operations

### Database Test Results
- **Pass/Fail**: Whether performance targets were met
- **Actual Times**: Measured execution times
- **Assertions**: Performance threshold validations

## Continuous Performance Monitoring

### CI Integration
Add to build pipeline:
```yaml
- name: Run Performance Tests
  run: |
    dotnet test GCCWebApp.Performance.Tests --logger "trx;LogFileName=performance-results.trx"
    dotnet run --project GCCWebApp.Performance.Tests --configuration Release certificates
```

### Performance Regression Detection
- Compare benchmark results across builds
- Set up alerts for performance degradation
- Track performance trends over time

## Troubleshooting

### Common Issues

1. **Load tests failing to connect**
   - Ensure GCCWebApp is running
   - Check port configuration
   - Verify API endpoints are available

2. **High benchmark variance**
   - Run in Release configuration
   - Close other applications
   - Use dedicated test environment

3. **Database tests timing out**
   - Check for data seeding issues
   - Verify in-memory database setup
   - Review query complexity

### Performance Analysis Tips

1. **Identify Bottlenecks**
   - Run benchmarks to find slow methods
   - Use load tests to find capacity limits
   - Monitor database query performance

2. **Optimize Based on Results**
   - Focus on highest-impact optimizations
   - Use profiling data to guide changes
   - Validate improvements with re-testing

3. **Monitor Trends**
   - Track performance over time
   - Set up automated alerts
   - Compare before/after optimization results

## Adding New Performance Tests

### New Benchmark
1. Add methods to `CertificateBenchmarks.cs`
2. Use `[Benchmark]` attribute
3. Follow naming conventions
4. Include performance assertions

### New Load Test
1. Add test methods to `ApiLoadTests.cs`
2. Configure load simulation parameters
3. Add result validation
4. Document expected performance

### New Database Test
1. Add test methods to `DatabasePerformanceTests.cs`
2. Include timing assertions
3. Use representative test data
4. Test both happy path and edge cases