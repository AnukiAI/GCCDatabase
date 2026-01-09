-- =============================================
-- PRODUCT IDENTIFIERS TEST DATA SEED
-- =============================================
-- Purpose: Populate ProductIdentifiers table with test data
-- Date: 2026-01-08
-- Author: Claude AI
--
-- Identifier Types: ASIN, UPC, GTIN, EAN, ISBN, SKU, MPN
-- =============================================

-- First, clear existing data (optional - comment out if you want to keep existing)
-- DELETE FROM ProductIdentifiers;

PRINT 'Starting Product Identifiers seed...';
PRINT '';

-- Check if table exists
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ProductIdentifiers')
BEGIN
    PRINT 'ERROR: ProductIdentifiers table does not exist!';
    RETURN;
END

-- Get count before insert
DECLARE @BeforeCount INT;
SELECT @BeforeCount = COUNT(*) FROM ProductIdentifiers;
PRINT 'Identifiers before seed: ' + CAST(@BeforeCount AS VARCHAR(10));

-- =============================================
-- INSERT PRODUCT IDENTIFIERS
-- Adding multiple identifier types per product for realistic test data
-- =============================================

-- Product 1: 900ET Exit Device Trim with EcoFlexr
INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 1, 'ASIN', 'B08N5WRWNW', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 1 AND IdentifierType = 'ASIN');

INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 1, 'UPC', '012345678901', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 1 AND IdentifierType = 'UPC');

INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 1, 'MPN', 'EXIT-900ET-ECO', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 1 AND IdentifierType = 'MPN');

-- Product 2: Standard Exit Device - Model 9800
INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 2, 'ASIN', 'B07PQRSTYZ', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 2 AND IdentifierType = 'ASIN');

INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 2, 'UPC', '023456789012', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 2 AND IdentifierType = 'UPC');

INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 2, 'GTIN', '00234567890123', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 2 AND IdentifierType = 'GTIN');

-- Product 3: Heavy-Duty Exit Hardware - Series 2000
INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 3, 'ASIN', 'B09HDTYVWX', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 3 AND IdentifierType = 'ASIN');

INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 3, 'MPN', 'HD-2000-SERIES', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 3 AND IdentifierType = 'MPN');

-- Product 4: Commercial Door Closer - DC300
INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 4, 'ASIN', 'B08KLM4567', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 4 AND IdentifierType = 'ASIN');

INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 4, 'UPC', '045678901234', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 4 AND IdentifierType = 'UPC');

INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 4, 'SKU', 'DC300-COM-BLK', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 4 AND IdentifierType = 'SKU');

-- Product 5: Security Exit Device - EX700
INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 5, 'ASIN', 'B07SECURITY1', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 5 AND IdentifierType = 'ASIN');

INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 5, 'EAN', '5012345678901', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 5 AND IdentifierType = 'EAN');

-- Product 6: Reusable Water Bottle - Stainless Steel
INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 6, 'ASIN', 'B08WATRBOTL', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 6 AND IdentifierType = 'ASIN');

INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 6, 'UPC', '067890123456', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 6 AND IdentifierType = 'UPC');

INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 6, 'GTIN', '00678901234567', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 6 AND IdentifierType = 'GTIN');

INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 6, 'SKU', 'WB-SS-32OZ-BLU', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 6 AND IdentifierType = 'SKU');

-- Product 7: Bamboo Coffee Cup with Lid
INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 7, 'ASIN', 'B09BAMBOCUP', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 7 AND IdentifierType = 'ASIN');

INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 7, 'UPC', '078901234567', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 7 AND IdentifierType = 'UPC');

-- Product 8: Glass Storage Container Set
INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 8, 'ASIN', 'B08GLASSCNT', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 8 AND IdentifierType = 'ASIN');

INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 8, 'UPC', '089012345678', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 8 AND IdentifierType = 'UPC');

INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 8, 'MPN', 'GSC-12PC-CLR', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 8 AND IdentifierType = 'MPN');

-- Product 9: Organic Cotton Tote Bag
INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 9, 'ASIN', 'B07COTTNTOT', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 9 AND IdentifierType = 'ASIN');

INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 9, 'SKU', 'TOTE-ORG-NAT', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 9 AND IdentifierType = 'SKU');

