using Common.Domain;
using Confluent.Kafka;

namespace NetworkState;

public class ArrivalPredictionConsumer(IConsumer<Null, ArrivalPrediction> consumer, ILogger<ArrivalPredictionConsumer> logger) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        consumer.Subscribe("arrival-predictions");

        return Task.Run(() =>
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var result = consumer.Consume(stoppingToken);
                logger.LogDebug("Received {@ArrivalPrediction}", result.Message.Value);
            }
        }, stoppingToken);
    }
}
