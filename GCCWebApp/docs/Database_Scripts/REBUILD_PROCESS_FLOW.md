# Database Rebuild Process Flow

## Visual Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                    DATABASE REBUILD PROCESS                      │
└─────────────────────────────────────────────────────────────────┘

                              START
                                │
                                ▼
┌───────────────────────────────────────────────────────────────────┐
│  STEP 1: Generate Fresh Schema Script                             │
│  ─────────────────────────────────────────────────────────────   │
│  Command: dotnet ef migrations script --idempotent               │
│  Input:   Entity Framework migrations in Migrations/ folder      │
│  Output:  schema_migrations.sql (with SET statements)            │
│  Time:    ~10 seconds                                            │
│  Status:  ✅ Required - ensures latest schema                    │
└───────────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌───────────────────────────────────────────────────────────────────┐
│  STEP 2: Drop All Tables Safely                                  │
│  ─────────────────────────────────────────────────────────────   │
│  Script:  DROP_ALL_TABLES_SAFE.sql                               │
│  Action:  1. Drop all foreign key constraints                    │
│           2. Drop all tables (except __EFMigrationsHistory)       │
│           3. Drop __EFMigrationsHistory last                      │
│  Result:  Empty database (0 tables)                              │
│  Time:    ~5 seconds                                             │
│  Status:  ⚠️  DESTRUCTIVE - deletes all data!                     │
└───────────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌───────────────────────────────────────────────────────────────────┐
│  STEP 3: Recreate Schema from Migrations                         │
│  ─────────────────────────────────────────────────────────────   │
│  Script:  schema_migrations.sql (generated in Step 1)            │
│  Action:  1. Create __EFMigrationsHistory table                  │
│           2. Create all 35 application tables                     │
│           3. Create indexes, constraints, foreign keys            │
│           4. Insert migration records                             │
│  Result:  35 tables with proper structure (no data)              │
│  Time:    ~30-60 seconds                                         │
│  Status:  ✅ Safe - only creates structure                        │
└───────────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌───────────────────────────────────────────────────────────────────┐
│  STEP 4: Seed Test Data                                          │
│  ─────────────────────────────────────────────────────────────   │
│  Script:  FULL_TEST_DATA_SEED.sql                                │
│  Action:  Insert test data in order:                             │
│           1. Product Categories (6)                               │
│           2. Certification Types (5)                              │
│           3. Partners (3)                                         │
│           4. Companies (28)                                       │
│           5. Product Types (19)                                   │
│           6. Products (43)                                        │
│           7. Roles (4)                                            │
│           8. Permissions (6)                                      │
│           9. Permission-Role assignments                          │
│          10. Users (15) with BCrypt hashed passwords              │
│          11. User-Role assignments                                │
│          12. Base Certificates (25)                               │
│          13. Tags (10)                                            │
│          14. API Keys (3)                                         │
│  Result:  Fully populated database ready for testing             │
│  Time:    ~30-60 seconds                                         │
│  Status:  ✅ Safe - only inserts data                             │
└───────────────────────────────────────────────────────────────────┘
                                │
                                ▼
┌───────────────────────────────────────────────────────────────────┐
│  VERIFICATION: Check Results                                      │
│  ─────────────────────────────────────────────────────────────   │
│  Checks:  1. Table count = 35                                    │
│           2. User count = 15                                      │
│           3. Product count = 43                                   │
│           4. Certificate count = 25                               │
│           5. All tables in 'dbo' schema                           │
│  Test:    Login as admin/TestPass123!                            │
│  Status:  ✅ Rebuild successful!                                  │
└───────────────────────────────────────────────────────────────────┘
                                │
                                ▼
                              END
```

---

## Decision Tree: Which Method to Use?

```
                    Need to rebuild database?
                              │
                              ▼
                     ┌────────┴────────┐
                     │                 │
            Are you comfortable    Do you want
            with command line?     automation?
                     │                 │
                ┌────┴───┐         ┌───┴────┐
                │        │         │        │
               YES      NO        YES      NO
                │        │         │        │
                ▼        ▼         ▼        ▼
         ┌──────────┐ ┌──────┐ ┌─────────────┐ ┌──────────┐
         │ Manual   │ │ Use  │ │ Run         │ │ Use      │
         │ Step by  │ │ GUI  │ │ REBUILD_    │ │ Database │
         │ Step     │ │ Tool │ │ COMPLETE_   │ │ Manager  │
         │          │ │ (N/A)│ │ PRODUCTION  │ │ Menu     │
         └──────────┘ └──────┘ └─────────────┘ └──────────┘
              │                       │              │
              │                       │              │
              └───────────────┬───────┴──────────────┘
                              │
                    Follow instructions
                    in QUICK_REFERENCE.md
                    or PRODUCTION_REBUILD_GUIDE.md
```

---

## Dependency Flow

```
┌─────────────────────────────────────────────────────────────────┐
│                      DATA SEEDING ORDER                          │
│                  (Must follow this sequence)                     │
└─────────────────────────────────────────────────────────────────┘

