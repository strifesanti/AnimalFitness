import { Suspense } from "react";
import { NutritionView } from "@/features/nutrition/nutrition-view";

export const instant = false;

export default function NutritionPage() {
  return (
    <Suspense
      fallback={<p className="text-sm text-[var(--muted)]">Cargando nutrición…</p>}
    >
      <NutritionView />
    </Suspense>
  );
}
