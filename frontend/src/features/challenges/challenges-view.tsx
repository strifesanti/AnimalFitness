"use client";

import { useParams } from "next/navigation";
import { useCallback, useEffect, useState } from "react";
import { Button } from "@/components/ui/button";
import { Card, CardDescription, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Badge, Notice } from "@/components/ui/notice";
import { useAthleteScope } from "@/features/advanced/use-athlete-scope";
import { errorMessage, formatDate, selectClassName } from "@/lib/admin";
import {
  CHALLENGE_KINDS,
  CHALLENGE_METRICS,
  challengeKindLabel,
  challengeMetricLabel,
  challengeStatus,
  challengeStatusLabel,
  challengeStatusTone,
  dateInputOffset,
} from "@/lib/advanced";
import { dateInputToIso } from "@/lib/progress";
import { intOrNull, textOrNull } from "@/lib/training";
import { challengesApi } from "@/services/api";
import type {
  Achievement,
  Challenge,
  ChallengeKind,
  ChallengeMetric,
  LeaderboardEntry,
} from "@/types/advanced";

const emptyForm = () => ({
  name: "",
  description: "",
  kind: "Individual" as ChallengeKind,
  metric: "ManualPoints" as ChallengeMetric,
  startsAt: dateInputOffset(0),
  endsAt: dateInputOffset(30),
  targetValue: "",
});

