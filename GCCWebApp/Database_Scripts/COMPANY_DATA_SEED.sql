-- ============================================
-- GCC Company Management Data Seeding Script
-- ============================================
-- This script seeds comprehensive company data for the Company Management page
-- For: https://gccwebapp.com/Company/Index
-- Database: SQL Server
-- Safe to run multiple times (idempotent)
-- ============================================

SET NOCOUNT ON;
GO

PRINT '============================================';
PRINT 'GCC Company Management Data Seeding';
PRINT 'Started at: ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '============================================';
PRINT '';

BEGIN TRANSACTION;

BEGIN TRY

-- ============================================
-- Update Existing Companies with Email & Website
-- ============================================
PRINT '1. Updating existing companies with email and website data...';

-- Soft-delete any fictional companies that may exist (they may have certificates)
UPDATE dbo.Companies
SET IsDeleted = 1, IsActive = 0, UpdatedAt = GETDATE()
WHERE Name IN (
    'Acme Corporation',
    'Globex Industries',
    'Stark Enterprises',
    'Wayne Technologies',
    'EcoFlex Manufacturing',
    'Green Products Inc. - Environmental Division'
);

DECLARE @DeletedCount INT = @@ROWCOUNT;
IF @DeletedCount > 0
    PRINT '✓ Soft-deleted ' + CAST(@DeletedCount AS VARCHAR) + ' fictional companies';
ELSE
    PRINT '○ No fictional companies found';

-- Update REAL companies with correct contact information
UPDATE dbo.Companies SET Email = 'sustainability@assaabloy.com', Website = 'https://www.assaabloy.com'
WHERE Name LIKE '%ASSA ABLOY%' AND (Email IS NULL OR Email = '');

UPDATE dbo.Companies SET Email = 'sustainability@gaf.com', Website = 'https://www.gaf.com'
WHERE Name = 'GAF Materials Corporation' AND (Email IS NULL OR Email = '');

UPDATE dbo.Companies SET Email = 'sustainability@3m.com', Website = 'https://www.3m.com'
WHERE Name = '3M Company' AND (Email IS NULL OR Email = '');

UPDATE dbo.Companies SET Email = 'sustainability@shawinc.com', Website = 'https://www.shawinc.com'
WHERE Name = 'Shaw Industries Group' AND (Email IS NULL OR Email = '');

UPDATE dbo.Companies SET Email = 'sustainability@mohawkind.com', Website = 'https://www.mohawkind.com'
WHERE Name = 'Mohawk Industries' AND (Email IS NULL OR Email = '');

UPDATE dbo.Companies SET Email = 'sustainability@armstrongceilings.com', Website = 'https://www.armstrongceilings.com'
WHERE Name = 'Armstrong World Industries' AND (Email IS NULL OR Email = '');

UPDATE dbo.Companies SET Email = 'sustainability@owenscorning.com', Website = 'https://www.owenscorning.com'
WHERE Name = 'Owens Corning' AND (Email IS NULL OR Email = '');

UPDATE dbo.Companies SET Email = 'sustainability@certainteed.com', Website = 'https://www.certainteed.com'
WHERE Name = 'CertainTeed Corporation' AND (Email IS NULL OR Email = '');

UPDATE dbo.Companies SET Email = 'sustainability@jm.com', Website = 'https://www.jm.com'
WHERE Name = 'Johns Manville' AND (Email IS NULL OR Email = '');

UPDATE dbo.Companies SET Email = 'sustainability@kingspan.com', Website = 'https://www.kingspan.com'
WHERE Name = 'Kingspan Insulation' AND (Email IS NULL OR Email = '');

UPDATE dbo.Companies SET Email = 'sustainability@hunterdouglas.com', Website = 'https://www.hunterdouglas.com'
WHERE Name = 'Hunter Douglas' AND (Email IS NULL OR Email = '');

UPDATE dbo.Companies SET Email = 'sustainability@steelcase.com', Website = 'https://www.steelcase.com'
WHERE Name = 'Steelcase Inc.' AND (Email IS NULL OR Email = '');

