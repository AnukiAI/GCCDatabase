# Step 5: Set Up MariaDB Database with Secure Credentials

## Database Configuration for angry-pascal.70-32-82-212.plesk.page

Your Plesk server uses **MariaDB v5.5.68** (MySQL-compatible) on port 3306.

**Secure Credentials Generated:**
- **Database Name**: `gccdb`
- **Username**: `gccuser`
- **Password**: `74Fzs0YFLMXLrKJG5fzexsTkk`
- **Host**: `localhost`
- **Port**: `3306`

---

## Step 5.1: Connect to MariaDB

### Method A: Root user connection
```bash
# Connect as root user
mysql -u root -p
# Enter your MariaDB root password when prompted
```

### Method B: Through Plesk Database Management
1. Open Plesk Panel
2. Go to **Databases**
3. Click **Add Database** or use existing database management

### Method C: Command line with specific host
```bash
# If connecting from specific host
mysql -h localhost -P 3306 -u root -p
```

---

## Step 5.2: Create Database and User (COPY THESE COMMANDS EXACTLY)

```sql
-- Create database
CREATE DATABASE gccdb CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- Create user with the secure password we generated
CREATE USER 'gccuser'@'localhost' IDENTIFIED BY '74Fzs0YFLMXLrKJG5fzexsTkk';

-- Grant all permissions on the database
GRANT ALL PRIVILEGES ON gccdb.* TO 'gccuser'@'localhost';

-- Allow connections from Docker containers (127.0.0.1)
CREATE USER 'gccuser'@'127.0.0.1' IDENTIFIED BY '74Fzs0YFLMXLrKJG5fzexsTkk';
GRANT ALL PRIVILEGES ON gccdb.* TO 'gccuser'@'127.0.0.1';

-- Flush privileges to apply changes
FLUSH PRIVILEGES;

-- Show created users
SELECT User, Host FROM mysql.user WHERE User = 'gccuser';

-- Exit MariaDB
EXIT;
```

---

## Step 5.3: Test Database Connection

```bash
# Test connection with your new credentials
mysql -h localhost -P 3306 -u gccuser -p gccdb

# When prompted for password, enter: 74Fzs0YFLMXLrKJG5fzexsTkk
```

**Expected Output:**
```
Welcome to the MariaDB monitor.  Commands end with ; or \g.
Your MariaDB connection id is 123
Server version: 5.5.68-MariaDB MariaDB Server

Copyright (c) 2000, 2018, Oracle, MariaDB Corporation Ab and others.

Type 'help;' or '\h' for help. Type '\c' to clear the current input statement.

MariaDB [gccdb]>
```

---

## Step 5.4: Verify Database and Permissions

```sql
-- Inside MariaDB console
SHOW DATABASES;
USE gccdb;
SHOW TABLES;

-- Test basic operations
CREATE TABLE test_table (id INT AUTO_INCREMENT PRIMARY KEY, name VARCHAR(50));
INSERT INTO test_table (name) VALUES ('Test Connection');
SELECT * FROM test_table;
DROP TABLE test_table;

-- Check grants
SHOW GRANTS FOR 'gccuser'@'localhost';

-- Exit
EXIT;
```

---

## Step 5.5: Update Connection Strings for MariaDB/MySQL

The connection string format needs to be updated from PostgreSQL to MySQL format:

### OLD (PostgreSQL format):
```
Host=localhost;Database=gccdb;Username=gccuser;Password=74Fzs0YFLMXLrKJG5fzexsTkk;Port=5432
```

### NEW (MySQL/MariaDB format):
```
Server=localhost;Port=3306;Database=gccdb;User=gccuser;Password=74Fzs0YFLMXLrKJG5fzexsTkk;
```

**OR** (alternative format):
```
server=localhost;port=3306;database=gccdb;uid=gccuser;pwd=74Fzs0YFLMXLrKJG5fzexsTkk;
```

---

## Step 5.6: Configure MariaDB for Docker Access

### Check MariaDB Configuration

1. **Find MariaDB configuration file:**
```bash
# Common locations
ls -la /etc/mysql/my.cnf
ls -la /etc/my.cnf
ls -la /usr/etc/my.cnf
```

