"use client";

import { useRouter } from "next/navigation";
import { useEffect } from "react";
import { useAuth } from "@/features/auth/auth-context";

export function RequireAuth({
  children,
  loginHref = "/login",
}: {
  children: React.ReactNode;
  loginHref?: string;
}) {
  const { user, loading } = useAuth();
  const router = useRouter();

  useEffect(() => {
    if (!loading && !user) {
      router.replace(loginHref);
    }
  }, [loading, user, router, loginHref]);

  if (loading) {
    return (
      <div className="grid min-h-screen place-items-center bg-[var(--background)] text-[var(--muted)]">
        Cargando sesión…
      </div>
    );
  }

  if (!user) {
    return null;
  }

  return <>{children}</>;
}
