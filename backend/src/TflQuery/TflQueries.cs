using Common;
using TfL;
using TfL.Entities;

namespace TflQuery;

public class TflQueries(TfLClient tflClient, Line[] lines)
{
    public Task<TfLArrival[]> QueryLineArrivals(CancellationToken ct) =>
        tflClient.Line.GetLineArrivalsAsync(LinesToString(lines), ct);

    public async Task<NaptanId[]> QueryRouteSequence(Line line, bool outbound, CancellationToken ct)
    {
        var routesAndStops = await tflClient.Line.GetRoutesAndStopsAsync(line.ToString(),
            outbound ? TfLDirectionEnum.Outbound : TfLDirectionEnum.Inbound,
            serviceType: null,
            excludeCrowding: true,
            cancellationToken: ct);
        return routesAndStops.OrderedLineRoutes.First().NaptanIds.Select(NaptanId.FromString).ToArray();
    }

    public async Task<TfLStopPoint[]> QueryStopPoints(Line line, CancellationToken ct) =>
        await tflClient.Line.GetLineStationsAsync(line.ToString(), false, ct);

    private static IEnumerable<string> LinesToString(Line[] lines) => lines.Select(l => l.ToString());
}

