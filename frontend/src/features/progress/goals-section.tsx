"use client";

import { useCallback, useEffect, useState } from "react";
import { Button } from "@/components/ui/button";
import { Card, CardDescription, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Badge, Notice } from "@/components/ui/notice";
import { errorMessage, formatDate } from "@/lib/admin";
import {
  formatValue,
  goalStatus,
  goalStatusLabel,
  goalStatusTone,
} from "@/lib/progress";
import { numberOrNull, textOrNull } from "@/lib/training";
import { progressApi } from "@/services/api";
import type { ProgressGoal } from "@/types/progress";

type Props = {
  slug: string;
  athleteId: string | null;
  needsAthlete: boolean;
};

const emptyForm = {
  title: "",
  description: "",
  targetValue: "",
  unit: "",
  dueDate: "",
};

export function GoalsSection({ slug, athleteId, needsAthlete }: Props) {
  const [items, setItems] = useState<ProgressGoal[]>([]);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [form, setForm] = useState(emptyForm);

  const load = useCallback(async () => {
    if (needsAthlete) {
      setItems([]);
      setLoading(false);
      return;
    }
    try {
      setItems(await progressApi.listGoals(slug, athleteId));
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

  async function create(event: React.FormEvent) {
    event.preventDefault();
    setBusy(true);
    setError(null);
    setSuccess(null);
    try {
      await progressApi.createGoal(slug, {
        athleteProfileId: athleteId,
        title: form.title.trim(),
        description: textOrNull(form.description),
        targetValue: numberOrNull(form.targetValue),
        unit: textOrNull(form.unit),
        dueDate: form.dueDate || null,
      });
      setSuccess("Objetivo creado.");
      setForm(emptyForm);
      await load();
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setBusy(false);
    }
  }

  async function change(goal: ProgressGoal, kind: "complete" | "cancel") {
    setBusy(true);
    setError(null);
    setSuccess(null);
    try {
      const message =
        kind === "complete"
          ? await progressApi.completeGoal(slug, goal.id)
          : await progressApi.cancelGoal(slug, goal.id);
      setSuccess(message || "Objetivo actualizado.");
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
        <CardTitle>Nuevo objetivo</CardTitle>
        <CardDescription>
          Ej. «Bajar a 75 kg» con un valor objetivo y fecha límite opcional.
        </CardDescription>
        <form onSubmit={create} className="mt-4 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          <div className="space-y-2 sm:col-span-2">
            <Label htmlFor="g-title">Título</Label>
            <Input
              id="g-title"
              required
              value={form.title}
              onChange={(e) => setForm({ ...form, title: e.target.value })}
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="g-target">Valor objetivo</Label>
            <Input
              id="g-target"
              inputMode="decimal"
              value={form.targetValue}
              onChange={(e) => setForm({ ...form, targetValue: e.target.value })}
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="g-unit">Unidad</Label>
            <Input
              id="g-unit"
              placeholder="kg, %, cm…"
              value={form.unit}
              onChange={(e) => setForm({ ...form, unit: e.target.value })}
            />
          </div>
          <div className="space-y-2 sm:col-span-2">
            <Label htmlFor="g-desc">Descripción</Label>
            <Input
              id="g-desc"
              value={form.description}
              onChange={(e) => setForm({ ...form, description: e.target.value })}
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="g-due">Fecha límite</Label>
            <Input
              id="g-due"
              type="date"
              value={form.dueDate}
              onChange={(e) => setForm({ ...form, dueDate: e.target.value })}
            />
          </div>
          <div className="flex items-end">
            <Button type="submit" disabled={busy}>
              {busy ? "Guardando…" : "Crear objetivo"}
            </Button>
          </div>
        </form>
      </Card>

      <Card>
        <CardTitle>Objetivos</CardTitle>
        {loading ? (
          <p className="mt-3 text-sm text-[var(--muted)]">Cargando…</p>
        ) : items.length === 0 ? (
          <p className="mt-3 text-sm text-[var(--muted)]">Aún no hay objetivos.</p>
        ) : (
          <ul className="mt-3 space-y-2">
            {items.map((goal) => {
              const status = goalStatus(goal.status);
              return (
                <li
                  key={goal.id}
                  className="flex flex-wrap items-center justify-between gap-3 rounded-md border border-[var(--border)] px-3 py-2"
                >
                  <div className="min-w-0">
                    <p className="text-sm font-medium">
                      {goal.title}{" "}
                      <Badge tone={goalStatusTone(status)}>
                        {goalStatusLabel(status)}
                      </Badge>
                    </p>
                    <p className="text-xs text-[var(--muted)]">
                      {goal.targetValue !== null
                        ? `Meta: ${formatValue(goal.targetValue, goal.unit ?? "")}`
                        : "Sin valor objetivo"}
                      {goal.dueDate ? ` · Límite: ${formatDate(goal.dueDate)}` : ""}
                      {goal.description ? ` · ${goal.description}` : ""}
                    </p>
                  </div>
                  {status === "Active" ? (
                    <div className="flex gap-2">
                      <Button
                        size="sm"
                        disabled={busy}
                        onClick={() => change(goal, "complete")}
                      >
                        Cumplido
                      </Button>
                      <Button
                        size="sm"
                        variant="secondary"
                        disabled={busy}
                        onClick={() => change(goal, "cancel")}
                      >
                        Cancelar
                      </Button>
                    </div>
                  ) : null}
                </li>
              );
            })}
          </ul>
        )}
      </Card>
    </div>
  );
}
