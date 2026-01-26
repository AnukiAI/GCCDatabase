-- Migration: 14_20260122_AddMultiPartnerSupport
-- Description: Adds multi-partner support for certificates and PartnerId to Companies
-- Author: Claude Code
-- Date: 2026-01-22

-- =====================================================
-- PHASE 1A: Add PartnerId to Companies table (for reporting)
-- =====================================================

-- Check if column exists before adding
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'Companies' AND COLUMN_NAME = 'PartnerId'
)
BEGIN
    ALTER TABLE Companies
    ADD PartnerId INT NULL;

    -- Add foreign key constraint
    ALTER TABLE Companies
    ADD CONSTRAINT FK_Companies_Partners
    FOREIGN KEY (PartnerId) REFERENCES Partners(Id)
    ON DELETE SET NULL;

    -- Create index for reporting queries
    CREATE INDEX IX_Companies_PartnerId ON Companies(PartnerId);

    PRINT 'Added PartnerId column to Companies table';
END
ELSE
BEGIN
    PRINT 'PartnerId column already exists in Companies table';
END
GO

-- =====================================================
-- PHASE 1B: Create CertificatePartners join table
-- =====================================================

-- Create the many-to-many join table for Certificate-Partner
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'CertificatePartners')
BEGIN
    CREATE TABLE CertificatePartners (
        CertificatesId INT NOT NULL,
        PartnersId INT NOT NULL,
        CONSTRAINT PK_CertificatePartners PRIMARY KEY (CertificatesId, PartnersId),
        CONSTRAINT FK_CertificatePartners_Certificates FOREIGN KEY (CertificatesId)
            REFERENCES Certificates(Id) ON DELETE CASCADE,
        CONSTRAINT FK_CertificatePartners_Partners FOREIGN KEY (PartnersId)
            REFERENCES Partners(Id) ON DELETE CASCADE
    );

    -- Create indexes for performance
    CREATE INDEX IX_CertificatePartners_CertificatesId ON CertificatePartners(CertificatesId);
    CREATE INDEX IX_CertificatePartners_PartnersId ON CertificatePartners(PartnersId);

    PRINT 'Created CertificatePartners join table';
END
ELSE
BEGIN
    PRINT 'CertificatePartners table already exists';
END
GO

-- =====================================================
-- PHASE 1C: Migrate existing Certificate.Partner data
-- =====================================================

-- Migrate existing Partner string values to CertificatePartners
-- Only run if there's data to migrate and the Partner column still has data
IF EXISTS (
    SELECT 1 FROM Certificates
    WHERE Partner IS NOT NULL AND Partner != 'None' AND Partner != ''
)
BEGIN
    PRINT 'Migrating existing Partner data to CertificatePartners...';

    -- Insert Amazon partner associations
    INSERT INTO CertificatePartners (CertificatesId, PartnersId)
    SELECT c.Id, p.Id
    FROM Certificates c
    CROSS JOIN Partners p
    WHERE c.Partner = 'Amazon' AND p.Name = 'Amazon'
    AND NOT EXISTS (
        SELECT 1 FROM CertificatePartners cp
        WHERE cp.CertificatesId = c.Id AND cp.PartnersId = p.Id
    );

    PRINT 'Migrated Amazon partner associations: ' + CAST(@@ROWCOUNT AS VARCHAR);

    -- Insert Novi partner associations
    INSERT INTO CertificatePartners (CertificatesId, PartnersId)
    SELECT c.Id, p.Id
    FROM Certificates c
    CROSS JOIN Partners p
    WHERE c.Partner = 'Novi' AND p.Name = 'Novi'
    AND NOT EXISTS (
        SELECT 1 FROM CertificatePartners cp
        WHERE cp.CertificatesId = c.Id AND cp.PartnersId = p.Id
    );

    PRINT 'Migrated Novi partner associations: ' + CAST(@@ROWCOUNT AS VARCHAR);

    -- Insert Wayfair partner associations (legacy data)
    INSERT INTO CertificatePartners (CertificatesId, PartnersId)
    SELECT c.Id, p.Id
    FROM Certificates c
    CROSS JOIN Partners p
    WHERE c.Partner = 'Wayfair' AND p.Name = 'Novi'  -- Wayfair was renamed to Novi
    AND NOT EXISTS (
        SELECT 1 FROM CertificatePartners cp
        WHERE cp.CertificatesId = c.Id AND cp.PartnersId = p.Id
    );

    PRINT 'Migrated Wayfair->Novi partner associations: ' + CAST(@@ROWCOUNT AS VARCHAR);

    PRINT 'Partner data migration complete';
END
ELSE
BEGIN
    PRINT 'No Partner data to migrate (all values are NULL or None)';
END
GO

-- =====================================================
-- VERIFICATION QUERIES
-- =====================================================

-- Verify CertificatePartners table structure
SELECT
    'CertificatePartners Table Created' AS Status,
    COUNT(*) AS RecordCount
FROM CertificatePartners;

-- Verify PartnerId on Companies
SELECT
    'Companies.PartnerId Column Added' AS Status,
    COUNT(*) AS CompaniesWithPartner
FROM Companies
WHERE PartnerId IS NOT NULL;

-- Summary of certificate-partner associations
SELECT
    p.Name AS PartnerName,
    COUNT(cp.CertificatesId) AS CertificateCount
FROM Partners p
LEFT JOIN CertificatePartners cp ON p.Id = cp.PartnersId
WHERE p.IsDeleted = 0
GROUP BY p.Id, p.Name
ORDER BY p.Name;

PRINT 'Migration 14_20260122_AddMultiPartnerSupport completed successfully';
GO
