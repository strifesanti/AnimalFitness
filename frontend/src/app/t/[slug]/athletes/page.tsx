import { Suspense } from "react";
import { TenantAthletes } from "@/features/admin/tenant-athletes";

export const instant = false;

export default function TenantAthletesPage() {
  return (
    <Suspense
      fallback={<p className="text-sm text-[var(--muted)]">Cargando alumnos…</p>}
    >
      <TenantAthletes />
    </Suspense>
  );
}
