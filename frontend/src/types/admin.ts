import type { MembershipRole, MembershipStatus, TenantStatus } from "@/types/auth";

/**
 * El backend puede serializar los enums como número (System.Text.Json por defecto)
 * o como texto. Los DTOs de lectura se tipan con `EnumWire` y se normalizan en
 * `@/lib/admin`.
 */
export type EnumWire = string | number;

export type ApiEnvelope<T> = { value: T; message?: string | null };

export type TenantListItem = {
  id: string;
  name: string;
  slug: string;
  status: EnumWire;
  contactEmail: string;
  phone: string | null;
  createdAt: string;
};

export type TenantDetail = {
  id: string;
  name: string;
  slug: string;
  status: EnumWire;
  contactEmail: string;
  phone: string | null;
  timeZoneId: string;
  currency: string;
  logoFileId: string | null;
  primaryColor: string;
  locale: string;
  settingsJson: string;
};

export type CreateTenantInput = {
  name: string;
  slug: string;
  contactEmail: string;
  ownerEmail: string;
  phone?: string | null;
  timeZoneId?: string | null;
  currency?: string | null;
};

export type UpdateOrganizationInput = {
  name: string;
  contactEmail: string;
  phone: string | null;
  timeZoneId: string;
  currency: string;
};

export type UpdateOrganizationSettingsInput = {
  primaryColor: string | null;
  locale: string | null;
  currency: string | null;
  settingsJson: string | null;
};

export type MemberListItem = {
  membershipId: string;
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  role: EnumWire;
  status: EnumWire;
  joinedAt: string;
  athleteProfileId: string | null;
  trainerProfileId: string | null;
};

export type InvitationListItem = {
  id: string;
  email: string;
  role: EnumWire;
  status: string;
  expiresAt: string;
  createdAt: string;
};

export type JoinRequestListItem = {
  id: string;
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  status: string;
  createdAt: string;
  rejectionReason: string | null;
};

export type AthleteProfile = {
  id: string;
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  heightCm: number | null;
  weightKg: number | null;
  birthDate: string | null;
  notes: string | null;
  assignedTrainerProfileId: string | null;
};

export type UpdateAthleteInput = {
  heightCm: number | null;
  weightKg: number | null;
  birthDate: string | null;
  notes: string | null;
};

export type TrainerProfile = {
  id: string;
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  bio: string | null;
  specialties: string | null;
  activeAthleteCount: number;
};

export type DirectoryItem = {
  name: string;
  slug: string;
  status: EnumWire;
};

export type UpdateMyProfileInput = {
  firstName: string;
  lastName: string;
  phoneNumber: string | null;
};

export type AcceptInvitationResult = {
  tenantId: string;
  slug: string;
  role: EnumWire;
};

export type { MembershipRole, MembershipStatus, TenantStatus };
