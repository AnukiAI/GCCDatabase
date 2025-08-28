# GCC System Overview - Visual Guide

## 🏗️ System Architecture

```mermaid
graph TB
    subgraph "Users"
        PU[Public Users]
        PA[Partners]
        IA[Internal Admin]
        AN[Analysts]
    end
    
    subgraph "Frontend"
        WEB[Web Application]
        MOB[Mobile Responsive]
        API[API Interface]
    end
    
    subgraph "Application Layer"
        AUTH[Authentication<br/>Azure AD B2C]
        BUS[Business Logic]
        WF[Workflow Engine]
    end
    
    subgraph "Data Layer"
        DB[(SQL Server)]
        CACHE[Cache Layer]
        FILES[File Storage]
    end
    
    PU --> WEB
    PA --> API
    IA --> WEB
    AN --> WEB
    
    WEB --> AUTH
    API --> AUTH
    AUTH --> BUS
    BUS --> WF
    BUS --> DB
    BUS --> CACHE
    WF --> DB
    DB --> FILES
    
    style WEB fill:#4CAF50
    style API fill:#2196F3
    style DB fill:#FF9800
    style AUTH fill:#9C27B0
```

---

## 📊 Data Flow Diagram

```mermaid
flowchart LR
    subgraph Input
        A[Certificate Data] 
        B[User Input]
        C[Partner API]
    end
    
    subgraph Processing
        D[Validation]
        E[Business Rules]
        F[Workflow]
    end
    
    subgraph Storage
        G[(Database)]
        H[File System]
    end
    
    subgraph Output
        I[Web Display]
        J[PDF Export]
        K[API Response]
        L[Analytics]
    end
    
    A --> D
    B --> D
    C --> D
    D --> E
    E --> F
    F --> G
    F --> H
    G --> I
    G --> J
    G --> K
    G --> L
    
    style D fill:#FF5252
    style G fill:#4CAF50
    style I fill:#2196F3
```

---

## 🔄 Certificate Lifecycle

```mermaid
stateDiagram-v2
    [*] --> Draft: Create
    Draft --> Pending: Submit
    Pending --> UnderReview: Assign Reviewer
    UnderReview --> RequiresRevision: Issues Found
    UnderReview --> Approved: Approve
    RequiresRevision --> UnderReview: Resubmit
    Approved --> Active: Publish
    Active --> Suspended: Suspend
    Suspended --> Active: Reactivate
    Active --> Expiring: Near Expiry
    Expiring --> Expired: Time Passes
    Expired --> Renewal: Renew
    Renewal --> UnderReview: Submit
    Active --> Revoked: Revoke
    Revoked --> [*]
    Expired --> Archived: Archive
    Archived --> [*]
```

---

## 👥 User Journey Map

```mermaid
journey
    title Certificate Search User Journey
    section Public User
      Visit Website: 5: Public
      Search Certificates: 4: Public
      View Results: 5: Public
      Filter Results: 4: Public
      View Certificate: 5: Public
      Download PDF: 5: Public
    section Partner User
      API Authentication: 3: Partner
      Request Data: 4: Partner
      Receive Response: 5: Partner
      Process Data: 4: Partner
      Display on Site: 5: Partner
    section Admin User
      Login: 5: Admin
      Dashboard View: 5: Admin
      Create Certificate: 4: Admin
      Review Workflow: 4: Admin
      Approve Certificate: 5: Admin
      Generate Reports: 5: Admin
```

---

## 🗺️ Site Navigation Structure

```mermaid
graph TD
    HOME[Home Page]
    
    HOME --> PUBLIC[Public Access]
    HOME --> AUTH[Authenticated]
    
    PUBLIC --> SEARCH[Certificate Search]
    PUBLIC --> VIEW[View Certificates]
    PUBLIC --> DOWNLOAD[Download PDF]
    
    AUTH --> LOGIN[Login]
    LOGIN --> DASH[Dashboard]
    
    DASH --> CERT[Certificates]
    DASH --> COMP[Companies]
    DASH --> PROD[Products]
    DASH --> USERS[Users]
    DASH --> REPORTS[Reports]
    
    CERT --> CREATE[Create]
    CERT --> EDIT[Edit]
    CERT --> DELETE[Delete]
    CERT --> EXPORT[Export]
    
    REPORTS --> ANALYTICS[Analytics]
    REPORTS --> AUDIT[Audit Trail]
    REPORTS --> METRICS[Metrics]
    
    style HOME fill:#4CAF50
    style PUBLIC fill:#2196F3
    style AUTH fill:#FF9800
    style DASH fill:#9C27B0
```

