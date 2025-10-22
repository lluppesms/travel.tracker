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

## Future Enhancements

These APIs are designed to be ready for:
- MCP (Model Context Protocol) integration
- Agent Framework integration
- Chatbot interface development

The endpoints follow RESTful conventions and return JSON data suitable for these future integrations.
