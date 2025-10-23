# Travel Tracker API Documentation

This document describes the RESTful API endpoints available in the Travel Tracker application.

## Interactive API Documentation

The API includes **Swagger/OpenAPI** documentation for easy exploration and testing:

- **Swagger UI**: `http://localhost:5173/api/swagger`
  - Interactive interface to explore and test all API endpoints
  - View request/response schemas
  - Try out API calls directly from the browser

- **OpenAPI Specification**: `http://localhost:5173/swagger/v1/swagger.json`
  - Download the complete OpenAPI 3.0 specification
  - Import into API development tools (Postman, Insomnia, etc.)
  - Use for API client generation

## Model Context Protocol (MCP) Server

The Travel Tracker API is also available as an **MCP (Model Context Protocol) Server**, enabling AI agents and assistants to interact with your travel data using structured tools.

- **MCP Endpoint**: `http://localhost:5173/api/mcp`
  - Exposes travel tracking functionality as discoverable tools for AI agents
  - Supports standard MCP protocol for tool discovery and invocation
  - Enables integration with AI assistants like Claude, GitHub Copilot, and Azure AI agents
  - Compatible with Microsoft's Agent Framework

### MCP Tools Available

The MCP server exposes the following tool categories:

1. **Location Tools** - Create, read, update, and delete travel locations
2. **National Park Tools** - Query national parks information
3. **Chatbot Tools** - Interact with the AI travel assistant