1. Product Categories
   └─→ No dependencies
       │
2. Certification Types
   └─→ No dependencies
       │
3. Partners
   └─→ No dependencies
       │
4. Companies
   └─→ No dependencies
       │
5. Product Types
   └─→ Depends on: Product Categories
       │
6. Products
   └─→ Depends on: Companies, Product Types, Product Categories
       │
7. Roles
   └─→ No dependencies
       │
8. Permissions
   └─→ No dependencies
       │
9. Permission-Role Assignments
   └─→ Depends on: Roles, Permissions
       │
10. Users
    └─→ No dependencies (direct)
        │
11. User-Role Assignments
    └─→ Depends on: Users, Roles
        │
12. Certificates
    └─→ Depends on: Companies, Products, Certification Types, Partners, Users
        │
13. Tags
    └─→ No dependencies
        │
14. API Keys
    └─→ Depends on: Users (CreatedBy field)

⚠️ ORDER MATTERS! Do not change sequence or foreign key violations will occur.
```

---

## Error Recovery Flow

```
                    Script Failed?
                         │
                         ▼
                Which step failed?
                         │
        ┌────────────────┼────────────────┐
        │                │                │
   Step 1          Step 2-3           Step 4
   (Generate)      (Drop/Create)      (Seed)
        │                │                │
        ▼                ▼                ▼
   ┌─────────┐    ┌──────────┐    ┌──────────┐
   │ Check   │    │ Check    │    │ Check    │
   │ .NET    │    │ SQL      │    │ INSERT   │
   │ SDK     │    │ syntax   │    │ columns  │
   │ version │    │ errors   │    │ match    │
   └─────────┘    └──────────┘    └──────────┘
        │                │                │
        ▼                ▼                ▼
   Install/         Add SET          Update
   Update          statements       FULL_TEST_
   .NET 8.0        to script        DATA_SEED.sql
        │                │                │
        └────────────────┼────────────────┘
                         │
                         ▼
                  Re-run from
                   Step 1
                         │
                         ▼
                   Success?
                   │     │
              ┌────┘     └────┐
             YES             NO
              │               │
              ▼               ▼
           Done!      Consult
                    PRODUCTION_
                    REBUILD_
                    GUIDE.md
```

---

## Safety Checklist

Before running rebuild:

```
□ Production test environment (NOT real production)
□ Connection details verified (68.71.129.120,1533)
□ User has db_owner permissions
□ Backup created (if needed)
□ All developers notified (if shared environment)
□ Time allocated (~5 minutes total)
□ No active user sessions on database
□ REBUILD_COMPLETE_PRODUCTION.cmd ready to run
```

After rebuild:

```
□ Table count = 35
□ User count = 15
□ Login successful (admin/TestPass123!)
□ User Management page loads
□ Product Management page loads
□ Certificate Tile View page loads
□ All tables in 'dbo' schema (not username schema)
```

---

## Quick Command Reference

| Task | Command |
|------|---------|
| **Full Rebuild (Automated)** | `REBUILD_COMPLETE_PRODUCTION.cmd` |
| **Generate Schema** | `dotnet ef migrations script --idempotent` |
| **Drop Tables** | `sqlcmd ... -i DROP_ALL_TABLES_SAFE.sql` |
| **Create Schema** | `sqlcmd ... -i schema_migrations.sql` |
| **Seed Data** | `sqlcmd ... -i FULL_TEST_DATA_SEED.sql` |
| **Check Tables** | `sqlcmd ... -Q "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES..."` |
| **Verify Data** | `sqlcmd ... -Q "SELECT COUNT(*) FROM Users"` |

---

## File Dependencies

```
REBUILD_COMPLETE_PRODUCTION.cmd
  │
  ├─→ Calls: dotnet ef migrations script
  │          (generates schema_migrations.sql)
  │
  ├─→ Calls: DROP_ALL_TABLES_SAFE.sql
  │          (Database_Scripts/DROP_ALL_TABLES_SAFE.sql)
  │
  ├─→ Calls: schema_migrations.sql
  │          (generated in GCCWebApp/ folder)
  │
  └─→ Calls: FULL_TEST_DATA_SEED.sql
             (Database_Scripts/FULL_TEST_DATA_SEED.sql)

All scripts must be present and up-to-date!
```

---

## Success Indicators

✅ **Step 1 Success**: File `schema_migrations.sql` created, starts with SET statements
✅ **Step 2 Success**: Output shows "SUCCESS: All tables dropped"
✅ **Step 3 Success**: No error messages, 35 tables created
✅ **Step 4 Success**: Output shows "SEEDING COMPLETE" with statistics

❌ **Failure Signs**: Error messages with "Msg XXXX", "Cannot insert", "Invalid object"

---

For detailed instructions, see: **PRODUCTION_REBUILD_GUIDE.md**
For quick commands, see: **QUICK_REFERENCE.md**
