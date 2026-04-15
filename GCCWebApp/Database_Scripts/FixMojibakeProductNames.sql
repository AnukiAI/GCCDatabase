-- QC #123 - Fix mojibake (special characters) in Product names
-- Root cause: UTF-8 bytes for ™ and ® were stored as individual UTF-16 characters during CSV import
-- ™ (U+2122) UTF-8 bytes E2 84 A2 became: â (226) + „ (8222) + ¢ (162)
-- ® (U+00AE) UTF-8 bytes C2 AE became: Â (194) + ® (174)
-- Date: 2026-04-14
-- Run against: Production (gccwebap_certificates)

BEGIN TRANSACTION;

-- Preview before fix
SELECT Id, Name AS CurrentName FROM Products WHERE Id IN (62, 71, 73, 76, 80);

-- Fix ™ mojibake: replace â„¢ (3 chars: U+00E2 + U+201E + U+00A2) with ™ (U+2122)
UPDATE Products
SET Name = REPLACE(Name, NCHAR(0x00E2) + NCHAR(0x201E) + NCHAR(0x00A2), NCHAR(0x2122))
WHERE Id IN (62, 76);

-- Fix ® mojibake: replace Â® (2 chars: U+00C2 + U+00AE) with ® (U+00AE)
UPDATE Products
SET Name = REPLACE(Name, NCHAR(0x00C2) + NCHAR(0x00AE), NCHAR(0x00AE))
WHERE Id IN (71, 73, 80);

-- Verify after fix
SELECT Id, Name AS CorrectedName FROM Products WHERE Id IN (62, 71, 73, 76, 80);

-- Uncomment to commit when verified:
-- COMMIT;
-- Or rollback if something looks wrong:
-- ROLLBACK;
