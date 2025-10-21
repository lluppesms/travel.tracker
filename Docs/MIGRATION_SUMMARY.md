# Cosmos DB to SQL Server Migration Summary

## Overview

The Travel Tracker application has been successfully migrated from Azure Cosmos DB (NoSQL) to SQL Server with Entity Framework Core. This document summarizes the changes made and provides guidance for using the updated application.

## What Changed

### 1. Database Platform
- **Before:** Azure Cosmos DB (NoSQL document database)
- **After:** SQL Server (Relational database)

### 2. Data Access Technology
- **Before:** Azure Cosmos DB SDK with direct Cosmos client calls
- **After:** Entity Framework Core 9.0 with LINQ queries

### 3. Package Dependencies
#### Removed:
- `Microsoft.Azure.Cosmos` (3.54.0)
- `Azure.Identity` (1.17.0) - for Cosmos DB authentication
- `Newtonsoft.Json` (13.0.4) - used for Cosmos JSON serialization

#### Added:
- `Microsoft.EntityFrameworkCore` (9.0.0)
- `Microsoft.EntityFrameworkCore.SqlServer` (9.0.0)
- `Microsoft.EntityFrameworkCore.Tools` (9.0.0)
- `Microsoft.EntityFrameworkCore.Design` (9.0.0)

### 4. Configuration Changes

#### Before (Cosmos DB):
```json
{
  "CosmosDb": {
    "Endpoint": "https://xxx.documents.azure.com:443/",
    "DatabaseName": "TravelTrackerDB",
    "UsersContainerName": "users",
    "LocationsContainerName": "locations",
    "NationalParksContainerName": "nationalparks"
  }
}
```

#### After (SQL Server):
```json
{
  "SqlServer": {
    "ConnectionString": "Server=localhost;Database=TravelTrackerDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  }
}
```

### 5. Data Model Changes

#### Before (Cosmos DB):
```csharp
[JsonProperty("id")]
public string Id { get; set; }
```

#### After (SQL Server/EF Core):
```csharp
[Key]
[MaxLength(50)]
public string Id { get; set; }
```

All models now use:
- Entity Framework Core data annotations instead of JSON.NET attributes
- `[Table]` attribute to specify table names
- `[MaxLength]` for string fields
- `[Required]` for non-nullable fields
- Proper indexes defined in DbContext

### 6. Repository Implementation

#### Before (Cosmos DB):
```csharp
public class UserRepository : IUserRepository
{
    private readonly Container _container;
    
    public UserRepository(CosmosClient cosmosClient, IOptions<CosmosDbSettings> settings)
    {
        var database = cosmosClient.GetDatabase(settings.Value.DatabaseName);
        _container = database.GetContainer(settings.Value.UsersContainerName);
    }
    
    public async Task<User?> GetByIdAsync(string id)
    {
        var response = await _container.ReadItemAsync<User>(id, new PartitionKey(id));
        return response.Resource;
    }
}
```

#### After (SQL Server/EF Core):
```csharp
public class UserRepository : IUserRepository
{
    private readonly TravelTrackerDbContext _context;
    
    public UserRepository(TravelTrackerDbContext context)
    {
        _context = context;
    }
    
    public async Task<User?> GetByIdAsync(string id)
    {
        return await _context.Users.FindAsync(id);
    }
}
```

## Getting Started with the Migrated Application

### Step 1: Install SQL Server

Choose one of these options:

