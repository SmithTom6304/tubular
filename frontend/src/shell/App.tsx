import { buildViewModel } from "../core/buildViewModel";
import { layoutFromRawStations, type RawStation } from "../data/layoutAdapter";
import rawStations from "../data/stratford-stanmore.json";
import { TubeMap } from "./TubeMap";

const layout = layoutFromRawStations(rawStations as RawStation[]);
const viewModel = buildViewModel(layout, { width: 1000, height: 1400, padding: 48 });

export function App() {
  return (
    <main
      style={{
        maxWidth: 720,
        margin: "0 auto",
        padding: 16,
        fontFamily: "system-ui, sans-serif",
      }}
    >
      <h1 style={{ fontSize: 20, fontWeight: 600 }}>Jubilee line — Stratford to Stanmore</h1>
      <TubeMap viewModel={viewModel} />
    </main>
  );
}
