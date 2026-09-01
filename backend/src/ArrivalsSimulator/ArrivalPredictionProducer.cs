using Common.Domain;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace ArrivalsSimulator;

internal class ArrivalPredictionProducer(IProducer<Null, ArrivalPrediction> producer, ILogger<ArrivalPredictionProducer> logger)
{
    public async Task SendPredictionAsync(ArrivalPrediction prediction, CancellationToken token)
    {
        var result = await producer.ProduceAsync("arrival-predictions", new Message<Null, ArrivalPrediction> { Value = prediction }, token);
        logger.LogDebug("Produced {@ArrivalPrediction}", result.Message.Value);
    }
}