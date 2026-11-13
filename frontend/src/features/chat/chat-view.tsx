"use client";

import {
  HubConnection,
  HubConnectionBuilder,
  LogLevel,
} from "@microsoft/signalr";
import { useParams, useSearchParams } from "next/navigation";
import { useCallback, useEffect, useRef, useState } from "react";
import { Button } from "@/components/ui/button";
import { Card, CardDescription, CardTitle } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Badge, Notice } from "@/components/ui/notice";
import { useAuth } from "@/features/auth/auth-context";
import { errorMessage, normalizeRole, selectClassName } from "@/lib/admin";
import { formatTime } from "@/lib/advanced";
import { roleLabel } from "@/lib/navigation";
import { formatDateTime } from "@/lib/training";
import { cn } from "@/lib/utils";
import { chatApi } from "@/services/api";
import type {
  ChatContact,
  ChatMessage,
  Conversation,
  ConversationKind,
} from "@/types/advanced";

type LinkState = "connecting" | "connected" | "offline";

/** Origen del hub. Vacío = mismo origen (Next.js reescribe `/hubs/*` hacia la API). */
const HUB_ORIGIN = process.env.NEXT_PUBLIC_HUB_ORIGIN ?? "";

/** Intervalo de respaldo cuando no hay conexión en tiempo real. */
const FALLBACK_POLL_MS = 8000;

function mergeMessage(list: ChatMessage[], message: ChatMessage): ChatMessage[] {
  if (list.some((item) => item.id === message.id)) {
    return list;
  }
  return [...list, message].sort(
    (a, b) => new Date(a.sentAt).getTime() - new Date(b.sentAt).getTime(),
  );
}

