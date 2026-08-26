namespace Common;

/// A prediction of when a given train will arrive at a stop point
public sealed record ArrivalPrediction(TrainId Train, NaptanId StopPoint, DateTimeOffset ExpectedArrival);