For more information about MCP, see the [Microsoft Learn documentation](https://learn.microsoft.com/en-us/azure/developer/ai/intro-agents-mcp).

## Authentication

All location-related and visited parks endpoints require authentication. The API uses the existing authentication service to identify users.

## Endpoints

### Locations API

Base path: `/api/locations`

#### Get All Locations
- **GET** `/api/locations`
- **Description**: Get all locations for the authenticated user
- **Authentication**: Required
- **Response**: Array of Location objects

#### Get Location by ID
- **GET** `/api/locations/{id}`
- **Description**: Get a specific location by ID
- **Authentication**: Required
- **Parameters**:
  - `id` (path): Location ID
- **Response**: Location object
- **Status Codes**:
  - 200: Success
  - 401: Unauthorized
  - 404: Location not found

#### Get Locations by State
- **GET** `/api/locations/by-state/{state}`
- **Description**: Get all locations in a specific state
- **Authentication**: Required
- **Parameters**:
  - `state` (path): State abbreviation (e.g., "CA", "NY")
- **Response**: Array of Location objects

#### Get Locations by Date Range
- **GET** `/api/locations/by-date-range?startDate={startDate}&endDate={endDate}`
- **Description**: Get locations within a specific date range
- **Authentication**: Required
- **Query Parameters**:
  - `startDate` (query): Start date (ISO 8601 format)
  - `endDate` (query): End date (ISO 8601 format)
- **Response**: Array of Location objects
- **Status Codes**:
  - 200: Success
  - 400: Invalid date range (start date after end date)
  - 401: Unauthorized

#### Get Location Count by State
- **GET** `/api/locations/count-by-state`
- **Description**: Get the count of locations grouped by state
- **Authentication**: Required
- **Response**: Dictionary of state codes to counts
- **Example Response**:
```json
{
  "CA": 5,
  "NY": 3,
  "WY": 2
}
```

#### Create Location
- **POST** `/api/locations`
- **Description**: Create a new location
- **Authentication**: Required
- **Request Body**: Location object
- **Response**: Created Location object
- **Status Codes**:
  - 201: Created
  - 400: Invalid input or validation failed
  - 401: Unauthorized

**Example Request Body**:
```json
{
  "name": "Yellowstone RV Park",
  "locationType": "RV Park",
  "address": "123 Main St",
  "city": "West Yellowstone",
  "state": "MT",
  "zipCode": "59758",
  "latitude": 44.6596,
  "longitude": -111.1039,
  "startDate": "2024-06-15T00:00:00Z",
  "endDate": "2024-06-20T00:00:00Z",
  "rating": 5,
  "comments": "Beautiful park with great amenities",
  "tags": ["scenic", "family-friendly"]
}
```

#### Update Location
- **PUT** `/api/locations/{id}`
- **Description**: Update an existing location
- **Authentication**: Required
- **Parameters**:
  - `id` (path): Location ID
- **Request Body**: Location object with updated fields
- **Response**: Updated Location object
- **Status Codes**:
  - 200: Success
  - 400: Invalid input or ID mismatch
  - 401: Unauthorized
  - 404: Location not found

#### Delete Location
- **DELETE** `/api/locations/{id}`
- **Description**: Delete a location
- **Authentication**: Required
- **Parameters**:
  - `id` (path): Location ID
- **Status Codes**:
  - 204: No Content (success)
  - 401: Unauthorized
  - 404: Location not found

---

### National Parks API

Base path: `/api/nationalparks`

#### Get All National Parks
- **GET** `/api/nationalparks`
- **Description**: Get all national parks in the database
- **Authentication**: Not required
- **Response**: Array of NationalPark objects

#### Get National Park by ID
- **GET** `/api/nationalparks/{id}?state={state}`
- **Description**: Get a specific national park by ID and state
- **Authentication**: Not required
- **Parameters**:
  - `id` (path): Park ID
  - `state` (query): State abbreviation (required)
- **Response**: NationalPark object
- **Status Codes**:
  - 200: Success
  - 400: Missing state parameter
  - 404: Park not found

#### Get National Parks by State
- **GET** `/api/nationalparks/by-state/{state}`
- **Description**: Get all national parks in a specific state
- **Authentication**: Not required
- **Parameters**:
  - `state` (path): State abbreviation
- **Response**: Array of NationalPark objects

#### Get Visited National Parks
- **GET** `/api/nationalparks/visited`
- **Description**: Get national parks visited by the authenticated user
- **Authentication**: Required
- **Response**: Array of NationalPark objects
- **Status Codes**:
  - 200: Success
  - 401: Unauthorized

---

### Location Types API

Base path: `/api/locationtypes`

#### Get All Location Types
- **GET** `/api/locationtypes`
- **Description**: Get all available location types
- **Authentication**: Not required
- **Response**: Array of LocationType objects

#### Get Location Type by ID
- **GET** `/api/locationtypes/{id}`
- **Description**: Get a specific location type by ID
- **Authentication**: Not required
- **Parameters**:
  - `id` (path): Location Type ID
- **Response**: LocationType object
- **Status Codes**:
  - 200: Success
  - 404: Location type not found

#### Get Location Type by Name
- **GET** `/api/locationtypes/by-name/{name}`
- **Description**: Get a specific location type by name
- **Authentication**: Not required
- **Parameters**:
  - `name` (path): Location Type name
- **Response**: LocationType object
- **Status Codes**:
  - 200: Success
  - 404: Location type not found

---

## Data Models

### Location
```json
{
  "id": 1,
  "type": "location",
  "userId": 123,
  "name": "Yellowstone RV Park",
  "locationTypeId": 1,
  "locationType": "RV Park",
  "address": "123 Main St",
  "city": "West Yellowstone",
  "state": "MT",
  "zipCode": "59758",
  "latitude": 44.6596,
  "longitude": -111.1039,
  "startDate": "2024-06-15T00:00:00Z",
  "endDate": "2024-06-20T00:00:00Z",
  "rating": 5,
  "comments": "Beautiful park with great amenities",
  "tags": ["scenic", "family-friendly"],
  "tagsJson": "[\"scenic\",\"family-friendly\"]",
  "createdDate": "2024-06-01T00:00:00Z",
  "modifiedDate": "2024-06-01T00:00:00Z"
}
```

### NationalPark
```json
{
  "id": 1,
  "type": "nationalpark",
  "name": "Yellowstone",
  "state": "WY",
  "latitude": 44.4280,
  "longitude": -110.5885,
  "description": "America's first national park..."
}
```

### LocationType
```json
{
  "id": 1,
  "name": "RV Park",
  "description": "Recreational vehicle parking and camping facilities"
}
```

---

## Error Responses

All endpoints return consistent error response format:

```json
{
  "message": "Error description"
}
```

Common HTTP status codes:
- **200**: Success
- **201**: Created
- **204**: No Content (success with no response body)
- **400**: Bad Request (validation error)
- **401**: Unauthorized (authentication required)
- **404**: Not Found

---

## MCP Tools Reference

The MCP server exposes the following tools for AI agents:

### Location Management Tools

- **GetAllLocations** - Retrieve all travel locations for the authenticated user
- **GetLocationById** - Get details of a specific location by ID
- **GetLocationsByState** - Get all locations in a specific US state
- **GetLocationsByDateRange** - Get locations visited within a date range
- **GetLocationCountByState** - Get count of locations grouped by state
- **CreateLocation** - Create a new travel location
- **UpdateLocation** - Update details of an existing location
- **DeleteLocation** - Delete a location by ID

### National Park Tools

- **GetAllNationalParks** - Get a list of all US national parks
- **GetNationalParkById** - Get details of a specific national park
- **GetNationalParksByState** - Get all national parks in a specific state
- **GetVisitedNationalParks** - Get national parks visited by the user (requires authentication)

### Chatbot Tools

- **SendMessageToChatbot** - Send a message to the AI travel assistant and get a response

### Using MCP Tools

AI agents can discover and invoke these tools through the MCP protocol. Each tool includes:
- **Description** - What the tool does
- **Parameters** - Required and optional input parameters with descriptions
- **Authentication** - Whether the tool requires user authentication
- **Return Type** - The structure of the response data

Example MCP tool invocation flow:
1. Agent connects to `/api/mcp`
2. Agent lists available tools
3. Agent invokes tool with parameters
4. Server executes the tool and returns structured results

For implementation details, see:
- [Microsoft MCP Documentation](https://learn.microsoft.com/en-us/azure/developer/ai/intro-agents-mcp)
- [MCP C# SDK](https://github.com/modelcontextprotocol/csharp-sdk)
