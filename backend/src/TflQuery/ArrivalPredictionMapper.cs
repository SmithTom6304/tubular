using Common.Domain;
using TfL.Entities;

namespace TflQuery;

public static class ArrivalPredictionMapper
{
    /// TfL's own ExpectedArrival is always UTC in practice (see
    /// docs/arrival-predictions/tfl-example-data.schema.md) but Newtonsoft doesn't reliably tag
    /// it as DateTimeKind.Utc on deserialize, so force it rather than let DateTimeOffset guess
    /// an offset from an Unspecified Kind.
    public static ArrivalPrediction ToArrivalPrediction(this TfLArrival arrival) => new(
        TrainId.FromString(arrival.VehicleId),
        arrival.LineId,
        NaptanId.FromString(arrival.NaptanId),
        new DateTimeOffset(DateTime.SpecifyKind(arrival.ExpectedArrival, DateTimeKind.Utc)));
}
