-- Setup Partners table if it doesn't exist
CREATE TABLE IF NOT EXISTS Partners (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500),
    Website NVARCHAR(255),
    ContactInfo NVARCHAR(255),
    Email NVARCHAR(100),
    IsActive BOOLEAN NOT NULL DEFAULT 1,
    IsDeleted BOOLEAN NOT NULL DEFAULT 0,
    SortOrder INTEGER NOT NULL DEFAULT 0,
    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Create indexes
CREATE INDEX IF NOT EXISTS IX_Partners_Name ON Partners (Name);
CREATE INDEX IF NOT EXISTS IX_Partners_IsActive ON Partners (IsActive);
CREATE INDEX IF NOT EXISTS IX_Partners_IsDeleted ON Partners (IsDeleted);

-- Insert initial data if table is empty
INSERT INTO Partners (Name, Description, Website, SortOrder, IsActive)
SELECT 'Amazon', 'Global e-commerce marketplace specializing in certified sustainable products', 'https://www.amazon.com', 1, 1
WHERE NOT EXISTS (SELECT 1 FROM Partners WHERE Name = 'Amazon');

INSERT INTO Partners (Name, Description, Website, SortOrder, IsActive)
SELECT 'Wayfair', 'Online furniture and home goods retailer focused on sustainable product offerings', 'https://www.wayfair.com', 2, 1
WHERE NOT EXISTS (SELECT 1 FROM Partners WHERE Name = 'Wayfair');

INSERT INTO Partners (Name, Description, Website, SortOrder, IsActive)
SELECT 'Other', 'Other retail partners and general partner category', '', 3, 1
WHERE NOT EXISTS (SELECT 1 FROM Partners WHERE Name = 'Other');

-- Check if data was inserted
SELECT 'Partners table setup complete. Records:' as Status;
SELECT COUNT(*) as PartnerCount FROM Partners;
SELECT Name, Description, IsActive FROM Partners ORDER BY SortOrder;