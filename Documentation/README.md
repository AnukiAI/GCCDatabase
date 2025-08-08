# GreenCircle Certified API Documentation

Complete API documentation for the GreenCircle Certified Web Application.

## 📚 Documentation Overview

This documentation suite provides comprehensive guides for integrating with the GreenCircle Certified API:

### Core Documentation

1. **[API Specification](./api-specification.yaml)** - OpenAPI 3.0 specification
   - Complete API definition in machine-readable format
   - Can be imported into Postman, Insomnia, or Swagger UI
   - Includes all endpoints, request/response schemas, and examples

2. **[Authentication Guide](./api-authentication-guide.md)** - Security and access control
   - JWT Bearer tokens, API keys, OAuth 2.0, and cookie authentication
   - Implementation examples for multiple programming languages
   - Security best practices and token management

3. **[Endpoints Reference](./api-endpoints-reference.md)** - Complete endpoint documentation
   - All available API endpoints with request/response examples
   - Query parameters, filters, and pagination
   - HTTP status codes and response formats

4. **[Integration Guide](./api-integration-guide.md)** - Practical implementation examples
   - Complete code examples in JavaScript, Python, and C#
   - Common integration patterns and scenarios
   - SDK usage, webhooks, and best practices

5. **[Error Codes & Troubleshooting](./api-error-codes-troubleshooting.md)** - Error handling
   - Complete error code reference with solutions
   - Debugging tools and troubleshooting workflows
   - Support escalation procedures

## 🚀 Quick Start

