"use client";

import { useEffect, useState } from "react";
import { Button } from "@/components/ui/button";
import { Card, CardDescription, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Notice } from "@/components/ui/notice";
import { useAuth } from "@/features/auth/auth-context";
import { errorMessage } from "@/lib/admin";
import { adminApi } from "@/services/api";

export function MyProfile() {
  const { user, refresh } = useAuth();
  const [firstName, setFirstName] = useState("");
  const [lastName, setLastName] = useState("");
  const [phone, setPhone] = useState("");
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  useEffect(() => {
    if (user) {
      // eslint-disable-next-line react-hooks/set-state-in-effect
      setFirstName(user.firstName);
      setLastName(user.lastName);
      setPhone(user.phoneNumber ?? "");
    }
  }, [user]);

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    setSaving(true);
    setError(null);
    setSuccess(null);
    try {
      const message = await adminApi.updateMyProfile({
        firstName: firstName.trim(),
        lastName: lastName.trim(),
        phoneNumber: phone.trim() || null,
      });
      await refresh();
      setSuccess(message || "Perfil actualizado.");
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="space-y-8">
      <p className="text-sm text-[var(--muted)]">
        Tus datos personales se comparten en todas tus academias.
      </p>

      {error ? <Notice tone="error">{error}</Notice> : null}
      {success ? <Notice tone="success">{success}</Notice> : null}

      <Card>
        <CardTitle>Mi perfil</CardTitle>
        <CardDescription>Correo: {user?.email}</CardDescription>
        <form onSubmit={onSubmit} className="mt-4 grid gap-4 sm:grid-cols-2">
          <div className="space-y-2">
            <Label htmlFor="pf-first">Nombre</Label>
            <Input
              id="pf-first"
              required
              autoComplete="given-name"
              value={firstName}
              onChange={(e) => setFirstName(e.target.value)}
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="pf-last">Apellido</Label>
            <Input
              id="pf-last"
              required
              autoComplete="family-name"
              value={lastName}
              onChange={(e) => setLastName(e.target.value)}
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="pf-phone">Teléfono</Label>
            <Input
              id="pf-phone"
              type="tel"
              autoComplete="tel"
              value={phone}
              onChange={(e) => setPhone(e.target.value)}
            />
          </div>
          <div className="flex items-end">
            <Button type="submit" disabled={saving}>
              {saving ? "Guardando…" : "Guardar perfil"}
            </Button>
          </div>
        </form>
      </Card>
    </div>
  );
}
