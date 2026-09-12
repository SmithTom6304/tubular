using Common.Domain;

namespace NetworkState.Grains.Interfaces;

public static class SegmentTimingGrainKey
{
    public static string Build(string line, NaptanId from, NaptanId to) => $"{line}:{from}:{to}";
}
