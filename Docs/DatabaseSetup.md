# SQL Server Database Setup

This document provides instructions for setting up and managing the SQL Server database for the Travel Tracker application.

## Overview

The Travel Tracker application uses SQL Server as its backend database with Entity Framework Core for data access. The database stores user information, location tracking data, and national park information.

## Prerequisites

You need one of the following SQL Server options:

1. **SQL Server Express** (free, recommended for development)
2. **SQL Server LocalDB** (lightweight, included with Visual Studio)
3. **SQL Server Developer Edition** (free, full-featured for development)
4. **Azure SQL Database** (cloud-based option)

## Installation

### Option 1: SQL Server Express (Recommended)

1. Download SQL Server Express from [Microsoft's website](https://www.microsoft.com/en-us/sql-server/sql-server-downloads)
2. Run the installer and choose "Basic" installation
3. Note the connection string provided at the end of installation

### Option 2: SQL Server LocalDB

LocalDB is included with Visual Studio 2022. If not installed:

```bash
# Download and install SQL Server Express with LocalDB
# Or install via Visual Studio Installer > Individual Components > SQL Server Express LocalDB
```

### Option 3: Azure SQL Database

1. Create an Azure SQL Database in the Azure Portal
2. Configure firewall rules to allow your IP address
3. Obtain the connection string from the Azure Portal

## Configuration

### Update Connection String

Edit `src/TravelTracker/appsettings.json`:

```json
{
  "SqlServer": {
    "ConnectionString": "YOUR_CONNECTION_STRING_HERE"
  }
}
```

### Connection String Examples

**SQL Server Express (Windows Authentication):**
```
Server=localhost\\SQLEXPRESS;Database=TravelTrackerDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true
```

**SQL Server LocalDB:**
```
Server=(localdb)\\mssqllocaldb;Database=TravelTrackerDB;Trusted_Connection=True;MultipleActiveResultSets=true
```

**SQL Server with SQL Authentication:**
```
Server=localhost;Database=TravelTrackerDB;User Id=sa;Password=YourPassword;TrustServerCertificate=True;MultipleActiveResultSets=true
```

**Azure SQL Database:**
```
Server=tcp:your-server.database.windows.net,1433;Database=TravelTrackerDB;User ID=yourusername;Password=yourpassword;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

## Database Creation and Migration

### Initial Setup

1. Install Entity Framework Core tools (if not already installed):
   ```bash
   dotnet tool install --global dotnet-ef
   ```

2. Navigate to the data project directory:
   ```bash
   cd src/TravelTracker.Data
   ```

3. Create the database and apply migrations:
   ```bash
   dotnet ef database update --startup-project ../TravelTracker
   ```

This will:
- Create the `TravelTrackerDB` database (if it doesn't exist)
- Create the following tables:
  - `Users` - User accounts and authentication data
  - `Locations` - Travel location tracking data
  - `NationalParks` - National park reference data

### Verify Database Creation

You can verify the database was created successfully using:

**SQL Server Management Studio (SSMS):**
1. Open SSMS
2. Connect to your SQL Server instance
3. Expand "Databases" and locate "TravelTrackerDB"
4. Expand the database to view tables

**Command Line (sqlcmd):**
```bash
sqlcmd -S localhost -E -Q "SELECT name FROM sys.databases WHERE name = 'TravelTrackerDB'"
```

## Database Schema

### Users Table

| Column | Type | Description |
|--------|------|-------------|
| Id | nvarchar(50) | Primary key, unique user identifier |
| Type | nvarchar(50) | Entity type (default: "user") |
| Username | nvarchar(200) | User's display name |
| Email | nvarchar(200) | User's email address |
| EntraIdUserId | nvarchar(50) | Azure AD (Entra ID) user identifier |
| CreatedDate | datetime2 | Timestamp when user was created |
| LastLoginDate | datetime2 | Timestamp of last login (nullable) |

**Indexes:**
- Primary key on `Id`
- Unique index on `EntraIdUserId`
- Index on `Email`

### Locations Table

| Column | Type | Description |
|--------|------|-------------|
| Id | nvarchar(50) | Primary key, unique location identifier |
| Type | nvarchar(50) | Entity type (default: "location") |
| UserId | nvarchar(50) | Foreign key to Users table |
| Name | nvarchar(200) | Location name |
| LocationType | nvarchar(100) | Type of location (e.g., city, attraction) |
| Address | nvarchar(300) | Street address |
| City | nvarchar(100) | City name |
| State | nvarchar(50) | State abbreviation |
| ZipCode | nvarchar(20) | Postal code |
| Latitude | float | Latitude coordinate |
| Longitude | float | Longitude coordinate |
| StartDate | datetime2 | Visit start date |
| EndDate | datetime2 | Visit end date (nullable) |
| Rating | int | User rating (0-5) |
| Comments | nvarchar(max) | User comments |
| TagsJson | nvarchar(2000) | JSON array of tags |
| CreatedDate | datetime2 | Timestamp when created |
| ModifiedDate | datetime2 | Timestamp when last modified |

**Indexes:**
- Primary key on `Id`
- Index on `UserId`
- Index on `State`
- Index on `StartDate`

### NationalParks Table

| Column | Type | Description |
|--------|------|-------------|
| Id | nvarchar(50) | Primary key, unique park identifier |
| Type | nvarchar(50) | Entity type (default: "nationalpark") |
| Name | nvarchar(200) | Park name |
| State | nvarchar(50) | State where park is located |
| Latitude | float | Latitude coordinate |
| Longitude | float | Longitude coordinate |
| Description | nvarchar(max) | Park description |

**Indexes:**
- Primary key on `Id`
- Index on `State`
- Index on `Name`

## Migration Management

### Creating New Migrations

When you modify the data models, create a new migration:

```bash
cd src/TravelTracker.Data
dotnet ef migrations add MigrationName --startup-project ../TravelTracker
```

### Applying Migrations

Apply pending migrations to the database:

```bash
cd src/TravelTracker.Data
dotnet ef database update --startup-project ../TravelTracker
```

### Rolling Back Migrations

Revert to a specific migration:

```bash
cd src/TravelTracker.Data
dotnet ef database update MigrationName --startup-project ../TravelTracker
```

### Removing the Last Migration

Remove the most recent migration (before applying it):

```bash
cd src/TravelTracker.Data
dotnet ef migrations remove --startup-project ../TravelTracker
```

### Viewing Migration SQL

See the SQL that will be executed by a migration:

```bash
cd src/TravelTracker.Data
dotnet ef migrations script --startup-project ../TravelTracker
```

## Common Issues and Troubleshooting

### Connection Failures

**Error:** "Cannot open database"
- Verify SQL Server is running
- Check connection string is correct
- Ensure firewall allows SQL Server connections

**Error:** "Login failed for user"
- Verify credentials in connection string
- Check SQL Server authentication mode
- Ensure user has appropriate permissions

### Migration Issues

**Error:** "Build failed"
- Ensure all projects build successfully: `dotnet build`
- Check for compilation errors in model classes

**Error:** "No DbContext was found"
- Verify startup project is specified: `--startup-project ../TravelTracker`
- Check Program.cs registers DbContext

### Performance Considerations

1. **Indexes**: The schema includes indexes on frequently queried columns
2. **Connection Pooling**: Enabled by default with MultipleActiveResultSets
3. **Async Operations**: All database operations use async/await pattern

## Backup and Restore

### Creating a Backup

```sql
BACKUP DATABASE TravelTrackerDB 
TO DISK = 'C:\Backup\TravelTrackerDB.bak'
WITH FORMAT, INIT, NAME = 'Full Backup of TravelTrackerDB';
```

### Restoring from Backup

```sql
RESTORE DATABASE TravelTrackerDB 
FROM DISK = 'C:\Backup\TravelTrackerDB.bak'
WITH REPLACE;
```

## Seeding Data

To add initial data (e.g., national parks), you can:

1. Create a seeding script in SQL
2. Add a DbContext seed method in `TravelTrackerDbContext.cs`
3. Use Entity Framework migrations with seed data

Example seed method:

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    
    modelBuilder.Entity<NationalPark>().HasData(
        new NationalPark 
        { 
            Id = "yellowstone", 
            Name = "Yellowstone National Park",
            State = "WY",
            Latitude = 44.4280,
            Longitude = -110.5885,
            Description = "America's first national park"
        }
    );
}
```

## Production Considerations

### For Azure SQL Database:

1. Use Azure Key Vault for connection strings
2. Enable automatic backups
3. Configure appropriate pricing tier
4. Set up geo-replication for disaster recovery
5. Use Azure AD authentication instead of SQL authentication
6. Enable Advanced Threat Protection

### General Best Practices:

1. Never commit connection strings with credentials to source control
2. Use environment variables or user secrets for sensitive data
3. Implement proper error handling and logging
4. Monitor database performance and query execution
5. Regularly update statistics and rebuild indexes
6. Test migrations in a development environment first

## Additional Resources

- [Entity Framework Core Documentation](https://docs.microsoft.com/en-us/ef/core/)
- [SQL Server Documentation](https://docs.microsoft.com/en-us/sql/sql-server/)
- [Azure SQL Database Documentation](https://docs.microsoft.com/en-us/azure/azure-sql/)
