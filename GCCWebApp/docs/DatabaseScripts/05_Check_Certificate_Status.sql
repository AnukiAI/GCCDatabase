-- =========================================
-- Check Certificate Status Values in Production
-- This will show WHY certificates aren't displaying
-- =========================================

USE gccwebap_certificates;
GO

PRINT '========================================='
PRINT 'CERTIFICATE STATUS ANALYSIS'
PRINT '========================================='
PRINT ''

-- 1. Count by Status
PRINT 'Certificates by Status:'
PRINT '-----------------------------------------'
SELECT
    Status,
    COUNT(*) as Count,
    CASE
        WHEN Status IN ('active', 'expiring_soon', 'renewal_in_progress')
        THEN 'VISIBLE on public site'
        ELSE 'HIDDEN from public site'
    END as Visibility
FROM Certificates
GROUP BY Status
ORDER BY COUNT(*) DESC;

PRINT ''
PRINT '========================================='
PRINT 'Sample Certificates with Status:'
PRINT '-----------------------------------------'
SELECT TOP 10
    Id,
    Number,
    Status,
    CompanyId,
    ProductId,
    CertificationTypeId,
    EffectiveDate,
    ExpirationDate,
    CASE
        WHEN Status IN ('active', 'expiring_soon', 'renewal_in_progress')
        THEN 'YES'
        ELSE 'NO'
    END as WillDisplay
FROM Certificates
ORDER BY Id;

PRINT ''
PRINT '========================================='
PRINT 'DIAGNOSIS:'
PRINT '-----------------------------------------'
PRINT 'The public Certificate Tile View API filters by status:'
PRINT '  - SHOWS: active, expiring_soon, renewal_in_progress'
PRINT '  - HIDES: draft, pending, suspended, expired, etc.'
PRINT ''
PRINT 'If all your certificates have status other than the above,'
PRINT 'they will NOT display on the public tile view.'
PRINT ''
PRINT 'FIX: Update certificate status to "active"'
PRINT '========================================='
GO
