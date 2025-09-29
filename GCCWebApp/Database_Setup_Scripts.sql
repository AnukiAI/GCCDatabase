-- ============================================
-- DATABASE SETUP SCRIPTS FOR GCC WEB APPLICATION
-- ============================================
-- Choose the appropriate section for your database system

-- ============================================
-- MARIADB / MYSQL SETUP
-- ============================================

-- Create Database
CREATE DATABASE IF NOT EXISTS gcc_database 
CHARACTER SET utf8mb4 
COLLATE utf8mb4_unicode_ci;

USE gcc_database;

-- Create Users Table
CREATE TABLE IF NOT EXISTS Users (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Username VARCHAR(100) NOT NULL UNIQUE,
    Email VARCHAR(255) NOT NULL UNIQUE,
    PasswordHash VARCHAR(255) NOT NULL,
    Role VARCHAR(50) NOT NULL DEFAULT 'Public',
    IsActive BOOLEAN DEFAULT TRUE,
    TwoFactorEnabled BOOLEAN DEFAULT FALSE,
    EmailConfirmed BOOLEAN DEFAULT FALSE,
    LockoutEnd DATETIME NULL,
    AccessFailedCount INT DEFAULT 0,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    LastLoginAt DATETIME NULL,
    INDEX idx_email (Email),
    INDEX idx_username (Username),
    INDEX idx_role (Role)
) ENGINE=InnoDB;

-- Create Companies Table
CREATE TABLE IF NOT EXISTS Companies (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(255) NOT NULL,
    ContactEmail VARCHAR(255),
    ContactPhone VARCHAR(50),
    Address TEXT,
    IsActive BOOLEAN DEFAULT TRUE,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_name (Name)
) ENGINE=InnoDB;

-- Create Products Table
CREATE TABLE IF NOT EXISTS Products (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(255) NOT NULL,
    Description TEXT,
    ProductType VARCHAR(100),
    CompanyId INT,
    IsActive BOOLEAN DEFAULT TRUE,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (CompanyId) REFERENCES Companies(Id) ON DELETE SET NULL,
    INDEX idx_name (Name),
    INDEX idx_type (ProductType),
    INDEX idx_company (CompanyId)
) ENGINE=InnoDB;

-- Create Certificates Table
CREATE TABLE IF NOT EXISTS Certificates (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    CertificateNumber VARCHAR(100) NOT NULL UNIQUE,
    ProductId INT,
    CompanyId INT,
    IssueDate DATE NOT NULL,
    ExpiryDate DATE,
    Status VARCHAR(50) DEFAULT 'Active',
    CertificateType VARCHAR(100),
    Certifier VARCHAR(255),
    JsonData JSON,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (ProductId) REFERENCES Products(Id) ON DELETE SET NULL,
    FOREIGN KEY (CompanyId) REFERENCES Companies(Id) ON DELETE SET NULL,
    INDEX idx_cert_number (CertificateNumber),
    INDEX idx_status (Status),
    INDEX idx_issue_date (IssueDate),
    INDEX idx_expiry_date (ExpiryDate)
) ENGINE=InnoDB;

-- Create ApiKeys Table
CREATE TABLE IF NOT EXISTS ApiKeys (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(255) NOT NULL,
    KeyHash VARCHAR(255) NOT NULL,
    Description TEXT,
    UserId INT,
    IsActive BOOLEAN DEFAULT TRUE,
    ExpiresAt DATETIME,
    LastUsedAt DATETIME,
    AllowedIPs TEXT,
    AllowedOrigins TEXT,
    Scopes TEXT,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    INDEX idx_key_hash (KeyHash),
    INDEX idx_user (UserId),
    INDEX idx_expires (ExpiresAt)
) ENGINE=InnoDB;

