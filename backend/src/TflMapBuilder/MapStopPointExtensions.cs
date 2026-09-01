using Common;
using Common.Domain;
using TfL.Entities;

namespace TflMapBuilder;

public static class MapStopPointExtensions
{
    public static MapStopPoint ToMapStopPoint(this TfLStopPoint tfLStopPoint)
    {
        return new MapStopPoint()
        {
            NaptanId = NaptanId.FromString(tfLStopPoint.NaptanId),
            Name = tfLStopPoint.CommonName,
            Latitude = tfLStopPoint.Latitude,
            Longitude = tfLStopPoint.Longitude
        };
    }
}