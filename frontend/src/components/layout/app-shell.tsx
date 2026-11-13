"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { Menu, X } from "lucide-react";
import { useState } from "react";
import { BrandMark } from "@/components/brand/brand-mark";
import { Button } from "@/components/ui/button";
import { useAuth } from "@/features/auth/auth-context";
import { cn } from "@/lib/utils";
import type { NavItem } from "@/lib/navigation";

type AppShellProps = {
  title: string;
  subtitle?: string;
  items: NavItem[];
  /** Controles opcionales a la derecha del encabezado (p. ej. la campana de notificaciones). */
  headerActions?: React.ReactNode;
  children: React.ReactNode;
};

export function AppShell({
  title,
  subtitle,
  items,
  headerActions,
  children,
}: AppShellProps) {
  const pathname = usePathname();
  const router = useRouter();
  const { user, logout } = useAuth();
  const [open, setOpen] = useState(false);

  async function onLogout() {
    await logout();
    router.replace("/login");
  }

  return (
    <div className="min-h-screen bg-[var(--background)] text-[var(--foreground)]">
      <div className="pointer-events-none fixed inset-0 bg-[radial-gradient(ellipse_at_top,_rgba(124,58,237,0.16),_transparent_55%)]" />
      <div className="relative mx-auto flex min-h-screen max-w-7xl">
        <aside
          className={cn(
            "fixed inset-y-0 left-0 z-40 w-72 border-r border-[var(--border)] bg-[var(--surface)]/95 p-5 backdrop-blur-md transition md:static md:translate-x-0",
            open ? "translate-x-0" : "-translate-x-full md:translate-x-0",
          )}
        >
          <div className="mb-8 flex items-start justify-between gap-2">
            <div className="min-w-0">
              <BrandMark compact size={40} showSlogan />
              {subtitle ? (
                <p className="mt-2 text-[10px] uppercase tracking-[0.18em] text-[var(--muted)]">
                  {subtitle}
                </p>
              ) : null}
            </div>
            <button
              type="button"
              className="md:hidden"
              onClick={() => setOpen(false)}
              aria-label="Cerrar menú"
            >
              <X className="size-5" />
            </button>
          </div>

          <nav className="space-y-1">
            {items.map((item) => {
              const active = pathname === item.href;
              if (!item.enabled) {
                return (
                  <span
                    key={item.href}
                    className="flex items-center justify-between rounded-md px-3 py-2 text-sm text-[var(--muted)] opacity-60"
                  >
                    {item.label}
                    {item.soon ? (
                      <span className="text-[10px] uppercase tracking-wider">Próximamente</span>
                    ) : null}
                  </span>
                );
              }

              return (
                <Link
                  key={item.href}
                  href={item.href}
                  onClick={() => setOpen(false)}
                  className={cn(
                    "block rounded-md px-3 py-2 text-sm font-medium transition",
                    active
                      ? "bg-[var(--accent)] text-[var(--accent-foreground)] shadow-[0_0_20px_rgba(124,58,237,0.35)]"
                      : "text-[var(--muted)] hover:bg-[var(--surface-elevated)] hover:text-[var(--foreground)]",
                  )}
                >
                  {item.label}
                </Link>
              );
            })}
          </nav>

          <div className="absolute bottom-5 left-5 right-5 space-y-3 border-t border-[var(--border)] pt-4">
            <div>
              <p className="text-sm font-medium">
                {user?.firstName} {user?.lastName}
              </p>
              <p className="truncate text-xs text-[var(--muted)]">{user?.email}</p>
            </div>
            <Button variant="secondary" className="w-full" onClick={onLogout}>
              Cerrar sesión
            </Button>
          </div>
        </aside>

        <div className="flex min-h-screen flex-1 flex-col">
          <header className="sticky top-0 z-20 flex items-center justify-between border-b border-[var(--border)] bg-[var(--background)]/80 px-4 py-4 backdrop-blur md:px-8">
            <div className="flex items-center gap-3">
              <button
                type="button"
                className="md:hidden"
                onClick={() => setOpen(true)}
                aria-label="Abrir menú"
              >
                <Menu className="size-5" />
              </button>
              <h1 className="font-[family-name:var(--font-display)] text-xl tracking-wide md:text-2xl">
                {title}
              </h1>
            </div>
            {headerActions ? <div className="flex items-center gap-2">{headerActions}</div> : null}
          </header>
          <main className="flex-1 px-4 py-6 md:px-8 md:py-8">{children}</main>
        </div>
      </div>
      {open ? (
        <button
          type="button"
          className="fixed inset-0 z-30 bg-black/50 md:hidden"
          aria-label="Cerrar menú"
          onClick={() => setOpen(false)}
        />
      ) : null}
    </div>
  );
}
