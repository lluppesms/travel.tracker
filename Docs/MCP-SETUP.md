# Model Context Protocol (MCP) Server Setup

This document describes how to use the Travel Tracker MCP server to integrate with AI agents and assistants.

## What is MCP?

Model Context Protocol (MCP) is an open protocol developed to standardize how AI applications interact with external tools, data sources, and services. It provides:

- **Tool Discovery** - AI agents can discover available tools and their parameters
- **Structured Invocation** - Tools are called with type-safe parameters
- **Standardized Communication** - Works across different AI platforms and agents
- **Real-time Integration** - Direct access to live data and services

## MCP Server Endpoint

The Travel Tracker MCP server is available at:

```
http://localhost:5173/api/mcp
```

When deployed to Azure, it will be available at:

```
https://your-app-name.azurewebsites.net/api/mcp
```

## Available Tools

The MCP server exposes three categories of tools:

### 1. Location Management Tools

These tools allow AI agents to manage travel locations on behalf of authenticated users.

#### GetAllLocations
- **Description**: Get all travel locations for the authenticated user
- **Authentication**: Required
- **Parameters**: None
- **Returns**: Array of Location objects

#### GetLocationById
- **Description**: Get details of a specific location
- **Authentication**: Required
- **Parameters**:
  - `locationId` (int) - The unique identifier of the location
- **Returns**: Location object or null

#### GetLocationsByState
- **Description**: Get all locations in a specific US state
- **Authentication**: Required
- **Parameters**:
  - `state` (string) - Two-letter US state code (e.g., "CA", "NY")
- **Returns**: Array of Location objects

#### GetLocationsByDateRange
- **Description**: Get locations visited within a specific date range
- **Authentication**: Required
- **Parameters**:
  - `startDate` (DateTime) - Start date in ISO 8601 format
  - `endDate` (DateTime) - End date in ISO 8601 format
- **Returns**: Array of Location objects

#### GetLocationCountByState
- **Description**: Get count of locations grouped by state
- **Authentication**: Required
- **Parameters**: None
- **Returns**: Dictionary<string, int> mapping state codes to counts

#### CreateLocation
- **Description**: Create a new travel location
- **Authentication**: Required
- **Parameters**:
  - `name` (string, required) - Name of the location
  - `locationType` (string, required) - Type (e.g., "RV Park", "National Park")
  - `address` (string, required) - Street address
  - `city` (string, required) - City name
  - `state` (string, required) - Two-letter state code
  - `zipCode` (string, required) - ZIP/postal code
  - `latitude` (double?, optional) - Latitude coordinate
  - `longitude` (double?, optional) - Longitude coordinate
  - `startDate` (DateTime?, optional) - Visit start date
  - `endDate` (DateTime?, optional) - Visit end date
  - `rating` (int?, optional) - Rating from 1-5 stars
  - `comments` (string?, optional) - Comments or notes
- **Returns**: Created Location object or null

#### UpdateLocation
- **Description**: Update an existing location
- **Authentication**: Required
- **Parameters**:
  - `locationId` (int, required) - ID of location to update
  - All other parameters are optional and will only update if provided
- **Returns**: Updated Location object or null

#### DeleteLocation
- **Description**: Delete a location
- **Authentication**: Required
- **Parameters**:
  - `locationId` (int) - ID of location to delete
- **Returns**: Boolean indicating success

### 2. National Park Tools

These tools provide information about US national parks.

#### GetAllNationalParks
- **Description**: Get a list of all US national parks in the database
- **Authentication**: Not required
- **Parameters**: None
- **Returns**: Array of NationalPark objects

#### GetNationalParkById
- **Description**: Get details of a specific national park
- **Authentication**: Not required
- **Parameters**:
  - `parkId` (int) - The unique identifier of the park
  - `state` (string) - Two-letter state code where the park is located
- **Returns**: NationalPark object or null

#### GetNationalParksByState
- **Description**: Get all national parks in a specific state
- **Authentication**: Not required
- **Parameters**:
  - `state` (string) - Two-letter US state code
- **Returns**: Array of NationalPark objects

#### GetVisitedNationalParks
- **Description**: Get national parks the user has visited
- **Authentication**: Required
- **Parameters**: None
- **Returns**: Array of NationalPark objects

