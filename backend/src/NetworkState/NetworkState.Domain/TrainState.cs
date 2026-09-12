using Common.Domain;

namespace NetworkState.Domain;

/// A train's current position/journey state, as tracked by ITrainGrain. Immutable — see
/// NetworkState.Application for the logic that produces successor states via `with`.
public sealed record TrainState
{
    public required TrainId Id { get; init; }
    public required string Line { get; init; }
    public NaptanId? PreviousStopPoint { get; init; }
    public NaptanId? NextStopPoint { get; init; }
    public DateTimeOffset? ExpectedArrivalAtNext { get; init; }
    public DateTimeOffset LastUpdated { get; init; }
}
