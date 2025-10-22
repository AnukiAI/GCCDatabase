-- Add Partners table to existing database
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

-- Create indexes for Partners table
CREATE INDEX IF NOT EXISTS IX_Partners_Name ON Partners (Name);
CREATE INDEX IF NOT EXISTS IX_Partners_IsActive ON Partners (IsActive);
CREATE INDEX IF NOT EXISTS IX_Partners_IsDeleted ON Partners (IsDeleted);

-- Create UserPartners junction table if it doesn't exist
CREATE TABLE IF NOT EXISTS UserPartners (
    UserId INTEGER NOT NULL,
    PartnerId INTEGER NOT NULL,
    PRIMARY KEY (UserId, PartnerId),
    FOREIGN KEY (UserId) REFERENCES Users (Id) ON DELETE CASCADE,
    FOREIGN KEY (PartnerId) REFERENCES Partners (Id) ON DELETE CASCADE
);

-- Insert initial Partners data
INSERT OR IGNORE INTO Partners (Name, Description, Website, SortOrder, IsActive) VALUES 
('Amazon', 'Global e-commerce marketplace specializing in certified sustainable products', 'https://www.amazon.com', 1, 1),
('Wayfair', 'Online furniture and home goods retailer focused on sustainable product offerings', 'https://www.wayfair.com', 2, 1),
('Other', 'Other retail partners and general partner category', '', 3, 1);

-- Update migration history to mark Partners migration as applied
INSERT OR IGNORE INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES 
('20250909010923_AddPartnerEntity', '9.0.6');