# Frontend Prototype Plan — Static Jubilee-Line Map (React)

Implementation plan for the **first buildable step** of the tube-map frontend:
a pure TypeScript core library that turns layout data into a render-ready
view-model, and a thin React app that renders it to SVG. The goal is **pixels on
screen** — the Jubilee-line stations from `data/stratford-stanmore.json` drawn as
a connected line.

Read `tube-app/frontend-design-doc.md` first — it holds the *why*. This plan is
the *what/how* for one step and does not repeat the rationale.

## Scope

**In scope (roadmap step 1):**

- A pure, framework-agnostic core library: `Layout → TubeMapViewModel`.
- An adapter that turns the raw station JSON into a `Layout`.
- A minimal React shell that renders the view-model as SVG and runs in a browser.
- Light unit tests on the pure core.

**Explicitly out of scope (do not build):**

- Interactivity — no click-to-select, hover, or details panel.
- Live data / SignalR.
- Pan / zoom.
- Routing / login.
- Fable / Elmish (a later shell swap; keep the core clean so it stays cheap).
- Full network, multiple lines, curved edges / waypoints.
- `TflMapBuilder` integration (the adapter is a temporary stand-in for it).

## Decisions already made

- **Stack:** Vite + React + TypeScript.
- **Location:** new `frontend/` directory at the repo root.
- **Separation:** `src/core/` is pure and imports **no** React (this is what keeps
  the future Fable port cheap); `src/shell/` is the React rendering layer.
- **Coordinates:** project the existing geographic lat/long. Crucially, this
  projection lives in the **adapter**, not the core. The core's `Layout` contract
  uses abstract grid `x`/`y` (matching the design doc and the future
  `TflMapBuilder` output); the core only scales grid → screen. When
  `TflMapBuilder` later emits real grid coords, the adapter's lat/long projection
  is deleted and the core is untouched.
- **Line colour:** Jubilee grey `#A0A5A9`.
- **Edges:** for a single line, connect stations consecutively in file order.
- **Maths lib:** use `d3-scale` for grid → screen scaling. Straight path strings
  for now; `d3-shape` (curves) and `d3-zoom` (pan/zoom) are deferred.
- **Responsiveness:** compute the view-model at a fixed logical size and let the
  SVG `viewBox` scale it to the container. No resize/measurement code in step one.

## Directory layout

```
frontend/
  package.json
  vite.config.ts
  tsconfig.json
  index.html
  src/
    core/                 # PURE — no React imports
      types.ts            # Layout, TubeMapViewModel, etc.
      buildViewModel.ts   # Layout -> TubeMapViewModel (the interesting maths)
      buildViewModel.test.ts
    shell/                # React rendering layer
      App.tsx
      TubeMap.tsx         # renders a TubeMapViewModel as SVG
    data/
      stratford-stanmore.json   # copied from repo-root data/ (self-contained app)
      layoutAdapter.ts    # raw JSON -> Layout (TEMPORARY: projects lat/long -> grid)
    main.tsx
```

## Data

Copy `data/stratford-stanmore.json` (repo root) into `frontend/src/data/` so the
app is self-contained. Each record is:

```json
{ "NaptanId": "940GZZLUSTD", "Name": "Stratford Underground Station",
  "Latitude": 51.541806, "Longitude": -0.003458 }
```

## Core library

### Types (`src/core/types.ts`)

```ts
export interface Line    { id: string; name: string; colour: string; }
export interface Station { id: string; label: string; x: number; y: number; }  // abstract grid coords, y up = north
export interface Edge    { line: string; from: string; to: string; }            // waypoints come later
export interface Layout  { lines: Line[]; stations: Station[]; edges: Edge[]; }

export interface RenderOptions { width: number; height: number; padding: number; }

// Pure, render-ready output — no DOM, no framework
export interface TubeMapViewModel {
  stations: { id: string; label: string; cx: number; cy: number }[];  // screen coords
  edges:    { line: string; d: string; colour: string }[];            // d = SVG path string
  bounds:   { width: number; height: number };
}
```

### `buildViewModel(layout, opts): TubeMapViewModel` (`src/core/buildViewModel.ts`)

Pure function. Responsibilities:

