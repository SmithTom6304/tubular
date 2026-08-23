# ADR-0001: Microsoft Orleans for the core engine

**Status:** Accepted
**Date:** 2026-08-23

## Context

The core engine holds the live state of the tube network — where every train is,
which stations they're approaching, and eventually crowding levels. Arrivals
stream in continuously from Kafka, and updates stream out to connected clients
over SignalR.

This is an inherently concurrent problem. Every vehicle and every station is an
independent piece of mutable state being updated from a shared event stream and
read by many clients at once.

A stated goal of the project is to gain experience with concurrent systems, so
"write the concurrency layer by hand as a learning exercise" was a genuine
candidate rather than a strawman.

## Decision

Use **Microsoft Orleans** for the core engine.

## Alternatives considered

**Hand-rolled concurrency** — bespoke locking, channels, or an actor model built
from scratch. Rejected. The learning value is real but narrow: it would produce
a worse version of a solved problem, and consume the project's budget on
mechanics rather than on the distributed-systems questions that motivated it
(partitioning, backpressure, event contracts, service boundaries). Orleans still
demands that its concurrency model be understood properly — grain lifecycle,
turn-based execution, reentrancy — so the concepts are learned either way.

**Stateless service over Redis** — the shape assumed by the original design
draft. Rejected as the primary model. It pushes every state transition through a
network round-trip and needs explicit optimistic concurrency to handle
contention on hot keys. Grains hold their state in memory with single-threaded
execution per grain, which removes that class of problem. Redis may still return
later as a persistence or cache layer.

**Akka.NET** — comparable actor model. Not chosen; Orleans' virtual-actor model
means no explicit lifecycle management, and its .NET integration is more direct.
No strong objection to it beyond that.

## Consequences

- Grain topology becomes a live design question — per-train, per-station,
  per-line, or a mix. Deliberately unresolved; tracked as an open issue.
- Grain identity should map onto domain identity, which puts pressure on
  `NaptanId` to behave correctly as a key. It didn't at the time this ADR was
  written — `GetHashCode` returned a reference hash — fixed in #1, with the
  representation decision recorded in ADR-0004.
- Orleans clustering is another moving part in local development, which
  strengthens the case for .NET Aspire over hand-rolled docker-compose.
- Grain state is in-memory by default. Persistence and what survives a restart
  are deferred, not solved.
