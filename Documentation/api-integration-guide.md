# GreenCircle Certified API Integration Guide

## Table of Contents

1. [Getting Started](#getting-started)
2. [Quick Start Examples](#quick-start-examples)
3. [Integration Patterns](#integration-patterns)
4. [SDK and Libraries](#sdk-and-libraries)
5. [Code Examples by Language](#code-examples-by-language)
6. [Common Integration Scenarios](#common-integration-scenarios)
7. [Webhooks](#webhooks)
8. [Best Practices](#best-practices)
9. [Testing and Debugging](#testing-and-debugging)
10. [Performance Optimization](#performance-optimization)

## Getting Started

### Prerequisites

1. **API Account**: Register at https://developers.greencircle.com
2. **API Keys**: Obtain your API credentials from the developer dashboard
3. **Environment**: Choose between sandbox and production environments

### Environment URLs

| Environment | Base URL | Purpose |
|-------------|----------|---------|
| **Sandbox** | `https://sandbox-api.greencircle.com/v1` | Testing and development |
| **Production** | `https://api.greencircle.com/v1` | Live data and operations |

### Authentication Setup

Before making any API calls, you need to authenticate:

```bash
# Test your API key
curl -H "X-API-Key: your-api-key-here" \
     https://sandbox-api.greencircle.com/v1/auth/test
```

## Quick Start Examples

### 1. Basic Certificate Retrieval

```bash
# Get list of certificates
curl -H "Authorization: Bearer your-jwt-token" \
     "https://api.greencircle.com/v1/certificates?page=1&pageSize=10"
```

### 2. Search for Specific Certificate

```bash
# Search for certificates by company
curl -H "Authorization: Bearer your-jwt-token" \
     "https://api.greencircle.com/v1/certificates?search=ASSA%20ABLOY"
```

### 3. Create a New Certificate

```bash
curl -X POST \
     -H "Authorization: Bearer your-jwt-token" \
     -H "Content-Type: application/json" \
     -d '{
       "certificateNumber": "24-9999",
       "companyId": 1,
       "productName": "Test Product",
       "certificateType": "Recycled Content",
       "issueDate": "2024-03-01",
       "expirationDate": "2025-03-01"
     }' \
     "https://api.greencircle.com/v1/certificates"
```

## Integration Patterns

### 1. Synchronous API Calls

Best for real-time data retrieval and immediate responses.

```javascript
// Simple synchronous pattern
async function getCertificate(certificateId) {
  try {
    const response = await fetch(`/api/certificates/${certificateId}`, {
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      }
    });
    
    if (!response.ok) {
      throw new Error(`HTTP ${response.status}: ${response.statusText}`);
    }
    
    return await response.json();
  } catch (error) {
    console.error('Error fetching certificate:', error);
    throw error;
  }
}
```

### 2. Batch Processing

For handling multiple operations efficiently.

```javascript
// Batch certificate creation
async function createCertificatesBatch(certificates) {
  const batchSize = 10;
  const results = [];
  
  for (let i = 0; i < certificates.length; i += batchSize) {
    const batch = certificates.slice(i, i + batchSize);
    const batchPromises = batch.map(cert => createCertificate(cert));
    
    try {
      const batchResults = await Promise.allSettled(batchPromises);
      results.push(...batchResults);
      
      // Rate limiting: wait between batches
      if (i + batchSize < certificates.length) {
        await new Promise(resolve => setTimeout(resolve, 1000));
      }
    } catch (error) {
      console.error(`Error in batch ${Math.floor(i / batchSize)}:`, error);
    }
  }
  
  return results;
}
```

### 3. Pagination Handler

For efficiently retrieving large datasets.

```javascript
class PaginationHandler {
  constructor(apiClient) {
    this.apiClient = apiClient;
  }

  async *getAllPages(endpoint, params = {}) {
    let page = 1;
    const pageSize = 100; // Maximum page size
    
    while (true) {
      const response = await this.apiClient.get(endpoint, {
        ...params,
        page,
        pageSize
      });
      
      yield response.data;
      
      if (!response.pagination.hasNext) {
        break;
      }
      
      page++;
    }
  }

  async getAllItems(endpoint, params = {}) {
    const allItems = [];
    
    for await (const pageItems of this.getAllPages(endpoint, params)) {
      allItems.push(...pageItems);
    }
    
    return allItems;
  }
}

// Usage
const paginator = new PaginationHandler(apiClient);
const allCertificates = await paginator.getAllItems('/certificates', {
  status: 'active'
});
```

## SDK and Libraries

### Official JavaScript/TypeScript SDK

```bash
npm install @greencircle/api-sdk
```

```typescript
import { GreenCircleAPI } from '@greencircle/api-sdk';

const client = new GreenCircleAPI({
  apiKey: process.env.GREENCIRCLE_API_KEY,
  environment: 'sandbox' // or 'production'
});

// Type-safe certificate retrieval
const certificate = await client.certificates.get('24-1605');
const certificates = await client.certificates.list({
  page: 1,
  pageSize: 20,
  status: 'active'
});
```

### Python SDK

```bash
pip install greencircle-api
```

```python
from greencircle_api import GreenCircleClient

client = GreenCircleClient(
    api_key=os.environ['GREENCIRCLE_API_KEY'],
    environment='sandbox'
)

# Get certificates
certificates = client.certificates.list(
    page=1,
    page_size=20,
    status='active'
)

# Create certificate
new_cert = client.certificates.create({
    'certificate_number': '24-9999',
    'company_id': 1,
    'product_name': 'Sustainable Product',
    'certificate_type': 'Recycled Content'
})
```

### C# / .NET SDK

```bash
dotnet add package GreenCircle.Api.Client
```

```csharp
using GreenCircle.Api.Client;

var client = new GreenCircleApiClient(new GreenCircleConfig
{
    ApiKey = Environment.GetEnvironmentVariable("GREENCIRCLE_API_KEY"),
    Environment = ApiEnvironment.Sandbox
});

// Get certificates
var certificates = await client.Certificates.ListAsync(new CertificateListRequest
{
    Page = 1,
    PageSize = 20,
    Status = CertificateStatus.Active
});

// Create certificate
var newCertificate = await client.Certificates.CreateAsync(new CreateCertificateRequest
{
    CertificateNumber = "24-9999",
    CompanyId = 1,
    ProductName = "Sustainable Product",
    CertificateType = "Recycled Content"
});
```

## Code Examples by Language

### JavaScript / Node.js

#### Complete Integration Example

```javascript
const axios = require('axios');

class GreenCircleIntegration {
  constructor(config) {
    this.baseURL = config.baseURL || 'https://api.greencircle.com/v1';
    this.apiKey = config.apiKey;
    this.token = null;
    this.tokenExpiry = null;
    
    this.client = axios.create({
      baseURL: this.baseURL,
      timeout: 30000,
      headers: {
        'Content-Type': 'application/json'
      }
    });
    
    // Response interceptor for error handling
    this.client.interceptors.response.use(
      (response) => response,
      (error) => this.handleError(error)
    );
  }

  // Authentication
  async authenticate(email, password) {
    try {
      const response = await this.client.post('/account/login', {
        email,
        password
      });
      
      this.token = response.data.token;
      this.tokenExpiry = Date.now() + (response.data.expiresIn * 1000);
      
      // Set default auth header
      this.client.defaults.headers.common['Authorization'] = 
        `Bearer ${this.token}`;
      
      return response.data.user;
    } catch (error) {
      throw new Error(`Authentication failed: ${error.message}`);
    }
  }

  // Check if token is valid
  isTokenValid() {
    return this.token && Date.now() < this.tokenExpiry;
  }

  // Certificate operations
  async getCertificates(filters = {}) {
    if (!this.isTokenValid()) {
      throw new Error('Authentication required');
    }

    const params = new URLSearchParams();
    Object.entries(filters).forEach(([key, value]) => {
      if (value !== undefined && value !== null) {
        params.append(key, value);
      }
    });

    const response = await this.client.get(`/certificates?${params}`);
    return response.data;
  }

  async getCertificate(certificateId) {
    const response = await this.client.get(`/certificates/${certificateId}`);
    return response.data;
  }

  async createCertificate(certificateData) {
    const response = await this.client.post('/certificates', certificateData);
    return response.data;
  }

  async updateCertificate(certificateId, updates) {
    const response = await this.client.put(
      `/certificates/${certificateId}`, 
      updates
    );
    return response.data;
  }

  // Company operations
  async getCompanies(search = '') {
    const params = search ? `?search=${encodeURIComponent(search)}` : '';
    const response = await this.client.get(`/companies${params}`);
    return response.data;
  }

  async createCompany(companyData) {
    const response = await this.client.post('/companies', companyData);
    return response.data;
  }

  // Error handling
  handleError(error) {
    if (error.response) {
      const { status, data } = error.response;
      
      switch (status) {
        case 401:
          this.token = null;
          throw new Error('Authentication required');
        case 403:
          throw new Error('Insufficient permissions');
        case 429:
          const retryAfter = error.response.headers['retry-after'] || 60;
          throw new Error(`Rate limited. Retry after ${retryAfter} seconds`);
        case 422:
          throw new Error(`Validation error: ${data.message}`);
        default:
          throw new Error(`API error: ${data.message || error.message}`);
      }
    } else if (error.request) {
      throw new Error('Network error: Please check your connection');
    } else {
      throw new Error(`Request error: ${error.message}`);
    }
  }
}

// Usage example
async function main() {
  const integration = new GreenCircleIntegration({
    baseURL: 'https://sandbox-api.greencircle.com/v1',
    apiKey: process.env.GREENCIRCLE_API_KEY
  });

  try {
    // Authenticate
    await integration.authenticate('user@example.com', 'password');

    // Get active certificates
    const certificates = await integration.getCertificates({
      status: 'active',
      page: 1,
      pageSize: 10
    });

    console.log(`Found ${certificates.pagination.totalItems} certificates`);

    // Create a new certificate
    const newCert = await integration.createCertificate({
      certificateNumber: '24-TEST-001',
      companyId: 1,
      productName: 'Test Product',
      certificateType: 'Recycled Content',
      issueDate: '2024-03-01',
      expirationDate: '2025-03-01',
      attributes: {
        recycledContent: '45%',
        certificationStandard: 'ISO 14021'
      }
    });

    console.log('Certificate created:', newCert.data.certificateNumber);

  } catch (error) {
    console.error('Integration error:', error.message);
  }
}

if (require.main === module) {
  main();
}

module.exports = GreenCircleIntegration;
```

### Python

#### Complete Integration Example

```python
import os
import requests
import time
from datetime import datetime, timedelta
from typing import Dict, List, Optional, Any

class GreenCircleAPI:
    def __init__(self, api_key: str, base_url: str = None):
        self.api_key = api_key
        self.base_url = base_url or 'https://api.greencircle.com/v1'
        self.token = None
        self.token_expiry = None
        self.session = requests.Session()
        self.session.headers.update({
            'Content-Type': 'application/json',
            'User-Agent': 'GreenCircle-Python-Client/1.0'
        })

    def authenticate(self, email: str, password: str) -> Dict[str, Any]:
        """Authenticate and get access token"""
        url = f"{self.base_url}/account/login"
        data = {
            'email': email,
            'password': password
        }
        
        response = self.session.post(url, json=data)
        self._handle_response(response)
        
        result = response.json()
        self.token = result['token']
        self.token_expiry = datetime.now() + timedelta(seconds=result['expiresIn'])
        
        # Update session headers
        self.session.headers.update({
            'Authorization': f"Bearer {self.token}"
        })
        
        return result['user']

    def is_token_valid(self) -> bool:
        """Check if current token is still valid"""
        return self.token and datetime.now() < self.token_expiry

    def _handle_response(self, response: requests.Response) -> None:
        """Handle API response errors"""
        if response.status_code == 401:
            self.token = None
            raise Exception("Authentication required")
        elif response.status_code == 403:
            raise Exception("Insufficient permissions")
        elif response.status_code == 429:
            retry_after = int(response.headers.get('Retry-After', 60))
            raise Exception(f"Rate limited. Retry after {retry_after} seconds")
        elif response.status_code >= 400:
            try:
                error_data = response.json()
                raise Exception(f"API error: {error_data.get('message', 'Unknown error')}")
            except ValueError:
                response.raise_for_status()

    def _make_request(self, method: str, endpoint: str, **kwargs) -> requests.Response:
        """Make authenticated API request"""
        if not self.is_token_valid():
            raise Exception("Authentication required")
        
        url = f"{self.base_url}{endpoint}"
        response = self.session.request(method, url, **kwargs)
        self._handle_response(response)
        return response

    # Certificate operations
    def get_certificates(self, **filters) -> Dict[str, Any]:
        """Get list of certificates with optional filters"""
        params = {k: v for k, v in filters.items() if v is not None}
        response = self._make_request('GET', '/certificates', params=params)
        return response.json()

    def get_certificate(self, certificate_id: str) -> Dict[str, Any]:
        """Get specific certificate by ID"""
        response = self._make_request('GET', f'/certificates/{certificate_id}')
        return response.json()

    def create_certificate(self, certificate_data: Dict[str, Any]) -> Dict[str, Any]:
        """Create new certificate"""
        response = self._make_request('POST', '/certificates', json=certificate_data)
        return response.json()

    def update_certificate(self, certificate_id: str, updates: Dict[str, Any]) -> Dict[str, Any]:
        """Update existing certificate"""
        response = self._make_request('PUT', f'/certificates/{certificate_id}', json=updates)
        return response.json()

    def delete_certificate(self, certificate_id: str) -> Dict[str, Any]:
        """Delete certificate"""
        response = self._make_request('DELETE', f'/certificates/{certificate_id}')
        return response.json()

    # Company operations
    def get_companies(self, search: str = None) -> Dict[str, Any]:
        """Get list of companies"""
        params = {'search': search} if search else {}
        response = self._make_request('GET', '/companies', params=params)
        return response.json()

    def create_company(self, company_data: Dict[str, Any]) -> Dict[str, Any]:
        """Create new company"""
        response = self._make_request('POST', '/companies', json=company_data)
        return response.json()

    # Utility methods
    def get_all_certificates(self, **filters) -> List[Dict[str, Any]]:
        """Get all certificates across all pages"""
        all_certificates = []
        page = 1
        page_size = 100
        
        while True:
            response = self.get_certificates(
                page=page,
                pageSize=page_size,
                **filters
            )
            
            all_certificates.extend(response['data'])
            
            if not response['pagination']['hasNext']:
                break
            
            page += 1
            time.sleep(0.1)  # Rate limiting courtesy
        
        return all_certificates

    def search_certificates(self, query: str, **additional_filters) -> List[Dict[str, Any]]:
        """Search certificates by query string"""
        return self.get_all_certificates(search=query, **additional_filters)

# Usage example
def main():
    # Initialize API client
    api = GreenCircleAPI(
        api_key=os.environ.get('GREENCIRCLE_API_KEY'),
        base_url='https://sandbox-api.greencircle.com/v1'
    )

    try:
        # Authenticate
        user = api.authenticate('user@example.com', 'password')
        print(f"Authenticated as: {user['name']}")

        # Get active certificates
        certificates = api.get_certificates(status='active', pageSize=10)
        print(f"Found {certificates['pagination']['totalItems']} active certificates")

        # Search for specific certificates
        lumber_certs = api.search_certificates('lumber', status='active')
        print(f"Found {len(lumber_certs)} lumber certificates")

        # Create new certificate
        new_cert_data = {
            'certificateNumber': '24-PYTHON-001',
            'companyId': 1,
            'productName': 'Python Test Product',
            'certificateType': 'Recycled Content',
            'issueDate': '2024-03-01',
            'expirationDate': '2025-03-01',
            'attributes': {
                'recycledContent': '50%',
                'postConsumerContent': '35%',
                'certificationStandard': 'ISO 14021'
            }
        }

        new_cert = api.create_certificate(new_cert_data)
        print(f"Created certificate: {new_cert['data']['certificateNumber']}")

        # Update certificate
        updates = {
            'productName': 'Updated Python Test Product',
            'attributes': {
                'recycledContent': '55%',
                'postConsumerContent': '40%'
            }
        }

        updated_cert = api.update_certificate(
            new_cert['data']['id'],
            updates
        )
        print(f"Updated certificate: {updated_cert['data']['productName']}")

    except Exception as error:
        print(f"Error: {error}")

if __name__ == '__main__':
    main()
```

### C# / .NET

#### Complete Integration Example

```csharp
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class GreenCircleApiClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private string _token;
    private DateTime _tokenExpiry;

    public GreenCircleApiClient(string baseUrl = null, string apiKey = null)
    {
        _baseUrl = baseUrl ?? "https://api.greencircle.com/v1";
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_baseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };

        _httpClient.DefaultRequestHeaders.Add("Content-Type", "application/json");
        
        if (!string.IsNullOrEmpty(apiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("X-API-Key", apiKey);
        }
    }

    public async Task<UserModel> AuthenticateAsync(string email, string password)
    {
        var loginData = new
        {
            email = email,
            password = password
        };

        var response = await PostAsync<LoginResponse>("/account/login", loginData);
        
        _token = response.Token;
        _tokenExpiry = DateTime.UtcNow.AddSeconds(response.ExpiresIn);
        
        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _token);
        
        return response.User;
    }

    private bool IsTokenValid()
    {
        return !string.IsNullOrEmpty(_token) && DateTime.UtcNow < _tokenExpiry;
    }

    // Certificate operations
    public async Task<CertificateListResponse> GetCertificatesAsync(
        int page = 1,
        int pageSize = 20,
        string status = null,
        string search = null,
        int? companyId = null)
    {
        EnsureAuthenticated();

        var queryParams = new List<string>();
        queryParams.Add($"page={page}");
        queryParams.Add($"pageSize={pageSize}");
        
        if (!string.IsNullOrEmpty(status))
            queryParams.Add($"status={Uri.EscapeDataString(status)}");
        
        if (!string.IsNullOrEmpty(search))
            queryParams.Add($"search={Uri.EscapeDataString(search)}");
        
        if (companyId.HasValue)
            queryParams.Add($"companyId={companyId.Value}");

        var queryString = string.Join("&", queryParams);
        return await GetAsync<CertificateListResponse>($"/certificates?{queryString}");
    }

    public async Task<CertificateDetailResponse> GetCertificateAsync(string certificateId)
    {
        EnsureAuthenticated();
        return await GetAsync<CertificateDetailResponse>($"/certificates/{certificateId}");
    }

    public async Task<CertificateResponse> CreateCertificateAsync(CreateCertificateRequest request)
    {
        EnsureAuthenticated();
        return await PostAsync<CertificateResponse>("/certificates", request);
    }

    public async Task<CertificateResponse> UpdateCertificateAsync(string certificateId, UpdateCertificateRequest request)
    {
        EnsureAuthenticated();
        return await PutAsync<CertificateResponse>($"/certificates/{certificateId}", request);
    }

    // Company operations
    public async Task<CompanyListResponse> GetCompaniesAsync(string search = null)
    {
        EnsureAuthenticated();
        var queryString = !string.IsNullOrEmpty(search) ? $"?search={Uri.EscapeDataString(search)}" : "";
        return await GetAsync<CompanyListResponse>($"/companies{queryString}");
    }

    public async Task<CompanyResponse> CreateCompanyAsync(CreateCompanyRequest request)
    {
        EnsureAuthenticated();
        return await PostAsync<CompanyResponse>("/companies", request);
    }

    // Utility methods
    public async Task<List<CertificateSummary>> GetAllCertificatesAsync(
        string status = null,
        string search = null,
        int? companyId = null)
    {
        var allCertificates = new List<CertificateSummary>();
        int page = 1;
        const int pageSize = 100;

        while (true)
        {
            var response = await GetCertificatesAsync(
                page: page,
                pageSize: pageSize,
                status: status,
                search: search,
                companyId: companyId
            );

            allCertificates.AddRange(response.Data);

            if (!response.Pagination.HasNext)
                break;

            page++;
            await Task.Delay(100); // Rate limiting courtesy
        }

        return allCertificates;
    }

    // HTTP helper methods
    private async Task<T> GetAsync<T>(string endpoint)
    {
        var response = await _httpClient.GetAsync(endpoint);
        return await ProcessResponse<T>(response);
    }

    private async Task<T> PostAsync<T>(string endpoint, object data)
    {
        var json = JsonSerializer.Serialize(data);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(endpoint, content);
        return await ProcessResponse<T>(response);
    }

    private async Task<T> PutAsync<T>(string endpoint, object data)
    {
        var json = JsonSerializer.Serialize(data);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PutAsync(endpoint, content);
        return await ProcessResponse<T>(response);
    }

    private async Task<T> ProcessResponse<T>(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            HandleErrorResponse(response, content);
        }

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        return JsonSerializer.Deserialize<T>(content, options);
    }

    private void HandleErrorResponse(HttpResponseMessage response, string content)
    {
        var statusCode = (int)response.StatusCode;
        
        switch (statusCode)
        {
            case 401:
                _token = null;
                throw new UnauthorizedAccessException("Authentication required");
            case 403:
                throw new UnauthorizedAccessException("Insufficient permissions");
            case 429:
                var retryAfter = response.Headers.RetryAfter?.Delta?.TotalSeconds ?? 60;
                throw new Exception($"Rate limited. Retry after {retryAfter} seconds");
            case 422:
                throw new ArgumentException($"Validation error: {content}");
            default:
                throw new HttpRequestException($"API error {statusCode}: {content}");
        }
    }

    private void EnsureAuthenticated()
    {
        if (!IsTokenValid())
        {
            throw new InvalidOperationException("Authentication required");
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

// Model classes
public class LoginResponse
{
    public string Token { get; set; }
    public int ExpiresIn { get; set; }
    public UserModel User { get; set; }
}

public class UserModel
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public string Role { get; set; }
}

public class CertificateListResponse
{
    public bool Success { get; set; }
    public List<CertificateSummary> Data { get; set; }
    public PaginationInfo Pagination { get; set; }
}

public class CertificateSummary
{
    public string Id { get; set; }
    public string CertificateNumber { get; set; }
    public string CompanyName { get; set; }
    public string ProductName { get; set; }
    public string CertificateType { get; set; }
    public DateTime IssueDate { get; set; }
    public DateTime ExpirationDate { get; set; }
    public string Status { get; set; }
}

public class PaginationInfo
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages { get; set; }
    public bool HasNext { get; set; }
    public bool HasPrevious { get; set; }
}

// Usage example
class Program
{
    static async Task Main(string[] args)
    {
        var client = new GreenCircleApiClient(
            baseUrl: "https://sandbox-api.greencircle.com/v1"
        );

        try
        {
            // Authenticate
            var user = await client.AuthenticateAsync("user@example.com", "password");
            Console.WriteLine($"Authenticated as: {user.Name}");

            // Get certificates
            var certificates = await client.GetCertificatesAsync(
                page: 1,
                pageSize: 10,
                status: "active"
            );
            Console.WriteLine($"Found {certificates.Pagination.TotalItems} certificates");

            // Create new certificate
            var newCert = await client.CreateCertificateAsync(new CreateCertificateRequest
            {
                CertificateNumber = "24-CSHARP-001",
                CompanyId = 1,
                ProductName = "C# Test Product",
                CertificateType = "Recycled Content",
                IssueDate = DateTime.Now,
                ExpirationDate = DateTime.Now.AddYears(1)
            });

            Console.WriteLine($"Created certificate: {newCert.Data.CertificateNumber}");

            // Get all active certificates
            var allActiveCerts = await client.GetAllCertificatesAsync(status: "active");
            Console.WriteLine($"Retrieved all {allActiveCerts.Count} active certificates");

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
        finally
        {
            client.Dispose();
        }
    }
}
```

## Common Integration Scenarios

### 1. E-commerce Platform Integration

```javascript
// Shopify app integration
class ShopifyGreenCircleIntegration {
  constructor(shopDomain, accessToken, greenCircleClient) {
    this.shopify = new Shopify(shopDomain, accessToken);
    this.greenCircle = greenCircleClient;
  }

  async syncProductCertificates() {
    // Get all products from Shopify
    const products = await this.shopify.getProducts();
    
    for (const product of products) {
      // Check if product has certification info in metafields
      const certNumber = product.metafields?.certification_number;
      
      if (certNumber) {
        try {
          // Get certificate details from GreenCircle
          const certificate = await this.greenCircle.getCertificate(certNumber);
          
          // Update product with certificate data
          await this.updateProductWithCertificate(product, certificate);
        } catch (error) {
          console.warn(`Certificate ${certNumber} not found for product ${product.id}`);
        }
      }
    }
  }

  async updateProductWithCertificate(product, certificate) {
    const metafields = [
      {
        key: 'certification_type',
        value: certificate.certificateType,
        type: 'single_line_text_field'
      },
      {
        key: 'certification_status',
        value: certificate.status,
        type: 'single_line_text_field'
      },
      {
        key: 'certification_expiry',
        value: certificate.expirationDate,
        type: 'date'
      }
    ];

    await this.shopify.updateProductMetafields(product.id, metafields);
  }
}
```

### 2. CRM Integration

```python
# Salesforce integration
class SalesforceCertificateSync:
    def __init__(self, sf_client, gc_client):
        self.salesforce = sf_client
        self.greencircle = gc_client

    def sync_opportunities_with_certificates(self):
        """Sync Salesforce opportunities with certificate data"""
        
        # Get opportunities that need certificate sync
        opportunities = self.salesforce.query("""
            SELECT Id, Company_Name__c, Product_Name__c, Certificate_Number__c
            FROM Opportunity
            WHERE Certificate_Number__c != NULL
            AND Certificate_Status__c = NULL
        """)

        for opp in opportunities['records']:
            cert_number = opp['Certificate_Number__c']
            
            try:
                # Get certificate from GreenCircle
                certificate = self.greencircle.get_certificate(cert_number)
                
                # Update opportunity with certificate status
                self.salesforce.sobject('Opportunity').update(opp['Id'], {
                    'Certificate_Status__c': certificate['status'],
                    'Certificate_Type__c': certificate['certificateType'],
                    'Certificate_Expiry__c': certificate['expirationDate'],
                    'Sustainability_Score__c': self.calculate_score(certificate)
                })
                
            except Exception as error:
                print(f"Error syncing certificate {cert_number}: {error}")

    def calculate_score(self, certificate):
        """Calculate sustainability score based on certificate attributes"""
        attributes = certificate.get('attributes', {})
        score = 0
        
        # Example scoring logic
        if 'recycledContent' in attributes:
            recycled_pct = float(attributes['recycledContent'].rstrip('%'))
            score += min(recycled_pct, 50)  # Max 50 points for recycled content
        
        if certificate['certificateType'] == 'Carbon Neutral':
            score += 30
        
        return min(score, 100)  # Cap at 100
```

### 3. Data Warehouse Integration

```python
# ETL pipeline for data warehouse
import pandas as pd
from sqlalchemy import create_engine

class CertificateETL:
    def __init__(self, gc_client, db_connection_string):
        self.gc_client = gc_client
        self.engine = create_engine(db_connection_string)

    def extract_certificates(self):
        """Extract all certificates from GreenCircle API"""
        certificates = self.gc_client.get_all_certificates()
        return pd.DataFrame(certificates)

    def transform_certificates(self, df):
        """Transform certificate data for data warehouse"""
        
        # Normalize nested attributes
        attributes_df = pd.json_normalize(df['attributes'])
        attributes_df = attributes_df.add_prefix('attr_')
        
        # Combine with main data
        transformed = pd.concat([df.drop('attributes', axis=1), attributes_df], axis=1)
        
        # Data type conversions
        transformed['issueDate'] = pd.to_datetime(transformed['issueDate'])
        transformed['expirationDate'] = pd.to_datetime(transformed['expirationDate'])
        
        # Calculate certificate age and validity
        transformed['certificate_age_days'] = (
            pd.Timestamp.now() - transformed['issueDate']
        ).dt.days
        
        transformed['days_until_expiry'] = (
            transformed['expirationDate'] - pd.Timestamp.now()
        ).dt.days
        
        transformed['is_expired'] = transformed['days_until_expiry'] < 0
        
        return transformed

    def load_certificates(self, df):
        """Load transformed data to data warehouse"""
        
        # Load to staging table first
        df.to_sql(
            'certificates_staging',
            self.engine,
            if_exists='replace',
            index=False
        )
        
        # Execute stored procedure to merge into production table
        with self.engine.connect() as conn:
            conn.execute("""
                EXEC sp_MergeCertificates
            """)

    def run_etl(self):
        """Run complete ETL pipeline"""
        print("Starting certificate ETL...")
        
        # Extract
        print("Extracting certificates...")
        df = self.extract_certificates()
        print(f"Extracted {len(df)} certificates")
        
        # Transform
        print("Transforming data...")
        df_transformed = self.transform_certificates(df)
        
        # Load
        print("Loading to data warehouse...")
        self.load_certificates(df_transformed)
        
        print("ETL complete!")
        
        return {
            'total_certificates': len(df),
            'active_certificates': len(df[df['status'] == 'active']),
            'expired_certificates': len(df_transformed[df_transformed['is_expired']])
        }
```

## Webhooks

### Setting Up Webhooks

Configure webhooks to receive real-time notifications about certificate changes:

```bash
# Register webhook endpoint
curl -X POST https://api.greencircle.com/v1/webhooks \
  -H "Authorization: Bearer your-token" \
  -H "Content-Type: application/json" \
  -d '{
    "url": "https://yourapp.com/webhooks/greencircle",
    "events": ["certificate.created", "certificate.updated", "certificate.expired"],
    "secret": "webhook-secret-key"
  }'
```

### Webhook Handler Example

```javascript
const express = require('express');
const crypto = require('crypto');

const app = express();
app.use(express.raw({ type: 'application/json' }));

const WEBHOOK_SECRET = process.env.GREENCIRCLE_WEBHOOK_SECRET;

function verifyWebhookSignature(payload, signature, secret) {
  const hmac = crypto.createHmac('sha256', secret);
  hmac.update(payload);
  const expectedSignature = 'sha256=' + hmac.digest('hex');
  
  return crypto.timingSafeEqual(
    Buffer.from(signature),
    Buffer.from(expectedSignature)
  );
}

app.post('/webhooks/greencircle', (req, res) => {
  const signature = req.headers['x-greencircle-signature'];
  const payload = req.body;

  // Verify webhook signature
  if (!verifyWebhookSignature(payload, signature, WEBHOOK_SECRET)) {
    return res.status(401).send('Invalid signature');
  }

  const event = JSON.parse(payload);
  
  // Handle different event types
  switch (event.type) {
    case 'certificate.created':
      handleCertificateCreated(event.data);
      break;
    case 'certificate.updated':
      handleCertificateUpdated(event.data);
      break;
    case 'certificate.expired':
      handleCertificateExpired(event.data);
      break;
    default:
      console.log(`Unknown event type: ${event.type}`);
  }

  res.status(200).send('OK');
});

function handleCertificateCreated(certificate) {
  console.log(`New certificate created: ${certificate.certificateNumber}`);
  
  // Example: Send notification to Slack
  sendSlackNotification(
    `🎉 New certificate created: ${certificate.certificateNumber} for ${certificate.companyName}`
  );
  
  // Example: Update local database
  updateLocalCertificateCache(certificate);
}

function handleCertificateExpired(certificate) {
  console.log(`Certificate expired: ${certificate.certificateNumber}`);
  
  // Example: Send alert email
  sendExpirationAlert(certificate);
  
  // Example: Update product listings
  updateProductCertificationStatus(certificate, 'expired');
}

app.listen(3000, () => {
  console.log('Webhook handler listening on port 3000');
});
```

## Best Practices

### 1. Error Handling and Resilience

```javascript
class ResilientApiClient {
  constructor(config) {
    this.client = new GreenCircleAPI(config);
    this.retryConfig = {
      maxRetries: 3,
      baseDelay: 1000,
      maxDelay: 10000
    };
  }

  async makeResilientRequest(operation, ...args) {
    let lastError;
    
    for (let attempt = 0; attempt <= this.retryConfig.maxRetries; attempt++) {
      try {
        return await operation.apply(this.client, args);
      } catch (error) {
        lastError = error;
        
        // Don't retry for client errors (4xx)
        if (error.status >= 400 && error.status < 500) {
          throw error;
        }
        
        // Don't retry on last attempt
        if (attempt === this.retryConfig.maxRetries) {
          break;
        }
        
        // Calculate exponential backoff delay
        const delay = Math.min(
          this.retryConfig.baseDelay * Math.pow(2, attempt),
          this.retryConfig.maxDelay
        );
        
        console.log(`Attempt ${attempt + 1} failed, retrying in ${delay}ms`);
        await new Promise(resolve => setTimeout(resolve, delay));
      }
    }
    
    throw lastError;
  }

  // Wrapper methods with resilience
  async getCertificates(...args) {
    return this.makeResilientRequest(this.client.getCertificates, ...args);
  }

  async createCertificate(...args) {
    return this.makeResilientRequest(this.client.createCertificate, ...args);
  }
}
```

### 2. Caching Strategy

```javascript
class CachedGreenCircleClient {
  constructor(apiClient, cacheConfig = {}) {
    this.api = apiClient;
    this.cache = new Map();
    this.cacheConfig = {
      ttl: cacheConfig.ttl || 300000, // 5 minutes default
      maxSize: cacheConfig.maxSize || 1000
    };
  }

  getCacheKey(method, ...args) {
    return `${method}:${JSON.stringify(args)}`;
  }

  isCacheValid(cacheEntry) {
    return Date.now() - cacheEntry.timestamp < this.cacheConfig.ttl;
  }

  setCache(key, data) {
    // Implement LRU eviction if cache is full
    if (this.cache.size >= this.cacheConfig.maxSize) {
      const firstKey = this.cache.keys().next().value;
      this.cache.delete(firstKey);
    }

    this.cache.set(key, {
      data,
      timestamp: Date.now()
    });
  }

  async getCachedCertificate(certificateId) {
    const cacheKey = this.getCacheKey('getCertificate', certificateId);
    const cached = this.cache.get(cacheKey);

    if (cached && this.isCacheValid(cached)) {
      console.log(`Cache hit for certificate ${certificateId}`);
      return cached.data;
    }

    console.log(`Cache miss for certificate ${certificateId}`);
    const certificate = await this.api.getCertificate(certificateId);
    this.setCache(cacheKey, certificate);
    
    return certificate;
  }

  // Cache invalidation
  invalidateCertificate(certificateId) {
    const patterns = [
      `getCertificate:["${certificateId}"]`,
      'getCertificates:', // Invalidate list caches too
    ];

    for (const [key] of this.cache.entries()) {
      if (patterns.some(pattern => key.startsWith(pattern))) {
        this.cache.delete(key);
      }
    }
  }
}
```

### 3. Rate Limiting

```javascript
class RateLimitedClient {
  constructor(apiClient, rateLimit = 10) {
    this.api = apiClient;
    this.rateLimit = rateLimit; // requests per second
    this.requests = [];
  }

  async throttledRequest(operation, ...args) {
    // Remove requests older than 1 second
    const now = Date.now();
    this.requests = this.requests.filter(time => now - time < 1000);

    // Wait if we've exceeded rate limit
    if (this.requests.length >= this.rateLimit) {
      const oldestRequest = Math.min(...this.requests);
      const waitTime = 1000 - (now - oldestRequest);
      
      if (waitTime > 0) {
        console.log(`Rate limit reached, waiting ${waitTime}ms`);
        await new Promise(resolve => setTimeout(resolve, waitTime));
      }
    }

    // Record this request
    this.requests.push(Date.now());

    // Make the API call
    return await operation.apply(this.api, args);
  }
}
```

### 4. Configuration Management

```javascript
// config.js
const config = {
  development: {
    baseUrl: 'https://sandbox-api.greencircle.com/v1',
    timeout: 30000,
    retries: 3,
    cacheConfig: {
      ttl: 60000, // 1 minute in dev
      maxSize: 100
    }
  },
  production: {
    baseUrl: 'https://api.greencircle.com/v1',
    timeout: 10000,
    retries: 5,
    cacheConfig: {
      ttl: 300000, // 5 minutes in prod
      maxSize: 1000
    }
  }
};

module.exports = config[process.env.NODE_ENV || 'development'];
```

## Testing and Debugging

### Unit Testing

```javascript
// Jest test example
const GreenCircleAPI = require('./greencircle-api');

describe('GreenCircleAPI', () => {
  let api;
  let mockFetch;

  beforeEach(() => {
    mockFetch = jest.fn();
    global.fetch = mockFetch;
    api = new GreenCircleAPI({ baseURL: 'https://test.api.com' });
  });

  test('should authenticate successfully', async () => {
    const mockResponse = {
      token: 'test-token',
      expiresIn: 3600,
      user: { id: 1, name: 'Test User' }
    };

    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: () => Promise.resolve(mockResponse)
    });

    const user = await api.authenticate('test@example.com', 'password');
    
    expect(user.name).toBe('Test User');
    expect(api.token).toBe('test-token');
    expect(mockFetch).toHaveBeenCalledWith(
      expect.stringContaining('/account/login'),
      expect.objectContaining({
        method: 'POST',
        body: JSON.stringify({
          email: 'test@example.com',
          password: 'password'
        })
      })
    );
  });

  test('should handle authentication failure', async () => {
    mockFetch.mockResolvedValueOnce({
      ok: false,
      status: 401,
      json: () => Promise.resolve({
        success: false,
        message: 'Invalid credentials'
      })
    });

    await expect(api.authenticate('bad@example.com', 'wrong'))
      .rejects.toThrow('Invalid credentials');
  });

  test('should get certificates with filters', async () => {
    const mockCertificates = {
      success: true,
      data: [
        { id: '1', certificateNumber: '24-1605', status: 'active' }
      ],
      pagination: { page: 1, totalItems: 1 }
    };

    mockFetch.mockResolvedValueOnce({
      ok: true,
      json: () => Promise.resolve(mockCertificates)
    });

    api.token = 'test-token';
    api.tokenExpiry = Date.now() + 3600000;

    const result = await api.getCertificates({ status: 'active' });
    
    expect(result.data).toHaveLength(1);
    expect(result.data[0].certificateNumber).toBe('24-1605');
  });
});
```

### Integration Testing

```javascript
// Integration test
describe('GreenCircle API Integration', () => {
  let api;

  beforeAll(() => {
    api = new GreenCircleAPI({
      baseURL: process.env.GREENCIRCLE_TEST_URL,
      apiKey: process.env.GREENCIRCLE_TEST_API_KEY
    });
  });

  test('full certificate workflow', async () => {
    // Authenticate
    await api.authenticate(
      process.env.TEST_EMAIL,
      process.env.TEST_PASSWORD
    );

    // Create certificate
    const newCert = await api.createCertificate({
      certificateNumber: `TEST-${Date.now()}`,
      companyId: 1,
      productName: 'Integration Test Product',
      certificateType: 'Recycled Content',
      issueDate: new Date().toISOString().split('T')[0],
      expirationDate: new Date(Date.now() + 365*24*60*60*1000)
        .toISOString().split('T')[0]
    });

    expect(newCert.success).toBe(true);
    expect(newCert.data.certificateNumber).toMatch(/^TEST-/);

    const certificateId = newCert.data.id;

    // Get certificate
    const retrieved = await api.getCertificate(certificateId);
    expect(retrieved.productName).toBe('Integration Test Product');

    // Update certificate
    const updated = await api.updateCertificate(certificateId, {
      productName: 'Updated Integration Test Product'
    });

    expect(updated.success).toBe(true);
    expect(updated.data.productName).toBe('Updated Integration Test Product');

    // Clean up - delete certificate
    const deleted = await api.deleteCertificate(certificateId);
    expect(deleted.success).toBe(true);
  });
});
```

### Debug Logging

```javascript
class DebugGreenCircleAPI extends GreenCircleAPI {
  constructor(config) {
    super(config);
    this.debug = config.debug || false;
  }

  log(level, message, data = null) {
    if (!this.debug) return;

    const timestamp = new Date().toISOString();
    console.log(`[${timestamp}] ${level.toUpperCase()}: ${message}`);
    
    if (data) {
      console.log(JSON.stringify(data, null, 2));
    }
  }

  async makeRequest(method, endpoint, data = null) {
    const requestId = Math.random().toString(36).substr(2, 9);
    
    this.log('info', `[${requestId}] ${method} ${endpoint}`, {
      endpoint,
      method,
      data: data ? this.sanitizeData(data) : null
    });

    const startTime = Date.now();
    
    try {
      const response = await super.makeRequest(method, endpoint, data);
      const duration = Date.now() - startTime;
      
      this.log('info', `[${requestId}] Response received (${duration}ms)`, {
        status: 'success',
        duration
      });
      
      return response;
    } catch (error) {
      const duration = Date.now() - startTime;
      
      this.log('error', `[${requestId}] Request failed (${duration}ms)`, {
        error: error.message,
        status: error.status || 'unknown',
        duration
      });
      
      throw error;
    }
  }

  sanitizeData(data) {
    // Remove sensitive information from logs
    const sanitized = { ...data };
    const sensitiveFields = ['password', 'token', 'apiKey'];
    
    sensitiveFields.forEach(field => {
      if (sanitized[field]) {
        sanitized[field] = '[REDACTED]';
      }
    });
    
    return sanitized;
  }
}
```

## Performance Optimization

### 1. Batch Operations

```javascript
class BatchProcessor {
  constructor(apiClient, options = {}) {
    this.api = apiClient;
    this.batchSize = options.batchSize || 10;
    this.concurrency = options.concurrency || 3;
    this.delayBetweenBatches = options.delay || 1000;
  }

  async processBatch(items, processor) {
    const results = [];
    
    // Split items into batches
    for (let i = 0; i < items.length; i += this.batchSize) {
      const batch = items.slice(i, i + this.batchSize);
      
      // Process batch with limited concurrency
      const batchPromises = batch.map(item => 
        this.withRetry(() => processor(item))
      );
      
      // Wait for current batch to complete
      const batchResults = await Promise.allSettled(batchPromises);
      results.push(...batchResults);
      
      // Progress logging
      console.log(`Processed batch ${Math.floor(i / this.batchSize) + 1}/${Math.ceil(items.length / this.batchSize)}`);
      
      // Delay between batches (rate limiting)
      if (i + this.batchSize < items.length) {
        await new Promise(resolve => setTimeout(resolve, this.delayBetweenBatches));
      }
    }
    
    return results;
  }

  async withRetry(operation, maxRetries = 3) {
    for (let attempt = 0; attempt <= maxRetries; attempt++) {
      try {
        return await operation();
      } catch (error) {
        if (attempt === maxRetries || error.status < 500) {
          throw error;
        }
        
        const delay = Math.pow(2, attempt) * 1000;
        await new Promise(resolve => setTimeout(resolve, delay));
      }
    }
  }
}

// Usage example
const processor = new BatchProcessor(apiClient, {
  batchSize: 5,
  concurrency: 2,
  delay: 500
});

const certificates = [/* array of certificate data */];
const results = await processor.processBatch(certificates, async (cert) => {
  return await apiClient.createCertificate(cert);
});

const successful = results.filter(r => r.status === 'fulfilled').length;
const failed = results.filter(r => r.status === 'rejected').length;
console.log(`Created ${successful} certificates, ${failed} failed`);
```

### 2. Connection Pooling

```javascript
// HTTP/2 connection pooling
const http2 = require('http2');

class Http2ApiClient {
  constructor(baseUrl) {
    this.baseUrl = baseUrl;
    this.sessions = new Map();
    this.maxSessions = 5;
  }

  getSession(hostname) {
    if (!this.sessions.has(hostname)) {
      if (this.sessions.size >= this.maxSessions) {
        // Remove oldest session
        const oldestKey = this.sessions.keys().next().value;
        const oldestSession = this.sessions.get(oldestKey);
        oldestSession.close();
        this.sessions.delete(oldestKey);
      }

      const session = http2.connect(`https://${hostname}`);
      this.sessions.set(hostname, session);
      
      session.on('error', (error) => {
        console.error(`Session error for ${hostname}:`, error);
        this.sessions.delete(hostname);
      });
    }

    return this.sessions.get(hostname);
  }

  async makeRequest(method, path, data = null) {
    const url = new URL(path, this.baseUrl);
    const session = this.getSession(url.hostname);

    return new Promise((resolve, reject) => {
      const headers = {
        ':method': method,
        ':path': path,
        'content-type': 'application/json',
        'authorization': `Bearer ${this.token}`
      };

      const req = session.request(headers);
      
      let responseData = '';
      req.on('data', chunk => responseData += chunk);
      req.on('end', () => {
        try {
          resolve(JSON.parse(responseData));
        } catch (error) {
          reject(error);
        }
      });
      req.on('error', reject);

      if (data) {
        req.write(JSON.stringify(data));
      }
      
      req.end();
    });
  }

  cleanup() {
    for (const session of this.sessions.values()) {
      session.close();
    }
    this.sessions.clear();
  }
}
```

---

This comprehensive integration guide provides everything needed to successfully integrate with the GreenCircle Certified API across multiple programming languages and scenarios. The examples are production-ready and include proper error handling, authentication, rate limiting, and performance optimization techniques.