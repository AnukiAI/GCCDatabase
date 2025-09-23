# GCC Database Seeding Process Documentation

## Overview

The GCC Database Solution implements a comprehensive seeding process that initializes the database with reference data, test users, and sample certificates. This process ensures the application has the necessary baseline data for development, testing, and initial production deployment.

## High-Level Seeding Flow

```mermaid
flowchart TD
    A[Application Startup] --> B{Database Connection Available?}
    B -->|No| C[Log Error & Continue]
    B -->|Yes| D[EnsureCreatedAsync]
    D --> E{Data Already Exists?}
    E -->|Yes| F[Skip Seeding - Log Info]
    E -->|No| G[Begin Seeding Process]
    G --> H[Seed Reference Data]
    H --> I[Seed Core Entities]
    I --> J[Seed Certificates & Sample Data]
    J --> K[Seed Users & Security]
    K --> L[Log Success]
    F --> M[Application Ready]
    L --> M
    C --> M
```

## Detailed Entity Seeding Sequence

```mermaid
flowchart TD
    A[Start Seeding Process] --> B[Check if Companies exist]
    B -->|Exists| C[Return - Database already seeded]
    B -->|Empty| D[Seed ProductCategories - 6 items]

    D --> E[Seed CertificationTypes - 5 items]
    E --> F[Seed Partners - 3 items]
    F --> G[Seed Companies - 25 items]
    G --> H[Seed ProductTypes - 8 items]
    H --> I[Seed ProductSubTypes - 10 items]
    I --> J[Seed Products - 43 items]
    J --> K[Seed Certificates - 175+ items]
    K --> L[Seed Tags - 20 items]
    L --> M[Seed Permissions - 6 items]
    M --> N[Seed Roles - 4 items]
    N --> O[Assign Role Permissions]
    O --> P[Seed Users - 15 items]
    P --> Q[Assign User Roles]
    Q --> R[Seed API Keys - 5 items]
    R --> S[Complete - Log Success]

    style D fill:#e1f5fe
    style E fill:#e1f5fe
    style F fill:#e1f5fe
    style G fill:#f3e5f5
    style H fill:#f3e5f5
    style I fill:#f3e5f5
    style J fill:#f3e5f5
    style K fill:#fff3e0
    style L fill:#e8f5e8
    style M fill:#ffebee
    style N fill:#ffebee
    style O fill:#ffebee
    style P fill:#ffebee
    style Q fill:#ffebee
    style R fill:#fff8e1
```

## Database Schema Dependencies

```mermaid
erDiagram
    ProductCategories ||--o{ ProductTypes : "has many"
    ProductTypes ||--o{ ProductSubTypes : "has many"
    ProductCategories ||--o{ Products : "categorizes"
    ProductTypes ||--o{ Products : "types"
    Companies ||--o{ Products : "manufactures"

    CertificationTypes ||--o{ Certificates : "certifies"
    Companies ||--o{ Certificates : "owns"
    Products ||--o{ Certificates : "certified"

    Roles ||--o{ Users : "assigned to"
    Permissions ||--o{ Roles : "granted to"
    Companies ||--o{ Users : "employed by"
    Partners ||--o{ Users : "partnered with"

    Users ||--o{ Certificates : "created by"
    Users ||--o{ Certificates : "updated by"
    Users ||--o{ ApiKeys : "owns"

    Certificates ||--o{ CertificateProductIds : "has many"
    Certificates ||--o{ Tags : "tagged with"
    Products ||--o{ Tags : "tagged with"
```

## Startup Integration Flow

```mermaid
sequenceDiagram
    participant App as Application
    participant PM as Program.cs
    participant SC as Scope
    participant DB as DbContext
    participant SD as SeedData
    participant Log as Logger

    App->>PM: Application Start
    PM->>PM: Configure Services
    PM->>PM: Build App
    PM->>PM: Configure Middleware
    PM->>SC: CreateScope()
    SC->>DB: GetRequiredService<ApplicationDbContext>()
    PM->>SD: InitializeAsync(context)

    SD->>DB: EnsureCreatedAsync()
    SD->>DB: Companies.AnyAsync()
    alt Database Empty
        SD->>DB: Seed ProductCategories
        SD->>DB: SaveChangesAsync()
        SD->>DB: Seed CertificationTypes
        SD->>DB: SaveChangesAsync()
        SD->>DB: Seed Partners
        SD->>DB: SaveChangesAsync()
        SD->>DB: Seed Companies (25 items)
        SD->>DB: SaveChangesAsync()
        SD->>DB: Seed ProductTypes & SubTypes
        SD->>DB: SaveChangesAsync()
        SD->>DB: Seed Products (43 items)
        SD->>DB: SaveChangesAsync()
        SD->>DB: Seed Certificates (175+ items)
        SD->>DB: SaveChangesAsync()
        SD->>DB: Seed Security Data
        SD->>DB: SaveChangesAsync()
        SD->>Log: Database seeded successfully
    else Database Has Data
        SD->>Log: Database already seeded
    end

    PM->>App: app.Run()
```

## Entity Seeding Details

### 1. Reference Data (Foundation Layer)

#### ProductCategories (6 items)
```mermaid
graph LR
    A[Building Materials] --> B[Electronics/Electrical]
    B --> C[Materials/Plastics]
    C --> D[Paper Products]
    D --> E[Textiles]
    E --> F[Consumer Products]
```

**Data Structure:**
- Name, Description, SortOrder, IsActive
- Used as foundation for product categorization

