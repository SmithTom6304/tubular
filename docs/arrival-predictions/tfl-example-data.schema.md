# TfL Arrivals response — rough schema

Based on `tfl-example-data.json`: a single poll of TfL's `/Line/{id}/Arrivals` endpoint for
the Jubilee line — one JSON array, 370 `Prediction` objects, no wrapper object. Captured
2026-10-17 07:34 UTC.

## Shape

```
Prediction[]
  id: string                   // opaque, not always numeric (e.g. "-185418420") — treat as string
  vehicleId: string            // the train — numeric string in this sample, but treat as opaque
  naptanId: string             // the predicted stop — 11 chars in this sample (our NaptanId
                                //    type's 12-byte buffer comfortably covers this)
  stationName: string          // 1:1 with naptanId
  lineId: string                // "jubilee" — matches our existing Line convention
  lineName: string              // "Jubilee" — display-only, redundant with lineId
  platformName: string          // e.g. "Northbound - Platform 1" — always present
  direction: string | null      // "inbound" | "outbound" | null (~15% of records are null;
                                //    no obvious correlation with platformName's own direction)
  destinationNaptanId: string   // fixed per vehicleId for the sample's duration (one journey)
  destinationName: string
  towards: string               // display name of destination, redundant with destinationName
  timestamp: string (ISO8601)   // IDENTICAL across all 370 records — this is the poll time,
                                //    not per-prediction. The whole array is one network-wide
                                //    snapshot at this instant.
  timeToStation: int            // seconds from `timestamp` to `expectedArrival` — exact:
                                //    expectedArrival == timestamp + timeToStationSeconds, always
  currentLocation: string       // free-text, e.g. "Between Baker Street and St. John's Wood"
  expectedArrival: string (ISO8601)
  timeToLive: string (ISO8601)  // always identical to expectedArrival in this sample — redundant
  modeName: string              // "tube" — constant for this dataset
  operationType: int            // always 1 in this sample — meaning unknown, probably an enum
  bearing: string                // always "" in this sample — no useful data
  timing: object
    source, insert, received: string (ISO8601)  // always "0001-01-01T00:00:00" sentinel —
                                                   //   unpopulated by TfL for this feed
    sent: string (ISO8601)      // always identical to top-level `timestamp`
    read: string (ISO8601)      // varies per record by a few hundred ms — TfL's own internal
                                 //   processing timestamp, not very useful to us
    countdownServerAdjustment: string (duration)  // always "00:00:00" in this sample
  $type: string                  // .NET type-discriminator noise from TfL's own serializer,
                                  //    not a domain field — ignore
```

## What actually matters for us

- **One train → many predictions, one per upcoming stop**, not one prediction per train. In
  this sample, 37 distinct `vehicleId`s produced 370 predictions (avg ~10 each, up to 15) —
  confirms the "TfL gives us next stop, the one after, etc." behaviour this doc exists to record.
- **The whole array is one atomic snapshot** (`timestamp` is the same value on every record)
  — predictions for different stops/trains in the same response are directly comparable at
  that instant, which is exactly the thing we want to exploit (e.g. "lowest `timeToStation`
  for a given `vehicleId` is its next stop").
- **Exact duplicate `(vehicleId, naptanId)` pairs occur within one response**: 370 records but
  only 336 unique `(vehicleId, naptanId)` pairs in this sample — some predictions repeat
  verbatim. Any consumer needs to be duplicate-tolerant (e.g. dedupe or just let "last write
  wins" absorb it), not assume one row per (train, stop).
- `expectedArrival` is fully redundant with `timestamp + timeToStation`; `timeToLive` is fully
  redundant with `expectedArrival`; `lineName`/`towards` are fully redundant with
  `lineId`/`destinationName`. Only one of each pair needs carrying forward.
- `bearing` and the three sentinel `timing.*` fields carry no real data in this sample.
- `direction` is nullable and worth treating as optional/unreliable rather than a hard fact.

## Open question this doc feeds into

How we consume "many predictions per train per poll" without breaking `TrainTracker.Apply`'s
current one-prediction-is-the-next-transition assumption — see the discussion already underway
about filtering to the lowest-`timeToStation` prediction per vehicle at the poller boundary vs.
widening the `ArrivalPrediction` contract to carry the whole per-train batch.
