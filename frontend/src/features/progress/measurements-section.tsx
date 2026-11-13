"use client";

import { useCallback, useEffect, useState } from "react";
import { Button } from "@/components/ui/button";
import { Card, CardDescription, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Notice } from "@/components/ui/notice";
import { LineChart } from "@/features/progress/line-chart";
import { errorMessage, formatDate, textareaClassName } from "@/lib/admin";
import {
  dateInputToIso,
  formatValue,
  todayInputValue,
} from "@/lib/progress";
import { numberOrNull, textOrNull } from "@/lib/training";
import { progressApi } from "@/services/api";
import type { BodyMeasurement, ProgressCharts } from "@/types/progress";

type Props = {
  slug: string;
  /** Alumno consultado; null cuando es el propio alumno. */
  athleteId: string | null;
  /** Falta elegir alumno (Owner/Trainer). */
  needsAthlete: boolean;
};

const emptyForm = {
  date: todayInputValue(),
  weightKg: "",
  heightCm: "",
  bodyFatPercent: "",
  muscleMassKg: "",
  waistCm: "",
  chestCm: "",
  armCm: "",
  legCm: "",
  hipCm: "",
  notes: "",
};

type FieldKey = Exclude<keyof typeof emptyForm, "date" | "notes">;

const FIELDS: { key: FieldKey; label: string }[] = [
  { key: "weightKg", label: "Peso (kg)" },
  { key: "heightCm", label: "Estatura (cm)" },
  { key: "bodyFatPercent", label: "Grasa (%)" },
  { key: "muscleMassKg", label: "Masa muscular (kg)" },
  { key: "waistCm", label: "Cintura (cm)" },
  { key: "chestCm", label: "Pecho (cm)" },
  { key: "armCm", label: "Brazo (cm)" },
  { key: "legCm", label: "Pierna (cm)" },
  { key: "hipCm", label: "Cadera (cm)" },
];