-- Create AuditLogs Table
CREATE TABLE IF NOT EXISTS AuditLogs (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    UserId INT,
    Action VARCHAR(100) NOT NULL,
    EntityType VARCHAR(100),
    EntityId INT,
    OldValues JSON,
    NewValues JSON,
    IpAddress VARCHAR(45),
    UserAgent TEXT,
    Timestamp DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE SET NULL,
    INDEX idx_user (UserId),
    INDEX idx_action (Action),
    INDEX idx_timestamp (Timestamp),
    INDEX idx_entity (EntityType, EntityId)
) ENGINE=InnoDB;

-- Create Dashboard Table
CREATE TABLE IF NOT EXISTS Dashboard (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    UserId INT,
    WidgetType VARCHAR(100),
    Configuration JSON,
    Position INT,
    IsVisible BOOLEAN DEFAULT TRUE,
    CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    INDEX idx_user (UserId)
) ENGINE=InnoDB;

-- Insert default admin user (password: Admin@123456)
-- Note: Change this password immediately after first login!
INSERT INTO Users (Username, Email, PasswordHash, Role, IsActive, EmailConfirmed)
VALUES ('admin', 'admin@greencirclecertified.com', 
        '$2a$11$rBNVdhYPhQAxDTO5Xe5P8e4RbF7vt6y6L5XwCkh3yDgnru/jxfWLa',
        'Admin', TRUE, TRUE);

-- Insert sample companies
INSERT INTO Companies (Name, ContactEmail, ContactPhone, Address) VALUES
('Green Manufacturing Inc.', 'contact@greenmanufacturing.com', '555-0100', '123 Eco Street, Green City, GC 12345'),
('Sustainable Products Co.', 'info@sustainableproducts.com', '555-0200', '456 Nature Ave, Eco Town, ET 67890'),
('EcoFriendly Solutions', 'hello@ecofriendly.com', '555-0300', '789 Earth Road, Planet City, PC 13579');

-- ============================================
-- POSTGRESQL SETUP
-- ============================================

-- Create Database (run as superuser)
-- CREATE DATABASE gcc_database;
-- \c gcc_database;

-- Create Users Table
CREATE TABLE IF NOT EXISTS users (
    id SERIAL PRIMARY KEY,
    username VARCHAR(100) NOT NULL UNIQUE,
    email VARCHAR(255) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    role VARCHAR(50) NOT NULL DEFAULT 'Public',
    is_active BOOLEAN DEFAULT TRUE,
    two_factor_enabled BOOLEAN DEFAULT FALSE,
    email_confirmed BOOLEAN DEFAULT FALSE,
    lockout_end TIMESTAMP NULL,
    access_failed_count INT DEFAULT 0,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    last_login_at TIMESTAMP NULL
);

CREATE INDEX idx_users_email ON users(email);
CREATE INDEX idx_users_username ON users(username);
CREATE INDEX idx_users_role ON users(role);