export function ChallengesView() {
  const { slug } = useParams<{ slug: string }>();
  const scope = useAthleteScope(slug, { requireAthlete: false });
  const isStaff = scope.isStaff;

  const [challenges, setChallenges] = useState<Challenge[]>([]);
  const [achievements, setAchievements] = useState<Achievement[]>([]);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [form, setForm] = useState(emptyForm);

  const [openId, setOpenId] = useState<string | null>(null);
  const [board, setBoard] = useState<LeaderboardEntry[]>([]);
  const [points, setPoints] = useState({ athleteId: "", points: "", note: "" });

  const load = useCallback(async () => {
    try {
      const [list, awards] = await Promise.all([
        challengesApi.list(slug),
        challengesApi.listAchievements(slug),
      ]);
      setChallenges(list);
      setAchievements(awards);
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

  const loadBoard = useCallback(
    async (id: string) => {
      try {
        setBoard(await challengesApi.leaderboard(slug, id));
      } catch (err) {
        setError(errorMessage(err));
      }
    },
    [slug],
  );

  async function run(work: () => Promise<string | void>, fallback: string) {
    setBusy(true);
    setError(null);
    setSuccess(null);
    try {
      const message = await work();
      setSuccess(message || fallback);
      await load();
      if (openId) {
        await loadBoard(openId);
      }
      return true;
    } catch (err) {
      setError(errorMessage(err));
      return false;
    } finally {
      setBusy(false);
    }
  }

  async function create(event: React.FormEvent) {
    event.preventDefault();
    const startsAt = dateInputToIso(form.startsAt);
    const endsAt = dateInputToIso(form.endsAt);
    if (!startsAt || !endsAt) {
      setError("Indica las fechas de inicio y fin.");
      return;
    }
    const done = await run(async () => {
      await challengesApi.create(slug, {
        name: form.name.trim(),
        description: textOrNull(form.description),
        kind: form.kind,
        metric: form.metric,
        startsAt,
        endsAt,
        targetValue: intOrNull(form.targetValue),
      });
    }, "Reto creado como borrador. Actívalo para que los alumnos puedan unirse.");
    if (done) {
      setForm(emptyForm());
    }
  }

  async function toggleBoard(id: string) {
    if (openId === id) {
      setOpenId(null);
      return;
    }
    setOpenId(id);
    setPoints({ athleteId: "", points: "", note: "" });
    await loadBoard(id);
  }

  async function addPoints(event: React.FormEvent) {
    event.preventDefault();
    if (!openId || !points.athleteId) {
      setError("Elige a quién sumar los puntos.");
      return;
    }
    const amount = intOrNull(points.points);
    if (!amount || amount < 1) {
      setError("Los puntos deben ser un número positivo.");
      return;
    }
    const done = await run(async () => {
      await challengesApi.addActivity(slug, openId, {
        athleteProfileId: points.athleteId,
        points: amount,
        note: textOrNull(points.note),
      });
    }, "Puntos registrados.");
    if (done) {
      setPoints({ ...points, points: "", note: "" });
    }
  }

  if (!scope.ready) {
    return <p className="text-sm text-[var(--muted)]">Cargando…</p>;
  }

  return (
    <div className="space-y-6">
      <p className="text-sm text-[var(--muted)]">
        {isStaff
          ? "Crea retos, actívalos y reparte puntos para motivar a tus alumnos."
          : "Únete a los retos activos y sigue la clasificación."}
      </p>

      {error ?? scope.error ? (
        <Notice tone="error">{error ?? scope.error}</Notice>
      ) : null}
      {success ? <Notice tone="success">{success}</Notice> : null}

      {isStaff ? (
        <Card>
          <CardTitle>Nuevo reto</CardTitle>
          <CardDescription>
            Se crea como borrador; actívalo cuando quieras abrir las inscripciones.
          </CardDescription>
          <form onSubmit={create} className="mt-4 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
            <div className="space-y-2 lg:col-span-2">
              <Label htmlFor="c-name">Nombre</Label>
              <Input
                id="c-name"
                required
                value={form.name}
                onChange={(e) => setForm({ ...form, name: e.target.value })}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="c-target">Meta (opcional)</Label>
              <Input
                id="c-target"
                inputMode="numeric"
                placeholder="Ej. 12 entrenamientos"
                value={form.targetValue}
                onChange={(e) => setForm({ ...form, targetValue: e.target.value })}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="c-kind">Tipo</Label>
              <select
                id="c-kind"
                className={selectClassName}
                value={form.kind}
                onChange={(e) => setForm({ ...form, kind: e.target.value as ChallengeKind })}
              >
                {CHALLENGE_KINDS.map((kind) => (
                  <option key={kind} value={kind}>
                    {challengeKindLabel(kind)}
                  </option>
                ))}
              </select>
            </div>
            <div className="space-y-2">
              <Label htmlFor="c-metric">Métrica</Label>
              <select
                id="c-metric"
                className={selectClassName}
                value={form.metric}
                onChange={(e) =>
                  setForm({ ...form, metric: e.target.value as ChallengeMetric })
                }
              >
                {CHALLENGE_METRICS.map((metric) => (
                  <option key={metric} value={metric}>
                    {challengeMetricLabel(metric)}
                  </option>
                ))}
              </select>
            </div>
            <div className="space-y-2">
              <Label htmlFor="c-start">Inicio</Label>
              <Input
                id="c-start"
                type="date"
                required
                value={form.startsAt}
                onChange={(e) => setForm({ ...form, startsAt: e.target.value })}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="c-end">Fin</Label>
              <Input
                id="c-end"
                type="date"
                required
                value={form.endsAt}
                onChange={(e) => setForm({ ...form, endsAt: e.target.value })}
              />
            </div>
            <div className="space-y-2 sm:col-span-2">
              <Label htmlFor="c-desc">Descripción</Label>
              <Input
                id="c-desc"
                value={form.description}
                onChange={(e) => setForm({ ...form, description: e.target.value })}
              />
            </div>
            <div className="flex items-end">
              <Button type="submit" disabled={busy}>
                {busy ? "Guardando…" : "Crear reto"}
              </Button>
            </div>
          </form>
        </Card>
      ) : null}

      <Card>
        <CardTitle>Retos</CardTitle>
        {loading ? (
          <p className="mt-3 text-sm text-[var(--muted)]">Cargando…</p>
        ) : challenges.length === 0 ? (
          <p className="mt-3 text-sm text-[var(--muted)]">Aún no hay retos.</p>
        ) : (
          <ul className="mt-3 space-y-3">
            {challenges.map((challenge) => {
              const status = challengeStatus(challenge.status);
              const open = openId === challenge.id;
              return (
                <li
                  key={challenge.id}
                  className="rounded-md border border-[var(--border)] px-3 py-3"
                >
                  <div className="flex flex-wrap items-start justify-between gap-3">
                    <div className="min-w-0">
                      <p className="text-sm font-medium">
                        {challenge.name}{" "}
                        <Badge tone={challengeStatusTone(status)}>
                          {challengeStatusLabel(status)}
                        </Badge>{" "}
                        {challenge.joined ? <Badge tone="good">Participas</Badge> : null}
                      </p>
                      <p className="text-xs text-[var(--muted)]">
                        {challengeKindLabel(challenge.kind)} ·{" "}
                        {challengeMetricLabel(challenge.metric)} ·{" "}
                        {formatDate(challenge.startsAt)} → {formatDate(challenge.endsAt)} ·{" "}
                        {challenge.participantCount} participantes
                        {challenge.targetValue !== null ? ` · Meta: ${challenge.targetValue}` : ""}
                      </p>
                      {challenge.description ? (
                        <p className="mt-1 text-xs text-[var(--muted)]">
                          {challenge.description}
                        </p>
                      ) : null}
                    </div>
                    <div className="flex flex-wrap gap-2">
                      {!isStaff && status === "Active" && !challenge.joined ? (
                        <Button
                          size="sm"
                          disabled={busy}
                          onClick={() =>
                            run(
                              () => challengesApi.join(slug, challenge.id),
                              "Te uniste al reto.",
                            )
                          }
                        >
                          Unirme
                        </Button>
                      ) : null}
                      {isStaff && status === "Draft" ? (
                        <Button
                          size="sm"
                          disabled={busy}
                          onClick={() =>
                            run(
                              () => challengesApi.activate(slug, challenge.id),
                              "Reto activado.",
                            )
                          }
                        >
                          Activar
                        </Button>
                      ) : null}
                      {isStaff && status === "Active" ? (
                        <Button
                          size="sm"
                          disabled={busy}
                          onClick={() =>
                            run(
                              () => challengesApi.complete(slug, challenge.id),
                              "Reto finalizado.",
                            )
                          }
                        >
                          Finalizar
                        </Button>
                      ) : null}
                      {isStaff && (status === "Draft" || status === "Active") ? (
                        <Button
                          size="sm"
                          variant="secondary"
                          disabled={busy}
                          onClick={() =>
                            run(
                              () => challengesApi.cancel(slug, challenge.id),
                              "Reto cancelado.",
                            )
                          }
                        >
                          Cancelar
                        </Button>
                      ) : null}
                      {status !== "Draft" || isStaff ? (
                        <Button
                          size="sm"
                          variant="ghost"
                          onClick={() => toggleBoard(challenge.id)}
                        >
                          {open ? "Ocultar clasificación" : "Clasificación"}
                        </Button>
                      ) : null}
                    </div>
                  </div>

                  {open ? (
                    <div className="mt-4 space-y-4 border-t border-[var(--border)] pt-4">
                      {board.length === 0 ? (
                        <p className="text-sm text-[var(--muted)]">
                          Todavía no hay participantes.
                        </p>
                      ) : (
                        <ol className="space-y-1">
                          {board.map((entry) => (
                            <li
                              key={entry.participantId}
                              className="flex items-center justify-between rounded-md bg-white/5 px-3 py-1.5 text-sm"
                            >
                              <span>
                                <span className="mr-3 inline-block w-6 text-[var(--accent)]">
                                  {entry.rank}.
                                </span>
                                {entry.athleteName || "Alumno"}
                              </span>
                              <span className="font-semibold">{entry.points} pts</span>
                            </li>
                          ))}
                        </ol>
                      )}

                      {isStaff && status === "Active" && board.length > 0 ? (
                        <form
                          onSubmit={addPoints}
                          className="grid gap-3 sm:grid-cols-4"
                        >
                          <div className="space-y-2">
                            <Label htmlFor="p-athlete">Alumno</Label>
                            <select
                              id="p-athlete"
                              className={selectClassName}
                              value={points.athleteId}
                              onChange={(e) =>
                                setPoints({ ...points, athleteId: e.target.value })
                              }
                            >
                              <option value="">Selecciona…</option>
                              {board.map((entry) => (
                                <option
                                  key={entry.athleteProfileId}
                                  value={entry.athleteProfileId}
                                >
                                  {entry.athleteName || "Alumno"}
                                </option>
                              ))}
                            </select>
                          </div>
                          <div className="space-y-2">
                            <Label htmlFor="p-points">Puntos</Label>
                            <Input
                              id="p-points"
                              inputMode="numeric"
                              value={points.points}
                              onChange={(e) =>
                                setPoints({ ...points, points: e.target.value })
                              }
                            />
                          </div>
                          <div className="space-y-2">
                            <Label htmlFor="p-note">Nota</Label>
                            <Input
                              id="p-note"
                              value={points.note}
                              onChange={(e) =>
                                setPoints({ ...points, note: e.target.value })
                              }
                            />
                          </div>
                          <div className="flex items-end">
                            <Button type="submit" disabled={busy}>
                              Sumar puntos
                            </Button>
                          </div>
                        </form>
                      ) : null}
                    </div>
                  ) : null}
                </li>
              );
            })}
          </ul>
        )}
      </Card>

      <Card>
        <CardTitle>Logros</CardTitle>
        {achievements.length === 0 ? (
          <p className="mt-3 text-sm text-[var(--muted)]">Aún no hay logros.</p>
        ) : (
          <ul className="mt-3 space-y-2">
            {achievements.map((item) => (
              <li
                key={item.id}
                className="flex flex-wrap items-center justify-between gap-2 rounded-md border border-[var(--border)] px-3 py-2"
              >
                <div>
                  <p className="text-sm font-medium">🏆 {item.title}</p>
                  <p className="text-xs text-[var(--muted)]">
                    {isStaff && item.athleteName ? `${item.athleteName} · ` : ""}
                    {formatDate(item.awardedAt)}
                    {item.description ? ` · ${item.description}` : ""}
                  </p>
                </div>
              </li>
            ))}
          </ul>
        )}
      </Card>
    </div>
  );
}
