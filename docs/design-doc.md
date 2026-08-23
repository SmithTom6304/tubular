# Systems Design

Systems-level design for tubular. For the frontend specifically, see
[frontend-design-doc.md](frontend-design-doc.md). For individual decisions and
their rationale, see [adr/](adr/).

## Goals

Primary goal: **gain experience with microservices, Kafka, and concurrent
systems.** The product is real, but the architecture is chosen to exercise those
ideas rather than to reach a working map by the shortest path.

Product goals:

- Display a live map of stations and trains from TfL data
- Show crowding level per station
- Detect train delays from ETA drift
- Predict near-future crowding when delays occur
- Offer a developer mode — simulated, non-live data with "normal" operations,
  where delays can be injected to watch the network react

## Phase 1 — Live tube map

The current focus.

1. **TfL poller** polls the TfL API for arrivals and produces events to a Kafka
   arrivals topic.
2. **Core engine** consumes the topic and maintains network state as Microsoft
   Orleans grains ([ADR-0001](adr/0001-orleans-for-core-engine.md)).
3. **Frontend** connects to the engine over SignalR and renders live updates.
4. **Simulated arrivals service** produces synthetic events to the same topic,
   so throughput and delay scenarios can be tested without waiting on real
   trains.

The poller and the simulator write the **same event contract** — the engine
cannot distinguish them. This is the system's central seam, and pinning it down
is a prerequisite for building any of the three services independently.

Network topology is a build-time artifact rather than runtime discovery
([ADR-0002](adr/0002-topology-as-build-time-artifact.md)).

### Delivery order

Repo hygiene → a **thin vertical slice** (one line, one arrival, poller → Kafka
→ trivial engine → SignalR → moving dot, no cleverness) → the simulator → then
depth in each service.

The slice comes first because the seams between services are where the risk and
the learning are; building any single service to depth before they connect just
defers finding out whether the contract is right.

## Phase 2 — Crowding prediction

Deliberately deferred. Sketch only:

- A **persister service** also consumes the arrivals topic and writes snapshots
  to a time-series database.
- A **prediction service** periodically trains a model on that history, and can
  then be queried with live state to project near-future crowding.

Phase 2 is not designed yet and should not constrain Phase 1 beyond keeping the
arrivals topic replayable.

## Position inference

The arrivals endpoint gives a `timeToStation` per station per vehicle, which is
enough to place a train *between* stations rather than just at one. The
reasoning is worked through in
[Tracking trains between stations.md](Tracking%20trains%20between%20stations.md).
This is unresolved domain modelling, not a settled design.

## Open questions

Tracked as GitHub issues; listed here for context.

- **Orleans grain topology** — grain-per-train, grain-per-station,
  grain-per-line, or a mix? Grain-per-train maps most naturally onto position
  inference, with stations as a projection.
- **Kafka partition key** — partitioning by line caps parallelism at ~11
  partitions. Is that acceptable, and what does the simulator reveal under load?
- **Local orchestration** — .NET Aspire versus hand-rolled docker-compose.
  Aspire gives service discovery and distributed tracing across all four
  services out of the box, without foreclosing Kubernetes later.
- **Data scale** — what volume are we actually dealing with, per line and
  network-wide? This gates most of the above.
- **Kubernetes** — a learning goal, but not yet justified by the system's needs.

## Settled questions

Previously open, now decided — kept so the reasoning isn't relitigated:

- **Backend language: C#.** Chosen partly for Orleans.
- **Concurrency: Orleans, not hand-rolled.** Writing a bespoke actor/concurrency
  layer was considered as a learning exercise and rejected — see
  [ADR-0001](adr/0001-orleans-for-core-engine.md).
- **State: Orleans grains, not Redis.** An earlier draft assumed Redis as the
  engine's store. Grains hold state directly, with persistence a later concern.
- **Transport to frontend: SignalR, not raw websockets.**
- **Frontend: React + TypeScript**, with a pure D3 core — see
  [frontend-design-doc.md](frontend-design-doc.md).

## Research questions

Longer-horizon, not blocking:

- Can a neural network predict crowding levels from tube delays?
- How are upstream and downstream stations affected by a delay? Does crowding
  propagate, and how far?