export function ChatView() {
  const { slug } = useParams<{ slug: string }>();
  const search = useSearchParams();
  const { user } = useAuth();
  const me = user?.id ?? "";

  const [conversations, setConversations] = useState<Conversation[]>([]);
  const [contacts, setContacts] = useState<ChatContact[]>([]);
  const [activeId, setActiveId] = useState<string | null>(search.get("c"));
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [text, setText] = useState("");
  const [link, setLink] = useState<LinkState>("connecting");
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [creating, setCreating] = useState(false);
  const [newChat, setNewChat] = useState({
    kind: "Direct" as ConversationKind,
    title: "",
    userIds: [] as string[],
  });

  const connectionRef = useRef<HubConnection | null>(null);
  const joinedRef = useRef<Set<string>>(new Set());
  const activeRef = useRef<string | null>(activeId);
  const endRef = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    activeRef.current = activeId;
  }, [activeId]);

  const loadConversations = useCallback(async () => {
    try {
      setConversations(await chatApi.conversations(slug));
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setLoading(false);
    }
  }, [slug]);

  const loadMessages = useCallback(
    async (id: string) => {
      try {
        const list = await chatApi.messages(slug, id);
        if (activeRef.current === id) {
          setMessages(list);
        }
      } catch (err) {
        setError(errorMessage(err));
      }
    },
    [slug],
  );

  const markRead = useCallback(
    async (id: string) => {
      try {
        await chatApi.markRead(slug, id);
        setConversations((prev) =>
          prev.map((item) => (item.id === id ? { ...item, unreadCount: 0 } : item)),
        );
      } catch {
        // Marcar como leído es secundario: no interrumpe la conversación.
      }
    },
    [slug],
  );

  // Carga inicial: conversaciones y contactos.
  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect
    void loadConversations();
    chatApi
      .contacts(slug)
      .then(setContacts)
      .catch((err) => setError(errorMessage(err)));
  }, [slug, loadConversations]);

  // Conexión SignalR (cookies de sesión). Si no es posible, se usa sondeo.
  useEffect(() => {
    const joined = joinedRef.current;
    const connection = new HubConnectionBuilder()
      .withUrl(`${HUB_ORIGIN}/hubs/chat`, { withCredentials: true })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 20000])
      .configureLogging(LogLevel.Warning)
      .build();
    connectionRef.current = connection;

    connection.on("messageReceived", (message: ChatMessage) => {
      if (message.conversationId === activeRef.current) {
        setMessages((prev) => mergeMessage(prev, message));
        void chatApi.markRead(slug, message.conversationId).catch(() => undefined);
      }
      setConversations((prev) => {
        const found = prev.find((item) => item.id === message.conversationId);
        if (!found) {
          void loadConversations();
          return prev;
        }
        const own = message.senderUserId === me;
        const active = message.conversationId === activeRef.current;
        const updated: Conversation = {
          ...found,
          lastMessageAt: message.sentAt,
          lastMessagePreview: message.body || "📎 Adjunto",
          unreadCount: own || active ? 0 : found.unreadCount + 1,
        };
        return [updated, ...prev.filter((item) => item.id !== found.id)];
      });
    });

    connection.onreconnecting(() => setLink("connecting"));
    connection.onclose(() => {
      joined.clear();
      setLink("offline");
    });
    connection.onreconnected(() => {
      // El servidor pierde los grupos al reconectar: se vuelven a pedir.
      joined.clear();
      setLink("connected");
      void loadConversations();
      if (activeRef.current) {
        void loadMessages(activeRef.current);
      }
    });

    connection
      .start()
      .then(() => setLink("connected"))
      .catch(() => setLink("offline"));

    return () => {
      connectionRef.current = null;
      joined.clear();
      void connection.stop();
    };
  }, [slug, me, loadConversations, loadMessages]);

  // Suscribe la conexión a cada conversación visible (para recibir mensajes y no leídos).
  useEffect(() => {
    const connection = connectionRef.current;
    if (link !== "connected" || !connection) {
      return;
    }
    for (const conversation of conversations) {
      if (joinedRef.current.has(conversation.id)) {
        continue;
      }
      joinedRef.current.add(conversation.id);
      connection.invoke("JoinConversation", slug, conversation.id).catch(() => {
        joinedRef.current.delete(conversation.id);
      });
    }
  }, [link, conversations, slug]);

  // Respaldo por sondeo si no hay tiempo real.
  useEffect(() => {
    if (link === "connected") {
      return;
    }
    const timer = setInterval(() => {
      void loadConversations();
      if (activeRef.current) {
        void loadMessages(activeRef.current);
      }
    }, FALLBACK_POLL_MS);
    return () => clearInterval(timer);
  }, [link, loadConversations, loadMessages]);

  // Carga los mensajes al abrir una conversación.
  useEffect(() => {
    if (!activeId) {
      // eslint-disable-next-line react-hooks/set-state-in-effect
      setMessages([]);
      return;
    }
    void loadMessages(activeId);
    void markRead(activeId);
  }, [activeId, loadMessages, markRead]);

  useEffect(() => {
    endRef.current?.scrollIntoView({ block: "end" });
  }, [messages]);

  async function send(event: React.FormEvent) {
    event.preventDefault();
    const body = text.trim();
    if (!activeId || !body) {
      return;
    }
    setBusy(true);
    setError(null);
    try {
      const message = await chatApi.sendMessage(slug, activeId, body);
      setText("");
      setMessages((prev) => mergeMessage(prev, message));
      setConversations((prev) => {
        const found = prev.find((item) => item.id === activeId);
        return found
          ? [
              { ...found, lastMessageAt: message.sentAt, lastMessagePreview: message.body },
              ...prev.filter((item) => item.id !== found.id),
            ]
          : prev;
      });
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setBusy(false);
    }
  }

  async function createConversation(event: React.FormEvent) {
    event.preventDefault();
    setBusy(true);
    setError(null);
    try {
      const created = await chatApi.createConversation(slug, {
        kind: newChat.kind,
        title: newChat.kind === "Group" ? newChat.title.trim() : null,
        participantUserIds: newChat.userIds,
      });
      setCreating(false);
      setNewChat({ kind: "Direct", title: "", userIds: [] });
      await loadConversations();
      setActiveId(created.id);
    } catch (err) {
      setError(errorMessage(err));
    } finally {
      setBusy(false);
    }
  }

  function toggleUser(userId: string) {
    setNewChat((prev) => {
      if (prev.kind === "Direct") {
        return { ...prev, userIds: prev.userIds[0] === userId ? [] : [userId] };
      }
      return {
        ...prev,
        userIds: prev.userIds.includes(userId)
          ? prev.userIds.filter((id) => id !== userId)
          : [...prev.userIds, userId],
      };
    });
  }

  const active = conversations.find((item) => item.id === activeId) ?? null;
  const canCreate =
    newChat.userIds.length > 0 &&
    (newChat.kind === "Direct" || newChat.title.trim().length > 0);

  return (
    <div className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <p className="text-sm text-[var(--muted)]">
          Conversa con tu academia en tiempo real.
        </p>
        <Badge tone={link === "connected" ? "good" : link === "connecting" ? "warn" : "bad"}>
          {link === "connected"
            ? "En tiempo real"
            : link === "connecting"
              ? "Conectando…"
              : "Sin conexión en vivo (se actualiza cada pocos segundos)"}
        </Badge>
      </div>

      {error ? <Notice tone="error">{error}</Notice> : null}

      <div className="grid gap-4 md:grid-cols-[320px_1fr]">
        <Card className="space-y-3 md:max-h-[70vh] md:overflow-y-auto">
          <div className="flex items-center justify-between">
            <CardTitle>Conversaciones</CardTitle>
            <Button size="sm" variant="secondary" onClick={() => setCreating((v) => !v)}>
              {creating ? "Cerrar" : "Nueva"}
            </Button>
          </div>

          {creating ? (
            <form onSubmit={createConversation} className="space-y-3 rounded-md border border-[var(--border)] p-3">
              <div className="space-y-2">
                <Label htmlFor="chat-kind">Tipo</Label>
                <select
                  id="chat-kind"
                  className={selectClassName}
                  value={newChat.kind}
                  onChange={(e) =>
                    setNewChat({ kind: e.target.value as ConversationKind, title: "", userIds: [] })
                  }
                >
                  <option value="Direct">Directa (2 personas)</option>
                  <option value="Group">Grupo</option>
                </select>
              </div>
              {newChat.kind === "Group" ? (
                <div className="space-y-2">
                  <Label htmlFor="chat-title">Título</Label>
                  <Input
                    id="chat-title"
                    value={newChat.title}
                    onChange={(e) => setNewChat({ ...newChat, title: e.target.value })}
                  />
                </div>
              ) : null}
              <div className="space-y-1">
                <Label>{newChat.kind === "Direct" ? "Persona" : "Personas"}</Label>
                <ul className="max-h-48 space-y-1 overflow-y-auto">
                  {contacts.map((contact) => (
                    <li key={contact.userId}>
                      <label className="flex cursor-pointer items-center gap-2 rounded-md px-2 py-1 text-sm hover:bg-white/5">
                        <input
                          type={newChat.kind === "Direct" ? "radio" : "checkbox"}
                          name="chat-contact"
                          checked={newChat.userIds.includes(contact.userId)}
                          onChange={() => toggleUser(contact.userId)}
                        />
                        <span className="min-w-0 flex-1 truncate">{contact.name}</span>
                        <span className="text-[10px] uppercase text-[var(--muted)]">
                          {roleLabel(normalizeRole(contact.role))}
                        </span>
                      </label>
                    </li>
                  ))}
                  {contacts.length === 0 ? (
                    <li className="text-sm text-[var(--muted)]">No hay otras personas aún.</li>
                  ) : null}
                </ul>
              </div>
              <Button type="submit" disabled={busy || !canCreate}>
                Crear conversación
              </Button>
            </form>
          ) : null}

          {loading ? (
            <p className="text-sm text-[var(--muted)]">Cargando…</p>
          ) : conversations.length === 0 ? (
            <CardDescription>Aún no tienes conversaciones. Crea la primera.</CardDescription>
          ) : (
            <ul className="space-y-1">
              {conversations.map((conversation) => (
                <li key={conversation.id}>
                  <button
                    type="button"
                    onClick={() => setActiveId(conversation.id)}
                    className={cn(
                      "w-full rounded-md border px-3 py-2 text-left transition",
                      conversation.id === activeId
                        ? "border-[var(--accent)] bg-[var(--accent)]/10"
                        : "border-transparent hover:bg-white/5",
                    )}
                  >
                    <span className="flex items-center justify-between gap-2">
                      <span className="truncate text-sm font-medium">
                        {conversation.kind === "Group" ? "👥 " : ""}
                        {conversation.title}
                      </span>
                      {conversation.unreadCount > 0 ? (
                        <span className="rounded-full bg-[var(--accent)] px-2 text-[10px] font-bold text-[var(--accent-foreground)]">
                          {conversation.unreadCount}
                        </span>
                      ) : null}
                    </span>
                    {conversation.lastMessagePreview ? (
                      <span className="mt-0.5 block truncate text-xs text-[var(--muted)]">
                        {conversation.lastMessagePreview}
                      </span>
                    ) : null}
                  </button>
                </li>
              ))}
            </ul>
          )}
        </Card>

        <Card className="flex min-h-[420px] flex-col md:max-h-[70vh]">
          {active ? (
            <>
              <div className="border-b border-[var(--border)] pb-3">
                <CardTitle>{active.title}</CardTitle>
                <CardDescription>
                  {active.participants.map((p) => p.name).join(", ")}
                </CardDescription>
              </div>

              <div className="flex-1 space-y-2 overflow-y-auto py-4" aria-live="polite">
                {messages.length === 0 ? (
                  <p className="text-sm text-[var(--muted)]">Aún no hay mensajes. Escribe el primero.</p>
                ) : (
                  messages.map((message) => {
                    const own = message.senderUserId === me;
                    return (
                      <div
                        key={message.id}
                        className={cn("flex", own ? "justify-end" : "justify-start")}
                      >
                        <div
                          className={cn(
                            "max-w-[80%] rounded-lg px-3 py-2 text-sm",
                            own
                              ? "bg-[var(--accent)] text-[var(--accent-foreground)]"
                              : "bg-white/10",
                          )}
                        >
                          {!own ? (
                            <p className="mb-0.5 text-[10px] font-semibold uppercase tracking-wider opacity-70">
                              {message.senderName}
                            </p>
                          ) : null}
                          <p className="whitespace-pre-wrap break-words">{message.body}</p>
                          {message.attachmentUrl ? (
                            <a
                              href={message.attachmentUrl}
                              target="_blank"
                              rel="noopener noreferrer"
                              className="text-xs underline"
                            >
                              Adjunto
                            </a>
                          ) : null}
                          <p
                            className="mt-1 text-right text-[10px] opacity-60"
                            title={formatDateTime(message.sentAt)}
                          >
                            {formatTime(message.sentAt)}
                          </p>
                        </div>
                      </div>
                    );
                  })
                )}
                <div ref={endRef} />
              </div>

              <form onSubmit={send} className="flex gap-2 border-t border-[var(--border)] pt-3">
                <Input
                  aria-label="Mensaje"
                  placeholder="Escribe un mensaje…"
                  maxLength={4000}
                  value={text}
                  onChange={(e) => setText(e.target.value)}
                />
                <Button type="submit" disabled={busy || !text.trim()}>
                  Enviar
                </Button>
              </form>
            </>
          ) : (
            <div className="grid flex-1 place-items-center text-sm text-[var(--muted)]">
              Selecciona o crea una conversación.
            </div>
          )}
        </Card>
      </div>
    </div>
  );
}
