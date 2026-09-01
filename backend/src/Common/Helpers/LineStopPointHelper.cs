using Common.Domain;
using Microsoft.Extensions.Logging;

namespace Common.Helpers;

public class LineStopPointHelper(ILogger<LineStopPointHelper> logger)
{
    public ICollection<StopPoint> GetStopPointsOnLine(string line)
    {
        if (!StopPointsOnLine.TryGetValue(line, out var stopPoints))
            logger.LogWarning("No stop points on line {line}", line);
        return stopPoints ?? [];
    }

    // TODO Move to Database
    private Dictionary<string, ICollection<StopPoint>> StopPointsOnLine { get; } = new()
    {
        { "jubilee", new List<StopPoint>
            {
                new() { NaptanId = NaptanId.FromString("940GZZLUSTD"), Name = "Stratford Underground Station"},
                new() { NaptanId = NaptanId.FromString("940GZZLUWHM"), Name = "West Ham Underground Station"},
                new() { NaptanId = NaptanId.FromString("940GZZLUCGT"), Name = "Canning Town Underground Station"},
                new() { NaptanId = NaptanId.FromString("940GZZLUNGW"), Name = "North Greenwich Underground Station"},
                new() { NaptanId = NaptanId.FromString("940GZZLUCYF"), Name = "Canary Wharf Underground Station"}
            }
        }
    };
}