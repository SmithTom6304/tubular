using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NetworkState.Application;
using NetworkState.DebugViewer;
using Orleans.Serialization;

var builder = Host.CreateApplicationBuilder(args);

builder.UseOrleansClient(client =>
{
    client.UseLocalhostClustering();
    client.AddMemoryStreams(TrainStreamConstants.ProviderName);

    // Must match the host's serializer config (Program.cs in NetworkState) so stream payloads
    // deserialize correctly on this side of the wire.
    client.Services.AddSerializer(serializer => serializer.AddJsonSerializer(
        isSupported: type => type.Namespace is "NetworkState.Domain" or "NetworkState.Application" or "Common.Domain"));
});

builder.Services.AddHostedService<TrainStreamDashboard>();

var host = builder.Build();
await host.RunAsync();
