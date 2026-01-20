# Travel Tracker SQL Database Project

This folder contains the SQL Server Database Project (DACPAC) for the Travel Tracker application.

## Structure

- **dbo/Tables/** - Database table definitions
  - Locations.sql - Location tracking information
  - LocationTypes.sql - Location type definitions
  - DestinationTypes.sql - Destination type definitions
  - Destinations.sql - Destination information
  - Users.sql - User information

- **dbo/Stored Procedures/** - Stored procedures
  - usp_LocationSummary.sql - Gets location summary for a user

- **Patch/** - Post-deployment scripts and patches

## Building the DACPAC

### Using MSBuild (Windows)
```bash
msbuild sql.database.sln /p:Configuration=Release
```

### Using dotnet build
```bash
dotnet build sql.database.sqlproj
```

The DACPAC file will be generated in `bin/Release/sql.database.dacpac`

## Deploying the DACPAC

### Using GitHub Actions
Use the workflow `4-build-deploy-dacpac.yml` to build and deploy the database.

### Using SqlPackage
```bash
sqlpackage /Action:Publish /SourceFile:sql.database.dacpac /TargetServerName:your-server.database.windows.net /TargetDatabaseName:TravelTrackerDB /TargetUser:username /TargetPassword:password
```

## Project Configuration

The project is configured in `.github/config/projects.yml` for use with GitHub Actions workflows.
