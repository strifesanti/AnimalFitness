"use client";

import { Bell } from "lucide-react";
import Link from "next/link";
import { useEffect, useState } from "react";
import { notificationsApi } from "@/services/api";

const POLL_MS = 30000;

/** Campana del encabezado: muestra cuántas notificaciones sin leer hay y enlaza a la lista. */
export function NotificationBell({ slug }: { slug: string }) {
  const [unread, setUnread] = useState(0);

  useEffect(() => {
    let active = true;
    async function refresh() {
      try {
        const list = await notificationsApi.list(slug, true);
        if (active) {
          setUnread(list.unreadCount);
        }
      } catch {
        // Sin conexión o sin permiso: la campana simplemente no muestra contador.
      }
    }
    void refresh();
    const timer = setInterval(refresh, POLL_MS);
    return () => {
      active = false;
      clearInterval(timer);
    };
  }, [slug]);

  return (
    <Link
      href={`/t/${slug}/notifications`}
      className="relative rounded-md p-2 text-[var(--muted)] transition hover:bg-white/5 hover:text-[var(--foreground)]"
      aria-label={
        unread > 0 ? `Notificaciones: ${unread} sin leer` : "Notificaciones"
      }
    >
      <Bell className="size-5" />
      {unread > 0 ? (
        <span className="absolute -right-0.5 -top-0.5 min-w-4 rounded-full bg-[var(--accent)] px-1 text-center text-[10px] font-bold leading-4 text-[var(--accent-foreground)]">
          {unread > 99 ? "99+" : unread}
        </span>
      ) : null}
    </Link>
  );
}
