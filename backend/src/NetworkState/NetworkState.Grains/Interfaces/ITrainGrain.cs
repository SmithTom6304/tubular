using Common.Domain;
using NetworkState.Application;

namespace NetworkState.Grains.Interfaces;

/// Keyed by TrainId (see ADR-0006). Holds one train's position/journey state and pushes updates
/// onto its Orleans Stream.
public interface ITrainGrain : IGrainWithStringKey
{
    Task ApplyArrivalPrediction(ArrivalPrediction arrivalPrediction);

    /// For GET /train/{id}. Returns null if this id has never been observed.
    Task<TrainSnapshot?> GetSnapshot();
}
