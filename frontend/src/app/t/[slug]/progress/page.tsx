import { Suspense } from "react";
import { ProgressView } from "@/features/progress/progress-view";

export const instant = false;

export default function ProgressPage() {
  return (
    <Suspense
      fallback={<p className="text-sm text-[var(--muted)]">Cargando progreso…</p>}
    >
      <ProgressView />
    </Suspense>
  );
}