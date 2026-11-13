import { Suspense } from "react";
import { PlanDetail } from "@/features/training/plan-detail";

export const instant = false;

export default function WorkoutPlanPage() {
  return (
    <Suspense
      fallback={<p className="text-sm text-[var(--muted)]">Cargando plan…</p>}
    >
      <PlanDetail />
    </Suspense>
  );
}