### 1. Get API Credentials
Register at the [Developer Portal](https://developers.greencircle.com) to obtain:
- API Key for server-to-server authentication
- Client credentials for OAuth 2.0 flows

### 2. Test Your Setup
```bash
# Test API connectivity
curl -H "X-API-Key: your-api-key" \
     https://sandbox-api.greencircle.com/v1/auth/test
```

### 3. Authenticate and Make Your First Request
```javascript
// JavaScript example
const response = await fetch('https://sandbox-api.greencircle.com/v1/account/login', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    email: 'your-email@example.com',
    password: 'your-password'
  })
});

const { token } = await response.json();

// Get certificates
const certificates = await fetch('https://sandbox-api.greencircle.com/v1/certificates', {
  headers: { 'Authorization': `Bearer ${token}` }
});
```

## 🌍 API Environments

| Environment | Base URL | Purpose |
|-------------|----------|---------|
| **Sandbox** | `https://sandbox-api.greencircle.com/v1` | Development and testing |
| **Production** | `https://api.greencircle.com/v1` | Live data and operations |

## 📖 API Resources

### Core Resources
- **Certificates** - Environmental certification data
- **Companies** - Organizations that hold certificates  
- **Products** - Product categories and classifications
- **Users** - User accounts and permissions
- **Roles** - Permission management

### Admin Resources
- **Settings** - System configuration
- **Analytics** - Usage tracking and reporting
- **Webhooks** - Event notifications

## 🔧 Development Tools

### Postman Collection
Import our API specification into Postman for easy testing:
1. Open Postman
2. Import → Link → `https://docs.greencircle.com/api/postman`
3. Set environment variables for your API credentials

### Swagger UI
Interactive API documentation:
- **Sandbox**: https://sandbox-api.greencircle.com/docs
- **Production**: https://api.greencircle.com/docs

### SDKs and Libraries

| Language | Package | Installation |
|----------|---------|-------------|
| **JavaScript/Node.js** | `@greencircle/api-sdk` | `npm install @greencircle/api-sdk` |
| **Python** | `greencircle-api` | `pip install greencircle-api` |
| **C# / .NET** | `GreenCircle.Api.Client` | `dotnet add package GreenCircle.Api.Client` |
| **PHP** | `greencircle/api-client` | `composer require greencircle/api-client` |
| **Ruby** | `greencircle-api` | `gem install greencircle-api` |

## 📋 Common Use Cases

### E-commerce Integration
Display certificate information on product pages:
```javascript
// Get certificate for product
const certificate = await api.getCertificate('24-1605');

// Display certification badge
const badge = {
  type: certificate.certificateType,
  status: certificate.status,
  expirationDate: certificate.expirationDate
};
```

### CRM Synchronization
Keep customer sustainability data in sync:
```python
# Sync certificates with CRM
certificates = api.get_all_certificates(company_id=customer.greencircle_id)
crm.update_customer_certifications(customer.id, certificates)
```

### Compliance Reporting
Generate sustainability reports:
```csharp
// Get certificates expiring soon
var expiringCerts = await client.GetCertificatesAsync(
    status: "active",
    expiringBefore: DateTime.Now.AddDays(30)
);

// Generate compliance report
var report = ReportGenerator.CreateExpirationReport(expiringCerts);
```

## 🔐 Security Best Practices

### Token Management
- Store tokens securely (never in client-side code)
- Implement token refresh logic
- Use short-lived access tokens with refresh tokens

### API Key Security
- Rotate API keys regularly
- Use environment variables for credentials
- Implement IP whitelisting for production

### Data Protection
- Always use HTTPS for API communications
- Validate and sanitize all input data
- Implement proper error handling to avoid data leaks

## ⚡ Performance Guidelines

### Request Optimization
- Use pagination for large datasets
- Implement caching for frequently accessed data
- Batch requests when possible

### Rate Limiting
- Respect rate limits (check headers)
- Implement exponential backoff
- Use webhooks for real-time updates instead of polling

### Error Handling
- Implement retry logic for transient failures
- Log errors with request IDs for debugging
- Monitor API usage and error rates

## 📊 Monitoring and Analytics

### Built-in Analytics
The API provides usage analytics:
- Request volume and response times
- Error rates and patterns  
- Popular endpoints and features

### Custom Monitoring
Implement monitoring for:
- API availability and latency
- Authentication success rates
- Business metrics (certificates created, updated)

### Alerts and Notifications
Set up alerts for:
- API errors above threshold
- Rate limit approaches
- Certificate expiration notifications

## 🔄 Webhooks

Configure webhooks for real-time notifications:

```javascript
// Register webhook
await api.createWebhook({
  url: 'https://yourapp.com/webhooks/greencircle',
  events: ['certificate.created', 'certificate.expired'],
  secret: 'webhook-secret-key'
});

// Handle webhook
app.post('/webhooks/greencircle', (req, res) => {
  const event = req.body;
  
  switch (event.type) {
    case 'certificate.created':
      handleNewCertificate(event.data);
      break;
    case 'certificate.expired':
      handleExpiredCertificate(event.data);
      break;
  }
  
  res.status(200).send('OK');
});
```

## 🆘 Support and Resources

### Documentation
- **API Docs**: https://docs.greencircle.com/api
- **Developer Portal**: https://developers.greencircle.com
- **Status Page**: https://status.greencircle.com

### Community
- **Developer Forum**: https://community.greencircle.com/developers
- **Stack Overflow**: Tag questions with `greencircle-api`
- **GitHub**: https://github.com/greencircle/api-examples

### Support Channels
- **Email**: api-support@greencircle.com
- **Chat**: Available in developer portal
- **Phone**: +1-555-API-HELP (enterprise customers)

### Response Times
- **Community Forum**: 1-2 business days
- **Email Support**: 4-8 hours
- **Priority Support**: 1-2 hours (paid plans)
- **Emergency**: 30 minutes (enterprise only)

## 📝 Changelog and Updates

### API Versioning
- Current version: `v1`
- Versioning strategy: URL-based (`/v1/`, `/v2/`)
- Backward compatibility: 12 months minimum

### Release Notes
Stay updated with API changes:
- **Release Notes**: https://docs.greencircle.com/api/releases
- **Breaking Changes**: 90 days advance notice
- **Deprecation Policy**: 12 months support after deprecation

### Migration Guides
When new versions are released:
- Detailed migration guides provided
- Automated migration tools when possible
- Support team assistance for enterprise customers

## 📄 Legal and Compliance

### Terms of Service
- **API Terms**: https://greencircle.com/api-terms
- **Privacy Policy**: https://greencircle.com/privacy
- **Data Processing**: GDPR and CCPA compliant

### SLA and Uptime
- **Uptime Target**: 99.9% (paid plans)
- **Response Time**: <200ms average
- **Support SLA**: Based on plan tier

### Data Retention
- **Log Retention**: 90 days
- **Deleted Data**: 30-day recovery period
- **Backup Policy**: Daily backups, 7-year retention

---

## 📞 Need Help?

If you can't find what you're looking for in this documentation:

1. **Check the [FAQ](https://docs.greencircle.com/api/faq)** for common questions
2. **Search the [Developer Forum](https://community.greencircle.com/developers)** 
3. **Contact [API Support](mailto:api-support@greencircle.com)** with specific questions

Happy coding! 🚀