import { Suspense } from "react";
import { AcceptInvitation } from "@/features/admin/accept-invitation";

export const instant = false;

export default function AcceptInvitationPage() {
  return (
    <Suspense
      fallback={
        <div className="grid min-h-screen place-items-center text-[var(--muted)]">
          Cargando invitación…
        </div>
      }
    >
      <AcceptInvitation />
    </Suspense>
  );
}
