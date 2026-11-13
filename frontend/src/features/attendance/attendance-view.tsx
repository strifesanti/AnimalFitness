"use client";

import { useParams } from "next/navigation";
import { useCallback, useEffect, useMemo, useState } from "react";
import { Button } from "@/components/ui/button";
import { Card, CardDescription, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Badge, Notice } from "@/components/ui/notice";
import {
  AthleteSelect,
  useAthleteScope,
} from "@/features/advanced/use-athlete-scope";
import { errorMessage, fullName, selectClassName } from "@/lib/admin";
import {
  ATTENDANCE_STATUSES,
  attendanceLabel,
  attendanceTone,
  formatDay,
} from "@/lib/advanced";
import { todayInputValue } from "@/lib/progress";
import { textOrNull } from "@/lib/training";
import { attendanceApi } from "@/services/api";
import type {
  AttendanceRecord,
  AttendanceStats,
  AttendanceStatus,
} from "@/types/advanced";

const WEEKDAYS = ["L", "M", "X", "J", "V", "S", "D"];

function pad(value: number) {
  return String(value).padStart(2, "0");
}

function monthRange(month: string) {
  const [year, index] = month.split("-").map(Number);
  const last = new Date(year, index, 0).getDate();
  return { from: `${month}-01`, to: `${month}-${pad(last)}`, year, index, last };
}

