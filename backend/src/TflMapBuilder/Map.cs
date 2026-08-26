namespace TflMapBuilder;

public class Map
{
    public HashSet<MapStopPoint> StopPoints = new();
    public HashSet<MapLink> Links { get; set; } = new();
}