using Common.Domain;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace ArrivalsSimulator;

internal class ArrivalPredictionProducer(IProducer<Null, IReadOnlyList<ArrivalPrediction>> producer, ILogger<ArrivalPredictionProducer> logger)
{
    /// Sends one poll's worth of predictions as a single message — TfL's own Arrivals endpoint
    /// returns every prediction for a line (many trains, several upcoming stops each) in one
    /// response (see docs/arrival-predictions/tfl-example-data.schema.md), so we mirror that
    /// batching rather than one message per prediction.
    public async Task SendPredictionsAsync(IReadOnlyList<ArrivalPrediction> predictions, CancellationToken token)
    {
        var result = await producer.ProduceAsync("arrival-predictions", new Message<Null, IReadOnlyList<ArrivalPrediction>> { Value = predictions }, token);
        logger.LogDebug("Produced {Count} arrival predictions", result.Message.Value.Count);
    }
}