using Microsoft.AspNetCore.DataProtection;
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

public class TravelAssistantActionServiceTests
{
    [Fact]
    public async Task PrepareAddLocationAsync_WhenEquivalentRequestRepeats_ReturnsSameEncryptedAction()
    {
        var user = new TravelAssistantUserContext(7, "external", "User", "user@example.com");
        var candidate = Candidate();
        var lookup = new Mock<ILocationLookupService>();
        lookup.Setup(service => service.ResolveCandidateAsync("candidate", It.IsAny<CancellationToken>()))
            .ReturnsAsync(candidate);
        var locations = new Mock<ILocationService>();
        locations.Setup(service => service.FindDuplicateAsync(
                user.UserId,
                candidate.Name,
                new DateOnly(2026, 8, 31),
                candidate.City,
                candidate.State,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Location?)null);
        var types = new Mock<ILocationTypeService>();
        types.Setup(service => service.ResolveLocationTypeAsync("rv park", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LocationTypeResolutionResult
            {
                Status = LocationTypeResolutionStatus.Found,
                LocationType = new LocationType { Id = 3, Name = "RV Park" },
                Matches = ["RV Park"]
            });
        var dates = new Mock<IRelativeDateResolver>();
        dates.Setup(service => service.Resolve("Yesterday", new DateOnly(2026, 8, 31)))
            .Returns(RelativeDateResolution.Resolved(new DateOnly(2026, 8, 31)));
        var repository = new Mock<IAssistantActionRepository>();
        AssistantAction? stored = null;
        repository.Setup(service => service.GetByIdempotencyKeyAsync(
                user.UserId,
                "thread-1",
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => stored);
        repository.Setup(service => service.AddAsync(It.IsAny<AssistantAction>(), It.IsAny<CancellationToken>()))
            .Callback<AssistantAction, CancellationToken>((action, _) => stored = action)
            .Returns(Task.CompletedTask);
        repository.Setup(service => service.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        var service = new TravelAssistantActionService(
            lookup.Object,
            locations.Object,
            types.Object,
            dates.Object,
            repository.Object,
            new EphemeralDataProtectionProvider(),
            TimeProvider.System,
            Options.Create(new TravelAssistantOptions()),
            NullLogger<TravelAssistantActionService>.Instance);

        var first = await service.PrepareAddLocationAsync(
            user,
            "thread-1",
            "candidate",
            candidate.Name,
            "rv park",
            "Yesterday",
            "2026-08-31");
        var second = await service.PrepareAddLocationAsync(
            user,
            "thread-1",
            "candidate",
            candidate.Name,
            "rv park",
            "Yesterday",
            "2026-08-31");

        Assert.Equal(first.ActionId, second.ActionId);
        Assert.NotNull(stored);
        Assert.Equal(32, stored.PayloadHashSha256.Length);
        Assert.DoesNotContain(candidate.Name, stored.CanonicalCommandCiphertext, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PrepareAddLocationAsync_WhenCandidateExpired_DoesNotPersistAction()
    {
        var lookup = new Mock<ILocationLookupService>();
        lookup.Setup(service => service.ResolveCandidateAsync("expired", It.IsAny<CancellationToken>()))
            .ReturnsAsync((PlaceCandidate?)null);
        var repository = new Mock<IAssistantActionRepository>();
        var service = new TravelAssistantActionService(
            lookup.Object,
            Mock.Of<ILocationService>(),
            Mock.Of<ILocationTypeService>(),
            Mock.Of<IRelativeDateResolver>(),
            repository.Object,
            new EphemeralDataProtectionProvider(),
            TimeProvider.System,
            Options.Create(new TravelAssistantOptions()),
            NullLogger<TravelAssistantActionService>.Instance);

        var result = await service.PrepareAddLocationAsync(
            new TravelAssistantUserContext(7, "external", "User", "user@example.com"),
            "thread-1",
            "expired",
            "Place",
            "RV Park",
            "Yesterday");

        Assert.Equal("candidate_expired", result.ErrorCode);
        repository.Verify(
            value => value.AddAsync(It.IsAny<AssistantAction>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    private static PlaceCandidate Candidate() =>
        new()
        {
            CandidateId = "candidate",
            Name = "Buffalo House RV Park",
            Address = "2590 Guss Road",
            City = "Duluth",
            State = "MN",
            PostalCode = "55811",
            Latitude = 46.7867,
            Longitude = -92.1005,
            Score = 1,
            ExpiresAtUtc = DateTime.UtcNow.AddMinutes(15)
        };
}
