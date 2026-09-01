namespace Common.Domain;

public struct StopPoint
{
    public required NaptanId NaptanId { get; init; }
    public string Name { get; init; }
    public double Latitude { get; init; }
    public double Longitude { get; init; }
}