1. **Fit grid → screen.** Build `d3-scale` linear scales mapping the layout's grid
   `x`/`y` extents into `[padding, width-padding]` / `[padding, height-padding]`.
   - **Preserve aspect ratio** so the line isn't stretched (use a single shared
     units-per-pixel derived from the larger of the two axis ranges, then centre).
   - **Flip Y**: grid `y` is up = north; screen y grows downward, so invert the y
     scale. (Owning the screen-space flip in the core matches how `TflMapBuilder`
     grid coords will behave.)
2. **Project stations** to `{ id, label, cx, cy }` using those scales.
3. **Build edge paths**: for each edge, look up its two endpoints' screen coords
   and emit a straight segment `d = \`M ${x1} ${y1} L ${x2} ${y2}\``, tagged with
   the line's colour. (Swap for a `d3-shape` curve generator when waypoints land.)
4. Return `{ stations, edges, bounds: { width, height } }`.

### Tests (`src/core/buildViewModel.test.ts`)

Vitest. Keep light but meaningful on the pure maths:

- Corner stations map to the padded viewport corners (respecting the Y-flip).
- Aspect ratio preserved (equal grid deltas → equal screen deltas on both axes).
- An edge's path string references its two endpoints' projected coords.
- Empty / single-station layouts don't throw.

## Adapter (`src/shell/data/layoutAdapter.ts`) — temporary

Turns the raw JSON into a `Layout`. **This is a stand-in for `TflMapBuilder`** and
is where the throwaway geographic projection lives.

- `lines`: single entry `{ id: "jubilee", name: "Jubilee", colour: "#A0A5A9" }`.
- `stations`: map each record → `{ id: NaptanId, label: <Name with " Underground
  Station" stripped>, x, y }`, where `x`/`y` come from an **equirectangular
  projection** of lon/lat into abstract grid units:
  - `x` from longitude, `y` from latitude with **north = larger y**.
  - Optionally scale longitude by `cos(meanLatitude)` so east–west distance isn't
    exaggerated at London's latitude (nice-to-have, not required).
- `edges`: consecutive pairs in file order, all on line `jubilee`.

Keep this isolated so deleting it later touches nothing in `core/`.

## React shell

### `TubeMap.tsx`

Pure render of a `TubeMapViewModel`. No state, no handlers.

- Render one `<svg>` with `viewBox="0 0 {bounds.width} {bounds.height}"` and CSS
  `width: 100%; height: auto` (responsive scaling for free).
- Draw order: edges first (`<path d stroke={colour} fill="none" stroke-width>`),
  then stations (`<circle cx cy r>`), then `<text>` labels with a small offset.
- Give the line a chunky stroke and stations a white fill + coloured ring so it
  reads like a tube map.

### `App.tsx` / `main.tsx`

- Import the raw JSON, run `layoutAdapter` → `Layout`.
- Call `buildViewModel(layout, { width: 1000, height: 1400, padding: 48 })`
  (logical size; tune to taste).
- Render `<TubeMap viewModel={vm} />` centred on the page.

## Build order

1. Scaffold `frontend/` with Vite (`react-ts` template); add `d3-scale` +
   `@types/d3-scale` and `vitest`.
2. Copy the data file into `src/data/`.
3. Write `core/types.ts`.
4. Write `core/buildViewModel.ts` (scales + Y-flip + aspect ratio + straight paths).
5. Write `core/buildViewModel.test.ts`; get it green.
6. Write `shell/data/layoutAdapter.ts`.
7. Write `shell/TubeMap.tsx` and wire `App.tsx` / `main.tsx`.
8. `npm run dev` and confirm the map renders.

## Definition of done

- `npm run dev` serves the app and the browser shows the Jubilee line: ~all
  Stratford→Stanmore stations as circles, connected in order by a grey line, with
  readable station labels, correctly oriented (north up), not stretched.
- `npm test` passes the core unit tests.
- `src/core/` contains **zero** React imports.
- The app builds (`npm run build`) to static assets (confirms the nginx-hosting
  path from the design doc).

## Notes to keep the eventual Fable port cheap

- All interesting maths stays in `src/core/` as pure functions returning the
  view-model. The Fable port reimplements only `TubeMap.tsx` (view-model → SVG).
- Don't leak React types or DOM access into `core/`.
- The `TubeMapViewModel` is the seam between core and shell — keep it plain data.
