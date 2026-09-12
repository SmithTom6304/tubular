using Common.Domain;
using NetworkState.Domain;

namespace NetworkState.Application;

public interface ITrainTracker
{
    /// Folds a new prediction into a train's state, returning the successor state and, if this
    /// prediction supersedes the one for a stop point further along the route, the segment just
    /// completed (see ADR-0006).
    (TrainState State, SegmentTraversal? Traversal) Apply(TrainState current, NaptanId stopPoint, DateTimeOffset expectedArrival, DateTimeOffset now);

    /// Fraction (0-1) of the way between PreviousStopPoint and NextStopPoint, or null if the train
    /// isn't currently tracked between two known stops. See "Tracking trains between stations.md".
    double? PositionBetweenStops(TrainState state, DateTimeOffset now, TimeSpan averageSegmentDuration);

    /// Builds the GET /train/{id} query DTO. averageSegmentDuration should come from the
    /// ISegmentTimingGrain for (state.Line, state.PreviousStopPoint, state.NextStopPoint), or be
    /// null if either stop is unknown or the segment has no observations yet.
    TrainSnapshot ToSnapshot(TrainState state, DateTimeOffset now, TimeSpan? averageSegmentDuration);

    /// Builds the event to push onto the train's stream after a successful Apply. See ToSnapshot
    /// for averageSegmentDuration.
    TrainPositionChanged ToPositionChangedEvent(TrainState state, DateTimeOffset now, TimeSpan? averageSegmentDuration);
}
