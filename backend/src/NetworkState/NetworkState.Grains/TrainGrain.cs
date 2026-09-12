using Common.Domain;
using NetworkState.Application;
using NetworkState.Domain;
using NetworkState.Grains.Interfaces;
using Orleans.Runtime;
using Orleans.Streams;

namespace NetworkState.Grains;

public sealed class TrainGrain(
    [PersistentState("train", "Default")] IPersistentState<TrainState> state,
    ITrainTracker tracker) : Grain, ITrainGrain, IRemindable
{
    private const string StalenessReminderName = "staleness";
    private static readonly TimeSpan StalenessWindow = TimeSpan.FromSeconds(90);

    public async Task ApplyArrivalPrediction(ArrivalPrediction arrivalPrediction)
    {
        var now = DateTimeOffset.UtcNow;
        var current = state.State.Line is null
            ? new TrainState { Id = arrivalPrediction.Train, Line = arrivalPrediction.Line, LastUpdated = now }
            : state.State;

        var (updated, traversal) = tracker.Apply(current, arrivalPrediction.StopPoint, arrivalPrediction.ExpectedArrival, now);
        state.State = updated;
        await state.WriteStateAsync();

        if (traversal is not null)
        {
            var segmentGrain = GetSegmentTimingGrain(traversal.Line, traversal.From, traversal.To);
            await segmentGrain.RecordTraversal(traversal.Duration);
        }

        await this.RegisterOrUpdateReminder(StalenessReminderName, StalenessWindow, StalenessWindow);

        var averageSegmentDuration = await GetAverageSegmentDuration(updated);
        var streamEvent = tracker.ToPositionChangedEvent(updated, now, averageSegmentDuration);
        await GetTrainStream().OnNextAsync(streamEvent);
    }

    public async Task<TrainSnapshot?> GetSnapshot()
    {
        if (state.State.Line is null)
            return null;

        var averageSegmentDuration = await GetAverageSegmentDuration(state.State);
        return tracker.ToSnapshot(state.State, DateTimeOffset.UtcNow, averageSegmentDuration);
    }

    public async Task ReceiveReminder(string reminderName, TickStatus status)
    {
        await GetTrainStream().OnNextAsync(new TrainGone { Id = state.State.Id });
        await state.ClearStateAsync();

        var reminder = await this.GetReminder(StalenessReminderName);
        if (reminder is not null)
            await this.UnregisterReminder(reminder);

        DeactivateOnIdle();
    }

    private ISegmentTimingGrain GetSegmentTimingGrain(string line, NaptanId from, NaptanId to) =>
        GrainFactory.GetGrain<ISegmentTimingGrain>(SegmentTimingGrainKey.Build(line, from, to));

    private Task<TimeSpan?> GetAverageSegmentDuration(TrainState trainState) =>
        trainState.PreviousStopPoint is { } from && trainState.NextStopPoint is { } to
            ? GetSegmentTimingGrain(trainState.Line, from, to).GetAverageDuration()
            : Task.FromResult((TimeSpan?)null);

    private IAsyncStream<TrainStreamEvent> GetTrainStream() =>
        this.GetStreamProvider(TrainStreamConstants.ProviderName)
            .GetStream<TrainStreamEvent>(TrainStreamConstants.Namespace, this.GetPrimaryKeyString());
}
