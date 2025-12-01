-- =============================================
-- SEED_PARTNER_API_KEYS.sql
-- Seeds Partner and PartnerApiKey data for production/test environments
-- Compatible with: SQL Server 2019+
-- Date: November 24, 2025
-- =============================================

USE [gccwebap_certificates]; -- Change to your database name
GO

SET NOCOUNT ON;
GO

PRINT 'Starting Partner and PartnerApiKey data seeding...';
PRINT '';

-- =============================================
-- STEP 1: Seed Partners Table
-- =============================================
PRINT 'STEP 1: Seeding Partners...';

-- Check if Partners table exists
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Partners' AND SCHEMA_NAME(schema_id) = 'dbo')
BEGIN
    PRINT 'ERROR: Partners table does not exist. Run schema migrations first.';
    RAISERROR('Partners table not found', 16, 1);
    RETURN;
END

-- Seed Amazon
IF NOT EXISTS (SELECT 1 FROM dbo.Partners WHERE Name = 'Amazon')
BEGIN
    INSERT INTO dbo.Partners (Name, Description, Website, Email, ContactInfo, IsActive, IsDeleted, SortOrder, CreatedAt, UpdatedAt)
    VALUES (
        'Amazon',
        'Global e-commerce marketplace specializing in certified sustainable products',
        'https://www.amazon.com',
        'partner-support@amazon.com',
        'Amazon Partner Program',
        1, -- IsActive
        0, -- IsDeleted
        1, -- SortOrder
        GETUTCDATE(),
        GETUTCDATE()
    );
    PRINT '  ✓ Inserted Partner: Amazon';
END
ELSE
BEGIN
    PRINT '  - Partner "Amazon" already exists';
END

-- Seed Wayfair
IF NOT EXISTS (SELECT 1 FROM dbo.Partners WHERE Name = 'Wayfair')
BEGIN
    INSERT INTO dbo.Partners (Name, Description, Website, Email, ContactInfo, IsActive, IsDeleted, SortOrder, CreatedAt, UpdatedAt)
    VALUES (
        'Wayfair',
        'Online furniture and home goods retailer focused on sustainable product offerings',
        'https://www.wayfair.com',
        'sustainability@wayfair.com',
        'Wayfair Sustainability Program',
        1, -- IsActive
        0, -- IsDeleted
        2, -- SortOrder
        GETUTCDATE(),
        GETUTCDATE()
    );
    PRINT '  ✓ Inserted Partner: Wayfair';
END
ELSE
BEGIN
    PRINT '  - Partner "Wayfair" already exists';
END

-- Seed Other
IF NOT EXISTS (SELECT 1 FROM dbo.Partners WHERE Name = 'Other')
BEGIN
    INSERT INTO dbo.Partners (Name, Description, Website, Email, ContactInfo, IsActive, IsDeleted, SortOrder, CreatedAt, UpdatedAt)
    VALUES (
        'Other',
        'Other retail partners and general partner category',
        '',
        '',
        '',
        1, -- IsActive
        0, -- IsDeleted
        3, -- SortOrder
        GETUTCDATE(),
        GETUTCDATE()
    );
    PRINT '  ✓ Inserted Partner: Other';
END
ELSE
BEGIN
    PRINT '  - Partner "Other" already exists';
END

PRINT '';
PRINT 'Partners table seeded successfully.';
PRINT '';

-- =============================================
-- STEP 2: Seed PartnerApiKeys Table
-- =============================================
PRINT 'STEP 2: Seeding PartnerApiKeys...';

-- Check if PartnerApiKeys table exists
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PartnerApiKeys' AND SCHEMA_NAME(schema_id) = 'dbo')
BEGIN
    PRINT 'ERROR: PartnerApiKeys table does not exist. Run schema migrations first.';
    RAISERROR('PartnerApiKeys table not found', 16, 1);
    RETURN;
END

-- Get Partner IDs
DECLARE @AmazonPartnerId INT = (SELECT Id FROM dbo.Partners WHERE Name = 'Amazon');
DECLARE @WayfairPartnerId INT = (SELECT Id FROM dbo.Partners WHERE Name = 'Wayfair');

IF @AmazonPartnerId IS NULL OR @WayfairPartnerId IS NULL
BEGIN
    PRINT 'ERROR: Amazon or Wayfair partner not found. Run STEP 1 first.';
    RAISERROR('Partner not found', 16, 1);
    RETURN;
END

