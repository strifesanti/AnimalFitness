"use client";

import { useParams } from "next/navigation";
import { useEffect, useState } from "react";
import { Button } from "@/components/ui/button";
import { Card, CardDescription } from "@/components/ui/card";
import { Label } from "@/components/ui/label";
import { Notice } from "@/components/ui/notice";
import { useAuth } from "@/features/auth/auth-context";
import { AssessmentsSection } from "@/features/progress/assessments-section";
import { GoalsSection } from "@/features/progress/goals-section";
import { MeasurementsSection } from "@/features/progress/measurements-section";
import { PhotosSection } from "@/features/progress/photos-section";
import { errorMessage, fullName, normalizeRole, selectClassName } from "@/lib/admin";
import { adminApi } from "@/services/api";
import type { AthleteProfile } from "@/types/admin";

type Tab = "measurements" | "photos" | "goals" | "assessments";

const TABS: { id: Tab; label: string }[] = [
  { id: "measurements", label: "Mediciones" },
  { id: "photos", label: "Fotos" },
  { id: "goals", label: "Objetivos" },
  { id: "assessments", label: "Valoraciones" },
];

export function ProgressView() {
  const { slug } = useParams<{ slug: string }>();
  const { user } = useAuth();
  const membership = user?.memberships.find((item) => item.slug === slug);
  const canManage = membership
    ? normalizeRole(membership.role) !== "Athlete"
    : false;

  const [athletes, setAthletes] = useState<AthleteProfile[]>([]);
  const [athleteId, setAthleteId] = useState("");
  const [tab, setTab] = useState<Tab>("measurements");
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!canManage) {
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
  }, [slug, canManage]);

  if (!membership) {
    return <p className="text-sm text-[var(--muted)]">Cargando…</p>;
  }

  const selected = canManage ? athleteId || null : null;
  const needsAthlete = canManage && !selected;
  const sectionProps = { slug, athleteId: selected, needsAthlete };

  return (
    <div className="space-y-6">
      <p className="text-sm text-[var(--muted)]">
        {canManage
          ? "Consulta y registra el progreso de tus alumnos."
          : "Registra tus medidas, fotos y objetivos para ver tu evolución."}
      </p>

      {error ? <Notice tone="error">{error}</Notice> : null}

      {canManage ? (
        <Card>
          <div className="max-w-sm space-y-2">
            <Label htmlFor="progress-athlete">Alumno</Label>
            <select
              id="progress-athlete"
              className={selectClassName}
              value={athleteId}
              onChange={(e) => setAthleteId(e.target.value)}
            >
              <option value="">Selecciona un alumno…</option>
              {athletes.map((athlete) => (
                <option key={athlete.id} value={athlete.id}>
                  {fullName(athlete)}
                </option>
              ))}
            </select>
          </div>
          {needsAthlete ? (
            <CardDescription>
              Elige un alumno para ver su progreso.
            </CardDescription>
          ) : null}
        </Card>
      ) : null}

      <div className="flex flex-wrap gap-2" role="tablist" aria-label="Secciones de progreso">
        {TABS.map((item) => (
          <Button
            key={item.id}
            role="tab"
            aria-selected={tab === item.id}
            size="sm"
            variant={tab === item.id ? "primary" : "secondary"}
            onClick={() => setTab(item.id)}
          >
            {item.label}
          </Button>
        ))}
      </div>

      {tab === "measurements" ? (
        <MeasurementsSection key={selected ?? "own"} {...sectionProps} />
      ) : null}
      {tab === "photos" ? (
        <PhotosSection key={selected ?? "own"} {...sectionProps} />
      ) : null}
      {tab === "goals" ? (
        <GoalsSection key={selected ?? "own"} {...sectionProps} />
      ) : null}
      {tab === "assessments" ? (
        <AssessmentsSection key={selected ?? "own"} {...sectionProps} />
      ) : null}
    </div>
  );
}
