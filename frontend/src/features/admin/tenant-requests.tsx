"use client";

import { useParams } from "next/navigation";
import { useCallback, useEffect, useState } from "react";
import { Button } from "@/components/ui/button";
import { Card, CardDescription, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Badge, Notice } from "@/components/ui/notice";
import { roleLabel } from "@/lib/navigation";
import {
  ROLE_TO_NUMBER,
  errorMessage,
  formatDate,
  fullName,
  normalizeRole,
  requestStatusLabel,
  selectClassName,
} from "@/lib/admin";
import { adminApi } from "@/services/api";
import type { InvitationListItem, JoinRequestListItem } from "@/types/admin";

export function TenantRequests() {
  const { slug } = useParams<{ slug: string }>();
  const [requests, setRequests] = useState<JoinRequestListItem[]>([]);
  const [invitations, setInvitations] = useState<InvitationListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [busyId, setBusyId] = useState<string | null>(null);
  const [rejectingId, setRejectingId] = useState<string | null>(null);
  const [reason, setReason] = useState("");
  const [inviteEmail, setInviteEmail] = useState("");
  const [inviteRole, setInviteRole] = useState<"Trainer" | "Athlete">("Athlete");
  const [inviting, setInviting] = useState(false);

  const load = useCallback(async () => {
    try {
      const [requestList, invitationList] = await Promise.all([
        adminApi.listJoinRequests(slug),
        adminApi.listInvitations(slug),
      ]);
      setRequests(requestList);
      setInvitations(invitationList);
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
      return true;
    } catch (err) {
      setError(errorMessage(err));
      return false;
    } finally {
      setBusyId(null);
    }
  }

  async function onInvite(event: React.FormEvent) {
    event.preventDefault();
    setInviting(true);
    setError(null);
    setSuccess(null);
    try {
      await adminApi.createInvitation(
        slug,
        inviteEmail.trim(),
        ROLE_TO_NUMBER[inviteRole],
      );
      setSuccess(`Invitación enviada a ${inviteEmail.trim()}.`);
      setInviteEmail("");
      await load();
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setInviting(false);
    }
  }

  async function onReject(event: React.FormEvent, id: string) {
    event.preventDefault();
    const ok = await run(
      id,
      () => adminApi.rejectJoinRequest(slug, id, reason.trim()),
      "Solicitud rechazada.",
    );
    if (ok) {
      setRejectingId(null);
      setReason("");
    }
  }

  return (
    <div className="space-y-8">
      <p className="text-sm text-[var(--muted)]">
        Aprueba solicitudes de ingreso o invita directamente a entrenadores y
        alumnos por correo.
      </p>

      {error ? <Notice tone="error">{error}</Notice> : null}
      {success ? <Notice tone="success">{success}</Notice> : null}

      <Card>
        <CardTitle>Solicitudes de ingreso</CardTitle>
        <CardDescription>
          Personas que pidieron unirse a esta academia como alumnos.
        </CardDescription>
        {loading ? (
          <p className="mt-4 text-sm text-[var(--muted)]">Cargando…</p>
        ) : requests.length === 0 ? (
          <p className="mt-4 text-sm text-[var(--muted)]">
            No hay solicitudes.
          </p>
        ) : (
          <ul className="mt-4 space-y-2">
            {requests.map((request) => {
              const pending = request.status === "Pending";
              const busy = busyId === request.id;
              return (
                <li
                  key={request.id}
                  className="space-y-3 rounded-md border border-[var(--border)] px-3 py-3"
                >
                  <div className="flex flex-wrap items-center justify-between gap-2">
                    <div className="min-w-0">
                      <p className="font-medium">
                        {fullName(request)}{" "}
                        <Badge
                          tone={
                            pending
                              ? "warn"
                              : request.status === "Approved"
                                ? "good"
                                : "neutral"
                          }
                        >
                          {requestStatusLabel(request.status)}
                        </Badge>
                      </p>
                      <p className="truncate text-xs text-[var(--muted)]">
                        {request.email} · {formatDate(request.createdAt)}
                      </p>
                      {request.rejectionReason ? (
                        <p className="text-xs text-[var(--muted)]">
                          Motivo: {request.rejectionReason}
                        </p>
                      ) : null}
                    </div>
                    {pending ? (
                      <div className="flex gap-2">
                        <Button
                          size="sm"
                          disabled={busy}
                          onClick={() =>
                            run(
                              request.id,
                              () => adminApi.approveJoinRequest(slug, request.id),
                              "Solicitud aprobada.",
                            )
                          }
                        >
                          Aprobar
                        </Button>
                        <Button
                          size="sm"
                          variant="secondary"
                          disabled={busy}
                          onClick={() => {
                            setRejectingId(
                              rejectingId === request.id ? null : request.id,
                            );
                            setReason("");
                          }}
                        >
                          Rechazar
                        </Button>
                      </div>
                    ) : null}
                  </div>
                  {rejectingId === request.id ? (
                    <form
                      onSubmit={(e) => onReject(e, request.id)}
                      className="flex flex-wrap items-end gap-3 border-t border-[var(--border)] pt-3"
                    >
                      <div className="min-w-56 flex-1 space-y-2">
                        <Label htmlFor={`reason-${request.id}`}>Motivo</Label>
                        <Input
                          id={`reason-${request.id}`}
                          required
                          value={reason}
                          onChange={(e) => setReason(e.target.value)}
                        />
                      </div>
                      <Button type="submit" variant="danger" disabled={busy}>
                        Confirmar rechazo
                      </Button>
                    </form>
                  ) : null}
                </li>
              );
            })}
          </ul>
        )}
      </Card>

      <Card>
        <CardTitle>Invitar por correo</CardTitle>
        <CardDescription>
          El invitado recibe un token de un solo uso válido por 7 días.
        </CardDescription>
        <form
          onSubmit={onInvite}
          className="mt-4 grid gap-4 sm:grid-cols-[1fr_200px_auto]"
        >
          <div className="space-y-2">
            <Label htmlFor="invite-email">Correo</Label>
            <Input
              id="invite-email"
              type="email"
              required
              value={inviteEmail}
              onChange={(e) => setInviteEmail(e.target.value)}
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="invite-role">Rol</Label>
            <select
              id="invite-role"
              className={selectClassName}
              value={inviteRole}
              onChange={(e) => setInviteRole(e.target.value as "Trainer" | "Athlete")}
            >
              <option value="Athlete">{roleLabel("Athlete")}</option>
              <option value="Trainer">{roleLabel("Trainer")}</option>
            </select>
          </div>
          <div className="flex items-end">
            <Button type="submit" disabled={inviting}>
              {inviting ? "Enviando…" : "Enviar invitación"}
            </Button>
          </div>
        </form>

        <h4 className="mt-8 text-xs font-medium uppercase tracking-[0.14em] text-[var(--muted)]">
          Invitaciones
        </h4>
        {loading ? null : invitations.length === 0 ? (
          <p className="mt-3 text-sm text-[var(--muted)]">
            No hay invitaciones.
          </p>
        ) : (
          <ul className="mt-3 space-y-2">
            {invitations.map((invitation) => {
              const pending = invitation.status === "Pending";
              return (
                <li
                  key={invitation.id}
                  className="flex flex-wrap items-center justify-between gap-2 rounded-md border border-[var(--border)] px-3 py-3"
                >
                  <div className="min-w-0">
                    <p className="truncate font-medium">
                      {invitation.email}{" "}
                      <Badge tone={pending ? "warn" : "neutral"}>
                        {requestStatusLabel(invitation.status)}
                      </Badge>
                    </p>
                    <p className="text-xs text-[var(--muted)]">
                      {roleLabel(normalizeRole(invitation.role))} · expira{" "}
                      {formatDate(invitation.expiresAt)}
                    </p>
                  </div>
                  {pending ? (
                    <Button
                      size="sm"
                      variant="secondary"
                      disabled={busyId === invitation.id}
                      onClick={() =>
                        run(
                          invitation.id,
                          () => adminApi.cancelInvitation(slug, invitation.id),
                          "Invitación cancelada.",
                        )
                      }
                    >
                      Cancelar
                    </Button>
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
