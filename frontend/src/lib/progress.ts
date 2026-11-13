import type { GoalStatus, PhotoPose } from "@/types/progress";

export const PHOTO_POSES: PhotoPose[] = ["Front", "Side", "Back", "Other"];

export function photoPose(value: unknown): PhotoPose {
  const text = String(value);
  return text === "Side" || text === "Back" || text === "Other" ? text : "Front";
}

export function poseLabel(value: unknown): string {
  switch (photoPose(value)) {
    case "Front":
      return "Frente";
    case "Side":
      return "Perfil";
    case "Back":
      return "Espalda";
    case "Other":
      return "Otro";
  }
}

export function goalStatus(value: unknown): GoalStatus {
  const text = String(value);
  return text === "Completed" || text === "Cancelled" ? text : "Active";
}

export function goalStatusLabel(status: GoalStatus): string {
  switch (status) {
    case "Active":
      return "Activo";
    case "Completed":
      return "Cumplido";
    case "Cancelled":
      return "Cancelado";
  }
}

export function goalStatusTone(status: GoalStatus): "neutral" | "good" | "warn" {
  return status === "Completed" ? "good" : status === "Active" ? "warn" : "neutral";
}

/** Valor numérico para mostrar (hasta 2 decimales) o guion. */
export function formatValue(value: number | null | undefined, unit = ""): string {
  if (value === null || value === undefined) {
    return "—";
  }
  const text = Number(value.toFixed(2)).toLocaleString("es");
  return unit ? `${text} ${unit}` : text;
}

/** `yyyy-MM-dd` de hoy en hora local, para inputs de fecha. */
export function todayInputValue(): string {
  const now = new Date();
  const month = String(now.getMonth() + 1).padStart(2, "0");
  const day = String(now.getDate()).padStart(2, "0");
  return `${now.getFullYear()}-${month}-${day}`;
}

/** Convierte `yyyy-MM-dd` a un instante ISO (mediodía local) o null si vacío. */
export function dateInputToIso(value: string): string | null {
  if (!value) {
    return null;
  }
  const date = new Date(`${value}T12:00:00`);
  return Number.isNaN(date.getTime()) ? null : date.toISOString();
}
