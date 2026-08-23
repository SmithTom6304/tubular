# TFL Tube App — Frontend Design

Design notes for the frontend that visualises the tube network. Starts as a
static map of station/line data (from `data/*.json`), grows into an
interactive, live-updating map fed by the backend over SignalR.

## Guiding principles

- **Separate the *map* from the *shell*.** The map rendering is one concern;
  the application (state, panels, routing, live-data connection) is another.
  Keeping them apart keeps the framework decision reversible and keeps the
  valuable geometry code portable.
- **Separate *static layout* from *live data*.** Two different data shapes with
  two different lifecycles (see Data contracts). Never conflate them.
- **Spend the learning budget on the backend.** Kafka/Redis/Orleans/k8s are
  where the interesting problems live; the frontend should stay boring and
  well-factored.

## Architecture overview

Three layers:

1. **Core D3 library** — framework-agnostic, pure TypeScript. The interesting
   maths of building the map. No DOM, no framework.
2. **Shell** — the app that renders the map and owns application state. React
   first, later swapped for Fable/Elmish.
3. **Deployment** — the built app is static assets served by nginx; a separate,
   stateless container that fits the microservice/k8s direction.

```
layout JSON ──▶ [ Core D3 lib ] ──▶ view-model ──▶ [ Shell renders SVG ]
(from backend)   pure TS maths        pure data       React / Fable-Elmish
                                                            │
                                    live data (later) ──────┘  via SignalR
```

### Core D3 library (Architecture "B": D3 for maths, shell for rendering)

The core is a **pure function `layout → view-model`**. It does all the geometry
and hands back render-ready data; it never touches the DOM and knows nothing
about React or Fable. The shell is then a thin, near-mechanical renderer of the
view-model.

