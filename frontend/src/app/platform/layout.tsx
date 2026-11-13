"use client";

import { useRouter } from "next/navigation";
import { useEffect } from "react";
import { AppShell } from "@/components/layout/app-shell";
import { RequireAuth } from "@/components/layout/require-auth";
import { useAuth } from "@/features/auth/auth-context";
import { platformNav } from "@/lib/navigation";

export default function PlatformLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const { user } = useAuth();
  const router = useRouter();

  useEffect(() => {
    if (user && !user.isSuperAdmin) {
      router.replace("/");
    }
  }, [user, router]);

  return (
    <RequireAuth>
      <AppShell title="Plataforma" subtitle="SuperAdmin" items={platformNav()}>
        {children}
      </AppShell>
    </RequireAuth>
  );
}
