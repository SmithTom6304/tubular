using Common;

namespace TflQuery;

public class Config
{
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(5);
    public string[] Lines { get; set; } = ["jubilee"];
}