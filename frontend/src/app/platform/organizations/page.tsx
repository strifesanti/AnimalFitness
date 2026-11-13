import { Suspense } from "react";
import { PlatformOrganizations } from "@/features/admin/platform-organizations";

export const instant = false;

export default function PlatformOrganizationsPage() {
  return (
    <Suspense
      fallback={
        <p className="text-sm text-[var(--muted)]">Cargando organizaciones…</p>
      }
    >
      <PlatformOrganizations />
    </Suspense>
  );
}
