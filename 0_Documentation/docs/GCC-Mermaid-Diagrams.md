# GCC Web Application - Mermaid Diagrams

This document contains comprehensive Mermaid diagrams for the GCC (Global Certificate Compliance) Web Application system.

## Table of Contents

1. [Flowcharts](#flowcharts)
2. [Sequence Diagrams](#sequence-diagrams)
3. [Class Diagrams](#class-diagrams)
4. [State Diagrams](#state-diagrams)
5. [Entity Relationship Diagrams](#entity-relationship-diagrams)
6. [Gantt Charts](#gantt-charts)
7. [Pie Charts](#pie-charts)
8. [Git Graphs](#git-graphs)
9. [Mind Maps](#mind-maps)
10. [Timeline](#timeline)
11. [Requirement Diagrams](#requirement-diagrams)
12. [Quadrant Charts](#quadrant-charts)
13. [User Journey Diagrams](#user-journey-diagrams)

---

## Flowcharts

### Certificate Management Process Flow
```mermaid
flowchart TD
    A[User Login] --> B{Authentication Success?}
    B -->|No| C[Return to Login]
    B -->|Yes| D[Dashboard Access]
    D --> E[Certificate Management]
    E --> F{Action Type}
    F -->|Create| G[Create Certificate Form]
    F -->|View| H[Certificate List/Details]
    F -->|Update| I[Edit Certificate Form]
    F -->|Delete| J[Confirm Deletion]
    
    G --> K[Validate Input]
    K -->|Invalid| L[Show Validation Errors]
    L --> G
    K -->|Valid| M[Save to Database]
    M --> N[Success Message]
    
    I --> O[Load Existing Data]
    O --> P[Update Form]
    P --> Q[Validate Changes]
    Q -->|Invalid| R[Show Errors]
    R --> P
    Q -->|Valid| S[Update Database]
    S --> T[Success Message]
    
    J -->|Confirmed| U[Delete from Database]
    U --> V[Success Message]
    J -->|Cancelled| H
    
    H --> W[Filter & Search Options]
    W --> X[Paginated Results]
    X --> Y[Certificate Details View]
    Y --> Z[Download Certificate PDF]
```

### API Request Processing Flow
```mermaid
flowchart TD
    A[API Request] --> B{Authentication Check}
    B -->|Fail| C[401 Unauthorized]
    B -->|Pass| D[API Key Validation]
    D -->|Invalid| E[403 Forbidden]
    D -->|Valid| F{Rate Limiting Check}
    F -->|Exceeded| G[429 Too Many Requests]
    F -->|Within Limits| H[Route to Controller]
    
    H --> I{HTTP Method}
    I -->|GET| J[Retrieve Data]
    I -->|POST| K[Create Resource]
    I -->|PUT| L[Update Resource]
    I -->|DELETE| M[Delete Resource]
    
    J --> N[Database Query]
    K --> O[Validate Input]
    L --> P[Validate Input & Check Exists]
    M --> Q[Check Resource Exists]
    
    N --> R{Data Found?}
    R -->|No| S[404 Not Found]
    R -->|Yes| T[200 OK with Data]
    
    O -->|Invalid| U[400 Bad Request]
    O -->|Valid| V[Save to Database]
    V -->|Success| W[201 Created]
    V -->|Error| X[500 Internal Server Error]
    
    P -->|Invalid| Y[400 Bad Request]
    P -->|Valid| Z[Update Database]
    Z -->|Success| AA[200 OK]
    Z -->|Error| BB[500 Internal Server Error]
    
    Q -->|Not Found| CC[404 Not Found]
    Q -->|Found| DD[Delete from Database]
    DD -->|Success| EE[204 No Content]
    DD -->|Error| FF[500 Internal Server Error]
```

### ETL Data Processing Flow
```mermaid
flowchart TD
    A[ETL Job Start] --> B[Load Configuration]
    B --> C[Data Extraction Phase]
    C --> D{Data Source Type}
    D -->|File| E[Read File Data]
    D -->|Database| F[Execute Query]
    D -->|API| G[HTTP Request]
    
    E --> H[Parse File Content]
    F --> I[Process Result Set]
    G --> J[Process API Response]
    
    H --> K[Data Transformation Phase]
    I --> K
    J --> K
    
    K --> L[Apply Business Rules]
    L --> M[Data Validation]
    M -->|Invalid| N[Log Data Quality Issues]
    M -->|Valid| O[Data Loading Phase]
    
    N --> P[Generate Alert]
    P --> Q{Continue Processing?}
    Q -->|No| R[Job Failed]
    Q -->|Yes| S[Skip Invalid Records]
    S --> O
    
    O --> T[Batch Insert/Update]
    T --> U{Database Operation Success?}
    U -->|No| V[Log Error & Retry]
    U -->|Yes| W[Update Aggregate Data]
    W --> X[Job Completed Successfully]
    
    V --> Y{Retry Limit Reached?}
    Y -->|No| T
    Y -->|Yes| Z[Job Failed with Errors]
    
    X --> AA[Send Success Notification]
    R --> BB[Send Failure Notification]
    Z --> BB
```

---

## Sequence Diagrams

### Certificate API Creation Sequence
```mermaid
sequenceDiagram
    participant Client
    participant ApiKeyAuth as API Key Auth
    participant Controller as Certificates Controller
    participant Context as DB Context
    participant Database
    participant Logger

    Client->>ApiKeyAuth: POST /api/v1/certificates
    ApiKeyAuth->>ApiKeyAuth: Validate API Key
    alt Invalid API Key
        ApiKeyAuth->>Client: 403 Forbidden
    else Valid API Key
        ApiKeyAuth->>Controller: Forward Request
        Controller->>Controller: Validate Model State
        alt Invalid Model
            Controller->>Client: 400 Bad Request
        else Valid Model
            Controller->>Context: Check Duplicate Certificate Number
            Context->>Database: Query Certificates
            Database->>Context: Result Set
            Context->>Controller: Duplicate Check Result
            alt Duplicate Found
                Controller->>Logger: Log Warning
                Controller->>Client: 400 Bad Request
            else No Duplicate
                Controller->>Context: Add Certificate
                Context->>Database: INSERT Certificate
                Database->>Context: Success/Failure
                alt Database Error
                    Context->>Controller: Exception
                    Controller->>Logger: Log Error
                    Controller->>Client: 500 Internal Server Error
                else Success
                    Context->>Controller: Certificate Created
                    Controller->>Context: Load Full Certificate Data
                    Context->>Database: Query with Includes
                    Database->>Context: Complete Certificate Data
                    Context->>Controller: Full Certificate Object
                    Controller->>Logger: Log Success
                    Controller->>Client: 201 Created
                end
            end
        end
    end
```

### User Authentication Sequence
```mermaid
sequenceDiagram
    participant User
    participant Browser
    participant AuthController as Auth Controller
    participant TwoFactorService as 2FA Service
    participant EmailService as Email Service
    participant Database
    participant Session

    User->>Browser: Enter Credentials
    Browser->>AuthController: POST /Account/Login
    AuthController->>Database: Validate Credentials
    Database->>AuthController: User Data
    
    alt Invalid Credentials
        AuthController->>Database: Log Failed Attempt
        AuthController->>Browser: Login Failed
        Browser->>User: Error Message
    else Valid Credentials
        AuthController->>AuthController: Check 2FA Required
        alt 2FA Enabled
            AuthController->>TwoFactorService: Generate 2FA Code
            TwoFactorService->>EmailService: Send 2FA Code
            EmailService->>User: Email with Code
            AuthController->>Browser: Request 2FA Code
            Browser->>User: 2FA Input Form
            User->>Browser: Enter 2FA Code
            Browser->>AuthController: POST /Account/Verify2FA
            AuthController->>TwoFactorService: Validate Code
            alt Invalid 2FA Code
                TwoFactorService->>AuthController: Validation Failed
                AuthController->>Browser: 2FA Failed
                Browser->>User: Error Message
            else Valid 2FA Code
                TwoFactorService->>AuthController: Validation Success
                AuthController->>Session: Create Session
                AuthController->>Database: Update Last Login
                AuthController->>Browser: Redirect to Dashboard
                Browser->>User: Dashboard Page
            end
        else 2FA Disabled
            AuthController->>Session: Create Session
            AuthController->>Database: Update Last Login
            AuthController->>Browser: Redirect to Dashboard
            Browser->>User: Dashboard Page
        end
    end
```

### ETL Data Processing Sequence
```mermaid
sequenceDiagram
    participant Scheduler
    participant ETLService as ETL Service
    participant DataExtractor
    participant DataTransformer
    participant DataLoader as Data Loader
    participant Database
    participant Monitor as ETL Monitor
    participant AlertService as Alert Service

    Scheduler->>ETLService: Trigger ETL Job
    ETLService->>Monitor: Job Started
    ETLService->>DataExtractor: Extract Data
    
    DataExtractor->>DataExtractor: Connect to Data Source
    DataExtractor->>DataExtractor: Retrieve Raw Data
    DataExtractor->>ETLService: Raw Data
    
    ETLService->>DataTransformer: Transform Data
    DataTransformer->>DataTransformer: Apply Business Rules
    DataTransformer->>DataTransformer: Validate Data Quality
    
    alt Data Quality Issues
        DataTransformer->>Monitor: Log Quality Issues
        DataTransformer->>AlertService: Send Quality Alert
        AlertService->>AlertService: Generate Alert
    end
    
    DataTransformer->>ETLService: Transformed Data
    ETLService->>DataLoader: Load Data
    
    DataLoader->>Database: Batch Insert/Update
    Database->>DataLoader: Operation Result
    
    alt Database Error
        DataLoader->>Monitor: Log Error
        DataLoader->>AlertService: Send Error Alert
        AlertService->>AlertService: Generate Error Alert
        DataLoader->>ETLService: Load Failed
        ETLService->>Monitor: Job Failed
    else Success
        DataLoader->>ETLService: Load Success
        ETLService->>Database: Update Aggregate Data
        ETLService->>Monitor: Job Completed
        Monitor->>Monitor: Update Job Statistics
    end
```

---

## Class Diagrams

### Core Domain Models
```mermaid
classDiagram
    class Certificate {
        +int Id
        +string Number
        +string Status
        +DateTime EffectiveDate
        +DateTime ExpirationDate
        +string Location
        +string Country
        +string Asin
        +string Upc
        +string Gtin
        +string Ean
        +string CertificateFile
        +int CompanyId
        +int ProductId
        +int CertificationTypeId
        +int? CreatedBy
        +int? UpdatedBy
        +DateTime CreatedAt
        +DateTime UpdatedAt
        +Company Company
        +Product Product
        +CertificationType CertificationType
        +User CreatedByUser
        +User UpdatedByUser
        +ICollection~Tag~ Tags
        +ICollection~CustomFieldValue~ CustomFieldValues
        +ICollection~CertificateDocument~ Documents
    }

    class Company {
        +int Id
        +string Name
        +string ContactInfo
        +string Email
        +string Website
        +string Location
        +string Country
        +bool IsActive
        +bool IsDeleted
        +DateTime CreatedAt
        +DateTime UpdatedAt
        +ICollection~Certificate~ Certificates
        +ICollection~Product~ Products
        +ICollection~User~ Users
    }

    class Product {
        +int Id
        +string Name
        +string Description
        +string Model
        +string Sku
        +string Upc
        +string Asin
        +int CategoryId
        +int? ProductTypeId
        +int? ProductSubcategoryId
        +int? CompanyId
        +bool IsActive
        +bool IsDeleted
        +DateTime CreatedAt
        +DateTime UpdatedAt
        +ProductCategory Category
        +ProductType ProductType
        +ProductSubcategory ProductSubcategory
        +Company Company
        +ICollection~Certificate~ Certificates
        +ICollection~Tag~ Tags
        +ICollection~ProductIdentifier~ ProductIdentifiers
    }

    class User {
        +int Id
        +string Username
        +string Email
        +string Name
        +string FullName
        +DateTime? LastLoginAt
        +bool IsDeleted
        +DateTime? DeletedAt
        +string PasswordHash
        +string Status
        +bool IsActive
        +bool IsStaff
        +bool IsSuperuser
        +bool RequirePasswordReset
        +DateTime? LastLogin
        +DateTime? PasswordChangedAt
        +int FailedLoginAttempts
        +DateTime? LockedUntil
        +string TwoFactorSecret
        +bool TwoFactorEnabled
        +string TwoFactorMethod
        +string PhoneNumber
        +bool IsPhoneVerified
        +bool IsEmailVerified
        +string RecoveryCodes
        +DateTime CreatedAt
        +DateTime UpdatedAt
        +ICollection~Certificate~ CreatedCertificates
        +ICollection~Certificate~ UpdatedCertificates
        +ICollection~Role~ Roles
        +ICollection~Company~ Companies
    }

    class CertificationType {
        +int Id
        +string Name
        +string Description
        +bool IsActive
        +DateTime CreatedAt
        +DateTime UpdatedAt
        +ICollection~Certificate~ Certificates
        +ICollection~CustomField~ CustomFields
    }

    class ProductCategory {
        +int Id
        +string Name
        +string Description
        +bool IsActive
        +DateTime CreatedAt
        +DateTime UpdatedAt
        +ICollection~Product~ Products
        +ICollection~ProductType~ ProductTypes
    }

    class Tag {
        +int Id
        +string Name
        +string Color
        +DateTime CreatedAt
        +DateTime UpdatedAt
        +ICollection~Certificate~ Certificates
        +ICollection~Product~ Products
    }

    Certificate ||--o{ Company : "belongs to"
    Certificate ||--o{ Product : "certifies"
    Certificate ||--o{ CertificationType : "has type"
    Certificate ||--o{ User : "created by"
    Certificate ||--o{ User : "updated by"
    Certificate }|--|| Tag : "tagged with"
    
    Product ||--o{ Company : "manufactured by"
    Product ||--o{ ProductCategory : "belongs to category"
    Product }|--|| Tag : "tagged with"
    
    User }|--|| Company : "works for"
    User }|--|| Role : "has roles"
```

### Service Layer Architecture
```mermaid
classDiagram
    class ICertificateService {
        <<interface>>
        +Task~IEnumerable~Certificate~~ GetAllAsync()
        +Task~Certificate~ GetByIdAsync(int id)
        +Task~Certificate~ CreateAsync(Certificate certificate)
        +Task~Certificate~ UpdateAsync(Certificate certificate)
        +Task DeleteAsync(int id)
        +Task~IEnumerable~Certificate~~ SearchAsync(string term)
    }

    class CertificateService {
        -IUnitOfWork _unitOfWork
        -ILogger _logger
        +Task~IEnumerable~Certificate~~ GetAllAsync()
        +Task~Certificate~ GetByIdAsync(int id)
        +Task~Certificate~ CreateAsync(Certificate certificate)
        +Task~Certificate~ UpdateAsync(Certificate certificate)
        +Task DeleteAsync(int id)
        +Task~IEnumerable~Certificate~~ SearchAsync(string term)
        -Task ValidateCertificateAsync(Certificate certificate)
    }

    class IUnitOfWork {
        <<interface>>
        +ICertificateRepository Certificates
        +ICompanyRepository Companies
        +IProductRepository Products
        +IUserRepository Users
        +Task SaveChangesAsync()
        +void Dispose()
    }

    class UnitOfWork {
        -ApplicationDbContext _context
        +ICertificateRepository Certificates
        +ICompanyRepository Companies
        +IProductRepository Products
        +IUserRepository Users
        +Task SaveChangesAsync()
        +void Dispose()
    }

    class IRepository~T~ {
        <<interface>>
        +Task~IEnumerable~T~~ GetAllAsync()
        +Task~T~ GetByIdAsync(int id)
        +Task~T~ AddAsync(T entity)
        +Task UpdateAsync(T entity)
        +Task DeleteAsync(T entity)
        +Task~bool~ ExistsAsync(int id)
    }

    class Repository~T~ {
        -ApplicationDbContext _context
        -DbSet~T~ _dbSet
        +Task~IEnumerable~T~~ GetAllAsync()
        +Task~T~ GetByIdAsync(int id)
        +Task~T~ AddAsync(T entity)
        +Task UpdateAsync(T entity)
        +Task DeleteAsync(T entity)
        +Task~bool~ ExistsAsync(int id)
    }

    class CertificateRepository {
        +Task~IEnumerable~Certificate~~ GetExpiringAsync(int days)
        +Task~IEnumerable~Certificate~~ GetByCompanyAsync(int companyId)
        +Task~IEnumerable~Certificate~~ GetByProductAsync(int productId)
        +Task~Certificate~ GetByNumberAsync(string number)
    }

    ICertificateService <|.. CertificateService
    CertificateService --> IUnitOfWork
    IUnitOfWork <|.. UnitOfWork
    UnitOfWork --> Repository~T~
    IRepository~T~ <|.. Repository~T~
    Repository~T~ <|-- CertificateRepository
```

---

## State Diagrams

### User Authentication States
```mermaid
stateDiagram-v2
    [*] --> Anonymous
    Anonymous --> LoginAttempt : Enter Credentials
    LoginAttempt --> InvalidCredentials : Invalid Username/Password
    LoginAttempt --> ValidCredentials : Valid Username/Password
    
    InvalidCredentials --> FailedAttempts : Increment Counter
    FailedAttempts --> Anonymous : Retry Login
    FailedAttempts --> AccountLocked : Max Attempts Reached
    AccountLocked --> [*] : Account Locked
    
    ValidCredentials --> TwoFactorRequired : 2FA Enabled
    ValidCredentials --> Authenticated : 2FA Disabled
    
    TwoFactorRequired --> TwoFactorPrompt : Request 2FA Code
    TwoFactorPrompt --> InvalidTwoFactor : Invalid Code
    TwoFactorPrompt --> Authenticated : Valid Code
    InvalidTwoFactor --> TwoFactorPrompt : Retry
    
    Authenticated --> SessionActive : Create Session
    SessionActive --> SessionExpired : Timeout/Logout
    SessionActive --> PasswordExpired : Check Password Age
    
    PasswordExpired --> ForcePasswordChange : Redirect
    ForcePasswordChange --> Authenticated : Password Updated
    
    SessionExpired --> Anonymous : Session Ended
    
    state ValidCredentials {
        [*] --> CheckingPassword
        CheckingPassword --> PasswordValid : Hash Match
        CheckingPassword --> PasswordInvalid : Hash Mismatch
        PasswordValid --> CheckingAccount
        CheckingAccount --> AccountActive : Account OK
        CheckingAccount --> AccountInactive : Account Disabled
        CheckingAccount --> AccountLocked : Too Many Failures
    }
```

### Certificate Lifecycle States
```mermaid
stateDiagram-v2
    [*] --> Draft
    Draft --> UnderReview : Submit for Review
    UnderReview --> Draft : Reject/Return for Changes
    UnderReview --> Approved : Approve
    
    Approved --> Active : Publish
    Active --> Suspended : Temporary Suspension
    Suspended --> Active : Reinstate
    
    Active --> Expired : Expiration Date Reached
    Active --> Revoked : Manual Revocation
    
    Expired --> Renewed : Renewal Process
    Expired --> Archived : Archive Expired
    
    Renewed --> Active : New Certificate Active
    Revoked --> Archived : Archive Revoked
    
    Archived --> [*]
    
    state Active {
        [*] --> Valid
        Valid --> ExpiringSoon : 30 Days Before Expiry
        ExpiringSoon --> Valid : Renewed
        ExpiringSoon --> Expired : Not Renewed
    }
    
    state UnderReview {
        [*] --> PendingValidation
        PendingValidation --> ValidationPassed : Documents OK
        PendingValidation --> ValidationFailed : Issues Found
        ValidationFailed --> PendingValidation : Resubmit
        ValidationPassed --> PendingApproval
        PendingApproval --> Approved : Manager Approval
        PendingApproval --> Rejected : Manager Rejection
    }
```

### ETL Job States
```mermaid
stateDiagram-v2
    [*] --> Scheduled
    Scheduled --> Running : Job Triggered
    Running --> Extracting : Start Data Extraction
    
    Extracting --> ExtractionFailed : Data Source Error
    Extracting --> Transforming : Data Extracted Successfully
    
    ExtractionFailed --> Failed : Mark Job Failed
    
    Transforming --> TransformationFailed : Validation Error
    Transforming --> Loading : Data Transformed Successfully
    
    TransformationFailed --> PartialFailure : Some Records Failed
    
    Loading --> LoadingFailed : Database Error
    Loading --> Completed : Data Loaded Successfully
    
    LoadingFailed --> Failed : Mark Job Failed
    PartialFailure --> CompletedWithWarnings : Continue with Valid Records
    
    Completed --> [*] : Job Success
    CompletedWithWarnings --> [*] : Job Success with Issues
    Failed --> [*] : Job Failed
    
    state Running {
        [*] --> Initializing
        Initializing --> ReadingConfig : Load Configuration
        ReadingConfig --> ValidatingConfig : Validate Settings
        ValidatingConfig --> ConfigValid : Config OK
        ValidatingConfig --> ConfigInvalid : Config Error
        ConfigInvalid --> Failed
        ConfigValid --> Ready : Ready to Process
    }
    
    state Transforming {
        [*] --> ApplyingRules
        ApplyingRules --> ValidatingData
        ValidatingData --> DataValid : All Valid
        ValidatingData --> DataIssues : Some Invalid
        DataIssues --> LoggingIssues
        LoggingIssues --> GeneratingAlerts
        GeneratingAlerts --> PartiallyTransformed
    }
```

---

## Entity Relationship Diagrams

### Core Database Schema
```mermaid
erDiagram
    CERTIFICATES {
        int Id PK
        string Number UK "Certificate Number"
        string Status "active/expired/pending"
        datetime EffectiveDate
        datetime ExpirationDate
        string Location
        string Country
        string Asin
        string Upc
        string Gtin
        string Ean
        string CertificateFile
        int CompanyId FK
        int ProductId FK
        int CertificationTypeId FK
        int CreatedBy FK
        int UpdatedBy FK
        datetime CreatedAt
        datetime UpdatedAt
    }
    
    COMPANIES {
        int Id PK
        string Name UK
        string ContactInfo
        string Email
        string Website
        string Location
        string Country
        bool IsActive
        bool IsDeleted
        datetime CreatedAt
        datetime UpdatedAt
    }
    
    PRODUCTS {
        int Id PK
        string Name
        text Description
        string Model
        string Sku
        string Upc
        string Asin
        int CategoryId FK
        int ProductTypeId FK
        int ProductSubcategoryId FK
        int CompanyId FK
        bool IsActive
        bool IsDeleted
        datetime CreatedAt
        datetime UpdatedAt
    }
    
    USERS {
        int Id PK
        string Username UK
        string Email UK
        string Name
        string FullName
        datetime LastLoginAt
        bool IsDeleted
        datetime DeletedAt
        string PasswordHash
        string Status
        bool IsActive
        bool IsStaff
        bool IsSuperuser
        bool TwoFactorEnabled
        string TwoFactorMethod
        string PhoneNumber
        datetime CreatedAt
        datetime UpdatedAt
    }
    
    CERTIFICATION_TYPES {
        int Id PK
        string Name UK
        text Description
        bool IsActive
        datetime CreatedAt
        datetime UpdatedAt
    }
    
    PRODUCT_CATEGORIES {
        int Id PK
        string Name UK
        text Description
        bool IsActive
        datetime CreatedAt
        datetime UpdatedAt
    }
    
    PRODUCT_TYPES {
        int Id PK
        string Name
        text Description
        int CategoryId FK
        int ParentId FK
        bool IsActive
        datetime CreatedAt
        datetime UpdatedAt
    }
    
    PRODUCT_SUBCATEGORIES {
        int Id PK
        string Name
        text Description
        int ProductTypeId FK
        bool IsActive
        datetime CreatedAt
        datetime UpdatedAt
    }
    
    TAGS {
        int Id PK
        string Name UK
        string Color
        datetime CreatedAt
        datetime UpdatedAt
    }
    
    CUSTOM_FIELDS {
        int Id PK
        string Name
        string FieldType
        text DefaultValue
        bool IsRequired
        int CertificateTypeId FK
        datetime CreatedAt
        datetime UpdatedAt
    }
    
    CUSTOM_FIELD_VALUES {
        int Id PK
        int CertificateId FK
        int FieldId FK
        text Value
        datetime CreatedAt
        datetime UpdatedAt
    }
    
    CERTIFICATE_DOCUMENTS {
        int Id PK
        int CertificateId FK
        string FileName
        string FilePath
        string FileHash
        long FileSize
        string ContentType
        int UploadedBy FK
        datetime UploadedAt
    }
    
    ROLES {
        int Id PK
        string Name UK
        text Description
        datetime CreatedAt
        datetime UpdatedAt
    }
    
    PERMISSIONS {
        int Id PK
        string Name
        string Codename UK
        text Description
        datetime CreatedAt
        datetime UpdatedAt
    }
    
    API_KEYS {
        int Id PK
        string KeyName
        string KeyHash
        string KeyPrefix
        int UserId FK
        bool IsActive
        datetime ExpiresAt
        datetime CreatedAt
        datetime UpdatedAt
    }

    %% Relationships
    CERTIFICATES ||--o{ COMPANIES : "belongs_to"
    CERTIFICATES ||--o{ PRODUCTS : "certifies"
    CERTIFICATES ||--o{ CERTIFICATION_TYPES : "has_type"
    CERTIFICATES ||--o{ USERS : "created_by"
    CERTIFICATES ||--o{ USERS : "updated_by"
    CERTIFICATES ||--o{ CUSTOM_FIELD_VALUES : "has_custom_fields"
    CERTIFICATES ||--o{ CERTIFICATE_DOCUMENTS : "has_documents"
    
    PRODUCTS ||--o{ COMPANIES : "manufactured_by"
    PRODUCTS ||--o{ PRODUCT_CATEGORIES : "belongs_to_category"
    PRODUCTS ||--o{ PRODUCT_TYPES : "has_type"
    PRODUCTS ||--o{ PRODUCT_SUBCATEGORIES : "has_subcategory"
    
    PRODUCT_TYPES ||--o{ PRODUCT_CATEGORIES : "belongs_to_category"
    PRODUCT_TYPES ||--o{ PRODUCT_TYPES : "parent_type"
    PRODUCT_SUBCATEGORIES ||--o{ PRODUCT_TYPES : "belongs_to_type"
    
    CUSTOM_FIELDS ||--o{ CERTIFICATION_TYPES : "belongs_to_type"
    CUSTOM_FIELD_VALUES ||--o{ CERTIFICATES : "belongs_to_certificate"
    CUSTOM_FIELD_VALUES ||--o{ CUSTOM_FIELDS : "field_definition"
    
    CERTIFICATE_DOCUMENTS ||--o{ CERTIFICATES : "belongs_to_certificate"
    CERTIFICATE_DOCUMENTS ||--o{ USERS : "uploaded_by"
    
    API_KEYS ||--o{ USERS : "belongs_to_user"
    
    %% Many-to-Many Relationships
    CERTIFICATES }|--|| TAGS : "certificate_tags"
    PRODUCTS }|--|| TAGS : "product_tags"
    USERS }|--|| ROLES : "user_roles"
    USERS }|--|| COMPANIES : "user_companies"
    ROLES }|--|| PERMISSIONS : "role_permissions"
```

### ETL System Schema
```mermaid
erDiagram
    ETL_CONFIGURATIONS {
        int Id PK
        string Name
        text Description
        string SourceType
        string ConnectionString
        text DataSources
        text Transformations
        text Destinations
        text Settings
        string CronExpression
        bool IsActive
        datetime CreatedAt
        datetime UpdatedAt
    }
    
    ETL_LOG_ENTRIES {
        int Id PK
        string JobId
        string Level
        string Message
        string Exception
        text Properties
        datetime Timestamp
    }
    
    ETL_ALERTS {
        int Id PK
        string AlertType
        string Severity
        string Title
        text Message
        text Context
        bool IsResolved
        datetime ResolvedAt
        datetime CreatedAt
    }
    
    ETL_DATA_QUALITY_ISSUES {
        int Id PK
        string JobId
        string IssueType
        string FieldName
        text ExpectedValue
        text ActualValue
        string Severity
        text Description
        bool IsResolved
        datetime DetectedAt
    }
    
    CERTIFICATE_AGGREGATES {
        string Period PK
        date PeriodDate PK
        int TotalCertificates
        int ActiveCertificates
        int ExpiredCertificates
        int ExpiringCertificates
        text CertificatesByType
        text CertificatesByCountry
        text CertificatesByCompany
        datetime CreatedAt
        datetime UpdatedAt
    }
    
    ETL_LOG_ENTRIES ||--o{ ETL_CONFIGURATIONS : "belongs_to_job"
    ETL_ALERTS ||--o{ ETL_CONFIGURATIONS : "related_to_job"
    ETL_DATA_QUALITY_ISSUES ||--o{ ETL_CONFIGURATIONS : "detected_in_job"
```

---

## Gantt Charts

### Project Development Timeline
```mermaid
gantt
    title GCC Web Application Development Timeline
    dateFormat YYYY-MM-DD
    section Phase 1 - Foundation
    Project Setup           :done, setup, 2025-01-01, 2025-01-07
    Database Design         :done, dbdesign, 2025-01-08, 2025-01-15
    Core Models            :done, models, 2025-01-16, 2025-01-22
    Authentication System   :done, auth, 2025-01-23, 2025-01-30
    
    section Phase 2 - Core Features
    Certificate Management  :done, certmgmt, 2025-01-31, 2025-02-14
    Company Management     :done, companymgmt, 2025-02-15, 2025-02-21
    Product Management     :done, productmgmt, 2025-02-22, 2025-02-28
    User Management        :done, usermgmt, 2025-03-01, 2025-03-07
    
    section Phase 3 - Advanced Features
    API Development        :done, api, 2025-03-08, 2025-03-21
    File Upload System     :done, fileupload, 2025-03-22, 2025-03-28
    Search & Filtering     :done, search, 2025-03-29, 2025-04-04
    Reporting System       :done, reporting, 2025-04-05, 2025-04-11
    
    section Phase 4 - ETL & Analytics
    ETL Framework          :active, etl, 2025-04-12, 2025-04-25
    Data Quality Monitoring :active, dataquality, 2025-04-26, 2025-05-02
    Analytics Dashboard    :active, analytics, 2025-05-03, 2025-05-09
    Performance Optimization :performance, 2025-05-10, 2025-05-16
    
    section Phase 5 - Security & Deployment
    Security Hardening     :security, 2025-05-17, 2025-05-23
    Testing & QA          :testing, 2025-05-24, 2025-05-30
    Documentation         :docs, 2025-05-31, 2025-06-06
    Production Deployment  :deployment, 2025-06-07, 2025-06-13
    
    section Phase 6 - Post-Launch
    Bug Fixes & Support   :support, 2025-06-14, 2025-07-11
    Feature Enhancements  :enhancements, 2025-07-12, 2025-08-08
    Performance Monitoring :monitoring, 2025-08-09, 2025-08-22
```

### Sprint Planning Gantt
```mermaid
gantt
    title Current Sprint Development Plan
    dateFormat YYYY-MM-DD
    section Sprint 12 - Security Enhancements
    API Security Audit        :active, apisec, 2025-08-25, 2025-08-27
    Two-Factor Authentication  :active, 2fa, 2025-08-25, 2025-08-29
    Security Headers          :secheaders, 2025-08-28, 2025-08-30
    Rate Limiting            :ratelimit, 2025-08-31, 2025-09-02
    
    section Sprint 13 - Performance
    Database Optimization    :dbopt, 2025-09-03, 2025-09-06
    Caching Implementation   :caching, 2025-09-07, 2025-09-09
    Query Performance       :queryopt, 2025-09-10, 2025-09-12
    Load Testing           :loadtest, 2025-09-13, 2025-09-15
    
    section Sprint 14 - User Experience
    UI/UX Improvements      :uiux, 2025-09-16, 2025-09-20
    Mobile Responsiveness   :mobile, 2025-09-21, 2025-09-23
    Accessibility Features  :a11y, 2025-09-24, 2025-09-26
    User Feedback Integration :feedback, 2025-09-27, 2025-09-29
```

---

## Pie Charts

### Certificate Distribution by Status
```mermaid
pie title Certificate Status Distribution
    "Active" : 65
    "Expired" : 20
    "Pending" : 10
    "Suspended" : 3
    "Revoked" : 2
```

### User Role Distribution
```mermaid
pie title User Role Distribution
    "Standard User" : 45
    "Administrator" : 15
    "Analyst" : 25
    "Manager" : 10
    "Super Admin" : 5
```

### API Usage by Endpoint
```mermaid
pie title API Endpoint Usage
    "GET /certificates" : 35
    "GET /certificates/{id}" : 25
    "POST /certificates" : 15
    "PUT /certificates/{id}" : 12
    "GET /companies" : 8
    "DELETE /certificates/{id}" : 3
    "Other Endpoints" : 2
```

### Database Storage by Entity
```mermaid
pie title Database Storage Distribution
    "Certificates" : 40
    "Certificate Documents" : 25
    "Products" : 15
    "Users" : 8
    "Companies" : 7
    "Audit Logs" : 3
    "Other Tables" : 2
```

---

## Git Graphs

### Repository Branch Structure
```mermaid
gitgraph
    commit id: "Initial commit"
    commit id: "Setup project structure"
    commit id: "Add basic models"
    
    branch feature/authentication
    checkout feature/authentication
    commit id: "Add user model"
    commit id: "Implement login"
    commit id: "Add 2FA support"
    
    checkout main
    merge feature/authentication
    commit id: "Authentication complete"
    
    branch feature/certificate-management
    checkout feature/certificate-management
    commit id: "Add certificate model"
    commit id: "CRUD operations"
    commit id: "File upload support"
    
    branch feature/api-development
    checkout feature/api-development
    commit id: "API controllers"
    commit id: "Authentication middleware"
    commit id: "Swagger documentation"
    
    checkout main
    merge feature/certificate-management
    commit id: "Certificate management ready"
    
    merge feature/api-development
    commit id: "API endpoints complete"
    
    branch feature/etl-system
    checkout feature/etl-system
    commit id: "ETL framework"
    commit id: "Data transformation"
    commit id: "Quality monitoring"
    
    checkout main
    branch hotfix/security-patch
    checkout hotfix/security-patch
    commit id: "Fix security issue"
    
    checkout main
    merge hotfix/security-patch
    commit id: "Security patch applied"
    
    merge feature/etl-system
    commit id: "ETL system integrated"
    
    commit id: "Release v1.0"
```

### Feature Development Flow
```mermaid
gitgraph
    commit id: "Sprint start"
    
    branch feature/user-management
    checkout feature/user-management
    commit id: "User CRUD"
    commit id: "Role management"
    
    branch feature/advanced-search
    checkout feature/advanced-search
    commit id: "Search filters"
    commit id: "Pagination"
    
    checkout main
    branch feature/reporting
    checkout feature/reporting
    commit id: "Report templates"
    commit id: "Export functionality"
    
    checkout feature/user-management
    merge main
    commit id: "Merge latest changes"
    
    checkout main
    merge feature/user-management
    commit id: "User management complete"
    
    checkout feature/advanced-search
    merge main
    commit id: "Sync with main"
    
    checkout main
    merge feature/advanced-search
    commit id: "Search features ready"
    
    merge feature/reporting
    commit id: "Reporting system live"
```

---

## Mind Maps

### System Architecture Overview
```mermaid
mindmap
  root((GCC Web App))
    Frontend
      MVC Views
        Certificate Views
        Admin Views
        Account Views
      JavaScript
        jQuery
        Bootstrap
        Custom Scripts
      Responsive Design
        Mobile Support
        Tablet Support
    Backend
      Controllers
        Certificate Controller
        API Controllers
        Admin Controller
        Account Controller
      Services
        Certificate Service
        ETL Service
        Email Service
        Security Service
      Repositories
        Generic Repository
        Unit of Work
        Specific Repositories
    Database
      SQLite
        Core Tables
        Audit Tables
        ETL Tables
      Entity Framework
        Code First
        Migrations
        Relationships
    Security
      Authentication
        Cookie Auth
        API Key Auth
        2FA Support
      Authorization
        Role Based
        Permission Based
        Policy Based
      Data Protection
        Encryption
        Secure Headers
        Rate Limiting
    Integration
      APIs
        RESTful Design
        Versioning
        Swagger Docs
      ETL System
        Data Extraction
        Transformation
        Loading
        Monitoring
      File Management
        Upload System
        Storage
        Validation
```

### Feature Breakdown
```mermaid
mindmap
  root((Core Features))
    Certificate Management
      CRUD Operations
        Create Certificate
        View Certificate
        Update Certificate
        Delete Certificate
      File Management
        PDF Upload
        Document Storage
        File Validation
      Search & Filter
        Text Search
        Date Filters
        Status Filters
        Advanced Search
    User Management
      Authentication
        Login/Logout
        Password Reset
        Two-Factor Auth
        Session Management
      Authorization
        Role Assignment
        Permission Control
        Access Control
      Profile Management
        User Details
        Security Settings
        Preferences
    Data Management
      Import/Export
        Excel Import
        CSV Export
        PDF Export
        Data Validation
      ETL Processing
        Scheduled Jobs
        Data Quality
        Error Handling
        Monitoring
      Backup/Restore
        Database Backup
        File Backup
        Recovery Options
    System Administration
      Configuration
        System Settings
        Email Settings
        Security Settings
      Monitoring
        Performance Metrics
        Error Logging
        Audit Trail
        Health Checks
      Maintenance
        Database Cleanup
        Log Rotation
        System Updates
```

---

## Timeline

### Development Milestones
```mermaid
timeline
    title GCC Web Application Development Timeline
    
    2025-01-01 : Project Initiation
               : Requirements Gathering
               : Technology Stack Selection
    
    2025-01-15 : Database Design Complete
               : Initial Models Created
               : Entity Framework Setup
    
    2025-02-01 : Authentication System
               : Basic CRUD Operations
               : MVC Structure
    
    2025-03-01 : Certificate Management
               : File Upload System
               : Search Functionality
    
    2025-04-01 : API Development
               : Swagger Documentation
               : Security Implementation
    
    2025-05-01 : ETL Framework
               : Data Quality Monitoring
               : Performance Optimization
    
    2025-06-01 : Testing & QA
               : Documentation
               : Security Audit
    
    2025-07-01 : Production Deployment
               : User Training
               : Go-Live Support
    
    2025-08-01 : Post-Launch Support
               : Bug Fixes
               : Feature Enhancements
               
    2025-08-25 : Current Status
               : Security Enhancements
               : Performance Improvements
```

### Security Enhancement Timeline
```mermaid
timeline
    title Security Enhancement Implementation
    
    2025-08-01 : Security Assessment
               : Vulnerability Scan
               : Risk Analysis
    
    2025-08-05 : API Key Authentication
               : Rate Limiting Setup
               : Security Headers
    
    2025-08-10 : Two-Factor Authentication
               : Password Policies
               : Account Lockout
    
    2025-08-15 : Data Encryption
               : Secure Configuration
               : Logging Enhancement
    
    2025-08-20 : Security Testing
               : Penetration Testing
               : Code Review
    
    2025-08-25 : Current Phase
               : Final Security Audit
               : Documentation Update
    
    2025-08-30 : Security Certification
               : Compliance Check
               : Go-Live Approval
```

---

## Requirement Diagrams

### System Requirements
```mermaid
requirementDiagram
    requirement certificate_management {
        id: 1
        text: The system shall manage certificates
        risk: high
        verifymethod: test
    }

    requirement user_authentication {
        id: 2
        text: The system shall authenticate users
        risk: high
        verifymethod: test
    }

    requirement api_access {
        id: 3
        text: The system shall provide API access
        risk: medium
        verifymethod: test
    }

    requirement file_upload {
        id: 4
        text: The system shall support file uploads
        risk: medium
        verifymethod: test
    }

    requirement data_export {
        id: 5
        text: The system shall export data
        risk: low
        verifymethod: test
    }

    requirement security_compliance {
        id: 6
        text: The system shall be security compliant
        risk: high
        verifymethod: audit
    }

    requirement performance {
        id: 7
        text: The system shall meet performance requirements
        risk: medium
        verifymethod: performance_test
    }

    requirement etl_processing {
        id: 8
        text: The system shall process ETL data
        risk: medium
        verifymethod: test
    }

    element certificate_service {
        type: service
    }

    element authentication_service {
        type: service
    }

    element api_controller {
        type: controller
    }

    element file_service {
        type: service
    }

    element export_service {
        type: service
    }

    element security_middleware {
        type: middleware
    }

    element cache_service {
        type: service
    }

    element etl_service {
        type: service
    }

    certificate_management - satisfies -> certificate_service
    user_authentication - satisfies -> authentication_service
    api_access - satisfies -> api_controller
    file_upload - satisfies -> file_service
    data_export - satisfies -> export_service
    security_compliance - satisfies -> security_middleware
    performance - satisfies -> cache_service
    etl_processing - satisfies -> etl_service

    certificate_service - derives -> user_authentication
    api_controller - derives -> user_authentication
    file_service - derives -> security_compliance
    export_service - derives -> security_compliance
```

### Functional Requirements
```mermaid
requirementDiagram
    requirement user_registration {
        id: FR-001
        text: Users shall be able to register accounts
        risk: medium
        verifymethod: test
    }

    requirement certificate_crud {
        id: FR-002
        text: Users shall perform CRUD operations on certificates
        risk: high
        verifymethod: test
    }

    requirement certificate_search {
        id: FR-003
        text: Users shall search certificates by multiple criteria
        risk: medium
        verifymethod: test
    }

    requirement file_management {
        id: FR-004
        text: System shall manage certificate files
        risk: medium
        verifymethod: test
    }

    requirement role_management {
        id: FR-005
        text: Administrators shall manage user roles
        risk: high
        verifymethod: test
    }

    requirement audit_logging {
        id: FR-006
        text: System shall log all user activities
        risk: high
        verifymethod: audit
    }

    requirement data_validation {
        id: FR-007
        text: System shall validate all input data
        risk: high
        verifymethod: test
    }

    requirement notification_system {
        id: FR-008
        text: System shall send notifications for events
        risk: low
        verifymethod: test
    }

    element user_controller {
        type: controller
    }

    element certificate_controller {
        type: controller
    }

    element search_service {
        type: service
    }

    element file_upload_service {
        type: service
    }

    element admin_service {
        type: service
    }

    element audit_service {
        type: service
    }

    element validation_service {
        type: service
    }

    element notification_service {
        type: service
    }

    user_registration - satisfies -> user_controller
    certificate_crud - satisfies -> certificate_controller
    certificate_search - satisfies -> search_service
    file_management - satisfies -> file_upload_service
    role_management - satisfies -> admin_service
    audit_logging - satisfies -> audit_service
    data_validation - satisfies -> validation_service
    notification_system - satisfies -> notification_service
```

---

## Quadrant Charts

### Feature Priority Matrix
```mermaid
quadrantChart
    title Feature Priority Matrix
    x-axis Low Effort --> High Effort
    y-axis Low Impact --> High Impact
    
    quadrant-1 Do First
    quadrant-2 Schedule
    quadrant-3 Delegate
    quadrant-4 Don't Do
    
    Security Enhancements: [0.2, 0.9]
    Performance Optimization: [0.4, 0.8]
    Two-Factor Authentication: [0.3, 0.85]
    API Rate Limiting: [0.25, 0.7]
    Mobile Responsiveness: [0.6, 0.6]
    Advanced Reporting: [0.7, 0.65]
    Email Notifications: [0.15, 0.5]
    Dark Mode: [0.8, 0.3]
    Social Login: [0.75, 0.25]
    Chat Support: [0.9, 0.4]
    Bulk Operations: [0.5, 0.75]
    Data Export: [0.35, 0.6]
    User Preferences: [0.45, 0.35]
    Audit Logging: [0.3, 0.8]
    File Versioning: [0.65, 0.45]
```

### Technology Stack Evaluation
```mermaid
quadrantChart
    title Technology Stack Assessment
    x-axis Low Complexity --> High Complexity
    y-axis Low Value --> High Value
    
    quadrant-1 Adopt
    quadrant-2 Trial
    quadrant-3 Assess
    quadrant-4 Hold
    
    ASP.NET Core: [0.3, 0.9]
    Entity Framework: [0.25, 0.85]
    SQLite: [0.15, 0.8]
    Bootstrap: [0.2, 0.7]
    jQuery: [0.1, 0.6]
    Swagger: [0.2, 0.75]
    Serilog: [0.25, 0.7]
    Docker: [0.6, 0.8]
    Kubernetes: [0.9, 0.85]
    Redis: [0.4, 0.6]
    Elasticsearch: [0.7, 0.65]
    GraphQL: [0.75, 0.5]
    SignalR: [0.45, 0.4]
    Blazor: [0.65, 0.45]
    Angular: [0.8, 0.55]
    React: [0.85, 0.6]
```

---

## User Journey Diagrams

### Certificate Creation Journey
```mermaid
journey
    title Certificate Creation User Journey
    section Login
      Navigate to site: 5: User
      Enter credentials: 4: User
      Two-factor auth: 3: User
      Access dashboard: 5: User
    section Certificate Creation
      Click create certificate: 5: User
      Fill basic info: 4: User
      Select company: 4: User
      Select product: 3: User
      Enter dates: 4: User
      Upload certificate file: 2: User
      Add custom fields: 3: User
      Review information: 4: User
      Submit certificate: 5: User
    section Validation
      System validation: 3: System
      File processing: 2: System
      Success notification: 5: User, System
      View created certificate: 5: User
```

### Admin User Management Journey
```mermaid
journey
    title Admin User Management Journey
    section Access Admin Panel
      Login as admin: 5: Admin
      Navigate to user management: 5: Admin
      View user list: 4: Admin
    section User Creation
      Click add user: 5: Admin
      Fill user details: 4: Admin
      Assign roles: 4: Admin
      Set permissions: 3: Admin
      Configure security: 3: Admin
      Send invitation: 4: Admin, System
    section User Management
      Search users: 5: Admin
      Filter by role: 4: Admin
      View user details: 4: Admin
      Modify permissions: 3: Admin
      Deactivate user: 4: Admin
      Audit user activity: 5: Admin
    section Monitoring
      Check login attempts: 4: Admin
      Review security logs: 3: Admin
      Monitor system health: 4: Admin, System
```

### API Integration Journey
```mermaid
journey
    title API Integration User Journey
    section API Setup
      Register for API access: 4: Developer
      Generate API key: 5: Developer, System
      Read documentation: 3: Developer
      Test endpoints: 3: Developer
    section Integration Development
      Implement authentication: 2: Developer
      Create certificate queries: 4: Developer
      Handle pagination: 3: Developer
      Implement error handling: 3: Developer
      Add rate limiting logic: 2: Developer
    section Testing & Deployment
      Unit testing: 4: Developer
      Integration testing: 3: Developer
      Performance testing: 2: Developer
      Production deployment: 4: Developer
      Monitor API usage: 5: Developer, System
    section Maintenance
      Update API keys: 4: Developer
      Handle deprecations: 2: Developer
      Monitor rate limits: 3: Developer, System
      Review usage analytics: 4: Developer
```

### Certificate Expiry Management Journey
```mermaid
journey
    title Certificate Expiry Management Journey
    section Monitoring
      System checks expiring certificates: 5: System
      Generate expiry reports: 4: System
      Send email notifications: 3: System, User
    section User Response
      Receive notification: 2: User
      Login to system: 4: User
      Review expiring certificates: 4: User
      Prioritize renewals: 3: User
    section Renewal Process
      Contact certificate issuer: 2: User
      Prepare renewal documents: 3: User
      Submit renewal application: 3: User
      Upload new certificate: 4: User
      Update system records: 5: User
    section Verification
      Validate new certificate: 4: System
      Update expiry dates: 5: System
      Confirm renewal: 5: User, System
      Archive old certificate: 4: System
```

---

## Summary

This comprehensive collection of Mermaid diagrams provides detailed visualization of the GCC Web Application system architecture, processes, and user interactions. The diagrams cover:

- **Operational Flows**: Certificate management, API processing, ETL operations
- **System Architecture**: Class relationships, database schema, service layers
- **User Experience**: Authentication states, user journeys, admin workflows
- **Project Management**: Development timelines, feature priorities, sprint planning
- **System Analytics**: Usage statistics, performance metrics, technology evaluation

These diagrams serve as living documentation that can be updated as the system evolves and provide clear visual communication for stakeholders, developers, and system administrators.