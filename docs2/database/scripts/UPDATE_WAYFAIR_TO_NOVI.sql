-- ============================================================================
-- UPDATE WAYFAIR TO NOVI
-- Run this script to rename "Wayfair" partner to "Novi" in all relevant tables
-- ============================================================================

PRINT '============================================================================';
PRINT 'UPDATING WAYFAIR TO NOVI';
PRINT '============================================================================';

-- Step 1: Update Partners table
PRINT '';
PRINT 'Step 1: Updating Partners table...';

UPDATE dbo.Partners
SET
    Name = 'Novi',
    Description = 'Novi Connect - Clean beauty and ingredient verification platform',
    Website = 'https://noviconnect.com',
    Email = 'support@noviconnect.com',
    ContactInfo = 'Novi Clean Beauty Standards',
    UpdatedAt = GETDATE()
WHERE Name = 'Wayfair';

IF @@ROWCOUNT > 0
    PRINT '  ✓ Updated Partner: Wayfair -> Novi';
ELSE
    PRINT '  - No Wayfair partner found to update';

-- Step 2: Update PartnerApiKeys descriptions if any reference Wayfair
PRINT '';
PRINT 'Step 2: Updating PartnerApiKeys descriptions...';

UPDATE dbo.PartnerApiKeys
SET
    Description = REPLACE(Description, 'Wayfair', 'Novi'),
    Notes = REPLACE(ISNULL(Notes, ''), 'Wayfair', 'Novi')
WHERE Description LIKE '%Wayfair%' OR Notes LIKE '%Wayfair%';

PRINT '  ✓ Updated ' + CAST(@@ROWCOUNT AS VARCHAR) + ' PartnerApiKey records';

-- Step 3: Update any Certificates that reference Wayfair partner
PRINT '';
PRINT 'Step 3: Updating Certificates with Wayfair partner...';

UPDATE dbo.Certificates
SET
    Partner = 'Novi',
    UpdatedAt = GETDATE()
WHERE Partner = 'Wayfair';

PRINT '  ✓ Updated ' + CAST(@@ROWCOUNT AS VARCHAR) + ' Certificate records';

-- Verification
PRINT '';
PRINT '============================================================================';
PRINT 'VERIFICATION';
PRINT '============================================================================';

PRINT '';
PRINT 'Partners in database:';
SELECT Id, Name, Description, Website FROM dbo.Partners ORDER BY SortOrder;

PRINT '';
PRINT 'Partner API Keys by Partner:';
SELECT p.Name AS PartnerName, COUNT(k.Id) AS ApiKeyCount
FROM dbo.Partners p
LEFT JOIN dbo.PartnerApiKeys k ON p.Id = k.PartnerId
GROUP BY p.Name
ORDER BY p.Name;

PRINT '';
PRINT '============================================================================';
PRINT 'UPDATE COMPLETE';
PRINT '============================================================================';
