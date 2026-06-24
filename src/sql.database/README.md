# Travel Tracker SQL Database Project

This folder contains the SQL Server Database Project (DACPAC) for the Travel Tracker application.

All application-owned objects are deployed into the `Travel` schema so the app can share a database with other applications without using `dbo`.

## Structure

- **Travel/** - Schema definition
  - Travel.sql - Creates the `Travel` schema

- **dbo/Tables/** - Database table definitions for objects deployed into the `Travel` schema
  - Locations.sql - Location tracking information
  - LocationTypes.sql - Location type definitions
  - DestinationTypes.sql - Destination type definitions
  - Destinations.sql - Destination information
  - Users.sql - User information

- **dbo/Stored Procedures/** - Stored procedures deployed into the `Travel` schema
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

Use a dedicated login/user that only has permissions on the `Travel` schema.

## Project Configuration

The project is configured in `.github/config/projects.yml` for use with GitHub Actions workflows.
