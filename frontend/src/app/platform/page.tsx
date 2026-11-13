"use client";

import Link from "next/link";
import { StatCard } from "@/components/dashboard/stat-card";
import { Card, CardDescription, CardTitle } from "@/components/ui/card";
import { useAuth } from "@/features/auth/auth-context";

export default function PlatformDashboardPage() {
  const { user } = useAuth();
  const membershipCount = user?.memberships.length ?? 0;

  return (
    <div className="space-y-8">
      <div>
        <p className="text-sm text-[var(--muted)]">
          Hola, {user?.firstName}. Vista global de AnimalFitness.
        </p>
      </div>

      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-3">
        <StatCard
          label="Rol global"
          value="SuperAdmin"
          hint="Administración de la plataforma"
        />
        <StatCard
          label="Membresías propias"
          value={membershipCount}
          hint="Organizaciones a las que también perteneces"
        />
        <StatCard
          label="Correo confirmado"
          value={user?.emailConfirmed ? "Sí" : "No"}
        />
      </div>

      <Card>
        <CardTitle>Organizaciones disponibles</CardTitle>
        <CardDescription>
          En esta fase solo se listan las membresías de tu cuenta. La gestión
          completa de academias llega en la Fase 2.
        </CardDescription>
        <ul className="mt-4 space-y-2">
          {user?.memberships.length ? (
            user.memberships.map((membership) => (
              <li key={membership.tenantId}>
                <Link
                  href={`/t/${membership.slug}`}
                  className="flex items-center justify-between rounded-md border border-[var(--border)] px-3 py-2 hover:border-[var(--accent)]/50"
                >
                  <span>{membership.tenantName}</span>
                  <span className="text-xs uppercase tracking-wider text-[var(--muted)]">
                    {membership.role}
                  </span>
                </Link>
              </li>
            ))
          ) : (
            <li className="text-sm text-[var(--muted)]">
              No tienes membresías de academia asociadas.
            </li>
          )}
        </ul>
      </Card>
    </div>
  );
}
