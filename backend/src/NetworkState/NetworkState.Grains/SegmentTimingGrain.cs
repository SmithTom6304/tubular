using Microsoft.Extensions.Logging;
using NetworkState.Application;
using NetworkState.Domain;
using NetworkState.Grains.Interfaces;

namespace NetworkState.Grains;

public sealed class SegmentTimingGrain(
    [PersistentState("segmentTiming", "Default")] IPersistentState<SegmentTimingState> state,
    ISegmentTimingTracker tracker,
    ILogger<SegmentTimingGrain> logger) : Grain, ISegmentTimingGrain
{
    public async Task RecordTraversal(TimeSpan duration)
    {
        logger.LogDebug("Recording traversal for segment {Segment} taking {Duration}", this.GetPrimaryKeyString(), duration);
        state.State = tracker.RecordTraversal(state.State, duration);
        await state.WriteStateAsync();
    }

    public Task<TimeSpan?> GetAverageDuration() =>
        Task.FromResult(state.State.Count > 0 ? state.State.AverageDuration : (TimeSpan?)null);
}
