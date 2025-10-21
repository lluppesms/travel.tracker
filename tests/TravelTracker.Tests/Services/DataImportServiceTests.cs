using Moq;
using TravelTracker.Data.Models;
using TravelTracker.Services.Interfaces;
using TravelTracker.Services.Services;

namespace TravelTracker.Tests.Services;

public class DataImportServiceTests
{
    [Fact]
    public async Task ImportFromJsonAsync_ValidJson_ImportsLocations()
    {
        // Arrange
        var userId = "user123";
        var jsonContent = @"{
            ""locations"": [
                {
                    ""name"": ""Test Location"",
                    ""state"": ""CA"",
                    ""latitude"": 37.7749,
                    ""longitude"": -122.4194,
                    ""startDate"": ""2024-01-01""
                }
            ]
        }";

        var mockLocationService = new Mock<ILocationService>();
        mockLocationService.Setup(s => s.CreateLocationAsync(It.IsAny<Location>()))
            .ReturnsAsync((Location loc) => loc);

        var service = new DataImportService(mockLocationService.Object);
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonContent));

        // Act
        var result = await service.ImportFromJsonAsync(stream, userId);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(1, result.TotalRecords);
        Assert.Equal(1, result.ImportedRecords);
        Assert.Equal(0, result.FailedRecords);
        mockLocationService.Verify(s => s.CreateLocationAsync(It.IsAny<Location>()), Times.Once);
    }

    [Fact]
    public async Task ImportFromJsonAsync_InvalidJson_ReturnsErrors()
    {
        // Arrange
        var userId = "user123";
        var invalidJson = @"{ ""invalid"": ""structure"" }";

        var mockLocationService = new Mock<ILocationService>();
        var service = new DataImportService(mockLocationService.Object);
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(invalidJson));

        // Act
        var result = await service.ImportFromJsonAsync(stream, userId);

        // Assert
        Assert.False(result.Success);
        Assert.NotEmpty(result.Errors);
        mockLocationService.Verify(s => s.CreateLocationAsync(It.IsAny<Location>()), Times.Never);
    }

    [Fact]
    public async Task ImportFromCsvAsync_ValidCsv_ImportsLocations()
    {
        // Arrange
        var userId = "user123";
        var csvContent = @"RowId,Location,Arrival,Departure,Comments,Address,Latitude,Longitude
1,Yellowstone NP,2024-06-15,2024-06-18,Amazing geysers,""Yellowstone National Park, WY 82190"",44.427963,-110.588455";

        var mockLocationService = new Mock<ILocationService>();
        mockLocationService.Setup(s => s.CreateLocationAsync(It.IsAny<Location>()))
            .ReturnsAsync((Location loc) => loc);

        var service = new DataImportService(mockLocationService.Object);
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await service.ImportFromCsvAsync(stream, userId);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(1, result.TotalRecords);
        Assert.Equal(1, result.ImportedRecords);
        Assert.Equal(0, result.FailedRecords);
        mockLocationService.Verify(s => s.CreateLocationAsync(It.IsAny<Location>()), Times.Once);
    }

    [Fact]
    public async Task ImportFromCsvAsync_InvalidHeader_ReturnsError()
    {
        // Arrange
        var userId = "user123";
        var csvContent = @"WrongHeader1,WrongHeader2
data1,data2";

        var mockLocationService = new Mock<ILocationService>();
        var service = new DataImportService(mockLocationService.Object);
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await service.ImportFromCsvAsync(stream, userId);

        // Assert
        Assert.False(result.Success);
        Assert.NotEmpty(result.Errors);
        Assert.Contains("Invalid CSV header", result.Errors[0]);
        mockLocationService.Verify(s => s.CreateLocationAsync(It.IsAny<Location>()), Times.Never);
    }

    [Fact]
    public async Task ValidateJsonAsync_ValidJson_ReturnsValid()
    {
        // Arrange
        var jsonContent = @"{
            ""locations"": [
                {
                    ""name"": ""Test Location"",
                    ""state"": ""CA"",
                    ""latitude"": 37.7749,
                    ""longitude"": -122.4194
                }
            ]
        }";

        var mockLocationService = new Mock<ILocationService>();
        var service = new DataImportService(mockLocationService.Object);
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonContent));

        // Act
        var result = await service.ValidateJsonAsync(stream);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(1, result.RecordCount);
        Assert.NotEmpty(result.ValidationMessages);
    }

    [Fact]
    public async Task ValidateCsvAsync_ValidCsv_ReturnsValid()
    {
        // Arrange
        var csvContent = @"RowId,Location,Arrival,Departure,Comments,Address,Latitude,Longitude
1,Test Location,2024-01-01,2024-01-02,Great place,""123 Main St, CA"",37.7749,-122.4194";

        var mockLocationService = new Mock<ILocationService>();
        var service = new DataImportService(mockLocationService.Object);
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await service.ValidateCsvAsync(stream);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(1, result.RecordCount);
        Assert.NotEmpty(result.ValidationMessages);
    }

    [Fact]
    public async Task ImportFromCsvAsync_ExtractsStateFromAddress()
    {
        // Arrange
        var userId = "user123";
        var csvContent = @"RowId,Location,Arrival,Departure,Comments,Address,Latitude,Longitude
1,Test Location,2024-01-01,2024-01-02,Comment,""City Name, CA 12345"",37.7749,-122.4194";

        Location? capturedLocation = null;
        var mockLocationService = new Mock<ILocationService>();
        mockLocationService.Setup(s => s.CreateLocationAsync(It.IsAny<Location>()))
            .Callback<Location>(loc => capturedLocation = loc)
            .ReturnsAsync((Location loc) => loc);

        var service = new DataImportService(mockLocationService.Object);
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await service.ImportFromCsvAsync(stream, userId);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(capturedLocation);
        Assert.Equal("CA", capturedLocation.State);
        Assert.Equal("Test Location", capturedLocation.Name);
    }

    [Fact]
    public async Task ImportFromCsvAsync_MultipleLocations_ImportsAll()
    {
        // Arrange
        var userId = "user123";
        var csvContent = @"RowId,Location,Arrival,Departure,Comments,Address,Latitude,Longitude
1,Location 1,2024-01-01,2024-01-02,Comment 1,""Address 1, CA"",37.7749,-122.4194
2,Location 2,2024-02-01,2024-02-02,Comment 2,""Address 2, NY"",40.7128,-74.0060
3,Location 3,2024-03-01,2024-03-02,Comment 3,""Address 3, TX"",29.7604,-95.3698";

        var mockLocationService = new Mock<ILocationService>();
        mockLocationService.Setup(s => s.CreateLocationAsync(It.IsAny<Location>()))
            .ReturnsAsync((Location loc) => loc);

        var service = new DataImportService(mockLocationService.Object);
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvContent));

        // Act
        var result = await service.ImportFromCsvAsync(stream, userId);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(3, result.TotalRecords);
        Assert.Equal(3, result.ImportedRecords);
        Assert.Equal(0, result.FailedRecords);
        mockLocationService.Verify(s => s.CreateLocationAsync(It.IsAny<Location>()), Times.Exactly(3));
    }
}
