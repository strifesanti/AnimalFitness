import { Suspense } from "react";
import { TenantDashboard } from "@/features/tenancy/tenant-dashboard";

export const instant = false;

export default function TenantDashboardPage() {
  return (
    <Suspense
      fallback={<p className="text-sm text-[var(--muted)]">Cargando panel…</p>}
    >
      <TenantDashboard />
    </Suspense>
  );
}