function shiftMonth(month: string, delta: number) {
  const [year, index] = month.split("-").map(Number);
  const date = new Date(year, index - 1 + delta, 1);
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}`;
}

const TONE_CELL: Record<string, string> = {
  good: "border-[var(--accent)]/60 bg-[var(--accent)]/15 text-[var(--accent)]",
  warn: "border-amber-400/60 bg-amber-400/15 text-amber-300",
  bad: "border-red-500/60 bg-red-500/15 text-red-300",
  neutral: "border-[var(--border)] bg-white/5 text-[var(--foreground)]",
};

export function AttendanceView() {
  const { slug } = useParams<{ slug: string }>();
  const scope = useAthleteScope(slug, { requireAthlete: false });
  const { selected } = scope;

  const [month, setMonth] = useState(() => todayInputValue().slice(0, 7));
  const [records, setRecords] = useState<AttendanceRecord[]>([]);
  const [stats, setStats] = useState<AttendanceStats | null>(null);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);
  const [form, setForm] = useState({
    athleteId: "",
    date: todayInputValue(),
    status: "Present" as AttendanceStatus,
    notes: "",
  });

  const range = useMemo(() => monthRange(month), [month]);

  const load = useCallback(async () => {
    try {
      const [list, summary] = await Promise.all([
        attendanceApi.list(slug, selected, range.from, range.to),
        attendanceApi.stats(slug, selected, range.from, range.to),
      ]);
      setRecords(list);
      setStats(summary);
      setError(null);
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setLoading(false);
    }
  }, [slug, selected, range.from, range.to]);

  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect
    void load();
  }, [load]);

  const byDate = useMemo(() => {
    const map = new Map<string, AttendanceRecord>();
    for (const record of records) {
      map.set(record.date, record);
    }
    return map;
  }, [records]);

  async function mark(event: React.FormEvent) {
    event.preventDefault();
    const athleteProfileId = form.athleteId || selected;
    if (!athleteProfileId) {
      setError("Elige el alumno al que registrar la asistencia.");
      return;
    }
    setBusy(true);
    setError(null);
    setSuccess(null);
    try {
      await attendanceApi.upsert(slug, {
        athleteProfileId,
        date: form.date,
        status: form.status,
        notes: textOrNull(form.notes),
      });
      setSuccess("Asistencia guardada.");
      setForm({ ...form, notes: "" });
      await load();
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setBusy(false);
    }
  }

  if (!scope.ready) {
    return <p className="text-sm text-[var(--muted)]">Cargando…</p>;
  }

  const firstWeekday = (new Date(range.year, range.index - 1, 1).getDay() + 6) % 7;
  const showCalendar = !scope.isStaff || Boolean(selected);
  const monthLabel = new Date(range.year, range.index - 1, 1).toLocaleDateString("es", {
    month: "long",
    year: "numeric",
  });
  const sorted = [...records].sort((a, b) => b.date.localeCompare(a.date));

  return (
    <div className="space-y-6">
      <p className="text-sm text-[var(--muted)]">
        {scope.isStaff
          ? "Registra y consulta la asistencia de tus alumnos."
          : "Consulta tu asistencia. Tu entrenador es quien la registra."}
      </p>

      {error ?? scope.error ? (
        <Notice tone="error">{error ?? scope.error}</Notice>
      ) : null}
      {success ? <Notice tone="success">{success}</Notice> : null}

      {scope.isStaff ? (
        <AthleteSelect
          id="attendance-athlete"
          athletes={scope.athletes}
          value={scope.athleteId}
          onChange={scope.setAthleteId}
          allowAll
          hint="Sin alumno seleccionado verás la asistencia de toda la organización."
        />
      ) : null}

      <Card>
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div>
            <CardTitle className="capitalize">{monthLabel}</CardTitle>
            <CardDescription>
              {stats && stats.total > 0
                ? `${stats.presentPercent}% presente · ${stats.attendedPercent}% asistió (incluye tardes) · ${stats.total} registros`
                : "Sin registros este mes."}
            </CardDescription>
          </div>
          <div className="flex gap-2">
            <Button size="sm" variant="secondary" onClick={() => setMonth(shiftMonth(month, -1))}>
              ← Anterior
            </Button>
            <Button
              size="sm"
              variant="secondary"
              onClick={() => setMonth(todayInputValue().slice(0, 7))}
            >
              Hoy
            </Button>
            <Button size="sm" variant="secondary" onClick={() => setMonth(shiftMonth(month, 1))}>
              Siguiente →
            </Button>
          </div>
        </div>

        {stats && stats.total > 0 ? (
          <div className="mt-4 flex flex-wrap gap-2">
            <Badge tone="good">Presentes {stats.present}</Badge>
            <Badge tone="warn">Tarde {stats.late}</Badge>
            <Badge tone="neutral">Justificados {stats.excused}</Badge>
            <Badge tone="bad">Ausentes {stats.absent}</Badge>
          </div>
        ) : null}

        {showCalendar ? (
          <div className="mt-5">
            <div className="grid grid-cols-7 gap-1 text-center text-[10px] uppercase tracking-wider text-[var(--muted)]">
              {WEEKDAYS.map((day) => (
                <span key={day}>{day}</span>
              ))}
            </div>
            <div className="mt-1 grid grid-cols-7 gap-1">
              {Array.from({ length: firstWeekday }, (_, i) => (
                <span key={`blank-${i}`} />
              ))}
              {Array.from({ length: range.last }, (_, i) => {
                const day = i + 1;
                const key = `${month}-${pad(day)}`;
                const record = byDate.get(key);
                const tone = record ? attendanceTone(record.status) : null;
                return (
                  <button
                    key={key}
                    type="button"
                    disabled={!scope.isStaff}
                    title={record ? attendanceLabel(record.status) : "Sin registro"}
                    onClick={() => setForm({ ...form, date: key })}
                    className={`h-10 rounded-md border text-sm transition ${
                      tone
                        ? TONE_CELL[tone]
                        : "border-[var(--border)] text-[var(--muted)]"
                    } ${scope.isStaff ? "hover:border-[var(--accent)]" : "cursor-default"} ${
                      form.date === key && scope.isStaff ? "ring-2 ring-[var(--accent)]" : ""
                    }`}
                  >
                    {day}
                  </button>
                );
              })}
            </div>
          </div>
        ) : null}
      </Card>

      {scope.isStaff ? (
        <Card>
          <CardTitle>Marcar asistencia</CardTitle>
          <CardDescription>
            Un registro por alumno y día: si ya existe, se actualiza.
          </CardDescription>
          <form onSubmit={mark} className="mt-4 grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
            <div className="space-y-2">
              <Label htmlFor="a-athlete">Alumno</Label>
              <select
                id="a-athlete"
                className={selectClassName}
                value={form.athleteId || selected || ""}
                onChange={(e) => setForm({ ...form, athleteId: e.target.value })}
              >
                <option value="">Selecciona…</option>
                {scope.athletes.map((athlete) => (
                  <option key={athlete.id} value={athlete.id}>
                    {fullName(athlete)}
                  </option>
                ))}
              </select>
            </div>
            <div className="space-y-2">
              <Label htmlFor="a-date">Fecha</Label>
              <Input
                id="a-date"
                type="date"
                required
                value={form.date}
                onChange={(e) => setForm({ ...form, date: e.target.value })}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="a-status">Estado</Label>
              <select
                id="a-status"
                className={selectClassName}
                value={form.status}
                onChange={(e) =>
                  setForm({ ...form, status: e.target.value as AttendanceStatus })
                }
              >
                {ATTENDANCE_STATUSES.map((status) => (
                  <option key={status} value={status}>
                    {attendanceLabel(status)}
                  </option>
                ))}
              </select>
            </div>
            <div className="space-y-2">
              <Label htmlFor="a-notes">Notas</Label>
              <Input
                id="a-notes"
                value={form.notes}
                onChange={(e) => setForm({ ...form, notes: e.target.value })}
              />
            </div>
            <div className="sm:col-span-2 lg:col-span-4">
              <Button type="submit" disabled={busy}>
                {busy ? "Guardando…" : "Guardar asistencia"}
              </Button>
            </div>
          </form>
        </Card>
      ) : null}

      <Card>
        <CardTitle>Registros del mes</CardTitle>
        {loading ? (
          <p className="mt-3 text-sm text-[var(--muted)]">Cargando…</p>
        ) : sorted.length === 0 ? (
          <p className="mt-3 text-sm text-[var(--muted)]">No hay registros.</p>
        ) : (
          <ul className="mt-3 space-y-2">
            {sorted.map((record) => (
              <li
                key={record.id}
                className="flex flex-wrap items-center justify-between gap-3 rounded-md border border-[var(--border)] px-3 py-2"
              >
                <div className="min-w-0">
                  <p className="text-sm font-medium capitalize">
                    {formatDay(record.date)}
                    {scope.isStaff && record.athleteName ? ` · ${record.athleteName}` : ""}
                  </p>
                  {record.notes ? (
                    <p className="text-xs text-[var(--muted)]">{record.notes}</p>
                  ) : null}
                </div>
                <Badge tone={attendanceTone(record.status)}>
                  {attendanceLabel(record.status)}
                </Badge>
              </li>
            ))}
          </ul>
        )}
      </Card>
    </div>
  );
}