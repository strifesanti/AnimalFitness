"use client";

import { useParams, useRouter } from "next/navigation";
import { useEffect, useMemo, useState } from "react";
import { AppShell } from "@/components/layout/app-shell";
import { RequireAuth } from "@/components/layout/require-auth";
import { useAuth } from "@/features/auth/auth-context";
import { NotificationBell } from "@/features/notifications/notification-bell";
import { roleLabel, tenantNav } from "@/lib/navigation";
import { ApiError, tenancyApi } from "@/services/api";
import type { MembershipRole } from "@/types/auth";

export function TenantShell({ children }: { children: React.ReactNode }) {
  const params = useParams<{ slug: string }>();
  const slug = params.slug;
  const { user } = useAuth();
  const router = useRouter();
  const [error, setError] = useState<string | null>(null);
  const [ready, setReady] = useState(false);
  const [role, setRole] = useState<MembershipRole | null>(null);

  const membership = useMemo(
    () => user?.memberships.find((item) => item.slug === slug) ?? null,
    [user, slug],
  );

  useEffect(() => {
    let active = true;
    (async () => {
      if (!user) {
        return;
      }
      try {
        const context = await tenancyApi.context(slug);
        if (!active) {
          return;
        }
        if (!context.hasTenant || !context.role) {
          setError("No tienes acceso a esta organización.");
          return;
        }
        setRole(context.role as MembershipRole);
        setReady(true);
      } catch (err) {
        if (!active) {
          return;
        }
        if (err instanceof ApiError) {
          setError(err.message);
        } else {
          setError("No se pudo validar la organización.");
        }
      }
    })();
    return () => {
      active = false;
    };
  }, [slug, user]);

  useEffect(() => {
    if (user && !membership && !user.isSuperAdmin) {
      router.replace("/");
    }
  }, [user, membership, router]);

  if (error) {
    return (
      <RequireAuth>
        <div className="grid min-h-screen place-items-center px-6 text-center">
          <div>
            <p className="font-[family-name:var(--font-display)] text-2xl tracking-wide">
              Acceso denegado
            </p>
            <p className="mt-2 text-sm text-[var(--muted)]">{error}</p>
          </div>
        </div>
      </RequireAuth>
    );
  }

  if (!ready || !role) {
    return (
      <RequireAuth>
        <div className="grid min-h-screen place-items-center text-[var(--muted)]">
          Validando organización…
        </div>
      </RequireAuth>
    );
  }

  return (
    <RequireAuth>
      <AppShell
        title={membership?.tenantName ?? slug}
        subtitle={roleLabel(role)}
        items={tenantNav(slug, role)}
        headerActions={<NotificationBell slug={slug} />}
      >
        {children}
      </AppShell>
    </RequireAuth>
  );
}