### 3. Chatbot Tools

These tools enable AI agents to interact with the Travel Tracker chatbot.

#### SendMessageToChatbot
- **Description**: Send a message to the AI travel assistant
- **Authentication**: Required
- **Parameters**:
  - `message` (string, required) - The message or question
  - `threadId` (string?, optional) - Thread ID to continue a conversation
  - `lastMessageDate` (DateTimeOffset?, optional) - Timestamp of last message
- **Returns**: ChatbotResponse object with message, threadId, and timestamp

## Using the MCP Server

### From AI Agents (Claude, GitHub Copilot, etc.)

Configure your AI agent to connect to the MCP server:

1. Add the MCP server configuration to your AI agent settings
2. Provide the server URL: `http://localhost:5173/api/mcp`
3. Ensure authentication is configured (Azure AD credentials)
4. The agent will automatically discover available tools

### From Microsoft Agent Framework

```csharp
// Example: Configure MCP client in Agent Framework
var mcpClient = new McpClient("http://localhost:5173/api/mcp");

// Discover available tools
var tools = await mcpClient.ListToolsAsync();

// Invoke a tool
var result = await mcpClient.InvokeToolAsync("GetAllLocations", new {});
```

### From Custom Applications

Use the MCP C# SDK to connect to the server:

```csharp
using ModelContextProtocol;

var client = new McpClient(new Uri("http://localhost:5173/api/mcp"));

// List available tools
var tools = await client.Tools.ListAsync();
foreach (var tool in tools)
{
    Console.WriteLine($"Tool: {tool.Name} - {tool.Description}");
}

// Call a tool
var response = await client.Tools.CallAsync("GetLocationsByState", 
    new Dictionary<string, object>
    {
        ["state"] = "CA"
    });
```

## Authentication

Most tools require authentication. The MCP server uses the same Azure AD authentication as the REST API:

1. User must be authenticated via Azure AD
2. Authentication token is validated for each request
3. User context is used to filter and secure data access

For tools that don't require authentication (like national park queries), they are available to all clients.

## Error Handling

The MCP server returns structured errors:

- **UnauthorizedAccessException** - User not authenticated
- **ArgumentException** - Invalid parameters provided
- **InvalidOperationException** - Operation failed (e.g., location not found)

## Testing the MCP Server

### Using Postman

Postman has built-in MCP support:

1. Create a new MCP connection in Postman
2. Enter the server URL: `http://localhost:5173/api/mcp`
3. Browse available tools
4. Test tool invocations with sample parameters

### Using cURL

```bash
# List available tools
curl -X POST http://localhost:5173/api/mcp \
  -H "Content-Type: application/json" \
  -d '{"method": "tools/list"}'

# Call a tool
curl -X POST http://localhost:5173/api/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "method": "tools/call",
    "params": {
      "name": "GetAllNationalParks",
      "arguments": {}
    }
  }'
```

## Best Practices

1. **Authentication** - Always ensure users are properly authenticated before invoking protected tools
2. **Error Handling** - Implement proper error handling for tool invocations
3. **Rate Limiting** - Consider implementing rate limiting for production deployments
4. **Logging** - Enable logging to track tool usage and diagnose issues
5. **Validation** - Tools perform input validation, but clients should also validate before calling

## Resources

- [Microsoft MCP Documentation](https://learn.microsoft.com/en-us/azure/developer/ai/intro-agents-mcp)
- [MCP C# SDK GitHub](https://github.com/modelcontextprotocol/csharp-sdk)
- [MCP Specification](https://spec.modelcontextprotocol.io/)
- [Travel Tracker API Documentation](./API-Documentation.md)

## Troubleshooting

### Server not responding
- Check that the application is running
- Verify the MCP endpoint is accessible at `/api/mcp`
- Check server logs for errors

### Authentication failures
- Ensure Azure AD is properly configured
- Verify user has valid authentication token
- Check that token includes required scopes

### Tools not appearing
- Verify SQL Server connection is configured
- Check that all MCP tool classes are registered in Program.cs
- Review server startup logs for MCP initialization messages

### Tool invocation errors
- Validate all required parameters are provided
- Check parameter types match expected types
- Review error messages for specific validation failures
