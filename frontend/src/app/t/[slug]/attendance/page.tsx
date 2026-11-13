import { Suspense } from "react";
import { AttendanceView } from "@/features/attendance/attendance-view";

export const instant = false;

export default function AttendancePage() {
  return (
    <Suspense
      fallback={<p className="text-sm text-[var(--muted)]">Cargando asistencia…</p>}
    >
      <AttendanceView />
    </Suspense>
  );
}
