"use client";

import { useParams } from "next/navigation";
import { useCallback, useEffect, useState } from "react";
import { Button } from "@/components/ui/button";
import { Card, CardDescription, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Notice } from "@/components/ui/notice";
import { ProgressBar } from "@/components/ui/progress-bar";
import {
  AthleteSelect,
  useAthleteScope,
} from "@/features/advanced/use-athlete-scope";
import { errorMessage, selectClassName } from "@/lib/admin";
import { MEAL_TYPES, mealLabel } from "@/lib/advanced";
import { todayInputValue } from "@/lib/progress";
import { numberOrNull, textOrNull } from "@/lib/training";
import { nutritionApi } from "@/services/api";
import type { MealType, NutritionSummary } from "@/types/advanced";

const emptyFood = {
  mealType: "Breakfast" as MealType,
  name: "",
  calories: "",
  proteinGrams: "",
  carbsGrams: "",
  fatGrams: "",
  notes: "",
};

const emptyTarget = {
  calories: "2000",
  proteinGrams: "120",
  carbsGrams: "220",
  fatGrams: "60",
  waterMl: "2500",
};

export function NutritionView() {
  const { slug } = useParams<{ slug: string }>();
  const scope = useAthleteScope(slug, { requireAthlete: true });
  const { selected, needsAthlete } = scope;

  const [date, setDate] = useState(todayInputValue);
  const [summary, setSummary] = useState<NutritionSummary | null>(null);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [food, setFood] = useState(emptyFood);
  const [target, setTarget] = useState(emptyTarget);
  const [customWater, setCustomWater] = useState("");

  const load = useCallback(async () => {
    if (needsAthlete) {
      setSummary(null);
      setLoading(false);
      return;
    }
    try {
      setSummary(await nutritionApi.summary(slug, selected, date));
      setError(null);
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setLoading(false);
    }
  }, [slug, selected, needsAthlete, date]);

  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect
    void load();
  }, [load]);

  async function run(work: () => Promise<string | void>, fallback: string) {
    setBusy(true);
    setError(null);
    setSuccess(null);
    try {
      const message = await work();
      setSuccess(message || fallback);
      await load();
      return true;
    } catch (err) {
      setError(errorMessage(err));
      return false;
    } finally {
      setBusy(false);
    }
  }

  async function addFood(event: React.FormEvent) {
    event.preventDefault();
    const done = await run(async () => {
      await nutritionApi.createFood(slug, {
        athleteProfileId: selected,
        entryDate: date,
        mealType: food.mealType,
        name: food.name.trim(),
        calories: Math.round(numberOrNull(food.calories) ?? 0),
        proteinGrams: numberOrNull(food.proteinGrams) ?? 0,
        carbsGrams: numberOrNull(food.carbsGrams) ?? 0,
        fatGrams: numberOrNull(food.fatGrams) ?? 0,
        notes: textOrNull(food.notes),
      });
    }, "Comida registrada.");
    if (done) {
      setFood(emptyFood);
    }
  }

  async function addWater(amountMl: number) {
    if (!Number.isFinite(amountMl) || amountMl < 1) {
      setError("Indica una cantidad de agua válida.");
      return;
    }
    const done = await run(async () => {
      await nutritionApi.createWater(slug, {
        athleteProfileId: selected,
        entryDate: date,
        amountMl: Math.round(amountMl),
      });
    }, "Agua registrada.");
    if (done) {
      setCustomWater("");
    }
  }

  async function saveTarget(event: React.FormEvent) {
    event.preventDefault();
    await run(async () => {
      await nutritionApi.createTarget(slug, {
        athleteProfileId: selected,
        calories: Math.round(numberOrNull(target.calories) ?? 0),
        proteinGrams: numberOrNull(target.proteinGrams) ?? 0,
        carbsGrams: numberOrNull(target.carbsGrams) ?? 0,
        fatGrams: numberOrNull(target.fatGrams) ?? 0,
        waterMl: Math.round(numberOrNull(target.waterMl) ?? 0),
        effectiveFrom: date,
      });
    }, "Objetivos guardados.");
  }

  if (!scope.ready) {
    return <p className="text-sm text-[var(--muted)]">Cargando…</p>;
  }

  const goal = summary?.target ?? null;
  const percentages = summary?.percentages ?? null;
  const consumed = summary?.consumed;

  return (
    <div className="space-y-6">
      <p className="text-sm text-[var(--muted)]">
        {scope.isStaff
          ? "Define objetivos y revisa la alimentación de tus alumnos."
          : "Registra tus comidas y tu agua para ver cómo vas frente a tus objetivos."}
      </p>

      {error ?? scope.error ? (
        <Notice tone="error">{error ?? scope.error}</Notice>
      ) : null}
      {success ? <Notice tone="success">{success}</Notice> : null}

      {scope.isStaff ? (
        <AthleteSelect
          id="nutrition-athlete"
          athletes={scope.athletes}
          value={scope.athleteId}
          onChange={scope.setAthleteId}
          hint={needsAthlete ? "Elige un alumno para ver su nutrición." : undefined}
        />
      ) : null}

      {needsAthlete ? null : (
        <>
          <Card>
            <div className="flex flex-wrap items-end justify-between gap-4">
              <div>
                <CardTitle>Resumen del día</CardTitle>
                <CardDescription>
                  {goal
                    ? `Objetivo vigente: ${goal.calories} kcal · P ${goal.proteinGrams} g · C ${goal.carbsGrams} g · G ${goal.fatGrams} g · ${goal.waterMl} ml`
                    : "Aún no hay objetivos definidos para esta fecha."}
                </CardDescription>
              </div>
              <div className="space-y-2">
                <Label htmlFor="nutrition-date">Fecha</Label>
                <Input
                  id="nutrition-date"
                  type="date"
                  value={date}
                  onChange={(e) => e.target.value && setDate(e.target.value)}
                />
              </div>
            </div>

            {loading || !consumed ? (
              <p className="mt-4 text-sm text-[var(--muted)]">Cargando…</p>
            ) : (
              <div className="mt-5 grid gap-4 md:grid-cols-2">
                <ProgressBar
                  label="Calorías"
                  unit="kcal"
                  value={consumed.calories}
                  goal={goal?.calories}
                  percent={percentages?.calories}
                />
                <ProgressBar
                  label="Agua"
                  unit="ml"
                  value={consumed.waterMl}
                  goal={goal?.waterMl}
                  percent={percentages?.water}
                />
                <ProgressBar
                  label="Proteína"
                  unit="g"
                  value={consumed.proteinGrams}
                  goal={goal?.proteinGrams}
                  percent={percentages?.protein}
                />
                <ProgressBar
                  label="Carbohidratos"
                  unit="g"
                  value={consumed.carbsGrams}
                  goal={goal?.carbsGrams}
                  percent={percentages?.carbs}
                />
                <ProgressBar
                  label="Grasas"
                  unit="g"
                  value={consumed.fatGrams}
                  goal={goal?.fatGrams}
                  percent={percentages?.fat}
                />
              </div>
            )}
          </Card>

          <div className="grid gap-6 lg:grid-cols-2">
            <Card>
              <CardTitle>Registrar comida</CardTitle>
              <form onSubmit={addFood} className="mt-4 grid gap-4 sm:grid-cols-2">
                <div className="space-y-2">
                  <Label htmlFor="f-meal">Comida</Label>
                  <select
                    id="f-meal"
                    className={selectClassName}
                    value={food.mealType}
                    onChange={(e) =>
                      setFood({ ...food, mealType: e.target.value as MealType })
                    }
                  >
                    {MEAL_TYPES.map((meal) => (
                      <option key={meal} value={meal}>
                        {mealLabel(meal)}
                      </option>
                    ))}
                  </select>
                </div>
                <div className="space-y-2">
                  <Label htmlFor="f-name">Alimento</Label>
                  <Input
                    id="f-name"
                    required
                    value={food.name}
                    onChange={(e) => setFood({ ...food, name: e.target.value })}
                  />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="f-cal">Calorías</Label>
                  <Input
                    id="f-cal"
                    required
                    inputMode="numeric"
                    value={food.calories}
                    onChange={(e) => setFood({ ...food, calories: e.target.value })}
                  />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="f-pro">Proteína (g)</Label>
                  <Input
                    id="f-pro"
                    inputMode="decimal"
                    value={food.proteinGrams}
                    onChange={(e) =>
                      setFood({ ...food, proteinGrams: e.target.value })
                    }
                  />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="f-car">Carbohidratos (g)</Label>
                  <Input
                    id="f-car"
                    inputMode="decimal"
                    value={food.carbsGrams}
                    onChange={(e) => setFood({ ...food, carbsGrams: e.target.value })}
                  />
                </div>
                <div className="space-y-2">
                  <Label htmlFor="f-fat">Grasas (g)</Label>
                  <Input
                    id="f-fat"
                    inputMode="decimal"
                    value={food.fatGrams}
                    onChange={(e) => setFood({ ...food, fatGrams: e.target.value })}
                  />
                </div>
                <div className="space-y-2 sm:col-span-2">
                  <Label htmlFor="f-notes">Notas</Label>
                  <Input
                    id="f-notes"
                    value={food.notes}
                    onChange={(e) => setFood({ ...food, notes: e.target.value })}
                  />
                </div>
                <div className="sm:col-span-2">
                  <Button type="submit" disabled={busy}>
                    {busy ? "Guardando…" : "Añadir comida"}
                  </Button>
                </div>
              </form>
            </Card>

            <Card>
              <CardTitle>Agua</CardTitle>
              <CardDescription>
                Total del día: {consumed?.waterMl ?? 0} ml
              </CardDescription>
              <div className="mt-4 flex flex-wrap gap-2">
                {[250, 500, 750].map((amount) => (
                  <Button
                    key={amount}
                    size="sm"
                    variant="secondary"
                    disabled={busy}
                    onClick={() => addWater(amount)}
                  >
                    +{amount} ml
                  </Button>
                ))}
              </div>
              <div className="mt-4 flex items-end gap-2">
                <div className="flex-1 space-y-2">
                  <Label htmlFor="w-custom">Otra cantidad (ml)</Label>
                  <Input
                    id="w-custom"
                    inputMode="numeric"
                    value={customWater}
                    onChange={(e) => setCustomWater(e.target.value)}
                  />
                </div>
                <Button
                  disabled={busy || !customWater}
                  onClick={() => addWater(Number(customWater))}
                >
                  Añadir
                </Button>
              </div>
              {summary && summary.water.length > 0 ? (
                <ul className="mt-4 space-y-1">
                  {summary.water.map((log) => (
                    <li
                      key={log.id}
                      className="flex items-center justify-between rounded-md border border-[var(--border)] px-3 py-1.5 text-sm"
                    >
                      <span>{log.amountMl} ml</span>
                      <Button
                        size="sm"
                        variant="ghost"
                        disabled={busy}
                        onClick={() =>
                          run(
                            () => nutritionApi.deleteWater(slug, log.id),
                            "Registro eliminado.",
                          )
                        }
                      >
                        Quitar
                      </Button>
                    </li>
                  ))}
                </ul>
              ) : null}
            </Card>
          </div>

          <Card>
            <CardTitle>Comidas del día</CardTitle>
            {summary && summary.foods.length > 0 ? (
              <ul className="mt-3 space-y-2">
                {summary.foods.map((item) => (
                  <li
                    key={item.id}
                    className="flex flex-wrap items-center justify-between gap-3 rounded-md border border-[var(--border)] px-3 py-2"
                  >
                    <div className="min-w-0">
                      <p className="text-sm font-medium">
                        {item.name}{" "}
                        <span className="text-xs font-normal text-[var(--muted)]">
                          · {mealLabel(item.mealType)}
                        </span>
                      </p>
                      <p className="text-xs text-[var(--muted)]">
                        {item.calories} kcal · P {item.proteinGrams} g · C{" "}
                        {item.carbsGrams} g · G {item.fatGrams} g
                        {item.notes ? ` · ${item.notes}` : ""}
                      </p>
                    </div>
                    <Button
                      size="sm"
                      variant="ghost"
                      disabled={busy}
                      onClick={() =>
                        run(
                          () => nutritionApi.deleteFood(slug, item.id),
                          "Comida eliminada.",
                        )
                      }
                    >
                      Quitar
                    </Button>
                  </li>
                ))}
              </ul>
            ) : (
              <p className="mt-3 text-sm text-[var(--muted)]">
                No hay comidas registradas este día.
              </p>
            )}
          </Card>

          <Card>
            <CardTitle>Objetivos diarios</CardTitle>
            <CardDescription>
              Orientativos. Se aplican desde la fecha seleccionada y no sustituyen el consejo de un profesional.
            </CardDescription>
            <form onSubmit={saveTarget} className="mt-4 grid gap-4 sm:grid-cols-2 lg:grid-cols-5">
              {(
                [
                  ["calories", "Calorías (kcal)", "t-cal"],
                  ["proteinGrams", "Proteína (g)", "t-pro"],
                  ["carbsGrams", "Carbohidratos (g)", "t-car"],
                  ["fatGrams", "Grasas (g)", "t-fat"],
                  ["waterMl", "Agua (ml)", "t-wat"],
                ] as const
              ).map(([key, label, id]) => (
                <div key={key} className="space-y-2">
                  <Label htmlFor={id}>{label}</Label>
                  <Input
                    id={id}
                    required
                    inputMode="decimal"
                    value={target[key]}
                    onChange={(e) => setTarget({ ...target, [key]: e.target.value })}
                  />
                </div>
              ))}
              <div className="sm:col-span-2 lg:col-span-5">
                <Button type="submit" disabled={busy}>
                  {busy ? "Guardando…" : "Guardar objetivos"}
                </Button>
              </div>
            </form>
          </Card>
        </>
      )}
    </div>
  );
}