---

## 🔐 Security Architecture

```mermaid
graph TB
    subgraph "External Layer"
        USER[User Request]
        HTTPS[HTTPS/TLS]
    end
    
    subgraph "Perimeter Security"
        FW[Firewall]
        WAF[Web App Firewall]
        DDOS[DDoS Protection]
    end
    
    subgraph "Application Security"
        AUTH[Authentication<br/>Azure AD B2C]
        AUTHZ[Authorization<br/>Role-Based]
        VAL[Input Validation]
        AUDIT[Audit Logging]
    end
    
    subgraph "Data Security"
        ENC[Encryption at Rest]
        BACKUP[Backup & Recovery]
        MASK[Data Masking]
    end
    
    USER --> HTTPS
    HTTPS --> FW
    FW --> WAF
    WAF --> DDOS
    DDOS --> AUTH
    AUTH --> AUTHZ
    AUTHZ --> VAL
    VAL --> AUDIT
    AUDIT --> ENC
    ENC --> BACKUP
    BACKUP --> MASK
    
    style HTTPS fill:#4CAF50
    style AUTH fill:#2196F3
    style ENC fill:#FF9800
```

---

## 📈 Performance Metrics Dashboard

```mermaid
graph LR
    subgraph "Real-Time Metrics"
        A[Response Time<br/>< 200ms]
        B[Uptime<br/>99.9%]
        C[Active Users<br/>150+]
        D[API Calls/min<br/>500]
    end
    
    subgraph "Daily Metrics"
        E[Total Certificates<br/>5,000+]
        F[New Users<br/>25/day]
        G[Downloads<br/>100/day]
        H[API Usage<br/>10K/day]
    end
    
    subgraph "Alerts"
        I[Performance]
        J[Security]
        K[Errors]
        L[Usage Limits]
    end
    
    A --> I
    B --> J
    C --> L
    D --> L
    
    style A fill:#4CAF50
    style B fill:#4CAF50
    style I fill:#FF9800
    style J fill:#F44336
```

---

## 🔄 Integration Points

```mermaid
graph TB
    subgraph "GCC System"
        CORE[Core Application]
        API[REST API]
        WEBHOOK[Webhooks]
    end
    
    subgraph "External Systems"
        AMAZON[Amazon]
        WAYFAIR[Wayfair]
        EMAIL[Email Service]
        SMS[SMS Service]
    end
    
    subgraph "Cloud Services"
        AZURE[Azure AD B2C]
        STORAGE[Azure Storage]
        CDN[Azure CDN]
    end
    
    CORE --> API
    API --> AMAZON
    API --> WAYFAIR
    WEBHOOK --> AMAZON
    WEBHOOK --> WAYFAIR
    CORE --> EMAIL
    CORE --> SMS
    CORE --> AZURE
    CORE --> STORAGE
    STORAGE --> CDN
    
    style CORE fill:#4CAF50
    style API fill:#2196F3
    style AZURE fill:#FF9800
```

---

## 📱 Responsive Design Flow

```mermaid
graph TD
    REQ[User Request]
    DETECT[Device Detection]
    
    REQ --> DETECT
    DETECT --> DESKTOP{Desktop?}
    DETECT --> TABLET{Tablet?}
    DETECT --> MOBILE{Mobile?}
    
    DESKTOP --> FULL[Full Layout<br/>1920x1080]
    TABLET --> MED[Medium Layout<br/>1024x768]
    MOBILE --> SMALL[Mobile Layout<br/>375x667]
    
    FULL --> RENDER[Render Page]
    MED --> RENDER
    SMALL --> RENDER
    
    RENDER --> OPTIMIZE[Optimize Images]
    OPTIMIZE --> CACHE[Cache Content]
    CACHE --> DISPLAY[Display to User]
    
    style DESKTOP fill:#4CAF50
    style TABLET fill:#2196F3
    style MOBILE fill:#FF9800
```

