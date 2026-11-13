"use client";

import Link from "next/link";
import { useParams } from "next/navigation";
import { useCallback, useEffect, useState } from "react";
import { Button } from "@/components/ui/button";
import { Card, CardDescription, CardTitle } from "@/components/ui/card";
import { Badge, Notice } from "@/components/ui/notice";
import { errorMessage } from "@/lib/admin";
import { formatDateTime } from "@/lib/training";
import { notificationsApi } from "@/services/api";
import type { NotificationList } from "@/types/advanced";

function kindLabel(kind: string): string {
  switch (kind) {
    case "ChatMessage":
      return "Chat";
    case "Challenge":
      return "Reto";
    case "Membership":
      return "Membresía";
    case "Workout":
      return "Entrenamiento";
    case "Reminder":
      return "Recordatorio";
    default:
      return "General";
  }
}

export function NotificationsView() {
  const { slug } = useParams<{ slug: string }>();
  const [data, setData] = useState<NotificationList | null>(null);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    try {
      setData(await notificationsApi.list(slug));
      setError(null);
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setLoading(false);
    }
  }, [slug]);

  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect
    void load();
  }, [load]);

  async function markRead(id: string) {
    setBusy(true);
    try {
      await notificationsApi.markRead(slug, id);
      await load();
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setBusy(false);
    }
  }

  async function markAll() {
    setBusy(true);
    try {
      await notificationsApi.markAllRead(slug);
      await load();
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <div className="space-y-4">
      {error ? <Notice tone="error">{error}</Notice> : null}
      <Card>
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div>
            <CardTitle>Notificaciones</CardTitle>
            <CardDescription>
              {data ? `${data.unreadCount} sin leer` : "Cargando…"}
            </CardDescription>
          </div>
          <Button
            size="sm"
            variant="secondary"
            disabled={busy || !data || data.unreadCount === 0}
            onClick={markAll}
          >
            Marcar todas como leídas
          </Button>
        </div>

        {loading ? null : !data || data.items.length === 0 ? (
          <p className="mt-4 text-sm text-[var(--muted)]">No tienes notificaciones.</p>
        ) : (
          <ul className="mt-4 space-y-2">
            {data.items.map((item) => (
              <li
                key={item.id}
                className={`rounded-md border px-3 py-2 ${
                  item.isRead
                    ? "border-[var(--border)] opacity-70"
                    : "border-[var(--accent)]/40 bg-[var(--accent)]/5"
                }`}
              >
                <div className="flex flex-wrap items-start justify-between gap-3">
                  <div className="min-w-0">
                    <p className="text-sm font-medium">
                      {item.title}{" "}
                      <Badge tone={item.isRead ? "neutral" : "good"}>
                        {kindLabel(String(item.kind))}
                      </Badge>
                    </p>
                    <p className="text-sm text-[var(--muted)]">{item.body}</p>
                    <p className="text-xs text-[var(--muted)]">
                      {formatDateTime(item.raisedAt)}
                    </p>
                  </div>
                  <div className="flex gap-2">
                    {item.linkPath ? (
                      <Link
                        href={`/t/${slug}${item.linkPath}`}
                        onClick={() => {
                          if (!item.isRead) {
                            void markRead(item.id);
                          }
                        }}
                        className="inline-flex h-9 items-center rounded-md border border-[var(--border)] px-3 text-xs font-semibold hover:border-[var(--accent)]"
                      >
                        Abrir
                      </Link>
                    ) : null}
                    {!item.isRead ? (
                      <Button
                        size="sm"
                        variant="ghost"
                        disabled={busy}
                        onClick={() => markRead(item.id)}
                      >
                        Leída
                      </Button>
                    ) : null}
                  </div>
                </div>
              </li>
            ))}
          </ul>
        )}
      </Card>
    </div>
  );
}
