"use client";

import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { useCallback, useEffect, useState } from "react";
import { Button } from "@/components/ui/button";
import { Card, CardDescription, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Badge, Notice } from "@/components/ui/notice";
import { useAuth } from "@/features/auth/auth-context";
import { WorkoutSessions } from "@/features/training/workout-sessions";
import { errorMessage, normalizeRole, textareaClassName } from "@/lib/admin";
import {
  planStatus,
  planStatusLabel,
  planStatusTone,
  textOrNull,
} from "@/lib/training";
import { trainingApi } from "@/services/api";
import type { WorkoutPlanSummary } from "@/types/training";

export function WorkoutList() {
  const { slug } = useParams<{ slug: string }>();
  const router = useRouter();
  const { user } = useAuth();
  const membership = user?.memberships.find((item) => item.slug === slug);
  const canManage = membership
    ? normalizeRole(membership.role) !== "Athlete"
    : false;

  const [plans, setPlans] = useState<WorkoutPlanSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [form, setForm] = useState({ name: "", goalLabel: "", description: "" });

  const load = useCallback(async () => {
    try {
      setPlans(await trainingApi.listPlans(slug));
      setError(null);
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setLoading(false);
    }
  }, [slug]);

  useEffect(() => {
    if (!membership) {
      return;
    }
    // eslint-disable-next-line react-hooks/set-state-in-effect
    void load();
  }, [load, membership]);

  async function createDraft(event: React.FormEvent) {
    event.preventDefault();
    setBusy(true);
    setError(null);
    try {
      const plan = await trainingApi.createPlan(slug, {
        name: form.name.trim(),
        goalLabel: textOrNull(form.goalLabel),
        description: textOrNull(form.description),
      });
      router.push(`/t/${slug}/workouts/${plan.id}`);
    } catch (err) {
      setError(errorMessage(err));
      setBusy(false);
    }
  }

  return (
    <div className="space-y-8">
      <p className="text-sm text-[var(--muted)]">
        {canManage
          ? "Crea planes de entrenamiento, publícalos y asígnalos a tus alumnos."
          : "Planes que tu entrenador te ha asignado."}
      </p>

      {error ? <Notice tone="error">{error}</Notice> : null}

      {canManage ? (
        <Card>
          <CardTitle>Nuevo plan</CardTitle>
          <CardDescription>
            Se crea como borrador; después agregas días y ejercicios.
          </CardDescription>
          <form
            onSubmit={createDraft}
            className="mt-4 grid gap-4 sm:grid-cols-2"
          >
            <div className="space-y-2">
              <Label htmlFor="np-name">Nombre</Label>
              <Input
                id="np-name"
                required
                value={form.name}
                onChange={(e) => setForm({ ...form, name: e.target.value })}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="np-goal">Objetivo</Label>
              <Input
                id="np-goal"
                placeholder="Ej. Hipertrofia"
                value={form.goalLabel}
                onChange={(e) => setForm({ ...form, goalLabel: e.target.value })}
              />
            </div>
            <div className="space-y-2 sm:col-span-2">
              <Label htmlFor="np-desc">Descripción</Label>
              <textarea
                id="np-desc"
                className={textareaClassName}
                value={form.description}
                onChange={(e) =>
                  setForm({ ...form, description: e.target.value })
                }
              />
            </div>
            <div className="sm:col-span-2">
              <Button type="submit" disabled={busy}>
                {busy ? "Creando…" : "Crear borrador"}
              </Button>
            </div>
          </form>
        </Card>
      ) : null}

      <Card>
        <CardTitle>{canManage ? "Planes" : "Mis planes"}</CardTitle>
        {loading ? (
          <p className="mt-4 text-sm text-[var(--muted)]">Cargando…</p>
        ) : plans.length === 0 ? (
          <p className="mt-4 text-sm text-[var(--muted)]">
            {canManage
              ? "Aún no hay planes. Crea el primero arriba."
              : "Todavía no tienes planes asignados."}
          </p>
        ) : (
          <ul className="mt-4 space-y-2">
            {plans.map((plan) => {
              const status = planStatus(plan.status);
              return (
                <li
                  key={plan.id}
                  className="flex flex-wrap items-center justify-between gap-2 rounded-md border border-[var(--border)] px-3 py-3"
                >
                  <div className="min-w-0">
                    <p className="font-medium">
                      {plan.name}{" "}
                      <Badge tone={planStatusTone(status)}>
                        {planStatusLabel(status)}
                      </Badge>{" "}
                      <Badge>v{plan.version}</Badge>
                    </p>
                    <p className="mt-1 text-xs text-[var(--muted)]">
                      {plan.goalLabel ? `Objetivo: ${plan.goalLabel} · ` : ""}
                      {plan.dayCount} {plan.dayCount === 1 ? "día" : "días"}
                    </p>
                  </div>
                  <Link
                    href={`/t/${slug}/workouts/${plan.id}`}
                    className="inline-flex h-9 items-center justify-center rounded-md border border-[var(--border)] bg-[var(--surface-elevated)] px-3 text-xs font-semibold hover:border-[var(--accent)]"
                  >
                    {canManage ? "Abrir" : "Ver plan"}
                  </Link>
                </li>
              );
            })}
          </ul>
        )}
      </Card>

      {membership && !loading ? (
        <WorkoutSessions slug={slug} canManage={canManage} plans={plans} />
      ) : null}
    </div>
  );
}
