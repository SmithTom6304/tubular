using Common.Domain;

namespace NetworkState.Application;

/// Point-in-time view of a train, returned by ITrainGrain.GetSnapshot() for GET /train/{id}.
public sealed record TrainSnapshot
{
    public required TrainId Id { get; init; }
    public required string Line { get; init; }
    public NaptanId? PreviousStopPoint { get; init; }
    public NaptanId? NextStopPoint { get; init; }
    public DateTimeOffset? ExpectedArrivalAtNext { get; init; }
    public double? Position { get; init; }
    public DateTimeOffset LastUpdated { get; init; }
}