#### CertificationTypes (5 items)
```mermaid
graph TD
    A[Certified Environmental Facts - CEF]
    B[Certified Sustainability Facts - CSF]
    C[Recycled Content - RC]
    D[Energy Savings - ES]
    E[Material Ingredient Reporting - MIR]
```

#### Partners (3 items)
```mermaid
graph LR
    A[Amazon] --> B[Wayfair]
    B --> C[Other]
```

### 2. Core Business Entities

#### Companies (25 items)
**Sample Companies:**
- ASSA ABLOY Access & Egress Hardware Group
- Green Products Inc.
- 3M Company
- Shaw Industries Group
- Armstrong World Industries
- And 20 more manufacturing companies

#### Products (43 items)
**Categories Include:**
- Exit devices and hardware
- Insulation materials
- Carpet tiles and flooring
- LED lighting
- Office furniture
- Building materials

### 3. Certificate Data (175+ items)

```mermaid
pie title Certificate Distribution by Status
    "Active" : 65
    "Draft" : 15
    "Expired" : 10
    "Pending" : 8
    "Suspended" : 2
    "Expiring Soon" : 25
```

**Certificate Generation Logic:**
```mermaid
flowchart TD
    A[Generate Base Certificates - 25 items] --> B[Generate Additional Certificates - 150 items]
    B --> C{Random Status Assignment}
    C --> D[Check Expiration Date]
    D -->|Past| E[Status = 'expired']
    D -->|Within 3 months| F[Status = 'expiring_soon']
    D -->|Future effective| G[Status = 'draft' or 'pending']
    D -->|Active period| H[Status = 'active']

    E --> I[Add Product IDs - 25% chance UPC]
    F --> I
    G --> I
    H --> I
    I --> J[Add ASIN - 20% chance]
    J --> K[Add GTIN - 17% chance]
    K --> L[Add EAN - 14% chance]
    L --> M[Save to Database]
```

### 4. Security & User Management

#### Roles & Permissions
```mermaid
graph TD
    A[Admin Role] --> B[All Permissions]
    C[Analyst Role] --> D[Certificate Management - No Delete]
    E[External Role] --> F[View & Reports Only]
    G[Public Role] --> H[View Only]

    B --> I[certificate_view]
    B --> J[certificate_create]
    B --> K[certificate_edit]
    B --> L[certificate_delete]
    B --> M[user_manage]
    B --> N[report_view]

    D --> I
    D --> J
    D --> K
    D --> N

    F --> I
    F --> N

    H --> I
```

#### Users (15 items)
**User Distribution:**
- 4 Admin users (including system admin)
- 4 Analyst users (certificate management)
- 6 External users (partner companies)
- 1 Public user

#### API Keys (5 items)
**Key Types:**
- Integration Test Key (Development)
- Partner API Access (Production)
- Mobile App Key (Production)
- Reporting Service (Production)
- Legacy Integration (Staging - Inactive)

## Performance Considerations

### Seeding Performance Metrics
```mermaid
gantt
    title Database Seeding Timeline
    dateFormat X
    axisFormat %s

    section Reference Data
    ProductCategories    :0, 1
    CertificationTypes   :1, 2
    Partners            :2, 3

    section Core Entities
    Companies           :3, 8
    ProductTypes        :8, 10
    Products            :10, 15

    section Certificates
    Base Certificates   :15, 25
    Generated Certs     :25, 45

    section Security
    Roles & Permissions :45, 48
    Users               :48, 52
    API Keys           :52, 55
```

**Estimated Seeding Time:** 3-8 seconds depending on hardware

### Database Size After Seeding
- **Tables:** 20+ core tables
- **Records:** 400+ total records
- **Indexes:** 50+ performance indexes
- **Size:** ~2-5 MB (SQLite)

## Error Handling

```mermaid
flowchart TD
    A[Seeding Process] --> B{Exception Occurred?}
    B -->|No| C[Log Success Message]
    B -->|Yes| D[Catch Exception]
    D --> E[Log Error with Details]
    E --> F[Application Continues]
    F --> G[Seeding Skipped for Current Run]

    C --> H[Database Ready for Use]
    G --> I[Manual Seeding May Be Required]
```

**Common Error Scenarios:**
1. **Database Connection Issues:** Application continues with limited functionality
2. **Constraint Violations:** Indicates data model issues
3. **Disk Space Issues:** Large certificate dataset generation fails
4. **Permission Issues:** Database write permissions required

## Environment-Specific Considerations

### Development Environment
- Full seeding with sample data
- Test users with known passwords
- Comprehensive certificate dataset for testing

### Production Environment
- Reference data seeding only
- No test users created
- Minimal sample certificates
- Security keys from environment variables

### Testing Environment
- Comprehensive test data
- Predictable random seed (42) for consistent data
- Extended certificate dataset for performance testing

## Maintenance & Updates

### Adding New Seed Data
1. Update `SeedData.cs` with new entities
2. Maintain referential integrity order
3. Test with empty database
4. Update documentation

### Seeding Validation
```bash
# Check if seeding completed successfully
dotnet run
# Look for log message: "Database seeded successfully"

# Verify data counts
# Should see: Companies(25), Products(43), Certificates(175+), Users(15)
```

## Conclusion

The GCC Database seeding process is designed to be:
- **Idempotent:** Safe to run multiple times
- **Comprehensive:** Covers all essential data
- **Realistic:** Uses industry-relevant sample data
- **Performance-optimized:** Efficient bulk operations
- **Environment-aware:** Adapts to different deployment scenarios

This seeding process ensures that the application is immediately functional after deployment with realistic data for testing and development purposes.