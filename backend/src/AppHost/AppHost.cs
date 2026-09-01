using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var kafka = builder.AddKafka("kafka").WithKafkaUI();

var simulator = builder.AddProject<Projects.ArrivalsSimulator>("arrivals-simulator")
    .WithReference(kafka)
    .WaitFor(kafka);
var networkState = builder.AddProject<Projects.NetworkState>("network-state")
    .WithReference(kafka)
    .WaitFor(kafka);

builder.Build().Run();