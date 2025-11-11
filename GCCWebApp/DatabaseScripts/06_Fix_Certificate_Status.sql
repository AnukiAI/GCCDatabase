-- =========================================
-- FIX: Update Certificate Status to "active"
-- This will make certificates visible on public tile view
-- =========================================

USE gccwebap_certificates;
GO

PRINT '========================================='
PRINT 'FIXING CERTIFICATE STATUS VALUES'
PRINT '========================================='
PRINT ''

-- Show current status distribution
PRINT 'BEFORE FIX:'
SELECT Status, COUNT(*) as Count
FROM Certificates
GROUP BY Status;

PRINT ''
PRINT 'Updating certificates to "active" status...'
GO

-- Update certificates that should be active
-- (not expired and not drafts)
UPDATE Certificates
SET Status = 'active'
WHERE Status NOT IN ('active', 'expiring_soon', 'renewal_in_progress')
  AND ExpirationDate > GETDATE()  -- Not expired
  AND Status != 'draft';           -- Keep drafts as drafts

-- Update expiring soon (within 30 days)
UPDATE Certificates
SET Status = 'expiring_soon'
WHERE ExpirationDate < DATEADD(day, 30, GETDATE())
  AND ExpirationDate > GETDATE()
  AND Status != 'draft';

-- Update expired certificates
UPDATE Certificates
SET Status = 'expired'
WHERE ExpirationDate <= GETDATE()
  AND Status != 'draft';

GO

PRINT ''
PRINT 'AFTER FIX:'
SELECT Status, COUNT(*) as Count
FROM Certificates
GROUP BY Status;

PRINT ''
PRINT '========================================='
PRINT 'Certificates that WILL display on public site:'
SELECT COUNT(*) as VisibleCount
FROM Certificates
WHERE Status IN ('active', 'expiring_soon', 'renewal_in_progress');

PRINT ''
PRINT '========================================='
PRINT 'FIX COMPLETE!'
PRINT 'Now refresh your website - certificates should display'
PRINT '========================================='
GO
