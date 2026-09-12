using Common.Domain;
using Confluent.Kafka;
using NetworkState.Grains.Interfaces;

namespace NetworkState;

public class ArrivalPredictionConsumer(
    IConsumer<Null, ArrivalPrediction> consumer,
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
                var prediction = result.Message.Value;
                logger.LogDebug("Received {@ArrivalPrediction}", prediction);

                var trainGrain = grainFactory.GetGrain<ITrainGrain>(prediction.Train.ToString());
                await trainGrain.ApplyArrivalPrediction(prediction);
            }
        }, stoppingToken);
    }
}
