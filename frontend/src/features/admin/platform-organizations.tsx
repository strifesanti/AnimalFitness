"use client";

import Link from "next/link";
import { useCallback, useEffect, useState } from "react";
import { Button } from "@/components/ui/button";
import { Card, CardDescription, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Badge, Notice } from "@/components/ui/notice";
import {
  errorMessage,
  formatDate,
  normalizeTenantStatus,
  tenantStatusLabel,
} from "@/lib/admin";
import { adminApi } from "@/services/api";
import type { TenantListItem } from "@/types/admin";

const emptyForm = {
  name: "",
  slug: "",
  contactEmail: "",
  ownerEmail: "",
  phone: "",
};

export function PlatformOrganizations() {
  const [tenants, setTenants] = useState<TenantListItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [busyId, setBusyId] = useState<string | null>(null);
  const [form, setForm] = useState(emptyForm);
  const [creating, setCreating] = useState(false);

  const load = useCallback(async () => {
    try {
      setTenants(await adminApi.listTenants());
      setError(null);
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect
    void load();
  }, [load]);

  async function onCreate(event: React.FormEvent) {
    event.preventDefault();
    setCreating(true);
    setError(null);
    setSuccess(null);
    try {
      await adminApi.createTenant({
        name: form.name.trim(),
        slug: form.slug.trim().toLowerCase(),
        contactEmail: form.contactEmail.trim(),
        ownerEmail: form.ownerEmail.trim(),
        phone: form.phone.trim() || null,
      });
      setSuccess("Organización creada.");
      setForm(emptyForm);
      await load();
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setCreating(false);
    }
  }

  async function onToggle(tenant: TenantListItem, activate: boolean) {
    setBusyId(tenant.id);
    setError(null);
    setSuccess(null);
    try {
      const message = activate
        ? await adminApi.activateTenant(tenant.id)
        : await adminApi.suspendTenant(tenant.id);
      setSuccess(message || (activate ? "Organización activada." : "Organización suspendida."));
      await load();
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setBusyId(null);
    }
  }

  return (
    <div className="space-y-8">
      <div>
        <p className="text-sm text-[var(--muted)]">
          Crea, activa y suspende academias de la plataforma.
        </p>
      </div>

      {error ? <Notice tone="error">{error}</Notice> : null}
      {success ? <Notice tone="success">{success}</Notice> : null}

      <Card>
        <CardTitle>Nueva organización</CardTitle>
        <CardDescription>
          El correo del administrador debe pertenecer a un usuario registrado.
        </CardDescription>
        <form onSubmit={onCreate} className="mt-4 grid gap-4 sm:grid-cols-2">
          <div className="space-y-2">
            <Label htmlFor="org-name">Nombre</Label>
            <Input
              id="org-name"
              required
              value={form.name}
              onChange={(e) => setForm({ ...form, name: e.target.value })}
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="org-slug">Slug</Label>
            <Input
              id="org-slug"
              required
              placeholder="mi-academia"
              value={form.slug}
              onChange={(e) => setForm({ ...form, slug: e.target.value })}
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="org-contact">Correo de contacto</Label>
            <Input
              id="org-contact"
              type="email"
              required
              value={form.contactEmail}
              onChange={(e) => setForm({ ...form, contactEmail: e.target.value })}
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="org-owner">Correo del administrador</Label>
            <Input
              id="org-owner"
              type="email"
              required
              value={form.ownerEmail}
              onChange={(e) => setForm({ ...form, ownerEmail: e.target.value })}
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="org-phone">Teléfono (opcional)</Label>
            <Input
              id="org-phone"
              value={form.phone}
              onChange={(e) => setForm({ ...form, phone: e.target.value })}
            />
          </div>
          <div className="flex items-end">
            <Button type="submit" disabled={creating}>
              {creating ? "Creando…" : "Crear organización"}
            </Button>
          </div>
        </form>
      </Card>

      <Card>
        <CardTitle>Organizaciones</CardTitle>
        {loading ? (
          <p className="mt-4 text-sm text-[var(--muted)]">Cargando…</p>
        ) : tenants.length === 0 ? (
          <p className="mt-4 text-sm text-[var(--muted)]">
            Aún no hay organizaciones.
          </p>
        ) : (
          <ul className="mt-4 space-y-2">
            {tenants.map((tenant) => {
              const status = normalizeTenantStatus(tenant.status);
              return (
                <li
                  key={tenant.id}
                  className="flex flex-wrap items-center justify-between gap-3 rounded-md border border-[var(--border)] px-3 py-3"
                >
                  <div className="min-w-0">
                    <p className="font-medium">
                      {tenant.name}{" "}
                      <Badge
                        tone={
                          status === "Active"
                            ? "good"
                            : status === "Suspended"
                              ? "bad"
                              : "warn"
                        }
                      >
                        {tenantStatusLabel(status)}
                      </Badge>
                    </p>
                    <p className="truncate text-xs text-[var(--muted)]">
                      {tenant.slug} · {tenant.contactEmail} · creada{" "}
                      {formatDate(tenant.createdAt)}
                    </p>
                  </div>
                  <div className="flex gap-2">
                    <Link
                      href={`/t/${tenant.slug}`}
                      className="inline-flex h-9 items-center rounded-md border border-[var(--border)] px-3 text-xs font-semibold hover:border-[var(--accent)]"
                    >
                      Abrir
                    </Link>
                    {status !== "Active" ? (
                      <Button
                        size="sm"
                        disabled={busyId === tenant.id}
                        onClick={() => onToggle(tenant, true)}
                      >
                        Activar
                      </Button>
                    ) : null}
                    {status !== "Suspended" ? (
                      <Button
                        size="sm"
                        variant="danger"
                        disabled={busyId === tenant.id}
                        onClick={() => onToggle(tenant, false)}
                      >
                        Suspender
                      </Button>
                    ) : null}
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
