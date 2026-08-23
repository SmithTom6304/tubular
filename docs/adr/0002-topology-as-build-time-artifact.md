# ADR-0002: Network topology is a build-time artifact

**Status:** Accepted
**Date:** 2026-08-23

## Context

The system needs to know the network's shape: which stations exist, which lines
they sit on, and in what order along each route. This is distinct from live
data — arrivals, positions, crowding — which changes second by second.

Topology changes on the order of years. New stations and line extensions are
rare and announced well in advance.

The TfL API can supply this at runtime via the route-sequence and line-stations
endpoints, so the engine *could* discover it on startup.

## Decision

Treat topology as **static data, generated ahead of time and committed to the
repository** as a JSON artifact under `data/`.

`backend/TflMapBuilder` is the generator: a one-shot tool that queries TfL and
writes the artifact. It is not a service and does not run in production.

## Alternatives considered

**Runtime discovery** — the engine fetches topology from TfL on startup.
Rejected. It makes engine startup depend on TfL availability, makes local and
test runs require network access, and introduces a class of bug where the
network's shape silently changes underfoot. None of that buys anything, given
how rarely the real topology changes.

**Hybrid** — ship an artifact, refresh it at runtime if stale. Rejected as
premature. It carries the failure modes of both approaches for a problem that
occurs every few years and can be handled by regenerating and redeploying.

## Consequences

- The artifact must be regenerated manually when the network changes. Acceptable
  at this cadence; worth a scheduled check eventually.
- Topology is available offline, so the engine, the simulator, and the frontend
  can all run with no TfL connectivity. This matters directly for the simulated
  arrivals service, whose purpose is testing without live data.
- The artifact is a shared contract between backend and frontend, and currently
  exists in two places (`data/` and `frontend/src/data/`). That duplication needs
  resolving.
- `TflMapBuilder` stays in the solution as a build tool. It also currently holds
  `Map`/`MapLink`/`ArrivalsProcessor`, which are early attempts at *inferring*
  topology from arrivals — superseded by this decision, and due to be moved or
  removed.
