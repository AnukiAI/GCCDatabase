# API Reference

## Authentication
- Cookie auth for web UI
- API Key header for programmatic access: `X-API-Key: {key}`
- Partner-specific API keys managed via Admin > Partner API

## Public Endpoints
```
GET /api/v1/certificates                    # List certificates
GET /api/v1/certificates/{id}               # Get by ID
GET /api/v1/certificates/number/{number}    # Get by certificate number
GET /api/v1/certificates/{id}/download      # Download PDF
GET /api/v1/certificates/search/productid   # Search by product ID
```

### Query Parameters
- `pageNumber` (default: 1)
- `pageSize` (default: 20, max: 100)
- `includeProductIds` (default: false)
- `status` (Active, Draft, Expired, etc.)
- `companyId`, `productId`, `partnerId`

## Internal Tile APIs
```
GET /api/CertificateTiles/*                 # Public tile view
GET /api/CertificateTileInternal/*          # Internal view (Analyst+)
GET /api/CertificateTileExternal/*          # External view (External+)
```

## Novi Integration

Partners pull data via GCC REST API with dedicated API keys.

### Field Mapping
| Novi Requirement | GCC API Field |
|------------------|---------------|
| Brand name | BrandName (Company.Name) |
| Variant / product name | ProductName (Product.Name) |
| Variant identifier | ProductIdentifiers[] (use `includeProductIds=true`) |
| Certification name | CertificationName (CertificationType.Name) |
| Certificate / license ID | CertificateNumber |
| Issue date | IssueDate (EffectiveDate) |
| Expiration date | ExpirationDate |
| Issuer | "Green Circle Certified" (static) |
| Standard version | StandardVersion (Subcategory) |

### Novi Endpoints
```bash
# List with product identifiers
GET /api/v1/certificates?includeProductIds=true&pageSize=100

# Search by GTIN
GET /api/v1/certificates/search/productid?productId={GTIN}&type=GTIN

# Get by certificate number
GET /api/v1/certificates/number/24-0366

# Download PDF
GET /api/v1/certificates/24-0366/download
```

### Sample Response
```json
{
  "success": true,
  "data": [{
    "id": 123,
    "certificateNumber": "24-0366",
    "status": "Active",
    "issueDate": "2024-01-15T00:00:00Z",
    "expirationDate": "2025-01-15T00:00:00Z",
    "brandName": "EcoProduct Co",
    "productName": "Recycled Paper Towels",
    "certificationName": "UL 2809",
    "issuer": "Green Circle Certified",
    "standardVersion": "Level 2",
    "productIdentifiers": [
      {
        "type": "ASIN",
        "value": "B08N5WRWNW",
        "status": "Active",
        "partner": "Amazon",
        "productName": "Sample Product",
        "productSku": "SKU123",
        "description": "Sample description"
      },
      {
        "type": "UPC",
        "value": "123456789012",
        "status": "Active",
        "partner": "None",
        "productName": "Another Product",
        "productSku": "SKU456",
        "description": "Another description"
      }
    ],
    "pdfDownloadUrl": "/api/v1/certificates/24-0366/download"
  }],
  "totalCount": 150,
  "pageNumber": 1,
  "pageSize": 100
}
```

### Product Identifier Fields
| Field | Type | Description |
|-------|------|-------------|
| type | string | Product ID type: ASIN, UPC, GTIN, EAN, ISBN, SKU, MPN |
| value | string | The actual product identifier value |
| status | string | Status: Active, Inactive, Pending, Expired |
| partner | string | Partner association: Amazon, Novi, None, Other |
| productName | string | Optional product name for this specific ID |
| productSku | string | Optional SKU |
| description | string | Optional description or notes |

## Certificate Thumbnail API
```
POST /api/CertificatesApi/GenerateThumbnail
```
- Accepts PDF file upload
- Returns PNG thumbnail path
- Used during Create/Edit for real-time preview

## Partner API Management
Admin interface: `/Admin/PartnerAPI`
- Generate/revoke API keys per partner
- View usage statistics
- Configure endpoint permissions
- Rate limits and quotas

## Product ID Types
- ASIN, UPC, GTIN, EAN, ISBN, SKU, MPN
- Bulk CSV import/export supported
- Multiple IDs per certificate (100s)

## Swagger
Development: http://localhost:5101/swagger
