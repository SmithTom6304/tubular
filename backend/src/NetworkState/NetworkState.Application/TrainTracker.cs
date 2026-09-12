using Common.Domain;
using NetworkState.Domain;

namespace NetworkState.Application;

public sealed class TrainTracker : ITrainTracker
{
    public (TrainState State, SegmentTraversal? Traversal) Apply(TrainState current, NaptanId stopPoint, DateTimeOffset expectedArrival, DateTimeOffset now)
    {
        var previousStopPoint = current.PreviousStopPoint;
        SegmentTraversal? traversal = null;

        if (current.NextStopPoint is { } previousNext
            && current.ExpectedArrivalAtNext is { } previousEta
            && previousNext != stopPoint)
        {
            previousStopPoint = previousNext;
            traversal = new SegmentTraversal(current.Line, previousNext, stopPoint, expectedArrival - previousEta);
        }

        var updated = current with
        {
            PreviousStopPoint = previousStopPoint,
            NextStopPoint = stopPoint,
            ExpectedArrivalAtNext = expectedArrival,
            LastUpdated = now,
        };

        return (updated, traversal);
    }

    public double? PositionBetweenStops(TrainState state, DateTimeOffset now, TimeSpan averageSegmentDuration)
    {
        if (state.NextStopPoint is null || state.ExpectedArrivalAtNext is null || averageSegmentDuration <= TimeSpan.Zero)
            return null;

        var timeToNext = state.ExpectedArrivalAtNext.Value - now;
        var fraction = 1 - timeToNext / averageSegmentDuration;
        return Math.Clamp(fraction, 0, 1);
    }

    public TrainSnapshot ToSnapshot(TrainState state, DateTimeOffset now, TimeSpan? averageSegmentDuration) => new()
    {
        Id = state.Id,
        Line = state.Line,
        PreviousStopPoint = state.PreviousStopPoint,
        NextStopPoint = state.NextStopPoint,
        ExpectedArrivalAtNext = state.ExpectedArrivalAtNext,
        Position = Position(state, now, averageSegmentDuration),
        LastUpdated = state.LastUpdated,
    };

    public TrainPositionChanged ToPositionChangedEvent(TrainState state, DateTimeOffset now, TimeSpan? averageSegmentDuration) => new()
    {
        Id = state.Id,
        Line = state.Line,
        PreviousStopPoint = state.PreviousStopPoint,
        NextStopPoint = state.NextStopPoint,
        ExpectedArrivalAtNext = state.ExpectedArrivalAtNext,
        Position = Position(state, now, averageSegmentDuration),
    };

    private double? Position(TrainState state, DateTimeOffset now, TimeSpan? averageSegmentDuration) =>
        averageSegmentDuration is { } duration ? PositionBetweenStops(state, now, duration) : null;
}
