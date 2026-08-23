// Pure core types. NO React, NO DOM. This module (and everything it imports)
// must stay framework-agnostic so the future Fable port only rewrites the shell.

/** A tube line. */
export interface Line {
  id: string;
  name: string;
  colour: string;
}

/** A station in abstract grid coordinates (y up = north). */
export interface Station {
  id: string;
  label: string;
  x: number;
  y: number;
}

/** A connection between two stations on a line. Waypoints come later. */
export interface Edge {
  line: string;
  from: string;
  to: string;
}

/** The static layout/topology fed into the core. Grid coords, not pixels. */
export interface Layout {
  lines: Line[];
  stations: Station[];
  edges: Edge[];
}

/** How to fit the layout onto the logical drawing surface. */
export interface RenderOptions {
  width: number;
  height: number;
  padding: number;
}

/** Pure, render-ready output — no DOM, no framework. */
export interface TubeMapViewModel {
  stations: { id: string; label: string; cx: number; cy: number }[]; // screen coords
  edges: { line: string; d: string; colour: string }[]; // d = SVG path string
  bounds: { width: number; height: number };
}
