using System.Text;
using Moq;
using TravelTracker.Data.Models;
using TravelTracker.Data.Repositories;
using TravelTracker.Services.Interfaces;
using TravelTracker.Services.Services;

namespace TravelTracker.Tests.Services;

public class DataImportServiceTests
{
    private readonly Mock<ILocationService> _locationService = new();
    private readonly Mock<ILocationTypeRepository> _locationTypes = new();
    private readonly DataImportService _service;

    public DataImportServiceTests()
    {
        _locationTypes.Setup(repository => repository.GetByNameAsync("National Park"))
            .ReturnsAsync(new LocationType { Id = 1, Name = "National Park" });
        _locationTypes.Setup(repository => repository.GetByNameAsync("Other"))
            .ReturnsAsync(new LocationType { Id = 2, Name = "Other" });
        _service = new DataImportService(_locationService.Object, _locationTypes.Object);
    }

    [Fact]
    public async Task ValidateJson_WithValidLocations_ReturnsValid()
    {
        var result = await _service.ValidateJsonAsync(StreamOf("{\"locations\":[{\"name\":\"Park\",\"state\":\"CA\"}]}"));

        Assert.True(result.IsValid);
        Assert.Equal(1, result.RecordCount);
        Assert.Contains("1 locations are valid", result.ValidationMessages.Single(message => message.Contains("valid")));
    }

    [Fact]
    public async Task ValidateJson_WithInvalidStructure_ReturnsError()
    {
        var result = await _service.ValidateJsonAsync(StreamOf("{}"));

        Assert.False(result.IsValid);
        Assert.Contains("Missing 'locations' array", result.Errors.Single());
    }

    [Fact]
    public async Task ValidateJson_WithMalformedJson_ReturnsParsingError()
    {
        var result = await _service.ValidateJsonAsync(StreamOf("{bad"));

        Assert.False(result.IsValid);
        Assert.Contains("JSON parsing error", result.Errors.Single());
    }

    [Fact]
    public async Task ValidateJson_WithNoValidLocations_ReturnsError()
    {
        var result = await _service.ValidateJsonAsync(StreamOf("{\"locations\":[{\"name\":\"\",\"state\":\"\"}]}"));

        Assert.False(result.IsValid);
        Assert.Contains("No valid locations found", result.Errors.Single());
    }

    [Fact]
    public async Task ValidateCsv_WithValidAndInvalidRows_ReportsValidRows()
    {
        var csv = Header + Environment.NewLine + "Park,2024-01-01,,Comments,City CA 90210,1.2,3.4,National Park";
        var result = await _service.ValidateCsvAsync(StreamOf(csv));

        Assert.True(result.IsValid);
        Assert.Equal(1, result.RecordCount);
        Assert.Contains("1 rows appear valid and ready for import.", result.ValidationMessages);
    }

    [Fact]
    public async Task ValidateCsv_WithInvalidHeader_ReturnsError()
    {
        var result = await _service.ValidateCsvAsync(StreamOf("Location,Wrong"));

        Assert.False(result.IsValid);
        Assert.Contains("Invalid CSV header", result.Errors.Single());
    }

    [Fact]
    public async Task ValidateCsv_WithNoValidRows_ReturnsError()
    {
        var result = await _service.ValidateCsvAsync(StreamOf(Header + Environment.NewLine + "Park,,,,,,,"));

        Assert.False(result.IsValid);
        Assert.Contains("No valid data rows", result.Errors.Single());
    }

    [Fact]
    public async Task ImportJson_ImportsNewAndSkipsExistingLocations()
    {
        _locationService.Setup(service => service.GetAllLocationsAsync(7)).ReturnsAsync([
            new Location { Name = "Existing", StartDate = new DateTime(2024, 1, 1) }]);
        _locationService.Setup(service => service.CreateLocationAsync(It.IsAny<Location>()))
            .ReturnsAsync((Location location, CancellationToken _) => location);
        var json = "{\"locations\":[" +
            "{\"name\":\"Existing\",\"locationType\":\"National Park\",\"state\":\"CA\",\"startDate\":\"2024-01-01\"}," +
            "{\"name\":\"New Park\",\"locationType\":\"National Park\",\"state\":\"CA\",\"startDate\":\"2024-02-01\"}]}";

        var result = await _service.ImportFromJsonAsync(StreamOf(json), 7);

        Assert.True(result.Success);
        Assert.Equal(2, result.TotalRecords);
        Assert.Equal(1, result.ImportedRecords);
        Assert.Equal(1, result.SkippedRecords);
        _locationService.Verify(service => service.CreateLocationAsync(It.Is<Location>(location => location.Name == "New Park")), Times.Once);
    }

    [Fact]
    public async Task ImportCsv_WithMalformedRow_RecordsFailure()
    {
        _locationService.Setup(service => service.GetAllLocationsAsync(7)).ReturnsAsync([]);
        var csv = Header + Environment.NewLine + "Bad,not-a-date,,Comments,City CA 90210,nope,3.4,National Park";

        var result = await _service.ImportFromCsvAsync(StreamOf(csv), 7);

        Assert.False(result.Success);
        Assert.Equal(1, result.FailedRecords);
        Assert.Contains("Line 2", result.Errors.Single());
    }

    private const string Header = "Location,Arrival,Departure,Comments,Address,Latitude,Longitude,Type,TripName";

    private static MemoryStream StreamOf(string content) => new(Encoding.UTF8.GetBytes(content));
}