"use client";

import { useRouter } from "next/navigation";
import { useEffect } from "react";
import { BrandMark } from "@/components/brand/brand-mark";
import { LoginForm } from "@/features/auth/login-form";
import { useAuth } from "@/features/auth/auth-context";
import { resolveHomePath } from "@/lib/navigation";

export default function LoginPage() {
  const { user, loading } = useAuth();
  const router = useRouter();

  useEffect(() => {
    if (!loading && user) {
      const next = new URLSearchParams(window.location.search).get("next");
      const safeNext =
        next && next.startsWith("/") && !next.startsWith("//") ? next : null;
      router.replace(safeNext ?? resolveHomePath(user));
    }
  }, [loading, user, router]);

  return (
    <div className="grid min-h-screen lg:grid-cols-2">
      <section className="relative hidden overflow-hidden lg:block">
        <div className="absolute inset-0 bg-[linear-gradient(145deg,#0a0a0f_8%,#1a1a22_48%,#0a0a0f_100%)]" />
        <div className="absolute inset-0 bg-[radial-gradient(circle_at_28%_18%,rgba(124,58,237,0.38),transparent_46%)]" />
        <div className="absolute inset-0 bg-[radial-gradient(circle_at_80%_70%,rgba(168,85,247,0.16),transparent_40%)]" />
        <div className="relative flex h-full flex-col justify-between p-12">
          <BrandMark size={72} />
          <div>
            <p className="font-[family-name:var(--font-display)] text-6xl leading-[0.92] tracking-[0.04em] text-[var(--foreground)]">
              TU MEJOR
              <br />
              <span className="text-[var(--accent-secondary)]">VERSIÓN</span>
              <br />
              EMPIEZA AQUÍ
            </p>
            <p className="mt-6 max-w-md text-base text-[var(--muted)]">
              Gestión profesional de academias, entrenadores y alumnos. Fuerza,
              disciplina y rendimiento en una sola plataforma.
            </p>
          </div>
        </div>
      </section>

      <section className="flex items-center justify-center px-6 py-12">
        <div className="w-full max-w-md space-y-8">
          <div className="space-y-4">
            <div className="lg:hidden">
              <BrandMark size={56} />
            </div>
            <h1 className="font-[family-name:var(--font-display)] text-4xl tracking-wide text-[var(--foreground)]">
              Inicia sesión
            </h1>
            <p className="text-sm text-[var(--muted)]">
              Usa tu cuenta de AnimalFitness. En Development puedes probar con
              los usuarios demo del backend.
            </p>
          </div>
          <LoginForm />
        </div>
      </section>
    </div>
  );
}
