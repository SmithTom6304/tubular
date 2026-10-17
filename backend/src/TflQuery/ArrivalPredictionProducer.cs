using Common.Domain;
using Confluent.Kafka;

namespace TflQuery;

public class ArrivalPredictionProducer(IProducer<Null, IReadOnlyList<ArrivalPrediction>> producer, ILogger<ArrivalPredictionProducer> logger)
{
    /// One poll's worth of predictions as a single message — TfL's own Arrivals endpoint returns
    /// every prediction for a line (many trains, several upcoming stops each) in one response,
    /// see docs/arrival-predictions/tfl-example-data.schema.md.
    public async Task SendPredictionsAsync(IReadOnlyList<ArrivalPrediction> predictions, CancellationToken token)
    {
        var result = await producer.ProduceAsync("arrival-predictions", new Message<Null, IReadOnlyList<ArrivalPrediction>> { Value = predictions }, token);
        logger.LogDebug("Produced {Count} arrival predictions", result.Message.Value.Count);
    }
}
