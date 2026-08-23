The arrivals endpoint gives us a timeToStation for each station along a given route, per vehicle.
So we know vehicle 1 is expected to reach station A in 10 seconds, station B in 90 seconds, station C in 150 seconds etc.
From this, we could say that A and B are 90 - 10 = 80 seconds apart, and B and C are 150 - 90 = 60 seconds apart.
These values may change over time based on delays, intentional waits etc. So to get a true value of roughly how far apart two stations are, we would need to track this over time, and find an average.
So if a train typically takes 90 seconds between stations, and we see it has 45s left to reach the station, we could say it is half way between two points.
If in the next update it is expected to reach the station in 60s, the train hasn't gone backwards, we just know there is a delay. And so the train is still at the half way point.