# Travel Tracker

A personal travel tracking and visualization web application built with Blazor and Azure.

## Overview

Travel Tracker is a responsive web application that enables users to track, visualize, and manage their travels within the United States. The application provides interactive map visualizations, location management, and travel statistics to help users document and reflect on their travel experiences.

### Key Features

- 📍 **Location Management** - Track visits with ratings, comments, and details
- 🗺️ **Interactive Maps** - Visualize travels using Azure Maps
- 📊 **Multiple Views** - Date range, state overview, and national parks modes
- 📤 **JSON Upload** - Import location data in bulk
- 🔐 **Secure Authentication** - Azure AD (Entra ID) integration
- 📱 **Responsive Design** - Works on desktop and mobile devices

## Project Status

**Phase 3: Development** 🔄 In Progress (October 2025)

Foundation development is complete. The application structure, data layer, service layer, and basic UI pages are implemented. Authentication and feature implementation are in progress.

## Documentation

📖 **[View Planning Documents →](./reports/)**

- **[Application Plan](./reports/Travel-Tracker-Application-Plan.md)** - Complete specification and development guide
- **[Status Report](./reports/Report-Status.md)** - Project status and progress tracking
- **[Reports README](./reports/README.md)** - Navigation guide for all planning documents

## Technology Stack

- **Frontend:** Blazor (Server + WebAssembly)
- **Backend:** C# / ASP.NET Core (.NET 8/9)
- **Database:** Azure Cosmos DB (NoSQL)
- **Authentication:** Azure AD (Entra ID)
- **Maps:** Azure Maps
- **Hosting:** Azure App Service
- **IaC:** Bicep
- **CI/CD:** GitHub Actions

## Development Phases

1. ✅ **Phase 1: Planning** - Complete
2. ⏸️ **Phase 2: Assessment** - N/A (new project)
3. 🔄 **Phase 3: Development** - In Progress (~20% complete)
   - ✅ Foundation & Project Structure
   - ✅ Data Models & Repositories
   - ✅ Service Layer
   - ✅ Basic UI Pages
   - 🔲 Authentication
   - 🔲 Feature Implementation
4. 🔲 **Phase 4: Infrastructure** - Not started
5. 🔲 **Phase 5: Deployment** - Not started
6. 🔲 **Phase 6: CI/CD Setup** - Not started

## Getting Started

### Prerequisites
- .NET 9 SDK
- Azure subscription (for Cosmos DB and Azure AD)
- Visual Studio 2022 or VS Code

### Running Locally

1. Clone the repository
2. Configure Azure services (Cosmos DB, Azure AD)
3. Update `appsettings.json` with your connection strings
4. Run the application:
   ```bash
   cd src/TravelTracker
   dotnet run
   ```

### Running Tests

```bash
dotnet test
```

## Current Implementation

### Completed
- ✅ Solution structure with 4 projects
- ✅ Data models (User, Location, NationalPark)
- ✅ Repository pattern with Cosmos DB
- ✅ Service layer for business logic
- ✅ Dependency injection configuration
- ✅ Basic UI pages and navigation
- ✅ Unit tests (3 passing)

### In Progress
- 🔄 Azure AD authentication
- 🔄 Location management features
- 🔄 Azure Maps integration

## Next Steps

- Complete authentication implementation
- Connect UI to backend services
- Implement location CRUD operations
- Add Azure Maps visualization
- Create infrastructure Bicep templates

## License

This project is licensed under the terms specified in the [LICENSE](./LICENSE) file.

---

For detailed information about the application design, architecture, and development roadmap, see the [planning documents](./reports/).