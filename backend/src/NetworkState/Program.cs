using Common;
using Common.Domain;
using Common.Infrastructure.Kafka.Serialization;
using Confluent.Kafka;
using NetworkState;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.UseOrleans(silo => silo.UseLocalhostClustering());

builder.AddKafkaConsumer<Null, ArrivalPrediction>(
    connectionName: "kafka",
    configureBuilder: (_, consumerBuilder) => consumerBuilder.SetValueDeserializer(new JsonDeserializer<ArrivalPrediction>()));

builder.Services.AddHostedService<ArrivalPredictionConsumer>();

var host = builder.Build();
host.Run();
