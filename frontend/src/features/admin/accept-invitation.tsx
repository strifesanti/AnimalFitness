"use client";

import { useRouter, useSearchParams } from "next/navigation";
import { useState } from "react";
import { BrandMark } from "@/components/brand/brand-mark";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Notice } from "@/components/ui/notice";
import { RequireAuth } from "@/components/layout/require-auth";
import { useAuth } from "@/features/auth/auth-context";
import { roleLabel } from "@/lib/navigation";
import { errorMessage, normalizeRole } from "@/lib/admin";
import { adminApi } from "@/services/api";

export function AcceptInvitation() {
  const router = useRouter();
  const params = useSearchParams();
  const { refresh } = useAuth();
  const [token, setToken] = useState(params.get("token") ?? "");
  const [submitting, setSubmitting] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  async function onSubmit(event: React.FormEvent) {
    event.preventDefault();
    setSubmitting(true);
    setError(null);
    setSuccess(null);
    try {
      const result = await adminApi.acceptInvitation(token.trim());
      await refresh();
      setSuccess(
        `Invitación aceptada. Ahora eres ${roleLabel(normalizeRole(result.role)).toLowerCase()} en la academia.`,
      );
      router.replace(`/t/${result.slug}`);
    } catch (err) {
      setError(errorMessage(err));
      setSubmitting(false);
    }
  }

  const loginHref = token
    ? `/login?next=${encodeURIComponent(`/invitations/accept?token=${encodeURIComponent(token)}`)}`
    : "/login";

  return (
    <RequireAuth loginHref={loginHref}>
      <div className="grid min-h-screen place-items-center px-6 py-12">
        <div className="w-full max-w-md space-y-6">
          <div>
            <BrandMark size={56} />
            <h1 className="mt-6 font-[family-name:var(--font-display)] text-3xl tracking-wide">
              Aceptar invitación
            </h1>
            <p className="mt-2 text-sm text-[var(--muted)]">
              Pega el token que recibiste por correo para unirte a la academia.
              Debes iniciar sesión con el mismo correo al que llegó la invitación.
            </p>
          </div>

          <form onSubmit={onSubmit} className="space-y-5">
            <div className="space-y-2">
              <Label htmlFor="token">Token de invitación</Label>
              <Input
                id="token"
                required
                autoComplete="off"
                value={token}
                onChange={(e) => setToken(e.target.value)}
              />
            </div>

            {error ? <Notice tone="error">{error}</Notice> : null}
            {success ? <Notice tone="success">{success}</Notice> : null}

            <Button type="submit" className="w-full" disabled={submitting || !token.trim()}>
              {submitting ? "Aceptando…" : "Aceptar invitación"}
            </Button>
          </form>
        </div>
      </div>
    </RequireAuth>
  );
}
