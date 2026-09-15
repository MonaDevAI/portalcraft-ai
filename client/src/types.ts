export type RequestRecord = {
  requestId: string;
  type: string;
  title: string;
  requester: string;
  status: string;
  stage: string;
  reviewTicketId: string | null;
  createdAt: string;
  details: string[];
};

export type ChatResponse = {
  message: string;
  requests: RequestRecord[];
  sources: Array<{ type: string; label: string; identifier: string }>;
  actions: Array<{
    type: string;
    view: "requests" | "reviewQueue";
    requestId?: string;
    reviewTicketId?: string;
  }>;
  selectedRequestId?: string;
  selectedReviewTicketId?: string;
};

export type ChatMessage = {
  role: "user" | "assistant";
  text: string;
  response?: ChatResponse;
};