UPDATE dbo.Companies SET Email = 'sustainability@hermanmiller.com', Website = 'https://www.hermanmiller.com'
WHERE Name = 'Herman Miller Inc.' AND (Email IS NULL OR Email = '');

UPDATE dbo.Companies SET Email = 'sustainability@knoll.com', Website = 'https://www.knoll.com'
WHERE Name = 'Knoll Inc.' AND (Email IS NULL OR Email = '');

UPDATE dbo.Companies SET Email = 'sustainability@teknion.com', Website = 'https://www.teknion.com'
WHERE Name = 'Teknion Corporation' AND (Email IS NULL OR Email = '');

UPDATE dbo.Companies SET Email = 'sustainability@forbo.com', Website = 'https://www.forbo.com'
WHERE Name = 'Forbo Flooring Systems' AND (Email IS NULL OR Email = '');

UPDATE dbo.Companies SET Email = 'sustainability@tarkett.com', Website = 'https://www.tarkett.com'
WHERE Name = 'Tarkett USA Inc.' AND (Email IS NULL OR Email = '');

UPDATE dbo.Companies SET Email = 'sustainability@milliken.com', Website = 'https://www.milliken.com'
WHERE Name = 'Milliken & Company' AND (Email IS NULL OR Email = '');

UPDATE dbo.Companies SET Email = 'sustainability@usg.com', Website = 'https://www.usg.com'
WHERE Name = 'USG Corporation' AND (Email IS NULL OR Email = '');

UPDATE dbo.Companies SET Email = 'sustainability@lutron.com', Website = 'https://www.lutron.com'
WHERE Name = 'Lutron Electronics' AND (Email IS NULL OR Email = '');

UPDATE dbo.Companies SET Email = 'sustainability@signify.com', Website = 'https://www.signify.com'
WHERE Name = 'Philips Lighting' AND (Email IS NULL OR Email = '');

UPDATE dbo.Companies SET Email = 'sustainability@creelighting.com', Website = 'https://www.creelighting.com'
WHERE Name = 'Cree Lighting' AND (Email IS NULL OR Email = '');

PRINT '✓ Existing companies updated with email and website information';
PRINT '';

-- ============================================
-- Add New Companies for Testing
-- ============================================
PRINT '2. Adding additional companies for Company Management testing...';

DECLARE @NewCompaniesAdded INT = 0;

