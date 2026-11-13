import { Suspense } from "react";
import { ExerciseLibrary } from "@/features/training/exercise-library";

export const instant = false;

export default function ExercisesPage() {
  return (
    <Suspense
      fallback={<p className="text-sm text-[var(--muted)]">Cargando biblioteca…</p>}
    >
      <ExerciseLibrary />
    </Suspense>
  );
}
