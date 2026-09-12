namespace NetworkState.Grains.Interfaces;

/// Network-wide historical average traversal duration for a directed edge between two adjacent
/// stops on a line. Keyed by (Line, From, To) — see ADR-0006 for why both ends of the edge are
/// needed. Use SegmentTimingGrainKey.Build to address one.
public interface ISegmentTimingGrain : IGrainWithStringKey
{
    Task RecordTraversal(TimeSpan duration);

    /// Null if no traversal has been observed for this segment yet.
    Task<TimeSpan?> GetAverageDuration();
}
