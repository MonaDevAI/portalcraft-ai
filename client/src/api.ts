import { ChatResponse } from "./types";

export async function askContextIq(
  message: string,
  selectedRequestId?: string,
  selectedReviewTicketId?: string
): Promise<ChatResponse> {
  const response = await fetch("/api/chat", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      message,
      currentUser: "alex",
      selectedRequestId,
      selectedReviewTicketId,
    }),
  });
  if (!response.ok) throw new Error(`Context IQ request failed (${response.status}).`);
  return response.json() as Promise<ChatResponse>;
}
