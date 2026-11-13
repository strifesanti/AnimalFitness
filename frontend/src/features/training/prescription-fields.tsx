"use client";

import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { intOrNull, numberOrNull, textOrNull } from "@/lib/training";
import type { PrescriptionFields, WorkoutExercise } from "@/types/training";

export type PrescriptionForm = {
  sortOrder: string;
  targetSets: string;
  targetReps: string;
  targetWeightKg: string;
  targetDurationSeconds: string;
  targetDistanceMeters: string;
  restSeconds: string;
  tempo: string;
  notes: string;
};

export const emptyPrescription: PrescriptionForm = {
  sortOrder: "",
  targetSets: "3",
  targetReps: "",
  targetWeightKg: "",
  targetDurationSeconds: "",
  targetDistanceMeters: "",
  restSeconds: "",
  tempo: "",
  notes: "",
};

export function prescriptionFromExercise(item: WorkoutExercise): PrescriptionForm {
  return {
    sortOrder: String(item.sortOrder),
    targetSets: String(item.targetSets),
    targetReps: item.targetReps ?? "",
    targetWeightKg: item.targetWeightKg?.toString() ?? "",
    targetDurationSeconds: item.targetDurationSeconds?.toString() ?? "",
    targetDistanceMeters: item.targetDistanceMeters?.toString() ?? "",
    restSeconds: item.restSeconds?.toString() ?? "",
    tempo: item.tempo ?? "",
    notes: item.notes ?? "",
  };
}

export function prescriptionToInput(form: PrescriptionForm): PrescriptionFields {
  return {
    targetSets: intOrNull(form.targetSets) ?? 1,
    targetReps: textOrNull(form.targetReps),
    targetWeightKg: numberOrNull(form.targetWeightKg),
    targetDurationSeconds: intOrNull(form.targetDurationSeconds),
    targetDistanceMeters: numberOrNull(form.targetDistanceMeters),
    restSeconds: intOrNull(form.restSeconds),
    tempo: textOrNull(form.tempo),
    notes: textOrNull(form.notes),
  };
}

export function PrescriptionInputs({
  idPrefix,
  value,
  onChange,
  showOrder = false,
}: {
  idPrefix: string;
  value: PrescriptionForm;
  onChange: (next: PrescriptionForm) => void;
  showOrder?: boolean;
}) {
  const field = (
    key: keyof PrescriptionForm,
    label: string,
    options: { inputMode?: "numeric" | "decimal"; placeholder?: string } = {},
  ) => (
    <div className="space-y-2">
      <Label htmlFor={`${idPrefix}-${key}`}>{label}</Label>
      <Input
        id={`${idPrefix}-${key}`}
        value={value[key]}
        inputMode={options.inputMode}
        placeholder={options.placeholder}
        onChange={(e) => onChange({ ...value, [key]: e.target.value })}
      />
    </div>
  );

  return (
    <>
      {field("targetSets", "Series", { inputMode: "numeric" })}
      {field("targetReps", "Repeticiones", { placeholder: "Ej. 8-10" })}
      {field("targetWeightKg", "Peso (kg)", { inputMode: "decimal" })}
      {field("targetDurationSeconds", "Duración (s)", { inputMode: "numeric" })}
      {field("targetDistanceMeters", "Distancia (m)", { inputMode: "decimal" })}
      {field("restSeconds", "Descanso (s)", { inputMode: "numeric" })}
      {field("tempo", "Tempo", { placeholder: "Ej. 3-1-1" })}
      {showOrder ? field("sortOrder", "Orden", { inputMode: "numeric" }) : null}
      <div className="space-y-2 sm:col-span-2 lg:col-span-4">
        <Label htmlFor={`${idPrefix}-notes`}>Notas</Label>
        <Input
          id={`${idPrefix}-notes`}
          value={value.notes}
          onChange={(e) => onChange({ ...value, notes: e.target.value })}
        />
      </div>
    </>
  );
}