-- Create Companies Table
CREATE TABLE IF NOT EXISTS companies (
    id SERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    contact_email VARCHAR(255),
    contact_phone VARCHAR(50),
    address TEXT,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_companies_name ON companies(name);

-- Create Products Table
CREATE TABLE IF NOT EXISTS products (
    id SERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    description TEXT,
    product_type VARCHAR(100),
    company_id INT REFERENCES companies(id) ON DELETE SET NULL,
    is_active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_products_name ON products(name);
CREATE INDEX idx_products_type ON products(product_type);
CREATE INDEX idx_products_company ON products(company_id);

-- Create Certificates Table
CREATE TABLE IF NOT EXISTS certificates (
    id SERIAL PRIMARY KEY,
    certificate_number VARCHAR(100) NOT NULL UNIQUE,
    product_id INT REFERENCES products(id) ON DELETE SET NULL,
    company_id INT REFERENCES companies(id) ON DELETE SET NULL,
    issue_date DATE NOT NULL,
    expiry_date DATE,
    status VARCHAR(50) DEFAULT 'Active',
    certificate_type VARCHAR(100),
    certifier VARCHAR(255),
    json_data JSONB,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_certificates_number ON certificates(certificate_number);
CREATE INDEX idx_certificates_status ON certificates(status);
CREATE INDEX idx_certificates_issue_date ON certificates(issue_date);
CREATE INDEX idx_certificates_expiry_date ON certificates(expiry_date);

-- Create ApiKeys Table
CREATE TABLE IF NOT EXISTS api_keys (
    id SERIAL PRIMARY KEY,
    name VARCHAR(255) NOT NULL,
    key_hash VARCHAR(255) NOT NULL,
    description TEXT,
    user_id INT REFERENCES users(id) ON DELETE CASCADE,
    is_active BOOLEAN DEFAULT TRUE,
    expires_at TIMESTAMP,
    last_used_at TIMESTAMP,
    allowed_ips TEXT,
    allowed_origins TEXT,
    scopes TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_api_keys_hash ON api_keys(key_hash);
CREATE INDEX idx_api_keys_user ON api_keys(user_id);
CREATE INDEX idx_api_keys_expires ON api_keys(expires_at);

-- Create AuditLogs Table
CREATE TABLE IF NOT EXISTS audit_logs (
    id SERIAL PRIMARY KEY,
    user_id INT REFERENCES users(id) ON DELETE SET NULL,
    action VARCHAR(100) NOT NULL,
    entity_type VARCHAR(100),
    entity_id INT,
    old_values JSONB,
    new_values JSONB,
    ip_address VARCHAR(45),
    user_agent TEXT,
    timestamp TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_audit_logs_user ON audit_logs(user_id);
CREATE INDEX idx_audit_logs_action ON audit_logs(action);
CREATE INDEX idx_audit_logs_timestamp ON audit_logs(timestamp);
CREATE INDEX idx_audit_logs_entity ON audit_logs(entity_type, entity_id);

-- Create Dashboard Table
CREATE TABLE IF NOT EXISTS dashboard (
    id SERIAL PRIMARY KEY,
    user_id INT REFERENCES users(id) ON DELETE CASCADE,
    widget_type VARCHAR(100),
    configuration JSONB,
    position INT,
    is_visible BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_dashboard_user ON dashboard(user_id);

-- Create update trigger for updated_at columns
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = CURRENT_TIMESTAMP;
    RETURN NEW;
END;
$$ language 'plpgsql';

CREATE TRIGGER update_users_updated_at BEFORE UPDATE ON users
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_companies_updated_at BEFORE UPDATE ON companies
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_products_updated_at BEFORE UPDATE ON products
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_certificates_updated_at BEFORE UPDATE ON certificates
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_api_keys_updated_at BEFORE UPDATE ON api_keys
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_dashboard_updated_at BEFORE UPDATE ON dashboard
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- Insert default admin user (password: Admin@123456)
-- Note: Change this password immediately after first login!
INSERT INTO users (username, email, password_hash, role, is_active, email_confirmed)
VALUES ('admin', 'admin@greencirclecertified.com', 
        '$2a$11$rBNVdhYPhQAxDTO5Xe5P8e4RbF7vt6y6L5XwCkh3yDgnru/jxfWLa',
        'Admin', TRUE, TRUE);

-- Insert sample companies
INSERT INTO companies (name, contact_email, contact_phone, address) VALUES
('Green Manufacturing Inc.', 'contact@greenmanufacturing.com', '555-0100', '123 Eco Street, Green City, GC 12345'),
('Sustainable Products Co.', 'info@sustainableproducts.com', '555-0200', '456 Nature Ave, Eco Town, ET 67890'),
('EcoFriendly Solutions', 'hello@ecofriendly.com', '555-0300', '789 Earth Road, Planet City, PC 13579');

-- ============================================
-- GRANT PERMISSIONS (Both MariaDB and PostgreSQL)
-- ============================================

-- For MariaDB:
-- GRANT ALL PRIVILEGES ON gcc_database.* TO 'gcc_user'@'localhost';
-- FLUSH PRIVILEGES;

-- For PostgreSQL:
-- GRANT ALL PRIVILEGES ON DATABASE gcc_database TO gcc_user;
-- GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA public TO gcc_user;
-- GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA public TO gcc_user;