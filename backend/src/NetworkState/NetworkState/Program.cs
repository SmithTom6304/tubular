using Common;
using Common.Domain;
using Common.Infrastructure.Kafka.Serialization;
using Confluent.Kafka;
using NetworkState;
using NetworkState.Application;
using NetworkState.Grains;
using Orleans.Serialization;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.UseOrleans(silo =>
{
    silo.UseLocalhostClustering();
    silo.UseInMemoryReminderService();
    silo.AddMemoryGrainStorageAsDefault();

    // Explicit stream subscriptions (our client-side consumers, e.g. NetworkState.DebugViewer)
    // need a PubSubStore grain-storage provider to track them.
    silo.AddMemoryGrainStorage("PubSubStore");
    silo.AddMemoryStreams(TrainStreamConstants.ProviderName);

    // NetworkState.Domain and NetworkState.Application stay Orleans-free (see ADR-0006), so their
    // types have no source-generated codecs; use System.Text.Json for them (and the Kafka
    // contract types they carry) instead of requiring [GenerateSerializer] there.
    silo.Services.AddSerializer(serializer => serializer.AddJsonSerializer(
        isSupported: type => type.Namespace is "NetworkState.Domain" or "NetworkState.Application" or "Common.Domain"));
});

builder.Services.AddSingleton<ITrainTracker, TrainTracker>();
builder.Services.AddSingleton<ISegmentTimingTracker, SegmentTimingTracker>();

builder.AddKafkaConsumer<Null, IReadOnlyList<ArrivalPrediction>>(
    connectionName: "kafka",
    configureBuilder: (_, consumerBuilder) => consumerBuilder.SetValueDeserializer(new JsonDeserializer<IReadOnlyList<ArrivalPrediction>>()));

builder.Services.AddHostedService<ArrivalPredictionConsumer>();

var host = builder.Build();
host.Run();
