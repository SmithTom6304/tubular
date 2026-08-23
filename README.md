# tubular

A live map of the London Underground, built on the [TfL API](https://api.tfl.gov.uk/).

Stations and trains update in near-real-time; a later phase detects delays and
projects how they ripple into crowding across the network.

> **Status: early prototype.** One of the four planned services partly exists.
> There is no Kafka, no Orleans engine, and no live end-to-end path yet — the
> frontend renders a static JSON snapshot. See [What actually works](#what-actually-works).

## Why this project exists

The primary goal is **hands-on experience with microservices, Kafka, and
concurrent systems**. That shapes the design: where there's a choice between the
quickest route and the one that exercises those ideas properly, this project
takes the second. A single well-factored monolith would serve the end user just
as well — that's deliberately not what's being built here.

## Architecture

```
   TfL API
      │  poll
      ▼
┌──────────────┐                          ┌──────────────────┐
│  TfL poller  │──┐                    ┌─▶│  Core engine     │
└──────────────┘  │    ┌─────────┐     │  │  (MS Orleans)    │
                  ├───▶│  Kafka  │─────┤  │  network state   │
┌──────────────┐  │    └─────────┘     │  └────────┬─────────┘
│  Simulated   │──┘     arrivals       │           │ SignalR
│  arrivals    │        topic          │           ▼
└──────────────┘                       │  ┌──────────────────┐
                                       │  │  Frontend        │
                                       └──┤  (React + D3)    │
                                          └──────────────────┘
```

| Service | Role | State |
|---|---|---|
| **TfL poller** (`backend/TflQuery`) | Polls the TfL API for arrivals, produces to Kafka | partly built — polls and logs; no Kafka yet |
| **Simulated arrivals** | Produces synthetic arrivals to the same topic, for testing throughput and delay scenarios without live trains | not started |
| **Core engine** | Holds network state as Orleans grains; pushes updates over SignalR | not started |
| **Frontend** (`frontend/`) | Renders the map | prototype — static JSON, no live feed |

Both producers write the **same arrivals event contract**, so the engine can't
tell real trains from simulated ones. That contract is the key seam in the
system.

The **network topology** (which stations sit on which line, in what order) is
static data, generated ahead of time rather than discovered at runtime — see
[ADR-0002](docs/adr/0002-topology-as-build-time-artifact.md).

## Repository layout

```
backend/            .NET 10 solution
  Common/             shared domain types (NaptanId, Line)
  TflQuery/           the TfL poller service
  TflMapBuilder/      one-shot tool that generates the topology artifact
  tests/
frontend/           Vite + React + TypeScript
  src/core/           pure, framework-free map geometry (no React imports)
  src/shell/          React components that render the core's view-model
data/               generated topology artifacts
docs/               design docs, ADRs, agent config (also an Obsidian vault)
```

## Running it

**Backend** (.NET 10 SDK required):

```bash
cd backend
dotnet build
dotnet run --project TflQuery      # polls the Jubilee line every 5s, logs arrivals
dotnet test
```

**Frontend** (Node 20+):

```bash
cd frontend
npm install
npm run dev        # http://localhost:5173
npm test
```

**Regenerating the topology artifact:**

```bash
cd backend
dotnet run --project TflMapBuilder
```

## What actually works

- ✅ `TflQuery` polls TfL arrivals on a `PeriodicTimer` and logs them
- ✅ `TflMapBuilder` generates the Jubilee line's ordered stop sequence to JSON
- ✅ The frontend renders that static JSON as an SVG map
- ❌ No Kafka, no Orleans engine, no SignalR, no simulator
- ❌ Only the Jubilee line is modelled (`Line` enum has one member)

The next milestone is a **thin vertical slice** — one arrival travelling
poller → Kafka → engine → SignalR → a moving dot on the map, for a single line,
with no cleverness anywhere.

## Documentation

- [`docs/design-doc.md`](docs/design-doc.md) — systems design and roadmap
- [`docs/adr/`](docs/adr/) — architecture decision records
- [`docs/frontend-design-doc.md`](docs/frontend-design-doc.md) — frontend architecture and rationale
- [`docs/agents/`](docs/agents/) — conventions for AI agents working in this repo

Issues are tracked as [GitHub issues](https://github.com/SmithTom6304/tubular/issues).
