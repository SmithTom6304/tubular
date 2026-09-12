using NetworkState.Domain;

namespace NetworkState.Application;

public interface ISegmentTimingTracker
{
    /// Folds a newly observed traversal duration into a segment's running average.
    SegmentTimingState RecordTraversal(SegmentTimingState current, TimeSpan duration);
}
