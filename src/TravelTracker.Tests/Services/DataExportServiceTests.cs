using Moq;
using TravelTracker.Data.Models;
using TravelTracker.Services.Interfaces;
using TravelTracker.Services.Services;
using System.Text;
using System.Text.Json;

namespace TravelTracker.Tests.Services;

public class DataExportServiceTests
{
    private Mock<ILocationService> CreateMockLocationService()
    {
        var mock = new Mock<ILocationService>();
        var locations = new List<Location>
        {
            new Location
            {
                Id = 1,
                UserId = 123,
                Name = "Yellowstone NP",
                TripName = "Summer Vacation 2024",
                LocationType = "National Park",
                Address = "Yellowstone National Park, WY 82190",
                City = "Yellowstone",
                State = "WY",
                ZipCode = "82190",
                Latitude = 44.427963,
                Longitude = -110.588455,
                StartDate = new DateTime(2024, 6, 15),
                EndDate = new DateTime(2024, 6, 18),
                Rating = 5,
                Comments = "Amazing geysers",
                Tags = new List<string> { "national-park" }
            },
            new Location
            {
                Id = 2,
                UserId = 123,
                Name = "Grand Canyon NP",
                TripName = "Summer Vacation 2024",
                LocationType = "National Park",
                Address = "Grand Canyon, AZ 86023",
                City = "Grand Canyon",
                State = "AZ",
                ZipCode = "86023",
                Latitude = 36.1069,
                Longitude = -112.1129,
                StartDate = new DateTime(2024, 7, 1),
                EndDate = new DateTime(2024, 7, 3),
                Rating = 5,
                Comments = "Stunning views",
                Tags = new List<string> { "national-park" }
            }
        };

        mock.Setup(s => s.GetAllLocationsAsync(It.IsAny<int>()))
            .ReturnsAsync(locations);

        return mock;
    }

    [Fact]
    public async Task ExportToJsonAsync_ReturnsValidJson()
    {
        // Arrange
        int userId = 123;
        var mockLocationService = CreateMockLocationService();
        var service = new DataExportService(mockLocationService.Object);

        // Act
        var stream = await service.ExportToJsonAsync(userId);

        // Assert
        Assert.NotNull(stream);
        stream.Position = 0;
        using var reader = new StreamReader(stream);
        var json = await reader.ReadToEndAsync();

        Assert.NotEmpty(json);
        
        // Verify it's valid JSON
        var document = JsonDocument.Parse(json);
        Assert.True(document.RootElement.TryGetProperty("locations", out var locationsProperty));
        
        var locationsArray = locationsProperty.EnumerateArray().ToList();
        Assert.Equal(2, locationsArray.Count);
        
        // Verify first location
        var firstLocation = locationsArray[0];
        Assert.Equal("Yellowstone NP", firstLocation.GetProperty("name").GetString());
        Assert.Equal("Summer Vacation 2024", firstLocation.GetProperty("tripName").GetString());
        Assert.Equal("National Park", firstLocation.GetProperty("locationType").GetString());
        
        mockLocationService.Verify(s => s.GetAllLocationsAsync(userId), Times.Once);
    }

    [Fact]
    public async Task ExportToCsvAsync_ReturnsValidCsv()
    {
        // Arrange
        int userId = 123;
        var mockLocationService = CreateMockLocationService();
        var service = new DataExportService(mockLocationService.Object);

        // Act
        var stream = await service.ExportToCsvAsync(userId);

        // Assert
        Assert.NotNull(stream);
        stream.Position = 0;
        using var reader = new StreamReader(stream);
        var csv = await reader.ReadToEndAsync();

        Assert.NotEmpty(csv);
        
        var lines = csv.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.True(lines.Length >= 3); // Header + 2 data rows
        
        // Verify header
        var header = lines[0];
        Assert.Equal("Location,Arrival,Departure,Comments,Address,Latitude,Longitude,Type,TripName", header);
        
        // Verify first data row contains expected data
        var firstDataRow = lines[1];
        Assert.Contains("Yellowstone NP", firstDataRow);
        Assert.Contains("2024-06-15", firstDataRow);
        Assert.Contains("2024-06-18", firstDataRow);
        
        mockLocationService.Verify(s => s.GetAllLocationsAsync(userId), Times.Once);
    }

    [Fact]
    public async Task ExportToCsvAsync_EscapesFieldsWithCommas()
    {
        // Arrange
        int userId = 123;
        var mockLocationService = new Mock<ILocationService>();
        var locations = new List<Location>
        {
            new Location
            {
                Id = 1,
                UserId = 123,
                Name = "Test, Location",
                TripName = "Trip, Name",
                LocationType = "RV Park",
                Address = "123 Main St, City, State 12345",
                City = "City",
                State = "CA",
                ZipCode = "12345",
                Latitude = 37.7749,
                Longitude = -122.4194,
                StartDate = new DateTime(2024, 1, 1),
                EndDate = null,
                Rating = 4,
                Comments = "Nice place, but expensive",
                Tags = new List<string>()
            }
        };
        mockLocationService.Setup(s => s.GetAllLocationsAsync(It.IsAny<int>()))
            .ReturnsAsync(locations);
        var service = new DataExportService(mockLocationService.Object);

        // Act
        var stream = await service.ExportToCsvAsync(userId);

        // Assert
        stream.Position = 0;
        using var reader = new StreamReader(stream);
        var csv = await reader.ReadToEndAsync();

        // Fields with commas should be quoted
        Assert.Contains("\"Test, Location\"", csv);
        Assert.Contains("\"Trip, Name\"", csv);
        Assert.Contains("\"123 Main St, City, State 12345\"", csv);
        Assert.Contains("\"Nice place, but expensive\"", csv);
    }

    [Fact]
    public async Task ExportToJsonAsync_EmptyLocations_ReturnsEmptyArray()
    {
        // Arrange
        int userId = 123;
        var mockLocationService = new Mock<ILocationService>();
        mockLocationService.Setup(s => s.GetAllLocationsAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<Location>());
        var service = new DataExportService(mockLocationService.Object);

        // Act
        var stream = await service.ExportToJsonAsync(userId);

        // Assert
        Assert.NotNull(stream);
        stream.Position = 0;
        using var reader = new StreamReader(stream);
        var json = await reader.ReadToEndAsync();

        var document = JsonDocument.Parse(json);
        Assert.True(document.RootElement.TryGetProperty("locations", out var locationsProperty));
        
        var locationsArray = locationsProperty.EnumerateArray().ToList();
        Assert.Empty(locationsArray);
    }

    [Fact]
    public async Task ExportToCsvAsync_EmptyLocations_ReturnsHeaderOnly()
    {
        // Arrange
        int userId = 123;
        var mockLocationService = new Mock<ILocationService>();
        mockLocationService.Setup(s => s.GetAllLocationsAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<Location>());
        var service = new DataExportService(mockLocationService.Object);

        // Act
        var stream = await service.ExportToCsvAsync(userId);

        // Assert
        stream.Position = 0;
        using var reader = new StreamReader(stream);
        var csv = await reader.ReadToEndAsync();

        var lines = csv.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        Assert.Single(lines); // Only header row
        Assert.Equal("Location,Arrival,Departure,Comments,Address,Latitude,Longitude,Type,TripName", lines[0]);
    }
}
