import { Suspense } from "react";
import { TenantSettings } from "@/features/admin/tenant-settings";

export const instant = false;

export default function TenantSettingsPage() {
  return (
    <Suspense
      fallback={
        <p className="text-sm text-[var(--muted)]">Cargando configuración…</p>
      }
    >
      <TenantSettings />
    </Suspense>
  );
}
