import { Suspense } from "react";
import { ChatView } from "@/features/chat/chat-view";

export const instant = false;

export default function ChatPage() {
  return (
    <Suspense
      fallback={<p className="text-sm text-[var(--muted)]">Cargando chat…</p>}
    >
      <ChatView />
    </Suspense>
  );
}
