using System.Net;
using System.Text;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using TravelTracker.Data.Configuration;
using TravelTracker.Services.Interfaces;
using TravelTracker.Services.Models;
using TravelTracker.Services.Services;

namespace TravelTracker.Tests.Services;

public class LocationLookupAPIServiceTests
{
    [Fact]
    public async Task LookupPlaceAsync_WhenExactProvidersAgree_ReturnsFoundOpaqueCandidateAndCachesResult()
    {
        var handler = new QueueHttpMessageHandler(
            JsonResponse("""
                [
                  {
                    "display_name": "Buffalo House RV Park, Duluth, Minnesota, United States",
                    "lat": "46.7867",
                    "lon": "-92.1005",
                    "namedetails": { "name": "Buffalo House RV Park" },
                    "address": { "city": "Duluth", "state": "Minnesota", "postcode": "55811" }
                  }
                ]
                """),
            JsonResponse("""
                { "features": [ { "geometry": { "coordinates": [ -92.1004, 46.7868 ] } } ] }
                """));
        var service = CreateService(handler);
        var request = new PlaceLookupRequest
        {
            Name = "Buffalo House RV Park",
            City = "Duluth",
            State = "MN"
        };

        var first = await service.LookupPlaceAsync(request);
        var second = await service.LookupPlaceAsync(request);

        Assert.Equal(PlaceLookupStatus.Found, first.Status);
        Assert.Equal(first.Candidates[0].CandidateId, second.Candidates[0].CandidateId);
        Assert.Equal(2, handler.Requests.Count);
        Assert.DoesNotContain("Buffalo", first.Candidates[0].CandidateId, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("TravelTracker/1.0", handler.Requests[0].Headers.UserAgent.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task LookupPlaceAsync_WhenProvidersDiverge_ReturnsAmbiguous()
    {
        var handler = new QueueHttpMessageHandler(
            JsonResponse("""
                [
                  {
                    "display_name": "Buffalo House RV Park, Duluth, Minnesota, United States",
                    "lat": "46.7867",
                    "lon": "-92.1005",
                    "namedetails": { "name": "Buffalo House RV Park" },
                    "address": { "city": "Duluth", "state": "Minnesota", "postcode": "55811" }
                  }
                ]
                """),
            JsonResponse("""
                { "features": [ { "geometry": { "coordinates": [ -80.0000, 35.0000 ] } } ] }
                """));
        var service = CreateService(handler);

        var result = await service.LookupPlaceAsync(new PlaceLookupRequest
        {
            Name = "Buffalo House RV Park",
            City = "Duluth",
            State = "MN"
        });

        Assert.Equal(PlaceLookupStatus.Ambiguous, result.Status);
        Assert.True(result.Candidates[0].CoordinateDivergenceDetected);
    }

    [Fact]
    public async Task LookupPlaceAsync_WhenSpecificQueryMisses_UsesBroaderFallback()
    {
        var handler = new QueueHttpMessageHandler(
            JsonResponse("[]"),
            JsonResponse("""
                [
                  {
                    "display_name": "Example Park, Austin, Texas, United States",
                    "lat": "30.2672",
                    "lon": "-97.7431",
                    "namedetails": { "name": "Example Park" },
                    "address": { "city": "Austin", "state": "Texas", "postcode": "78701" }
                  }
                ]
                """),
            JsonResponse("""
                { "features": [ { "geometry": { "coordinates": [ -97.7431, 30.2672 ] } } ] }
                """));
        var service = CreateService(handler);

        var result = await service.LookupPlaceAsync(new PlaceLookupRequest
        {
            Name = "Example Park",
            Address = "Unknown address",
            City = "Austin",
            State = "TX"
        });

        Assert.True(result.UsedBroaderFallback);
    }

    [Fact]
    public async Task LookupPlaceAsync_WhenCancelled_DoesNotCallProviders()
    {
        var handler = new QueueHttpMessageHandler();
        var service = CreateService(handler);
        using var source = new CancellationTokenSource();
        source.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            service.LookupPlaceAsync(
                new PlaceLookupRequest { Name = "Buffalo House RV Park" },
                source.Token));
    }

    [Fact]
    public async Task LookupPlaceAsync_WhenProviderIsUnavailable_ReturnsNotFound()
    {
        var handler = new QueueHttpMessageHandler(
            ErrorResponse(),
            ErrorResponse(),
            ErrorResponse(),
            ErrorResponse());
        var service = CreateService(handler);

        var result = await service.LookupPlaceAsync(new PlaceLookupRequest
        {
            Name = "Unavailable Place",
            City = "Austin",
            State = "TX"
        });

        Assert.Equal(PlaceLookupStatus.NotFound, result.Status);
        Assert.Empty(result.Candidates);
    }

    private static LocationLookupAPIService CreateService(QueueHttpMessageHandler handler)
    {
        var options = Options.Create(new TravelAssistantOptions
        {
            CandidateExpiryMinutes = 15,
            GeocodingMinimumIntervalMilliseconds = 0
        });
        return new LocationLookupAPIService(
            new HttpClient(handler),
            new PlaceCandidateStore(),
            new NoDelayRateLimiter(),
            TimeProvider.System,
            options,
            NullLogger<LocationLookupAPIService>.Instance);
    }

    private static HttpResponseMessage JsonResponse(string json) =>
        new(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

    private static HttpResponseMessage ErrorResponse() =>
        new(HttpStatusCode.ServiceUnavailable);

    private sealed class NoDelayRateLimiter : IPlaceLookupRateLimiter
    {
        public Task WaitAsync(CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class QueueHttpMessageHandler(params HttpResponseMessage[] responses) : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses = new(responses);

        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(_responses.Dequeue());
        }
    }
}
