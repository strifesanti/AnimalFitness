import Image from "next/image";
import { cn } from "@/lib/utils";

type BrandMarkProps = {
  className?: string;
  /** Tamaño del logo en px (lado). */
  size?: number;
  /** Muestra el eslogan debajo del nombre. */
  showSlogan?: boolean;
  /** Variante compacta para sidebar. */
  compact?: boolean;
};

export function BrandMark({
  className,
  size = 48,
  showSlogan = true,
  compact = false,
}: BrandMarkProps) {
  return (
    <div className={cn("flex items-center gap-3", className)}>
      <Image
        src="/brand/logo.jpg"
        alt="AnimalFitness"
        width={size}
        height={size}
        className="rounded-lg object-cover shadow-[0_0_24px_rgba(124,58,237,0.35)]"
        priority
      />
      <div className="min-w-0">
        <p
          className={cn(
            "font-[family-name:var(--font-display)] leading-none tracking-[0.08em] text-[var(--accent-secondary)]",
            compact ? "text-xl" : "text-2xl md:text-3xl",
          )}
        >
          ANIMALFITNESS
        </p>
        {showSlogan ? (
          <p className="mt-1 text-[10px] font-medium uppercase tracking-[0.22em] text-[var(--muted)]">
            Entrenamiento sin límites
          </p>
        ) : null}
      </div>
    </div>
  );
}
