using System.Diagnostics;
using Common;

namespace TflMapBuilder;

[DebuggerDisplay("{From.Name} -> {To.Name}")]
public class MapLink
{
    public MapStopPoint From { get; }
    public MapStopPoint To { get; }
    public Line Line { get; }

    public MapLink(MapStopPoint from, MapStopPoint to, Line line)
    {
        From = from;
        To = to;
        Line = line;
    }

    protected bool Equals(MapLink other)
    {
        return From == other.From && To == other.To && Line == other.Line;
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((MapLink)obj);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(From, To, (int)Line);
    }
}