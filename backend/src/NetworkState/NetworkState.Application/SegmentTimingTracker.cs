using NetworkState.Domain;

namespace NetworkState.Application;

public sealed class SegmentTimingTracker : ISegmentTimingTracker
{
    public SegmentTimingState RecordTraversal(SegmentTimingState current, TimeSpan duration)
    {
        var count = current.Count + 1;
        var average = current.AverageDuration + (duration - current.AverageDuration) / count;

        return current with
        {
            Count = count,
            AverageDuration = average,
        };
    }
}
