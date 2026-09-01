using Common;
using Common.Domain;
using TfL;
using TfL.Entities;

namespace TflQuery;

public class TflQueries(TfLClient tflClient, string[] lines)
{
    public Task<TfLArrival[]> QueryLineArrivals(CancellationToken ct) =>
        tflClient.Line.GetLineArrivalsAsync(lines, ct);

    public async Task<NaptanId[]> QueryRouteSequence(string line, bool outbound, CancellationToken ct)
    {
        var routesAndStops = await tflClient.Line.GetRoutesAndStopsAsync(line,
            outbound ? TfLDirectionEnum.Outbound : TfLDirectionEnum.Inbound,
            serviceType: null,
            excludeCrowding: true,
            cancellationToken: ct);
        return routesAndStops.OrderedLineRoutes.First().NaptanIds.Select(NaptanId.FromString).ToArray();
    }

    public async Task<TfLStopPoint[]> QueryStopPoints(string line, CancellationToken ct) =>
        await tflClient.Line.GetLineStationsAsync(line, false, ct);
}

