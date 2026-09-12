The arrivals endpoint gives us a timeToStation for each station along a given route, per vehicle.
So we know vehicle 1 is expected to reach station A in 10 seconds, station B in 90 seconds, station C in 150 seconds etc.
From this, we could say that A and B are 90 - 10 = 80 seconds apart, and B and C are 150 - 90 = 60 seconds apart.
These values may change over time based on delays, intentional waits etc. So to get a true value of roughly how far apart two stations are, we would need to track this over time, and find an average.
So if a train typically takes 90 seconds between stations, and we see it has 45s left to reach the station, we could say it is half way between two points.
If in the next update it is expected to reach the station in 60s, the train hasn't gone backwards, we just know there is a delay. And so the train is still at the half way point.

## Open question: what does TfL return for a train standing at a station?

Assumption, not yet confirmed against real data: when a train is stopped at (or very close to) a platform, the arrivals endpoint likely stops predicting for that stop point entirely and only reports `timeToStation` for the *next* one — i.e. there's no observed "0 seconds" prediction marking arrival. If true, position inference (and NetworkState's `TrainGrain`, which tracks `PreviousStopPoint`/`NextStopPoint` off these predictions) never sees an explicit "at platform" state — a train transitions straight from "approaching stop N" to "approaching stop N+1", and "currently at stop N" has to be inferred (e.g. treat position ≈ 1.0 briefly, or a dwell-time heuristic) rather than read directly off the feed.

Needs confirming against a real TfL response before NetworkState's application logic is written — flagged here so it isn't rediscovered from scratch later.