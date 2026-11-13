import type {
  AcceptInvitationResult,
  AthleteProfile,
  ApiEnvelope,
  CreateTenantInput,
  DirectoryItem,
  InvitationListItem,
  JoinRequestListItem,
  MemberListItem,
  TenantDetail,
  TenantListItem,
  TrainerProfile,
  UpdateAthleteInput,
  UpdateMyProfileInput,
  UpdateOrganizationInput,
  UpdateOrganizationSettingsInput,
} from "@/types/admin";
import type { AuthResponse, AuthUser, TenancyContext } from "@/types/auth";
import type {
  AddMediaInput,
  AddWorkoutExerciseInput,
  Exercise,
  ExerciseCategory,
  ExerciseFilters,
  ExerciseLog,
  ExerciseMedia,
  LogSetInput,
  SaveCategoryInput,
  SaveDayInput,
  SaveExerciseInput,
  SavePlanInput,
  StartSessionInput,
  UpdateWorkoutExerciseInput,
  WorkoutAssignment,
  WorkoutDay,
  WorkoutExercise,
  WorkoutPlanDetail,
  WorkoutPlanSummary,
  WorkoutSession,
} from "@/types/training";

import type {
  Achievement,
  AddActivityInput,
  AttendanceRecord,
  AttendanceStats,
  Challenge,
  ChallengeActivity,
  ChatContact,
  ChatMessage,
  Conversation,
  CreateChallengeInput,
  CreateConversationInput,
  CreateFoodInput,
  FoodEntry,
  LeaderboardEntry,
  NotificationList,
  NutritionSummary,
  NutritionTarget,
  SaveNutritionTargetInput,
  UpsertAttendanceInput,
  WaterLog,
} from "@/types/advanced";

import type {
  BodyMeasurement,
  CreateGoalInput,
  FitnessAssessment,
  ProgressCharts,
  ProgressGoal,
  ProgressPhoto,
  SaveAssessmentInput,
  SaveMeasurementInput,
  SavePhotoInput,
} from "@/types/progress";

export class ApiError extends Error {
  constructor(
    message: string,
    public status: number,
    public detail?: string,
  ) {
    super(message);
    this.name = "ApiError";
  }
}

async function readProblem(response: Response): Promise<ApiError> {
  try {
    const body = (await response.json()) as {
      title?: string;
      detail?: string;
    };
    return new ApiError(
      body.title ?? "Error de la API",
      response.status,
      body.detail,
    );
  } catch {
    return new ApiError("Error de la API", response.status);
  }
}

async function getCsrfToken(): Promise<string> {
  const response = await fetch("/api/v1/auth/csrf", {
    credentials: "include",
    cache: "no-store",
  });
  if (!response.ok) {
    throw await readProblem(response);
  }
  const body = (await response.json()) as { token: string };
  return body.token;
}

