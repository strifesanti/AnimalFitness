import { Suspense } from "react";
import { WorkoutList } from "@/features/training/workout-list";

export const instant = false;

export default function WorkoutsPage() {
  return (
    <Suspense
      fallback={<p className="text-sm text-[var(--muted)]">Cargando entrenamientos…</p>}
    >
      <WorkoutList />
    </Suspense>
  );
}
