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
import {
  emptyPrescription,
  PrescriptionInputs,
  prescriptionFromExercise,
  prescriptionToInput,
  type PrescriptionForm,
} from "@/features/training/prescription-fields";
import {
  errorMessage,
  fullName,
  normalizeRole,
  selectClassName,
  textareaClassName,
} from "@/lib/admin";
import {
  describePrescription,
  formatDateTime,
  intOrNull,
  planStatus,
  planStatusLabel,
  planStatusTone,
  textOrNull,
} from "@/lib/training";
import { adminApi, trainingApi } from "@/services/api";
import type { AthleteProfile } from "@/types/admin";
import type {
  Exercise,
  WorkoutAssignment,
  WorkoutDay,
  WorkoutPlanDetail,
} from "@/types/training";

export function PlanDetail() {
  const { slug, planId } = useParams<{ slug: string; planId: string }>();
  const router = useRouter();
  const { user } = useAuth();
  const membership = user?.memberships.find((item) => item.slug === slug);
  const role = membership ? normalizeRole(membership.role) : null;
  const canManage = role === "Owner" || role === "Trainer";
  const isAthlete = role === "Athlete";

  const [plan, setPlan] = useState<WorkoutPlanDetail | null>(null);
  const [exercises, setExercises] = useState<Exercise[]>([]);
  const [athletes, setAthletes] = useState<AthleteProfile[]>([]);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  // Metadatos del plan
  const [meta, setMeta] = useState({ name: "", goalLabel: "", description: "" });

  // Días
  const [newDay, setNewDay] = useState({ name: "", notes: "" });
  const [editingDayId, setEditingDayId] = useState<string | null>(null);
  const [dayForm, setDayForm] = useState({ name: "", notes: "" });

  // Ejercicios prescritos
  const [addingToDayId, setAddingToDayId] = useState<string | null>(null);
  const [addExerciseId, setAddExerciseId] = useState("");
  const [addForm, setAddForm] = useState<PrescriptionForm>(emptyPrescription);
  const [editingExId, setEditingExId] = useState<string | null>(null);
  const [exForm, setExForm] = useState<PrescriptionForm>(emptyPrescription);

  // Asignación
  const [athleteId, setAthleteId] = useState("");
  const [assignments, setAssignments] = useState<WorkoutAssignment[]>([]);

  const load = useCallback(async () => {
    try {
      const detail = await trainingApi.getPlan(slug, planId);
      setPlan(detail);
      setMeta({
        name: detail.name,
        goalLabel: detail.goalLabel ?? "",
        description: detail.description ?? "",
      });
      setError(null);
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setLoading(false);
    }
  }, [slug, planId]);

  useEffect(() => {
    if (!membership) {
      return;
    }
    // eslint-disable-next-line react-hooks/set-state-in-effect
    void load();
  }, [load, membership]);

  useEffect(() => {
    if (!canManage) {
      return;
    }
    let active = true;
    (async () => {
      try {
        const [exerciseList, athleteList] = await Promise.all([
          trainingApi.listExercises(slug),
          adminApi.listAthletes(slug),
        ]);
        if (active) {
          setExercises(exerciseList);
          setAthletes(athleteList);
        }
      } catch (err) {
        if (active) {
          setError(errorMessage(err));
        }
      }
    })();
    return () => {
      active = false;
    };
  }, [slug, canManage]);

  async function run(task: () => Promise<string>, after?: () => void) {
    setBusy(true);
    setError(null);
    setSuccess(null);
    try {
      setSuccess(await task());
      after?.();
      await load();
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setBusy(false);
    }
  }

  async function startSession(day: WorkoutDay | null) {
    setBusy(true);
    setError(null);
    try {
      const session = await trainingApi.startSession(slug, {
        planId,
        dayId: day?.id ?? null,
      });
      router.push(`/t/${slug}/workouts?session=${session.id}`);
    } catch (err) {
      setError(errorMessage(err));
      setBusy(false);
    }
  }

  if (loading) {
    return <p className="text-sm text-[var(--muted)]">Cargando plan…</p>;
  }

  if (!plan) {
    return (
      <div className="space-y-4">
        {error ? <Notice tone="error">{error}</Notice> : null}
        <Link
          href={`/t/${slug}/workouts`}
          className="text-sm text-[var(--accent)] underline"
        >
          Volver a los planes
        </Link>
      </div>
    );
  }

  const status = planStatus(plan.status);
  const isDraft = status === "Draft";
  const editable = canManage && isDraft;
  const athleteName = (id: string) => {
    const found = athletes.find((item) => item.id === id);
    return found ? fullName(found) : "Alumno";
  };

  return (
    <div className="space-y-8">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div className="min-w-0">
          <Link
            href={`/t/${slug}/workouts`}
            className="text-xs text-[var(--accent)] underline"
          >
            ← Volver a los planes
          </Link>
          <h2 className="mt-2 font-[family-name:var(--font-display)] text-2xl tracking-wide">
            {plan.name}{" "}
            <Badge tone={planStatusTone(status)}>{planStatusLabel(status)}</Badge>{" "}
            <Badge>v{plan.version}</Badge>
          </h2>
          {plan.goalLabel ? (
            <p className="mt-1 text-sm text-[var(--muted)]">
              Objetivo: {plan.goalLabel}
            </p>
          ) : null}
          {plan.description ? (
            <p className="mt-1 text-sm text-[var(--muted)]">{plan.description}</p>
          ) : null}
        </div>
        {canManage ? (
          <div className="flex flex-wrap gap-2">
            {isDraft ? (
              <Button
                size="sm"
                disabled={busy || plan.days.length === 0}
                title={
                  plan.days.length === 0
                    ? "Agrega al menos un día para publicar"
                    : undefined
                }
                onClick={() =>
                  run(() => trainingApi.publishPlan(slug, plan.id))
                }
              >
                Publicar
              </Button>
            ) : null}
            {status !== "Archived" ? (
              <Button
                size="sm"
                variant="secondary"
                disabled={busy}
                onClick={() => {
                  if (window.confirm("¿Archivar este plan?")) {
                    void run(() => trainingApi.archivePlan(slug, plan.id));
                  }
                }}
              >
                Archivar
              </Button>
            ) : null}
            {!isDraft ? (
              <Button
                size="sm"
                variant="secondary"
                disabled={busy}
                onClick={async () => {
                  setBusy(true);
                  setError(null);
                  try {
                    const next = await trainingApi.newPlanVersion(slug, plan.id);
                    router.push(`/t/${slug}/workouts/${next.id}`);
                  } catch (err) {
                    setError(errorMessage(err));
                  } finally {
                    setBusy(false);
                  }
                }}
              >
                Nueva versión
              </Button>
            ) : null}
          </div>
        ) : null}
      </div>

      {error ? <Notice tone="error">{error}</Notice> : null}
      {success ? <Notice tone="success">{success}</Notice> : null}

      {!isDraft && canManage ? (
        <Notice tone="success">
          Este plan no está en borrador y no se puede editar. Crea una nueva
          versión para modificarlo.
        </Notice>
      ) : null}

      {editable ? (
        <Card>
          <CardTitle>Datos del plan</CardTitle>
          <form
            className="mt-4 grid gap-4 sm:grid-cols-2"
            onSubmit={(e) => {
              e.preventDefault();
              void run(async () => {
                await trainingApi.updatePlan(slug, plan.id, {
                  name: meta.name.trim(),
                  goalLabel: textOrNull(meta.goalLabel),
                  description: textOrNull(meta.description),
                });
                return "Plan actualizado.";
              });
            }}
          >
            <div className="space-y-2">
              <Label htmlFor="p-name">Nombre</Label>
              <Input
                id="p-name"
                required
                value={meta.name}
                onChange={(e) => setMeta({ ...meta, name: e.target.value })}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="p-goal">Objetivo</Label>
              <Input
                id="p-goal"
                value={meta.goalLabel}
                onChange={(e) => setMeta({ ...meta, goalLabel: e.target.value })}
              />
            </div>
            <div className="space-y-2 sm:col-span-2">
              <Label htmlFor="p-desc">Descripción</Label>
              <textarea
                id="p-desc"
                className={textareaClassName}
                value={meta.description}
                onChange={(e) =>
                  setMeta({ ...meta, description: e.target.value })
                }
              />
            </div>
            <div className="sm:col-span-2">
              <Button type="submit" disabled={busy}>
                Guardar datos
              </Button>
            </div>
          </form>
        </Card>
      ) : null}

      <div className="space-y-4">
        <h3 className="font-[family-name:var(--font-display)] text-lg tracking-wide">
          Días
        </h3>
        {plan.days.length === 0 ? (
          <p className="text-sm text-[var(--muted)]">
            Este plan aún no tiene días.
          </p>
        ) : null}

        {plan.days.map((day) => {
          const editingDay = editingDayId === day.id;
          const adding = addingToDayId === day.id;
          return (
            <Card key={day.id}>
              <div className="flex flex-wrap items-start justify-between gap-2">
                <div className="min-w-0">
                  <CardTitle>
                    Día {day.dayIndex}: {day.name}
                  </CardTitle>
                  {day.notes ? (
                    <CardDescription>{day.notes}</CardDescription>
                  ) : null}
                </div>
                <div className="flex flex-wrap gap-2">
                  {isAthlete && status === "Published" ? (
                    <Button
                      size="sm"
                      disabled={busy}
                      onClick={() => startSession(day)}
                    >
                      Iniciar este día
                    </Button>
                  ) : null}
                  {editable ? (
                    <Button
                      size="sm"
                      variant="secondary"
                      disabled={busy}
                      onClick={() => {
                        if (editingDay) {
                          setEditingDayId(null);
                        } else {
                          setEditingDayId(day.id);
                          setDayForm({ name: day.name, notes: day.notes ?? "" });
                        }
                      }}
                    >
                      {editingDay ? "Cancelar" : "Editar día"}
                    </Button>
                  ) : null}
                </div>
              </div>

              {editingDay ? (
                <form
                  className="mt-4 grid gap-4 sm:grid-cols-2"
                  onSubmit={(e) => {
                    e.preventDefault();
                    void run(
                      async () => {
                        await trainingApi.updateDay(slug, day.id, {
                          name: dayForm.name.trim(),
                          notes: textOrNull(dayForm.notes),
                        });
                        return "Día actualizado.";
                      },
                      () => setEditingDayId(null),
                    );
                  }}
                >
                  <div className="space-y-2">
                    <Label htmlFor={`dn-${day.id}`}>Nombre</Label>
                    <Input
                      id={`dn-${day.id}`}
                      required
                      value={dayForm.name}
                      onChange={(e) =>
                        setDayForm({ ...dayForm, name: e.target.value })
                      }
                    />
                  </div>
                  <div className="space-y-2">
                    <Label htmlFor={`dt-${day.id}`}>Notas</Label>
                    <Input
                      id={`dt-${day.id}`}
                      value={dayForm.notes}
                      onChange={(e) =>
                        setDayForm({ ...dayForm, notes: e.target.value })
                      }
                    />
                  </div>
                  <div className="sm:col-span-2">
                    <Button type="submit" disabled={busy}>
                      Guardar día
                    </Button>
                  </div>
                </form>
              ) : null}

              {day.exercises.length === 0 ? (
                <p className="mt-4 text-sm text-[var(--muted)]">
                  Sin ejercicios prescritos.
                </p>
              ) : (
                <ul className="mt-4 space-y-2">
                  {day.exercises.map((item) => {
                    const editingEx = editingExId === item.id;
                    return (
                      <li
                        key={item.id}
                        className="rounded-md border border-[var(--border)] px-3 py-3"
                      >
                        <div className="flex flex-wrap items-start justify-between gap-2">
                          <div className="min-w-0">
                            <p className="text-sm font-medium">
                              {item.sortOrder}. {item.exerciseName}
                            </p>
                            <p className="mt-1 text-xs text-[var(--muted)]">
                              {describePrescription(item)}
                            </p>
                            {item.notes ? (
                              <p className="mt-1 text-xs text-[var(--muted)]">
                                Notas: {item.notes}
                              </p>
                            ) : null}
                          </div>
                          {editable ? (
                            <Button
                              size="sm"
                              variant="secondary"
                              disabled={busy}
                              onClick={() => {
                                if (editingEx) {
                                  setEditingExId(null);
                                } else {
                                  setEditingExId(item.id);
                                  setExForm(prescriptionFromExercise(item));
                                }
                              }}
                            >
                              {editingEx ? "Cancelar" : "Editar"}
                            </Button>
                          ) : null}
                        </div>
                        {editingEx ? (
                          <form
                            className="mt-4 grid gap-4 border-t border-[var(--border)] pt-4 sm:grid-cols-2 lg:grid-cols-4"
                            onSubmit={(e) => {
                              e.preventDefault();
                              void run(
                                async () => {
                                  await trainingApi.updateWorkoutExercise(
                                    slug,
                                    item.id,
                                    {
                                      ...prescriptionToInput(exForm),
                                      sortOrder:
                                        intOrNull(exForm.sortOrder) ??
                                        item.sortOrder,
                                    },
                                  );
                                  return "Ejercicio del plan actualizado.";
                                },
                                () => setEditingExId(null),
                              );
                            }}
                          >
                            <PrescriptionInputs
                              idPrefix={`ex-${item.id}`}
                              value={exForm}
                              onChange={setExForm}
                              showOrder
                            />
                            <div className="sm:col-span-2 lg:col-span-4">
                              <Button type="submit" disabled={busy}>
                                Guardar ejercicio
                              </Button>
                            </div>
                          </form>
                        ) : null}
                      </li>
                    );
                  })}
                </ul>
              )}

              {editable ? (
                <div className="mt-4">
                  {adding ? (
                    <form
                      className="grid gap-4 border-t border-[var(--border)] pt-4 sm:grid-cols-2 lg:grid-cols-4"
                      onSubmit={(e) => {
                        e.preventDefault();
                        void run(
                          async () => {
                            await trainingApi.addWorkoutExercise(slug, day.id, {
                              ...prescriptionToInput(addForm),
                              exerciseId: addExerciseId,
                              sortOrder: null,
                            });
                            return "Ejercicio agregado al día.";
                          },
                          () => {
                            setAddingToDayId(null);
                            setAddExerciseId("");
                            setAddForm(emptyPrescription);
                          },
                        );
                      }}
                    >
                      <div className="space-y-2 sm:col-span-2 lg:col-span-4">
                        <Label htmlFor={`ae-${day.id}`}>Ejercicio</Label>
                        <select
                          id={`ae-${day.id}`}
                          required
                          className={selectClassName}
                          value={addExerciseId}
                          onChange={(e) => setAddExerciseId(e.target.value)}
                        >
                          <option value="">Selecciona un ejercicio…</option>
                          {exercises.map((exercise) => (
                            <option key={exercise.id} value={exercise.id}>
                              {exercise.name} ({exercise.primaryMuscle})
                            </option>
                          ))}
                        </select>
                        {exercises.length === 0 ? (
                          <p className="text-xs text-[var(--muted)]">
                            La biblioteca está vacía. Crea ejercicios en la
                            Biblioteca primero.
                          </p>
                        ) : null}
                      </div>
                      <PrescriptionInputs
                        idPrefix={`add-${day.id}`}
                        value={addForm}
                        onChange={setAddForm}
                      />
                      <div className="flex gap-2 sm:col-span-2 lg:col-span-4">
                        <Button type="submit" disabled={busy || !addExerciseId}>
                          Agregar ejercicio
                        </Button>
                        <Button
                          type="button"
                          variant="ghost"
                          onClick={() => setAddingToDayId(null)}
                        >
                          Cancelar
                        </Button>
                      </div>
                    </form>
                  ) : (
                    <Button
                      size="sm"
                      variant="secondary"
                      disabled={busy}
                      onClick={() => {
                        setAddingToDayId(day.id);
                        setAddExerciseId("");
                        setAddForm(emptyPrescription);
                      }}
                    >
                      Agregar ejercicio
                    </Button>
                  )}
                </div>
              ) : null}
            </Card>
          );
        })}

        {editable ? (
          <Card>
            <CardTitle>Nuevo día</CardTitle>
            <form
              className="mt-4 grid gap-4 sm:grid-cols-[1fr_1fr_auto] sm:items-end"
              onSubmit={(e) => {
                e.preventDefault();
                void run(
                  async () => {
                    await trainingApi.addDay(slug, plan.id, {
                      name: newDay.name.trim(),
                      notes: textOrNull(newDay.notes),
                    });
                    return "Día agregado.";
                  },
                  () => setNewDay({ name: "", notes: "" }),
                );
              }}
            >
              <div className="space-y-2">
                <Label htmlFor="nd-name">Nombre</Label>
                <Input
                  id="nd-name"
                  required
                  placeholder="Ej. Pecho y tríceps"
                  value={newDay.name}
                  onChange={(e) => setNewDay({ ...newDay, name: e.target.value })}
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="nd-notes">Notas</Label>
                <Input
                  id="nd-notes"
                  value={newDay.notes}
                  onChange={(e) =>
                    setNewDay({ ...newDay, notes: e.target.value })
                  }
                />
              </div>
              <Button type="submit" disabled={busy}>
                Agregar día
              </Button>
            </form>
          </Card>
        ) : null}
      </div>

      {canManage && status === "Published" ? (
        <Card>
          <CardTitle>Asignar a un alumno</CardTitle>
          <CardDescription>
            El alumno verá este plan en «Mi entrenamiento» y podrá iniciar
            sesiones.
          </CardDescription>
          <form
            className="mt-4 flex flex-wrap items-end gap-3"
            onSubmit={(e) => {
              e.preventDefault();
              void run(
                async () => {
                  const assignment = await trainingApi.assignPlan(
                    slug,
                    plan.id,
                    athleteId,
                  );
                  setAssignments((current) => [assignment, ...current]);
                  return "Plan asignado.";
                },
                () => setAthleteId(""),
              );
            }}
          >
            <div className="min-w-56 flex-1 space-y-2">
              <Label htmlFor="as-athlete">Alumno</Label>
              <select
                id="as-athlete"
                required
                className={selectClassName}
                value={athleteId}
                onChange={(e) => setAthleteId(e.target.value)}
              >
                <option value="">Selecciona un alumno…</option>
                {athletes.map((athlete) => (
                  <option key={athlete.id} value={athlete.id}>
                    {fullName(athlete)} ({athlete.email})
                  </option>
                ))}
              </select>
            </div>
            <Button type="submit" disabled={busy || !athleteId}>
              Asignar
            </Button>
          </form>

          {assignments.length > 0 ? (
            <ul className="mt-4 space-y-2">
              {assignments.map((assignment) => {
                const ended = String(assignment.status) === "Ended";
                return (
                  <li
                    key={assignment.id}
                    className="flex flex-wrap items-center justify-between gap-2 rounded-md border border-[var(--border)] px-3 py-2"
                  >
                    <p className="text-sm">
                      {athleteName(assignment.athleteProfileId)}
                      <span className="ml-2 text-xs text-[var(--muted)]">
                        Asignado: {formatDateTime(assignment.assignedAt)}
                      </span>{" "}
                      <Badge tone={ended ? "neutral" : "good"}>
                        {ended ? "Finalizada" : "Activa"}
                      </Badge>
                    </p>
                    {!ended ? (
                      <Button
                        size="sm"
                        variant="secondary"
                        disabled={busy}
                        onClick={() =>
                          run(
                            async () => {
                              const message = await trainingApi.endAssignment(
                                slug,
                                assignment.id,
                              );
                              setAssignments((current) =>
                                current.map((item) =>
                                  item.id === assignment.id
                                    ? { ...item, status: "Ended" }
                                    : item,
                                ),
                              );
                              return message || "Asignación finalizada.";
                            },
                          )
                        }
                      >
                        Finalizar asignación
                      </Button>
                    ) : null}
                  </li>
                );
              })}
            </ul>
          ) : null}
        </Card>
      ) : null}
    </div>
  );
}
