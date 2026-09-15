import { fireEvent, render, screen } from "@testing-library/react";
import { expect, test, vi } from "vitest";
import App from "./App";

vi.stubGlobal("fetch", vi.fn(async () => ({
  ok: true,
  json: async () => ({
    message: "Found 1 recent request created by alex.",
    requests: [{
      requestId: "REQ-1042",
      type: "Access",
      title: "Add analytics workspace access",
      requester: "alex",
      status: "Submitted",
      stage: "SecurityReview",
      reviewTicketId: "REV-8421",
      createdAt: new Date().toISOString(),
      details: [],
    }],
    sources: [{ type: "requestSearch", label: "Request search", identifier: "alex" }],
    actions: [{ type: "openView", view: "requests" }],
  }),
})));

test("opens Context IQ and runs a recent request query", async () => {
  render(<App />);

  fireEvent.click(screen.getByRole("button", { name: "Open Context IQ" }));
  fireEvent.click(screen.getByRole("button", { name: "Show my recent requests" }));
  fireEvent.click(screen.getByRole("button", { name: "Send" }));

  expect(await screen.findByText("Found 1 recent request created by alex.")).toBeInTheDocument();
  expect(screen.getByText("REQ-1042")).toBeInTheDocument();
  expect(screen.getByText(/Source: Request search/)).toBeInTheDocument();
});
