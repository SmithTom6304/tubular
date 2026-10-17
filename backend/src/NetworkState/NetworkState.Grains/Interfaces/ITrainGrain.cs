using Common.Domain;
using NetworkState.Application;

namespace NetworkState.Grains.Interfaces;

/// Keyed by TrainId (see ADR-0006). Holds one train's position/journey state and pushes updates
/// onto its Orleans Stream.
public interface ITrainGrain : IGrainWithStringKey
{
    /// A batch of this train's predictions from one TfL poll — one per upcoming stop, not just
    /// the next one (see docs/arrival-predictions/tfl-example-data.schema.md). Reduces to the
    /// lowest ExpectedArrival (the soonest stop) before updating state.
    Task ApplyArrivalPredictions(IReadOnlyList<ArrivalPrediction> predictions);

    /// For GET /train/{id}. Returns null if this id has never been observed.
    Task<TrainSnapshot?> GetSnapshot();
}