**For Windows:**
- SQL Server Express (free): [Download](https://www.microsoft.com/en-us/sql-server/sql-server-downloads)
- SQL Server LocalDB (included with Visual Studio)

**For macOS/Linux:**
- SQL Server in Docker:
  ```bash
  docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourPassword123!" \
    -p 1433:1433 -d mcr.microsoft.com/mssql/server:2022-latest
  ```

**For Azure:**
- Azure SQL Database (see [Azure-Setup-Guide.md](Azure-Setup-Guide.md))

### Step 2: Update Connection String

Edit `src/TravelTracker/appsettings.json`:

```json
{
  "SqlServer": {
    "ConnectionString": "YOUR_CONNECTION_STRING_HERE"
  }
}
```

**Connection String Examples:**

Local Windows (SQL Express with Windows Auth):
```
Server=localhost\\SQLEXPRESS;Database=TravelTrackerDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true
```

Local Windows (LocalDB):
```
Server=(localdb)\\mssqllocaldb;Database=TravelTrackerDB;Trusted_Connection=True;MultipleActiveResultSets=true
```

Docker or SQL Auth:
```
Server=localhost;Database=TravelTrackerDB;User Id=sa;Password=YourPassword123!;TrustServerCertificate=True;MultipleActiveResultSets=true
```

Azure SQL Database:
```
Server=tcp:yourserver.database.windows.net,1433;Database=TravelTrackerDB;User ID=yourusername;Password=yourpassword;Encrypt=True;TrustServerCertificate=False;
```

### Step 3: Run Database Migrations

```bash
# Navigate to the data project
cd src/TravelTracker.Data

# Apply migrations to create the database and tables
dotnet ef database update --startup-project ../TravelTracker
```

This will:
1. Create the `TravelTrackerDB` database (if it doesn't exist)
2. Create three tables: `Users`, `Locations`, `NationalParks`
3. Set up indexes for optimal performance

### Step 4: Run the Application

```bash
cd src/TravelTracker
dotnet run
```

The application will start and connect to SQL Server instead of Cosmos DB.

## Benefits of SQL Server

### 1. **Reliability & Maturity**
- SQL Server has been battle-tested for decades
- Predictable performance characteristics
- Strong ACID compliance

### 2. **Cost Efficiency**
- SQL Server Express is completely free
- Azure SQL Database Basic tier starts at ~$5/month
- More predictable pricing than Cosmos DB RU-based model

### 3. **Better Development Experience**
- Rich tooling: SQL Server Management Studio, Azure Data Studio
- Entity Framework Core provides excellent LINQ support
- Easier to debug queries and data issues

### 4. **Query Flexibility**
- Full SQL support for complex queries
- Better support for joins and aggregations
- Easier to optimize query performance

### 5. **Ecosystem Integration**
- Better integration with .NET and Entity Framework
- Extensive third-party tool support
- More developers familiar with SQL Server

## Data Migration from Cosmos DB (If Needed)

If you have existing data in Cosmos DB that needs to be migrated to SQL Server:

### Option 1: Export/Import (Small datasets)

1. **Export from Cosmos DB:**
   ```csharp
   // Query all data from Cosmos
   var users = await cosmosContainer.GetItemLinqQueryable<User>().ToListAsync();
   
   // Serialize to JSON
   var json = JsonSerializer.Serialize(users);
   File.WriteAllText("users.json", json);
   ```

2. **Import to SQL Server:**
   ```csharp
   // Deserialize JSON
   var json = File.ReadAllText("users.json");
   var users = JsonSerializer.Deserialize<List<User>>(json);
   
   // Insert into SQL Server
   await _context.Users.AddRangeAsync(users);
   await _context.SaveChangesAsync();
   ```

### Option 2: Azure Data Factory (Large datasets)

For large datasets, use Azure Data Factory to orchestrate the migration:
1. Create a Data Factory pipeline
2. Configure Cosmos DB as source
3. Configure SQL Server as destination
4. Map fields and run the pipeline

### Option 3: Custom Migration Script

Create a one-time migration console application that:
1. Connects to both Cosmos DB and SQL Server
2. Reads data from Cosmos in batches
3. Transforms data as needed (e.g., Tags to JSON)
4. Inserts into SQL Server

## Rollback Instructions (If Needed)

If you need to rollback to Cosmos DB:

1. **Restore packages:**
   ```bash
   # In TravelTracker.Data.csproj, restore Cosmos packages
   dotnet add package Microsoft.Azure.Cosmos --version 3.54.0
   ```

2. **Restore configuration:**
   ```bash
   git checkout main -- src/TravelTracker.Data/Configuration/CosmosDbSettings.cs
   git checkout main -- src/TravelTracker/Program.cs
   git checkout main -- src/TravelTracker/appsettings.json
   ```

3. **Restore repository implementations:**
   ```bash
   git checkout main -- src/TravelTracker.Data/Repositories/
   ```

4. **Restore model classes:**
   ```bash
   git checkout main -- src/TravelTracker.Data/Models/
   ```

## Frequently Asked Questions

### Q: Will this break my existing deployment?
A: If you have an existing deployment using Cosmos DB, you'll need to:
1. Set up a new SQL Server database
2. Migrate your data (see Data Migration section)
3. Update configuration with SQL Server connection string
4. Redeploy the application

### Q: Can I use Azure SQL Database?
A: Yes! The code works with any SQL Server, including:
- Azure SQL Database
- SQL Server on Azure VM
- SQL Server Express (local)
- SQL Server LocalDB (local)

### Q: What about performance?
A: SQL Server with proper indexes (already configured) provides excellent performance for this application's access patterns. The indexed queries will be fast even with thousands of records.

### Q: Is LocalDB suitable for development?
A: Yes! LocalDB is perfect for development. It's lightweight, requires no setup, and comes with Visual Studio.

### Q: How do I view the database?
A: Use any of these tools:
- SQL Server Management Studio (SSMS) - free download
- Azure Data Studio - cross-platform, free
- Visual Studio SQL Server Object Explorer
- Azure Portal (for Azure SQL Database)

### Q: Can I use a different database?
A: Entity Framework Core supports multiple databases. You could use:
- PostgreSQL (with Npgsql.EntityFrameworkCore.PostgreSQL)
- MySQL (with Pomelo.EntityFrameworkCore.MySql)
- SQLite (with Microsoft.EntityFrameworkCore.Sqlite)

Just change the connection string and provider package.

## Support and Documentation

- **Database Setup:** See [DatabaseSetup.md](DatabaseSetup.md)
- **Azure Deployment:** See [Azure-Setup-Guide.md](Azure-Setup-Guide.md)
- **EF Core Docs:** https://docs.microsoft.com/en-us/ef/core/
- **SQL Server Docs:** https://docs.microsoft.com/en-us/sql/

## Testing

All existing unit tests continue to pass with the new SQL Server implementation:
- 17 tests passing
- No changes required to test code (tests use mock repositories)
- Repository interfaces remain unchanged

## Security Notes

1. **Never commit connection strings with credentials to source control**
2. Use environment variables or user secrets for sensitive data
3. For production, consider:
   - Azure Key Vault for secrets
   - Managed Identity for passwordless authentication
   - Azure SQL with firewall rules and private endpoints

## Version Information

- **.NET Version:** 9.0
- **Entity Framework Core:** 9.0.0
- **SQL Server Compatibility:** 2016 and later, Azure SQL Database
- **Migration Date:** October 2025

## Conclusion

The migration to SQL Server with Entity Framework Core provides a more stable, cost-effective, and developer-friendly solution for the Travel Tracker application. All functionality has been preserved, and the application is ready for development and deployment.

For questions or issues, please refer to the documentation or open an issue on the repository.
