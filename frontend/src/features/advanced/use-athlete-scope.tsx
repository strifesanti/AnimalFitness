"use client";

import { useEffect, useState } from "react";
import { Card, CardDescription } from "@/components/ui/card";
import { Label } from "@/components/ui/label";
import { useAuth } from "@/features/auth/auth-context";
import {
  errorMessage,
  fullName,
  normalizeRole,
  selectClassName,
} from "@/lib/admin";
import { adminApi } from "@/services/api";
import type { AthleteProfile } from "@/types/admin";

/**
 * Resuelve el rol y el alumno objetivo de una pantalla.
 * Los alumnos actúan sobre sí mismos; Owner y Trainer eligen un alumno de la lista.
 */
export function useAthleteScope(slug: string, options: { requireAthlete: boolean }) {
  const { user } = useAuth();
  const membership = user?.memberships.find((item) => item.slug === slug) ?? null;
  const role = membership ? normalizeRole(membership.role) : null;
  const isStaff = role !== null && role !== "Athlete";

  const [athletes, setAthletes] = useState<AthleteProfile[]>([]);
  const [athleteId, setAthleteId] = useState("");
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!isStaff) {
      return;
    }
    let active = true;
    adminApi
      .listAthletes(slug)
      .then((list) => {
        if (active) {
          setAthletes(list);
        }
      })
      .catch((err) => {
        if (active) {
          setError(errorMessage(err));
        }
      });
    return () => {
      active = false;
    };
  }, [slug, isStaff]);

  const selected = isStaff ? athleteId || null : null;
  return {
    ready: membership !== null,
    role,
    isStaff,
    athletes,
    athleteId,
    setAthleteId,
    selected,
    needsAthlete: options.requireAthlete && isStaff && !selected,
    error,
  };
}

type AthleteSelectProps = {
  id: string;
  athletes: AthleteProfile[];
  value: string;
  onChange: (value: string) => void;
  allowAll?: boolean;
  hint?: string;
};

/** Selector de alumno para Owner y Trainer. */
export function AthleteSelect({
  id,
  athletes,
  value,
  onChange,
  allowAll = false,
  hint,
}: AthleteSelectProps) {
  return (
    <Card>
      <div className="max-w-sm space-y-2">
        <Label htmlFor={id}>Alumno</Label>
        <select
          id={id}
          className={selectClassName}
          value={value}
          onChange={(e) => onChange(e.target.value)}
        >
          <option value="">
            {allowAll ? "Todos los alumnos" : "Selecciona un alumno…"}
          </option>
          {athletes.map((athlete) => (
            <option key={athlete.id} value={athlete.id}>
              {fullName(athlete)}
            </option>
          ))}
        </select>
      </div>
      {hint ? <CardDescription>{hint}</CardDescription> : null}
    </Card>
  );
}
