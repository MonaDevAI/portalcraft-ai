# PortalCraft AI

![PortalCraft AI logo](assets/portalcraft-ai-logo.svg)

PortalCraft AI is a governed agent framework for safely assisting users inside enterprise
portals. Generic copilots can answer questions about a portal, but they cannot reliably act
inside one: they do not understand the controls currently visible to a user, asynchronously
loaded pickers, validation state, or hierarchy-dependent required fields. PortalCraft AI
closes that gap without giving a language model direct access to the DOM, databases, or
systems of record.

## Four purpose-built modes

| Mode | Responsibility |
|---|---|
| **UI Simulation** | Operates approved visible controls, grids, tabs, pickers, and validation flows through structured UI commands. |
| **API Assistant** | Creates authenticated draft requests through product-owned APIs and never submits them automatically. |
| **Query** | Answers operational and business questions through deterministic, read-only tools without requiring a model. |
| **Access** | Explains roles, approver groups, authorization requirements, and existing access-request paths on demand. |

## Architecture is the innovation

The language model can propose only structured commands. Every command must pass server and
client policy gates before execution. An explicit allowlist blocks submission, deletion,
rejection, scripts, and arbitrary API calls. Consequential actions require separate human
confirmation. Client and server privacy filters minimize sensitive page context, audit
contracts use pseudonymous identifiers, and rate limits plus a kill switch bound the blast
radius.

The reusable core—tool registry, workflow runner, mode framework, confirmation policy,
privacy sanitizer, and audit contracts—is separated from portal-specific routes, fields,
APIs, and business rules. Adopting another React and .NET portal therefore means supplying
new adapters rather than building another agent. The roadmap advances from this reusable
portal reference implementation to a product pilot and then to a Copilot-hosted,
cross-surface capability.

The sample product is **Operations Hub**. It uses synthetic service requests and demonstrates:

- “Show my recent requests”
- “Show requests created by Alex”
- request details and grounded source metadata
- conversational continuity from a request to its review ticket
- opening a review ticket in a read-only review queue
- explicit handling when a request has no review ticket
- typed, deterministic intent routing instead of unrestricted model output

No product source, internal URLs, credentials, or production data are included.

## Repository structure

```text
client/                 React + TypeScript application
server/PortalCraft.Api/   ASP.NET Core API
server/PortalCraft.Tests/ xUnit tests
docs/                   architecture and demo script
video/                  product-neutral demo and captions
scripts/                local startup helpers
tools/PortalCraft.RepoTool/ repository discovery and integration generator
```

## Onboard another repository

The repository tool scans React/JavaScript and ASP.NET Core source, inventories UI routes
and API operations, proposes read-only PortalCraft AI queries, and generates separate React
and .NET integration scaffolding:

```powershell
dotnet run --project tools\PortalCraft.RepoTool -- analyze C:\path\to\product

dotnet run --project tools\PortalCraft.RepoTool -- generate C:\path\to\product `
  --output C:\path\to\product\.portalcraft-ai
```

Generated output includes:

- `portalcraft-ai.manifest.json` with discovered evidence and query candidates
- `server\PortalCraftGeneratedCatalog.cs` with a typed catalog API
- `client\portalCraft.generated.ts` with typed query descriptors
- `client\PortalCraftGeneratedPanel.tsx` with reusable prompt UI
- `dashboard\index.html` with an immediately runnable repository dashboard
- integration instructions and safety checks

Discovered `GET` operations and clearly named read-only POST searches (`search`, `filter`,
`lookup`, or `query`) become query candidates. Create, update, submit, approve, reject,
delete, generate, upload, and import operations are never exposed automatically. The
generator does not modify product source and refuses to overwrite a non-empty output
directory unless `--force` is supplied.

The tool can also be packed and installed:

```powershell
dotnet pack tools\PortalCraft.RepoTool -c Release
dotnet tool install --global --add-source tools\PortalCraft.RepoTool\bin\Release PortalCraft.RepoTool
portalcraft-ai-repo analyze C:\path\to\product
```

For end-to-end setup, branding, generation, and integration checks, see
[Installation and repository onboarding](docs/installation.md). A one-command
PowerShell helper is also available:

```powershell
.\scripts\install-portalcraft-ai.ps1 `
  -TargetRepository C:\path\to\product `
  -Branch main `
  -ProductName "Service Workspace" `
  -AssistantName "Workspace Assistant"
```

Then preview the generated repository dashboard:

```powershell
dotnet run --project tools\PortalCraft.RepoTool -- serve C:\path\to\product\.portalcraft-ai
```

Open `http://127.0.0.1:4318`.

### Generate scenarios from recent PRs

The tool can inspect first-parent history on `develop`, `main`, or an explicitly selected
branch and derive test scenarios from the changed UI, API, security, data, and test surfaces:

```powershell
dotnet run --project tools\PortalCraft.RepoTool -- pr-scenarios C:\path\to\product `
  --branch develop --since-days 30 --limit 50 `
  --output C:\path\to\product\.portalcraft-ai\pr-scenarios
```

It writes both `pr-test-scenarios.json` and `pr-test-scenarios.md`. Merge commits and
squash commits with PR numbers are labeled as PRs; other first-parent commits are retained
as commit-level fallbacks so recent behavior is not silently omitted.

### Generate application and change knowledge

PortalCraft AI can turn reviewed repository documentation and recent branch history into
a build-time knowledge package for an adopting assistant:

```powershell
dotnet run --project tools\PortalCraft.RepoTool -- knowledge C:\path\to\product `
  --branch develop --since-days 180 --limit 100 `
  --output C:\path\to\product\.portalcraft-ai\knowledge
```

The command writes JSON, Markdown, and a typed TypeScript module containing documentation
summaries plus categorized bug fixes, enhancements, security changes, and other commits.
It reads repository files and Git history without executing product code. Teams must review
the generated package before integration and continue to use authorized product APIs for
live business data.

## Run locally

Requirements:

- Node.js 20 or later
- .NET 8 SDK

From two terminals:

```powershell
dotnet run --project server\PortalCraft.Api
```

```powershell
cd client
npm install
npm run dev
```

Open http://localhost:5173. The client proxies `/api` to http://localhost:5080.

Alternatively:

```powershell
.\scripts\start-local.ps1
```

### Installation branding

Copy `client\.env.example` to `client\.env` and set the product and assistant names:

```dotenv
VITE_PRODUCT_NAME=Service Workspace
VITE_ASSISTANT_NAME=Workspace Assistant
```

The defaults remain `Operations Hub` and `PortalCraft AI`. These values are compiled into the
frontend, so restart the development server or rebuild after changing them.

## Validate

```powershell
dotnet test server\PortalCraft.sln

cd client
npm install
npm test
npm run build
```

## Safety model

- The sample tools are read-only.
- The server validates and routes supported intents.
- Responses include typed source metadata.
- Navigation uses application-owned identifiers.
- Missing identifiers trigger clarification.
- Missing review-ticket values are never invented.
- Approval, rejection, submission, deletion, and other mutations are intentionally absent.

## Demo

- [Live generic PortalCraft AI demo](video/PortalCraft-AI-Generic-Live-Demo.mp4)
- [English captions](video/PortalCraft-AI-Generic-Live-Demo.srt)
- [Demo script](docs/demo-script.md)
- [Architecture](docs/architecture.md)
