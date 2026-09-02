using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

using TravelTracker.Data.Configuration;
using TravelTracker.Data.Models;
using TravelTracker.Data.Repositories;
using TravelTracker.Services.Interfaces;
using TravelTracker.Services.Models;
using TravelTracker.Services.Services;

namespace TravelTracker.Tests.Services;

public class TravelAssistantActionConfirmationServiceTests
{
    [Fact]
    public async Task ConfirmActionAsync_WhenActionBelongsToAnotherUser_ReturnsForbidden()
    {
        var action = new AssistantAction
        {
            Id = Guid.NewGuid(),
            UserId = 9,
            ThreadId = "thread-1",
            State = AssistantActionStates.Pending
        };
        var repository = new Mock<IAssistantActionRepository>();
        repository.Setup(value => value.BeginSerializableTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<IDbContextTransaction>());
        repository.Setup(value => value.GetForUpdateAsync(action.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(action);
        var service = CreateService(repository.Object);

        var result = await service.ConfirmActionAsync(
            new TravelAssistantUserContext(7, "external", "User", "user@example.com"),
            "thread-1",
            action.Id.ToString("N"));

        Assert.Equal("action_forbidden", result.ErrorCode);
    }

    [Fact]
    public async Task ConfirmActionAsync_WhenAlreadyConfirmed_ReturnsPriorLocationWithoutCreatingAnother()
    {
        var action = new AssistantAction
        {
            Id = Guid.NewGuid(),
            UserId = 7,
            ThreadId = "thread-1",
            State = AssistantActionStates.Confirmed,
            CreatedLocationId = 42,
            SanitizedSummary = "Add Buffalo House RV Park"
        };
        var repository = new Mock<IAssistantActionRepository>();
        repository.Setup(value => value.BeginSerializableTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<IDbContextTransaction>());
        repository.Setup(value => value.GetForUpdateAsync(action.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(action);
        var locations = new Mock<ILocationService>();
        var service = CreateService(repository.Object, locations.Object);

        var result = await service.ConfirmActionAsync(
            new TravelAssistantUserContext(7, "external", "User", "user@example.com"),
            "thread-1",
            action.Id.ToString("N"));

        Assert.Equal(42, result.CreatedLocationId);
        locations.Verify(
            value => value.CreateLocationAsync(It.IsAny<Location>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ConfirmActionAsync_WhenPendingPayloadIsValid_CreatesOneLocationAndClearsCiphertext()
    {
        var dataProtection = new EphemeralDataProtectionProvider();
        var protector = dataProtection.CreateProtector("TravelTracker.AssistantActions.CanonicalCommand.v1");
        var command = new AssistantActionCommand
        {
            LocationName = "Buffalo House RV Park",
            LocationTypeId = 3,
            LocationTypeName = "RV Park",
            VisitDate = "2026-08-31",
            Address = "2590 Guss Road",
            City = "Duluth",
            State = "MN",
            PostalCode = "55811",
            Latitude = 46.7867,
            Longitude = -92.1005,
            Comments = string.Empty,
            Rating = 5
        };
        var canonicalJson = JsonSerializer.Serialize(command, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var action = new AssistantAction
        {
            Id = Guid.NewGuid(),
            UserId = 7,
            ThreadId = "thread-1",
            ActionType = "create_location",
            CommandSchemaVersion = 1,
            State = AssistantActionStates.Pending,
            CanonicalCommandCiphertext = protector.Protect(canonicalJson),
            PayloadHashSha256 = SHA256.HashData(Encoding.UTF8.GetBytes(canonicalJson)),
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            SanitizedSummary = "Add Buffalo House RV Park"
        };
        var repository = new Mock<IAssistantActionRepository>();
        repository.Setup(value => value.BeginSerializableTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<IDbContextTransaction>());
        repository.Setup(value => value.GetForUpdateAsync(action.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(action);
        repository.Setup(value => value.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        var locations = new Mock<ILocationService>();
        locations.Setup(value => value.FindDuplicateAsync(
                7,
                command.LocationName,
                new DateOnly(2026, 8, 31),
                command.City,
                command.State,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Location?)null);
        locations.Setup(value => value.CreateLocationAsync(
                It.IsAny<Location>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Location location, CancellationToken _) =>
            {
                location.Id = 42;
                return location;
            });
        var service = new TravelAssistantActionConfirmationService(
            repository.Object,
            locations.Object,
            dataProtection,
            TimeProvider.System,
            Options.Create(new TravelAssistantOptions()),
            NullLogger<TravelAssistantActionConfirmationService>.Instance);

        var result = await service.ConfirmActionAsync(
            new TravelAssistantUserContext(7, "external", "User", "user@example.com"),
            "thread-1",
            action.Id.ToString("N"));

        Assert.Equal(42, result.CreatedLocationId);
        Assert.Equal(AssistantActionStates.Confirmed, action.State);
        Assert.Null(action.CanonicalCommandCiphertext);
    }

    [Fact]
    public async Task ConfirmActionAsync_WhenLocationCreateFails_ClearsRolledBackTracking()
    {
        var dataProtection = new EphemeralDataProtectionProvider();
        var protector = dataProtection.CreateProtector("TravelTracker.AssistantActions.CanonicalCommand.v1");
        var command = new AssistantActionCommand
        {
            LocationName = "Buffalo House RV Park",
            LocationTypeId = 3,
            LocationTypeName = "RV Park",
            VisitDate = "2026-08-31",
            Address = string.Empty,
            City = "Duluth",
            State = "MN",
            PostalCode = string.Empty,
            Latitude = 46.7867,
            Longitude = -92.1005,
            Comments = string.Empty,
            Rating = 0
        };
        var canonicalJson = JsonSerializer.Serialize(command, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        var action = new AssistantAction
        {
            Id = Guid.NewGuid(),
            UserId = 7,
            ThreadId = "thread-1",
            ActionType = "create_location",
            CommandSchemaVersion = 1,
            State = AssistantActionStates.Pending,
            CanonicalCommandCiphertext = protector.Protect(canonicalJson),
            PayloadHashSha256 = SHA256.HashData(Encoding.UTF8.GetBytes(canonicalJson)),
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };
        var repository = new Mock<IAssistantActionRepository>();
        repository.Setup(value => value.BeginSerializableTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<IDbContextTransaction>());
        repository.Setup(value => value.GetForUpdateAsync(action.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(action);
        repository.Setup(value => value.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        var locations = new Mock<ILocationService>();
        locations.Setup(value => value.FindDuplicateAsync(
                7,
                command.LocationName,
                new DateOnly(2026, 8, 31),
                command.City,
                command.State,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Location?)null);
        locations.Setup(value => value.CreateLocationAsync(
                It.IsAny<Location>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("simulated persistence failure"));
        var service = new TravelAssistantActionConfirmationService(
            repository.Object,
            locations.Object,
            dataProtection,
            TimeProvider.System,
            Options.Create(new TravelAssistantOptions()),
            NullLogger<TravelAssistantActionConfirmationService>.Instance);

        var result = await service.ConfirmActionAsync(
            new TravelAssistantUserContext(7, "external", "User", "user@example.com"),
            "thread-1",
            action.Id.ToString("N"));

        Assert.Equal("persistence_failed", result.ErrorCode);
        repository.Verify(value => value.ClearTracking(), Times.Once);
    }

    private static TravelAssistantActionConfirmationService CreateService(
        IAssistantActionRepository repository,
        ILocationService? locations = null) =>
        new(
            repository,
            locations ?? Mock.Of<ILocationService>(),
            new EphemeralDataProtectionProvider(),
            TimeProvider.System,
            Options.Create(new TravelAssistantOptions()),
            NullLogger<TravelAssistantActionConfirmationService>.Instance);
}
