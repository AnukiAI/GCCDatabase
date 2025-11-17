Changed database context to 'GCCDatabase_Dev'.
-- ============================================
-- COMPANIES DATA (Production Compatible)
-- ============================================
 
SET IDENTITY_INSERT dbo.Companies ON;
 
INSERT INTO dbo.Companies (Id, Name, ContactInfo, Location, Country, IsActive, IsDeleted, Website, CreatedAt, UpdatedAt) VALUES (1, 'ASSA ABLOY Access & Egress Hardware Group, Inc. - Corbin Russwin Division', 'sustainability@assaabloy.com', 'Berlin, CT', 'United States', 1, 0, NULL, GETDATE(), GETDATE());
INSERT INTO dbo.Companies (Id, Name, ContactInfo, Location, Country, IsActive, IsDeleted, Website, CreatedAt, UpdatedAt) VALUES (2, 'Green Products Inc. - Environmental Division', 'contact@greenproducts.com', 'Portland, OR', 'United States', 1, 0, NULL, GETDATE(), GETDATE());
INSERT INTO dbo.Companies (Id, Name, ContactInfo, Location, Country, IsActive, IsDeleted, Website, CreatedAt, UpdatedAt) VALUES (3, 'Acme Corporation', 'info@acme.com', 'New York, NY', 'United States', 1, 0, 'https://www.acme.com', GETDATE(), GETDATE());
INSERT INTO dbo.Companies (Id, Name, ContactInfo, Location, Country, IsActive, IsDeleted, Website, CreatedAt, UpdatedAt) VALUES (4, 'Globex Industries', 'sales@globex.com', 'Chicago, IL', 'United States', 1, 0, 'https://www.globex.com', GETDATE(), GETDATE());
INSERT INTO dbo.Companies (Id, Name, ContactInfo, Location, Country, IsActive, IsDeleted, Website, CreatedAt, UpdatedAt) VALUES (5, 'Stark Enterprises', 'hello@stark.com', 'Malibu, CA', 'United States', 1, 0, 'https://www.stark.com', GETDATE(), GETDATE());
INSERT INTO dbo.Companies (Id, Name, ContactInfo, Location, Country, IsActive, IsDeleted, Website, CreatedAt, UpdatedAt) VALUES (6, 'Wayne Technologies', 'contact@waynetech.com', 'Gotham, NY', 'United States', 1, 0, 'https://www.waynetech.com', GETDATE(), GETDATE());
INSERT INTO dbo.Companies (Id, Name, ContactInfo, Location, Country, IsActive, IsDeleted, Website, CreatedAt, UpdatedAt) VALUES (7, 'EcoFlex Manufacturing', 'info@ecoflex.com', 'Austin, TX', 'United States', 1, 0, NULL, GETDATE(), GETDATE());
INSERT INTO dbo.Companies (Id, Name, ContactInfo, Location, Country, IsActive, IsDeleted, Website, CreatedAt, UpdatedAt) VALUES (8, 'GAF Materials Corporation', 'green@gaf.com', 'Cedar City, UT', 'United States', 1, 0, NULL, GETDATE(), GETDATE());
INSERT INTO dbo.Companies (Id, Name, ContactInfo, Location, Country, IsActive, IsDeleted, Website, CreatedAt, UpdatedAt) VALUES (9, '3M Company', 'sustainability@3m.com', 'St. Paul, MN', 'United States', 1, 0, NULL, GETDATE(), GETDATE());
INSERT INTO dbo.Companies (Id, Name, ContactInfo, Location, Country, IsActive, IsDeleted, Website, CreatedAt, UpdatedAt) VALUES (10, 'Shaw Industries Group', 'green@shawinc.com', 'Dalton, GA', 'United States', 1, 0, NULL, GETDATE(), GETDATE());
INSERT INTO dbo.Companies (Id, Name, ContactInfo, Location, Country, IsActive, IsDeleted, Website, CreatedAt, UpdatedAt) VALUES (11, 'Mohawk Industries', 'sustainability@mohawkind.com', 'Calhoun, GA', 'United States', 1, 0, NULL, GETDATE(), GETDATE());
INSERT INTO dbo.Companies (Id, Name, ContactInfo, Location, Country, IsActive, IsDeleted, Website, CreatedAt, UpdatedAt) VALUES (12, 'Armstrong World Industries', 'green@armstrong.com', 'Lancaster, PA', 'United States', 1, 0, NULL, GETDATE(), GETDATE());
INSERT INTO dbo.Companies (Id, Name, ContactInfo, Location, Country, IsActive, IsDeleted, Website, CreatedAt, UpdatedAt) VALUES (13, 'Owens Corning', 'sustainability@owenscorning.com', 'Toledo, OH', 'United States', 1, 0, NULL, GETDATE(), GETDATE());
INSERT INTO dbo.Companies (Id, Name, ContactInfo, Location, Country, IsActive, IsDeleted, Website, CreatedAt, UpdatedAt) VALUES (14, 'CertainTeed Corporation', 'sustainability@certainteed.com', 'Malvern, PA', 'United States', 1, 0, NULL, GETDATE(), GETDATE());
INSERT INTO dbo.Companies (Id, Name, ContactInfo, Location, Country, IsActive, IsDeleted, Website, CreatedAt, UpdatedAt) VALUES (15, 'Johns Manville', 'environmental@jm.com', 'Denver, CO', 'United States', 1, 0, NULL, GETDATE(), GETDATE());
INSERT INTO dbo.Companies (Id, Name, ContactInfo, Location, Country, IsActive, IsDeleted, Website, CreatedAt, UpdatedAt) VALU
 
