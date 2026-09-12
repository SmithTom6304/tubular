# ADR-0006: NetworkState grain design — per-train grains, streams over a registry, reminder-based staleness

**Status:** Accepted
**Date:** 2026-09-12

## Context

`NetworkState` (`backend/src/NetworkState`) exists as a skeleton: an Orleans
silo host and a Kafka consumer that logs `ArrivalPrediction` events
(`ArrivalPredictionConsumer`) but does nothing with them yet. Grain topology
was left deliberately open by ADR-0001 and the design doc's open questions.
This ADR settles the shape of the engine's core: what a grain represents,
how live state reaches the frontend, and how a train that stops reporting
gets noticed.

`ArrivalPrediction` (ADR-0005) is keyed by Train, Line and StopPoint and
carries an absolute `ExpectedArrival`. Position inference
(`docs/Tracking trains between stations.md`) needs, per train, the segment
it's currently between and how long that segment typically takes — both are
naturally per-train concerns, not per-station ones.

## Decisions

### 1. Grain-per-train, not grain-per-station

`ITrainGrain`, keyed by `TrainId`, is the only grain type for now. It holds
a single train's own position/journey state. Station- or line-level views
(crowding, arrivals boards) are projections computed from train grains
later, not separately stored — there is no `IStationGrain` yet.

Rejected: **grain-per-StopPoint**, which matches `ArrivalPrediction`'s own
key exactly and would set up crowding aggregation more directly, but
position-between-stops would need correlating two StopPoint grains per
train instead of reading one grain's own state. Rejected: **both from the
start** — more moving parts before Phase 1's vertical slice needs any of
them.

Segment timing (the running average duration between two adjacent stops on
a line, which `CurrentPosition` needs) is network-wide, historical data,
not per-train state — it must survive any individual train's grain
deactivating. It gets its own grain, `ISegmentTimingGrain`, keyed by
`(Line, NaptanId from, NaptanId to)` — both ends of the edge, not just the
origin. Several Underground lines branch (Central, District, Northern,
Piccadilly), so the same `(Line, from)` platform can be the origin of more
than one distinct next-stop edge depending on which branch a given train
is on; keying on `from` alone would average two unrelated segment
durations into one meaningless number. The grain is updated whenever any
`TrainGrain` observes a segment transition (the previous `NextStopPoint`
being superseded by a prediction for a further stop).

### 2. Orleans Streams for live updates; no train registry

`TrainGrain` pushes onto an Orleans Stream on every state change, rather
than the frontend/SignalR layer polling a list of active trains. This means
there is deliberately **no grain that enumerates or registers active
trains** — Orleans grains are always addressable by key
(`GetGrain<ITrainGrain>(trainId)`) without one, and a stream consumer can
forward every push straight to SignalR clients without ever needing to ask
"what trains currently exist?"

Two event shapes are needed on the stream, not one: a position update, and
a distinct terminal "train gone" event (see reminders below) — a client
needs to tell "still here, ETA changed" apart from "remove this dot",
which a single event type conflates.

Rejected: **a registry grain** all `TrainGrain`s register with on
activation. Works, but is the more conventional/less instructive path
given the project's goal of exercising Orleans concepts properly (see
ADR-0001), and stream consumers don't need it to forward pushes to SignalR.

### 3. `ITrainGrain` also answers direct queries by id

Despite no registry, `ITrainGrain` exposes a query method (e.g.
`Task<TrainSnapshot?> GetSnapshot()`) for a `GET /train/{id}` endpoint. This
doesn't reintroduce a registry: the caller must already have a `TrainId` —
there's still no way to list valid ids. A request for an id nobody has
tracked returns "not found" (default/empty grain state), not an error.

So the shape is: **stream, push, for the map's live feed of all trains**;
**direct grain call, pull, for a caller who already knows which train they
want**.

### 4. Staleness is a Reminder-driven dead man's switch, not a Timer

A train that stops producing predictions (reached its terminus, dropped out
of TfL's feed) needs to eventually stop appearing on the frontend. Orleans
**Timers** don't fit: they only run while a grain is currently activated,
and nothing in a push-only stream design calls into a quiet `TrainGrain` to
keep it "recently used" — Orleans could idle-collect it, silently stopping
the check with no "it's gone" event ever published.

**Reminders** are persisted and Orleans reactivates a grain to deliver one
even after deactivation, so the check is guaranteed to run. The pattern:

- `Apply(ArrivalPrediction)` calls `RegisterOrUpdateReminder` on every
  update, pushing the next check further out — each prediction resets the
  clock.
- If no prediction arrives before it fires, `ReceiveReminder` runs:
  publishes the terminal "train gone" event to the stream, clears the
  grain's persisted state (so a later `GetSnapshot()` correctly reports
  "not found" rather than the train's last known position, and a
  subsequent prediction starts a fresh journey instead of resuming the old
  segment), unregisters the reminder, and lets the grain deactivate.

This is one reminder per currently-active train rather than one sweeper
enumerating all of them — consistent with decision 2 (no registry): nothing
centrally tracks the live set, so the eviction check has to be
self-scheduled per grain instead of driven externally.

Reminders need a reminder storage provider, which `NetworkState`'s
`Program.cs` doesn't configure yet. `UseInMemoryReminderService()` covers
single-silo local development with no extra setup; it isn't durable across
restarts and doesn't work multi-silo, so a real provider (ADO.NET is the
common Orleans choice) becomes a decision once clustering is actually in
play — the same open question ADR-0001 already flagged for local
orchestration (Aspire vs. hand-rolled).

Reminders also have a practical minimum period (Orleans reminders aren't
meant to fire much faster than about once a minute), which sets the
staleness window: TfL polling runs on the order of 5–30s, so a ~60–90s
"definitely gone" threshold is a reasonable fit, not a limitation reminders
impose on us.

## Consequences

- `NetworkState.Application` (new project, no Orleans reference) holds the
  pure logic: the `Apply`/`CurrentPosition` state transitions, segment
  timing updates, and the stream event/query DTOs. It's unit-testable with
  `dotnet test`, no test silo required.
- `NetworkState.Grains` (new project) holds `ITrainGrain`,
  `ISegmentTimingGrain`, and their implementations — thin Orleans adapters
  that delegate computation to `NetworkState.Application`. Rather than
  putting `[GenerateSerializer]` on the persisted `Domain` state types
  (which would pull an Orleans reference into a layer meant to stay
  framework-free), the host registers a namespace-scoped
  `System.Text.Json` serializer (`AddSerializer(... AddJsonSerializer(...))`)
  covering `NetworkState.Domain`, `NetworkState.Application` and
  `Common.Domain` types instead.
- `NetworkState` (host project) becomes the composition root: silo setup,
  the reminder storage provider, the Kafka consumer routing
  `ArrivalPrediction`s to the right `ITrainGrain`, and (later) the stream
  consumer that forwards to SignalR.
- Position inference still needs confirming against real TfL data for one
  edge case — what the feed returns for a train standing at a platform,
  rather than approaching one. Recorded as an open question in
  `docs/Tracking trains between stations.md`; not resolved by this ADR.
- Deliberately deferred, not solved here: `VehicleId`/`TrainId` reuse across
  lines (would break a `TrainId`-keyed grain's assumption that one id means
  one continuous journey), and the `StopPoint`→`Station` rollup needed for
  any future crowding projection.
