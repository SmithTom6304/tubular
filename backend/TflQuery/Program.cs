using TfL;
using TflQuery;

public class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddSingleton<Config>();
        builder.Services.AddSingleton<TfLClient>();
        builder.Services.AddSingleton(CreateTflQueriesService);

        builder.Services.AddHostedService(CreatePollerService);

        var app = builder.Build();

        await app.RunAsync();
    }

    private static TflQueries CreateTflQueriesService(IServiceProvider sp)
    {
        var client = sp.GetRequiredService<TfLClient>();
        var config = sp.GetRequiredService<Config>();
        return new TflQueries(client, config.Lines);
    }

    private static Poller CreatePollerService(IServiceProvider sp)
    {
        var config = sp.GetRequiredService<Config>();
        var logger = sp.GetRequiredService<ILogger<Poller>>();
        var programLogger = sp.GetRequiredService<ILogger<Program>>();
        var tflQueries = sp.GetRequiredService<TflQueries>();

        var pollTask = (CancellationToken ct) => QueryHandler(ct, tflQueries, programLogger);

        return new Poller(pollTask, config.PollInterval, logger);
    }

    private static async Task QueryHandler(CancellationToken ct, TflQueries queries, ILogger<Program> logger)
    {
        var arrivals = await queries.QueryLineArrivals(ct);
        foreach(var arrival in arrivals)
            logger.LogInformation(arrival.ToString());
    }
}