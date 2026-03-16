# Product Identifiers Flow

This document describes the complete flow of the Product ID Manager component, which handles ASINs, UPCs, GTINs, EANs, ISBNs, SKUs, MPNs, and other product identifiers for certificates.

## Table of Contents
1. [Architecture Overview](#architecture-overview)
2. [Component Initialization](#component-initialization)
3. [CRUD Operations](#crud-operations)
4. [Validation System](#validation-system)
5. [Import/Export Flow](#importexport-flow)
6. [Filtering & Search](#filtering--search)
7. [Create vs Edit Page Behavior](#create-vs-edit-page-behavior)
8. [API Endpoints](#api-endpoints)
9. [Data Model](#data-model)

---

## Architecture Overview

### System Components
```mermaid
flowchart TB
    subgraph "Frontend - Browser"
        UI[_ProductIdManager.cshtml]
        JS[product-id-manager.js]
        MODAL[Add/Edit Modal]
        IMPORT[Import Preview Modal]
        TABLE[Product IDs Table]
    end

    subgraph "Backend - ASP.NET Core"
        API[ProductIdApiController]
        SVC[ProductIdService]
        REPO[Repository]
        DB[(Database)]
    end

    UI --> JS
    JS --> MODAL
    JS --> IMPORT
    JS --> TABLE

    JS -->|AJAX| API
    API --> SVC
    SVC --> REPO
    REPO --> DB
```

### Component Responsibility Matrix
```mermaid
flowchart LR
    subgraph "JavaScript - ProductIdManager Class"
        A1[UI Event Handling]
        A2[Local State Management]
        A3[Validation - Client Side]
        A4[Table Rendering]
        A5[Modal Management]
        A6[Import Preview Logic]
        A7[Export CSV Generation]
    end

    subgraph "API Controller"
        B1[Request Validation]
        B2[Authorization]
        B3[Response Formatting]
        B4[Error Handling]
    end

    subgraph "Service Layer"
        C1[Business Logic]
        C2[Validation - Server Side]
        C3[CSV Parsing]
        C4[Data Transformation]
    end
```

---

## Component Initialization

### Initialization Flow
```mermaid
flowchart TD
    A[DOM Content Loaded] --> B{Find .product-id-manager Element?}
    B -->|No| C[Exit - No Manager on Page]
    B -->|Yes| D[Read data-certificate-id Attribute]

    D --> E[Read data-readonly Attribute]
    E --> F[Create ProductIdManager Instance]

    F --> G[constructor Called]
    G --> H[Store Container Reference]
    H --> I[Store Certificate ID]
    I --> J[Store ReadOnly Flag]
    J --> K[Initialize Empty Arrays]
    K --> L[Call init Method]

    L --> M[bindEvents]
    M --> N[loadProductIds]
    N --> O[initializeTypeHelp]

    subgraph "bindEvents - Event Listeners"
        M --> M1[Add Product ID Button]
        M --> M2[Import Button]
        M --> M3[Export Button]
        M --> M4[Filter Dropdowns]
        M --> M5[Search Input]
        M --> M6[Form Submit]
        M --> M7[Table Row Actions - Delegation]
        M --> M8[Type Change - Help Text]
        M --> M9[Value Input - Live Validation]
    end

    subgraph "loadProductIds"
        N --> N1{Certificate ID = 0?}
        N1 -->|Yes| N2[Initialize Empty State]
        N1 -->|No| N3[Fetch from API]
        N3 --> N4[Parse Response]
        N4 --> N5[Store in productIds Array]
        N5 --> N6[Copy to filteredProductIds]
        N6 --> N7[Render Table]
        N7 --> N8[Update Count Display]
    end
```

### State Management
```mermaid
stateDiagram-v2
    [*] --> Uninitialized

    Uninitialized --> Initializing: DOM Ready
    Initializing --> Empty: Certificate ID = 0
    Initializing --> Loading: Certificate ID > 0

    Loading --> Loaded: API Success
    Loading --> Error: API Failure

    Loaded --> Filtering: User Applies Filter
    Filtering --> Loaded: Filter Applied

    Loaded --> Adding: User Clicks Add
    Adding --> Loaded: Save Success
    Adding --> Adding: Validation Error

    Loaded --> Editing: User Clicks Edit
    Editing --> Loaded: Save Success
    Editing --> Editing: Validation Error

    Loaded --> Deleting: User Confirms Delete
    Deleting --> Loaded: Delete Success

    Empty --> Adding: User Clicks Add
    Adding --> WithData: First Item Added

    WithData --> Empty: Last Item Deleted
```

---

## CRUD Operations

### Add Product ID Flow
```mermaid
flowchart TD
    A[User Clicks Add Product ID] --> B[showAddModal Called]
    B --> C[resetForm - Clear All Fields]
    C --> D[Set Modal Title = Add Product ID]
    D --> E[Show Bootstrap Modal]

    E --> F[User Fills Form]
    F --> G[Type Selection Triggers Help Update]
    G --> H[Value Input Triggers Live Validation]

    H --> I[User Clicks Save]
    I --> J[handleFormSubmit Called]
    J --> K[preventDefault]

    K --> L{validateForm?}
    L -->|No| M[Show Error Toast]
    M --> F

    L -->|Yes| N{Certificate ID = 0?}

    subgraph "Create Page - In-Memory Storage"
        N -->|Yes| O[Generate Temp ID: -Date.now]
        O --> P[Create Product ID Object]
        P --> Q[Push to productIds Array]
        Q --> R[Copy to filteredProductIds]
        R --> S[Render Table]
        S --> T[Close Modal]
        T --> U[Show Success Toast]
    end

    subgraph "Edit Page - API Storage"
        N -->|No| V[Show Loading]
        V --> W[POST /api/productids]
        W --> X{Response OK?}
        X -->|No| Y[Show Error Toast]
        Y --> Z[Hide Loading]

        X -->|Yes| AA[Close Modal]
        AA --> AB[loadProductIds - Refresh]
        AB --> AC[Show Success Toast]
        AC --> Z
    end
```

### Edit Product ID Flow
```mermaid
flowchart TD
    A[User Clicks Edit Button] --> B[handleTableAction - Event Delegation]
    B --> C[Extract data-product-id]
    C --> D[showEditModal Called]

    D --> E{Certificate ID = 0?}

    subgraph "Create Page - Find in Memory"
        E -->|Yes| F[Find in productIds Array by Temp ID]
        F --> G[populateForm with Data]
    end

    subgraph "Edit Page - Fetch from API"
        E -->|No| H[GET /api/productids/{id}]
        H --> I{Response OK?}
        I -->|No| J[Show Error Toast]
        I -->|Yes| K[populateForm with Response Data]
    end

    G --> L[Set Modal Title = Edit Product ID]
    K --> L
    L --> M[Show Bootstrap Modal]

    M --> N[User Modifies Form]
    N --> O[User Clicks Save]
    O --> P[handleFormSubmit Called]

    P --> Q{validateForm?}
    Q -->|No| R[Show Error Toast]
    R --> N

    Q -->|Yes| S{Certificate ID = 0?}

    subgraph "Create Page - Update Memory"
        S -->|Yes| T[Find Index by Temp ID]
        T --> U[Update Object in Array]
        U --> V[Render Table]
        V --> W[Close Modal]
        W --> X[Show Success Toast]
    end

    subgraph "Edit Page - Update via API"
        S -->|No| Y[PUT /api/productids/{id}]
        Y --> Z{Response OK?}
        Z -->|No| AA[Show Error Toast]
        Z -->|Yes| AB[Close Modal]
        AB --> AC[loadProductIds - Refresh]
        AC --> AD[Show Success Toast]
    end
```

### Delete Product ID Flow
```mermaid
flowchart TD
    A[User Clicks Delete Button] --> B[handleTableAction - Event Delegation]
    B --> C[Extract data-product-id]
    C --> D[handleDelete Called]

    D --> E[AdminCore.showConfirmation]
    E --> F{User Confirms?}
    F -->|No| G[Exit - No Action]

    F -->|Yes| H{Product ID < 0 OR Certificate ID = 0?}

    subgraph "In-Memory Delete"
        H -->|Yes| I[Filter Out from productIds]
        I --> J[Copy to filteredProductIds]
        J --> K[Render Table]
        K --> L[Update Count]
        L --> M[Show Success Toast]
    end

    subgraph "API Delete"
        H -->|No| N[Show Loading]
        N --> O[DELETE /api/productids/{id}]
        O --> P{Response OK?}
        P -->|No| Q[Show Error Toast]
        P -->|Yes| R[loadProductIds - Refresh]
        R --> S[Show Success Toast]
        Q --> T[Hide Loading]
        S --> T
    end
```

---

## Validation System

### Client-Side Validation
```mermaid
flowchart TD
    A[User Enters Value] --> B[validateProductIdValue Called]
    B --> C[Get Selected Type]
    C --> D{Type and Value Present?}
    D -->|No| E[Clear Validation State]

    D -->|Yes| F[isValidProductId Called]

    F --> G{Type = ISBN?}
    G -->|Yes| H[validateIsbn]
    G -->|No| I[Use Regex Pattern]

    subgraph "ISBN Validation"
        H --> H1{Length = 10?}
        H1 -->|Yes| H2[validateIsbn10Checksum]
        H1 -->|No| H3{Length = 13?}
        H3 -->|Yes| H4[validateIsbn13Checksum]
        H3 -->|No| H5[Invalid]

        H2 --> H6{Modulo 11 = 0?}
        H6 -->|Yes| H7[Valid]
        H6 -->|No| H5

        H4 --> H8{Starts with 978/979?}
        H8 -->|No| H5
        H8 -->|Yes| H9{Modulo 10 = 0?}
        H9 -->|Yes| H7
        H9 -->|No| H5
    end

    subgraph "Regex Patterns"
        I --> I1{Type?}
        I1 -->|ASIN| I2[/^A-Z0-9{10}$/]
        I1 -->|UPC| I3[/^0-9{12}$/]
        I1 -->|GTIN| I4[/^0-9{8}$|^0-9{12}$|^0-9{13}$|^0-9{14}$/]
        I1 -->|EAN| I5[/^0-9{13}$/]
        I1 -->|SKU| I6[/^A-Z0-9-_{3,50}$/i]
        I1 -->|MPN| I7[/^A-Z0-9-_.{1,50}$/i]
        I1 -->|OTHER| I8[/.{1,50}/]
    end

    H7 --> J[setValidationState - Valid]
    H5 --> K[setValidationState - Invalid]
    I2 & I3 & I4 & I5 & I6 & I7 & I8 --> L{Pattern Match?}
    L -->|Yes| J
    L -->|No| K

    J --> M[Add is-valid Class]
    M --> N[Clear Error Message]

    K --> O[Add is-invalid Class]
    O --> P[Show Error Message]
```

### Validation Format Reference
```mermaid
flowchart LR
    subgraph "Product ID Types & Formats"
        ASIN[ASIN: 10 alphanumeric<br>e.g., B08N5WRWNW]
        UPC[UPC: 12 digits<br>e.g., 123456789012]
        GTIN[GTIN: 8, 12, 13, or 14 digits<br>e.g., 12345678901234]
        EAN[EAN: 13 digits<br>e.g., 1234567890123]
        ISBN10[ISBN-10: 10 digits + X<br>e.g., 0-306-40615-2]
        ISBN13[ISBN-13: 13 digits 978/979<br>e.g., 978-0-306-40615-7]
        SKU[SKU: 3-50 alphanumeric<br>e.g., PROD-001-XL]
        MPN[MPN: 1-50 alphanumeric<br>e.g., ABC123-456]
    end
```

### Server-Side Validation
```mermaid
flowchart TD
    A[API Receives Request] --> B{ModelState.IsValid?}
    B -->|No| C[Return 400 Bad Request]

    B -->|Yes| D[ProductIdService.ValidateProductIdAsync]
    D --> E{Type Valid?}
    E -->|No| F[Return Validation Failed]

    E -->|Yes| G{Value Matches Pattern?}
    G -->|No| F

    G -->|Yes| H{Duplicate Check?}
    H -->|Duplicate| I[Return Duplicate Error]
    H -->|Unique| J[Proceed with Operation]
```

---

## Import/Export Flow

### CSV Import Flow
```mermaid
flowchart TD
    A[User Clicks Import Button] --> B[showImportModal Called]
    B --> C[Create Hidden File Input]
    C --> D[Set Accept = .csv]
    D --> E[Trigger Click]

    E --> F[User Selects File]
    F --> G[FileReader.readAsText]
    G --> H[parseAndPreviewImport Called]

    H --> I{Lines >= 2?}
    I -->|No| J[Show Error: Need Header + Data]

    I -->|Yes| K[Parse Header Row]
    K --> L[Loop Through Data Rows]

    L --> M[parseCSVLine - Handle Quotes]
    M --> N[Build Product ID Object]
    N --> O[Normalize Field Names]

    O --> P[buildImportPreview Called]

    subgraph "Preview Analysis"
        P --> P1[Initialize valuesInImport Map]
        P1 --> P2[Loop Through Parsed Items]

        P2 --> P3{Exists in Current Data?}
        P3 -->|Yes| P4[Compare Fields]
        P4 --> P5{Any Changes?}
        P5 -->|Yes| P6[Status = modified]
        P5 -->|No| P7[Status = unchanged]

        P3 -->|No| P8[Status = new]

        P6 & P7 & P8 --> P9[Validate Item]
        P9 --> P10{Type Valid?}
        P10 -->|No| P11[Add Error]

        P10 -->|Yes| P12{Value Valid?}
        P12 -->|No| P11

        P12 -->|Yes| P13{Duplicate in Import?}
        P13 -->|Yes| P11
        P13 -->|No| P14[Valid Row]

        P11 --> P15[Status = error]
        P14 & P15 --> P16{More Items?}
        P16 -->|Yes| P2
        P16 -->|No| P17[Show Preview Modal]
    end
```

### Import Preview Modal
```mermaid
flowchart TD
    A[showImportPreviewModal Called] --> B[Calculate Statistics]
    B --> C[Update Stat Displays]

    C --> D[Build Preview Table Rows]
    D --> E[Loop Through importPreviewData]

    E --> F{Row Status?}
    F -->|new| G[Green Badge - New]
    F -->|modified| H[Blue Badge - Modified]
    F -->|unchanged| I[Gray Badge - Unchanged]
    F -->|error| J[Red Badge - Error]

    G & H --> K[Checkbox Enabled + Checked]
    I --> L[Checkbox Enabled + Unchecked]
    J --> M[Checkbox Disabled + Unchecked]

    K & L & M --> N[Build Changes/Errors Column]
    N --> O[Append Row to Table]

    O --> P{More Rows?}
    P -->|Yes| E
    P -->|No| Q[Show Modal]
    Q --> R[updateApplyButtonState]
```

### Apply Import Flow
```mermaid
flowchart TD
    A[User Clicks Apply Import] --> B[applyImport Called]
    B --> C[Filter Selected + No Errors]

    C --> D{Any Rows to Import?}
    D -->|No| E[Show Error Toast]

    D -->|Yes| F{Certificate ID = 0?}

    subgraph "Create Page - Add to Memory"
        F -->|Yes| G[Loop Through Selected]
        G --> H[Generate Temp ID]
        H --> I[Add to productIds Array]
        I --> J{More Rows?}
        J -->|Yes| G
        J -->|No| K[Refresh Display]
        K --> L[Close Modal]
        L --> M[Show Success Toast]
    end

    subgraph "Edit Page - API Import"
        F -->|No| N[Loop Through Selected]
        N --> O{Action = update?}

        O -->|Yes| P[PUT /api/productids/{id}]
        O -->|No| Q[POST /api/productids]

        P & Q --> R{Response OK?}
        R -->|Yes| S[Increment Success Count]
        R -->|No| T[Increment Error Count]

        S & T --> U{More Rows?}
        U -->|Yes| N
        U -->|No| V[Close Modal]
        V --> W[loadProductIds - Refresh]
        W --> X[Show Result Toast]
    end
```

### CSV Export Flow
```mermaid
flowchart TD
    A[User Clicks Export Button] --> B[exportProductIds Called]
    B --> C[Show Loading]
    C --> D[GET /api/productids/certificate/{id}/export]

    D --> E{Response OK?}
    E -->|No| F[Show Error Toast]
    E -->|Yes| G[Get Blob from Response]

    G --> H[Create Object URL]
    H --> I[Create Hidden Anchor]
    I --> J[Set href and download attributes]
    J --> K[Trigger Click]
    K --> L[Revoke Object URL]
    L --> M[Remove Anchor]
    M --> N[Show Success Toast]

    F --> O[Hide Loading]
    N --> O
```

### Download Template Flow
```mermaid
flowchart TD
    A[User Clicks Template Button] --> B[downloadSample Called]
    B --> C[Build CSV Header Row]
    C --> D[Add Sample Data Rows]
    D --> E[Create Blob with CSV Content]
    E --> F[Create Object URL]
    F --> G[Create Hidden Anchor]
    G --> H[Set download = product-ids-sample.csv]
    H --> I[Trigger Click]
    I --> J[Revoke URL & Remove Anchor]
```

---

## Filtering & Search

### Filter Application Flow
```mermaid
flowchart TD
    A[User Changes Filter/Search] --> B[applyFilters Called]

    B --> C[Get Filter Values]
    C --> C1[typeFilter from #filterByType]
    C --> C2[statusFilter from #filterByStatus]
    C --> C3[partnerFilter from #filterByPartner]
    C --> C4[searchFilter from #searchProductIds]

    C1 & C2 & C3 & C4 --> D[Filter productIds Array]

    D --> E[Loop Through Each Product ID]
    E --> F{Type Match?}
    F -->|No Filter or Match| G{Status Match?}
    F -->|No Match| H[Exclude]

    G -->|No Filter or Match| I{Partner Match?}
    G -->|No Match| H

    I -->|No Filter or Match| J{Search Match?}
    I -->|No Match| H

    J --> K[Check productIdValue]
    K --> L[Check productName]
    L --> M[Check productSku]

    K & L & M -->|Any Match| N[Include in filteredProductIds]
    K & L & M -->|No Match| H

    H --> O{More Items?}
    N --> O
    O -->|Yes| E
    O -->|No| P[renderTable with filteredProductIds]
    P --> Q[updatePaginationInfo]
```

### Search Matching Logic
```mermaid
flowchart LR
    subgraph "Search Target Fields"
        A[productIdValue]
        B[productName]
        C[productSku]
    end

    subgraph "Match Logic"
        D[toLowerCase Both]
        E[includes Check]
        F[OR Logic - Any Match Passes]
    end

    A --> D --> E --> F
    B --> D --> E --> F
    C --> D --> E --> F
```

---

## Create vs Edit Page Behavior

### Behavioral Differences
```mermaid
flowchart LR
    subgraph "Create Page - Certificate ID = 0"
        A1[No API Calls for CRUD]
        A2[Temp IDs: Negative Timestamps]
        A3[Data Stored in Memory]
        A4[Submitted with Certificate Form]
        A5[Hidden Inputs Generated on Submit]
    end

    subgraph "Edit Page - Certificate ID > 0"
        B1[API Calls for All CRUD]
        B2[Real IDs from Database]
        B3[Data Persisted Immediately]
        B4[Changes Independent of Form]
        B5[Certificate Already Exists]
    end
```

### Form Submission Integration
```mermaid
flowchart TD
    A[Certificate Form Submit Triggered] --> B{productIdManager Exists?}
    B -->|No| C[Skip Product ID Processing]

    B -->|Yes| D{productIds Array Has Items?}
    D -->|No| C

    D -->|Yes| E[Remove Existing ProductIds Hidden Inputs]
    E --> F[Loop Through productIds]

    F --> G[Create Hidden Inputs for Each]
    G --> H[ProductIds index .CertificateId = 0]
    G --> I[ProductIds index .ProductIdType]
    G --> J[ProductIds index .ProductIdValue]
    G --> K[ProductIds index .Status]
    G --> L[ProductIds index .Partner]
    G --> M[ProductIds index .ProductName]
    G --> N[ProductIds index .ProductSku]
    G --> O[ProductIds index .Description]

    H & I & J & K & L & M & N & O --> P[Append to Form]
    P --> Q{More Items?}
    Q -->|Yes| F
    Q -->|No| R[Continue Form Submission]

    subgraph "Server Side Processing"
        R --> S[CertificateController.Create POST]
        S --> T[Model Binding Populates model.ProductIds]
        T --> U[Save Certificate First]
        U --> V[Loop Through ProductIds]
        V --> W[Set CertificateId = certificate.Id]
        W --> X[Add to Certificate.ProductIds Collection]
        X --> Y[Save to Database]
    end
```

---

## API Endpoints

### Endpoint Reference
```mermaid
flowchart LR
    subgraph "Product ID API - /api/productids"
        A[GET /certificate/{id}] --> A1[Get All for Certificate]
        B[GET /{id}] --> B1[Get Single by ID]
        C[POST /] --> C1[Create New]
        D[PUT /{id}] --> D1[Update Existing]
        E[DELETE /{id}] --> E1[Delete by ID]
        F[POST /certificate/{id}/import] --> F1[Bulk Import CSV]
        G[GET /certificate/{id}/export] --> G1[Export to CSV]
        H[GET /sample-csv] --> H1[Download Template]
        I[POST /validate] --> I1[Validate Without Save]
    end
```

### API Request/Response Flow
```mermaid
sequenceDiagram
    participant Browser
    participant Controller
    participant Service
    participant Repository
    participant Database

    Browser->>Controller: POST /api/productids
    Controller->>Controller: Check Authorization
    Controller->>Controller: Validate ModelState

    alt Invalid Model
        Controller-->>Browser: 400 Bad Request
    end

    Controller->>Service: ValidateProductIdAsync
    Service->>Service: Check Type & Format

    alt Validation Failed
        Controller-->>Browser: 400 Validation Failed
    end

    Controller->>Service: CreateProductIdAsync
    Service->>Repository: Add Entity
    Repository->>Database: INSERT
    Database-->>Repository: New ID
    Repository-->>Service: Saved Entity
    Service-->>Controller: Created Entity

    Controller-->>Browser: 201 Created + Entity
```

---

## Data Model

### Entity Relationship
```mermaid
erDiagram
    Certificate ||--o{ CertificateProductId : has
    User ||--o{ CertificateProductId : creates
    User ||--o{ CertificateProductId : updates

    CertificateProductId {
        int Id PK
        int CertificateId FK
        string ProductIdType
        string ProductIdValue
        string Status
        string Partner
        string ProductName
        string ProductSku
        string Description
        DateTime CreatedAt
        int CreatedBy FK
        DateTime UpdatedAt
        int UpdatedBy FK
    }

    Certificate {
        int Id PK
        string Number
        string Status
        int CompanyId FK
        int ProductId FK
    }

    User {
        int Id PK
        string Username
        string Email
    }
```

### Product ID Types Enum
```mermaid
flowchart TB
    subgraph "Supported Product ID Types"
        ASIN[ASIN - Amazon Standard ID]
        UPC[UPC - Universal Product Code]
        GTIN[GTIN - Global Trade Item Number]
        EAN[EAN - European Article Number]
        ISBN[ISBN - International Standard Book Number]
        SKU[SKU - Stock Keeping Unit]
        MPN[MPN - Manufacturer Part Number]
        OTHER[OTHER - Custom Identifier]
    end

    subgraph "Status Options"
        S1[Active]
        S2[Inactive]
        S3[Pending]
        S4[Expired]
    end

    subgraph "Partner Options"
        P1[Amazon]
        P2[Novi]
        P3[None]
        P4[Other]
    end
```

---

## Key Files Reference

| Component | File Path | Lines |
|-----------|-----------|-------|
| Partial View | `Views/Shared/_ProductIdManager.cshtml` | 340 |
| JavaScript Class | `wwwroot/js/product-id-manager.js` | 1177 |
| API Controller | `Controllers/Api/ProductIdApiController.cs` | 428 |
| Service Interface | `Services/IProductIdService.cs` | - |
| Service Implementation | `Services/ProductIdService.cs` | - |
| Model | `Models/CertificateProductId.cs` | - |
| CSS Styles | `wwwroot/css/product-id-manager.css` | - |

---

## Error Handling

### Error Types and Responses
```mermaid
flowchart TD
    subgraph "Client-Side Errors"
        A1[Validation Error] --> A2[Show Toast + Highlight Field]
        A3[Network Error] --> A4[Show Error Toast]
        A5[Parse Error] --> A6[Show Error Toast]
    end

    subgraph "Server-Side Errors"
        B1[400 Bad Request] --> B2[Show Validation Message]
        B3[401 Unauthorized] --> B4[Redirect to Login]
        B5[404 Not Found] --> B6[Show Not Found Message]
        B7[500 Server Error] --> B8[Show Generic Error]
    end

    subgraph "Import Errors"
        C1[Invalid CSV Format] --> C2[Show Format Error]
        C3[Missing Required Field] --> C4[Mark Row as Error]
        C5[Duplicate Value] --> C6[Mark Row as Error]
        C7[Invalid Type] --> C8[Mark Row as Error]
    end
```

---

## Performance Considerations

```mermaid
flowchart LR
    subgraph "Optimization Strategies"
        A[Event Delegation for Table Actions]
        B[Debounced Search Input]
        C[In-Memory Filtering - No API Calls]
        D[Batch Import via Preview Modal]
        E[Lazy Loading for Large Datasets]
    end

    subgraph "Memory Management"
        F[Arrays Cleared on Page Unload]
        G[Modal Elements Reused]
        H[Toast Elements Self-Remove]
    end
```
