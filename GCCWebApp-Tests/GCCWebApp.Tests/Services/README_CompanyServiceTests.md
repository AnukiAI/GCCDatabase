# CompanyService Unit Tests Documentation

## Overview

This document provides comprehensive documentation for the `CompanyServiceTests` class, which implements extensive unit testing for the `CompanyService` functionality in the GCC Web Application.

## Test File Location
- **File**: `GCCWebApp.Tests/Services/CompanyServiceTests.cs`
- **Validation File**: `GCCWebApp.Tests/Services/CompanyServiceTestValidation.cs`

## Test Architecture

### Infrastructure
- **Framework**: xUnit with FluentAssertions
- **Database**: Entity Framework Core InMemory Database
- **Isolation**: Each test uses a unique database instance
- **Pattern**: Arrange-Act-Assert (AAA)

### Dependencies Added
- `FluentAssertions 8.6.0` - Modern assertion library
- `Microsoft.EntityFrameworkCore.InMemory 9.0.8` - In-memory database for testing

## Test Coverage Summary

### Total Tests: 23 test methods

| Test Category | Count | Description |
|---------------|--------|-------------|
| GetCompaniesAsync Tests | 6 | Retrieval of company collections |
| GetCompanyByIdAsync Tests | 4 | Individual company retrieval |
| CreateCompanyAsync Tests | 4 | Company creation functionality |
| UpdateCompanyAsync Tests | 4 | Company update operations |
| DeleteCompanyAsync Tests | 4 | Soft delete functionality |
| Integration & Edge Cases | 5 | Complex scenarios and edge cases |

## Detailed Test Descriptions

### 1. GetCompaniesAsync Tests (6 tests)

#### `GetCompaniesAsync_ShouldReturnOnlyActiveCompanies`
- **Purpose**: Ensures only non-deleted companies are returned
- **Validates**: Soft delete filtering logic
- **Expected**: 3 active companies from seeded test data

#### `GetCompaniesAsync_ShouldMapPropertiesCorrectly`
- **Purpose**: Verifies correct mapping from Company entity to CompanyViewModel
- **Validates**: Property mapping accuracy
- **Expected**: All properties correctly mapped

#### `GetCompaniesAsync_ShouldHandleNullValuesWithEmptyStrings`
- **Purpose**: Tests handling of null database values
- **Validates**: Null coalescing to empty strings
- **Expected**: Null values become empty strings in ViewModel

#### `GetCompaniesAsync_WhenNoCompanies_ShouldReturnEmptyList`
- **Purpose**: Tests behavior when no companies exist
- **Validates**: Empty collection handling
- **Expected**: Empty but not null list

### 2. GetCompanyByIdAsync Tests (4 tests)

#### `GetCompanyByIdAsync_ValidActiveId_ShouldReturnCompany`
- **Purpose**: Tests successful retrieval by ID
- **Validates**: Standard retrieval functionality
- **Expected**: Correct company returned

#### `GetCompanyByIdAsync_DeletedCompanyId_ShouldReturnNull`
- **Purpose**: Ensures deleted companies cannot be retrieved
- **Validates**: Soft delete enforcement
- **Expected**: Null return for deleted companies

#### `GetCompanyByIdAsync_NonExistentId_ShouldReturnNull`
- **Purpose**: Tests behavior with invalid IDs
- **Validates**: Error handling for missing entities
- **Expected**: Null return for non-existent IDs

#### `GetCompanyByIdAsync_ShouldHandleNullFieldsCorrectly`
- **Purpose**: Tests null field handling in individual retrieval
- **Validates**: Null-to-empty string conversion
- **Expected**: Consistent null handling

### 3. CreateCompanyAsync Tests (4 tests)

#### `CreateCompanyAsync_ValidModel_ShouldCreateCompany`
- **Purpose**: Tests standard company creation
- **Validates**: Entity creation and property mapping
- **Expected**: New company ID returned, proper timestamps set

#### `CreateCompanyAsync_ShouldReturnNewCompanyId`
- **Purpose**: Verifies ID generation and return
- **Validates**: Identity column functionality
- **Expected**: Positive integer ID returned

