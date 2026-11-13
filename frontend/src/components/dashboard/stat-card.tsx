import { Card, CardDescription, CardTitle } from "@/components/ui/card";

export function StatCard({
  label,
  value,
  hint,
}: {
  label: string;
  value: string | number;
  hint?: string;
}) {
  return (
    <Card className="transition hover:border-[var(--accent)]/40">
      <p className="text-xs uppercase tracking-[0.16em] text-[var(--muted)]">{label}</p>
      <CardTitle className="mt-3 text-3xl text-[var(--accent)]">{value}</CardTitle>
      {hint ? <CardDescription>{hint}</CardDescription> : null}
    </Card>
  );
}
