#!/usr/bin/env python3
"""
Generate SQL INSERT statements for production database seeding
"""

import subprocess
import re
from datetime import datetime

# Database connection info
SOURCE_SERVER = "ANUKI01"
SOURCE_DB = "GCCDatabase_Dev"

# Tables in dependency order
TABLES = [
    {"name": "Roles", "identity": True, "columns": ["Id", "Name", "Description", "IsActive", "IsDefault", "CreatedAt", "UpdatedAt"]},
    {"name": "Companies", "identity": True, "columns": ["Id", "Name", "ContactName", "ContactEmail", "ContactPhone", "Address", "City", "State", "ZipCode", "Country", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy"]},
    {"name": "Partners", "identity": True, "columns": ["Id", "Name", "IsActive", "CreatedAt", "UpdatedAt"]},
    {"name": "CertificationTypes", "identity": True, "columns": ["Id", "Name", "Description", "Level", "ParentId", "IsActive", "DisplayOrder", "CreatedAt", "UpdatedAt"]},
    {"name": "Products", "identity": True, "columns": ["Id", "Name", "Description", "Sku", "CompanyId", "ProductType", "Category", "IsActive", "CreatedAt", "UpdatedAt", "CreatedBy", "UpdatedBy"]},
    {"name": "Users", "identity": True, "columns": ["Id", "UserName", "NormalizedUserName", "Email", "NormalizedEmail", "EmailConfirmed", "PasswordHash", "SecurityStamp", "ConcurrencyStamp", "PhoneNumber", "PhoneNumberConfirmed", "TwoFactorEnabled", "LockoutEnd", "LockoutEnabled", "AccessFailedCount", "FirstName", "LastName", "IsActive", "LastLoginDate", "CreatedAt", "UpdatedAt"]},
    {"name": "Tags", "identity": True, "columns": ["Id", "Name", "Description", "IsInternal", "Color", "CreatedAt", "UpdatedAt"]},
    {"name": "Certificates", "identity": True, "columns": ["Id", "Number", "Status", "EffectiveDate", "ExpirationDate", "Location", "Country", "Asin", "Upc", "Gtin", "Ean", "CertificateFile", "CertificateImagePath", "Partner", "CompanyId", "ProductId", "CertificationTypeId", "CreatedBy", "UpdatedBy", "CreatedAt", "UpdatedAt"]},
    {"name": "ApiKeys", "identity": True, "columns": ["Id", "KeyValue", "Description", "UserId", "IsActive", "ExpiresAt", "CreatedAt"]},
]

def escape_sql_value(value):
    """Escape single quotes in SQL values"""
    if value is None or value == "NULL":
        return "NULL"
    return "'" + str(value).replace("'", "''") + "'"

def format_value(value, column_name):
    """Format value based on type"""
    if value is None or value == "" or value == "NULL":
        return "NULL"

    # Boolean columns
    if column_name in ["IsActive", "IsDefault", "IsInternal", "EmailConfirmed", "PhoneNumberConfirmed", "TwoFactorEnabled", "LockoutEnabled"]:
        return "1" if value in ["1", "True", "true"] else "0"

    # Numeric columns
    if column_name in ["Id", "Level", "DisplayOrder", "CompanyId", "ProductId", "CertificationTypeId", "UserId", "ParentId", "CreatedBy", "UpdatedBy", "AccessFailedCount"]:
        try:
            return str(int(value))
        except:
            return "NULL"

    # DateTime columns
    if column_name in ["CreatedAt", "UpdatedAt", "EffectiveDate", "ExpirationDate", "LastLoginDate", "ExpiresAt", "LockoutEnd"]:
        if value and value != "NULL":
            return escape_sql_value(value)
        return "NULL"

    # String columns
    return escape_sql_value(value)

def export_table_data(table_name):
    """Export table data using sqlcmd"""
    cmd = f'sqlcmd -S {SOURCE_SERVER} -E -d {SOURCE_DB} -Q "SELECT * FROM [{table_name}]" -s"|" -W -h-1'
    result = subprocess.run(cmd, shell=True, capture_output=True, text=True)

    if result.returncode != 0:
        print(f"Error exporting {table_name}: {result.stderr}")
        return []

    lines = result.stdout.strip().split('\n')
    # Remove SQL stats line
    data_lines = [line for line in lines if not line.startswith('(') and line.strip()]
    return data_lines

def generate_inserts(table_info):
    """Generate INSERT statements for a table"""
    table_name = table_info["name"]
    columns = table_info["columns"]
    has_identity = table_info.get("identity", False)

    print(f"Generating INSERTs for {table_name}...")

    # Export data
    data_lines = export_table_data(table_name)

    if not data_lines:
        print(f"  No data found for {table_name}")
        return []

    inserts = []

    # Add identity insert ON if needed
    if has_identity:
        inserts.append(f"SET IDENTITY_INSERT [{table_name}] ON;")

    # Generate INSERT for each row
    for line in data_lines:
        values = line.split('|')
        if len(values) != len(columns):
            continue

        formatted_values = []
        for i, col in enumerate(columns):
            val = values[i].strip() if i < len(values) else ""
            formatted_values.append(format_value(val, col))

        column_list = ", ".join([f"[{col}]" for col in columns])
        value_list = ", ".join(formatted_values)

        inserts.append(f"INSERT INTO [{table_name}] ({column_list}) VALUES ({value_list});")

    # Add identity insert OFF if needed
    if has_identity:
        inserts.append(f"SET IDENTITY_INSERT [{table_name}] OFF;")

    print(f"  Generated {len(data_lines)} INSERT statements")
    return inserts

def main():
    """Main function"""
    output_file = "01_Seed_Production_Data.sql"

    print("=" * 60)
    print("GCC Database Seed Data Generator")
    print(f"Source: {SOURCE_SERVER} / {SOURCE_DB}")
    print(f"Output: {output_file}")
    print("=" * 60)
    print()

    all_inserts = []

    # Header
    all_inserts.append("-- =========================================")
    all_inserts.append("-- GCC Production Database Seed Data")
    all_inserts.append(f"-- Generated: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
    all_inserts.append(f"-- Source: {SOURCE_SERVER} / {SOURCE_DB}")
    all_inserts.append("-- Target: Production Database")
    all_inserts.append("-- =========================================")
    all_inserts.append("")
    all_inserts.append("-- Disable constraints temporarily for clean import")
    all_inserts.append("EXEC sp_MSforeachtable 'ALTER TABLE ? NOCHECK CONSTRAINT all';")
    all_inserts.append("GO")
    all_inserts.append("")

    # Generate inserts for each table
    for table in TABLES:
        all_inserts.append("")
        all_inserts.append(f"-- =========================================")
        all_inserts.append(f"-- {table['name']}")
        all_inserts.append(f"-- =========================================")

        inserts = generate_inserts(table)
        all_inserts.extend(inserts)

        all_inserts.append("GO")
        all_inserts.append("")

    # Footer
    all_inserts.append("-- Re-enable constraints")
    all_inserts.append("EXEC sp_MSforeachtable 'ALTER TABLE ? WITH CHECK CHECK CONSTRAINT all';")
    all_inserts.append("GO")
    all_inserts.append("")
    all_inserts.append("-- Seed data import complete")
    all_inserts.append("PRINT 'Production database seeded successfully';")
    all_inserts.append("GO")

    # Write to file
    with open(output_file, 'w', encoding='utf-8') as f:
        f.write('\n'.join(all_inserts))

    print()
    print("=" * 60)
    print(f"SUCCESS: Script generated - {output_file}")
    print("=" * 60)
    print()
    print("To deploy to production:")
    print(f'sqlcmd -S 68.71.129.120,1533 -U GCC_adminxyz123 -P "Q3Ez4e!gE_47KNi" -d gccwebap_certificates -i {output_file}')

if __name__ == "__main__":
    main()
