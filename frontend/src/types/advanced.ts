/** Tipos de la Fase 5: nutrición, asistencia, retos, chat y notificaciones. */

// ----- Nutrición -----

export type MealType = "Breakfast" | "Lunch" | "Dinner" | "Snack" | "Other";

export type NutritionTarget = {
  id: string;
  athleteProfileId: string;
  calories: number;
  proteinGrams: number;
  carbsGrams: number;
  fatGrams: number;
  waterMl: number;
  effectiveFrom: string;
};

export type SaveNutritionTargetInput = {
  athleteProfileId?: string | null;
  calories: number;
  proteinGrams: number;
  carbsGrams: number;
  fatGrams: number;
  waterMl: number;
  effectiveFrom?: string | null;
};

export type FoodEntry = {
  id: string;
  athleteProfileId: string;
  entryDate: string;
  mealType: MealType;
  name: string;
  calories: number;
  proteinGrams: number;
  carbsGrams: number;
  fatGrams: number;
  notes: string | null;
};

export type CreateFoodInput = {
  athleteProfileId?: string | null;
  entryDate?: string | null;
  mealType: MealType;
  name: string;
  calories: number;
  proteinGrams: number;
  carbsGrams: number;
  fatGrams: number;
  notes?: string | null;
};

export type WaterLog = {
  id: string;
  athleteProfileId: string;
  entryDate: string;
  amountMl: number;
  loggedAt: string;
};

export type NutritionTotals = {
  calories: number;
  proteinGrams: number;
  carbsGrams: number;
  fatGrams: number;
  waterMl: number;
};

export type NutritionPercentages = {
  calories: number;
  protein: number;
  carbs: number;
  fat: number;
  water: number;
};

export type NutritionSummary = {
  athleteProfileId: string | null;
  date: string;
  target: NutritionTarget | null;
  consumed: NutritionTotals;
  remaining: NutritionTotals | null;
  percentages: NutritionPercentages | null;
  foods: FoodEntry[];
  water: WaterLog[];
};

// ----- Asistencia -----

export type AttendanceStatus = "Present" | "Absent" | "Excused" | "Late";

export type AttendanceRecord = {
  id: string;
  athleteProfileId: string;
  athleteName: string;
  date: string;
  status: AttendanceStatus;
  notes: string | null;
  recordedByUserId: string;
};

export type UpsertAttendanceInput = {
  athleteProfileId: string;
  date: string;
  status: AttendanceStatus;
  notes?: string | null;
};

export type AttendanceStats = {
  athleteProfileId: string | null;
  from: string;
  to: string;
  total: number;
  present: number;
  late: number;
  absent: number;
  excused: number;
  presentPercent: number;
  attendedPercent: number;
};

// ----- Retos -----

export type ChallengeKind = "Individual" | "Group";
export type ChallengeMetric = "WorkoutsCompleted" | "AttendanceDays" | "ManualPoints";
export type ChallengeStatus = "Draft" | "Active" | "Completed" | "Cancelled";

export type Challenge = {
  id: string;
  name: string;
  description: string | null;
  kind: ChallengeKind;
  metric: ChallengeMetric;
  startsAt: string;
  endsAt: string;
  targetValue: number | null;
  status: ChallengeStatus;
  participantCount: number;
  joined: boolean;
};

export type CreateChallengeInput = {
  name: string;
  description?: string | null;
  kind: ChallengeKind;
  metric: ChallengeMetric;
  startsAt: string;
  endsAt: string;
  targetValue?: number | null;
};

export type LeaderboardEntry = {
  rank: number;
  participantId: string;
  athleteProfileId: string;
  athleteName: string;
  points: number;
};

export type AddActivityInput = {
  athleteProfileId: string;
  points: number;
  note?: string | null;
};

export type ChallengeActivity = {
  id: string;
  challengeId: string;
  participantId: string;
  athleteProfileId: string;
  athleteName: string;
  points: number;
  note: string | null;
  occurredAt: string;
};

export type Achievement = {
  id: string;
  athleteProfileId: string;
  athleteName: string;
  challengeId: string | null;
  title: string;
  description: string | null;
  awardedAt: string;
};

// ----- Chat y notificaciones -----

export type ConversationKind = "Direct" | "Group";

export type ChatContact = { userId: string; name: string; role: string };

export type ChatParticipant = { userId: string; name: string };

export type Conversation = {
  id: string;
  kind: ConversationKind;
  title: string;
  lastMessageAt: string | null;
  lastMessagePreview: string | null;
  unreadCount: number;
  participants: ChatParticipant[];
};

export type CreateConversationInput = {
  kind: ConversationKind;
  title?: string | null;
  participantUserIds: string[];
};

export type ChatMessage = {
  id: string;
  conversationId: string;
  senderUserId: string;
  senderName: string;
  body: string;
  attachmentUrl: string | null;
  sentAt: string;
};

export type NotificationKind =
  | "General"
  | "ChatMessage"
  | "Challenge"
  | "Membership"
  | "Workout"
  | "Reminder";

export type AppNotification = {
  id: string;
  kind: NotificationKind;
  title: string;
  body: string;
  linkPath: string | null;
  isRead: boolean;
  raisedAt: string;
};

export type NotificationList = {
  unreadCount: number;
  items: AppNotification[];
};
