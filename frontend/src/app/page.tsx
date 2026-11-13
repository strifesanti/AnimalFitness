"use client";

import { useRouter } from "next/navigation";
import { useEffect } from "react";
import { useAuth } from "@/features/auth/auth-context";
import { resolveHomePath } from "@/lib/navigation";

export default function HomePage() {
  const { user, loading } = useAuth();
  const router = useRouter();

  useEffect(() => {
    if (loading) {
      return;
    }
    router.replace(user ? resolveHomePath(user) : "/login");
  }, [loading, user, router]);

  return (
    <div className="grid min-h-screen place-items-center text-[var(--muted)]">
      Redirigiendo…
    </div>
  );
}
