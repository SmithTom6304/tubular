using NetworkState.Application;
using NetworkState.Domain;
using NetworkState.Grains.Interfaces;
using Orleans.Runtime;

namespace NetworkState.Grains;

public sealed class SegmentTimingGrain(
    [PersistentState("segmentTiming", "Default")] IPersistentState<SegmentTimingState> state,
    ISegmentTimingTracker tracker) : Grain, ISegmentTimingGrain
{
    public async Task RecordTraversal(TimeSpan duration)
    {
        state.State = tracker.RecordTraversal(state.State, duration);
        await state.WriteStateAsync();
    }

    public Task<TimeSpan?> GetAverageDuration() =>
        Task.FromResult(state.State.Count > 0 ? state.State.AverageDuration : (TimeSpan?)null);
}
