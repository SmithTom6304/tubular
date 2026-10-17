using ArrivalsSimulator;
using Common.Domain;
using Common.Helpers;
using Common.Infrastructure.Kafka.Serialization;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddSingleton<ISerializer<IReadOnlyList<ArrivalPrediction>>, JsonSerializer<IReadOnlyList<ArrivalPrediction>>>();
builder.AddKafkaProducer<Null, IReadOnlyList<ArrivalPrediction>>(
    connectionName: "kafka",
    configureBuilder: (_, producerBuilder) => producerBuilder.SetValueSerializer(new JsonSerializer<IReadOnlyList<ArrivalPrediction>>()));

builder.Services.AddSingleton<LineStopPointHelper>();
builder.Services.AddSingleton<TrainIdGenerator>();
builder.Services.AddSingleton<ArrivalPredictionProducer>();

string[] lines = ["jubilee"];
foreach (var line in lines)
{
    builder.Services.AddHostedService<LineArrivalPredictionSimulator>(sp =>
    {
        var lineStopPointHelper = sp.GetRequiredService<LineStopPointHelper>();
        var trainIdGenerator = sp.GetRequiredService<TrainIdGenerator>();
        var producer = sp.GetRequiredService<ArrivalPredictionProducer>();
        var logger = sp.GetRequiredService<ILogger<LineArrivalPredictionSimulator>>();
        return new LineArrivalPredictionSimulator(line,
            GetStopPointsForLine,
            trainIdGenerator,
            producer,
            logger);

        ICollection<StopPoint> GetStopPointsForLine(string l)
        {
            var stopPoints = lineStopPointHelper.GetStopPointsOnLine(l);
            if(stopPoints.Count == 0)
                logger.LogWarning("Found no stop points for line {Line}", l);
            return stopPoints;
        }
    });
}

var host = builder.Build();
host.Run();