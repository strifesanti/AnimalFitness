"use client";

import { useParams } from "next/navigation";
import { StatCard } from "@/components/dashboard/stat-card";
import { Card, CardDescription, CardTitle } from "@/components/ui/card";
import { useAuth } from "@/features/auth/auth-context";
import { roleLabel } from "@/lib/navigation";

export function TenantDashboard() {
  const params = useParams<{ slug: string }>();
  const { user } = useAuth();
  const membership = user?.memberships.find((item) => item.slug === params.slug);

  if (!membership) {
    return (
      <Card>
        <CardTitle>Organización</CardTitle>
        <CardDescription>
          No encontramos esta academia en tus membresías activas.
        </CardDescription>
      </Card>
    );
  }

  return (
    <div className="space-y-8">
      <div>
        <p className="text-sm text-[var(--muted)]">
          {roleLabel(membership.role)} en {membership.tenantName}
        </p>
      </div>

      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
        <StatCard label="Tu rol" value={roleLabel(membership.role)} />
        <StatCard label="Estado academia" value={membership.tenantStatus} />
        <StatCard label="Estado membresía" value={membership.membershipStatus} />
      </div>

      <Card>
        <CardTitle>Fase 1 operativa</CardTitle>
        <CardDescription>
          Aquí ya tienes sesión, aislamiento multi-tenant y navegación por rol.
          Entrenamientos, pagos, chat y el resto de módulos aparecen como
          “Próximamente” hasta su fase.
        </CardDescription>
        <div className="mt-4 grid gap-2 text-sm text-[var(--muted)]">
          <p>Slug: {membership.slug}</p>
          <p>TenantId: {membership.tenantId}</p>
          <p>Usuario: {user?.email}</p>
        </div>
      </Card>
    </div>
  );
}
