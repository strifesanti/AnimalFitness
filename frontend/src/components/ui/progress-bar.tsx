import { cn } from "@/lib/utils";

type ProgressBarProps = {
  label: string;
  value: number;
  goal?: number | null;
  unit?: string;
  percent?: number | null;
  className?: string;
};

/** Barra de cumplimiento con valor, objetivo y porcentaje (0–100 visual). */
export function ProgressBar({
  label,
  value,
  goal,
  unit = "",
  percent,
  className,
}: ProgressBarProps) {
  const shown = percent ?? (goal ? (value * 100) / goal : 0);
  const width = Math.min(100, Math.max(0, shown));
  const over = shown > 100;
  const format = (n: number) => Number(n.toFixed(1)).toLocaleString("es");

  return (
    <div className={cn("space-y-1", className)}>
      <div className="flex items-baseline justify-between gap-2 text-sm">
        <span className="font-medium">{label}</span>
        <span className="text-xs text-[var(--muted)]">
          {format(value)}
          {goal ? ` / ${format(goal)}` : ""} {unit}
          {goal ? ` · ${format(shown)}%` : ""}
        </span>
      </div>
      <div
        role="progressbar"
        aria-label={label}
        aria-valuemin={0}
        aria-valuemax={100}
        aria-valuenow={Math.round(width)}
        className="h-2 overflow-hidden rounded-full bg-white/10"
      >
        <div
          className={cn(
            "h-full rounded-full transition-all",
            over ? "bg-amber-400" : "bg-[var(--accent)]",
          )}
          style={{ width: `${width}%` }}
        />
      </div>
    </div>
  );
}
