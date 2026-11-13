import { Suspense } from "react";
import { TenantTrainers } from "@/features/admin/tenant-trainers";

export const instant = false;

export default function TenantTrainersPage() {
  return (
    <Suspense
      fallback={
        <p className="text-sm text-[var(--muted)]">Cargando entrenadores…</p>
      }
    >
      <TenantTrainers />
    </Suspense>
  );
}
