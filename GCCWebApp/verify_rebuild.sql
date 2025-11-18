-- Verify database rebuild success
PRINT 'Verification Results:';
PRINT '';

PRINT 'Users:';
SELECT TOP 5 Id, Username, Email, Name FROM dbo.Users ORDER BY Id;

PRINT '';
PRINT 'Companies:';
SELECT TOP 5 Id, Name, Country FROM dbo.Companies ORDER BY Id;

PRINT '';
PRINT 'Products:';
SELECT TOP 5 Id, Name FROM dbo.Products ORDER BY Id;

PRINT '';
PRINT 'Certificates:';
SELECT TOP 5 Id, Number, Status FROM dbo.Certificates ORDER BY Id;
GO
