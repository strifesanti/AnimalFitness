import type {
  ExerciseDifficulty,
  ExerciseType,
  WorkoutPlanStatus,
  WorkoutSessionStatus,
} from "@/types/training";

export const EXERCISE_TYPES: ExerciseType[] = [
  "Gym",
  "Calisthenics",
  "Cardio",
  "Other",
];

export const DIFFICULTIES: ExerciseDifficulty[] = [
  "Beginner",
  "Intermediate",
  "Advanced",
];

export function exerciseTypeLabel(value: unknown): string {
  switch (String(value)) {
    case "Gym":
      return "Gimnasio";
    case "Calisthenics":
      return "Calistenia";
    case "Cardio":
      return "Cardio";
    case "Other":
      return "Otro";
    default:
      return String(value);
  }
}

export function difficultyLabel(value: unknown): string {
  switch (String(value)) {
    case "Beginner":
      return "Principiante";
    case "Intermediate":
      return "Intermedio";
    case "Advanced":
      return "Avanzado";
    default:
      return String(value);
  }
}

export function planStatus(value: unknown): WorkoutPlanStatus {
  const text = String(value);
  return text === "Published" || text === "Archived" ? text : "Draft";
}

export function planStatusLabel(status: WorkoutPlanStatus): string {
  switch (status) {
    case "Draft":
      return "Borrador";
    case "Published":
      return "Publicado";
    case "Archived":
      return "Archivado";
  }
}

export function planStatusTone(
  status: WorkoutPlanStatus,
): "neutral" | "good" | "warn" {
  return status === "Published" ? "good" : status === "Draft" ? "warn" : "neutral";
}

export function sessionStatus(value: unknown): WorkoutSessionStatus {
  const text = String(value);
  return text === "Completed" || text === "Cancelled" ? text : "InProgress";
}

export function sessionStatusLabel(status: WorkoutSessionStatus): string {
  switch (status) {
    case "InProgress":
      return "En curso";
    case "Completed":
      return "Completada";
    case "Cancelled":
      return "Cancelada";
  }
}

export function sessionStatusTone(
  status: WorkoutSessionStatus,
): "neutral" | "good" | "warn" {
  return status === "Completed" ? "good" : status === "InProgress" ? "warn" : "neutral";
}

export function formatDateTime(value: string | null | undefined): string {
  if (!value) {
    return "—";
  }
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return value;
  }
  return date.toLocaleString("es", {
    year: "numeric",
    month: "short",
    day: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  });
}

/** Texto vacío → null. */
export function textOrNull(value: string): string | null {
  const trimmed = value.trim();
  return trimmed ? trimmed : null;
}

/** Número decimal opcional ("12,5" → 12.5). Vacío o inválido → null. */
export function numberOrNull(value: string): number | null {
  const trimmed = value.trim().replace(",", ".");
  if (!trimmed) {
    return null;
  }
  const parsed = Number(trimmed);
  return Number.isNaN(parsed) ? null : parsed;
}

/** Entero opcional. Vacío o inválido → null. */
export function intOrNull(value: string): number | null {
  const parsed = numberOrNull(value);
  return parsed === null ? null : Math.trunc(parsed);
}

/** Resumen legible de la prescripción de un ejercicio de plan. */
export function describePrescription(item: {
  targetSets: number;
  targetReps: string | null;
  targetWeightKg: number | null;
  targetDurationSeconds: number | null;
  targetDistanceMeters: number | null;
  restSeconds: number | null;
  tempo: string | null;
}): string {
  const parts: string[] = [];
  parts.push(
    item.targetReps
      ? `${item.targetSets} × ${item.targetReps}`
      : `${item.targetSets} series`,
  );
  if (item.targetWeightKg !== null) {
    parts.push(`${item.targetWeightKg} kg`);
  }
  if (item.targetDurationSeconds !== null) {
    parts.push(`${item.targetDurationSeconds} s`);
  }
  if (item.targetDistanceMeters !== null) {
    parts.push(`${item.targetDistanceMeters} m`);
  }
  if (item.restSeconds !== null) {
    parts.push(`descanso ${item.restSeconds} s`);
  }
  if (item.tempo) {
    parts.push(`tempo ${item.tempo}`);
  }
  return parts.join(" · ");
}
