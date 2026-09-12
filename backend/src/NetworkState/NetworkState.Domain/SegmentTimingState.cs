namespace NetworkState.Domain;

/// Running average traversal duration for a directed segment between two adjacent stops on a
/// line, as tracked by ISegmentTimingGrain. Immutable — see NetworkState.Application for the
/// logic that produces successor states via `with`.
public sealed record SegmentTimingState
{
    public int Count { get; init; }
    public TimeSpan AverageDuration { get; init; }
}
