# PortalCraft AI

PortalCraft AI is a complete, generic reference implementation of governed conversational assistance for a React application backed by a .NET API.

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

- [Hackathon installation, live workflow, and architecture demo](video/PortalCraft-AI-Hackathon-Demo.mp4)
- [Hackathon demo captions](video/PortalCraft-AI-Hackathon-Demo.srt)
- [Live generic PortalCraft AI demo](video/PortalCraft-AI-Generic-Live-Demo.mp4)
- [English captions](video/PortalCraft-AI-Generic-Live-Demo.srt)
- [Demo script](docs/demo-script.md)
- [Architecture](docs/architecture.md)
