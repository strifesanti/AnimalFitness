"use client";

import { useParams } from "next/navigation";
import { useCallback, useEffect, useState } from "react";
import { Button } from "@/components/ui/button";
import { Card, CardDescription, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Badge, Notice } from "@/components/ui/notice";
import { useAuth } from "@/features/auth/auth-context";
import {
  errorMessage,
  fullName,
  membershipStatusLabel,
  normalizeMembershipStatus,
  normalizeRole,
  selectClassName,
  textareaClassName,
} from "@/lib/admin";
import { adminApi } from "@/services/api";
import type {
  AthleteProfile,
  MemberListItem,
  TrainerProfile,
} from "@/types/admin";

type EditState = {
  heightCm: string;
  weightKg: string;
  birthDate: string;
  notes: string;
};

function toNumber(value: string): number | null {
  const trimmed = value.trim().replace(",", ".");
  if (!trimmed) {
    return null;
  }
  const parsed = Number(trimmed);
  return Number.isNaN(parsed) ? null : parsed;
}

export function TenantAthletes() {
  const { slug } = useParams<{ slug: string }>();
  const { user } = useAuth();
  const membership = user?.memberships.find((item) => item.slug === slug);
  const isOwner = membership ? normalizeRole(membership.role) === "Owner" : false;

  const [athletes, setAthletes] = useState<AthleteProfile[]>([]);
  const [members, setMembers] = useState<MemberListItem[]>([]);
  const [trainers, setTrainers] = useState<TrainerProfile[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [busyId, setBusyId] = useState<string | null>(null);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [edit, setEdit] = useState<EditState>({
    heightCm: "",
    weightKg: "",
    birthDate: "",
    notes: "",
  });
  const [assignTo, setAssignTo] = useState<Record<string, string>>({});

  const load = useCallback(async () => {
    try {
      const athleteList = await adminApi.listAthletes(slug);
      setAthletes(athleteList);
      if (isOwner) {
        const [memberList, trainerList] = await Promise.all([
          adminApi.listMembers(slug, "Athlete"),
          adminApi.listTrainers(slug),
        ]);
        setMembers(memberList);
        setTrainers(trainerList);
      }
      setError(null);
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setLoading(false);
    }
  }, [slug, isOwner]);

  useEffect(() => {
    if (!membership) {
      return;
    }
    // eslint-disable-next-line react-hooks/set-state-in-effect
    void load();
  }, [load, membership]);

  async function run(id: string, task: () => Promise<string>, fallback: string) {
    setBusyId(id);
    setError(null);
    setSuccess(null);
    try {
      setSuccess((await task()) || fallback);
      await load();
      return true;
    } catch (err) {
      setError(errorMessage(err));
      return false;
    } finally {
      setBusyId(null);
    }
  }

  function startEdit(athlete: AthleteProfile) {
    setEditingId(athlete.id);
    setEdit({
      heightCm: athlete.heightCm?.toString() ?? "",
      weightKg: athlete.weightKg?.toString() ?? "",
      birthDate: athlete.birthDate ?? "",
      notes: athlete.notes ?? "",
    });
  }

  async function saveEdit(event: React.FormEvent, athlete: AthleteProfile) {
    event.preventDefault();
    const ok = await run(
      athlete.id,
      () =>
        adminApi.updateAthlete(slug, athlete.id, {
          heightCm: toNumber(edit.heightCm),
          weightKg: toNumber(edit.weightKg),
          birthDate: edit.birthDate || null,
          notes: edit.notes.trim() || null,
        }),
      "Perfil de alumno actualizado.",
    );
    if (ok) {
      setEditingId(null);
    }
  }

  const trainerName = (profileId: string | null) => {
    if (!profileId) {
      return null;
    }
    const trainer = trainers.find((item) => item.id === profileId);
    return trainer ? fullName(trainer) : "Asignado";
  };

  return (
    <div className="space-y-8">
      <p className="text-sm text-[var(--muted)]">
        {isOwner
          ? "Gestiona los alumnos de la academia, sus perfiles y entrenadores asignados."
          : "Alumnos que tienes asignados."}
      </p>

      {error ? <Notice tone="error">{error}</Notice> : null}
      {success ? <Notice tone="success">{success}</Notice> : null}

      <Card>
        <CardTitle>{isOwner ? "Alumnos" : "Mis alumnos"}</CardTitle>
        <CardDescription>
          Datos físicos y notas visibles para el equipo de la academia.
        </CardDescription>
        {loading ? (
          <p className="mt-4 text-sm text-[var(--muted)]">Cargando…</p>
        ) : athletes.length === 0 ? (
          <p className="mt-4 text-sm text-[var(--muted)]">
            No hay alumnos para mostrar.
          </p>
        ) : (
          <ul className="mt-4 space-y-2">
            {athletes.map((athlete) => {
              const member = members.find((item) => item.userId === athlete.userId);
              const status = member ? normalizeMembershipStatus(member.status) : null;
              const busy = busyId === athlete.id || busyId === member?.membershipId;
              const assigned = trainerName(athlete.assignedTrainerProfileId);
              const editing = editingId === athlete.id;
              return (
                <li
                  key={athlete.id}
                  className="space-y-3 rounded-md border border-[var(--border)] px-3 py-3"
                >
                  <div className="flex flex-wrap items-start justify-between gap-2">
                    <div className="min-w-0">
                      <p className="font-medium">
                        {fullName(athlete)}{" "}
                        {status ? (
                          <Badge
                            tone={
                              status === "Active"
                                ? "good"
                                : status === "Suspended"
                                  ? "warn"
                                  : "bad"
                            }
                          >
                            {membershipStatusLabel(status)}
                          </Badge>
                        ) : null}
                      </p>
                      <p className="truncate text-xs text-[var(--muted)]">
                        {athlete.email}
                      </p>
                      <p className="mt-1 text-xs text-[var(--muted)]">
                        Altura: {athlete.heightCm ?? "—"} cm · Peso:{" "}
                        {athlete.weightKg ?? "—"} kg · Nacimiento:{" "}
                        {athlete.birthDate ?? "—"}
                        {isOwner ? ` · Entrenador: ${assigned ?? "sin asignar"}` : ""}
                      </p>
                      {athlete.notes ? (
                        <p className="mt-1 text-xs text-[var(--muted)]">
                          Notas: {athlete.notes}
                        </p>
                      ) : null}
                    </div>
                    <div className="flex flex-wrap gap-2">
                      <Button
                        size="sm"
                        variant="secondary"
                        disabled={busy}
                        onClick={() => (editing ? setEditingId(null) : startEdit(athlete))}
                      >
                        {editing ? "Cancelar" : "Editar"}
                      </Button>
                      {member && status === "Active" ? (
                        <Button
                          size="sm"
                          variant="secondary"
                          disabled={busy}
                          onClick={() =>
                            run(
                              member.membershipId,
                              () => adminApi.suspendMember(slug, member.membershipId),
                              "Miembro suspendido.",
                            )
                          }
                        >
                          Suspender
                        </Button>
                      ) : null}
                      {member && status === "Suspended" ? (
                        <Button
                          size="sm"
                          disabled={busy}
                          onClick={() =>
                            run(
                              member.membershipId,
                              () => adminApi.reactivateMember(slug, member.membershipId),
                              "Miembro reactivado.",
                            )
                          }
                        >
                          Reactivar
                        </Button>
                      ) : null}
                      {member && status !== "Removed" ? (
                        <Button
                          size="sm"
                          variant="danger"
                          disabled={busy}
                          onClick={() => {
                            if (
                              window.confirm(
                                `¿Eliminar a ${fullName(athlete)} de la academia?`,
                              )
                            ) {
                              void run(
                                member.membershipId,
                                () => adminApi.removeMember(slug, member.membershipId),
                                "Miembro eliminado.",
                              );
                            }
                          }}
                        >
                          Eliminar
                        </Button>
                      ) : null}
                    </div>
                  </div>

                  {isOwner ? (
                    <div className="flex flex-wrap items-center gap-2">
                      <select
                        aria-label={`Entrenador para ${fullName(athlete)}`}
                        className={`${selectClassName} h-9 w-auto min-w-48 text-xs`}
                        value={
                          assignTo[athlete.id] ??
                          athlete.assignedTrainerProfileId ??
                          ""
                        }
                        onChange={(e) =>
                          setAssignTo({ ...assignTo, [athlete.id]: e.target.value })
                        }
                      >
                        <option value="">Selecciona entrenador…</option>
                        {trainers.map((trainer) => (
                          <option key={trainer.id} value={trainer.id}>
                            {fullName(trainer)}
                          </option>
                        ))}
                      </select>
                      <Button
                        size="sm"
                        disabled={
                          busy ||
                          !(assignTo[athlete.id] ?? "") ||
                          assignTo[athlete.id] === athlete.assignedTrainerProfileId
                        }
                        onClick={() =>
                          run(
                            athlete.id,
                            () =>
                              adminApi.assignAthlete(
                                slug,
                                assignTo[athlete.id],
                                athlete.id,
                              ),
                            "Alumno asignado.",
                          )
                        }
                      >
                        Asignar
                      </Button>
                    </div>
                  ) : null}

                  {editing ? (
                    <form
                      onSubmit={(e) => saveEdit(e, athlete)}
                      className="grid gap-4 border-t border-[var(--border)] pt-4 sm:grid-cols-3"
                    >
                      <div className="space-y-2">
                        <Label htmlFor={`h-${athlete.id}`}>Altura (cm)</Label>
                        <Input
                          id={`h-${athlete.id}`}
                          inputMode="decimal"
                          value={edit.heightCm}
                          onChange={(e) => setEdit({ ...edit, heightCm: e.target.value })}
                        />
                      </div>
                      <div className="space-y-2">
                        <Label htmlFor={`w-${athlete.id}`}>Peso (kg)</Label>
                        <Input
                          id={`w-${athlete.id}`}
                          inputMode="decimal"
                          value={edit.weightKg}
                          onChange={(e) => setEdit({ ...edit, weightKg: e.target.value })}
                        />
                      </div>
                      <div className="space-y-2">
                        <Label htmlFor={`b-${athlete.id}`}>Nacimiento</Label>
                        <Input
                          id={`b-${athlete.id}`}
                          type="date"
                          value={edit.birthDate}
                          onChange={(e) => setEdit({ ...edit, birthDate: e.target.value })}
                        />
                      </div>
                      <div className="space-y-2 sm:col-span-3">
                        <Label htmlFor={`n-${athlete.id}`}>Notas</Label>
                        <textarea
                          id={`n-${athlete.id}`}
                          className={textareaClassName}
                          value={edit.notes}
                          onChange={(e) => setEdit({ ...edit, notes: e.target.value })}
                        />
                      </div>
                      <div className="sm:col-span-3">
                        <Button type="submit" disabled={busy}>
                          {busy ? "Guardando…" : "Guardar cambios"}
                        </Button>
                      </div>
                    </form>
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
