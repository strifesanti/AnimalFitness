import { Suspense } from "react";
import { TenantRequests } from "@/features/admin/tenant-requests";

export const instant = false;

export default function TenantRequestsPage() {
  return (
    <Suspense
      fallback={
        <p className="text-sm text-[var(--muted)]">Cargando solicitudes…</p>
      }
    >
      <TenantRequests />
    </Suspense>
  );
}
