import { Suspense } from "react";
import { TenantShell } from "@/features/tenancy/tenant-shell";

export const instant = false;

export default function TenantLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <Suspense
      fallback={
        <div className="grid min-h-screen place-items-center text-[var(--muted)]">
          Cargando academia…
        </div>
      }
    >
      <TenantShell>{children}</TenantShell>
    </Suspense>
  );
}
