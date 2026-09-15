import { FormEvent, useEffect, useRef, useState } from "react";
import { askContextIq } from "./api";
import { assistantName, productName } from "./config";
import { ChatMessage, RequestRecord } from "./types";

type Props = {
  onOpenView: (view: "requests" | "reviewQueue", requests: RequestRecord[]) => void;
};

const prompts = [
  "Show my recent requests",
  "Show requests created by Alex",
  "Show request REQ-1042",
  "Show review ticket REV-8398 in the review queue",
];

export function ContextIq({ onOpenView }: Props) {
  const [open, setOpen] = useState(false);
  const [input, setInput] = useState("");
  const [running, setRunning] = useState(false);
  const [selectedRequestId, setSelectedRequestId] = useState<string>();
  const [selectedReviewTicketId, setSelectedReviewTicketId] = useState<string>();
  const messagesRef = useRef<HTMLDivElement>(null);
  const [messages, setMessages] = useState<ChatMessage[]>([
    {
      role: "assistant",
      text: `Ask ${assistantName} about requests, review tickets, or the review queue.`,
    },
  ]);

  useEffect(() => {
    const container = messagesRef.current;
    if (container) {
      container.scrollTop = container.scrollHeight;
    }
  }, [messages, running]);

  async function submit(event?: FormEvent) {
    event?.preventDefault();
    const message = input.trim();
    if (!message || running) return;
    setInput("");
    setRunning(true);
    setMessages(current => [...current, { role: "user", text: message }]);
    try {
      const response = await askContextIq(message, selectedRequestId, selectedReviewTicketId);
      setSelectedRequestId(response.selectedRequestId || selectedRequestId);
      setSelectedReviewTicketId(response.selectedReviewTicketId || selectedReviewTicketId);
      setMessages(current => [...current, { role: "assistant", text: response.message, response }]);
    } catch (error) {
      setMessages(current => [
        ...current,
        { role: "assistant", text: error instanceof Error ? error.message : "Request failed." },
      ]);
    } finally {
      setRunning(false);
    }
  }

  return (
    <>
      {!open && <div className="launcher-label">Open {assistantName}</div>}
      <button className="launcher" aria-label={`Open ${assistantName}`} onClick={() => setOpen(true)}>
        ✦
      </button>
      <aside className={`assistant ${open ? "open" : ""}`} aria-label={assistantName}>
        <header className="assistant-header">
          <div className="iq-logo">IQ</div>
          <div>
            <strong>{assistantName}</strong>
            <span>Grounded assistance for {productName}</span>
          </div>
          <button className="close" aria-label="Close Context IQ" onClick={() => setOpen(false)}>×</button>
        </header>
        <nav className="mode-tabs" aria-label="Context IQ modes">
          <button>UI</button>
          <button className="active">Query</button>
          <button>Security</button>
        </nav>
        <div className="prompt-strip">
          {prompts.map(prompt => (
            <button key={prompt} onClick={() => setInput(prompt)}>{prompt}</button>
          ))}
        </div>
        <div className="messages" aria-live="polite" ref={messagesRef}>
          {messages.map((message, index) => (
            <article
              className={`message ${message.role === "assistant" ? "assistant-message" : "user"}`}
              key={`${message.role}-${index}`}
            >
              <span>{message.role === "user" ? "YOU" : assistantName.toUpperCase()}</span>
              <div className="bubble">
                {message.text}
                {message.response?.requests.map(request => (
                  <div className="result-card" key={request.requestId}>
                    <strong>{request.requestId}</strong>
                    <p>{request.title}</p>
                    <small>{request.type} · {request.status} · {request.stage}</small>
                    {request.reviewTicketId && <small>Review ticket: {request.reviewTicketId}</small>}
                    <button
                      onClick={() => {
                        setSelectedRequestId(request.requestId);
                        setSelectedReviewTicketId(request.reviewTicketId || undefined);
                        onOpenView("requests", [request]);
                      }}
                    >
                      Show in requests
                    </button>
                  </div>
                ))}
                {message.response?.sources.map(source => (
                  <small className="source" key={`${source.type}-${source.identifier}`}>
                    Source: {source.label} · {source.identifier}
                  </small>
                ))}
                {message.response?.actions
                  .filter(action => action.view === "reviewQueue")
                  .map(action => (
                    <button
                      className="open-action"
                      key={`${action.view}-${action.reviewTicketId}`}
                      onClick={() => onOpenView("reviewQueue", message.response?.requests || [])}
                    >
                      Open review queue
                    </button>
                  ))}
              </div>
            </article>
          ))}
          {running && <div className="typing">{assistantName} is checking trusted data…</div>}
        </div>
        <form className="composer" onSubmit={submit}>
          <textarea
            aria-label={`Ask ${assistantName}`}
            value={input}
            onChange={event => setInput(event.target.value)}
            placeholder="Ask about a request or review ticket…"
          />
          <button type="submit" disabled={running || !input.trim()} aria-label="Send">➤</button>
        </form>
      </aside>
    </>
  );
}
