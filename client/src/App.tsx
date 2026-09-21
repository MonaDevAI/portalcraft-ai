import { useState } from "react";
import { AssistantConfig } from "./AssistantConfig";
import { PortalCraft } from "./PortalCraft";
import portalCraftLogo from "./assets/portalcraft-assistant-logo.png";
import { productName } from "./config";
import { RequestRecord } from "./types";
import "./styles.css";

export default function App() {
  const [view, setView] = useState<"dashboard" | "requests" | "reviewQueue" | "assistantSetup">("dashboard");
  const [visibleRequests, setVisibleRequests] = useState<RequestRecord[]>([]);
  const viewLabels = {
    dashboard: "Dashboard",
    requests: "Requests",
    reviewQueue: "Review queue",
    assistantSetup: "Assistant setup",
  } as const;

  return (
    <div className="app">
      <header className="topbar">
        <img className="brand-logo" src={portalCraftLogo} alt="PortalCraft AI Assistant" />
        <span className="product-context">{productName}</span>
        <em>DEMO</em>
        <div className="user">Alex Morgan <b>AM</b></div>
      </header>
      <div className="layout">
        <nav className="sidebar">
          <small>WORKSPACE</small>
          {(["dashboard", "requests", "reviewQueue", "assistantSetup"] as const).map(item => (
            <button
              className={view === item ? "active" : ""}
              key={item}
              onClick={() => setView(item)}
            >
              {viewLabels[item]}
            </button>
          ))}
        </nav>
        <main>
          <h1>{viewLabels[view]}</h1>
          <p className="subtitle">
            {view === "assistantSetup"
              ? "Configure reusable assistant lookup and portal-navigation requirements."
              : "Synthetic data for the standalone PortalCraft AI reference application."}
          </p>
          {view === "dashboard" && (
            <section className="metrics">
              <div><span>Open requests</span><strong>14</strong><small>Across all teams</small></div>
              <div><span>Pending validation</span><strong>6</strong><small>Read-only queue</small></div>
              <div><span>Average age</span><strong>1.8d</strong><small>Last 30 days</small></div>
            </section>
          )}
          {view !== "dashboard" && (
            view === "assistantSetup" ? <AssistantConfig /> :
            <section className="table-card">
              <h2>{view === "requests" ? "Request search results" : "Requests awaiting validation"}</h2>
              {visibleRequests.length === 0 ? (
                <p>Use PortalCraft AI to locate a request.</p>
              ) : (
                <table>
                  <thead>
                    <tr>
                      <th>Request</th><th>Title</th><th>Status</th><th>Stage</th><th>Review ticket</th>
                    </tr>
                  </thead>
                  <tbody>
                    {visibleRequests.map(request => (
                      <tr key={request.requestId}>
                        <td>{request.requestId}</td>
                        <td>{request.title}</td>
                        <td>{request.status}</td>
                        <td>{request.stage}</td>
                        <td>{request.reviewTicketId || "Not generated"}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
            </section>
          )}
        </main>
      </div>
      <PortalCraft onOpenView={(nextView, requests) => {
        setView(nextView);
        setVisibleRequests(requests);
      }} />
    </div>
  );
}
