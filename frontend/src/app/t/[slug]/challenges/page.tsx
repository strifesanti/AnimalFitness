import { Suspense } from "react";
import { ChallengesView } from "@/features/challenges/challenges-view";

export const instant = false;

export default function ChallengesPage() {
  return (
    <Suspense
      fallback={<p className="text-sm text-[var(--muted)]">Cargando retos…</p>}
    >
      <ChallengesView />
    </Suspense>
  );
}
