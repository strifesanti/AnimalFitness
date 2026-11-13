import type { EnumWire } from "@/types/admin";
import type { MembershipRole, MembershipStatus, TenantStatus } from "@/types/auth";
import { ApiError } from "@/services/api";

const ROLE_BY_NUMBER: Record<number, MembershipRole> = {
  1: "Owner",
  2: "Trainer",
  3: "Athlete",
};

const MEMBERSHIP_STATUS_BY_NUMBER: Record<number, MembershipStatus> = {
  1: "Active",
  2: "Suspended",
  3: "Removed",
};

const TENANT_STATUS_BY_NUMBER: Record<number, TenantStatus> = {
  0: "Trial",
  1: "Active",
  2: "Suspended",
};

export const ROLE_TO_NUMBER: Record<MembershipRole, number> = {
  Owner: 1,
  Trainer: 2,
  Athlete: 3,
};

function normalize<T extends string>(
  value: EnumWire,
  byNumber: Record<number, T>,
  fallback: T,
): T {
  if (typeof value === "number") {
    return byNumber[value] ?? fallback;
  }
  const asNumber = Number(value);
  if (value.trim() !== "" && !Number.isNaN(asNumber)) {
    return byNumber[asNumber] ?? fallback;
  }
  return (value as T) || fallback;
}

export function normalizeRole(value: EnumWire): MembershipRole {
  return normalize(value, ROLE_BY_NUMBER, "Athlete");
}

export function normalizeMembershipStatus(value: EnumWire): MembershipStatus {
  return normalize(value, MEMBERSHIP_STATUS_BY_NUMBER, "Active");
}

export function normalizeTenantStatus(value: EnumWire): TenantStatus {
  return normalize(value, TENANT_STATUS_BY_NUMBER, "Trial");
}

export function membershipStatusLabel(status: MembershipStatus): string {
  switch (status) {
    case "Active":
      return "Activo";
    case "Suspended":
      return "Suspendido";
    case "Removed":
      return "Eliminado";
  }
}

export function tenantStatusLabel(status: TenantStatus): string {
  switch (status) {
    case "Trial":
      return "Prueba";
    case "Active":
      return "Activa";
    case "Suspended":
      return "Suspendida";
  }
}

export function requestStatusLabel(status: string): string {
  switch (status) {
    case "Pending":
      return "Pendiente";
    case "Approved":
      return "Aprobada";
    case "Rejected":
      return "Rechazada";
    case "Cancelled":
      return "Cancelada";
    case "Accepted":
      return "Aceptada";
    case "Expired":
      return "Expirada";
    case "Revoked":
      return "Revocada";
    default:
      return status;
  }
}

export function errorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    return error.detail ? `${error.message}: ${error.detail}` : error.message;
  }
  return "No se pudo completar la operación. ¿Está la API en marcha?";
}

export function formatDate(value: string | null | undefined): string {
  if (!value) {
    return "—";
  }
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return value;
  }
  return date.toLocaleDateString("es", {
    year: "numeric",
    month: "short",
    day: "numeric",
  });
}

export function fullName(person: { firstName: string; lastName: string }): string {
  return `${person.firstName} ${person.lastName}`.trim();
}

export const selectClassName =
  "flex h-11 w-full rounded-md border border-[var(--border)] bg-[var(--surface)] px-3 text-sm text-[var(--foreground)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--accent)] disabled:cursor-not-allowed disabled:opacity-50";

export const textareaClassName =
  "flex min-h-24 w-full rounded-md border border-[var(--border)] bg-[var(--surface)] px-3 py-2 text-sm text-[var(--foreground)] placeholder:text-[var(--muted)] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[var(--accent)] disabled:cursor-not-allowed disabled:opacity-50";