-- Seed Amazon Production API Key
DECLARE @AmazonApiKey NVARCHAR(512) = 'gcc_partner_amazon_prod_' + REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', '');
IF NOT EXISTS (SELECT 1 FROM dbo.PartnerApiKeys WHERE PartnerId = @AmazonPartnerId AND Description LIKE '%Production%')
BEGIN
    INSERT INTO dbo.PartnerApiKeys (
        PartnerId,
        ApiKey,
        Description,
        IsActive,
        CreatedAt,
        CreatedBy,
        ExpiresAt,
        LastUsedAt,
        RequestCount,
        RateLimitPerHour,
        AllowedEndpoints,
        Permissions,
        Notes,
        RevokedAt,
        RevokedBy
    )
    VALUES (
        @AmazonPartnerId,
        @AmazonApiKey,
        'Amazon Production API Key',
        1, -- IsActive
        GETUTCDATE(),
        'System Seed',
        DATEADD(YEAR, 1, GETUTCDATE()), -- Expires in 1 year
        NULL, -- LastUsedAt
        0, -- RequestCount
        2000, -- RateLimitPerHour
        '["/api/v1/certificates/*", "/api/v1/products/*"]', -- AllowedEndpoints
        '["read", "write"]', -- Permissions
        'Production API key for Amazon partner integration',
        NULL, -- RevokedAt
        NULL  -- RevokedBy
    );
    PRINT '  ✓ Inserted PartnerApiKey: Amazon Production';
    PRINT '    API Key: ' + @AmazonApiKey;
END
ELSE
BEGIN
    PRINT '  - Amazon Production API key already exists';
    SELECT @AmazonApiKey = ApiKey FROM dbo.PartnerApiKeys WHERE PartnerId = @AmazonPartnerId AND Description LIKE '%Production%';
    PRINT '    API Key: ' + @AmazonApiKey;
END

-- Seed Amazon Test API Key
DECLARE @AmazonTestApiKey NVARCHAR(512) = 'gcc_partner_amazon_test_' + REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', '');
IF NOT EXISTS (SELECT 1 FROM dbo.PartnerApiKeys WHERE PartnerId = @AmazonPartnerId AND Description LIKE '%Test%')
BEGIN
    INSERT INTO dbo.PartnerApiKeys (
        PartnerId,
        ApiKey,
        Description,
        IsActive,
        CreatedAt,
        CreatedBy,
        ExpiresAt,
        LastUsedAt,
        RequestCount,
        RateLimitPerHour,
        AllowedEndpoints,
        Permissions,
        Notes,
        RevokedAt,
        RevokedBy
    )
    VALUES (
        @AmazonPartnerId,
        @AmazonTestApiKey,
        'Amazon Test API Key',
        1, -- IsActive
        GETUTCDATE(),
        'System Seed',
        DATEADD(YEAR, 1, GETUTCDATE()), -- Expires in 1 year
        NULL, -- LastUsedAt
        0, -- RequestCount
        2000, -- RateLimitPerHour
        '["/api/v1/certificates/*", "/api/v1/products/*"]', -- AllowedEndpoints
        '["read", "write"]', -- Permissions
        'Test API key for Amazon partner integration',
        NULL, -- RevokedAt
        NULL  -- RevokedBy
    );
    PRINT '  ✓ Inserted PartnerApiKey: Amazon Test';
    PRINT '    API Key: ' + @AmazonTestApiKey;
END
ELSE
BEGIN
    PRINT '  - Amazon Test API key already exists';
    SELECT @AmazonTestApiKey = ApiKey FROM dbo.PartnerApiKeys WHERE PartnerId = @AmazonPartnerId AND Description LIKE '%Test%';
    PRINT '    API Key: ' + @AmazonTestApiKey;
END