2. **Edit configuration (if needed):**
```bash
sudo nano /etc/mysql/my.cnf
```

3. **Ensure these settings:**
```ini
[mysqld]
bind-address = 127.0.0.1
port = 3306
max_connections = 200
```

4. **Restart MariaDB:**
```bash
sudo systemctl restart mysql
sudo systemctl status mysql
```

---

## Step 5.7: Test Connection from Docker Container

```bash
# Test MySQL connection from Docker
docker run --rm --network host mysql:5.7 \
  mysql -h localhost -P 3306 -u gccuser -p74Fzs0YFLMXLrKJG5fzexsTkk -e "SELECT 1;"
```

---

## Step 5.8: Update Your Deployment Scripts

We need to update your deployment scripts to use MySQL instead of PostgreSQL:

### Updated Environment Variables:
```bash
# MariaDB Database Configuration (Updated)
MYSQL_HOST=localhost
MYSQL_DATABASE=gccdb
MYSQL_USER=gccuser
MYSQL_PASSWORD=74Fzs0YFLMXLrKJG5fzexsTkk
MYSQL_PORT=3306
```

### Updated Connection String:
```bash
ConnectionStrings__DefaultConnection="Server=localhost;Port=3306;Database=gccdb;User=gccuser;Password=74Fzs0YFLMXLrKJG5fzexsTkk;"
```

---

## Important: ASP.NET Core MySQL Support

Your ASP.NET Core application will need **MySQL Entity Framework provider** instead of PostgreSQL:

### Required NuGet Package:
```xml
<PackageReference Include="MySql.EntityFrameworkCore" Version="7.0.0" />
<!-- OR -->
<PackageReference Include="Pomelo.EntityFrameworkCore.MySql" Version="7.0.0" />
```

### Connection String in appsettings.json:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=gccdb;User=gccuser;Password=74Fzs0YFLMXLrKJG5fzexsTkk;"
  }
}
```

---

## Troubleshooting Common MariaDB Issues

### Issue 1: "Access denied for user 'gccuser'@'localhost'"
```sql
-- Check if user exists
SELECT User, Host FROM mysql.user WHERE User = 'gccuser';

-- Reset password if needed
SET PASSWORD FOR 'gccuser'@'localhost' = PASSWORD('74Fzs0YFLMXLrKJG5fzexsTkk');
FLUSH PRIVILEGES;
```

### Issue 2: "Unknown database 'gccdb'"
```sql
-- Show databases
SHOW DATABASES;

-- Create database if missing
CREATE DATABASE gccdb CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
```

### Issue 3: "Can't connect to MySQL server"
```bash
# Check MariaDB status
sudo systemctl status mysql

# Check if MariaDB is listening on port 3306
sudo netstat -tlnp | grep 3306

# Start MariaDB if stopped
sudo systemctl start mysql
```

### Issue 4: Docker connection issues
```bash
# Test connection from host
mysql -h localhost -P 3306 -u gccuser -p gccdb

# Check Docker network access
docker run --rm --network host mysql:5.7 \
  mysql -h localhost -P 3306 -u gccuser -p74Fzs0YFLMXLrKJG5fzexsTkk -e "SHOW DATABASES;"
```

---

## Security Notes for MariaDB

- ✅ **Password**: Uses secure 25-character password
- ✅ **User Permissions**: Limited to specific database only
- ✅ **Host Restrictions**: Only localhost and 127.0.0.1 access
- ✅ **UTF8MB4**: Full Unicode support configured
- ⚠️ **MariaDB 5.5.68**: Consider upgrading to newer version for better security

---

## Next Steps After Database Setup

1. ✅ **Update deployment scripts** with MySQL connection strings
2. ✅ **Verify ASP.NET Core has MySQL Entity Framework provider**
3. ➡️ **Continue to Step 6**: Install Docker
4. ➡️ **Continue to Step 7**: GitHub Container Registry login
5. ➡️ **Continue to Step 8**: Run updated deployment script

---

**Database Setup Status**: ✅ READY FOR MARIADB DEPLOYMENT

Your MariaDB database is now configured. **IMPORTANT**: We need to update your Docker deployment script to use MySQL connection strings instead of PostgreSQL format.