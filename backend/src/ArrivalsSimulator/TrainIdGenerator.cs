using Common.Domain;
using Microsoft.Extensions.Logging;

namespace ArrivalsSimulator;

internal class TrainIdGenerator(ILogger<TrainIdGenerator> logger)
{
    private int TrainIndex { get; set; }

    public TrainId GenerateTrainId() => TrainId.FromString(TrainIndex++.ToString());
}