-- Add companies if they don't exist (let SQL Server auto-generate IDs)
IF NOT EXISTS (SELECT 1 FROM dbo.Companies WHERE Name = 'Interface Inc.')
BEGIN
    INSERT INTO dbo.Companies (Name, ContactInfo, Email, Website, Location, Country, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES ('Interface Inc.', 'sustainability@interface.com', 'sustainability@interface.com',
            'https://www.interface.com', 'Atlanta, GA', 'United States', 1, 0, GETDATE(), GETDATE());
    SET @NewCompaniesAdded = @NewCompaniesAdded + 1;
END

IF NOT EXISTS (SELECT 1 FROM dbo.Companies WHERE Name = 'Patagonia Inc.')
BEGIN
    INSERT INTO dbo.Companies (Name, ContactInfo, Email, Website, Location, Country, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES ('Patagonia Inc.', 'environmental@patagonia.com', 'environmental@patagonia.com',
            'https://www.patagonia.com', 'Ventura, CA', 'United States', 1, 0, GETDATE(), GETDATE());
    SET @NewCompaniesAdded = @NewCompaniesAdded + 1;
END

IF NOT EXISTS (SELECT 1 FROM dbo.Companies WHERE Name = 'Seventh Generation Inc.')
BEGIN
    INSERT INTO dbo.Companies (Name, ContactInfo, Email, Website, Location, Country, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES ('Seventh Generation Inc.', 'info@seventhgeneration.com', 'info@seventhgeneration.com',
            'https://www.seventhgeneration.com', 'Burlington, VT', 'United States', 1, 0, GETDATE(), GETDATE());
    SET @NewCompaniesAdded = @NewCompaniesAdded + 1;
END

IF NOT EXISTS (SELECT 1 FROM dbo.Companies WHERE Name = 'Method Products PBC')
BEGIN
    INSERT INTO dbo.Companies (Name, ContactInfo, Email, Website, Location, Country, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES ('Method Products PBC', 'hello@methodhome.com', 'hello@methodhome.com',
            'https://www.methodhome.com', 'San Francisco, CA', 'United States', 1, 0, GETDATE(), GETDATE());
    SET @NewCompaniesAdded = @NewCompaniesAdded + 1;
END

IF NOT EXISTS (SELECT 1 FROM dbo.Companies WHERE Name = 'BASF Corporation')
BEGIN
    INSERT INTO dbo.Companies (Name, ContactInfo, Email, Website, Location, Country, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES ('BASF Corporation', 'sustainability@basf.com', 'sustainability@basf.com',
            'https://www.basf.com', 'Florham Park, NJ', 'United States', 1, 0, GETDATE(), GETDATE());
    SET @NewCompaniesAdded = @NewCompaniesAdded + 1;
END

IF NOT EXISTS (SELECT 1 FROM dbo.Companies WHERE Name = 'DuPont de Nemours Inc.')
BEGIN
    INSERT INTO dbo.Companies (Name, ContactInfo, Email, Website, Location, Country, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES ('DuPont de Nemours Inc.', 'environmental@dupont.com', 'environmental@dupont.com',
            'https://www.dupont.com', 'Wilmington, DE', 'United States', 1, 0, GETDATE(), GETDATE());
    SET @NewCompaniesAdded = @NewCompaniesAdded + 1;
END

IF NOT EXISTS (SELECT 1 FROM dbo.Companies WHERE Name = 'Unilever North America')
BEGIN
    INSERT INTO dbo.Companies (Name, ContactInfo, Email, Website, Location, Country, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES ( 'Unilever North America', 'sustainability.na@unilever.com', 'sustainability.na@unilever.com',
            'https://www.unileverusa.com', 'Englewood Cliffs, NJ', 'United States', 1, 0, GETDATE(), GETDATE());
    SET @NewCompaniesAdded = @NewCompaniesAdded + 1;
END

IF NOT EXISTS (SELECT 1 FROM dbo.Companies WHERE Name = 'Johnson & Johnson')
BEGIN
    INSERT INTO dbo.Companies (Name, ContactInfo, Email, Website, Location, Country, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES ( 'Johnson & Johnson', 'sustainability@jnj.com', 'sustainability@jnj.com',
            'https://www.jnj.com', 'New Brunswick, NJ', 'United States', 1, 0, GETDATE(), GETDATE());
    SET @NewCompaniesAdded = @NewCompaniesAdded + 1;
END

IF NOT EXISTS (SELECT 1 FROM dbo.Companies WHERE Name = 'Procter & Gamble')
BEGIN
    INSERT INTO dbo.Companies (Name, ContactInfo, Email, Website, Location, Country, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES ( 'Procter & Gamble', 'environmental@pg.com', 'environmental@pg.com',
            'https://www.pg.com', 'Cincinnati, OH', 'United States', 1, 0, GETDATE(), GETDATE());
    SET @NewCompaniesAdded = @NewCompaniesAdded + 1;
END

IF NOT EXISTS (SELECT 1 FROM dbo.Companies WHERE Name = 'Nike Inc.')
BEGIN
    INSERT INTO dbo.Companies (Name, ContactInfo, Email, Website, Location, Country, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES ( 'Nike Inc.', 'sustainability@nike.com', 'sustainability@nike.com',
            'https://www.nike.com', 'Beaverton, OR', 'United States', 1, 0, GETDATE(), GETDATE());
    SET @NewCompaniesAdded = @NewCompaniesAdded + 1;
END

IF NOT EXISTS (SELECT 1 FROM dbo.Companies WHERE Name = 'IKEA North America')
BEGIN
    INSERT INTO dbo.Companies (Name, ContactInfo, Email, Website, Location, Country, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES ( 'IKEA North America', 'sustainability@ikea.com', 'sustainability@ikea.com',
            'https://www.ikea.com', 'Conshohocken, PA', 'United States', 1, 0, GETDATE(), GETDATE());
    SET @NewCompaniesAdded = @NewCompaniesAdded + 1;
END

IF NOT EXISTS (SELECT 1 FROM dbo.Companies WHERE Name = 'Tesla Inc.')
BEGIN
    INSERT INTO dbo.Companies (Name, ContactInfo, Email, Website, Location, Country, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES ( 'Tesla Inc.', 'environmental@tesla.com', 'environmental@tesla.com',
            'https://www.tesla.com', 'Austin, TX', 'United States', 1, 0, GETDATE(), GETDATE());
    SET @NewCompaniesAdded = @NewCompaniesAdded + 1;
END

IF NOT EXISTS (SELECT 1 FROM dbo.Companies WHERE Name = 'Apple Inc.')
BEGIN
    INSERT INTO dbo.Companies (Name, ContactInfo, Email, Website, Location, Country, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES ( 'Apple Inc.', 'environment@apple.com', 'environment@apple.com',
            'https://www.apple.com', 'Cupertino, CA', 'United States', 1, 0, GETDATE(), GETDATE());
    SET @NewCompaniesAdded = @NewCompaniesAdded + 1;
END

IF NOT EXISTS (SELECT 1 FROM dbo.Companies WHERE Name = 'Google LLC')
BEGIN
    INSERT INTO dbo.Companies (Name, ContactInfo, Email, Website, Location, Country, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES ( 'Google LLC', 'sustainability@google.com', 'sustainability@google.com',
            'https://www.google.com', 'Mountain View, CA', 'United States', 1, 0, GETDATE(), GETDATE());
    SET @NewCompaniesAdded = @NewCompaniesAdded + 1;
END

IF NOT EXISTS (SELECT 1 FROM dbo.Companies WHERE Name = 'Microsoft Corporation')
BEGIN
    INSERT INTO dbo.Companies (Name, ContactInfo, Email, Website, Location, Country, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES ( 'Microsoft Corporation', 'environment@microsoft.com', 'environment@microsoft.com',
            'https://www.microsoft.com', 'Redmond, WA', 'United States', 1, 0, GETDATE(), GETDATE());
    SET @NewCompaniesAdded = @NewCompaniesAdded + 1;
END

IF NOT EXISTS (SELECT 1 FROM dbo.Companies WHERE Name = 'Dell Technologies')
BEGIN
    INSERT INTO dbo.Companies (Name, ContactInfo, Email, Website, Location, Country, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES ( 'Dell Technologies', 'sustainability@dell.com', 'sustainability@dell.com',
            'https://www.dell.com', 'Round Rock, TX', 'United States', 1, 0, GETDATE(), GETDATE());
    SET @NewCompaniesAdded = @NewCompaniesAdded + 1;
END

IF NOT EXISTS (SELECT 1 FROM dbo.Companies WHERE Name = 'HP Inc.')
BEGIN
    INSERT INTO dbo.Companies (Name, ContactInfo, Email, Website, Location, Country, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES ( 'HP Inc.', 'environmental@hp.com', 'environmental@hp.com',
            'https://www.hp.com', 'Palo Alto, CA', 'United States', 1, 0, GETDATE(), GETDATE());
    SET @NewCompaniesAdded = @NewCompaniesAdded + 1;
END

IF NOT EXISTS (SELECT 1 FROM dbo.Companies WHERE Name = 'Cisco Systems Inc.')
BEGIN
    INSERT INTO dbo.Companies (Name, ContactInfo, Email, Website, Location, Country, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES ( 'Cisco Systems Inc.', 'sustainability@cisco.com', 'sustainability@cisco.com',
            'https://www.cisco.com', 'San Jose, CA', 'United States', 1, 0, GETDATE(), GETDATE());
    SET @NewCompaniesAdded = @NewCompaniesAdded + 1;
END

IF NOT EXISTS (SELECT 1 FROM dbo.Companies WHERE Name = 'Intel Corporation')
BEGIN
    INSERT INTO dbo.Companies (Name, ContactInfo, Email, Website, Location, Country, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES ( 'Intel Corporation', 'environmental@intel.com', 'environmental@intel.com',
            'https://www.intel.com', 'Santa Clara, CA', 'United States', 1, 0, GETDATE(), GETDATE());
    SET @NewCompaniesAdded = @NewCompaniesAdded + 1;
END

IF NOT EXISTS (SELECT 1 FROM dbo.Companies WHERE Name = 'AMD Inc.')
BEGIN
    INSERT INTO dbo.Companies (Name, ContactInfo, Email, Website, Location, Country, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES ( 'AMD Inc.', 'sustainability@amd.com', 'sustainability@amd.com',
            'https://www.amd.com', 'Santa Clara, CA', 'United States', 1, 0, GETDATE(), GETDATE());
    SET @NewCompaniesAdded = @NewCompaniesAdded + 1;
END

IF NOT EXISTS (SELECT 1 FROM dbo.Companies WHERE Name = 'Starbucks Corporation')
BEGIN
    INSERT INTO dbo.Companies (Name, ContactInfo, Email, Website, Location, Country, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES ( 'Starbucks Corporation', 'environmental@starbucks.com', 'environmental@starbucks.com',
            'https://www.starbucks.com', 'Seattle, WA', 'United States', 1, 0, GETDATE(), GETDATE());
    SET @NewCompaniesAdded = @NewCompaniesAdded + 1;
END

IF NOT EXISTS (SELECT 1 FROM dbo.Companies WHERE Name = 'Whole Foods Market')
BEGIN
    INSERT INTO dbo.Companies (Name, ContactInfo, Email, Website, Location, Country, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES ( 'Whole Foods Market', 'sustainability@wholefoods.com', 'sustainability@wholefoods.com',
            'https://www.wholefoodsmarket.com', 'Austin, TX', 'United States', 1, 0, GETDATE(), GETDATE());
    SET @NewCompaniesAdded = @NewCompaniesAdded + 1;
END

IF NOT EXISTS (SELECT 1 FROM dbo.Companies WHERE Name = 'Target Corporation')
BEGIN
    INSERT INTO dbo.Companies (Name, ContactInfo, Email, Website, Location, Country, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES ( 'Target Corporation', 'sustainability@target.com', 'sustainability@target.com',
            'https://www.target.com', 'Minneapolis, MN', 'United States', 1, 0, GETDATE(), GETDATE());
    SET @NewCompaniesAdded = @NewCompaniesAdded + 1;
END

IF NOT EXISTS (SELECT 1 FROM dbo.Companies WHERE Name = 'Walmart Inc.')
BEGIN
    INSERT INTO dbo.Companies (Name, ContactInfo, Email, Website, Location, Country, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES ( 'Walmart Inc.', 'sustainability@walmart.com', 'sustainability@walmart.com',
            'https://www.walmart.com', 'Bentonville, AR', 'United States', 1, 0, GETDATE(), GETDATE());
    SET @NewCompaniesAdded = @NewCompaniesAdded + 1;
END

IF NOT EXISTS (SELECT 1 FROM dbo.Companies WHERE Name = 'The Home Depot')
BEGIN
    INSERT INTO dbo.Companies (Name, ContactInfo, Email, Website, Location, Country, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES ( 'The Home Depot', 'environmental@homedepot.com', 'environmental@homedepot.com',
            'https://www.homedepot.com', 'Atlanta, GA', 'United States', 1, 0, GETDATE(), GETDATE());
    SET @NewCompaniesAdded = @NewCompaniesAdded + 1;
END

IF NOT EXISTS (SELECT 1 FROM dbo.Companies WHERE Name = 'Lowe''s Companies Inc.')
BEGIN
    INSERT INTO dbo.Companies (Name, ContactInfo, Email, Website, Location, Country, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES ( 'Lowe''s Companies Inc.', 'sustainability@lowes.com', 'sustainability@lowes.com',
            'https://www.lowes.com', 'Mooresville, NC', 'United States', 1, 0, GETDATE(), GETDATE());
    SET @NewCompaniesAdded = @NewCompaniesAdded + 1;
END

IF NOT EXISTS (SELECT 1 FROM dbo.Companies WHERE Name = 'Costco Wholesale Corporation')
BEGIN
    INSERT INTO dbo.Companies (Name, ContactInfo, Email, Website, Location, Country, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES ( 'Costco Wholesale Corporation', 'environmental@costco.com', 'environmental@costco.com',
            'https://www.costco.com', 'Issaquah, WA', 'United States', 1, 0, GETDATE(), GETDATE());
    SET @NewCompaniesAdded = @NewCompaniesAdded + 1;
END

IF NOT EXISTS (SELECT 1 FROM dbo.Companies WHERE Name = 'Best Buy Co. Inc.')
BEGIN
    INSERT INTO dbo.Companies (Name, ContactInfo, Email, Website, Location, Country, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES ( 'Best Buy Co. Inc.', 'sustainability@bestbuy.com', 'sustainability@bestbuy.com',
            'https://www.bestbuy.com', 'Richfield, MN', 'United States', 1, 0, GETDATE(), GETDATE());
    SET @NewCompaniesAdded = @NewCompaniesAdded + 1;
END

IF NOT EXISTS (SELECT 1 FROM dbo.Companies WHERE Name = 'Wayfair LLC')
BEGIN
    INSERT INTO dbo.Companies (Name, ContactInfo, Email, Website, Location, Country, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES ( 'Wayfair LLC', 'sustainability@wayfair.com', 'sustainability@wayfair.com',
            'https://www.wayfair.com', 'Boston, MA', 'United States', 1, 0, GETDATE(), GETDATE());
    SET @NewCompaniesAdded = @NewCompaniesAdded + 1;
END

IF NOT EXISTS (SELECT 1 FROM dbo.Companies WHERE Name = 'Amazon.com Inc.')
BEGIN
    INSERT INTO dbo.Companies (Name, ContactInfo, Email, Website, Location, Country, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES ( 'Amazon.com Inc.', 'sustainability@amazon.com', 'sustainability@amazon.com',
            'https://www.amazon.com', 'Seattle, WA', 'United States', 1, 0, GETDATE(), GETDATE());
    SET @NewCompaniesAdded = @NewCompaniesAdded + 1;
END

-- Building Materials Companies
IF NOT EXISTS (SELECT 1 FROM dbo.Companies WHERE Name = 'Pella Corporation')
BEGIN
    INSERT INTO dbo.Companies (Name, ContactInfo, Email, Website, Location, Country, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES ('Pella Corporation', 'sustainability@pella.com', 'sustainability@pella.com',
            'https://www.pella.com', 'Pella, IA', 'United States', 1, 0, GETDATE(), GETDATE());
    SET @NewCompaniesAdded = @NewCompaniesAdded + 1;
END

IF NOT EXISTS (SELECT 1 FROM dbo.Companies WHERE Name = 'Andersen Corporation')
BEGIN
    INSERT INTO dbo.Companies (Name, ContactInfo, Email, Website, Location, Country, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES ('Andersen Corporation', 'sustainability@andersencorp.com', 'sustainability@andersencorp.com',
            'https://www.andersenwindows.com', 'Bayport, MN', 'United States', 1, 0, GETDATE(), GETDATE());
    SET @NewCompaniesAdded = @NewCompaniesAdded + 1;
END

IF NOT EXISTS (SELECT 1 FROM dbo.Companies WHERE Name = 'Marvin Windows and Doors')
BEGIN
    INSERT INTO dbo.Companies (Name, ContactInfo, Email, Website, Location, Country, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES ('Marvin Windows and Doors', 'info@marvin.com', 'info@marvin.com',
            'https://www.marvin.com', 'Warroad, MN', 'United States', 1, 0, GETDATE(), GETDATE());
    SET @NewCompaniesAdded = @NewCompaniesAdded + 1;
END

IF NOT EXISTS (SELECT 1 FROM dbo.Companies WHERE Name = 'JELD-WEN Inc.')
BEGIN
    INSERT INTO dbo.Companies (Name, ContactInfo, Email, Website, Location, Country, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES ('JELD-WEN Inc.', 'sustainability@jeldwen.com', 'sustainability@jeldwen.com',
            'https://www.jeld-wen.com', 'Charlotte, NC', 'United States', 1, 0, GETDATE(), GETDATE());
    SET @NewCompaniesAdded = @NewCompaniesAdded + 1;
END

IF NOT EXISTS (SELECT 1 FROM dbo.Companies WHERE Name = 'James Hardie Building Products')
BEGIN
    INSERT INTO dbo.Companies (Name, ContactInfo, Email, Website, Location, Country, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES ('James Hardie Building Products', 'sustainability@jameshardie.com', 'sustainability@jameshardie.com',
            'https://www.jameshardie.com', 'Chicago, IL', 'United States', 1, 0, GETDATE(), GETDATE());
    SET @NewCompaniesAdded = @NewCompaniesAdded + 1;
END

IF NOT EXISTS (SELECT 1 FROM dbo.Companies WHERE Name = 'Louisiana-Pacific Corporation')
BEGIN
    INSERT INTO dbo.Companies (Name, ContactInfo, Email, Website, Location, Country, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES ('Louisiana-Pacific Corporation', 'sustainability@lpcorp.com', 'sustainability@lpcorp.com',
            'https://www.lpcorp.com', 'Nashville, TN', 'United States', 1, 0, GETDATE(), GETDATE());
    SET @NewCompaniesAdded = @NewCompaniesAdded + 1;
END

IF NOT EXISTS (SELECT 1 FROM dbo.Companies WHERE Name = 'Masco Corporation')
BEGIN
    INSERT INTO dbo.Companies (Name, ContactInfo, Email, Website, Location, Country, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES ('Masco Corporation', 'sustainability@masco.com', 'sustainability@masco.com',
            'https://www.masco.com', 'Livonia, MI', 'United States', 1, 0, GETDATE(), GETDATE());
    SET @NewCompaniesAdded = @NewCompaniesAdded + 1;
END

IF NOT EXISTS (SELECT 1 FROM dbo.Companies WHERE Name = 'Kohler Co.')
BEGIN
    INSERT INTO dbo.Companies (Name, ContactInfo, Email, Website, Location, Country, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES ('Kohler Co.', 'sustainability@kohler.com', 'sustainability@kohler.com',
            'https://www.kohler.com', 'Kohler, WI', 'United States', 1, 0, GETDATE(), GETDATE());
    SET @NewCompaniesAdded = @NewCompaniesAdded + 1;
END

IF NOT EXISTS (SELECT 1 FROM dbo.Companies WHERE Name = 'Sherwin-Williams Company')
BEGIN
    INSERT INTO dbo.Companies (Name, ContactInfo, Email, Website, Location, Country, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES ('Sherwin-Williams Company', 'sustainability@sherwin.com', 'sustainability@sherwin.com',
            'https://www.sherwin-williams.com', 'Cleveland, OH', 'United States', 1, 0, GETDATE(), GETDATE());
    SET @NewCompaniesAdded = @NewCompaniesAdded + 1;
END

IF NOT EXISTS (SELECT 1 FROM dbo.Companies WHERE Name = 'PPG Industries Inc.')
BEGIN
    INSERT INTO dbo.Companies (Name, ContactInfo, Email, Website, Location, Country, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES ('PPG Industries Inc.', 'sustainability@ppg.com', 'sustainability@ppg.com',
            'https://www.ppg.com', 'Pittsburgh, PA', 'United States', 1, 0, GETDATE(), GETDATE());
    SET @NewCompaniesAdded = @NewCompaniesAdded + 1;
END

IF NOT EXISTS (SELECT 1 FROM dbo.Companies WHERE Name = 'Saint-Gobain North America')
BEGIN
    INSERT INTO dbo.Companies (Name, ContactInfo, Email, Website, Location, Country, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES ('Saint-Gobain North America', 'sustainability@saint-gobain.com', 'sustainability@saint-gobain.com',
            'https://www.saint-gobain.com', 'Malvern, PA', 'United States', 1, 0, GETDATE(), GETDATE());
    SET @NewCompaniesAdded = @NewCompaniesAdded + 1;
END

IF NOT EXISTS (SELECT 1 FROM dbo.Companies WHERE Name = 'Boral North America')
BEGIN
    INSERT INTO dbo.Companies (Name, ContactInfo, Email, Website, Location, Country, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES ('Boral North America', 'sustainability@boral.com', 'sustainability@boral.com',
            'https://www.boral.com', 'San Jose, CA', 'United States', 1, 0, GETDATE(), GETDATE());
    SET @NewCompaniesAdded = @NewCompaniesAdded + 1;
END

IF NOT EXISTS (SELECT 1 FROM dbo.Companies WHERE Name = 'Whirlpool Corporation')
BEGIN
    INSERT INTO dbo.Companies (Name, ContactInfo, Email, Website, Location, Country, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES ('Whirlpool Corporation', 'sustainability@whirlpool.com', 'sustainability@whirlpool.com',
            'https://www.whirlpool.com', 'Benton Harbor, MI', 'United States', 1, 0, GETDATE(), GETDATE());
    SET @NewCompaniesAdded = @NewCompaniesAdded + 1;
END

IF NOT EXISTS (SELECT 1 FROM dbo.Companies WHERE Name = 'General Electric Appliances')
BEGIN
    INSERT INTO dbo.Companies (Name, ContactInfo, Email, Website, Location, Country, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES ('General Electric Appliances', 'sustainability@geappliances.com', 'sustainability@geappliances.com',
            'https://www.geappliances.com', 'Louisville, KY', 'United States', 1, 0, GETDATE(), GETDATE());
    SET @NewCompaniesAdded = @NewCompaniesAdded + 1;
END

IF NOT EXISTS (SELECT 1 FROM dbo.Companies WHERE Name = 'Electrolux North America')
BEGIN
    INSERT INTO dbo.Companies (Name, ContactInfo, Email, Website, Location, Country, IsActive, IsDeleted, CreatedAt, UpdatedAt)
    VALUES ('Electrolux North America', 'sustainability@electrolux.com', 'sustainability@electrolux.com',
            'https://www.electrolux.com', 'Charlotte, NC', 'United States', 1, 0, GETDATE(), GETDATE());
    SET @NewCompaniesAdded = @NewCompaniesAdded + 1;
END

PRINT '✓ ' + CAST(@NewCompaniesAdded AS VARCHAR) + ' new companies added';
PRINT '';

-- Commit transaction
COMMIT TRANSACTION;

PRINT '';
PRINT '============================================';
PRINT 'COMPANY DATA SEEDING COMPLETE';
PRINT '============================================';

-- Generate statistics
PRINT '';
PRINT 'Company Statistics:';
SELECT
    COUNT(*) AS TotalCompanies,
    SUM(CASE WHEN Email IS NOT NULL AND Email != '' THEN 1 ELSE 0 END) AS CompaniesWithEmail,
    SUM(CASE WHEN Website IS NOT NULL AND Website != '' THEN 1 ELSE 0 END) AS CompaniesWithWebsite,
    SUM(CASE WHEN IsActive = 1 THEN 1 ELSE 0 END) AS ActiveCompanies,
    SUM(CASE WHEN IsDeleted = 1 THEN 1 ELSE 0 END) AS DeletedCompanies
FROM dbo.Companies;

PRINT '';
PRINT 'Sample Companies:';
SELECT TOP 10
    Name,
    Email,
    Website,
    Location,
    Country
FROM dbo.Companies
WHERE IsActive = 1 AND IsDeleted = 0
ORDER BY Name;

PRINT '';
PRINT 'Completed at: ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '============================================';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    PRINT '';
    PRINT '============================================';
    PRINT 'ERROR OCCURRED DURING SEEDING';
    PRINT '============================================';
    PRINT 'Error Message: ' + ERROR_MESSAGE();
    PRINT 'Error Line: ' + CAST(ERROR_LINE() AS VARCHAR);
    PRINT 'Error Severity: ' + CAST(ERROR_SEVERITY() AS VARCHAR);
    PRINT 'Error State: ' + CAST(ERROR_STATE() AS VARCHAR);
    PRINT '============================================';

    -- Re-throw the error
    THROW;
END CATCH;

GO
