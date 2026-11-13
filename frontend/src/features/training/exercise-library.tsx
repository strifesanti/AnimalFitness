"use client";

import { useParams } from "next/navigation";
import { useCallback, useEffect, useState } from "react";
import { Button } from "@/components/ui/button";
import { Card, CardDescription, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Badge, Notice } from "@/components/ui/notice";
import { useAuth } from "@/features/auth/auth-context";
import {
  errorMessage,
  normalizeRole,
  selectClassName,
  textareaClassName,
} from "@/lib/admin";
import {
  DIFFICULTIES,
  EXERCISE_TYPES,
  difficultyLabel,
  exerciseTypeLabel,
  intOrNull,
  textOrNull,
} from "@/lib/training";
import { trainingApi } from "@/services/api";
import type {
  Exercise,
  ExerciseCategory,
  ExerciseDifficulty,
  ExerciseMediaType,
  ExerciseType,
} from "@/types/training";

type ExerciseForm = {
  name: string;
  primaryMuscle: string;
  secondaryMuscles: string;
  type: ExerciseType;
  difficulty: ExerciseDifficulty;
  categoryId: string;
  equipment: string;
  description: string;
  instructions: string;
  variationOfExerciseId: string;
};

const emptyForm: ExerciseForm = {
  name: "",
  primaryMuscle: "",
  secondaryMuscles: "",
  type: "Gym",
  difficulty: "Beginner",
  categoryId: "",
  equipment: "",
  description: "",
  instructions: "",
  variationOfExerciseId: "",
};

type CategoryForm = { name: string; description: string; sortOrder: string };
const emptyCategory: CategoryForm = { name: "", description: "", sortOrder: "0" };

type MediaForm = {
  mediaType: ExerciseMediaType;
  url: string;
  caption: string;
};
const emptyMedia: MediaForm = { mediaType: "Image", url: "", caption: "" };

function toForm(exercise: Exercise): ExerciseForm {
  return {
    name: exercise.name,
    primaryMuscle: exercise.primaryMuscle,
    secondaryMuscles: exercise.secondaryMuscles ?? "",
    type: String(exercise.type) as ExerciseType,
    difficulty: String(exercise.difficulty) as ExerciseDifficulty,
    categoryId: exercise.categoryId ?? "",
    equipment: exercise.equipment ?? "",
    description: exercise.description ?? "",
    instructions: exercise.instructions ?? "",
    variationOfExerciseId: exercise.variationOfExerciseId ?? "",
  };
}

