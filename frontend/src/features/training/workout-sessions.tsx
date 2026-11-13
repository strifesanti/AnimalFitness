"use client";

import { useSearchParams } from "next/navigation";
import { useCallback, useEffect, useMemo, useState } from "react";
import { Button } from "@/components/ui/button";
import { Card, CardDescription, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Badge, Notice } from "@/components/ui/notice";
import { errorMessage, fullName, selectClassName } from "@/lib/admin";
import {
  describePrescription,
  formatDateTime,
  intOrNull,
  numberOrNull,
  planStatus,
  sessionStatus,
  sessionStatusLabel,
  sessionStatusTone,
  textOrNull,
} from "@/lib/training";
import { adminApi, trainingApi } from "@/services/api";
import type { AthleteProfile } from "@/types/admin";
import type {
  Exercise,
  ExerciseLog,
  WorkoutExercise,
  WorkoutPlanDetail,
  WorkoutPlanSummary,
  WorkoutSession,
} from "@/types/training";

type Props = {
  slug: string;
  canManage: boolean;
  plans: WorkoutPlanSummary[];
};

export function WorkoutSessions({ slug, canManage, plans }: Props) {
  const searchParams = useSearchParams();
  const paramId = searchParams.get("session");
  const [picked, setPicked] = useState<string | null>(null);
  const selectedId = picked ?? paramId;

  const [sessions, setSessions] = useState<WorkoutSession[]>([]);
  const [athletes, setAthletes] = useState<AthleteProfile[]>([]);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  // Formulario de inicio
  const [athleteId, setAthleteId] = useState("");
  const [planId, setPlanId] = useState("");
  const [dayId, setDayId] = useState("");
  const [planDetail, setPlanDetail] = useState<WorkoutPlanDetail | null>(null);
  const [notes, setNotes] = useState("");

  const startablePlans = useMemo(
    () => plans.filter((plan) => planStatus(plan.status) === "Published"),
    [plans],
  );

  const load = useCallback(async () => {
    try {
      setSessions(await trainingApi.listSessions(slug));
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

  useEffect(() => {
    if (!canManage) {
      return;
    }
    let active = true;
    adminApi
      .listAthletes(slug)
      .then((list) => {
        if (active) {
          setAthletes(list);
        }
      })
      .catch((err) => {
        if (active) {
          setError(errorMessage(err));
        }
      });
    return () => {
      active = false;
    };
  }, [slug, canManage]);

  async function onPlanChange(id: string) {
    setPlanId(id);
    setDayId("");
    setPlanDetail(null);
    if (!id) {
      return;
    }
    try {
      setPlanDetail(await trainingApi.getPlan(slug, id));
    } catch (err) {
      setError(errorMessage(err));
    }
  }

  async function start(event: React.FormEvent) {
    event.preventDefault();
    setBusy(true);
    setError(null);
    setSuccess(null);
    try {
      const session = await trainingApi.startSession(slug, {
        athleteProfileId: canManage ? athleteId : null,
        planId: planId || null,
        dayId: dayId || null,
        notes: textOrNull(notes),
      });
      setSuccess("Sesión iniciada.");
      setNotes("");
      setPicked(session.id);
      await load();
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setBusy(false);
    }
  }

  const planName = (id: string | null) =>
    id ? (plans.find((plan) => plan.id === id)?.name ?? "Plan") : "Sesión libre";
  const athleteName = (id: string) => {
    const found = athletes.find((item) => item.id === id);
    return found ? fullName(found) : "Alumno";
  };
  const selected = sessions.find((item) => item.id === selectedId) ?? null;

  return (
    <div className="space-y-6">
      <h2 className="font-[family-name:var(--font-display)] text-xl tracking-wide">
        Sesiones de entrenamiento
      </h2>

      {error ? <Notice tone="error">{error}</Notice> : null}
      {success ? <Notice tone="success">{success}</Notice> : null}

      <Card>
        <CardTitle>Iniciar sesión</CardTitle>
        <CardDescription>
          {canManage
            ? "Inicia una sesión en nombre de un alumno."
            : "Entrena un día de tu plan o registra una sesión libre."}
        </CardDescription>
        <form
          onSubmit={start}
          className="mt-4 grid gap-4 sm:grid-cols-2 lg:grid-cols-4"
        >
          {canManage ? (
            <div className="space-y-2">
              <Label htmlFor="s-athlete">Alumno</Label>
              <select
                id="s-athlete"
                required
                className={selectClassName}
                value={athleteId}
                onChange={(e) => setAthleteId(e.target.value)}
              >
                <option value="">Selecciona un alumno…</option>
                {athletes.map((athlete) => (
                  <option key={athlete.id} value={athlete.id}>
                    {fullName(athlete)}
                  </option>
                ))}
              </select>
            </div>
          ) : null}
          <div className="space-y-2">
            <Label htmlFor="s-plan">Plan</Label>
            <select
              id="s-plan"
              className={selectClassName}
              value={planId}
              onChange={(e) => void onPlanChange(e.target.value)}
            >
              <option value="">Sin plan (sesión libre)</option>
              {startablePlans.map((plan) => (
                <option key={plan.id} value={plan.id}>
                  {plan.name} (v{plan.version})
                </option>
              ))}
            </select>
          </div>
          <div className="space-y-2">
            <Label htmlFor="s-day">Día</Label>
            <select
              id="s-day"
              className={selectClassName}
              value={dayId}
              disabled={!planDetail}
              onChange={(e) => setDayId(e.target.value)}
            >
              <option value="">Todo el plan</option>
              {planDetail?.days.map((day) => (
                <option key={day.id} value={day.id}>
                  Día {day.dayIndex}: {day.name}
                </option>
              ))}
            </select>
          </div>
          <div className="space-y-2">
            <Label htmlFor="s-notes">Notas</Label>
            <Input
              id="s-notes"
              value={notes}
              onChange={(e) => setNotes(e.target.value)}
            />
          </div>
          <div className="sm:col-span-2 lg:col-span-4">
            <Button type="submit" disabled={busy || (canManage && !athleteId)}>
              {busy ? "Iniciando…" : "Iniciar sesión"}
            </Button>
          </div>
        </form>
      </Card>

      {selected ? (
        <SessionRunner
          key={selected.id}
          slug={slug}
          session={selected}
          planName={planName(selected.planId)}
          athleteLabel={canManage ? athleteName(selected.athleteProfileId) : null}
          onChanged={load}
          onClose={() => setPicked("")}
        />
      ) : null}

      <Card>
        <CardTitle>{canManage ? "Sesiones recientes" : "Mis sesiones"}</CardTitle>
        {loading ? (
          <p className="mt-4 text-sm text-[var(--muted)]">Cargando…</p>
        ) : sessions.length === 0 ? (
          <p className="mt-4 text-sm text-[var(--muted)]">
            Aún no hay sesiones.
          </p>
        ) : (
          <ul className="mt-4 space-y-2">
            {sessions.map((session) => {
              const status = sessionStatus(session.status);
              return (
                <li
                  key={session.id}
                  className="flex flex-wrap items-center justify-between gap-2 rounded-md border border-[var(--border)] px-3 py-2"
                >
                  <div className="min-w-0">
                    <p className="text-sm font-medium">
                      {planName(session.planId)}{" "}
                      <Badge tone={sessionStatusTone(status)}>
                        {sessionStatusLabel(status)}
                      </Badge>
                    </p>
                    <p className="text-xs text-[var(--muted)]">
                      {canManage ? `${athleteName(session.athleteProfileId)} · ` : ""}
                      Inicio: {formatDateTime(session.startedAt)}
                      {session.completedAt
                        ? ` · Fin: ${formatDateTime(session.completedAt)}`
                        : ""}
                    </p>
                  </div>
                  <Button
                    size="sm"
                    variant="secondary"
                    onClick={() =>
                      setPicked(selectedId === session.id ? "" : session.id)
                    }
                  >
                    {selectedId === session.id
                      ? "Ocultar"
                      : status === "InProgress"
                        ? "Continuar"
                        : "Ver detalle"}
                  </Button>
                </li>
              );
            })}
          </ul>
        )}
      </Card>
    </div>
  );
}

// ---------------------------------------------------------------------------

type SetForm = {
  target: string; // id del ejercicio del plan, o id del ejercicio de la biblioteca
  setNumber: string;
  reps: string;
  weightKg: string;
  durationSeconds: string;
  distanceMeters: string;
  rpe: string;
  notes: string;
};

const emptySetForm: SetForm = {
  target: "",
  setNumber: "1",
  reps: "",
  weightKg: "",
  durationSeconds: "",
  distanceMeters: "",
  rpe: "",
  notes: "",
};

function SessionRunner({
  slug,
  session,
  planName,
  athleteLabel,
  onChanged,
  onClose,
}: {
  slug: string;
  session: WorkoutSession;
  planName: string;
  athleteLabel: string | null;
  onChanged: () => Promise<void>;
  onClose: () => void;
}) {
  const status = sessionStatus(session.status);
  const active = status === "InProgress";

  const [logs, setLogs] = useState<ExerciseLog[]>([]);
  const [library, setLibrary] = useState<Exercise[]>([]);
  const [plan, setPlan] = useState<WorkoutPlanDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [form, setForm] = useState<SetForm>(emptySetForm);
  const [finishNotes, setFinishNotes] = useState("");

  const loadLogs = useCallback(async () => {
    setLogs(await trainingApi.listSessionLogs(slug, session.id));
  }, [slug, session.id]);

  useEffect(() => {
    let alive = true;
    (async () => {
      try {
        const [logList, exerciseList, planDetail] = await Promise.all([
          trainingApi.listSessionLogs(slug, session.id),
          trainingApi.listExercises(slug),
          session.planId
            ? trainingApi.getPlan(slug, session.planId)
            : Promise.resolve(null),
        ]);
        if (!alive) {
          return;
        }
        setLogs(logList);
        setLibrary(exerciseList);
        setPlan(planDetail);
      } catch (err) {
        if (alive) {
          setError(errorMessage(err));
        }
      } finally {
        if (alive) {
          setLoading(false);
        }
      }
    })();
    return () => {
      alive = false;
    };
  }, [slug, session.id, session.planId]);

  // Ejercicios prescritos que aplican a esta sesión (día elegido o todo el plan).
  const prescribed: WorkoutExercise[] = useMemo(() => {
    if (!plan) {
      return [];
    }
    return plan.days
      .filter((day) => !session.dayId || day.id === session.dayId)
      .flatMap((day) => day.exercises);
  }, [plan, session.dayId]);

  const exerciseName = useCallback(
    (exerciseId: string) =>
      prescribed.find((item) => item.exerciseId === exerciseId)?.exerciseName ??
      library.find((item) => item.id === exerciseId)?.name ??
      "Ejercicio",
    [prescribed, library],
  );

  const usingPlan = prescribed.length > 0;

  function resolveTarget(target: string) {
    if (usingPlan) {
      const item = prescribed.find((entry) => entry.id === target);
      return item
        ? { exerciseId: item.exerciseId, workoutExerciseId: item.id, item }
        : null;
    }
    return target
      ? { exerciseId: target, workoutExerciseId: null, item: null }
      : null;
  }

  function onTargetChange(target: string) {
    const resolved = resolveTarget(target);
    const nextSet = resolved
      ? logs.filter((log) => log.exerciseId === resolved.exerciseId).length + 1
      : 1;
    const item = resolved?.item;
    setForm({
      ...emptySetForm,
      target,
      setNumber: String(nextSet),
      weightKg: item?.targetWeightKg?.toString() ?? "",
      durationSeconds: item?.targetDurationSeconds?.toString() ?? "",
      distanceMeters: item?.targetDistanceMeters?.toString() ?? "",
    });
  }

  async function submitSet(event: React.FormEvent) {
    event.preventDefault();
    const resolved = resolveTarget(form.target);
    if (!resolved) {
      return;
    }
    setBusy(true);
    setError(null);
    setSuccess(null);
    try {
      await trainingApi.logSet(slug, session.id, {
        exerciseId: resolved.exerciseId,
        workoutExerciseId: resolved.workoutExerciseId,
        setNumber: intOrNull(form.setNumber) ?? 1,
        reps: intOrNull(form.reps),
        weightKg: numberOrNull(form.weightKg),
        durationSeconds: intOrNull(form.durationSeconds),
        distanceMeters: numberOrNull(form.distanceMeters),
        rpe: intOrNull(form.rpe),
        notes: textOrNull(form.notes),
      });
      setSuccess("Serie registrada.");
      await loadLogs();
      setForm({
        ...form,
        setNumber: String((intOrNull(form.setNumber) ?? 1) + 1),
        reps: "",
        rpe: "",
        notes: "",
      });
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setBusy(false);
    }
  }

  async function finish(kind: "complete" | "cancel") {
    if (kind === "cancel" && !window.confirm("¿Cancelar esta sesión?")) {
      return;
    }
    setBusy(true);
    setError(null);
    setSuccess(null);
    try {
      const message =
        kind === "complete"
          ? await trainingApi.completeSession(
              slug,
              session.id,
              textOrNull(finishNotes),
            )
          : await trainingApi.cancelSession(slug, session.id);
      setSuccess(
        message ||
          (kind === "complete" ? "Sesión completada." : "Sesión cancelada."),
      );
      await onChanged();
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setBusy(false);
    }
  }

  const loggedCount = (item: WorkoutExercise) =>
    logs.filter((log) => log.workoutExerciseId === item.id).length;

  return (
    <Card className="space-y-5 border-[var(--accent)]/40">
      <div className="flex flex-wrap items-start justify-between gap-2">
        <div className="min-w-0">
          <CardTitle>
            {planName}{" "}
            <Badge tone={sessionStatusTone(status)}>
              {sessionStatusLabel(status)}
            </Badge>
          </CardTitle>
          <CardDescription>
            {athleteLabel ? `${athleteLabel} · ` : ""}
            Inicio: {formatDateTime(session.startedAt)}
            {session.notes ? ` · ${session.notes}` : ""}
          </CardDescription>
        </div>
        <Button size="sm" variant="ghost" onClick={onClose}>
          Cerrar
        </Button>
      </div>

      {error ? <Notice tone="error">{error}</Notice> : null}
      {success ? <Notice tone="success">{success}</Notice> : null}

      {loading ? (
        <p className="text-sm text-[var(--muted)]">Cargando…</p>
      ) : (
        <>
          {usingPlan ? (
            <div>
              <p className="text-sm font-medium">Ejercicios del plan</p>
              <ul className="mt-2 space-y-1">
                {prescribed.map((item) => (
                  <li
                    key={item.id}
                    className="flex flex-wrap items-center justify-between gap-2 rounded-md border border-[var(--border)] px-3 py-2 text-sm"
                  >
                    <span>
                      {item.exerciseName}
                      <span className="ml-2 text-xs text-[var(--muted)]">
                        {describePrescription(item)}
                      </span>
                    </span>
                    <Badge
                      tone={loggedCount(item) >= item.targetSets ? "good" : "neutral"}
                    >
                      {loggedCount(item)}/{item.targetSets} series
                    </Badge>
                  </li>
                ))}
              </ul>
            </div>
          ) : null}

          {active ? (
            <form
              onSubmit={submitSet}
              className="grid gap-4 border-t border-[var(--border)] pt-4 sm:grid-cols-2 lg:grid-cols-4"
            >
              <div className="space-y-2 sm:col-span-2 lg:col-span-3">
                <Label htmlFor="l-target">Ejercicio</Label>
                <select
                  id="l-target"
                  required
                  className={selectClassName}
                  value={form.target}
                  onChange={(e) => onTargetChange(e.target.value)}
                >
                  <option value="">Selecciona un ejercicio…</option>
                  {usingPlan
                    ? prescribed.map((item) => (
                        <option key={item.id} value={item.id}>
                          {item.exerciseName}
                        </option>
                      ))
                    : library.map((item) => (
                        <option key={item.id} value={item.id}>
                          {item.name}
                        </option>
                      ))}
                </select>
              </div>
              <div className="space-y-2">
                <Label htmlFor="l-set">Serie nº</Label>
                <Input
                  id="l-set"
                  inputMode="numeric"
                  required
                  value={form.setNumber}
                  onChange={(e) => setForm({ ...form, setNumber: e.target.value })}
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="l-reps">Repeticiones</Label>
                <Input
                  id="l-reps"
                  inputMode="numeric"
                  value={form.reps}
                  onChange={(e) => setForm({ ...form, reps: e.target.value })}
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="l-weight">Peso (kg)</Label>
                <Input
                  id="l-weight"
                  inputMode="decimal"
                  value={form.weightKg}
                  onChange={(e) => setForm({ ...form, weightKg: e.target.value })}
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="l-dur">Duración (s)</Label>
                <Input
                  id="l-dur"
                  inputMode="numeric"
                  value={form.durationSeconds}
                  onChange={(e) =>
                    setForm({ ...form, durationSeconds: e.target.value })
                  }
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="l-dist">Distancia (m)</Label>
                <Input
                  id="l-dist"
                  inputMode="decimal"
                  value={form.distanceMeters}
                  onChange={(e) =>
                    setForm({ ...form, distanceMeters: e.target.value })
                  }
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="l-rpe">RPE (1-10)</Label>
                <Input
                  id="l-rpe"
                  inputMode="numeric"
                  value={form.rpe}
                  onChange={(e) => setForm({ ...form, rpe: e.target.value })}
                />
              </div>
              <div className="space-y-2 sm:col-span-2">
                <Label htmlFor="l-notes">Notas</Label>
                <Input
                  id="l-notes"
                  value={form.notes}
                  onChange={(e) => setForm({ ...form, notes: e.target.value })}
                />
              </div>
              <div className="sm:col-span-2 lg:col-span-4">
                <Button type="submit" disabled={busy || !form.target}>
                  Registrar serie
                </Button>
              </div>
            </form>
          ) : null}

          <div className="border-t border-[var(--border)] pt-4">
            <p className="text-sm font-medium">Series registradas</p>
            {logs.length === 0 ? (
              <p className="mt-2 text-sm text-[var(--muted)]">
                Aún no hay series registradas.
              </p>
            ) : (
              <ul className="mt-2 space-y-1">
                {logs.map((log) => (
                  <li
                    key={log.id}
                    className="rounded-md border border-[var(--border)] px-3 py-2 text-sm"
                  >
                    {exerciseName(log.exerciseId)} · serie {log.setNumber}
                    <span className="ml-2 text-xs text-[var(--muted)]">
                      {[
                        log.reps !== null ? `${log.reps} reps` : null,
                        log.weightKg !== null ? `${log.weightKg} kg` : null,
                        log.durationSeconds !== null
                          ? `${log.durationSeconds} s`
                          : null,
                        log.distanceMeters !== null
                          ? `${log.distanceMeters} m`
                          : null,
                        log.rpe !== null ? `RPE ${log.rpe}` : null,
                        log.notes,
                      ]
                        .filter(Boolean)
                        .join(" · ")}
                    </span>
                  </li>
                ))}
              </ul>
            )}
          </div>

          {active ? (
            <div className="flex flex-wrap items-end gap-3 border-t border-[var(--border)] pt-4">
              <div className="min-w-56 flex-1 space-y-2">
                <Label htmlFor="f-notes">Notas finales</Label>
                <Input
                  id="f-notes"
                  value={finishNotes}
                  onChange={(e) => setFinishNotes(e.target.value)}
                />
              </div>
              <Button disabled={busy} onClick={() => void finish("complete")}>
                Completar sesión
              </Button>
              <Button
                variant="danger"
                disabled={busy}
                onClick={() => void finish("cancel")}
              >
                Cancelar sesión
              </Button>
            </div>
          ) : null}
        </>
      )}
    </Card>
  );
}
