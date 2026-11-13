"use client";

import { useCallback, useEffect, useState } from "react";
import { Button } from "@/components/ui/button";
import { Card, CardDescription, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Notice } from "@/components/ui/notice";
import { errorMessage, formatDate, selectClassName } from "@/lib/admin";
import {
  dateInputToIso,
  PHOTO_POSES,
  photoPose,
  poseLabel,
  todayInputValue,
} from "@/lib/progress";
import { textOrNull } from "@/lib/training";
import { progressApi } from "@/services/api";
import type { PhotoPose, ProgressPhoto } from "@/types/progress";

type Props = {
  slug: string;
  athleteId: string | null;
  needsAthlete: boolean;
};

export function PhotosSection({ slug, athleteId, needsAthlete }: Props) {
  const [items, setItems] = useState<ProgressPhoto[]>([]);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [form, setForm] = useState(() => ({
    date: todayInputValue(),
    pose: "Front" as PhotoPose,
    url: "",
    caption: "",
  }));

  const load = useCallback(async () => {
    if (needsAthlete) {
      setItems([]);
      setLoading(false);
      return;
    }
    try {
      setItems(await progressApi.listPhotos(slug, athleteId));
      setError(null);
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setLoading(false);
    }
  }, [slug, athleteId, needsAthlete]);

  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect
    void load();
  }, [load]);

  async function submit(event: React.FormEvent) {
    event.preventDefault();
    setBusy(true);
    setError(null);
    setSuccess(null);
    try {
      await progressApi.createPhoto(slug, {
        athleteProfileId: athleteId,
        takenAt: dateInputToIso(form.date),
        pose: form.pose,
        url: form.url.trim(),
        caption: textOrNull(form.caption),
        notes: null,
      });
      setSuccess("Foto registrada.");
      setForm({ date: todayInputValue(), pose: "Front", url: "", caption: "" });
      await load();
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setBusy(false);
    }
  }

  if (needsAthlete) {
    return null;
  }

  return (
    <div className="space-y-6">
      {error ? <Notice tone="error">{error}</Notice> : null}
      {success ? <Notice tone="success">{success}</Notice> : null}

      <Card>
        <CardTitle>Nueva foto de progreso</CardTitle>
        <CardDescription>
          Indica la URL de la imagen (aún no se suben archivos).
        </CardDescription>
        <form onSubmit={submit} className="mt-4 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          <div className="space-y-2 sm:col-span-2">
            <Label htmlFor="p-url">URL de la imagen</Label>
            <Input
              id="p-url"
              type="url"
              required
              placeholder="https://…"
              value={form.url}
              onChange={(e) => setForm({ ...form, url: e.target.value })}
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="p-pose">Pose</Label>
            <select
              id="p-pose"
              className={selectClassName}
              value={form.pose}
              onChange={(e) =>
                setForm({ ...form, pose: e.target.value as PhotoPose })
              }
            >
              {PHOTO_POSES.map((pose) => (
                <option key={pose} value={pose}>
                  {poseLabel(pose)}
                </option>
              ))}
            </select>
          </div>
          <div className="space-y-2">
            <Label htmlFor="p-date">Fecha</Label>
            <Input
              id="p-date"
              type="date"
              required
              value={form.date}
              onChange={(e) => setForm({ ...form, date: e.target.value })}
            />
          </div>
          <div className="space-y-2 sm:col-span-2 lg:col-span-4">
            <Label htmlFor="p-caption">Leyenda</Label>
            <Input
              id="p-caption"
              value={form.caption}
              onChange={(e) => setForm({ ...form, caption: e.target.value })}
            />
          </div>
          <div className="sm:col-span-2 lg:col-span-4">
            <Button type="submit" disabled={busy}>
              {busy ? "Guardando…" : "Agregar foto"}
            </Button>
          </div>
        </form>
      </Card>

      <Card>
        <CardTitle>Fotos</CardTitle>
        {loading ? (
          <p className="mt-3 text-sm text-[var(--muted)]">Cargando…</p>
        ) : items.length === 0 ? (
          <p className="mt-3 text-sm text-[var(--muted)]">Aún no hay fotos.</p>
        ) : (
          <ul className="mt-4 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {items.map((photo) => (
              <li
                key={photo.id}
                className="overflow-hidden rounded-lg border border-[var(--border)] bg-[var(--surface-elevated)]"
              >
                <a href={photo.url} target="_blank" rel="noopener noreferrer">
                  {/* eslint-disable-next-line @next/next/no-img-element */}
                  <img
                    src={photo.url}
                    alt={photo.caption ?? `Foto ${poseLabel(photo.pose)}`}
                    loading="lazy"
                    referrerPolicy="no-referrer"
                    className="h-48 w-full bg-black/30 object-cover"
                  />
                </a>
                <div className="p-3 text-sm">
                  <p className="font-medium">
                    {poseLabel(photoPose(photo.pose))} · {formatDate(photo.takenAt)}
                  </p>
                  {photo.caption ? (
                    <p className="text-xs text-[var(--muted)]">{photo.caption}</p>
                  ) : null}
                </div>
              </li>
            ))}
          </ul>
        )}
      </Card>
    </div>
  );
}