async function apiFetch<T>(
  path: string,
  init: RequestInit & { csrf?: boolean } = {},
): Promise<T> {
  const headers = new Headers(init.headers);
  if (init.body && !headers.has("Content-Type")) {
    headers.set("Content-Type", "application/json");
  }
  if (init.csrf) {
    headers.set("X-XSRF-TOKEN", await getCsrfToken());
  }

  const response = await fetch(path, {
    ...init,
    headers,
    credentials: "include",
    cache: "no-store",
  });

  if (!response.ok) {
    throw await readProblem(response);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

export const authApi = {
  async me(): Promise<AuthUser | null> {
    try {
      const body = await apiFetch<AuthResponse>("/api/v1/auth/me");
      return body.user ?? null;
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) {
        return null;
      }
      throw error;
    }
  },

  async login(email: string, password: string, rememberMe = false) {
    return apiFetch<AuthResponse>("/api/v1/auth/login", {
      method: "POST",
      csrf: true,
      body: JSON.stringify({ email, password, rememberMe }),
    });
  },

  async logout() {
    return apiFetch<{ message: string }>("/api/v1/auth/logout", {
      method: "POST",
      csrf: true,
      body: JSON.stringify({}),
    });
  },

  async register(input: {
    email: string;
    password: string;
    firstName: string;
    lastName: string;
  }) {
    return apiFetch<AuthResponse>("/api/v1/auth/register", {
      method: "POST",
      csrf: true,
      body: JSON.stringify(input),
    });
  },
};

export const tenancyApi = {
  context(slug?: string) {
    const path = slug
      ? `/api/v1/t/${encodeURIComponent(slug)}/context`
      : "/api/v1/tenancy/context";
    return apiFetch<TenancyContext>(path);
  },
};

// ---------------------------------------------------------------------------
// Fase 2: administración (respuestas `{ value, message }`)
// ---------------------------------------------------------------------------

async function unwrap<T>(request: Promise<ApiEnvelope<T>>): Promise<T> {
  const body = await request;
  return body.value;
}

/** Acciones sin `value`: devuelve el mensaje del backend. */
async function action(
  path: string,
  method: "POST" | "PUT" | "DELETE",
  body?: unknown,
): Promise<string> {
  const result = await apiFetch<{ message?: string | null }>(path, {
    method,
    csrf: true,
    body: JSON.stringify(body ?? {}),
  });
  return result?.message ?? "";
}

function send<T>(path: string, method: "POST" | "PUT", body?: unknown) {
  return unwrap(
    apiFetch<ApiEnvelope<T>>(path, {
      method,
      csrf: true,
      body: JSON.stringify(body ?? {}),
    }),
  );
}

function get<T>(path: string) {
  return unwrap(apiFetch<ApiEnvelope<T>>(path));
}

function tenantPath(slug: string, suffix: string) {
  return `/api/v1/t/${encodeURIComponent(slug)}${suffix}`;
}

export const adminApi = {
  // Plataforma (SuperAdmin)
  listTenants: () => get<TenantListItem[]>("/api/v1/platform/tenants"),
  createTenant: (input: CreateTenantInput) =>
    send<TenantDetail>("/api/v1/platform/tenants", "POST", input),
  activateTenant: (id: string) =>
    action(`/api/v1/platform/tenants/${id}/activate`, "POST"),
  suspendTenant: (id: string) =>
    action(`/api/v1/platform/tenants/${id}/suspend`, "POST"),

  // Organización
  getOrganization: (slug: string) =>
    get<TenantDetail>(tenantPath(slug, "/organization")),
  updateOrganization: (slug: string, input: UpdateOrganizationInput) =>
    send<TenantDetail>(tenantPath(slug, "/organization"), "PUT", input),
  updateOrganizationSettings: (
    slug: string,
    input: UpdateOrganizationSettingsInput,
  ) =>
    send<TenantDetail>(tenantPath(slug, "/organization/settings"), "PUT", input),

  // Miembros
  listMembers: (slug: string, role?: "Trainer" | "Athlete") =>
    get<MemberListItem[]>(
      tenantPath(slug, `/members${role ? `?role=${role}` : ""}`),
    ),
  changeMemberRole: (slug: string, membershipId: string, role: number) =>
    action(tenantPath(slug, `/members/${membershipId}/role`), "POST", { role }),
  suspendMember: (slug: string, membershipId: string) =>
    action(tenantPath(slug, `/members/${membershipId}/suspend`), "POST"),
  reactivateMember: (slug: string, membershipId: string) =>
    action(tenantPath(slug, `/members/${membershipId}/reactivate`), "POST"),
  removeMember: (slug: string, membershipId: string) =>
    action(tenantPath(slug, `/members/${membershipId}/remove`), "POST"),

  // Invitaciones
  listInvitations: (slug: string) =>
    get<InvitationListItem[]>(tenantPath(slug, "/invitations")),
  createInvitation: (slug: string, email: string, role: number) =>
    send<InvitationListItem>(tenantPath(slug, "/invitations"), "POST", {
      email,
      role,
    }),
  cancelInvitation: (slug: string, id: string) =>
    action(tenantPath(slug, `/invitations/${id}/cancel`), "POST"),
  acceptInvitation: (token: string) =>
    send<AcceptInvitationResult>("/api/v1/invitations/accept", "POST", {
      token,
    }),

  // Solicitudes de ingreso
  listJoinRequests: (slug: string) =>
    get<JoinRequestListItem[]>(tenantPath(slug, "/join-requests")),
  approveJoinRequest: (slug: string, id: string) =>
    action(tenantPath(slug, `/join-requests/${id}/approve`), "POST"),
  rejectJoinRequest: (slug: string, id: string, reason: string) =>
    action(tenantPath(slug, `/join-requests/${id}/reject`), "POST", { reason }),
  listDirectory: () => get<DirectoryItem[]>("/api/v1/organizations"),
  submitJoinRequest: (slug: string) =>
    action(
      `/api/v1/organizations/${encodeURIComponent(slug)}/join-requests`,
      "POST",
    ),

  // Alumnos y entrenadores
  listAthletes: (slug: string) =>
    get<AthleteProfile[]>(tenantPath(slug, "/athletes")),
  updateAthlete: (slug: string, id: string, input: UpdateAthleteInput) =>
    action(tenantPath(slug, `/athletes/${id}`), "PUT", input),
  assignAthlete: (
    slug: string,
    trainerProfileId: string,
    athleteProfileId: string,
  ) =>
    action(tenantPath(slug, "/athletes/assign"), "POST", {
      trainerProfileId,
      athleteProfileId,
    }),
  listTrainers: (slug: string) =>
    get<TrainerProfile[]>(tenantPath(slug, "/trainers")),

  // Perfil propio
  updateMyProfile: (input: UpdateMyProfileInput) =>
    action("/api/v1/profile/me", "PUT", input),
};

// ---------------------------------------------------------------------------
// Fase 3: entrenamiento (ejercicios, planes y sesiones)
// ---------------------------------------------------------------------------

export const trainingApi = {
  // Ejercicios
  listCategories: (slug: string) =>
    get<ExerciseCategory[]>(tenantPath(slug, "/exercises/categories")),
  createCategory: (slug: string, input: SaveCategoryInput) =>
    send<ExerciseCategory>(tenantPath(slug, "/exercises/categories"), "POST", input),
  updateCategory: (slug: string, id: string, input: SaveCategoryInput) =>
    send<ExerciseCategory>(
      tenantPath(slug, `/exercises/categories/${id}`),
      "PUT",
      input,
    ),
  listExercises: (slug: string, filters: ExerciseFilters = {}) => {
    const params = new URLSearchParams();
    if (filters.q?.trim()) {
      params.set("q", filters.q.trim());
    }
    if (filters.type) {
      params.set("type", filters.type);
    }
    if (filters.muscle?.trim()) {
      params.set("muscle", filters.muscle.trim());
    }
    const query = params.toString();
    return get<Exercise[]>(
      tenantPath(slug, `/exercises${query ? `?${query}` : ""}`),
    );
  },
  createExercise: (slug: string, input: SaveExerciseInput) =>
    send<Exercise>(tenantPath(slug, "/exercises"), "POST", input),
  updateExercise: (slug: string, id: string, input: SaveExerciseInput) =>
    send<Exercise>(tenantPath(slug, `/exercises/${id}`), "PUT", input),
  addExerciseMedia: (slug: string, id: string, input: AddMediaInput) =>
    send<ExerciseMedia>(tenantPath(slug, `/exercises/${id}/media`), "POST", input),

  // Planes
  listPlans: (slug: string) =>
    get<WorkoutPlanSummary[]>(tenantPath(slug, "/workouts")),
  getPlan: (slug: string, planId: string) =>
    get<WorkoutPlanDetail>(tenantPath(slug, `/workouts/${planId}`)),
  createPlan: (slug: string, input: SavePlanInput) =>
    send<WorkoutPlanDetail>(tenantPath(slug, "/workouts"), "POST", input),
  updatePlan: (slug: string, planId: string, input: SavePlanInput) =>
    send<WorkoutPlanDetail>(tenantPath(slug, `/workouts/${planId}`), "PUT", input),
  publishPlan: (slug: string, planId: string) =>
    action(tenantPath(slug, `/workouts/${planId}/publish`), "POST"),
  archivePlan: (slug: string, planId: string) =>
    action(tenantPath(slug, `/workouts/${planId}/archive`), "POST"),
  newPlanVersion: (slug: string, planId: string) =>
    send<WorkoutPlanDetail>(
      tenantPath(slug, `/workouts/${planId}/new-version`),
      "POST",
    ),
  addDay: (slug: string, planId: string, input: SaveDayInput) =>
    send<WorkoutDay>(tenantPath(slug, `/workouts/${planId}/days`), "POST", input),
  updateDay: (slug: string, dayId: string, input: SaveDayInput) =>
    send<WorkoutDay>(tenantPath(slug, `/workouts/days/${dayId}`), "PUT", input),
  addWorkoutExercise: (
    slug: string,
    dayId: string,
    input: AddWorkoutExerciseInput,
  ) =>
    send<WorkoutExercise>(
      tenantPath(slug, `/workouts/days/${dayId}/exercises`),
      "POST",
      input,
    ),
  updateWorkoutExercise: (
    slug: string,
    id: string,
    input: UpdateWorkoutExerciseInput,
  ) =>
    send<WorkoutExercise>(
      tenantPath(slug, `/workouts/exercises/${id}`),
      "PUT",
      input,
    ),
  assignPlan: (slug: string, planId: string, athleteProfileId: string) =>
    send<WorkoutAssignment>(tenantPath(slug, `/workouts/${planId}/assign`), "POST", {
      athleteProfileId,
    }),
  endAssignment: (slug: string, id: string) =>
    action(tenantPath(slug, `/workouts/assignments/${id}/end`), "POST"),

  // Sesiones
  listSessions: (slug: string) =>
    get<WorkoutSession[]>(tenantPath(slug, "/sessions")),
  startSession: (slug: string, input: StartSessionInput) =>
    send<WorkoutSession>(tenantPath(slug, "/sessions/start"), "POST", input),
  completeSession: (slug: string, id: string, notes?: string | null) =>
    action(tenantPath(slug, `/sessions/${id}/complete`), "POST", {
      notes: notes ?? null,
    }),
  cancelSession: (slug: string, id: string) =>
    action(tenantPath(slug, `/sessions/${id}/cancel`), "POST"),
  listSessionLogs: (slug: string, id: string) =>
    get<ExerciseLog[]>(tenantPath(slug, `/sessions/${id}/logs`)),
  logSet: (slug: string, id: string, input: LogSetInput) =>
    send<ExerciseLog>(tenantPath(slug, `/sessions/${id}/logs`), "POST", input),
};

// ---------------------------------------------------------------------------
// Fase 4: seguimiento del progreso
// ---------------------------------------------------------------------------

function progressQuery(
  athleteProfileId?: string | null,
  extra: Record<string, string | null | undefined> = {},
) {
  const params = new URLSearchParams();
  if (athleteProfileId) {
    params.set("athleteProfileId", athleteProfileId);
  }
  for (const [key, value] of Object.entries(extra)) {
    if (value) {
      params.set(key, value);
    }
  }
  const query = params.toString();
  return query ? `?${query}` : "";
}

export const progressApi = {
  // Mediciones
  listMeasurements: (slug: string, athleteProfileId?: string | null) =>
    get<BodyMeasurement[]>(
      tenantPath(slug, `/progress/measurements${progressQuery(athleteProfileId)}`),
    ),
  createMeasurement: (slug: string, input: SaveMeasurementInput) =>
    send<BodyMeasurement>(tenantPath(slug, "/progress/measurements"), "POST", input),
  updateMeasurement: (slug: string, id: string, input: SaveMeasurementInput) =>
    send<BodyMeasurement>(
      tenantPath(slug, `/progress/measurements/${id}`),
      "PUT",
      input,
    ),

  // Fotos
  listPhotos: (slug: string, athleteProfileId?: string | null) =>
    get<ProgressPhoto[]>(
      tenantPath(slug, `/progress/photos${progressQuery(athleteProfileId)}`),
    ),
  createPhoto: (slug: string, input: SavePhotoInput) =>
    send<ProgressPhoto>(tenantPath(slug, "/progress/photos"), "POST", input),
  updatePhoto: (slug: string, id: string, input: SavePhotoInput) =>
    send<ProgressPhoto>(tenantPath(slug, `/progress/photos/${id}`), "PUT", input),

  // Valoraciones
  listAssessments: (slug: string, athleteProfileId?: string | null) =>
    get<FitnessAssessment[]>(
      tenantPath(slug, `/progress/assessments${progressQuery(athleteProfileId)}`),
    ),
  createAssessment: (slug: string, input: SaveAssessmentInput) =>
    send<FitnessAssessment>(tenantPath(slug, "/progress/assessments"), "POST", input),
  updateAssessment: (slug: string, id: string, input: SaveAssessmentInput) =>
    send<FitnessAssessment>(
      tenantPath(slug, `/progress/assessments/${id}`),
      "PUT",
      input,
    ),

  // Objetivos
  listGoals: (slug: string, athleteProfileId?: string | null) =>
    get<ProgressGoal[]>(
      tenantPath(slug, `/progress/goals${progressQuery(athleteProfileId)}`),
    ),
  createGoal: (slug: string, input: CreateGoalInput) =>
    send<ProgressGoal>(tenantPath(slug, "/progress/goals"), "POST", input),
  completeGoal: (slug: string, id: string) =>
    action(tenantPath(slug, `/progress/goals/${id}/complete`), "POST"),
  cancelGoal: (slug: string, id: string) =>
    action(tenantPath(slug, `/progress/goals/${id}/cancel`), "POST"),

  // Gráficos
  charts: (
    slug: string,
    athleteProfileId?: string | null,
    from?: string | null,
    to?: string | null,
  ) =>
    get<ProgressCharts>(
      tenantPath(
        slug,
        `/progress/charts${progressQuery(athleteProfileId, { from, to })}`,
      ),
    ),
};

// ---------------------------------------------------------------------------
// Fase 5: nutrición, asistencia, retos, chat y notificaciones
// ---------------------------------------------------------------------------

export const nutritionApi = {
  listTargets: (slug: string, athleteProfileId?: string | null) =>
    get<NutritionTarget[]>(
      tenantPath(slug, `/nutrition/targets${progressQuery(athleteProfileId)}`),
    ),
  createTarget: (slug: string, input: SaveNutritionTargetInput) =>
    send<NutritionTarget>(tenantPath(slug, "/nutrition/targets"), "POST", input),
  updateTarget: (slug: string, id: string, input: SaveNutritionTargetInput) =>
    send<NutritionTarget>(tenantPath(slug, `/nutrition/targets/${id}`), "PUT", input),
  deleteTarget: (slug: string, id: string) =>
    action(tenantPath(slug, `/nutrition/targets/${id}`), "DELETE"),

  listFoods: (slug: string, athleteProfileId?: string | null, date?: string | null) =>
    get<FoodEntry[]>(
      tenantPath(slug, `/nutrition/foods${progressQuery(athleteProfileId, { date })}`),
    ),
  createFood: (slug: string, input: CreateFoodInput) =>
    send<FoodEntry>(tenantPath(slug, "/nutrition/foods"), "POST", input),
  deleteFood: (slug: string, id: string) =>
    action(tenantPath(slug, `/nutrition/foods/${id}`), "DELETE"),

  listWater: (slug: string, athleteProfileId?: string | null, date?: string | null) =>
    get<WaterLog[]>(
      tenantPath(slug, `/nutrition/water${progressQuery(athleteProfileId, { date })}`),
    ),
  createWater: (
    slug: string,
    input: { athleteProfileId?: string | null; entryDate?: string | null; amountMl: number },
  ) => send<WaterLog>(tenantPath(slug, "/nutrition/water"), "POST", input),
  deleteWater: (slug: string, id: string) =>
    action(tenantPath(slug, `/nutrition/water/${id}`), "DELETE"),

  summary: (slug: string, athleteProfileId?: string | null, date?: string | null) =>
    get<NutritionSummary>(
      tenantPath(slug, `/nutrition/summary${progressQuery(athleteProfileId, { date })}`),
    ),
};

export const attendanceApi = {
  list: (
    slug: string,
    athleteProfileId?: string | null,
    from?: string | null,
    to?: string | null,
  ) =>
    get<AttendanceRecord[]>(
      tenantPath(slug, `/attendance${progressQuery(athleteProfileId, { from, to })}`),
    ),
  upsert: (slug: string, input: UpsertAttendanceInput) =>
    send<AttendanceRecord>(tenantPath(slug, "/attendance"), "POST", input),
  stats: (
    slug: string,
    athleteProfileId?: string | null,
    from?: string | null,
    to?: string | null,
  ) =>
    get<AttendanceStats>(
      tenantPath(slug, `/attendance/stats${progressQuery(athleteProfileId, { from, to })}`),
    ),
};

export const challengesApi = {
  list: (slug: string) => get<Challenge[]>(tenantPath(slug, "/challenges")),
  create: (slug: string, input: CreateChallengeInput) =>
    send<Challenge>(tenantPath(slug, "/challenges"), "POST", input),
  activate: (slug: string, id: string) =>
    action(tenantPath(slug, `/challenges/${id}/activate`), "POST"),
  complete: (slug: string, id: string) =>
    action(tenantPath(slug, `/challenges/${id}/complete`), "POST"),
  cancel: (slug: string, id: string) =>
    action(tenantPath(slug, `/challenges/${id}/cancel`), "POST"),
  join: (slug: string, id: string, athleteProfileId?: string | null) =>
    action(tenantPath(slug, `/challenges/${id}/join`), "POST", {
      athleteProfileId: athleteProfileId ?? null,
    }),
  leaderboard: (slug: string, id: string) =>
    get<LeaderboardEntry[]>(tenantPath(slug, `/challenges/${id}/leaderboard`)),
  listActivities: (slug: string, id: string) =>
    get<ChallengeActivity[]>(tenantPath(slug, `/challenges/${id}/activities`)),
  addActivity: (slug: string, id: string, input: AddActivityInput) =>
    send<ChallengeActivity>(tenantPath(slug, `/challenges/${id}/activities`), "POST", input),
  listAchievements: (slug: string, athleteProfileId?: string | null) =>
    get<Achievement[]>(
      tenantPath(slug, `/achievements${progressQuery(athleteProfileId)}`),
    ),
};

export const chatApi = {
  contacts: (slug: string) => get<ChatContact[]>(tenantPath(slug, "/chat/contacts")),
  conversations: (slug: string) =>
    get<Conversation[]>(tenantPath(slug, "/chat/conversations")),
  createConversation: (slug: string, input: CreateConversationInput) =>
    send<Conversation>(tenantPath(slug, "/chat/conversations"), "POST", input),
  messages: (slug: string, conversationId: string, limit = 100) =>
    get<ChatMessage[]>(
      tenantPath(slug, `/chat/conversations/${conversationId}/messages?limit=${limit}`),
    ),
  sendMessage: (slug: string, conversationId: string, body: string) =>
    send<ChatMessage>(
      tenantPath(slug, `/chat/conversations/${conversationId}/messages`),
      "POST",
      { body, attachmentUrl: null },
    ),
  markRead: (slug: string, conversationId: string) =>
    action(tenantPath(slug, `/chat/conversations/${conversationId}/read`), "POST"),
};

export const notificationsApi = {
  list: (slug: string, unreadOnly = false) =>
    get<NotificationList>(
      tenantPath(slug, `/notifications${unreadOnly ? "?unreadOnly=true" : ""}`),
    ),
  markRead: (slug: string, id: string) =>
    action(tenantPath(slug, `/notifications/${id}/read`), "POST"),
  markAllRead: (slug: string) =>
    action(tenantPath(slug, "/notifications/read-all"), "POST"),
};