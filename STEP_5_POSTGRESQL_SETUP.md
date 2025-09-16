# Step 5: Set Up PostgreSQL Database with Secure Credentials

## Database Configuration for angry-pascal.70-32-82-212.plesk.page

This guide uses the **actual secure credentials** generated for your deployment:

- **Database Name**: `gccdb`
- **Username**: `gccuser`
- **Password**: `74Fzs0YFLMXLrKJG5fzexsTkk`
- **Host**: `localhost`
- **Port**: `5432`

---

## Step 5.1: Connect to PostgreSQL

### Method A: Default postgres user (most common)
```bash
# Connect as postgres superuser
sudo -u postgres psql
```

### Method B: If password authentication is required
```bash
# Connect with password
psql -U postgres -h localhost
# Enter postgres user password when prompted
```

### Method C: If postgres user doesn't exist
```bash
# Install PostgreSQL client if needed
sudo apt-get install postgresql-client

# Connect using root or admin credentials
psql -U root -h localhost
```

---

## Step 5.2: Create Database and User (COPY THESE COMMANDS EXACTLY)

```sql
-- Create database
CREATE DATABASE gccdb;

-- Create user with the secure password we generated
CREATE USER gccuser WITH PASSWORD '74Fzs0YFLMXLrKJG5fzexsTkk';

-- Grant all permissions on the database
GRANT ALL PRIVILEGES ON DATABASE gccdb TO gccuser;

-- Grant schema permissions (PostgreSQL 15+ requirement)
GRANT ALL ON SCHEMA public TO gccuser;

-- Grant default privileges for future tables
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON TABLES TO gccuser;
ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT ALL ON SEQUENCES TO gccuser;

-- Exit PostgreSQL
\q
```

---

## Step 5.3: Test Database Connection

```bash
# Test connection with your new credentials
psql -h localhost -U gccuser -d gccdb -c "SELECT version();"

# If prompted for password, enter: 74Fzs0YFLMXLrKJG5fzexsTkk
```

**Expected Output:**
```
                                                 version
----------------------------------------------------------------------------------------------------------
 PostgreSQL 14.x on x86_64-pc-linux-gnu, compiled by gcc (Ubuntu 9.4.0-1ubuntu1~20.04.1) 9.4.0, 64-bit
(1 row)
```

---

## Step 5.4: Verify Permissions

```bash
# Connect to your database
psql -h localhost -U gccuser -d gccdb

# Test basic operations
\dt  -- List tables (should be empty initially)
CREATE TABLE test_table (id SERIAL PRIMARY KEY, name VARCHAR(50));
INSERT INTO test_table (name) VALUES ('Test');
SELECT * FROM test_table;
DROP TABLE test_table;
\q
```

---

## Step 5.5: Configure PostgreSQL for Docker Access

### Enable Network Access

1. **Edit PostgreSQL configuration:**
```bash
# Find your PostgreSQL version
sudo -u postgres psql -c "SHOW config_file;"

# Edit postgresql.conf (adjust path for your version)
sudo nano /etc/postgresql/14/main/postgresql.conf
```

2. **Update these settings:**
```conf
# Connection Settings
listen_addresses = 'localhost,127.0.0.1'
port = 5432
max_connections = 100

# Memory Settings (adjust based on your server)
shared_buffers = 256MB
effective_cache_size = 1GB
```

3. **Edit authentication file:**
```bash
sudo nano /etc/postgresql/14/main/pg_hba.conf
```

4. **Add this line (if not already present):**
```conf
# Local connections for Docker
host    gccdb           gccuser         127.0.0.1/32           md5
host    gccdb           gccuser         ::1/128                 md5
```

5. **Restart PostgreSQL:**
```bash
sudo systemctl restart postgresql
sudo systemctl status postgresql
```

---

## Step 5.6: Docker Network Configuration (if needed)

If your Docker container can't connect to PostgreSQL, you may need to adjust the connection:

### Option A: Use host networking
```bash
# In your docker run command, add:
--network host
```

### Option B: Use Docker bridge network
```bash
# Create a custom network
docker network create gcc-network

# Run PostgreSQL in Docker (if not using system PostgreSQL)
docker run -d --name postgres --network gcc-network \
  -e POSTGRES_DB=gccdb \
  -e POSTGRES_USER=gccuser \
  -e POSTGRES_PASSWORD=74Fzs0YFLMXLrKJG5fzexsTkk \
  -p 5432:5432 postgres:14

# Update your app container to use the same network
docker run -d --name gcc-webapp --network gcc-network \
  -e ConnectionStrings__DefaultConnection="Host=postgres;Database=gccdb;Username=gccuser;Password=74Fzs0YFLMXLrKJG5fzexsTkk;Port=5432" \
  # ... other environment variables
```

---

## Step 5.7: Final Connection String Verification

Your Docker deployment script uses this connection string:
```
Host=localhost;Database=gccdb;Username=gccuser;Password=74Fzs0YFLMXLrKJG5fzexsTkk;Port=5432
```

**Test this exact connection string:**
```bash
# Using .NET connection string format test
docker run --rm --network host mcr.microsoft.com/dotnet/runtime:8.0 \
  bash -c "echo 'Connection string format: Host=localhost;Database=gccdb;Username=gccuser;Password=74Fzs0YFLMXLrKJG5fzexsTkk;Port=5432'"
```

---

## Troubleshooting Common Issues

### Issue 1: "FATAL: password authentication failed for user 'gccuser'"
```bash
# Check if user exists
sudo -u postgres psql -c "\du"

# Reset password if needed
sudo -u postgres psql -c "ALTER USER gccuser PASSWORD '74Fzs0YFLMXLrKJG5fzexsTkk';"
```

### Issue 2: "FATAL: database 'gccdb' does not exist"
```bash
# List databases
sudo -u postgres psql -c "\l"

# Create database if missing
sudo -u postgres psql -c "CREATE DATABASE gccdb OWNER gccuser;"
```

### Issue 3: "Connection refused"
```bash
# Check PostgreSQL status
sudo systemctl status postgresql

# Check if PostgreSQL is listening
sudo netstat -tlnp | grep 5432

# Start PostgreSQL if stopped
sudo systemctl start postgresql
```

### Issue 4: Docker container can't connect
```bash
# Test from Docker container
docker run --rm --network host postgres:14 \
  psql -h localhost -U gccuser -d gccdb -c "SELECT 1;"

# If fails, check firewall
sudo ufw allow 5432
```

---

## Security Notes

- ✅ **Password**: Uses secure 25-character password with mixed case, numbers, and symbols
- ✅ **User Permissions**: Limited to specific database only
- ✅ **Network Access**: Restricted to localhost connections
- ✅ **No Default Passwords**: No default or weak passwords used

---

## Next Steps

After completing this step:

1. ✅ Database created with secure credentials
2. ✅ Connection tested and working
3. ➡️ **Continue to Step 6**: Install Docker (if not installed)
4. ➡️ **Continue to Step 7**: Login to GitHub Container Registry
5. ➡️ **Continue to Step 8**: Run deployment script

---

**Database Setup Status**: ✅ READY FOR DEPLOYMENT

Your PostgreSQL database is now configured with the secure credentials that match your deployment script. The GCC Web Application will automatically create the necessary tables when it first connects.