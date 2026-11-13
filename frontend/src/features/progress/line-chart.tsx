import { formatDate } from "@/lib/admin";
import { formatValue } from "@/lib/progress";
import type { ChartPoint } from "@/types/progress";

const WIDTH = 320;
const HEIGHT = 140;
const PAD_X = 12;
const PAD_Y = 16;

type Props = {
  title: string;
  unit: string;
  points: ChartPoint[];
};

/** Gráfico de línea ligero en SVG (sin dependencias). */
export function LineChart({ title, unit, points }: Props) {
  const sorted = [...points].sort(
    (a, b) => new Date(a.at).getTime() - new Date(b.at).getTime(),
  );
  const last = sorted.at(-1);
  const first = sorted[0];

  return (
    <div className="rounded-lg border border-[var(--border)] bg-[var(--surface-elevated)] p-3">
      <div className="flex items-baseline justify-between gap-2">
        <p className="text-sm font-medium">{title}</p>
        <p className="text-sm text-[var(--accent)]">
          {last ? formatValue(last.value, unit) : "—"}
        </p>
      </div>

      {sorted.length === 0 ? (
        <p className="mt-6 pb-6 text-center text-xs text-[var(--muted)]">
          Sin datos en el rango.
        </p>
      ) : (
        <>
          <Plot points={sorted} title={title} />
          <div className="flex justify-between text-[10px] text-[var(--muted)]">
            <span>{first ? formatDate(first.at) : ""}</span>
            <span>{last && last !== first ? formatDate(last.at) : ""}</span>
          </div>
        </>
      )}
    </div>
  );
}

function Plot({ points, title }: { points: ChartPoint[]; title: string }) {
  const values = points.map((point) => point.value);
  let min = Math.min(...values);
  let max = Math.max(...values);
  if (min === max) {
    min -= 1;
    max += 1;
  }

  const times = points.map((point) => new Date(point.at).getTime());
  const t0 = times[0];
  const span = times[times.length - 1] - t0;

  const coords = points.map((point, index) => {
    const x =
      points.length === 1 || span === 0
        ? WIDTH / 2
        : PAD_X + ((times[index] - t0) / span) * (WIDTH - PAD_X * 2);
    const y =
      HEIGHT - PAD_Y - ((point.value - min) / (max - min)) * (HEIGHT - PAD_Y * 2);
    return { x, y, point };
  });
  const path = coords.map(({ x, y }) => `${x.toFixed(1)},${y.toFixed(1)}`).join(" ");

  return (
    <svg
      viewBox={`0 0 ${WIDTH} ${HEIGHT}`}
      role="img"
      aria-label={`Evolución de ${title}`}
      className="mt-2 h-36 w-full"
    >
      <line
        x1={PAD_X}
        x2={WIDTH - PAD_X}
        y1={HEIGHT - PAD_Y}
        y2={HEIGHT - PAD_Y}
        stroke="var(--border)"
      />
      <text x={PAD_X} y={10} fontSize="9" fill="var(--muted)">
        {formatValue(max)}
      </text>
      <text x={PAD_X} y={HEIGHT - 4} fontSize="9" fill="var(--muted)">
        {formatValue(min)}
      </text>
      {coords.length > 1 ? (
        <polyline
          points={path}
          fill="none"
          stroke="var(--accent)"
          strokeWidth="2"
          strokeLinejoin="round"
          strokeLinecap="round"
        />
      ) : null}
      {coords.map(({ x, y, point }) => (
        <circle key={`${point.at}-${point.value}`} cx={x} cy={y} r="3" fill="var(--accent)">
          <title>{`${formatDate(point.at)}: ${formatValue(point.value)}`}</title>
        </circle>
      ))}
    </svg>
  );
}
