import type { TubeMapViewModel } from "../core/types";

/**
 * Pure render of a `TubeMapViewModel` to SVG. No state, no handlers — this is
 * the thin, near-mechanical layer the Fable port will reimplement.
 */
export function TubeMap({ viewModel }: { viewModel: TubeMapViewModel }) {
  const { stations, edges, bounds } = viewModel;

  return (
    <svg
      viewBox={`0 0 ${bounds.width} ${bounds.height}`}
      style={{ width: "100%", height: "auto", display: "block" }}
      role="img"
      aria-label="Jubilee line map"
    >
      {/* Edges first, so stations sit on top of the line. */}
      {edges.map((e, i) => (
        <path
          key={i}
          d={e.d}
          stroke={e.colour}
          strokeWidth={8}
          strokeLinecap="round"
          strokeLinejoin="round"
          fill="none"
        />
      ))}

      {/* Stations: white fill + coloured ring, tube-map style. */}
      {stations.map((s) => (
        <g key={s.id}>
          <circle cx={s.cx} cy={s.cy} r={6} fill="#ffffff" stroke="#A0A5A9" strokeWidth={3} />
          <text
            x={s.cx + 10}
            y={s.cy + 4}
            fontSize={12}
            fontFamily="system-ui, sans-serif"
            fill="#111111"
          >
            {s.label}
          </text>
        </g>
      ))}
    </svg>
  );
}
