# BreakingScoreBoard Installation Guide

This guide provides detailed instructions for installing and running the BreakingScoreBoard application on Linux, macOS, and Windows.

## Table of Contents

- [Prerequisites](#prerequisites)
- [Quick Start with Docker (Recommended)](#quick-start-with-docker-recommended)
- [Platform-Specific Installation](#platform-specific-installation)
  - [Linux](#linux)
  - [macOS](#macos)
  - [Windows](#windows)
- [Configuration](#configuration)
- [Running the Application](#running-the-application)
- [Accessing the Application](#accessing-the-application)
- [Database Management](#database-management)
- [Troubleshooting](#troubleshooting)
- [Production Deployment](#production-deployment)

---

## Prerequisites

### Option 1: Docker (Recommended for Quick Setup)

**Required:**
- [Docker](https://www.docker.com/get-started) (version 20.10 or later)
- [Docker Compose](https://docs.docker.com/compose/install/) (usually included with Docker Desktop)

**System Requirements:**
- 2 GB RAM minimum (4 GB recommended)
- 2 GB free disk space
- Internet connection for initial image download

### Option 2: Manual Installation

**Required:**
- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [PostgreSQL 15+](https://www.postgresql.org/download/)

**System Requirements:**
- 4 GB RAM minimum
- 5 GB free disk space

---

## Quick Start with Docker (Recommended)

This is the fastest way to get the application running. Works identically on Linux, macOS, and Windows.

### Step 1: Install Docker

**Linux:**
```bash
# Ubuntu/Debian
curl -fsSL https://get.docker.com -o get-docker.sh
sudo sh get-docker.sh
sudo usermod -aG docker $USER
# Log out and log back in for group changes to take effect

# Start Docker
sudo systemctl start docker
sudo systemctl enable docker
```

**macOS:**
1. Download [Docker Desktop for Mac](https://www.docker.com/products/docker-desktop/)
2. Install the .dmg file
3. Start Docker Desktop from Applications

**Windows:**
1. Download [Docker Desktop for Windows](https://www.docker.com/products/docker-desktop/)
2. Install the executable
3. Enable WSL 2 if prompted
4. Start Docker Desktop

### Step 2: Download/Clone the Repository

```bash
# Clone via HTTPS
git clone https://github.com/Alex-Witkowski/BreakingScoreBoard.git
cd BreakingScoreBoard

# OR clone via SSH
git clone git@github.com:Alex-Witkowski/BreakingScoreBoard.git
cd BreakingScoreBoard
```

Alternatively, download and extract the ZIP file from GitHub.

### Step 3: Configure Environment Variables

```bash
# Copy the example environment file
cp .env.example .env

# Edit the .env file (use nano, vim, or any text editor)
nano .env
```

**Important:** Change at least these values:
```bash
POSTGRES_PASSWORD=your_secure_password_here
ADMIN_PIN=your_secure_admin_pin
```

### Step 4: Start the Application

```bash
# Start all services (database + application)
docker compose up -d

# View logs to ensure everything started correctly
docker compose logs -f
```

The application will automatically:
1. Pull required Docker images
2. Create a PostgreSQL database
3. Run database migrations
4. Start the web application

### Step 5: Access the Application

Open your browser and navigate to:
- **Main Application:** http://localhost:8080
- **API Documentation:** http://localhost:8080/swagger
- **Health Check:** http://localhost:8080/health

**Default Access:**
- **Admin PIN:** 1234 (change this in `.env`!)
- **Database:** localhost:5432

### Managing Docker Services

```bash
# Stop the application
docker compose down

# Stop and remove all data (including database)
docker compose down -v

# Restart the application
docker compose restart

# View logs
docker compose logs -f app      # Application logs
docker compose logs -f db       # Database logs

# Update to latest version
git pull
docker compose down
docker compose build --no-cache
docker compose up -d
```

---

## Platform-Specific Installation

For manual installation without Docker.

### Linux

#### Install .NET 9 SDK

**Ubuntu 22.04/24.04:**
```bash
# Add Microsoft package repository
wget https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

# Install .NET SDK
sudo apt-get update
sudo apt-get install -y dotnet-sdk-9.0
```

**Fedora:**
```bash
sudo dnf install dotnet-sdk-9.0
```

**Other distributions:** See [Microsoft's installation guide](https://learn.microsoft.com/en-us/dotnet/core/install/linux)

#### Install PostgreSQL

**Ubuntu/Debian:**
```bash
sudo apt-get update
sudo apt-get install -y postgresql postgresql-contrib
sudo systemctl start postgresql
sudo systemctl enable postgresql
```

**Fedora:**
```bash
sudo dnf install postgresql-server postgresql-contrib
sudo postgresql-setup --initdb
sudo systemctl start postgresql
sudo systemctl enable postgresql
```

#### Configure PostgreSQL

```bash
# Switch to postgres user
sudo -u postgres psql

# In PostgreSQL prompt:
CREATE DATABASE breakingscoreboard;
CREATE USER breakingscoreboarduser WITH PASSWORD 'your_password';
GRANT ALL PRIVILEGES ON DATABASE breakingscoreboard TO breakingscoreboarduser;
\q
```

#### Run the Application

```bash
# Clone repository
git clone https://github.com/Alex-Witkowski/BreakingScoreBoard.git
cd BreakingScoreBoard

# Restore dependencies
dotnet restore

# Update database connection in appsettings
cd src/BreakingScoreBoard.Api
cp appsettings.json appsettings.Production.json

# Edit appsettings.Production.json and update:
# - ConnectionStrings:DefaultConnection
# - AdminPin

# Run migrations
dotnet ef database update

# Run application
dotnet run --urls "http://0.0.0.0:8080"
```

### macOS

#### Install .NET 9 SDK

**Using Homebrew (recommended):**
```bash
brew install dotnet@9
```

**Manual installation:**
1. Download the [.NET 9 SDK installer for macOS](https://dotnet.microsoft.com/download/dotnet/9.0)
2. Run the .pkg installer
3. Verify installation: `dotnet --version`

#### Install PostgreSQL

**Using Homebrew:**
```bash
brew install postgresql@16
brew services start postgresql@16
```

**Using Postgres.app (alternative):**
1. Download [Postgres.app](https://postgresapp.com/)
2. Move to Applications folder
3. Start Postgres.app

#### Configure PostgreSQL

```bash
# Using Homebrew installation:
psql postgres

# In PostgreSQL prompt:
CREATE DATABASE breakingscoreboard;
CREATE USER breakingscoreboarduser WITH PASSWORD 'your_password';
GRANT ALL PRIVILEGES ON DATABASE breakingscoreboard TO breakingscoreboarduser;
\q
```

#### Run the Application

```bash
# Clone repository
git clone https://github.com/Alex-Witkowski/BreakingScoreBoard.git
cd BreakingScoreBoard

# Restore dependencies
dotnet restore

# Update configuration
cd src/BreakingScoreBoard.Api
cp appsettings.json appsettings.Production.json

# Edit appsettings.Production.json and update:
# - ConnectionStrings:DefaultConnection
# - AdminPin

# Run migrations
dotnet ef database update

# Run application
dotnet run --urls "http://localhost:8080"
```

### Windows

#### Install .NET 9 SDK

1. Download [.NET 9 SDK installer for Windows](https://dotnet.microsoft.com/download/dotnet/9.0)
2. Run the installer
3. Verify in PowerShell: `dotnet --version`

#### Install PostgreSQL

1. Download [PostgreSQL installer for Windows](https://www.postgresql.org/download/windows/)
2. Run the installer
3. During installation:
   - Remember the password for the `postgres` user
   - Default port: 5432
   - Install Stack Builder if you want additional tools

#### Configure PostgreSQL

Using **pgAdmin** (installed with PostgreSQL):
1. Open pgAdmin
2. Connect to PostgreSQL Server
3. Right-click "Databases" → Create → Database
   - Name: `breakingscoreboard`
4. Right-click "Login/Group Roles" → Create → Login/Group Role
   - Name: `breakingscoreboarduser`
   - Password: Set a secure password
   - Privileges: Can login
5. Right-click `breakingscoreboard` database → Properties → Security
   - Add `breakingscoreboarduser` with all privileges

**Using PowerShell:**
```powershell
# Add PostgreSQL to PATH if not already
$env:Path += ";C:\Program Files\PostgreSQL\16\bin"

# Connect to PostgreSQL
psql -U postgres

# In PostgreSQL prompt:
CREATE DATABASE breakingscoreboard;
CREATE USER breakingscoreboarduser WITH PASSWORD 'your_password';
GRANT ALL PRIVILEGES ON DATABASE breakingscoreboard TO breakingscoreboarduser;
\q
```

#### Run the Application

**Using PowerShell:**
```powershell
# Clone repository
git clone https://github.com/Alex-Witkowski/BreakingScoreBoard.git
cd BreakingScoreBoard

# Restore dependencies
dotnet restore

# Update configuration
cd src\BreakingScoreBoard.Api
Copy-Item appsettings.json appsettings.Production.json

# Edit appsettings.Production.json and update:
# - ConnectionStrings:DefaultConnection
# - AdminPin

# Run migrations
dotnet ef database update

# Run application
dotnet run --urls "http://localhost:8080"
```

---

## Configuration

### Environment Variables

The application can be configured using environment variables (recommended for Docker) or `appsettings.json` files.

#### Docker Environment Variables (in `.env` file):

```bash
# Database
POSTGRES_USER=postgres
POSTGRES_PASSWORD=your_secure_password
POSTGRES_DB=breakingscoreboard

# Application
ADMIN_PIN=your_admin_pin
API_BASE_URL=http://localhost:8080
LOG_LEVEL=Information
```

#### appsettings.json Format:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=breakingscoreboard;Username=breakingscoreboarduser;Password=your_password"
  },
  "AdminPin": "your_admin_pin",
  "ApiBaseUrl": "http://localhost:8080",
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  }
}
```

### Configuration Options

| Setting | Description | Default | Required |
|---------|-------------|---------|----------|
| `ConnectionStrings:DefaultConnection` | PostgreSQL connection string | - | Yes |
| `AdminPin` | PIN for administrative operations | 1234 | Yes |
| `ApiBaseUrl` | Base URL for API client services | http://localhost:5000 | Yes |
| `ASPNETCORE_URLS` | URL bindings for the application | http://+:8080 | No |
| `ASPNETCORE_ENVIRONMENT` | Environment name | Production | No |
| `Logging:LogLevel:Default` | Logging verbosity | Information | No |

### Security Best Practices

1. **ALWAYS** change the default `ADMIN_PIN` before production use
2. **NEVER** use default passwords for PostgreSQL in production
3. Use strong passwords (12+ characters, mixed case, numbers, symbols)
4. **DO NOT** commit `.env` file or `appsettings.Production.json` to version control
5. Consider using Azure Key Vault, AWS Secrets Manager, or similar for production secrets
6. Use HTTPS in production (configure reverse proxy like nginx or use a cloud service)

---

## Running the Application

### Development Mode

```bash
cd src/BreakingScoreBoard.Api
dotnet run
# Or with watch mode for auto-reload:
dotnet watch run
```

### Production Mode

```bash
# Build for production
dotnet publish -c Release -o ./publish

# Run the published app
cd publish
dotnet BreakingScoreBoard.Api.dll
```

### Running as a Service (Linux)

Create a systemd service file:

```bash
sudo nano /etc/systemd/system/breakingscoreboard.service
```

Content:
```ini
[Unit]
Description=BreakingScoreBoard Application
After=network.target postgresql.service

[Service]
Type=notify
WorkingDirectory=/opt/breakingscoreboard
ExecStart=/usr/bin/dotnet /opt/breakingscoreboard/BreakingScoreBoard.Api.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=breakingscoreboard
User=www-data
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target
```

Enable and start:
```bash
sudo systemctl daemon-reload
sudo systemctl enable breakingscoreboard
sudo systemctl start breakingscoreboard
sudo systemctl status breakingscoreboard
```

---

## Accessing the Application

Once running, access the application at:

### Web Interface

- **Homepage:** http://localhost:8080
- **Organizer Dashboard:** http://localhost:8080/organizer/dashboard
- **Judge Scoring:** http://localhost:8080/judge/scoring
- **Spectator Scoreboard:** http://localhost:8080/spectator/scoreboard

### API & Documentation

- **Swagger UI:** http://localhost:8080/swagger
- **Health Check:** http://localhost:8080/health
- **API Base:** http://localhost:8080/api

### Default Credentials

- **Admin PIN:** 1234 (configured in `.env` or `appsettings.json`)
- Judge PINs are created when setting up battle events

---

## Database Management

### Viewing Database Content

**Using psql (command line):**
```bash
# Docker
docker compose exec db psql -U postgres -d breakingscoreboard

# Manual installation
psql -U breakingscoreboarduser -d breakingscoreboard
```

**Using pgAdmin:**
1. Open pgAdmin
2. Add server: localhost:5432
3. Connect with your credentials
4. Browse tables under breakingscoreboard → Schemas → public → Tables

### Backup Database

**Docker:**
```bash
# Backup
docker compose exec db pg_dump -U postgres breakingscoreboard > backup.sql

# Restore
docker compose exec -T db psql -U postgres breakingscoreboard < backup.sql
```

**Manual installation:**
```bash
# Backup
pg_dump -U breakingscoreboarduser breakingscoreboard > backup.sql

# Restore
psql -U breakingscoreboarduser breakingscoreboard < backup.sql
```

### Reset Database

**Docker:**
```bash
# Stop services
docker compose down

# Remove database volume
docker volume rm breakingscoreboard_postgres_data

# Restart (will recreate database)
docker compose up -d
```

**Manual installation:**
```bash
# Drop and recreate database
psql -U postgres
DROP DATABASE breakingscoreboard;
CREATE DATABASE breakingscoreboard;
GRANT ALL PRIVILEGES ON DATABASE breakingscoreboard TO breakingscoreboarduser;
\q

# Run migrations again
cd src/BreakingScoreBoard.Api
dotnet ef database update
```

---

## Troubleshooting

### Common Issues

#### Port Already in Use

**Problem:** Error: "Address already in use" or "Port 8080 is already allocated"

**Solution:**
```bash
# Find process using port 8080
# Linux/macOS:
sudo lsof -i :8080
sudo kill -9 <PID>

# Windows PowerShell:
netstat -ano | findstr :8080
taskkill /PID <PID> /F

# OR change the port in .env:
APP_PORT=8081
```

#### Database Connection Failed

**Problem:** "Connection refused" or "could not connect to server"

**Solution for Docker:**
```bash
# Check if database is running
docker compose ps

# Check database logs
docker compose logs db

# Ensure database is healthy
docker compose exec db pg_isready -U postgres

# Restart services
docker compose restart
```

**Solution for Manual Installation:**
```bash
# Check PostgreSQL status
# Linux:
sudo systemctl status postgresql

# macOS (Homebrew):
brew services list

# Windows:
services.msc  # Look for "postgresql-x64-16"

# Test connection
psql -U postgres -h localhost -p 5432
```

#### Migration Errors

**Problem:** "Pending model changes" or migration failures

**Solution:**
```bash
cd src/BreakingScoreBoard.Api

# Check migration status
dotnet ef migrations list

# Apply migrations
dotnet ef database update

# If migrations fail, try resetting
dotnet ef database drop
dotnet ef database update
```

#### Permission Denied (Linux/macOS)

**Problem:** Cannot access files or run Docker commands

**Solution:**
```bash
# Add user to docker group
sudo usermod -aG docker $USER
# Log out and back in

# Fix file permissions
sudo chown -R $USER:$USER .
```

#### Out of Memory

**Problem:** Application crashes or becomes unresponsive

**Solution:**
- Increase Docker memory limit (Docker Desktop → Settings → Resources)
- Close other applications
- Restart Docker Desktop
- For production, use a machine with at least 4 GB RAM

### Checking Logs

**Docker:**
```bash
# Application logs
docker compose logs -f app

# Database logs
docker compose logs -f db

# Save logs to file
docker compose logs app > app-logs.txt
```

**Manual installation:**
```bash
# Console output shows logs when running with dotnet run

# For systemd service:
sudo journalctl -u breakingscoreboard -f

# Check application logs directory
ls -la logs/
```

### Getting Help

If you encounter issues:

1. Check the logs (see above)
2. Verify your configuration matches the examples
3. Ensure all prerequisites are installed
4. Review the [GitHub Issues](https://github.com/Alex-Witkowski/BreakingScoreBoard/issues)
5. Create a new issue with:
   - Your operating system and version
   - Installation method (Docker or manual)
   - Error messages or logs
   - Steps to reproduce

---

## Production Deployment

### Recommended Setup

For production environments, consider:

1. **Use HTTPS:** Configure a reverse proxy (nginx, Apache, or cloud load balancer)
2. **Secure Secrets:** Use environment variables or secrets management
3. **Managed Database:** Use cloud PostgreSQL (Azure Database, AWS RDS, etc.)
4. **Monitoring:** Set up logging aggregation and application monitoring
5. **Backups:** Automate database backups
6. **Resource Limits:** Configure appropriate memory and CPU limits

### Example nginx Configuration

```nginx
server {
    listen 80;
    server_name yourdomain.com;
    return 301 https://$server_name$request_uri;
}

server {
    listen 443 ssl http2;
    server_name yourdomain.com;

    ssl_certificate /path/to/cert.pem;
    ssl_certificate_key /path/to/key.pem;

    location / {
        proxy_pass http://localhost:8080;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

### Cloud Deployment

**Azure App Service:**
```bash
# Install Azure CLI
# Linux:
curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash

# Deploy
az webapp up --name breakingscoreboard --runtime "DOTNETCORE:9.0"
```

**AWS Elastic Beanstalk:**
```bash
# Install EB CLI
pip install awsebcli

# Deploy
eb init -p "64bit Amazon Linux 2023 v3.1.0 running .NET 9"
eb create breakingscoreboard-env
eb deploy
```

**Google Cloud Run:**
```bash
# Build and push to GCP Container Registry
gcloud builds submit --tag gcr.io/PROJECT-ID/breakingscoreboard

# Deploy
gcloud run deploy breakingscoreboard \
  --image gcr.io/PROJECT-ID/breakingscoreboard \
  --platform managed
```

---

## Summary

### Quick Reference

**Docker (Recommended):**
```bash
cp .env.example .env
# Edit .env with your settings
docker compose up -d
# Access: http://localhost:8080
```

**Manual Installation:**
```bash
# Install .NET 9 SDK + PostgreSQL
# Configure database
dotnet restore
dotnet ef database update
dotnet run --project src/BreakingScoreBoard.Api
# Access: http://localhost:8080
```

**Useful Commands:**
```bash
# Docker
docker compose up -d          # Start
docker compose down           # Stop
docker compose logs -f        # View logs
docker compose restart        # Restart

# .NET
dotnet run                    # Run
dotnet watch run              # Run with auto-reload
dotnet ef database update     # Apply migrations
dotnet test                   # Run tests
```

---

**Need help?** Open an issue on [GitHub](https://github.com/Alex-Witkowski/BreakingScoreBoard/issues)

**License:** MIT
