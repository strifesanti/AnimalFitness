import type { AuthUser, MembershipRole } from "@/types/auth";

export type NavItem = {
  label: string;
  href: string;
  enabled: boolean;
  soon?: boolean;
};

export function resolveHomePath(user: AuthUser): string {
  if (user.isSuperAdmin && user.memberships.length === 0) {
    return "/platform";
  }

  const first = user.memberships[0];
  if (first) {
    return `/t/${first.slug}`;
  }

  if (user.isSuperAdmin) {
    return "/platform";
  }

  return "/login";
}

export function platformNav(): NavItem[] {
  return [
    { label: "Dashboard", href: "/platform", enabled: true },
    { label: "Organizaciones", href: "/platform/organizations", enabled: true },
    { label: "Suscripciones", href: "/platform/subscriptions", enabled: false, soon: true },
    { label: "Usuarios", href: "/platform/users", enabled: false, soon: true },
    { label: "Estadísticas", href: "/platform/stats", enabled: false, soon: true },
    { label: "Configuración", href: "/platform/settings", enabled: false, soon: true },
  ];
}

export function tenantNav(slug: string, role: MembershipRole): NavItem[] {
  const base = `/t/${slug}`;

  if (role === "Owner") {
    return [
      { label: "Dashboard", href: base, enabled: true },
      { label: "Entrenadores", href: `${base}/trainers`, enabled: true },
      { label: "Alumnos", href: `${base}/athletes`, enabled: true },
      { label: "Solicitudes", href: `${base}/requests`, enabled: true },
      { label: "Entrenamientos", href: `${base}/workouts`, enabled: true },
      { label: "Progreso", href: `${base}/progress`, enabled: true },
      { label: "Biblioteca", href: `${base}/exercises`, enabled: true },
      { label: "Asistencia", href: `${base}/attendance`, enabled: true },
      { label: "Retos", href: `${base}/challenges`, enabled: true },
      { label: "Nutrición", href: `${base}/nutrition`, enabled: true },
      { label: "Chat", href: `${base}/chat`, enabled: true },
      { label: "Notificaciones", href: `${base}/notifications`, enabled: true },
      { label: "Pagos", href: `${base}/payments`, enabled: false, soon: true },
      { label: "Configuración", href: `${base}/settings`, enabled: true },
    ];
  }

  if (role === "Trainer") {
    return [
      { label: "Inicio", href: base, enabled: true },
      { label: "Mis alumnos", href: `${base}/athletes`, enabled: true },
      { label: "Rutinas", href: `${base}/workouts`, enabled: true },
      { label: "Progreso", href: `${base}/progress`, enabled: true },
      { label: "Biblioteca", href: `${base}/exercises`, enabled: true },
      { label: "Asistencia", href: `${base}/attendance`, enabled: true },
      { label: "Retos", href: `${base}/challenges`, enabled: true },
      { label: "Nutrición", href: `${base}/nutrition`, enabled: true },
      { label: "Mensajes", href: `${base}/chat`, enabled: true },
      { label: "Notificaciones", href: `${base}/notifications`, enabled: true },
    ];
  }

  return [
    { label: "Mi inicio", href: base, enabled: true },
    { label: "Mi entrenamiento", href: `${base}/workouts`, enabled: true },
    { label: "Mi progreso", href: `${base}/progress`, enabled: true },
    { label: "Mi nutrición", href: `${base}/nutrition`, enabled: true },
    { label: "Mi asistencia", href: `${base}/attendance`, enabled: true },
    { label: "Retos", href: `${base}/challenges`, enabled: true },
    { label: "Mi chat", href: `${base}/chat`, enabled: true },
    { label: "Notificaciones", href: `${base}/notifications`, enabled: true },
    { label: "Mi membresía", href: `${base}/membership`, enabled: false, soon: true },
    { label: "Mi perfil", href: `${base}/profile`, enabled: true },
  ];
}

export function roleLabel(role: MembershipRole | null | undefined): string {
  switch (role) {
    case "Owner":
      return "Administrador";
    case "Trainer":
      return "Entrenador";
    case "Athlete":
      return "Alumno";
    default:
      return "Usuario";
  }
}
