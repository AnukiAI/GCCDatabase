-- ============================================
-- GCC Dashboard Data Seed Script
-- ============================================
-- This script updates certificate dates and creates
-- proper data distribution for dashboard statistics
-- Run AFTER: FULL_TEST_DATA_SEED.sql
-- ============================================

PRINT '============================================';
PRINT 'GCC Dashboard Data Seeding';
PRINT 'Started at: ' + CONVERT(VARCHAR, GETDATE(), 120);
PRINT '============================================';
PRINT '';

BEGIN TRANSACTION;

BEGIN TRY

    -- ============================================
    -- Step 1: Update Certificate Dates for Better Distribution
    -- ============================================
    PRINT '1. Updating certificate dates for dashboard statistics...';

    DECLARE @Now DATETIME = GETDATE();
    DECLARE @ThisYear INT = YEAR(@Now);
    DECLARE @LastYear INT = @ThisYear - 1;
    DECLARE @ThisMonth INT = MONTH(@Now);
    DECLARE @LastMonth INT = CASE WHEN @ThisMonth = 1 THEN 12 ELSE @ThisMonth - 1 END;
    DECLARE @NextMonth INT = CASE WHEN @ThisMonth = 12 THEN 1 ELSE @ThisMonth + 1 END;

    -- Update CreatedAt dates to create monthly distribution (last 12 months)
    DECLARE @CertId INT = 1;
    DECLARE @MonthsBack INT;
    DECLARE @TotalCerts INT;

    SELECT @TotalCerts = COUNT(*) FROM dbo.Certificates;

    WHILE @CertId <= @TotalCerts
    BEGIN
        -- Distribute certificates across last 12 months
        SET @MonthsBack = (@CertId % 12);

        UPDATE dbo.Certificates
        SET CreatedAt = DATEADD(MONTH, -@MonthsBack, @Now),
            UpdatedAt = DATEADD(MONTH, -@MonthsBack, @Now)
        WHERE Id = @CertId;

        SET @CertId = @CertId + 1;
    END

    PRINT '   Updated ' + CAST(@TotalCerts AS VARCHAR) + ' certificate CreatedAt dates';

    -- ============================================
    -- Step 2: Set Expiration Dates with Varied Distribution
    -- ============================================
    PRINT '2. Setting expiration dates with varied distribution...';

    -- 20% Expired (ExpirationDate in the past)
    UPDATE TOP (20) PERCENT dbo.Certificates
    SET ExpirationDate = DATEADD(DAY, -CAST((ABS(CHECKSUM(NEWID())) % 365) AS INT), @Now),
        Status = 'Expired'
    WHERE ExpirationDate IS NOT NULL;

    -- 10% Expiring in next 30 days
    UPDATE TOP (10) PERCENT dbo.Certificates
    SET ExpirationDate = DATEADD(DAY, CAST((ABS(CHECKSUM(NEWID())) % 30) AS INT), @Now),
        Status = 'Active'
    WHERE Status != 'Expired';

    -- 10% Expiring in 31-60 days
    UPDATE TOP (10) PERCENT dbo.Certificates
    SET ExpirationDate = DATEADD(DAY, 31 + CAST((ABS(CHECKSUM(NEWID())) % 30) AS INT), @Now),
        Status = 'Active'
    WHERE Status != 'Expired' AND ExpirationDate IS NULL;

    -- 10% Expiring in 61-90 days
    UPDATE TOP (10) PERCENT dbo.Certificates
    SET ExpirationDate = DATEADD(DAY, 61 + CAST((ABS(CHECKSUM(NEWID())) % 30) AS INT), @Now),
        Status = 'Active'
    WHERE Status != 'Expired' AND ExpirationDate IS NULL;

    -- Remaining: Expiring in 91+ days (Active)
    UPDATE dbo.Certificates
    SET ExpirationDate = DATEADD(DAY, 91 + CAST((ABS(CHECKSUM(NEWID())) % 275) AS INT), @Now),
        Status = 'Active'
    WHERE ExpirationDate IS NULL;

    PRINT '   Expiration dates set for all certificates';

    -- ============================================
    -- Step 3: Create Certificate Aggregates Table Data
    -- ============================================
    PRINT '3. Populating CertificateAggregates table...';

    -- Delete existing aggregates
    DELETE FROM dbo.CertificateAggregates;

    -- Insert monthly aggregates for last 12 months
    DECLARE @Month INT = 0;
    WHILE @Month < 12
    BEGIN
        DECLARE @AggregateDate DATETIME = DATEADD(MONTH, -@Month, @Now);
        DECLARE @MonthStart DATETIME = DATEADD(DAY, 1-DAY(@AggregateDate), @AggregateDate);
        DECLARE @MonthEnd DATETIME = DATEADD(DAY, -1, DATEADD(MONTH, 1, @MonthStart));
        DECLARE @PeriodString NVARCHAR(450) = FORMAT(@MonthStart, 'yyyy-MM');

        -- Calculate aggregate values
        DECLARE @TotalCertsInPeriod INT;
        DECLARE @ActiveCertsInPeriod INT;
        DECLARE @ExpiredCertsInPeriod INT;
        DECLARE @ExpiringInPeriod INT;
        DECLARE @AvgValidity FLOAT;

        SELECT @TotalCertsInPeriod = COUNT(*) FROM dbo.Certificates WHERE CreatedAt <= @MonthEnd;
        SELECT @ActiveCertsInPeriod = COUNT(*) FROM dbo.Certificates WHERE Status = 'Active' AND CreatedAt <= @MonthEnd;
        SELECT @ExpiredCertsInPeriod = COUNT(*) FROM dbo.Certificates WHERE Status = 'Expired' AND ExpirationDate <= @MonthEnd;
        SELECT @ExpiringInPeriod = COUNT(*) FROM dbo.Certificates
             WHERE YEAR(ExpirationDate) = YEAR(@MonthStart)
             AND MONTH(ExpirationDate) = MONTH(@MonthStart);

        -- Calculate average validity period (in days)
        SELECT @AvgValidity = AVG(DATEDIFF(DAY, EffectiveDate, ExpirationDate))
        FROM dbo.Certificates
        WHERE EffectiveDate IS NOT NULL AND ExpirationDate IS NOT NULL;

        -- For now, use empty JSON arrays for the text fields (can be populated later if needed)
        INSERT INTO dbo.CertificateAggregates
        (
            Period,
            PeriodDate,
            TotalCertificates,
            ActiveCertificates,
            ExpiredCertificates,
            ExpiringThisMonth,
            CertificatesByType,
            CertificatesByCountry,
            CertificatesByCompany,
            AverageValidityPeriod,
            CreatedAt
        )
        VALUES
        (
            @PeriodString,
            @MonthStart,
            @TotalCertsInPeriod,
            @ActiveCertsInPeriod,
            @ExpiredCertsInPeriod,
            @ExpiringInPeriod,
            '[]',  -- Empty JSON array for now
            '[]',  -- Empty JSON array for now
            '[]',  -- Empty JSON array for now
            ISNULL(@AvgValidity, 0),
            @Now
        );

        SET @Month = @Month + 1;
    END

    PRINT '   Created 12 monthly aggregate records';

    -- ============================================
    -- Step 4: Update Status Field for Proper Dashboard Display
    -- ============================================
    PRINT '4. Updating certificate status field...';

    -- Mark as Expired if expiration date passed
    UPDATE dbo.Certificates
    SET Status = 'Expired'
    WHERE ExpirationDate < @Now AND Status != 'Expired';

    -- Mark as Active if not expired
    UPDATE dbo.Certificates
    SET Status = 'Active'
    WHERE ExpirationDate >= @Now AND Status = 'Expired';

    DECLARE @ActiveCount INT = (SELECT COUNT(*) FROM dbo.Certificates WHERE Status = 'Active');
    DECLARE @ExpiredCount INT = (SELECT COUNT(*) FROM dbo.Certificates WHERE Status = 'Expired');
    DECLARE @ExpiringSoonCount INT = (SELECT COUNT(*) FROM dbo.Certificates
                                      WHERE Status = 'Active'
                                      AND ExpirationDate BETWEEN @Now AND DATEADD(DAY, 30, @Now));

    PRINT '   Active: ' + CAST(@ActiveCount AS VARCHAR);
    PRINT '   Expired: ' + CAST(@ExpiredCount AS VARCHAR);
    PRINT '   Expiring in 30 days: ' + CAST(@ExpiringSoonCount AS VARCHAR);

    -- ============================================
    -- Step 5: Verify Dashboard Statistics
    -- ============================================
    PRINT '';
    PRINT '5. Dashboard statistics summary:';
    PRINT '   ================================';

    -- Declare variables for statistics
    DECLARE @TotalCertificatesCount INT;
    DECLARE @TotalCompaniesCount INT;
    DECLARE @TotalProductsCount INT;
    DECLARE @TotalCertificationTypesCount INT;

    -- Get counts
    SELECT @TotalCertificatesCount = COUNT(*) FROM dbo.Certificates;
    SELECT @TotalCompaniesCount = COUNT(*) FROM dbo.Companies;
    SELECT @TotalProductsCount = COUNT(*) FROM dbo.Products;
    SELECT @TotalCertificationTypesCount = COUNT(*) FROM dbo.CertificationTypes;

    PRINT '   Total Certificates: ' + CAST(@TotalCertificatesCount AS VARCHAR);
    PRINT '   Total Companies: ' + CAST(@TotalCompaniesCount AS VARCHAR);
    PRINT '   Total Products: ' + CAST(@TotalProductsCount AS VARCHAR);
    PRINT '   Total Certification Types: ' + CAST(@TotalCertificationTypesCount AS VARCHAR);

    PRINT '';
    PRINT '   Certificates by Status:';
    PRINT '   - Active: ' + CAST(@ActiveCount AS VARCHAR);
    PRINT '   - Expired: ' + CAST(@ExpiredCount AS VARCHAR);
    PRINT '   - Expiring Soon (30 days): ' + CAST(@ExpiringSoonCount AS VARCHAR);

    PRINT '';
    PRINT '   Recent Activity:';
    DECLARE @ThisMonthCreated INT = (SELECT COUNT(*) FROM dbo.Certificates
                                     WHERE YEAR(CreatedAt) = @ThisYear
                                     AND MONTH(CreatedAt) = @ThisMonth);
    DECLARE @LastMonthCreated INT = (SELECT COUNT(*) FROM dbo.Certificates
                                     WHERE YEAR(CreatedAt) = CASE WHEN @ThisMonth = 1 THEN @LastYear ELSE @ThisYear END
                                     AND MONTH(CreatedAt) = @LastMonth);
    DECLARE @ThisYearCreated INT = (SELECT COUNT(*) FROM dbo.Certificates WHERE YEAR(CreatedAt) = @ThisYear);
    DECLARE @LastYearCreated INT = (SELECT COUNT(*) FROM dbo.Certificates WHERE YEAR(CreatedAt) = @LastYear);

    PRINT '   - Created this month: ' + CAST(@ThisMonthCreated AS VARCHAR);
    PRINT '   - Created last month: ' + CAST(@LastMonthCreated AS VARCHAR);
    PRINT '   - Created this year: ' + CAST(@ThisYearCreated AS VARCHAR);
    PRINT '   - Created last year: ' + CAST(@LastYearCreated AS VARCHAR);

    PRINT '';
    PRINT '   Expiration Tracking:';
    DECLARE @ExpiringThisMonth INT = (SELECT COUNT(*) FROM dbo.Certificates
                                      WHERE YEAR(ExpirationDate) = @ThisYear
                                      AND MONTH(ExpirationDate) = @ThisMonth);
    DECLARE @ExpiringNextMonth INT = (SELECT COUNT(*) FROM dbo.Certificates
                                      WHERE YEAR(ExpirationDate) = CASE WHEN @NextMonth = 1 THEN @ThisYear + 1 ELSE @ThisYear END
                                      AND MONTH(ExpirationDate) = @NextMonth);
    DECLARE @ExpiredLastMonth INT = (SELECT COUNT(*) FROM dbo.Certificates
                                     WHERE Status = 'Expired'
                                     AND YEAR(ExpirationDate) = CASE WHEN @ThisMonth = 1 THEN @LastYear ELSE @ThisYear END
                                     AND MONTH(ExpirationDate) = @LastMonth);

    PRINT '   - Expiring this month: ' + CAST(@ExpiringThisMonth AS VARCHAR);
    PRINT '   - Expiring next month: ' + CAST(@ExpiringNextMonth AS VARCHAR);
    PRINT '   - Expired last month: ' + CAST(@ExpiredLastMonth AS VARCHAR);

    COMMIT TRANSACTION;

    PRINT '';
    PRINT '============================================';
    PRINT 'DASHBOARD DATA SEEDING COMPLETE';
    PRINT 'Completed at: ' + CONVERT(VARCHAR, GETDATE(), 120);
    PRINT '============================================';
    PRINT '';
    PRINT 'NEXT STEPS:';
    PRINT '1. Navigate to /Dashboard/Index to view dashboard';
    PRINT '2. Login as admin@greencirclecertified.com';
    PRINT '3. Verify charts and statistics are displaying';
    PRINT '============================================';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrorLine INT = ERROR_LINE();

    PRINT '';
    PRINT '============================================';
    PRINT 'ERROR DURING DASHBOARD DATA SEEDING';
    PRINT '============================================';
    PRINT 'Error on line ' + CAST(@ErrorLine AS VARCHAR) + ':';
    PRINT @ErrorMessage;
    PRINT '============================================';

    -- Re-throw the error
    THROW;
END CATCH
