import { Suspense } from "react";
import { MyProfile } from "@/features/admin/my-profile";

export const instant = false;

export default function TenantProfilePage() {
  return (
    <Suspense
      fallback={<p className="text-sm text-[var(--muted)]">Cargando perfil…</p>}
    >
      <MyProfile />
    </Suspense>
  );
}
