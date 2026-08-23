import { scaleLinear } from "d3-scale";
import type { Layout, RenderOptions, TubeMapViewModel } from "./types";

/**
 * Pure `Layout -> TubeMapViewModel`. All the interesting geometry lives here so
 * the shell stays a mechanical renderer. No DOM, no framework.
 *
 * - Fits abstract grid coords into the padded logical viewport.
 * - Preserves aspect ratio (a single shared units-per-pixel, then centres) so
 *   the map is never stretched.
 * - Flips Y: grid y is up = north, screen y grows downward.
 * - Emits straight-segment SVG path strings per edge (curves come with waypoints).
 */
export function buildViewModel(
  layout: Layout,
  opts: RenderOptions,
): TubeMapViewModel {
  const { width, height, padding } = opts;
  const bounds = { width, height };

  // Nothing to place — hand back an empty, valid view-model.
  if (layout.stations.length === 0) {
    return { stations: [], edges: [], bounds };
  }

  const xs = layout.stations.map((s) => s.x);
  const ys = layout.stations.map((s) => s.y);
  const minX = Math.min(...xs);
  const maxX = Math.max(...xs);
  const minY = Math.min(...ys);
  const maxY = Math.max(...ys);

  const gridW = maxX - minX;
  const gridH = maxY - minY;

  const availW = width - 2 * padding;
  const availH = height - 2 * padding;

  // Guard against a zero span (single station, or a perfectly vertical/
  // horizontal line): treat it as span 1 so we can still divide, then the
  // content size on that axis is 0 and the point lands dead-centre.
  const spanX = gridW || 1;
  const spanY = gridH || 1;

  // One uniform scale for both axes = preserved aspect ratio. Fit-to-box: the
  // larger relative axis range is the limiting one.
  const unitsPerPixel = Math.max(gridW / availW, gridH / availH) || 1 / availW;
  const scale = 1 / unitsPerPixel;

  const contentW = gridW * scale;
  const contentH = gridH * scale;

  // Centre the fitted content in the padded viewport.
  const left = padding + (availW - contentW) / 2;
  const top = padding + (availH - contentH) / 2;
  const bottom = top + contentH;

  const xScale = scaleLinear()
    .domain([minX, minX + spanX])
    .range([left, left + spanX * scale]);

  // Y is flipped: larger grid y (north) -> smaller screen y (top).
  const yScale = scaleLinear()
    .domain([minY, minY + spanY])
    .range([bottom, bottom - spanY * scale]);

  const stations = layout.stations.map((s) => ({
    id: s.id,
    label: s.label,
    cx: xScale(s.x),
    cy: yScale(s.y),
  }));

  const colourOf = new Map(layout.lines.map((l) => [l.id, l.colour]));
  const pos = new Map(stations.map((s) => [s.id, s]));

  const edges = layout.edges.flatMap((e) => {
    const a = pos.get(e.from);
    const b = pos.get(e.to);
    // Skip edges that reference an unknown station rather than emit NaN paths.
    if (!a || !b) return [];
    return [
      {
        line: e.line,
        d: `M ${a.cx} ${a.cy} L ${b.cx} ${b.cy}`,
        colour: colourOf.get(e.line) ?? "#000000",
      },
    ];
  });

  return { stations, edges, bounds };
}