#### `CreateCompanyAsync_WithMinimalData_ShouldCreateCompany`
- **Purpose**: Tests creation with minimal required data
- **Validates**: Required vs optional field handling
- **Expected**: Successful creation with empty optional fields

#### `CreateCompanyAsync_MultipleCalls_ShouldCreateUniqueCompanies`
- **Purpose**: Tests concurrent/multiple creation scenarios
- **Validates**: Unique ID generation
- **Expected**: Different IDs for different companies

### 4. UpdateCompanyAsync Tests (4 tests)

#### `UpdateCompanyAsync_ValidId_ShouldUpdateCompany`
- **Purpose**: Tests standard update functionality
- **Validates**: Entity modification and timestamp updates
- **Expected**: True return value, updated properties, new UpdatedAt

#### `UpdateCompanyAsync_NonExistentId_ShouldReturnFalse`
- **Purpose**: Tests update behavior with invalid ID
- **Validates**: Error handling for missing entities
- **Expected**: False return value

#### `UpdateCompanyAsync_DeletedCompanyId_ShouldReturnFalse`
- **Purpose**: Ensures deleted companies cannot be updated
- **Validates**: Soft delete enforcement in updates
- **Expected**: False return value

#### `UpdateCompanyAsync_PartialUpdate_ShouldUpdateOnlyProvidedFields`
- **Purpose**: Tests selective field updates
- **Validates**: Proper field-by-field updating
- **Expected**: Only modified fields change, CreatedAt preserved

### 5. DeleteCompanyAsync Tests (4 tests)

#### `DeleteCompanyAsync_ValidId_ShouldSoftDeleteCompany`
- **Purpose**: Tests soft delete functionality
- **Validates**: IsDeleted flag setting, timestamp update
- **Expected**: True return, IsDeleted=true, updated timestamp

#### `DeleteCompanyAsync_NonExistentId_ShouldReturnFalse`
- **Purpose**: Tests delete behavior with invalid ID
- **Validates**: Error handling for missing entities
- **Expected**: False return value

#### `DeleteCompanyAsync_AlreadyDeletedCompany_ShouldReturnFalse`
- **Purpose**: Tests double-delete scenario
- **Validates**: Idempotent delete behavior
- **Expected**: False return value

#### `DeleteCompanyAsync_ShouldNotActuallyRemoveFromDatabase`
- **Purpose**: Ensures soft delete doesn't physically remove records
- **Validates**: Data preservation with logical deletion
- **Expected**: Record still in database but marked deleted

#### `DeleteCompanyAsync_DeletedCompany_ShouldNotAppearInGetCompanies`
- **Purpose**: Integration test for delete filtering
- **Validates**: Cross-method consistency
- **Expected**: Deleted companies excluded from listings

### 6. Integration & Edge Case Tests (5 tests)

#### `CreateUpdateDelete_FullLifecycle_ShouldWorkCorrectly`
- **Purpose**: End-to-end workflow testing
- **Validates**: Complete CRUD cycle functionality
- **Expected**: All operations work together seamlessly

#### `ConcurrentOperations_ShouldHandleCorrectly`
- **Purpose**: Tests concurrent operations handling
- **Validates**: Thread safety and unique ID generation
- **Expected**: No conflicts in concurrent creations

#### `GetCompaniesAsync_WithLargeDataset_ShouldPerformEfficiently`
- **Purpose**: Performance testing with large datasets
- **Validates**: Query performance and memory usage
- **Expected**: Operation completes under 1 second with 100+ records

#### `CreateCompanyAsync_WithSpecialCharacters_ShouldHandleCorrectly`
- **Purpose**: Tests special character handling
- **Validates**: Data sanitization and storage
- **Expected**: Special characters preserved correctly

#### `UpdateCompanyAsync_WithEmptyStrings_ShouldUpdateToEmpty`
- **Purpose**: Tests empty string handling in updates
- **Validates**: Null vs empty string distinction
- **Expected**: Empty strings properly stored

## Test Data Seeding