export function ExerciseLibrary() {
  const { slug } = useParams<{ slug: string }>();
  const { user } = useAuth();
  const membership = user?.memberships.find((item) => item.slug === slug);
  const canManage = membership
    ? normalizeRole(membership.role) !== "Athlete"
    : false;

  const [exercises, setExercises] = useState<Exercise[]>([]);
  const [categories, setCategories] = useState<ExerciseCategory[]>([]);
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [success, setSuccess] = useState<string | null>(null);

  // Filtros
  const [q, setQ] = useState("");
  const [typeFilter, setTypeFilter] = useState<ExerciseType | "">("");
  const [muscle, setMuscle] = useState("");
  const [applied, setApplied] = useState<{
    q: string;
    type: ExerciseType | "";
    muscle: string;
  }>({ q: "", type: "", muscle: "" });

  // Formularios
  const [formOpen, setFormOpen] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [form, setForm] = useState<ExerciseForm>(emptyForm);
  const [mediaForm, setMediaForm] = useState<MediaForm>(emptyMedia);
  const [categoryForm, setCategoryForm] = useState<CategoryForm>(emptyCategory);
  const [editingCategoryId, setEditingCategoryId] = useState<string | null>(null);

  const load = useCallback(async () => {
    try {
      const [exerciseList, categoryList] = await Promise.all([
        trainingApi.listExercises(slug, applied),
        trainingApi.listCategories(slug),
      ]);
      setExercises(exerciseList);
      setCategories(categoryList);
      setError(null);
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setLoading(false);
    }
  }, [slug, applied]);

  useEffect(() => {
    if (!membership) {
      return;
    }
    // eslint-disable-next-line react-hooks/set-state-in-effect
    void load();
  }, [load, membership]);

  async function run(task: () => Promise<string>, after?: () => void) {
    setBusy(true);
    setError(null);
    setSuccess(null);
    try {
      setSuccess(await task());
      after?.();
      await load();
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setBusy(false);
    }
  }

  function applyFilters(event: React.FormEvent) {
    event.preventDefault();
    setLoading(true);
    setApplied({ q, type: typeFilter, muscle });
  }

  function clearFilters() {
    setQ("");
    setTypeFilter("");
    setMuscle("");
    setLoading(true);
    setApplied({ q: "", type: "", muscle: "" });
  }

  function openCreate() {
    setEditingId(null);
    setForm(emptyForm);
    setMediaForm(emptyMedia);
    setFormOpen(true);
  }

  function openEdit(exercise: Exercise) {
    setEditingId(exercise.id);
    setForm(toForm(exercise));
    setMediaForm(emptyMedia);
    setFormOpen(true);
  }

  function closeForm() {
    setFormOpen(false);
    setEditingId(null);
  }

  async function submitExercise(event: React.FormEvent) {
    event.preventDefault();
    const input = {
      name: form.name.trim(),
      primaryMuscle: form.primaryMuscle.trim(),
      type: form.type,
      difficulty: form.difficulty,
      categoryId: form.categoryId || null,
      description: textOrNull(form.description),
      secondaryMuscles: textOrNull(form.secondaryMuscles),
      equipment: textOrNull(form.equipment),
      instructions: textOrNull(form.instructions),
      variationOfExerciseId: form.variationOfExerciseId || null,
    };
    await run(async () => {
      if (editingId) {
        await trainingApi.updateExercise(slug, editingId, input);
        return "Ejercicio actualizado.";
      }
      await trainingApi.createExercise(slug, input);
      return "Ejercicio creado.";
    }, closeForm);
  }

  async function submitMedia(event: React.FormEvent) {
    event.preventDefault();
    if (!editingId) {
      return;
    }
    const exerciseId = editingId;
    const current = exercises.find((item) => item.id === exerciseId);
    await run(
      async () => {
        await trainingApi.addExerciseMedia(slug, exerciseId, {
          mediaType: mediaForm.mediaType,
          url: mediaForm.url.trim(),
          sortOrder: current?.media.length ?? 0,
          caption: textOrNull(mediaForm.caption),
        });
        return "Medio agregado.";
      },
      () => setMediaForm(emptyMedia),
    );
  }

  async function submitCategory(event: React.FormEvent) {
    event.preventDefault();
    const input = {
      name: categoryForm.name.trim(),
      description: textOrNull(categoryForm.description),
      sortOrder: intOrNull(categoryForm.sortOrder) ?? 0,
    };
    await run(
      async () => {
        if (editingCategoryId) {
          await trainingApi.updateCategory(slug, editingCategoryId, input);
          return "Categoría actualizada.";
        }
        await trainingApi.createCategory(slug, input);
        return "Categoría creada.";
      },
      () => {
        setCategoryForm(emptyCategory);
        setEditingCategoryId(null);
      },
    );
  }

  const categoryName = (id: string | null) =>
    id ? (categories.find((item) => item.id === id)?.name ?? null) : null;
  const editingExercise = editingId
    ? exercises.find((item) => item.id === editingId)
    : undefined;

  return (
    <div className="space-y-8">
      <p className="text-sm text-[var(--muted)]">
        Biblioteca de ejercicios de la academia. Los planes de entrenamiento se
        construyen con estos ejercicios.
      </p>

      {error ? <Notice tone="error">{error}</Notice> : null}
      {success ? <Notice tone="success">{success}</Notice> : null}

      <Card>
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div>
            <CardTitle>Ejercicios</CardTitle>
            <CardDescription>
              Busca por nombre, tipo o músculo principal.
            </CardDescription>
          </div>
          {canManage && !formOpen ? (
            <Button size="sm" onClick={openCreate}>
              Nuevo ejercicio
            </Button>
          ) : null}
        </div>

        <form
          onSubmit={applyFilters}
          className="mt-4 grid gap-3 sm:grid-cols-[1fr_12rem_12rem_auto_auto] sm:items-end"
        >
          <div className="space-y-2">
            <Label htmlFor="ex-q">Buscar</Label>
            <Input
              id="ex-q"
              value={q}
              placeholder="Nombre del ejercicio"
              onChange={(e) => setQ(e.target.value)}
            />
          </div>
          <div className="space-y-2">
            <Label htmlFor="ex-type">Tipo</Label>
            <select
              id="ex-type"
              className={selectClassName}
              value={typeFilter}
              onChange={(e) => setTypeFilter(e.target.value as ExerciseType | "")}
            >
              <option value="">Todos</option>
              {EXERCISE_TYPES.map((type) => (
                <option key={type} value={type}>
                  {exerciseTypeLabel(type)}
                </option>
              ))}
            </select>
          </div>
          <div className="space-y-2">
            <Label htmlFor="ex-muscle">Músculo</Label>
            <Input
              id="ex-muscle"
              value={muscle}
              placeholder="Ej. Pecho"
              onChange={(e) => setMuscle(e.target.value)}
            />
          </div>
          <Button type="submit" variant="secondary">
            Filtrar
          </Button>
          <Button type="button" variant="ghost" onClick={clearFilters}>
            Limpiar
          </Button>
        </form>

        {loading ? (
          <p className="mt-4 text-sm text-[var(--muted)]">Cargando…</p>
        ) : exercises.length === 0 ? (
          <p className="mt-4 text-sm text-[var(--muted)]">
            No hay ejercicios que coincidan.
          </p>
        ) : (
          <ul className="mt-4 space-y-2">
            {exercises.map((exercise) => {
              const category = categoryName(exercise.categoryId);
              return (
                <li
                  key={exercise.id}
                  className="rounded-md border border-[var(--border)] px-3 py-3"
                >
                  <div className="flex flex-wrap items-start justify-between gap-2">
                    <div className="min-w-0">
                      <p className="font-medium">
                        {exercise.name}{" "}
                        <Badge>{exerciseTypeLabel(exercise.type)}</Badge>{" "}
                        <Badge tone="warn">
                          {difficultyLabel(exercise.difficulty)}
                        </Badge>
                      </p>
                      <p className="mt-1 text-xs text-[var(--muted)]">
                        Músculo: {exercise.primaryMuscle}
                        {exercise.secondaryMuscles
                          ? ` (+ ${exercise.secondaryMuscles})`
                          : ""}
                        {exercise.equipment ? ` · Equipo: ${exercise.equipment}` : ""}
                        {category ? ` · Categoría: ${category}` : ""}
                      </p>
                      {exercise.description ? (
                        <p className="mt-1 text-xs text-[var(--muted)]">
                          {exercise.description}
                        </p>
                      ) : null}
                      {exercise.media.length > 0 ? (
                        <p className="mt-1 text-xs">
                          {exercise.media.map((media, index) => (
                            <a
                              key={media.id}
                              href={media.url}
                              target="_blank"
                              rel="noopener noreferrer"
                              className="mr-3 text-[var(--accent)] underline"
                            >
                              {media.caption ||
                                (String(media.mediaType) === "Video"
                                  ? `Video ${index + 1}`
                                  : `Imagen ${index + 1}`)}
                            </a>
                          ))}
                        </p>
                      ) : null}
                    </div>
                    {canManage ? (
                      <Button
                        size="sm"
                        variant="secondary"
                        disabled={busy}
                        onClick={() =>
                          editingId === exercise.id && formOpen
                            ? closeForm()
                            : openEdit(exercise)
                        }
                      >
                        {editingId === exercise.id && formOpen
                          ? "Cancelar"
                          : "Editar"}
                      </Button>
                    ) : null}
                  </div>
                </li>
              );
            })}
          </ul>
        )}
      </Card>

      {canManage && formOpen ? (
        <Card>
          <CardTitle>{editingId ? "Editar ejercicio" : "Nuevo ejercicio"}</CardTitle>
          <form
            onSubmit={submitExercise}
            className="mt-4 grid gap-4 sm:grid-cols-2"
          >
            <div className="space-y-2">
              <Label htmlFor="f-name">Nombre</Label>
              <Input
                id="f-name"
                required
                value={form.name}
                onChange={(e) => setForm({ ...form, name: e.target.value })}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="f-muscle">Músculo principal</Label>
              <Input
                id="f-muscle"
                required
                value={form.primaryMuscle}
                onChange={(e) =>
                  setForm({ ...form, primaryMuscle: e.target.value })
                }
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="f-type">Tipo</Label>
              <select
                id="f-type"
                className={selectClassName}
                value={form.type}
                onChange={(e) =>
                  setForm({ ...form, type: e.target.value as ExerciseType })
                }
              >
                {EXERCISE_TYPES.map((type) => (
                  <option key={type} value={type}>
                    {exerciseTypeLabel(type)}
                  </option>
                ))}
              </select>
            </div>
            <div className="space-y-2">
              <Label htmlFor="f-diff">Dificultad</Label>
              <select
                id="f-diff"
                className={selectClassName}
                value={form.difficulty}
                onChange={(e) =>
                  setForm({
                    ...form,
                    difficulty: e.target.value as ExerciseDifficulty,
                  })
                }
              >
                {DIFFICULTIES.map((item) => (
                  <option key={item} value={item}>
                    {difficultyLabel(item)}
                  </option>
                ))}
              </select>
            </div>
            <div className="space-y-2">
              <Label htmlFor="f-cat">Categoría</Label>
              <select
                id="f-cat"
                className={selectClassName}
                value={form.categoryId}
                onChange={(e) => setForm({ ...form, categoryId: e.target.value })}
              >
                <option value="">Sin categoría</option>
                {categories.map((category) => (
                  <option key={category.id} value={category.id}>
                    {category.name}
                  </option>
                ))}
              </select>
            </div>
            <div className="space-y-2">
              <Label htmlFor="f-equip">Equipo</Label>
              <Input
                id="f-equip"
                value={form.equipment}
                onChange={(e) => setForm({ ...form, equipment: e.target.value })}
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="f-sec">Músculos secundarios</Label>
              <Input
                id="f-sec"
                value={form.secondaryMuscles}
                onChange={(e) =>
                  setForm({ ...form, secondaryMuscles: e.target.value })
                }
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="f-var">Variación de</Label>
              <select
                id="f-var"
                className={selectClassName}
                value={form.variationOfExerciseId}
                onChange={(e) =>
                  setForm({ ...form, variationOfExerciseId: e.target.value })
                }
              >
                <option value="">Ninguna</option>
                {exercises
                  .filter((item) => item.id !== editingId)
                  .map((item) => (
                    <option key={item.id} value={item.id}>
                      {item.name}
                    </option>
                  ))}
              </select>
            </div>
            <div className="space-y-2 sm:col-span-2">
              <Label htmlFor="f-desc">Descripción</Label>
              <textarea
                id="f-desc"
                className={textareaClassName}
                value={form.description}
                onChange={(e) =>
                  setForm({ ...form, description: e.target.value })
                }
              />
            </div>
            <div className="space-y-2 sm:col-span-2">
              <Label htmlFor="f-inst">Instrucciones</Label>
              <textarea
                id="f-inst"
                className={textareaClassName}
                value={form.instructions}
                onChange={(e) =>
                  setForm({ ...form, instructions: e.target.value })
                }
              />
            </div>
            <div className="flex flex-wrap gap-2 sm:col-span-2">
              <Button type="submit" disabled={busy}>
                {busy
                  ? "Guardando…"
                  : editingId
                    ? "Guardar cambios"
                    : "Crear ejercicio"}
              </Button>
              <Button
                type="button"
                variant="ghost"
                disabled={busy}
                onClick={closeForm}
              >
                Cancelar
              </Button>
            </div>
          </form>

          {editingExercise ? (
            <form
              onSubmit={submitMedia}
              className="mt-6 grid gap-4 border-t border-[var(--border)] pt-4 sm:grid-cols-[10rem_1fr_1fr_auto] sm:items-end"
            >
              <div className="space-y-2">
                <Label htmlFor="m-type">Medio</Label>
                <select
                  id="m-type"
                  className={selectClassName}
                  value={mediaForm.mediaType}
                  onChange={(e) =>
                    setMediaForm({
                      ...mediaForm,
                      mediaType: e.target.value as ExerciseMediaType,
                    })
                  }
                >
                  <option value="Image">Imagen</option>
                  <option value="Video">Video</option>
                </select>
              </div>
              <div className="space-y-2">
                <Label htmlFor="m-url">URL</Label>
                <Input
                  id="m-url"
                  type="url"
                  required
                  placeholder="https://…"
                  value={mediaForm.url}
                  onChange={(e) =>
                    setMediaForm({ ...mediaForm, url: e.target.value })
                  }
                />
              </div>
              <div className="space-y-2">
                <Label htmlFor="m-cap">Descripción</Label>
                <Input
                  id="m-cap"
                  value={mediaForm.caption}
                  onChange={(e) =>
                    setMediaForm({ ...mediaForm, caption: e.target.value })
                  }
                />
              </div>
              <Button type="submit" variant="secondary" disabled={busy}>
                Agregar medio
              </Button>
            </form>
          ) : null}
        </Card>
      ) : null}

      <Card>
        <CardTitle>Categorías</CardTitle>
        <CardDescription>
          Organiza la biblioteca en grupos (por ejemplo, Piernas o Empuje).
        </CardDescription>
        {categories.length === 0 ? (
          <p className="mt-4 text-sm text-[var(--muted)]">
            Aún no hay categorías.
          </p>
        ) : (
          <ul className="mt-4 space-y-2">
            {categories.map((category) => (
              <li
                key={category.id}
                className="flex flex-wrap items-center justify-between gap-2 rounded-md border border-[var(--border)] px-3 py-2"
              >
                <div className="min-w-0">
                  <p className="text-sm font-medium">{category.name}</p>
                  {category.description ? (
                    <p className="text-xs text-[var(--muted)]">
                      {category.description}
                    </p>
                  ) : null}
                </div>
                {canManage ? (
                  <Button
                    size="sm"
                    variant="secondary"
                    disabled={busy}
                    onClick={() => {
                      setEditingCategoryId(category.id);
                      setCategoryForm({
                        name: category.name,
                        description: category.description ?? "",
                        sortOrder: String(category.sortOrder),
                      });
                    }}
                  >
                    Editar
                  </Button>
                ) : null}
              </li>
            ))}
          </ul>
        )}

        {canManage ? (
          <form
            onSubmit={submitCategory}
            className="mt-4 grid gap-3 border-t border-[var(--border)] pt-4 sm:grid-cols-[1fr_1fr_7rem_auto] sm:items-end"
          >
            <div className="space-y-2">
              <Label htmlFor="c-name">
                {editingCategoryId ? "Editar categoría" : "Nueva categoría"}
              </Label>
              <Input
                id="c-name"
                required
                value={categoryForm.name}
                onChange={(e) =>
                  setCategoryForm({ ...categoryForm, name: e.target.value })
                }
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="c-desc">Descripción</Label>
              <Input
                id="c-desc"
                value={categoryForm.description}
                onChange={(e) =>
                  setCategoryForm({ ...categoryForm, description: e.target.value })
                }
              />
            </div>
            <div className="space-y-2">
              <Label htmlFor="c-order">Orden</Label>
              <Input
                id="c-order"
                inputMode="numeric"
                value={categoryForm.sortOrder}
                onChange={(e) =>
                  setCategoryForm({ ...categoryForm, sortOrder: e.target.value })
                }
              />
            </div>
            <div className="flex gap-2">
              <Button type="submit" disabled={busy}>
                {editingCategoryId ? "Guardar" : "Crear"}
              </Button>
              {editingCategoryId ? (
                <Button
                  type="button"
                  variant="ghost"
                  onClick={() => {
                    setEditingCategoryId(null);
                    setCategoryForm(emptyCategory);
                  }}
                >
                  Cancelar
                </Button>
              ) : null}
            </div>
          </form>
        ) : null}
      </Card>
    </div>
  );
}
