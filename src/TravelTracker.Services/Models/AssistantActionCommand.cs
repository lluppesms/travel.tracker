namespace TravelTracker.Services.Models;

public sealed record AssistantActionCommand
{
    public required string LocationName { get; init; }
    public required int LocationTypeId { get; init; }
    public required string LocationTypeName { get; init; }
    public required string VisitDate { get; init; }
    public required string Address { get; init; }
    public required string City { get; init; }
    public required string State { get; init; }
    public required string PostalCode { get; init; }
    public required double Latitude { get; init; }
    public required double Longitude { get; init; }
    public required string Comments { get; init; }
    public required int Rating { get; init; }
}
