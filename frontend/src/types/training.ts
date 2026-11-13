/** Tipos de la Fase 3: ejercicios, planes y sesiones de entrenamiento. */

export type ExerciseType = "Gym" | "Calisthenics" | "Cardio" | "Other";
export type ExerciseDifficulty = "Beginner" | "Intermediate" | "Advanced";
export type WorkoutPlanStatus = "Draft" | "Published" | "Archived";
export type WorkoutSessionStatus = "InProgress" | "Completed" | "Cancelled";
export type ExerciseMediaType = "Image" | "Video";
export type PlanAssignmentStatus = "Active" | "Ended";

// ----- Ejercicios -----

export type ExerciseCategory = {
  id: string;
  name: string;
  description: string | null;
  sortOrder: number;
};

export type SaveCategoryInput = {
  name: string;
  description: string | null;
  sortOrder: number;
};

export type ExerciseMedia = {
  id: string;
  exerciseId: string;
  mediaType: ExerciseMediaType | string | number;
  url: string;
  sortOrder: number;
  caption: string | null;
};

export type Exercise = {
  id: string;
  categoryId: string | null;
  name: string;
  description: string | null;
  primaryMuscle: string;
  secondaryMuscles: string | null;
  type: ExerciseType | string | number;
  equipment: string | null;
  difficulty: ExerciseDifficulty | string | number;
  instructions: string | null;
  variationOfExerciseId: string | null;
  media: ExerciseMedia[];
};

export type SaveExerciseInput = {
  name: string;
  primaryMuscle: string;
  type: ExerciseType;
  difficulty: ExerciseDifficulty;
  categoryId: string | null;
  description: string | null;
  secondaryMuscles: string | null;
  equipment: string | null;
  instructions: string | null;
  variationOfExerciseId: string | null;
};

export type ExerciseFilters = {
  q?: string;
  type?: ExerciseType | "";
  muscle?: string;
};

export type AddMediaInput = {
  mediaType: ExerciseMediaType;
  url: string;
  sortOrder: number;
  caption: string | null;
};

// ----- Planes -----

export type WorkoutPlanSummary = {
  id: string;
  name: string;
  description: string | null;
  goalLabel: string | null;
  version: number;
  parentPlanId: string | null;
  status: WorkoutPlanStatus | string | number;
  dayCount: number;
};

export type WorkoutExercise = {
  id: string;
  dayId: string;
  exerciseId: string;
  exerciseName: string;
  sortOrder: number;
  targetSets: number;
  targetReps: string | null;
  targetWeightKg: number | null;
  targetDurationSeconds: number | null;
  targetDistanceMeters: number | null;
  restSeconds: number | null;
  tempo: string | null;
  notes: string | null;
};

export type WorkoutDay = {
  id: string;
  planId: string;
  dayIndex: number;
  name: string;
  notes: string | null;
  exercises: WorkoutExercise[];
};

export type WorkoutPlanDetail = {
  id: string;
  name: string;
  description: string | null;
  goalLabel: string | null;
  version: number;
  parentPlanId: string | null;
  status: WorkoutPlanStatus | string | number;
  days: WorkoutDay[];
};

export type SavePlanInput = {
  name: string;
  description: string | null;
  goalLabel: string | null;
};

export type SaveDayInput = {
  name: string;
  notes: string | null;
};

export type PrescriptionFields = {
  targetSets: number;
  targetReps: string | null;
  targetWeightKg: number | null;
  targetDurationSeconds: number | null;
  targetDistanceMeters: number | null;
  restSeconds: number | null;
  tempo: string | null;
  notes: string | null;
};

export type AddWorkoutExerciseInput = PrescriptionFields & {
  exerciseId: string;
  sortOrder?: number | null;
};

export type UpdateWorkoutExerciseInput = PrescriptionFields & {
  sortOrder: number;
};

export type WorkoutAssignment = {
  id: string;
  planId: string;
  athleteProfileId: string;
  assignedAt: string;
  status: PlanAssignmentStatus | string | number;
};

// ----- Sesiones -----

export type WorkoutSession = {
  id: string;
  athleteProfileId: string;
  planId: string | null;
  dayId: string | null;
  status: WorkoutSessionStatus | string | number;
  startedAt: string;
  completedAt: string | null;
  notes: string | null;
};

export type StartSessionInput = {
  athleteProfileId?: string | null;
  planId?: string | null;
  dayId?: string | null;
  notes?: string | null;
};

export type LogSetInput = {
  exerciseId: string;
  setNumber: number;
  workoutExerciseId: string | null;
  reps: number | null;
  weightKg: number | null;
  durationSeconds: number | null;
  distanceMeters: number | null;
  rpe: number | null;
  notes: string | null;
};

export type ExerciseLog = {
  id: string;
  sessionId: string;
  workoutExerciseId: string | null;
  exerciseId: string;
  setNumber: number;
  reps: number | null;
  weightKg: number | null;
  durationSeconds: number | null;
  distanceMeters: number | null;
  rpe: number | null;
  notes: string | null;
  loggedAt: string;
};
