# ADR-0005: Arrival Prediction events are StopPoint-grain with an absolute timestamp

**Status:** Accepted
**Date:** 2026-08-23

## Context

The Kafka event contract between the TfL poller and the simulated arrivals
service is the system's central seam — the core engine cannot distinguish
which service produced an event, so both must produce exactly the same
shape. Nothing about that shape was decided yet.

TfL's own arrival-prediction API (`TfLArrival`) gives, per train per stop
point: a relative countdown (`TimeToStation`, in seconds), an absolute
forecast (`ExpectedArrival`), a `VehicleId`, and a `NaptanId` naming the
specific platform, not the parent station. `TfLStopPoint` carries both its
own `NaptanId` and a separate `StationNaptan`, confirming stop point and
station are genuinely different identities in TfL's model, not synonyms —
the gap ADR-0003 flagged as unresolved (*StopPoint* vs *Station*, *Vehicle*
vs *Train*).

Two of those ambiguities bear directly on this contract's shape:

- **Location grain.** Position inference (`docs/Tracking trains between
  stations.md`) needs the time between two *specific* points, which only
  StopPoint-level data supports. The product goal is "crowding per
  station," which only needs the *aggregate*.
- **Time representation.** The design doc requires the arrivals topic stay
  replayable. A relative countdown baked into a stored event is correct
  only at the moment it was captured; replaying it later reads it as still
  counting down from then.

## Decision

**Arrival Prediction** events are keyed by **Train** and **StopPoint**, and
carry the forecast as the absolute **ExpectedArrival** timestamp.

## Alternatives considered

**Station-grain events.** Rejected. It matches the product goal directly
and needs no rollup downstream, but throws away the platform-level
precision position inference needs, and TfL's own data is stop-point
granular — aggregating up before storing the event discards information
the source actually provides. Station-level crowding becomes a projection
the engine computes from StopPoint events instead; the StopPoint→Station
mapping isn't modelled yet and is deferred until that projection is built.

**Relative `TimeToStation` in the event.** Rejected. Simpler passthrough of
the TfL field, but a replayed event's countdown no longer means what it
meant when captured — replay would need to be time-aware in a way that's
easy to get wrong. "Seconds until" is derived at consumption time from
`ExpectedArrival` minus now, not stored.

## Consequences

- The topology artifact (ADR-0002) will need to carry the StopPoint→Station
  mapping once crowding aggregation is built; it doesn't need to yet, since
  Phase 1's vertical slice only needs a moving dot, not crowding.
- The engine's read model derives "seconds until arrival" per client
  request rather than trusting a stored countdown — a small extra
  computation, paid once, in exchange for the topic staying meaningfully
  replayable.
- The frontend will also want Station-level grouping eventually — the same
  rollup gap called out above, on the consuming side rather than the engine
  side. Not resolved here; flagged for a follow-up discussion once the
  StopPoint→Station mapping is designed.
- `Arrival Prediction` is the event name going forward, in code and
  elsewhere — not `Arrival` or `ArrivalEvent`, which both read as an
  occurrence rather than a forecast. See `CONTEXT.md`.

## Update (2026-09-12): Line joined the grain

`ArrivalPrediction` now also carries `Line`, keying the event by Train,
Line and StopPoint rather than just Train and StopPoint. Needed because a
single StopPoint can sit on more than one line, and the simulator
(`ArrivalsSimulator`) generates predictions per line — without it, events
from different lines calling at the same platform would be indistinguishable.

`Line` is a plain `string` (e.g. `"jubilee"`), not a value type. The
previous `Line` enum (closed to the single `Jubilee` member) was deleted in
the same change rather than extended. That trades away the compile-time
safety a closed enum gave for a genuinely closed, small set (the Underground
lines) — worth revisiting once a second line is added to the simulator and
it's clear whether "closed enum" or "open string validated against known
lines" is the better fit. See `CONTEXT.md` for the `Line` term.