-- Product 10: 10Gain XP Category 6A Cable
INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 10, 'ASIN', 'B08CAT6CABL', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 10 AND IdentifierType = 'ASIN');

INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 10, 'UPC', '101234567890', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 10 AND IdentifierType = 'UPC');

INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 10, 'GTIN', '10123456789012', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 10 AND IdentifierType = 'GTIN');

INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 10, 'MPN', '10GAIN-XP-6A-1000', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 10 AND IdentifierType = 'MPN');

-- Product 11: Carbon Negative Carpet Tile
INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 11, 'ASIN', 'B09CARBNCPT', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 11 AND IdentifierType = 'ASIN');

INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 11, 'UPC', '112345678901', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 11 AND IdentifierType = 'UPC');

-- Product 12: 3M VHB Tape
INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 12, 'ASIN', 'B0013HKPJ8', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 12 AND IdentifierType = 'ASIN');

INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 12, 'UPC', '051131064355', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 12 AND IdentifierType = 'UPC');

INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 12, 'MPN', '4910-1X36', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 12 AND IdentifierType = 'MPN');

-- Product 13: Shaw Contract Carpet Tile
INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 13, 'ASIN', 'B07SHAWCARP', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 13 AND IdentifierType = 'ASIN');

INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 13, 'SKU', 'SHAW-CT-COMM', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 13 AND IdentifierType = 'SKU');

-- Product 14: Mohawk Hardwood Flooring
INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 14, 'ASIN', 'B08MOHAWKHW', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 14 AND IdentifierType = 'ASIN');

INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 14, 'UPC', '144567890123', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 14 AND IdentifierType = 'UPC');

INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 14, 'MPN', 'MHK-HW-OAK-5', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 14 AND IdentifierType = 'MPN');

-- Product 15: Armstrong Ceiling Tiles
INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 15, 'ASIN', 'B07ARMSTRNG', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 15 AND IdentifierType = 'ASIN');

INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 15, 'UPC', '155678901234', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 15 AND IdentifierType = 'UPC');

-- Product 16: Owens Corning Fiberglas Insulation
INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 16, 'ASIN', 'B08OCFIBERG', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 16 AND IdentifierType = 'ASIN');

INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 16, 'UPC', '166789012345', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 16 AND IdentifierType = 'UPC');

INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 16, 'MPN', 'OC-R30-16', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 16 AND IdentifierType = 'MPN');

-- Product 17: LED Light Fixtures
INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 17, 'ASIN', 'B09LEDLIGHT', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 17 AND IdentifierType = 'ASIN');

INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 17, 'UPC', '177890123456', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 17 AND IdentifierType = 'UPC');

-- Product 18: Office Chair
INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 18, 'ASIN', 'B08OFFCHAIR', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 18 AND IdentifierType = 'ASIN');

INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 18, 'SKU', 'OFC-ERGO-BLK', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 18 AND IdentifierType = 'SKU');

-- Product 19: CertainTeed Fiber Glass Insulation
INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 19, 'ASIN', 'B07CTFIBERG', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 19 AND IdentifierType = 'ASIN');

INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 19, 'MPN', 'CT-FG-R19-15', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 19 AND IdentifierType = 'MPN');

-- Product 20: Johns Manville Mineral Wool Insulation
INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 20, 'ASIN', 'B08JMMINWOL', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 20 AND IdentifierType = 'ASIN');

INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 20, 'UPC', '201234567890', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 20 AND IdentifierType = 'UPC');

-- Product 21-30: Additional products with various identifiers
INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 21, 'ASIN', 'B09KINGSPAN', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 21 AND IdentifierType = 'ASIN');

INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 22, 'ASIN', 'B07HUNTDOUG', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 22 AND IdentifierType = 'ASIN');

INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 22, 'UPC', '223456789012', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 22 AND IdentifierType = 'UPC');

INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 23, 'ASIN', 'B08STLCTHK', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 23 AND IdentifierType = 'ASIN');

INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 23, 'MPN', 'THINK-465A00', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 23 AND IdentifierType = 'MPN');

INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 24, 'ASIN', 'B01DGG4N9C', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 24 AND IdentifierType = 'ASIN');

INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 24, 'UPC', '840031400036', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 24 AND IdentifierType = 'UPC');

INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 24, 'MPN', 'AERON-B-GR', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 24 AND IdentifierType = 'MPN');

INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 25, 'ASIN', 'B07KNOLLREG', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 25 AND IdentifierType = 'ASIN');

INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 26, 'ASIN', 'B08TEKNION', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 26 AND IdentifierType = 'ASIN');

INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 27, 'ASIN', 'B09FORBOMRM', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 27 AND IdentifierType = 'ASIN');

INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 27, 'EAN', '8712486002001', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 27 AND IdentifierType = 'EAN');

INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 28, 'ASIN', 'B07TARKTIQS', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 28 AND IdentifierType = 'ASIN');

INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 29, 'ASIN', 'B08MILLIKEN', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 29 AND IdentifierType = 'ASIN');

INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 29, 'SKU', 'MILL-MOD-CT', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 29 AND IdentifierType = 'SKU');

INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 30, 'ASIN', 'B07USGSHEET', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 30 AND IdentifierType = 'ASIN');

INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 30, 'UPC', '301234567890', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 30 AND IdentifierType = 'UPC');

-- Products 31-39: More identifiers
INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 31, 'ASIN', 'B078LUTRON', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 31 AND IdentifierType = 'ASIN');

INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 31, 'UPC', '027557000994', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 31 AND IdentifierType = 'UPC');

INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 31, 'MPN', 'PD-6WCL-WH', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 31 AND IdentifierType = 'MPN');

INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 32, 'ASIN', 'B07PHILIPS', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 32 AND IdentifierType = 'ASIN');

INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 32, 'EAN', '8718696587416', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 32 AND IdentifierType = 'EAN');

INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 33, 'ASIN', 'B08CREELEDB', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 33 AND IdentifierType = 'ASIN');

INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 34, 'ASIN', 'B09INTERFAC', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 34 AND IdentifierType = 'ASIN');

INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 34, 'SKU', 'INT-NETW-CT', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 34 AND IdentifierType = 'SKU');

INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 35, 'ASIN', 'B073MSCOTCH', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 35 AND IdentifierType = 'ASIN');

INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 35, 'UPC', '051131065833', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 35 AND IdentifierType = 'UPC');

INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 36, 'ASIN', 'B08SHAWECOQ', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 36 AND IdentifierType = 'ASIN');

INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 37, 'ASIN', 'B07ARMCLEAN', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 37 AND IdentifierType = 'ASIN');

INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 37, 'MPN', 'ARM-CR-770', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 37 AND IdentifierType = 'MPN');

INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 38, 'ASIN', 'B09GAFTPOMB', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 38 AND IdentifierType = 'ASIN');

INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 39, 'ASIN', 'B08SUPESSEX', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 39 AND IdentifierType = 'ASIN');

INSERT INTO ProductIdentifiers (ProductId, IdentifierType, IdentifierValue, CreatedAt, UpdatedAt)
SELECT 39, 'MPN', 'SE-PW-6A-PL', GETUTCDATE(), GETUTCDATE()
WHERE NOT EXISTS (SELECT 1 FROM ProductIdentifiers WHERE ProductId = 39 AND IdentifierType = 'MPN');

-- =============================================
-- SUMMARY
-- =============================================
DECLARE @AfterCount INT;
SELECT @AfterCount = COUNT(*) FROM ProductIdentifiers;

PRINT '';
PRINT '=============================================';
PRINT 'Product Identifiers Seed Complete';
PRINT '=============================================';
PRINT 'Identifiers before: ' + CAST(@BeforeCount AS VARCHAR(10));
PRINT 'Identifiers after:  ' + CAST(@AfterCount AS VARCHAR(10));
PRINT 'Records added:      ' + CAST(@AfterCount - @BeforeCount AS VARCHAR(10));
PRINT '';

-- Display summary by type
SELECT
    IdentifierType,
    COUNT(*) AS Count
FROM ProductIdentifiers
GROUP BY IdentifierType
ORDER BY Count DESC;

-- Display products with most identifiers
SELECT TOP 10
    p.Name AS ProductName,
    COUNT(pi.Id) AS IdentifierCount
FROM Products p
LEFT JOIN ProductIdentifiers pi ON p.Id = pi.ProductId
GROUP BY p.Id, p.Name
HAVING COUNT(pi.Id) > 0
ORDER BY IdentifierCount DESC;

PRINT '';
PRINT 'Seed script completed successfully!';
GO
