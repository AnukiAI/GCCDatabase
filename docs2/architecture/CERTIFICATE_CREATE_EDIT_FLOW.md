# Certificate Create & Edit Page Flow

This document describes the flow of the Certificate Create and Edit pages in the GCC Web Application.

## Table of Contents
1. [Page Load Flow](#page-load-flow)
2. [Form Submission Flow](#form-submission-flow)
3. [Component Interactions](#component-interactions)
4. [File Upload Flow](#file-upload-flow)
5. [Tag Management Flow](#tag-management-flow)
6. [Product ID Management Flow](#product-id-management-flow)
7. [Custom Fields Flow](#custom-fields-flow)

---

## Page Load Flow

### Create Page Load
```mermaid
flowchart TD
    A[User navigates to /Certificate/Create] --> B{User Authorized?}
    B -->|No| C[Redirect to Login]
    B -->|Yes| D[CertificateController.Create GET]

    D --> E[Generate Next Certificate Number]
    E --> F[Set Default Values]
    F --> G[Load Dropdown Data]

    G --> G1[Load Companies]
    G --> G2[Load Products]
    G --> G3[Load Certification Types]
    G --> G4[Load Statuses by Role]
    G --> G5[Load External Tags]
    G --> G6[Load Internal Tags]

    G1 & G2 & G3 & G4 & G5 & G6 --> H[Create CertificateEditViewModel]
    H --> I[Return Create.cshtml View]

    I --> J[Browser Renders Page]
    J --> K[JavaScript Initializes]

    K --> K1[ImagePreview.init]
    K --> K2[PdfUpload.init]
    K --> K3[TagManager.init]
    K --> K4[FormValidation.init]
    K --> K5[UnsavedChanges.init]
    K --> K6[ProductIdManager.init]

    K1 & K2 & K3 & K4 & K5 & K6 --> L[Page Ready for User Input]
```

### Edit Page Load
```mermaid
flowchart TD
    A[User navigates to /Certificate/Edit/XX-XXXX] --> B{User Authorized?}
    B -->|No| C[Redirect to Login]
    B -->|Yes| D[CertificateController.Edit GET]

    D --> E{Certificate Exists?}
    E -->|No| F[Return 404 Not Found]
    E -->|Yes| G[Load Certificate from DB]

    G --> H[Map to CertificateEditViewModel]
    H --> I[Load Dropdown Data]

    I --> I1[Load Companies]
    I --> I2[Load Products]
    I --> I3[Load Certification Types]
    I --> I4[Load Statuses by Role]
    I --> I5[Load External Tags]
    I --> I6[Load Internal Tags]
    I --> I7[Load Existing Tags]
    I --> I8[Load Product IDs]
    I --> I9[Load Custom Field Values]

    I1 & I2 & I3 & I4 & I5 & I6 & I7 & I8 & I9 --> J[Return Edit.cshtml View]

    J --> K[Browser Renders Page]
    K --> L[JavaScript Initializes]

    L --> L1[Load Existing Tags UI]
    L --> L2[Initialize Subcategory Dropdown]
    L --> L3[Load Custom Fields for Type]
    L --> L4[Load Custom Field Values]
    L --> L5[Initialize Product ID Manager]

    L1 & L2 & L3 & L4 & L5 --> M[Page Ready for Editing]
```

---

## Form Submission Flow

### Create Form Submission
```mermaid
flowchart TD
    A[User Clicks Save/Save & Add Another] --> B[Form Submit Event Triggered]

    B --> C{HTML5 Validation Pass?}
    C -->|No| D[Show Validation Errors]
    D --> E[Highlight Invalid Fields]
    E --> F[Scroll to First Error]
    F --> G[Stop Submission]

    C -->|Yes| H{Custom Fields Loaded?}
    H -->|Yes| I{Custom Fields Valid?}
    I -->|No| J[Show Custom Field Errors]
    J --> G

    H -->|No| K[Skip Custom Field Validation]
    I -->|Yes| L[Collect Custom Field Values]
    K --> L

    L --> M[Add Custom Fields as Hidden Inputs]
    M --> N{Product IDs Exist?}

    N -->|Yes| O[Add Product IDs as Hidden Inputs]
    N -->|No| P[Skip Product IDs]

    O --> Q[Show Loading Spinner]
    P --> Q

    Q --> R[Submit Form to Server]
    R --> S[CertificateController.Create POST]

    S --> T{ModelState Valid?}
    T -->|No| U[Reload Form with Errors]

    T -->|Yes| V[Create Certificate Entity]
    V --> W{Image Upload?}
    W -->|Yes| X[Upload Image File]
    W -->|No| Y{PDF Upload?}

    X --> X1{Upload Success?}
    X1 -->|No| U
    X1 -->|Yes| Y

    Y -->|Yes| Z[Upload PDF File]
    Y -->|No| AA[Save Certificate to DB]

    Z --> Z1{Upload Success?}
    Z1 -->|No| U
    Z1 -->|Yes| Z2[Generate PDF Thumbnail]
    Z2 --> AA

    AA --> AB[Save Product IDs]
    AB --> AC[Save Tags]
    AC --> AD{Action = saveAndAddAnother?}

    AD -->|Yes| AE[Set Success Message]
    AE --> AF[Redirect to Create]

    AD -->|No| AG[Redirect to Details Page]
```

### Edit Form Submission
```mermaid
flowchart TD
    A[User Clicks Save Changes] --> B[Form Submit Event Triggered]

    B --> C{Custom Fields Valid?}
    C -->|No| D[Show Validation Errors]
    D --> E[Stop Submission]

    C -->|Yes| F[Collect Custom Field Values]
    F --> G[Add Custom Fields as Hidden Inputs]
    G --> H[Add Product IDs as Hidden Inputs]

    H --> I[Submit Form to Server]
    I --> J[CertificateController.Edit POST]

    J --> K{ID Match?}
    K -->|No| L[Return 404]

    K -->|Yes| M{ModelState Valid?}
    M -->|No| N[Reload Form with Errors]

    M -->|Yes| O[Load Existing Certificate]
    O --> P{Certificate Exists?}
    P -->|No| L

    P -->|Yes| Q[Update Certificate Properties]
    Q --> R{New Image Upload?}
    R -->|Yes| S[Upload New Image]
    R -->|No| T{New PDF Upload?}

    S --> S1{Success?}
    S1 -->|No| N
    S1 -->|Yes| T

    T -->|Yes| U[Upload New PDF]
    T -->|No| V[Update Product IDs]

    U --> U1{Success?}
    U1 -->|No| N
    U1 -->|Yes| U2[Generate New Thumbnail]
    U2 --> V

    V --> W[Update Tags]
    W --> X{Custom Fields Provided?}

    X -->|Yes| Y[Validate Custom Fields]
    Y --> Y1{Valid?}
    Y1 -->|No| N
    Y1 -->|Yes| Z[Save Custom Field Values]

    X -->|No| AA[Save Certificate to DB]
    Z --> AA

    AA --> AB[Set Success Message]
    AB --> AC[Redirect to Details Page]
```

---

## Component Interactions

### Overall Page Architecture
```mermaid
flowchart TB
    subgraph "Browser - Client Side"
        subgraph "JavaScript Modules"
            CC[certificate-create.js]
            PM[product-id-manager.js]
            CF[certificate-custom-fields.js]
            IC[certificate-inline-create.js]
        end

        subgraph "UI Components"
            FORM[Main Form]
            PDF[PDF Upload Zone]
            TAGS[Tag Selectors]
            PIDS[Product ID Manager]
            CUSTOM[Custom Fields Container]
            PREVIEW[Certificate Preview]
        end

        CC --> FORM
        CC --> PDF
        CC --> TAGS
        CC --> PREVIEW
        PM --> PIDS
        CF --> CUSTOM
    end

    subgraph "Server - ASP.NET Core"
        CTRL[CertificateController]
        SVC[CertificateService]
        FIELD[CertificateFieldService]
        FILE[FileUploadService]
        REPO[CertificateRepository]
        UOW[UnitOfWork]
        DB[(Database)]
    end

    FORM -->|POST| CTRL
    CTRL --> SVC
    CTRL --> FIELD
    CTRL --> FILE
    CTRL --> REPO
    SVC --> UOW
    FIELD --> UOW
    REPO --> UOW
    UOW --> DB
```

### JavaScript Module Dependencies
```mermaid
flowchart LR
    subgraph "Core Modules"
        ADMIN[AdminCore.js]
        JQUERY[jQuery]
    end

    subgraph "Certificate Modules"
        CC[certificate-create.js]
        PM[product-id-manager.js]
        CF[certificate-custom-fields.js]
        IC[certificate-inline-create.js]
    end

    JQUERY --> CC
    JQUERY --> PM
    JQUERY --> CF
    ADMIN --> CC
    ADMIN --> PM
    ADMIN --> CF

    CC -.->|Uses| PM
    CC -.->|Uses| CF
```

---

## File Upload Flow

### PDF Upload Process
```mermaid
flowchart TD
    A[User Interacts with Upload Zone] --> B{Drag & Drop or Click?}

    B -->|Click| C[Open File Dialog]
    B -->|Drag & Drop| D[Handle Drop Event]

    C --> E[User Selects PDF]
    D --> F[Extract File from DataTransfer]

    E --> G[File Selected]
    F --> G

    G --> H{File Type = PDF?}
    H -->|No| I[Show Error: Invalid File Type]
    I --> J[Clear Input]

    H -->|Yes| K{File Size <= 10MB?}
    K -->|No| L[Show Error: File Too Large]
    L --> J

    K -->|Yes| M[Show File Preview]
    M --> N[Display File Name & Size]
    N --> O[Add Remove Button]
    O --> P[Show Success Toast]
    P --> Q[File Ready for Form Submit]

    subgraph "On Form Submit"
        Q --> R[File Sent with Form]
        R --> S[Server: FileUploadService]
        S --> T[Save to wwwroot/uploads/certificates]
        T --> U[Generate Thumbnail from PDF]
        U --> V[Store Paths in Certificate Entity]
    end
```

---

## Tag Management Flow

### Adding Tags
```mermaid
flowchart TD
    A[User Selects Tag from Dropdown] --> B[TagManager.addTag Called]

    B --> C{Tag Value Empty?}
    C -->|Yes| D[Exit - No Action]

    C -->|No| E{Tag Already Added?}
    E -->|Yes| F[Show Warning: Tag Already Added]
    F --> G[Reset Dropdown Selection]

    E -->|No| H[Add Tag ID to Array]
    H --> I[Create Tag Element UI]
    I --> J[Append to Tags Container]
    J --> K[Update Hidden Input with JSON]
    K --> G

    subgraph "Tag Element Structure"
        L[Tag Badge]
        L --> L1[Tag Name Text]
        L --> L2[Remove Button X]
    end
```

### Removing Tags
```mermaid
flowchart TD
    A[User Clicks Remove X on Tag] --> B[TagManager.removeTag Called]

    B --> C[Find Tag in Array]
    C --> D{Tag Found?}

    D -->|No| E[Exit - No Action]
    D -->|Yes| F[Remove from Array]

    F --> G[Update Hidden Input JSON]
    G --> H[Find Tag Element in DOM]
    H --> I[Remove Element from Container]
```

---

## Product ID Management Flow

### Product ID Manager Lifecycle
```mermaid
flowchart TD
    A[Page Loads] --> B[ProductIdManager Initializes]

    B --> C{Edit Mode?}
    C -->|Yes| D[Load Existing Product IDs from data-product-ids]
    C -->|No| E[Initialize Empty Array]

    D --> F[Parse JSON Data]
    F --> G[Populate productIds Array]
    G --> H[Render Product ID Table]

    E --> H

    H --> I[Page Ready for User Input]

    subgraph "User Actions"
        J[Add Product ID]
        K[Edit Product ID]
        L[Delete Product ID]
        M[Import CSV]
        N[Export CSV]
    end

    I --> J & K & L & M & N
```

### Adding a Product ID
```mermaid
flowchart TD
    A[User Clicks Add Product ID] --> B[Open Add Modal]

    B --> C[User Fills Form]
    C --> D[User Clicks Save]

    D --> E{Validation Pass?}
    E -->|No| F[Show Validation Errors]
    F --> C

    E -->|Yes| G[Create Product ID Object]
    G --> H[Add to productIds Array]
    H --> I[Re-render Table Row]
    I --> J[Close Modal]
    J --> K[Show Success Toast]

    subgraph "On Form Submit"
        L[Form Submit Handler]
        L --> M[Loop Through productIds]
        M --> N[Create Hidden Inputs]
        N --> O[ProductIds index .PropertyName]
        O --> P[Append to Form]
    end
```

---

## Custom Fields Flow

### Loading Custom Fields
```mermaid
flowchart TD
    A[Certification Type Selected] --> B[certificateCustomFields.loadFieldsForType]

    B --> C[AJAX GET /Certificate/GetCustomFields]
    C --> D{Request Success?}

    D -->|No| E[Show Error Message]
    D -->|Yes| F[Parse Field Definitions]

    F --> G[Clear Container]
    G --> H[Loop Through Fields]

    H --> I{Field Type?}
    I -->|Text| J[Create Text Input]
    I -->|Number| K[Create Number Input]
    I -->|Date| L[Create Date Input]
    I -->|Select| M[Create Dropdown]
    I -->|Textarea| N[Create Textarea]
    I -->|Checkbox| O[Create Checkbox]

    J & K & L & M & N & O --> P[Add Validation Attributes]
    P --> Q[Append to Container]
    Q --> R{More Fields?}
    R -->|Yes| H
    R -->|No| S[Fields Rendered]

    subgraph "Edit Mode Only"
        S --> T[Load Field Values]
        T --> U[AJAX GET /Certificate/GetCustomFieldValues]
        U --> V[Populate Field Values]
    end
```

### Saving Custom Fields
```mermaid
flowchart TD
    A[Form Submit Triggered] --> B[Validate Custom Fields]

    B --> C{All Required Fields Filled?}
    C -->|No| D[Return Validation Errors]

    C -->|Yes| E{Field Values Valid?}
    E -->|No| D

    E -->|Yes| F[Collect Field Values]
    F --> G[Create fieldValues Object]

    G --> H[Remove Old Hidden Inputs]
    H --> I[Loop Through fieldValues]
    I --> J[Create Hidden Input]
    J --> K[Name = CustomFieldValues fieldId]
    K --> L[Append to Form]

    L --> M{More Fields?}
    M -->|Yes| I
    M -->|No| N[Continue Form Submit]

    subgraph "Server Side"
        N --> O[Controller Receives CustomFieldValues]
        O --> P[CertificateFieldService.ValidateFieldValuesAsync]
        P --> Q{Valid?}
        Q -->|No| R[Return Errors]
        Q -->|Yes| S[CertificateFieldService.SaveFieldValuesAsync]
        S --> T[Save to CertificateFieldValues Table]
    end
```

---

## Data Model Relationships

```mermaid
erDiagram
    Certificate ||--o{ CertificateProductId : has
    Certificate ||--o{ CertificateFieldValue : has
    Certificate }o--|| Company : belongs_to
    Certificate }o--|| Product : belongs_to
    Certificate }o--|| CertificationType : belongs_to
    Certificate }o--o| CertificationType : subcategory
    Certificate }o--o{ Tag : has

    CertificateField ||--o{ CertificateFieldValue : defines
    CertificationType ||--o{ CertificateField : has

    Certificate {
        int Id PK
        string Number UK
        string Status
        string Partner
        string CertificationResults
        datetime EffectiveDate
        datetime ExpirationDate
        string Location
        string Country
        string CertificateFile
        string CertificateImagePath
        int CompanyId FK
        int ProductId FK
        int CertificationTypeId FK
        int CertificationTypeSubcategoryId FK
    }

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
    }

    CertificateFieldValue {
        int Id PK
        int CertificateId FK
        int CertificateFieldId FK
        string Value
    }

    Tag {
        int Id PK
        string Name
        string TagType
        bool IsActive
    }
```

---

## Status Flow

### Certificate Statuses by Role
```mermaid
flowchart LR
    subgraph "Admin Role"
        A1[Draft]
        A2[Active]
        A3[Expired]
        A4[Revoked]
        A5[Suspended]
        A6[Pending Review]
        A7[Archived]
    end

    subgraph "Analyst Role"
        B1[Draft]
        B2[Active]
        B3[Expired]
        B4[Pending Review]
    end

    subgraph "External Role"
        C1[View Only - No Status Changes]
    end
```

---

## Error Handling Flow

```mermaid
flowchart TD
    A[Error Occurs] --> B{Error Type?}

    B -->|Validation Error| C[Client-Side Validation]
    C --> D[Highlight Invalid Fields]
    D --> E[Show Toast Notification]
    E --> F[Scroll to First Error]

    B -->|Server Error| G[ModelState Error]
    G --> H[Return View with Errors]
    H --> I[Display Validation Summary]
    I --> J[Highlight Fields with Errors]
    J --> K[Show Toast with Error Count]

    B -->|File Upload Error| L[FileUploadService Returns Error]
    L --> M[Add to ModelState]
    M --> H

    B -->|Database Error| N[Exception Caught]
    N --> O[Log Error]
    O --> P[Add Generic Error to ModelState]
    P --> H

    B -->|Authorization Error| Q[Redirect to Login/Access Denied]
```

---

## Key Files Reference

| Component | File Path |
|-----------|-----------|
| Create Controller Action | `Controllers/CertificateController.cs:308-556` |
| Edit Controller Action | `Controllers/CertificateController.cs:580-849` |
| Create View | `Views/Certificate/Create.cshtml` |
| Edit View | `Views/Certificate/Edit.cshtml` |
| Main JavaScript | `wwwroot/js/certificate-create.js` |
| Product ID Manager | `wwwroot/js/product-id-manager.js` |
| Custom Fields | `wwwroot/js/certificate-custom-fields.js` |
| Inline Create | `wwwroot/js/certificate-inline-create.js` |
| CSS Styles | `wwwroot/css/certificate-create.css` |
| Product ID Styles | `wwwroot/css/product-id-manager.css` |

---

## Configuration Points

### ViewBag Data Loaded
- `ViewBag.Companies` - All companies for dropdown
- `ViewBag.Products` - All products for dropdown
- `ViewBag.ParentCertificationTypes` - Parent cert types for primary dropdown
- `ViewBag.AllCertificationTypes` - All cert types for subcategory filtering
- `ViewBag.Statuses` - Role-based status options
- `ViewBag.ExternalTags` - External tags for dropdown
- `ViewBag.InternalTags` - Internal tags for dropdown
- `ViewBag.ExistingExternalTags` - (Edit only) Currently assigned external tags
- `ViewBag.ExistingInternalTags` - (Edit only) Currently assigned internal tags

### Hidden Form Fields
- `Id` - Certificate ID (Edit only)
- `ExternalTags` - JSON array of external tag IDs
- `InternalTags` - JSON array of internal tag IDs
- `ProductIds[n].PropertyName` - Product ID collection
- `CustomFieldValues[fieldId]` - Custom field values