The test suite uses a comprehensive seeding strategy:

```csharp
private void SeedTestData()
{
    var companies = new List<Company>
    {
        // Active Company 1 - Full data
        new Company { Id = 1, Name = "Active Company 1", Email = "active1@example.com", Website = "https://active1.com", IsDeleted = false },
        
        // Active Company 2 - Full data  
        new Company { Id = 2, Name = "Active Company 2", Email = "active2@example.com", Website = "https://active2.com", IsDeleted = false },
        
        // Deleted Company - For testing soft delete filtering
        new Company { Id = 3, Name = "Deleted Company", Email = "deleted@example.com", Website = "https://deleted.com", IsDeleted = true },
        
        // Company with Nulls - For testing null handling
        new Company { Id = 4, Name = "Company with Nulls", Email = null, Website = null, IsDeleted = false }
    };
}
```

## Key Testing Principles Demonstrated

### 1. **Comprehensive Coverage**
- All public service methods tested
- Both success and failure paths covered
- Edge cases and boundary conditions included

### 2. **Isolation**
- Each test uses its own database instance
- No test dependencies or shared state
- Proper cleanup with IDisposable pattern

### 3. **Realistic Scenarios**
- Tests use realistic data and scenarios
- Business logic validation included
- Integration between methods tested

### 4. **Performance Consideration**
- Large dataset performance testing
- Concurrent operation testing
- Memory usage validation

### 5. **Error Handling**
- Invalid input testing
- Missing entity scenarios
- Boundary condition validation

## Validation Logic

A separate validation class (`CompanyServiceTestValidation`) provides a programmatic way to verify service functionality:

```csharp
public static async Task<bool> ValidateCompanyServiceLogic()
{
    // Performs 8 key validation steps:
    // 1. Create company
    // 2. Retrieve created company
    // 3. Update company
    // 4. Verify update
    // 5. List all companies
    // 6. Soft delete company
    // 7. Verify deletion in listings
    // 8. Verify deletion in retrieval
}
```

## Expected Test Results

When executed successfully, all 23 tests should pass, demonstrating:

- ✅ **Create Operations**: Companies can be created with proper validation
- ✅ **Read Operations**: Companies can be retrieved individually and in collections
- ✅ **Update Operations**: Companies can be modified with proper validation
- ✅ **Delete Operations**: Companies can be soft-deleted maintaining data integrity
- ✅ **Business Logic**: Soft delete filtering works across all operations
- ✅ **Data Integrity**: Timestamps and audit fields are properly maintained
- ✅ **Error Handling**: Invalid operations return appropriate responses
- ✅ **Performance**: Operations complete efficiently even with large datasets

## Running the Tests

### Command Line
```bash
dotnet test --filter "FullyQualifiedName~CompanyServiceTests"
```

### Visual Studio Test Explorer
- Filter by "CompanyServiceTests"
- Run individual tests or entire suite
- View detailed test results and coverage

### CI/CD Integration
- Tests integrate with standard .NET test runners
- Compatible with Azure DevOps, GitHub Actions, etc.
- Generates standard test result formats (trx, junit)

## Troubleshooting

### Common Issues

1. **Build Errors**: Ensure all NuGet packages are restored
   - FluentAssertions
   - Microsoft.EntityFrameworkCore.InMemory

2. **Database Errors**: Each test creates its own database instance
   - No cleanup required
   - Memory databases automatically disposed

3. **Performance Issues**: Large dataset tests may take longer
   - Adjust timeout settings if needed
   - Monitor memory usage during execution

## Maintenance

### Adding New Tests
1. Follow existing naming conventions
2. Use proper AAA pattern structure  
3. Include both positive and negative test cases
4. Update this documentation

### Modifying Existing Tests
1. Ensure backward compatibility
2. Update assertions if service behavior changes
3. Maintain test data consistency

## Conclusion

The `CompanyServiceTests` suite provides comprehensive coverage of all `CompanyService` functionality, ensuring reliable operation of company management features in the GCC Web Application. The tests follow industry best practices and provide confidence in code quality and business logic implementation.