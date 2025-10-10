# GCC Web App Security Testing Suite

This project contains comprehensive security tests for the GCC Web Application, focusing on defensive security measures and vulnerability prevention.

## Test Categories

### 1. Authentication Tests
- JWT token validation and expiration
- API key authentication
- Login brute force protection
- Password strength validation
- Session timeout handling

### 2. Authorization Tests
- Role-based access control (RBAC)
- Resource-level permissions
- API endpoint authorization
- Cross-user data access prevention

### 3. Input Validation Tests
- SQL injection prevention
- NoSQL injection prevention
- Command injection prevention
- Data sanitization
- Model validation

### 4. XSS Protection Tests
- Stored XSS prevention
- Reflected XSS prevention
- DOM-based XSS prevention
- Content Security Policy validation

### 5. CSRF Protection Tests
- Anti-forgery token validation
- SameSite cookie configuration
- Cross-origin request blocking

### 6. File Upload Security Tests
- File type validation
- File size limits
- Malicious file detection
- Path traversal prevention

### 7. API Security Tests
- Rate limiting
- API versioning security
- Response data sanitization
- Error information disclosure

### 8. Session Management Tests
- Secure cookie configuration
- Session fixation prevention
- Concurrent session limits

### 9. Data Protection Tests
- Encryption at rest
- Encryption in transit
- Sensitive data masking
- Data anonymization

## Running Tests

```bash
# Run all security tests
dotnet test

# Run specific test category
dotnet test --filter "Category=Authentication"
dotnet test --filter "Category=Authorization"
dotnet test --filter "Category=InputValidation"
```

## Test Framework

- **xUnit**: Primary testing framework
- **FluentAssertions**: Assertion library
- **Moq**: Mocking framework
- **Microsoft.AspNetCore.Mvc.Testing**: Integration testing
- **AngleSharp**: HTML parsing for XSS tests