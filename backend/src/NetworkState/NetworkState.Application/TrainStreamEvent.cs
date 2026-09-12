using Common.Domain;

namespace NetworkState.Application;

/// Event pushed to a train's Orleans Stream on every state change (see ADR-0006). Two distinct
/// shapes are needed: a client must be able to tell "still here, ETA changed" apart from
/// "remove this dot".
public abstract record TrainStreamEvent
{
    public required TrainId Id { get; init; }
}

public sealed record TrainPositionChanged : TrainStreamEvent
{
    public required string Line { get; init; }
    public NaptanId? PreviousStopPoint { get; init; }
    public NaptanId? NextStopPoint { get; init; }
    public DateTimeOffset? ExpectedArrivalAtNext { get; init; }
    public double? Position { get; init; }
}

/// Published once a train's staleness reminder fires with no new prediction (see ADR-0006).
public sealed record TrainGone : TrainStreamEvent;
