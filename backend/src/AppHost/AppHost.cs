using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var kafka = builder.AddKafka("kafka")
    .WithKafkaUI()
    // Consumers/producers create topics on demand via metadata requests; without this the broker
    // may reject the first subscribe/produce with "Unknown topic or partition" instead of
    // creating "arrival-predictions" itself.
    .WithEnvironment("KAFKA_AUTO_CREATE_TOPICS_ENABLE", "true");

var poller = builder.AddProject<Projects.TflQuery>("tfl-poller")
    .WithReference(kafka)
    .WaitFor(kafka);
// One-shot delay: gives the poller a head start producing to "arrival-predictions" before
// NetworkState's consumer subscribes, avoiding a startup race where the topic doesn't exist yet.
var pollerWarmup = builder.AddExecutable("poller-warmup", "sleep", ".", "10")
    .WaitFor(poller)
    .ExcludeFromManifest();

var networkState = builder.AddProject<Projects.NetworkState>("network-state")
    .WithReference(kafka)
    .WaitFor(kafka)
    .WaitForCompletion(pollerWarmup);

builder.Build().Run();