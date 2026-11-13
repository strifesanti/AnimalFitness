export type MembershipRole = "Owner" | "Trainer" | "Athlete";
export type MembershipStatus = "Active" | "Suspended" | "Removed";
export type TenantStatus = "Trial" | "Active" | "Suspended";

export type MembershipSummary = {
  tenantId: string;
  slug: string;
  tenantName: string;
  tenantStatus: TenantStatus;
  role: MembershipRole;
  membershipStatus: MembershipStatus;
};

export type AuthUser = {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  phoneNumber: string | null;
  emailConfirmed: boolean;
  isSuperAdmin: boolean;
  memberships: MembershipSummary[];
  activeTenantId: string | null;
  activeRole: MembershipRole | null;
};

export type AuthResponse = {
  user?: AuthUser;
  message?: string;
};

export type TenancyContext = {
  tenantId: string | null;
  userId: string | null;
  role: string | null;
  hasTenant: boolean;
};
