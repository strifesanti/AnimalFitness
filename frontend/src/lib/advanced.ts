import type {
  AttendanceStatus,
  ChallengeKind,
  ChallengeMetric,
  ChallengeStatus,
  MealType,
} from "@/types/advanced";

export const MEAL_TYPES: MealType[] = ["Breakfast", "Lunch", "Dinner", "Snack", "Other"];

export function mealLabel(value: unknown): string {
  switch (String(value)) {
    case "Breakfast":
      return "Desayuno";
    case "Lunch":
      return "Almuerzo";
    case "Dinner":
      return "Cena";
    case "Snack":
      return "Snack";
    default:
      return "Otro";
  }
}

export const ATTENDANCE_STATUSES: AttendanceStatus[] = [
  "Present",
  "Late",
  "Excused",
  "Absent",
];

export function attendanceStatus(value: unknown): AttendanceStatus {
  const text = String(value);
  return text === "Absent" || text === "Excused" || text === "Late" ? text : "Present";
}

export function attendanceLabel(value: unknown): string {
  switch (attendanceStatus(value)) {
    case "Present":
      return "Presente";
    case "Absent":
      return "Ausente";
    case "Excused":
      return "Justificado";
    case "Late":
      return "Tarde";
  }
}

export function attendanceTone(value: unknown): "neutral" | "good" | "warn" | "bad" {
  switch (attendanceStatus(value)) {
    case "Present":
      return "good";
    case "Late":
      return "warn";
    case "Excused":
      return "neutral";
    case "Absent":
      return "bad";
  }
}

export function challengeStatus(value: unknown): ChallengeStatus {
  const text = String(value);
  return text === "Active" || text === "Completed" || text === "Cancelled"
    ? text
    : "Draft";
}

export function challengeStatusLabel(value: unknown): string {
  switch (challengeStatus(value)) {
    case "Draft":
      return "Borrador";
    case "Active":
      return "Activo";
    case "Completed":
      return "Finalizado";
    case "Cancelled":
      return "Cancelado";
  }
}

export function challengeStatusTone(value: unknown): "neutral" | "good" | "warn" | "bad" {
  switch (challengeStatus(value)) {
    case "Draft":
      return "neutral";
    case "Active":
      return "good";
    case "Completed":
      return "warn";
    case "Cancelled":
      return "bad";
  }
}

export const CHALLENGE_KINDS: ChallengeKind[] = ["Individual", "Group"];

export function challengeKindLabel(value: unknown): string {
  return String(value) === "Group" ? "Grupal" : "Individual";
}

export const CHALLENGE_METRICS: ChallengeMetric[] = [
  "ManualPoints",
  "WorkoutsCompleted",
  "AttendanceDays",
];

export function challengeMetricLabel(value: unknown): string {
  switch (String(value)) {
    case "WorkoutsCompleted":
      return "Entrenamientos completados";
    case "AttendanceDays":
      return "Días de asistencia";
    default:
      return "Puntos manuales";
  }
}

/** Fecha `yyyy-MM-dd` en hora local con un desplazamiento de días. */
export function dateInputOffset(days: number): string {
  const date = new Date();
  date.setDate(date.getDate() + days);
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${date.getFullYear()}-${month}-${day}`;
}

/** Fecha corta `yyyy-MM-dd` (sin zona horaria) para mostrar. */
export function formatDay(value: string): string {
  const [year, month, day] = value.split("-").map(Number);
  if (!year || !month || !day) {
    return value;
  }
  return new Date(year, month - 1, day).toLocaleDateString("es", {
    weekday: "short",
    day: "numeric",
    month: "short",
  });
}

/** Porcentaje acotado a 0–100 para barras de progreso. */
export function clampPercent(value: number | null | undefined): number {
  if (value === null || value === undefined || Number.isNaN(value)) {
    return 0;
  }
  return Math.min(100, Math.max(0, value));
}

/** Hora local `HH:mm` de un instante ISO. */
export function formatTime(value: string): string {
  const date = new Date(value);
  return Number.isNaN(date.getTime())
    ? ""
    : date.toLocaleTimeString("es", { hour: "2-digit", minute: "2-digit" });
}