---

## 🎯 Feature Roadmap

```mermaid
gantt
    title Implementation Roadmap
    dateFormat  YYYY-MM-DD
    section Phase 1 - Core
    Database Setup           :done,    p1, 2025-01-01, 30d
    User Authentication      :done,    p2, after p1, 30d
    Certificate CRUD         :done,    p3, after p2, 30d
    Basic Search            :done,    p4, after p3, 20d
    
    section Phase 2 - Enhanced
    Advanced Search         :active,  p5, 2025-08-15, 30d
    Partner Integration     :active,  p6, 2025-08-20, 40d
    Analytics Dashboard     :done,    p7, 2025-07-01, 30d
    Mobile Responsive       :active,  p8, 2025-08-25, 30d
    
    section Phase 3 - Advanced
    Autocomplete Search     :         p9, 2025-09-15, 20d
    Lazy Loading           :         p10, 2025-09-20, 15d
    Two-Factor Auth        :         p11, 2025-10-01, 20d
    Bulk Operations        :         p12, 2025-10-15, 25d
    
    section Phase 4 - Launch
    UAT Testing            :         p13, 2025-11-01, 30d
    Performance Tuning     :         p14, 2025-11-15, 20d
    Security Audit         :         p15, 2025-11-20, 15d
    Production Deploy      :crit,    p16, 2025-12-01, 10d
```

---

## 🐳 Docker Deployment Architecture

```mermaid
graph TB
    subgraph "Docker Host"
        subgraph "Docker Compose Stack"
            WEB[GCC Web App Container<br/>ASP.NET Core MVC]
            DB[SQL Server Container<br/>Database]
            VOL[Volume Mounts<br/>Data Persistence]
        end
        
        subgraph "Networking"
            NET[Docker Network]
            PORTS[Port Mapping<br/>80:80, 443:443]
        end
        
        subgraph "Storage"
            DATA[Data Volume]
            LOGS[Logs Volume]
            FILES[Files Volume]
        end
    end
    
    subgraph "External"
        USERS[Users]
        LB[Load Balancer]
        BACKUP[Backup System]
    end
    
    USERS --> LB
    LB --> PORTS
    PORTS --> WEB
    WEB --> NET
    NET --> DB
    WEB --> VOL
    VOL --> DATA
    VOL --> LOGS  
    VOL --> FILES
    DATA --> BACKUP
    
    style WEB fill:#00D4FF
    style DB fill:#FF6B35
    style VOL fill:#28A745
    style LB fill:#6C757D
```

### Docker Benefits
- **Consistency**: Same environment across dev, staging, production
- **Scalability**: Easy horizontal scaling with container orchestration
- **Isolation**: Application and database run in separate containers
- **Portability**: Deploy anywhere Docker runs
- **Version Control**: Infrastructure as code with docker-compose.yml

---

## 🔧 Technology Stack

### Core Technologies
```mermaid
graph LR
    subgraph "Frontend"
        A[ASP.NET Core MVC 8.0]
        B[Razor Views]
        C[Bootstrap CSS]
        D[JavaScript/jQuery]
    end
    
    subgraph "Backend"
        E[C# .NET 8.0]
        F[Entity Framework Core]
        G[Azure AD B2C]
        H[QuestPDF]
    end
    
    subgraph "Database"
        I[(SQL Server)]
        J[(SQLite - Dev)]
    end
    
    subgraph "Deployment"
        K[Docker Container]
        L[Docker Compose]
        M[Volume Mounts]
    end
    
    A --> E
    E --> F
    F --> I
    F --> J
    E --> G
    E --> H
    K --> E
    L --> K
    M --> E
    
    style A fill:#00D4FF
    style E fill:#512BD4
    style I fill:#FF6B35
    style K fill:#2496ED
```

### Development Tools
- **Testing**: xUnit, Playwright, SpecFlow
- **Containerization**: Docker, Docker Compose
- **CI/CD**: GitHub Actions with Docker builds
- **IDE**: Visual Studio / VS Code with Docker support
- **Version Control**: Git
- **Package Management**: NuGet

---

*These diagrams provide a visual overview of the GCC system architecture, data flows, and implementation roadmap. All diagrams are interactive and can be exported for presentations.*