import type { Layout } from "../core/types";

/**
 * TEMPORARY stand-in for `TflMapBuilder`.
 *
 * Turns the raw station JSON into a core `Layout`. The throwaway *geographic*
 * projection (lat/long -> abstract grid) lives here on purpose: when
 * `TflMapBuilder` later emits real grid coords, this whole file is deleted and
 * `core/` is untouched. Keep all lat/long knowledge inside this module.
 */

export interface RawStation {
  NaptanId: string;
  Name: string;
  Latitude: number;
  Longitude: number;
}

const JUBILEE = { id: "jubilee", name: "Jubilee", colour: "#A0A5A9" };

/** Strip the boilerplate suffix so labels read like a tube map. */
function cleanLabel(name: string): string {
  return name.replace(/\s+Underground Station$/, "").trim();
}

export function layoutFromRawStations(raw: RawStation[]): Layout {
  // Equirectangular projection: longitude -> x, latitude -> y (north = larger y,
  // matching the core's y-up grid convention). Scale longitude by
  // cos(meanLatitude) so east-west distance isn't exaggerated at London's
  // latitude. Absolute units are irrelevant — the core rescales to fit.
  const meanLat =
    raw.reduce((sum, s) => sum + s.Latitude, 0) / (raw.length || 1);
  const lonScale = Math.cos((meanLat * Math.PI) / 180);

  const stations = raw.map((s) => ({
    id: s.NaptanId,
    label: cleanLabel(s.Name),
    x: s.Longitude * lonScale,
    y: s.Latitude,
  }));

  // Single line: connect stations consecutively in file order.
  const edges = raw.slice(1).map((s, i) => ({
    line: JUBILEE.id,
    from: raw[i].NaptanId,
    to: s.NaptanId,
  }));

  return { lines: [JUBILEE], stations, edges };
}