export function MeasurementsSection({ slug, athleteId, needsAthlete }: Props) {
  const [items, setItems] = useState<BodyMeasurement[]>([]);
  const [charts, setCharts] = useState<ProgressCharts | null>(null);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [form, setForm] = useState(() => ({
    ...emptyForm,
    date: todayInputValue(),
  }));
  const [editingId, setEditingId] = useState<string | null>(null);
  const [from, setFrom] = useState("");
  const [to, setTo] = useState("");

  const load = useCallback(async () => {
    if (needsAthlete) {
      setItems([]);
      setCharts(null);
      setLoading(false);
      return;
    }
    try {
      const fromIso = dateInputToIso(from);
      const toIso = to ? new Date(`${to}T23:59:59`).toISOString() : null;
      const [list, series] = await Promise.all([
        progressApi.listMeasurements(slug, athleteId),
        progressApi.charts(slug, athleteId, fromIso, toIso),
      ]);
      setItems(list);
      setCharts(series);
      setError(null);
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setLoading(false);
    }
  }, [slug, athleteId, needsAthlete, from, to]);

  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect
    void load();
  }, [load]);

  function reset() {
    setForm({ ...emptyForm, date: todayInputValue() });
    setEditingId(null);
  }

  function edit(item: BodyMeasurement) {
    setEditingId(item.id);
    setForm({
      date: item.measuredAt.slice(0, 10),
      weightKg: item.weightKg?.toString() ?? "",
      heightCm: item.heightCm?.toString() ?? "",
      bodyFatPercent: item.bodyFatPercent?.toString() ?? "",
      muscleMassKg: item.muscleMassKg?.toString() ?? "",
      waistCm: item.waistCm?.toString() ?? "",
      chestCm: item.chestCm?.toString() ?? "",
      armCm: item.armCm?.toString() ?? "",
      legCm: item.legCm?.toString() ?? "",
      hipCm: item.hipCm?.toString() ?? "",
      notes: item.notes ?? "",
    });
    setSuccess(null);
  }

  async function submit(event: React.FormEvent) {
    event.preventDefault();
    setBusy(true);
    setError(null);
    setSuccess(null);
    try {
      const input = {
        athleteProfileId: athleteId,
        measuredAt: dateInputToIso(form.date),
        weightKg: numberOrNull(form.weightKg),
        heightCm: numberOrNull(form.heightCm),
        bodyFatPercent: numberOrNull(form.bodyFatPercent),
        muscleMassKg: numberOrNull(form.muscleMassKg),
        waistCm: numberOrNull(form.waistCm),
        chestCm: numberOrNull(form.chestCm),
        armCm: numberOrNull(form.armCm),
        legCm: numberOrNull(form.legCm),
        hipCm: numberOrNull(form.hipCm),
        notes: textOrNull(form.notes),
      };
      if (editingId) {
        await progressApi.updateMeasurement(slug, editingId, input);
        setSuccess("Medición actualizada.");
      } else {
        await progressApi.createMeasurement(slug, input);
        setSuccess("Medición registrada.");
      }
      reset();
      await load();
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setBusy(false);
    }
  }

  if (needsAthlete) {
    return null;
  }

  return (
    <div className="space-y-6">
      {error ? <Notice tone="error">{error}</Notice> : null}
      {success ? <Notice tone="success">{success}</Notice> : null}

      <Card>
        <CardTitle>Evolución</CardTitle>
        <CardDescription>
          Peso, grasa corporal, cintura e IMC (calculado con peso y estatura de
          cada medición).
        </CardDescription>
        <div className="mt-4 grid gap-4 sm:grid-cols-2 lg:max-w-md">
          <div className="space-y-2">
            <Label htmlFor="chart-from">Desde</Label>
            <Input
              id="chart-from"
              type="date"
              value={from}
              onChange={(e) => setFrom(e.target.value)}
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="chart-to">Hasta</Label>
            <Input
              id="chart-to"
              type="date"
              value={to}
              onChange={(e) => setTo(e.target.value)}
            />
          </div>
        </div>
        <div className="mt-4 grid gap-4 md:grid-cols-2">
          <LineChart title="Peso" unit="kg" points={charts?.weightKg ?? []} />
          <LineChart title="IMC" unit="" points={charts?.bmi ?? []} />
          <LineChart
            title="Grasa corporal"
            unit="%"
            points={charts?.bodyFatPercent ?? []}
          />
          <LineChart title="Cintura" unit="cm" points={charts?.waistCm ?? []} />
        </div>
      </Card>

      <Card>
        <CardTitle>{editingId ? "Editar medición" : "Nueva medición"}</CardTitle>
        <CardDescription>
          Registra al menos un valor. El IMC se calcula solo si hay peso y estatura.
        </CardDescription>
        <form onSubmit={submit} className="mt-4 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          <div className="space-y-2">
            <Label htmlFor="m-date">Fecha</Label>
            <Input
              id="m-date"
              type="date"
              required
              value={form.date}
              onChange={(e) => setForm({ ...form, date: e.target.value })}
            />
          </div>
          {FIELDS.map((field) => (
            <div key={field.key} className="space-y-2">
              <Label htmlFor={`m-${field.key}`}>{field.label}</Label>
              <Input
                id={`m-${field.key}`}
                inputMode="decimal"
                value={form[field.key]}
                onChange={(e) => setForm({ ...form, [field.key]: e.target.value })}
              />
            </div>
          ))}
          <div className="space-y-2 sm:col-span-2 lg:col-span-4">
            <Label htmlFor="m-notes">Notas</Label>
            <textarea
              id="m-notes"
              className={textareaClassName}
              value={form.notes}
              onChange={(e) => setForm({ ...form, notes: e.target.value })}
            />
          </div>
          <div className="flex gap-2 sm:col-span-2 lg:col-span-4">
            <Button type="submit" disabled={busy}>
              {busy ? "Guardando…" : editingId ? "Guardar cambios" : "Registrar medición"}
            </Button>
            {editingId ? (
              <Button type="button" variant="secondary" onClick={reset}>
                Cancelar edición
              </Button>
            ) : null}
          </div>
        </form>
      </Card>

      <Card>
        <CardTitle>Historial de mediciones</CardTitle>
        {loading ? (
          <p className="mt-3 text-sm text-[var(--muted)]">Cargando…</p>
        ) : items.length === 0 ? (
          <p className="mt-3 text-sm text-[var(--muted)]">Aún no hay mediciones.</p>
        ) : (
          <div className="mt-3 overflow-x-auto">
            <table className="w-full min-w-[640px] text-left text-sm">
              <thead className="text-xs uppercase text-[var(--muted)]">
                <tr>
                  <th className="py-2 pr-3">Fecha</th>
                  <th className="pr-3">Peso</th>
                  <th className="pr-3">Estatura</th>
                  <th className="pr-3">IMC</th>
                  <th className="pr-3">Grasa</th>
                  <th className="pr-3">Cintura</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {items.map((item) => (
                  <tr key={item.id} className="border-t border-[var(--border)]">
                    <td className="py-2 pr-3">{formatDate(item.measuredAt)}</td>
                    <td className="pr-3">{formatValue(item.weightKg, "kg")}</td>
                    <td className="pr-3">{formatValue(item.heightCm, "cm")}</td>
                    <td className="pr-3">{formatValue(item.bmi)}</td>
                    <td className="pr-3">{formatValue(item.bodyFatPercent, "%")}</td>
                    <td className="pr-3">{formatValue(item.waistCm, "cm")}</td>
                    <td className="text-right">
                      <Button size="sm" variant="ghost" onClick={() => edit(item)}>
                        Editar
                      </Button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </Card>
    </div>
  );
}
