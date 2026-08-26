using System.Diagnostics;
using Common;

namespace TflMapBuilder;

[DebuggerDisplay("{Name} - {NaptanId}")]
public class MapStopPoint : IEquatable<MapStopPoint>
{
    public NaptanId NaptanId { get; init; }
    public string Name { get; init; }
    public double Latitude { get; init; }
    public double Longitude { get; init; }

    public bool Equals(MapStopPoint? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return NaptanId == other.NaptanId;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((MapStopPoint)obj);
    }

    public override int GetHashCode()
    {
        return NaptanId.GetHashCode();
    }

    public static bool operator ==(MapStopPoint? left, MapStopPoint? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(MapStopPoint? left, MapStopPoint? right)
    {
        return !Equals(left, right);
    }
}