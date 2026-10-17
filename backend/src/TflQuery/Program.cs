using Common.Domain;
using Common.Infrastructure.Kafka.Serialization;
using Confluent.Kafka;
using TfL;
using TflQuery;

public class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddServiceDefaults();

        builder.Services.AddSingleton<TflQuery.Config>();
        builder.Services.AddSingleton<TfLClient>();
        builder.Services.AddSingleton(CreateTflQueriesService);

        builder.Services.AddSingleton<ISerializer<IReadOnlyList<ArrivalPrediction>>, JsonSerializer<IReadOnlyList<ArrivalPrediction>>>();
        builder.AddKafkaProducer<Null, IReadOnlyList<ArrivalPrediction>>(
            connectionName: "kafka",
            configureBuilder: (_, producerBuilder) => producerBuilder.SetValueSerializer(new JsonSerializer<IReadOnlyList<ArrivalPrediction>>()));
        builder.Services.AddSingleton<ArrivalPredictionProducer>();

        builder.Services.AddHostedService(CreatePollerService);

        var app = builder.Build();

        await app.RunAsync();
    }

    private static TflQueries CreateTflQueriesService(IServiceProvider sp)
    {
        var client = sp.GetRequiredService<TfLClient>();
        var config = sp.GetRequiredService<TflQuery.Config>();
        return new TflQueries(client, config.Lines);
    }

    private static Poller CreatePollerService(IServiceProvider sp)
    {
        var config = sp.GetRequiredService<TflQuery.Config>();
        var logger = sp.GetRequiredService<ILogger<Poller>>();
        var programLogger = sp.GetRequiredService<ILogger<Program>>();
        var tflQueries = sp.GetRequiredService<TflQueries>();
        var producer = sp.GetRequiredService<ArrivalPredictionProducer>();

        var pollTask = (CancellationToken ct) => QueryHandler(ct, tflQueries, producer, programLogger);

        return new Poller(pollTask, config.PollInterval, logger);
    }

    private static async Task QueryHandler(CancellationToken ct, TflQueries queries, ArrivalPredictionProducer producer, ILogger<Program> logger)
    {
        var arrivals = await queries.QueryLineArrivals(ct);
        var predictions = arrivals.Select(a => a.ToArrivalPrediction()).ToList();
        logger.LogInformation("Queried {ArrivalCount} arrivals, sending {PredictionCount} predictions", arrivals.Length, predictions.Count);

        if (predictions.Count > 0)
            await producer.SendPredictionsAsync(predictions, ct);
    }
}