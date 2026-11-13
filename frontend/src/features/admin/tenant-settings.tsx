"use client";

import { useParams } from "next/navigation";
import { useEffect, useState } from "react";
import { Button } from "@/components/ui/button";
import { Card, CardDescription, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Notice } from "@/components/ui/notice";
import { errorMessage } from "@/lib/admin";
import { adminApi } from "@/services/api";
import type { TenantDetail } from "@/types/admin";

type OrgForm = {
  name: string;
  contactEmail: string;
  phone: string;
  timeZoneId: string;
  currency: string;
};

type PrefsForm = { primaryColor: string; locale: string };

function toForms(detail: TenantDetail): { org: OrgForm; prefs: PrefsForm } {
  return {
    org: {
      name: detail.name,
      contactEmail: detail.contactEmail,
      phone: detail.phone ?? "",
      timeZoneId: detail.timeZoneId,
      currency: detail.currency,
    },
    prefs: { primaryColor: detail.primaryColor, locale: detail.locale },
  };
}

export function TenantSettings() {
  const { slug } = useParams<{ slug: string }>();
  const [detail, setDetail] = useState<TenantDetail | null>(null);
  const [org, setOrg] = useState<OrgForm | null>(null);
  const [prefs, setPrefs] = useState<PrefsForm | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [savingOrg, setSavingOrg] = useState(false);
  const [savingPrefs, setSavingPrefs] = useState(false);

  useEffect(() => {
    let active = true;
    (async () => {
      try {
        const loaded = await adminApi.getOrganization(slug);
        if (!active) {
          return;
        }
        const forms = toForms(loaded);
        setDetail(loaded);
        setOrg(forms.org);
        setPrefs(forms.prefs);
      } catch (err) {
        if (active) {
          setError(errorMessage(err));
        }
      } finally {
        if (active) {
          setLoading(false);
        }
      }
    })();
    return () => {
      active = false;
    };
  }, [slug]);

  function apply(updated: TenantDetail) {
    const forms = toForms(updated);
    setDetail(updated);
    setOrg(forms.org);
    setPrefs(forms.prefs);
  }

  async function onSaveOrg(event: React.FormEvent) {
    event.preventDefault();
    if (!org) {
      return;
    }
    setSavingOrg(true);
    setError(null);
    setSuccess(null);
    try {
      apply(
        await adminApi.updateOrganization(slug, {
          name: org.name.trim(),
          contactEmail: org.contactEmail.trim(),
          phone: org.phone.trim() || null,
          timeZoneId: org.timeZoneId.trim(),
          currency: org.currency.trim().toUpperCase(),
        }),
      );
      setSuccess("Organización actualizada.");
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setSavingOrg(false);
    }
  }

  async function onSavePrefs(event: React.FormEvent) {
    event.preventDefault();
    if (!prefs || !detail) {
      return;
    }
    setSavingPrefs(true);
    setError(null);
    setSuccess(null);
    try {
      apply(
        await adminApi.updateOrganizationSettings(slug, {
          primaryColor: prefs.primaryColor.trim() || null,
          locale: prefs.locale.trim() || null,
          currency: detail.currency,
          settingsJson: detail.settingsJson,
        }),
      );
      setSuccess("Preferencias actualizadas.");
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setSavingPrefs(false);
    }
  }

  if (loading) {
    return <p className="text-sm text-[var(--muted)]">Cargando configuración…</p>;
  }

  return (
    <div className="space-y-8">
      <p className="text-sm text-[var(--muted)]">
        Datos de contacto y preferencias de tu academia.
        {detail ? ` Slug: ${detail.slug}.` : ""}
      </p>

      {error ? <Notice tone="error">{error}</Notice> : null}
      {success ? <Notice tone="success">{success}</Notice> : null}

      {org ? (
        <Card>
          <CardTitle>Organización</CardTitle>
          <CardDescription>Información general y regional.</CardDescription>
          <form onSubmit={onSaveOrg} className="mt-4 grid gap-4 sm:grid-cols-2">
            <div className="space-y-2">
              <Label htmlFor="s-name">Nombre</Label>
              <Input
                id="s-name"
                required
                value={org.name}
                onChange={(e) => setOrg({ ...org, name: e.target.value })}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="s-email">Correo de contacto</Label>
              <Input
                id="s-email"
                type="email"
                required
                value={org.contactEmail}
                onChange={(e) => setOrg({ ...org, contactEmail: e.target.value })}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="s-phone">Teléfono</Label>
              <Input
                id="s-phone"
                value={org.phone}
                onChange={(e) => setOrg({ ...org, phone: e.target.value })}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="s-tz">Zona horaria</Label>
              <Input
                id="s-tz"
                required
                placeholder="America/Bogota"
                value={org.timeZoneId}
                onChange={(e) => setOrg({ ...org, timeZoneId: e.target.value })}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="s-currency">Moneda</Label>
              <Input
                id="s-currency"
                required
                maxLength={3}
                placeholder="USD"
                value={org.currency}
                onChange={(e) => setOrg({ ...org, currency: e.target.value })}
              />
            </div>
            <div className="flex items-end">
              <Button type="submit" disabled={savingOrg}>
                {savingOrg ? "Guardando…" : "Guardar organización"}
              </Button>
            </div>
          </form>
        </Card>
      ) : null}

      {prefs ? (
        <Card>
          <CardTitle>Preferencias visuales</CardTitle>
          <CardDescription>Color principal e idioma de la academia.</CardDescription>
          <form onSubmit={onSavePrefs} className="mt-4 grid gap-4 sm:grid-cols-2">
            <div className="space-y-2">
              <Label htmlFor="p-color">Color principal</Label>
              <div className="flex items-center gap-3">
                <input
                  type="color"
                  aria-label="Selector de color"
                  className="h-11 w-14 cursor-pointer rounded-md border border-[var(--border)] bg-[var(--surface)] p-1"
                  value={/^#[0-9a-fA-F]{6}$/.test(prefs.primaryColor) ? prefs.primaryColor : "#7C3AED"}
                  onChange={(e) => setPrefs({ ...prefs, primaryColor: e.target.value })}
                />
                <Input
                  id="p-color"
                  placeholder="#7C3AED"
                  value={prefs.primaryColor}
                  onChange={(e) => setPrefs({ ...prefs, primaryColor: e.target.value })}
                />
              </div>
            </div>
            <div className="space-y-2">
              <Label htmlFor="p-locale">Idioma</Label>
              <Input
                id="p-locale"
                placeholder="es"
                value={prefs.locale}
                onChange={(e) => setPrefs({ ...prefs, locale: e.target.value })}
              />
            </div>
            <div>
              <Button type="submit" disabled={savingPrefs}>
                {savingPrefs ? "Guardando…" : "Guardar preferencias"}
              </Button>
            </div>
          </form>
        </Card>
      ) : null}
    </div>
  );
}
