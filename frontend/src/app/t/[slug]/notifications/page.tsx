import { Suspense } from "react";
import { NotificationsView } from "@/features/notifications/notifications-view";

export const instant = false;

export default function NotificationsPage() {
  return (
    <Suspense
      fallback={
        <p className="text-sm text-[var(--muted)]">Cargando notificaciones…</p>
      }
    >
      <NotificationsView />
    </Suspense>
  );
}