-- Seed Wayfair Production API Key
DECLARE @WayfairApiKey NVARCHAR(512) = 'gcc_partner_wayfair_prod_' + REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', '');
IF NOT EXISTS (SELECT 1 FROM dbo.PartnerApiKeys WHERE PartnerId = @WayfairPartnerId AND Description LIKE '%Production%')
BEGIN
    INSERT INTO dbo.PartnerApiKeys (
        PartnerId,
        ApiKey,
        Description,
        IsActive,
        CreatedAt,
        CreatedBy,
        ExpiresAt,
        LastUsedAt,
        RequestCount,
        RateLimitPerHour,
        AllowedEndpoints,
        Permissions,
        Notes,
        RevokedAt,
        RevokedBy
    )
    VALUES (
        @WayfairPartnerId,
        @WayfairApiKey,
        'Wayfair Production API Key',
        1, -- IsActive
        GETUTCDATE(),
        'System Seed',
        DATEADD(YEAR, 1, GETUTCDATE()), -- Expires in 1 year
        NULL, -- LastUsedAt
        0, -- RequestCount
        5000, -- RateLimitPerHour (Wayfair has higher limit)
        '["/api/v1/certificates/*", "/api/v1/products/*"]', -- AllowedEndpoints
        '["read", "write"]', -- Permissions
        'Production API key for Wayfair partner integration',
        NULL, -- RevokedAt
        NULL  -- RevokedBy
    );
    PRINT '  ✓ Inserted PartnerApiKey: Wayfair Production';
    PRINT '    API Key: ' + @WayfairApiKey;
END
ELSE
BEGIN
    PRINT '  - Wayfair Production API key already exists';
    SELECT @WayfairApiKey = ApiKey FROM dbo.PartnerApiKeys WHERE PartnerId = @WayfairPartnerId AND Description LIKE '%Production%';
    PRINT '    API Key: ' + @WayfairApiKey;
END

-- Seed Wayfair Test API Key
DECLARE @WayfairTestApiKey NVARCHAR(512) = 'gcc_partner_wayfair_test_' + REPLACE(CONVERT(VARCHAR(36), NEWID()), '-', '');
IF NOT EXISTS (SELECT 1 FROM dbo.PartnerApiKeys WHERE PartnerId = @WayfairPartnerId AND Description LIKE '%Test%')
BEGIN
    INSERT INTO dbo.PartnerApiKeys (
        PartnerId,
        ApiKey,
        Description,
        IsActive,
        CreatedAt,
        CreatedBy,
        ExpiresAt,
        LastUsedAt,
        RequestCount,
        RateLimitPerHour,
        AllowedEndpoints,
        Permissions,
        Notes,
        RevokedAt,
        RevokedBy
    )
    VALUES (
        @WayfairPartnerId,
        @WayfairTestApiKey,
        'Wayfair Test API Key',
        1, -- IsActive
        GETUTCDATE(),
        'System Seed',
        DATEADD(YEAR, 1, GETUTCDATE()), -- Expires in 1 year
        NULL, -- LastUsedAt
        0, -- RequestCount
        5000, -- RateLimitPerHour
        '["/api/v1/certificates/*", "/api/v1/products/*"]', -- AllowedEndpoints
        '["read", "write"]', -- Permissions
        'Test API key for Wayfair partner integration',
        NULL, -- RevokedAt
        NULL  -- RevokedBy
    );
    PRINT '  ✓ Inserted PartnerApiKey: Wayfair Test';
    PRINT '    API Key: ' + @WayfairTestApiKey;
END
ELSE
BEGIN
    PRINT '  - Wayfair Test API key already exists';
    SELECT @WayfairTestApiKey = ApiKey FROM dbo.PartnerApiKeys WHERE PartnerId = @WayfairPartnerId AND Description LIKE '%Test%';
    PRINT '    API Key: ' + @WayfairTestApiKey;
END

PRINT '';
PRINT 'PartnerApiKeys table seeded successfully.';
PRINT '';

-- =============================================
-- STEP 3: Verification
-- =============================================
PRINT 'STEP 3: Verification...';
PRINT '';

-- Show Partners
PRINT 'Partners:';
SELECT
    Id,
    Name,
    Description,
    IsActive,
    CreatedAt
FROM dbo.Partners
ORDER BY SortOrder;

PRINT '';

-- Show PartnerApiKeys (truncated for security)
PRINT 'PartnerApiKeys:';
SELECT
    pak.Id,
    p.Name AS PartnerName,
    pak.Description,
    LEFT(pak.ApiKey, 30) + '...' AS ApiKey_Preview,
    pak.IsActive,
    pak.RateLimitPerHour,
    pak.ExpiresAt,
    pak.CreatedAt
FROM dbo.PartnerApiKeys pak
INNER JOIN dbo.Partners p ON pak.PartnerId = p.Id
ORDER BY p.Name, pak.Description;

PRINT '';
PRINT '=============================================';
PRINT 'Partner and PartnerApiKey seeding complete!';
PRINT '=============================================';
PRINT '';
PRINT 'IMPORTANT: Save the generated API keys for use in your applications.';
PRINT 'API keys are only shown once during generation.';
PRINT '';

SET NOCOUNT OFF;
GO
