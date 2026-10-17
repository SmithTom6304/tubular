using Common.Domain;
using Confluent.Kafka;
using NetworkState.Grains.Interfaces;

namespace NetworkState;

public class ArrivalPredictionConsumer(
    IConsumer<Null, IReadOnlyList<ArrivalPrediction>> consumer,
    IGrainFactory grainFactory,
    ILogger<ArrivalPredictionConsumer> logger) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        consumer.Subscribe("arrival-predictions");

        return Task.Run(async () =>
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var result = consumer.Consume(stoppingToken);
                var predictions = result.Message.Value;
                logger.LogDebug("Received {Count} arrival predictions", predictions.Count);

                // One poll batches predictions for every train on the line (several upcoming
                // stops each, see docs/arrival-predictions/tfl-example-data.schema.md) — route
                // each train's own predictions to its grain, which reduces them to the next stop.
                foreach (var group in predictions.GroupBy(p => p.Train))
                {
                    var trainGrain = grainFactory.GetGrain<ITrainGrain>(group.Key.ToString());
                    await trainGrain.ApplyArrivalPredictions(group.ToList());
                }
            }
        }, stoppingToken);
    }
}