Chosen deliberately over the alternative ("self-contained D3 library that owns
the SVG"). That alternative is more portable out of the box, but this split
gives a cleaner long-term architecture and better *declarative* interactivity in
the shell. The porting cost between shells is accepted (see Roadmap) and is
small precisely because the core carries the weight.

```ts
// Core output — pure data, no DOM, no framework
interface TubeMapViewModel {
  stations: { id: string; label: string; cx: number; cy: number }[];  // screen coords
  edges:    { line: string; d: string; colour: string }[];            // d = SVG path string
  bounds:   { width: number; height: number };
}
```

What lives in the core (all pure, all portable):

- grid-coordinate → screen-coordinate transforms
- edge path-string generation via `d3-shape` (pure — returns a `d` string)
- parallel-line offsets, label placement, interchange markers
- layout parsing / validation into an in-memory graph model

**Pure vs DOM-touching D3.** Only the *pure* parts of D3 belong in the core:
`d3-scale`, `d3-shape`, `d3-geo`, `d3-hierarchy`. The DOM-touching parts —
`d3-zoom`, `d3-drag`, `d3-selection`, `d3-brush` — cannot live in a pure core.
The practical casualty is **pan/zoom**: `d3-zoom` attaches listeners to a DOM
node, so it's either per-shell glue (attach to the SVG in each shell) or
hand-rolled as pure transform maths in the core. Decision: **skip zoom for step
one**, add it later as small per-shell glue (`d3-zoom`), only hand-roll it if
full portability proves worth the effort.

### Shell

The shell owns application state and renders the view-model to SVG. It is the
part that is framework-specific and gets rewritten on a framework switch.

- Renders view-model → SVG elements (stations as `<circle>`, edges as `<path>`).
- Owns state: selected station, and (later) the live-data overlay.
- Emits/handles interaction (click a station → select → show details panel).
- Holds the SignalR connection (later) and feeds the live-data cache.

Rule of thumb throughout: **D3 for the maths, the shell for the DOM.** The map
and any panels are pure functions of `(layout, liveData, selectedStation)`.

### Deployment (nginx)

The shell builds to static HTML/JS/CSS and is served by nginx — a stateless
container, cleanly separate from the C# services, trivial to deploy in k8s.
No SSR / Next.js needed. When multiple views appear (e.g. a login page), add
**client-side routing** (e.g. `react-router`); the app stays static and the only
nginx change is the SPA fallback:

```nginx
location / { try_files $uri $uri/ /index.html; }
```

Note: both a plain React build **and** a Fable app compile down to ordinary
static JS and host identically here. (Blazor WASM would also be static-hostable
but ships a .NET runtime — not chosen; see below.)

## Shell choice: React now, Fable/Elmish later

### Framework options considered

- **React / TypeScript** — chosen for step one. Already known, ideal for
  interactive SVG, native click/hover handling, huge ecosystem. TypeScript
  keeps it comfortable for a C#/typed-language developer and lets the SignalR
  contract be typed end-to-end.
- **Fable + Elmish (F# → JS)** — the intended longer-term shell. Fable compiles
  F# to **plain JavaScript targeting React** (via Feliz/Fable.React) — no .NET
  runtime shipped, so it hosts on nginx exactly like a normal React app. The
  Elm architecture (model → update → view) is a very clean fit for this app's
  state, and subscriptions are a first-class home for a long-lived SignalR feed.
  Costs: learning F# (modest) and a heavier build toolchain (.NET SDK + JS
  bundler).
- **Blazor WASM/Server, Svelte, Solid, Vue, Angular** — considered, not chosen.
  Blazor makes you do JS interop at exactly the interactive-SVG boundary and
  discards existing React knowledge; Blazor Server's own stateful SignalR
  circuit competes with the planned engine websocket and scales awkwardly in
  k8s. Svelte/Solid are pleasant but offer no decisive win at ~270-station
  scale.

### Why the port is cheap

With Architecture B, a framework switch rewrites only the shell:

- **Ports for free:** the whole `layout → view-model` core (coords, path
  strings, offsets, labels) — pure TS.
- **Thin per-shell rewrite:** the ~dozen lines mapping view-model → SVG
  elements, event wiring, and (if added) the zoom glue.

Switching is cheapest early and gets more expensive as the shell grows (panels,
routing, live-data state). Hence the roadmap starts in React and switches before
much shell accumulates.

## Live data (future — informs, doesn't block step one)

SignalR feeds a live-data overlay keyed by station/line id; the map merges it
onto the static layout at render. SignalR is an imperative event stream bridged
into declarative state:

- **React:** a single top-level hook/provider — `useEffect` starts one
  `HubConnection`, `conn.on(...)` dispatches into a `useReducer`; on reconnect,
  re-fetch a full snapshot (messages were missed while disconnected).
- **Fable/Elmish:** an Elmish **subscription** starts the connection and turns
  each callback into a `Msg`; `update` folds it into the model. Cleaner because
  the framework *is* this pattern (React's `useReducer` + context is a
  hand-rolled version of the same shape).

At tube scale, pushing whole-network snapshots per tick is fine; per-station
subscriptions can wait until proven necessary.

## Data contracts

- **Layout / topology** — slow-changing config produced by `TflMapBuilder`,
  loaded once. Abstract **grid** coordinates (not pixels, not lat/long); the
  core maps grid → screen. Geographic lat/long (current `data/*.json`) is just
  an input to layout generation, not a runtime coordinate system.

  ```jsonc
  {
    "lines":    [{ "id": "jubilee", "name": "Jubilee", "colour": "#A0A5A9" }],
    "stations": [{ "id": "940GZZLUSTD", "label": "Stratford", "x": 30, "y": 4 }],
    "edges":    [{ "line": "jubilee", "from": "940GZZLUSTD", "to": "940GZZLUWHM",
                   "waypoints": [] }]   // optional control points for line routing
  }
  ```

- **Live overlay** — fast-changing, streamed over SignalR, keyed by id
  (crowding, delays, ETAs). Separate shape, separate lifecycle.

## Roadmap

1. **Step one — static React map.** Vite + React + TypeScript. Build the pure
   core D3 library (`layout → view-model`) and a thin React shell that renders
   it. Load the existing Jubilee-line data and get the line on screen. No zoom,
   no live data, no routing.
2. **Interactivity.** Click a station → select → details panel. Still React,
   still local/static data.
3. **Switch shell to Fable/Elmish.** While the shell is still small: keep the
   core untouched, rewrite the thin rendering layer in F#/Feliz, learn F# away
   from the D3 geometry.
4. **Live data.** Add the SignalR client and the live overlay; wire it through
   Elmish subscriptions (or the React reducer hook if still on React).
5. **Later.** Zoom/pan, client-side routing + login, full-network layout,
   crowding visualisation.

## Open items

- Exact `TflMapBuilder` → layout-schema contract (and where it's versioned).
- Line routing: start with straight edges; add `waypoints` for the 45°/90°
  tube-map aesthetic later.
- Whether to eventually hand-roll pan/zoom for full portability vs per-shell
  `d3-zoom` glue.
