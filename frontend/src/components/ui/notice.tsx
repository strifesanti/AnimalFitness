import { cn } from "@/lib/utils";

type NoticeProps = {
  tone: "error" | "success";
  children: React.ReactNode;
  className?: string;
};

export function Notice({ tone, children, className }: NoticeProps) {
  return (
    <p
      role={tone === "error" ? "alert" : "status"}
      className={cn(
        "rounded-md border px-3 py-2 text-sm",
        tone === "error"
          ? "border-red-500/40 bg-red-500/10 text-red-300"
          : "border-[var(--success)]/40 bg-[var(--success)]/10 text-[var(--success)]",
        className,
      )}
    >
      {children}
    </p>
  );
}

export function Badge({
  children,
  tone = "neutral",
}: {
  children: React.ReactNode;
  tone?: "neutral" | "good" | "warn" | "bad";
}) {
  return (
    <span
      className={cn(
        "inline-block rounded-full border px-2 py-0.5 text-[10px] font-semibold uppercase tracking-wider",
        tone === "good" && "border-[var(--success)]/50 text-[var(--success)]",
        tone === "warn" && "border-amber-400/50 text-amber-300",
        tone === "bad" && "border-red-500/50 text-red-300",
        tone === "neutral" && "border-[var(--border)] text-[var(--muted)]",
      )}
    >
      {children}
    </span>
  );
}
