"use client";

import { useParams } from "next/navigation";
import { useCallback, useEffect, useState } from "react";
import { Button } from "@/components/ui/button";
import { Card, CardDescription, CardTitle } from "@/components/ui/card";
import { Badge, Notice } from "@/components/ui/notice";
import { roleLabel } from "@/lib/navigation";
import {
  ROLE_TO_NUMBER,
  errorMessage,
  formatDate,
  fullName,
  membershipStatusLabel,
  normalizeMembershipStatus,
  normalizeRole,
  selectClassName,
} from "@/lib/admin";
import { adminApi } from "@/services/api";
import type { MemberListItem, TrainerProfile } from "@/types/admin";
import type { MembershipRole } from "@/types/auth";

const ROLES: MembershipRole[] = ["Owner", "Trainer", "Athlete"];

export function TenantTrainers() {
  const { slug } = useParams<{ slug: string }>();
  const [members, setMembers] = useState<MemberListItem[]>([]);
  const [profiles, setProfiles] = useState<TrainerProfile[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [busyId, setBusyId] = useState<string | null>(null);

  const load = useCallback(async () => {
    try {
      const [memberList, profileList] = await Promise.all([
        adminApi.listMembers(slug, "Trainer"),
        adminApi.listTrainers(slug),
      ]);
      setMembers(memberList);
      setProfiles(profileList);
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

  async function run(id: string, task: () => Promise<string>, fallback: string) {
    setBusyId(id);
    setError(null);
    setSuccess(null);
    try {
      setSuccess((await task()) || fallback);
      await load();
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setBusyId(null);
    }
  }

  return (
    <div className="space-y-8">
      <p className="text-sm text-[var(--muted)]">
        Gestiona los entrenadores de la academia. Para sumar uno nuevo, envía
        una invitación desde Solicitudes.
      </p>

      {error ? <Notice tone="error">{error}</Notice> : null}
      {success ? <Notice tone="success">{success}</Notice> : null}

      <Card>
        <CardTitle>Entrenadores</CardTitle>
        <CardDescription>
          Miembros con rol de entrenador y sus alumnos activos.
        </CardDescription>
        {loading ? (
          <p className="mt-4 text-sm text-[var(--muted)]">Cargando…</p>
        ) : members.length === 0 ? (
          <p className="mt-4 text-sm text-[var(--muted)]">
            No hay entrenadores en esta academia.
          </p>
        ) : (
          <ul className="mt-4 space-y-2">
            {members.map((member) => {
              const status = normalizeMembershipStatus(member.status);
              const role = normalizeRole(member.role);
              const profile = profiles.find((item) => item.userId === member.userId);
              const busy = busyId === member.membershipId;
              return (
                <li
                  key={member.membershipId}
                  className="space-y-3 rounded-md border border-[var(--border)] px-3 py-3"
                >
                  <div className="flex flex-wrap items-center justify-between gap-2">
                    <div className="min-w-0">
                      <p className="font-medium">
                        {fullName(member)}{" "}
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
                      </p>
                      <p className="truncate text-xs text-[var(--muted)]">
                        {member.email} · desde {formatDate(member.joinedAt)}
                        {profile
                          ? ` · ${profile.activeAthleteCount} alumno(s) activos`
                          : ""}
                      </p>
                      {profile?.specialties ? (
                        <p className="text-xs text-[var(--muted)]">
                          Especialidades: {profile.specialties}
                        </p>
                      ) : null}
                    </div>
                    <div className="flex flex-wrap items-center gap-2">
                      <select
                        aria-label={`Rol de ${fullName(member)}`}
                        className={`${selectClassName} h-9 w-auto text-xs`}
                        value={role}
                        disabled={busy || status === "Removed"}
                        onChange={(e) => {
                          const next = e.target.value as MembershipRole;
                          void run(
                            member.membershipId,
                            () =>
                              adminApi.changeMemberRole(
                                slug,
                                member.membershipId,
                                ROLE_TO_NUMBER[next],
                              ),
                            "Rol actualizado.",
                          );
                        }}
                      >
                        {ROLES.map((item) => (
                          <option key={item} value={item}>
                            {roleLabel(item)}
                          </option>
                        ))}
                      </select>
                      {status === "Active" ? (
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
                      {status === "Suspended" ? (
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
                      {status !== "Removed" ? (
                        <Button
                          size="sm"
                          variant="danger"
                          disabled={busy}
                          onClick={() => {
                            if (
                              window.confirm(
                                `¿Eliminar a ${fullName(member)} de la academia?`,
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
                </li>
              );
            })}
          </ul>
        )}
      </Card>
    </div>
  );
}
