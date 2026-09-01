namespace Common.Domain;

/// A prediction of when a given train will arrive at a stop point
public sealed record ArrivalPrediction(TrainId Train, string Line, NaptanId StopPoint, DateTimeOffset ExpectedArrival);
