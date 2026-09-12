using Common.Domain;

namespace NetworkState.Domain;

/// A segment of track a train has been observed to traverse, used to feed ISegmentTimingGrain's running average.
public sealed record SegmentTraversal(string Line, NaptanId From, NaptanId To, TimeSpan Duration);
