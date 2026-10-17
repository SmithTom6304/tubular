namespace NetworkState.Application;

public static class TrainStreamConstants
{
    public const string ProviderName = "NetworkStateStreams";
    public const string Namespace = "trains";

    /// All TrainGrains publish onto this single stream (see ADR-0006) rather than one stream per
    /// train, so a consumer can observe every train without first knowing which ids exist.
    public const string AllTrainsKey = "all";
}
