using Common;
using TfL.Entities;

namespace TflMapBuilder;

public static class TflArrivalExtensions
{
    public static MapStopPoint ToMapStopPoint(this TfLArrival arrival) => new() {NaptanId = NaptanId.FromString(arrival.NaptanId), Name = arrival.StationName};
}