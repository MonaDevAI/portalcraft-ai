import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, expect, test, vi } from "vitest";
import App from "./App";

afterEach(cleanup);

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

test("opens PortalCraft AI and runs a recent request query", async () => {
  render(<App />);

  expect(screen.getByAltText("PortalCraft AI Assistant")).toBeInTheDocument();
  fireEvent.click(screen.getByRole("button", { name: "Open PortalCraft AI" }));
  fireEvent.click(screen.getByRole("button", { name: "Show my recent requests" }));
  fireEvent.click(screen.getByRole("button", { name: "Send" }));

  expect(await screen.findByText("Found 1 recent request created by alex.")).toBeInTheDocument();
  expect(screen.getByText("REQ-1042")).toBeInTheDocument();
  expect(screen.getByText(/Source: Request search/)).toBeInTheDocument();
});

test("authors assistant requirements and previews generator-compatible JSON", () => {
  render(<App />);

  fireEvent.click(screen.getByRole("button", { name: "Assistant setup" }));
  fireEvent.change(screen.getByLabelText("Assistant name"), {
    target: { value: "FMDM Assistant" },
  });
  fireEvent.change(screen.getByLabelText("Business entity 1 ID"), {
    target: { value: "ProductUnit" },
  });
  fireEvent.change(screen.getByLabelText("Business entity 1 label"), {
    target: { value: "Product Unit" },
  });
  fireEvent.change(screen.getByLabelText("Business entity 1 group"), {
    target: { value: "Product hierarchy" },
  });
  fireEvent.change(screen.getByLabelText("Business entity 1 aliases"), {
    target: { value: "product unit" },
  });
  fireEvent.change(screen.getByLabelText("Business entity 1 route"), {
    target: { value: "/search-product-hierarchy-requests" },
  });
  fireEvent.change(screen.getByLabelText("Business entity 1 route parameters"), {
    target: { value: "hierarchyEntity=ProductUnit" },
  });

  const preview = screen.getByLabelText("Assistant configuration JSON");
  expect(preview).toHaveTextContent('"assistantName": "FMDM Assistant"');
  expect(preview).toHaveTextContent('"validatorCRNumber"');
  expect(preview).toHaveTextContent('"requestRoute": "/search-product-hierarchy-requests"');
  expect(preview).toHaveTextContent('"hierarchyEntity": "ProductUnit"');
  expect(screen.getByRole("button", { name: "Download JSON" })).toBeEnabled();
});
