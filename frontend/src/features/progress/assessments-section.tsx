"use client";

import { useCallback, useEffect, useState } from "react";
import { Button } from "@/components/ui/button";
import { Card, CardDescription, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Badge, Notice } from "@/components/ui/notice";
import { errorMessage, formatDate, textareaClassName } from "@/lib/admin";
import { dateInputToIso, todayInputValue } from "@/lib/progress";
import { intOrNull } from "@/lib/training";
import { progressApi } from "@/services/api";
import type { FitnessAssessment } from "@/types/progress";

type Props = {
  slug: string;
  athleteId: string | null;
  needsAthlete: boolean;
};

export function AssessmentsSection({ slug, athleteId, needsAthlete }: Props) {
  const [items, setItems] = useState<FitnessAssessment[]>([]);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [form, setForm] = useState(() => ({
    date: todayInputValue(),
    title: "",
    summary: "",
    score: "",
  }));

  const load = useCallback(async () => {
    if (needsAthlete) {
      setItems([]);
      setLoading(false);
      return;
    }
    try {
      setItems(await progressApi.listAssessments(slug, athleteId));
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
      await progressApi.createAssessment(slug, {
        athleteProfileId: athleteId,
        assessedAt: dateInputToIso(form.date),
        title: form.title.trim(),
        summary: form.summary.trim(),
        score: intOrNull(form.score),
      });
      setSuccess("Valoración registrada.");
      setForm({ date: todayInputValue(), title: "", summary: "", score: "" });
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
        <CardTitle>Nueva valoración</CardTitle>
        <CardDescription>
          Observaciones de fuerza, movilidad o condición general.
        </CardDescription>
        <form onSubmit={submit} className="mt-4 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          <div className="space-y-2 sm:col-span-2">
            <Label htmlFor="a-title">Título</Label>
            <Input
              id="a-title"
              required
              value={form.title}
              onChange={(e) => setForm({ ...form, title: e.target.value })}
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="a-date">Fecha</Label>
            <Input
              id="a-date"
              type="date"
              required
              value={form.date}
              onChange={(e) => setForm({ ...form, date: e.target.value })}
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="a-score">Puntuación (1-10)</Label>
            <Input
              id="a-score"
              inputMode="numeric"
              value={form.score}
              onChange={(e) => setForm({ ...form, score: e.target.value })}
            />
          </div>
          <div className="space-y-2 sm:col-span-2 lg:col-span-4">
            <Label htmlFor="a-summary">Resumen</Label>
            <textarea
              id="a-summary"
              required
              className={textareaClassName}
              value={form.summary}
              onChange={(e) => setForm({ ...form, summary: e.target.value })}
            />
          </div>
          <div className="sm:col-span-2 lg:col-span-4">
            <Button type="submit" disabled={busy}>
              {busy ? "Guardando…" : "Registrar valoración"}
            </Button>
          </div>
        </form>
      </Card>

      <Card>
        <CardTitle>Valoraciones</CardTitle>
        {loading ? (
          <p className="mt-3 text-sm text-[var(--muted)]">Cargando…</p>
        ) : items.length === 0 ? (
          <p className="mt-3 text-sm text-[var(--muted)]">Aún no hay valoraciones.</p>
        ) : (
          <ul className="mt-3 space-y-2">
            {items.map((item) => (
              <li
                key={item.id}
                className="rounded-md border border-[var(--border)] px-3 py-2"
              >
                <p className="text-sm font-medium">
                  {item.title}{" "}
                  {item.score !== null ? (
                    <Badge tone="good">{item.score}/10</Badge>
                  ) : null}
                </p>
                <p className="text-xs text-[var(--muted)]">
                  {formatDate(item.assessedAt)}
                </p>
                <p className="mt-1 whitespace-pre-wrap text-sm">{item.summary}</p>
              </li>
            ))}
          </ul>
        )}
      </Card>
    </div>
  );
}