SET IDENTITY_INSERT dbo.Companies OFF;
 
-- ============================================
-- PRODUCTS DATA (Production Compatible)
-- ============================================
 
SET IDENTITY_INSERT dbo.Products ON;
 
INSERT INTO dbo.Products (Id, Name, Description, CategoryId, ProductTypeId, CompanyId, IsActive, IsDeleted, CreatedAt, UpdatedAt) VALUES (1, '900ET Exit Device Trim with EcoFlex®', 'Exit device trim with sustainable EcoFlex technology', 1, 1, 1, 1, 0, GETDATE(), GETDATE());
INSERT INTO dbo.Products (Id, Name, Description, CategoryId, ProductTypeId, CompanyId, IsActive, IsDeleted, CreatedAt, UpdatedAt) VALUES (2, 'Standard Exit Device - Model 9800', 'Commercial grade exit device', 1, 1, 1, 1, 0, GETDATE(), GETDATE());
INSERT INTO dbo.Products (Id, Name, Description, CategoryId, ProductTypeId, CompanyId, IsActive, IsDeleted, CreatedAt, UpdatedAt) VALUES (3, 'Heavy-Duty Exit Hardware - Series 2000', 'Heavy-duty commercial exit hardware', 1, 1, 1, 1, 0, GETDATE(), GETDATE());
INSERT INTO dbo.Products (Id, Name, Description, CategoryId, ProductTypeId, CompanyId, IsActive, IsDeleted, CreatedAt, UpdatedAt) VALUES (4, 'Commercial Door Closer - DC300', 'Commercial grade door closing mechanism', 1, 1, 1, 1, 0, GETDATE(), GETDATE());
INSERT INTO dbo.Products (Id, Name, Description, CategoryId, ProductTypeId, CompanyId, IsActive, IsDeleted, CreatedAt, UpdatedAt) VALUES (5, 'Security Exit Device - EX700', 'High-security exit device system', 1, 1, 1, 1, 0, GETDATE(), GETDATE());
INSERT INTO dbo.Products (Id, Name, Description, CategoryId, ProductTypeId, CompanyId, IsActive, IsDeleted, CreatedAt, UpdatedAt) VALUES (6, 'Reusable Water Bottle - Stainless Steel', 'Eco-friendly stainless steel water bottle', 6, NULL, 2, 1, 0, GETDATE(), GETDATE());
INSERT INTO dbo.Products (Id, Name, Description, CategoryId, ProductTypeId, CompanyId, IsActive, IsDeleted, CreatedAt, UpdatedAt) VALUES (7, 'Bamboo Coffee Cup with Lid', 'Sustainable bamboo coffee cup', 6, NULL, 2, 1, 0, GETDATE(), GETDATE());
INSERT INTO dbo.Products (Id, Name, Description, CategoryId, ProductTypeId, CompanyId, IsActive, IsDeleted, CreatedAt, UpdatedAt) VALUES (8, 'Glass Storage Container Set', 'Reusable glass storage containers', 6, NULL, 2, 1, 0, GETDATE(), GETDATE());
INSERT INTO dbo.Products (Id, Name, Description, CategoryId, ProductTypeId, CompanyId, IsActive, IsDeleted, CreatedAt, UpdatedAt) VALUES (9, 'Organic Cotton Tote Bag', 'Sustainable organic cotton tote bag', 5, NULL, 2, 1, 0, GETDATE(), GETDATE());
INSERT INTO dbo.Products (Id, Name, Description, CategoryId, ProductTypeId, CompanyId, IsActive, IsDeleted, CreatedAt, UpdatedAt) VALUES (10, '10Gain XP Category 6A Cable', 'High-performance network cable', 2, 6, 4, 1, 0, GETDATE(), GETDATE());
INSERT INTO dbo.Products (Id, Name, Description, CategoryId, ProductTypeId, CompanyId, IsActive, IsDeleted, CreatedAt, UpdatedAt) VALUES (11, 'Carbon Negative Carpet Tile', 'Sustainable carpet tiles', 5, 4, 5, 1, 0, GETDATE(), GETDATE());
INSERT INTO dbo.Products (Id, Name, Description, CategoryId, ProductTypeId, CompanyId, IsActive, IsDeleted, CreatedAt, UpdatedAt) VALUES (12, '3M VHB Tape', 'Very High Bond structural glazing tape', 1, NULL, 6, 1, 0, GETDATE(), GETDATE());
INSERT INTO dbo.Products (Id, Name, Description, CategoryId, ProductTypeId, CompanyId, IsActive, IsDeleted, CreatedAt, UpdatedAt) VALUES (13, 'Shaw Contract Carpet Tile', 'Commercial carpet tile with recycled content', 5, 4, 7, 1, 0, GETDATE(), GETDATE());
INSERT INTO dbo.Products (Id, Name, Description, CategoryId, ProductTypeId, CompanyId, IsActive, IsDeleted, CreatedAt, UpdatedAt) VALUES (14, 'Mohawk Hardwood Flooring', 'Sustainable hardwood flooring', 1, 3, 8, 1, 0, GETDATE(), GETDATE());
INSERT INTO dbo.Products (Id, Name, Description, CategoryId, ProductTypeId, CompanyId, IsActive, IsDeleted, CreatedAt, UpdatedAt) VALUES (15, 'Armstrong Ceiling Tiles', 'Mineral fiber ceiling tiles', 1, NULL, 9, 1, 0, GETDATE(), GETDATE());
INSERT INTO dbo.Products (Id, Name, Description, CategoryId, ProductTypeId, CompanyId, IsActive, IsDeleted, CreatedAt, UpdatedAt) VALUES (16, 'Owens Corning Fiberglas Insulation', 'Pink fiberglass insulation batts', 1, 1, 1
 
SET IDENTITY_INSERT dbo.Products OFF;
 
-- ============================================
-- CERTIFICATES DATA (Active Only - Production Compatible)
-- ============================================
 
SET IDENTITY_INSERT dbo.Certificates ON;
 
INSERT INTO dbo.Certificates (Id, Number, Status, EffectiveDate, ExpirationDate, Location, Country, CompanyId, ProductId, CertificationTypeId, Partner, Asin, Upc, Gtin, Ean, CreatedAt, UpdatedAt) VALUES (1, '24-1605', 'active', '2024-07-01 00:00:00', '2027-06-30 00:00:00', 'Berlin, CT', 'United States', 1, 1, 1, 'None', 'B08X7N9MRQ', '846378985421', '00846378985421', '0846378985421', GETDATE(), GETDATE());
INSERT INTO dbo.Certificates (Id, Number, Status, EffectiveDate, ExpirationDate, Location, Country, CompanyId, ProductId, CertificationTypeId, Partner, Asin, Upc, Gtin, Ean, CreatedAt, UpdatedAt) VALUES (2, '24-1606', 'active', '2024-09-01 00:00:00', '2027-08-31 00:00:00', 'Portland, OR', 'United States', 2, 6, 2, 'None', NULL, '846378985422', '00846378985422', '0846378985422', GETDATE(), GETDATE());
INSERT INTO dbo.Certificates (Id, Number, Status, EffectiveDate, ExpirationDate, Location, Country, CompanyId, ProductId, CertificationTypeId, Partner, Asin, Upc, Gtin, Ean, CreatedAt, UpdatedAt) VALUES (3, '24-1607', 'active', '2024-08-01 00:00:00', '2027-07-31 00:00:00', 'Berlin, CT', 'United States', 1, 2, 2, 'None', NULL, NULL, NULL, NULL, GETDATE(), GETDATE());
INSERT INTO dbo.Certificates (Id, Number, Status, EffectiveDate, ExpirationDate, Location, Country, CompanyId, ProductId, CertificationTypeId, Partner, Asin, Upc, Gtin, Ean, CreatedAt, UpdatedAt) VALUES (6, '24-5712', 'active', '2024-11-25 00:00:00', '2027-11-24 00:00:00', 'Ashdown, AR', 'United States', 3, 3, 1, 'None', NULL, '456789012345', NULL, NULL, GETDATE(), GETDATE());
INSERT INTO dbo.Certificates (Id, Number, Status, EffectiveDate, ExpirationDate, Location, Country, CompanyId, ProductId, CertificationTypeId, Partner, Asin, Upc, Gtin, Ean, CreatedAt, UpdatedAt) VALUES (7, '24-5633', 'active', '2024-10-01 00:00:00', '2027-09-30 00:00:00', 'Hoisington, KS', 'United States', 4, 4, 1, 'None', 'B09XYZ7890', '234567890123', NULL, NULL, GETDATE(), GETDATE());
INSERT INTO dbo.Certificates (Id, Number, Status, EffectiveDate, ExpirationDate, Location, Country, CompanyId, ProductId, CertificationTypeId, Partner, Asin, Upc, Gtin, Ean, CreatedAt, UpdatedAt) VALUES (8, '24-2108', 'active', '2024-10-01 00:00:00', '2027-09-30 00:00:00', 'Dalton, GA', 'United States', 5, 5, 3, 'None', 'B09DEF1234', '345678901234', NULL, NULL, GETDATE(), GETDATE());
INSERT INTO dbo.Certificates (Id, Number, Status, EffectiveDate, ExpirationDate, Location, Country, CompanyId, ProductId, CertificationTypeId, Partner, Asin, Upc, Gtin, Ean, CreatedAt, UpdatedAt) VALUES (9, '24-6201', 'active', '2024-12-01 00:00:00', '2027-11-30 00:00:00', 'St. Paul, MN', 'United States', 6, 6, 2, 'None', 'B10ABC1234', '456789012346', NULL, NULL, GETDATE(), GETDATE());
INSERT INTO dbo.Certificates (Id, Number, Status, EffectiveDate, ExpirationDate, Location, Country, CompanyId, ProductId, CertificationTypeId, Partner, Asin, Upc, Gtin, Ean, CreatedAt, UpdatedAt) VALUES (10, '24-6385', 'active', '2024-11-15 00:00:00', '2025-11-14 00:00:00', 'Dalton, GA', 'United States', 7, 7, 1, 'None', NULL, '567890123457', NULL, NULL, GETDATE(), GETDATE());
INSERT INTO dbo.Certificates (Id, Number, Status, EffectiveDate, ExpirationDate, Location, Country, CompanyId, ProductId, CertificationTypeId, Partner, Asin, Upc, Gtin, Ean, CreatedAt, UpdatedAt) VALUES (12, '24-8134', 'active', '2024-09-15 00:00:00', '2027-09-14 00:00:00', 'Lancaster, PA', 'United States', 9, 9, 4, 'None', NULL, '789012345679', NULL, NULL, GETDATE(), GETDATE());
INSERT INTO dbo.Certificates (Id, Number, Status, EffectiveDate, ExpirationDate, Location, Country, CompanyId, ProductId, CertificationTypeId, Partner, Asin, Upc, Gtin, Ean, CreatedAt, UpdatedAt) VALUES (14, '25-0001', 'active', '2025-01-01 00:00:00', '2028-12-31 00:00:00', 'Malvern, PA', 'United States', 11, 13, 1, 'None', 'B25CER0001', '125478963014', '01254789630142', '1254789630142', GETDATE(), GETDATE());
INSERT INTO dbo.Certificates (Id, Number, Status, EffectiveDate, ExpirationDate, Lo
 
SET IDENTITY_INSERT dbo.Certificates OFF;
 
-- ============================================
-- EXPORT COMPLETE
-- Copy all output and save as: PRODUCTION_DATA_FROM_DEV.sql
-- Then run on production database
-- ============================================
