import { describe, it, expect } from "vitest";
import { buildViewModel } from "./buildViewModel";
import type { Layout, RenderOptions } from "./types";

const line = { id: "jubilee", name: "Jubilee", colour: "#A0A5A9" };

function layout(stations: Layout["stations"], edges: Layout["edges"] = []): Layout {
  return { lines: [line], stations, edges };
}

describe("buildViewModel", () => {
  it("maps grid corners to the padded viewport corners, respecting the Y-flip", () => {
    // Square grid + square avail area => both axes touch the padding edges.
    const opts: RenderOptions = { width: 100, height: 100, padding: 10 };
    const vm = buildViewModel(
      layout([
        { id: "sw", label: "sw", x: 0, y: 0 },
        { id: "nw", label: "nw", x: 0, y: 10 },
        { id: "ne", label: "ne", x: 10, y: 10 },
        { id: "se", label: "se", x: 10, y: 0 },
      ]),
      opts,
    );
    const at = (id: string) => vm.stations.find((s) => s.id === id)!;

    // grid y up = north => north maps to the TOP (small screen y).
    expect(at("sw")).toMatchObject({ cx: 10, cy: 90 }); // bottom-left
    expect(at("nw")).toMatchObject({ cx: 10, cy: 10 }); // top-left
    expect(at("ne")).toMatchObject({ cx: 90, cy: 10 }); // top-right
    expect(at("se")).toMatchObject({ cx: 90, cy: 90 }); // bottom-right
  });

  it("preserves aspect ratio: equal grid deltas give equal screen deltas on both axes", () => {
    // Deliberately non-square viewport: a stretched fit would break this.
    const opts: RenderOptions = { width: 200, height: 100, padding: 0 };
    const vm = buildViewModel(
      layout([
        { id: "a", label: "a", x: 0, y: 0 },
        { id: "b", label: "b", x: 5, y: 0 },
        { id: "c", label: "c", x: 0, y: 5 },
      ]),
      opts,
    );
    const a = vm.stations.find((s) => s.id === "a")!;
    const b = vm.stations.find((s) => s.id === "b")!;
    const c = vm.stations.find((s) => s.id === "c")!;

    const dxScreen = Math.abs(b.cx - a.cx); // from a grid dx of 5
    const dyScreen = Math.abs(c.cy - a.cy); // from a grid dy of 5
    expect(dxScreen).toBeCloseTo(dyScreen);
  });

  it("emits an edge path string referencing its two endpoints' projected coords", () => {
    const opts: RenderOptions = { width: 300, height: 300, padding: 20 };
    const vm = buildViewModel(
      layout(
        [
          { id: "a", label: "a", x: 0, y: 0 },
          { id: "b", label: "b", x: 10, y: 4 },
        ],
        [{ line: "jubilee", from: "a", to: "b" }],
      ),
      opts,
    );
    const a = vm.stations.find((s) => s.id === "a")!;
    const b = vm.stations.find((s) => s.id === "b")!;

    expect(vm.edges).toHaveLength(1);
    expect(vm.edges[0]).toEqual({
      line: "jubilee",
      colour: "#A0A5A9",
      d: `M ${a.cx} ${a.cy} L ${b.cx} ${b.cy}`,
    });
  });

  it("does not throw on an empty layout", () => {
    const vm = buildViewModel(layout([]), { width: 100, height: 100, padding: 10 });
    expect(vm.stations).toEqual([]);
    expect(vm.edges).toEqual([]);
    expect(vm.bounds).toEqual({ width: 100, height: 100 });
  });

  it("centres a single station without producing NaNs", () => {
    const vm = buildViewModel(
      layout([{ id: "only", label: "only", x: 3, y: 3 }]),
      { width: 100, height: 100, padding: 10 },
    );
    expect(vm.stations).toHaveLength(1);
    expect(vm.stations[0]).toMatchObject({ cx: 50, cy: 50 });
    expect(Number.isNaN(vm.stations[0].cx)).toBe(false);
    expect(Number.isNaN(vm.stations[0].cy)).toBe(false);
  });
});
