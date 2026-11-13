/** Tipos de la Fase 4: seguimiento del progreso. */

export type PhotoPose = "Front" | "Side" | "Back" | "Other";
export type GoalStatus = "Active" | "Completed" | "Cancelled";

export type BodyMeasurement = {
  id: string;
  athleteProfileId: string;
  measuredAt: string;
  weightKg: number | null;
  heightCm: number | null;
  bodyFatPercent: number | null;
  muscleMassKg: number | null;
  waistCm: number | null;
  chestCm: number | null;
  armCm: number | null;
  legCm: number | null;
  hipCm: number | null;
  bmi: number | null;
  notes: string | null;
  recordedByUserId: string;
};

export type SaveMeasurementInput = {
  athleteProfileId?: string | null;
  measuredAt?: string | null;
  weightKg: number | null;
  heightCm: number | null;
  bodyFatPercent: number | null;
  muscleMassKg: number | null;
  waistCm: number | null;
  chestCm: number | null;
  armCm: number | null;
  legCm: number | null;
  hipCm: number | null;
  notes: string | null;
};

export type ProgressPhoto = {
  id: string;
  athleteProfileId: string;
  takenAt: string;
  pose: PhotoPose | string | number;
  url: string;
  caption: string | null;
  notes: string | null;
  recordedByUserId: string;
};

export type SavePhotoInput = {
  athleteProfileId?: string | null;
  takenAt?: string | null;
  pose: PhotoPose;
  url: string;
  caption: string | null;
  notes: string | null;
};

export type FitnessAssessment = {
  id: string;
  athleteProfileId: string;
  assessedAt: string;
  title: string;
  summary: string;
  score: number | null;
  assessedByUserId: string;
};

export type SaveAssessmentInput = {
  athleteProfileId?: string | null;
  assessedAt?: string | null;
  title: string;
  summary: string;
  score: number | null;
};

export type ProgressGoal = {
  id: string;
  athleteProfileId: string;
  title: string;
  description: string | null;
  targetValue: number | null;
  unit: string | null;
  dueDate: string | null;
  status: GoalStatus | string | number;
};

export type CreateGoalInput = {
  athleteProfileId?: string | null;
  title: string;
  description: string | null;
  targetValue: number | null;
  unit: string | null;
  dueDate: string | null;
};

export type ChartPoint = { at: string; value: number };

export type ProgressCharts = {
  athleteProfileId: string;
  weightKg: ChartPoint[];
  bodyFatPercent: ChartPoint[];
  waistCm: ChartPoint[];
  bmi: ChartPoint[];
};
