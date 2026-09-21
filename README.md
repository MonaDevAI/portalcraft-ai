# PortalCraft AI

![PortalCraft AI logo](assets/portalcraft-ai-logo.svg)

PortalCraft AI is a governed integration framework for adding portal-aware assistance to
existing React and .NET applications. Generic copilots can describe a portal, but they do
not automatically understand its routes, API contracts, visible workflows, authorization
boundaries, or recent product changes. PortalCraft AI creates that missing application
context without giving a model direct repository, database, or system-of-record access.

## What PortalCraft AI provides

- repository discovery for React routes and ASP.NET Core operations
- safe query candidates derived only from read-only operations
- generated React and .NET integration scaffolding in an isolated output directory
- deterministic, typed workflows with grounded source metadata and validated navigation
- application knowledge generated from reviewed documentation and recent commit history
- bug-fix and enhancement summaries that can improve assistant guidance
- regression scenarios derived from recent branch and pull-request changes
- explicit exclusion of create, update, submit, approve, reject, delete, upload, and import
  operations from automatic query generation

The reusable discovery, tool, workflow, knowledge, and generation layers are separated from
portal-specific routes, entities, APIs, and authorization rules. Adopting another portal
therefore means reviewing generated evidence and supplying product-owned adapters rather
than building a new assistant from scratch. Products can add UI automation, authenticated
draft operations, access guidance, or model-backed orchestration behind their own policy
and confirmation boundaries; those capabilities are not presented as prebuilt modes.

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

dotnet run --project tools\PortalCraft.RepoTool -- assistant C:\path\to\product `
  --manuals-path docs\help `
  --output C:\path\to\product\src\portalCraftAssistant.generated.ts
```

Generated output includes:

- `portalcraft-ai.manifest.json` with discovered evidence and query candidates
- `server\PortalCraftGeneratedCatalog.cs` with a typed catalog API
- `client\portalCraft.generated.ts` with typed query descriptors
- `client\portalCraftAssistant.generated.ts` with safe, discovered in-portal route helpers
  plus reusable greetings, capability answers, scope instructions, and optional reviewed
  help-document topics with generic question parsing, semantic matching, and clickable citations
- `client\PortalCraftGeneratedPanel.tsx` with reusable prompt UI
- `dashboard\index.html` with an immediately runnable repository dashboard
- integration instructions and safety checks

When `assistant` receives one or more `--manuals-path` values, it also emits canonical
`explain help topic <topic-id>` commands. A product's existing model can translate
paraphrased questions to those allowlisted commands, while the generated module returns
only reviewed document text and source citations. It also includes deterministic local
matching for applications that do not use a model.

Generated assistants expose `answerPortalCraftConversation` for greetings, thanks, and
capability questions. `answerPortalCraftHelpQuestion` and
`portalCraftHelpQuestionPatterns` parse common document questions such as “What does X
mean?”, “Explain X”, and “X definition” without product-specific routing tables.
`portalCraftAssistantScopeInstruction` keeps model fallbacks within discovered read-only
portal queries and reviewed documentation.

### Set up a reviewed help-manual assistant

1. Place approved Markdown manuals under the target repository, with optional
   `sourceTitle` and HTTPS `sourceUrl` YAML metadata for citations.
2. Run the `assistant` command with one or more `--manuals-path` values.
3. Review the parsed document and topic counts plus the sample questions printed by the
   command.
4. Use the generated `portalCraftAssistantSetup` and
   `portalCraftAssistantSampleQuestions` exports to show users the supported question
   types and document-derived starters.
5. Call `answerPortalCraftConversation` first, then
   `answerPortalCraftHelpQuestion`, and use the constrained model with
   `portalCraftAssistantScopeInstruction` only when deterministic parsing does not match.

Portal-specific lookup requirements can be supplied with `--assistant-config`:

```powershell
portalcraft-ai-repo assistant C:\src\portal `
  --manuals-path docs\help `
  --assistant-config docs\help\portalcraft-assistant.json `
  --output src\components\Assistant\portalCraftAssistant.generated.ts `
  --force
```

The JSON configuration declares the assistant name, supported lookup keys (for
example Request ID and Validator CR), business-entity groups, existing portal request
routes, and fixed route parameters such as a hierarchy entity. PortalCraft generates
`answerPortalCraftLookupClarification` for incomplete lookup prompts and
`buildPortalCraftRequestAction` for safe highlighted-request links. The product still
registers explicit authorized read-only API executors; this configuration does not
grant API access.

The PortalCraft reference UI also includes an **Assistant setup** workspace for
authoring this file without editing JSON manually. It supports repeated lookup keys and
business entities, aliases, entity groups, existing portal routes, fixed route
parameters, validation, JSON import, preview, copy, and download. The downloaded
`portalcraft-assistant.json` uses the same schema consumed by `--assistant-config`.

This setup parses headings and section text from every approved manual path. A portal does
not need separate PSA, OLS, Pool, or other product-specific definition routes; common
“What does X mean?”, “Explain X”, and “X definition” questions are resolved against the
generated reviewed topics.

For API-backed questions, PortalCraft generates `portalCraftQueryCommandDescription`,
`findPortalCraftAssistantQuery`, `parsePortalCraftQueryCommand`, and
`executePortalCraftQueryCommand`. The model converts natural language into an allowlisted
`run portal query <query-id> with <json>` command; PortalCraft validates the query and its
parameters before calling a product-owned executor. The portal supplies only the
authorized operation adapters that fetch its data, while query phrasing and parsing stay
shared.

Product executors can return Copilot-style Markdown through
`formatPortalCraftQueryResult`, including a concise summary, labeled fields, detail
sections, and safe internal or HTTPS action links such as “Open highlighted request.”

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
  --manuals-path docs\help `
  --output C:\path\to\product\.portalcraft-ai\knowledge
```

The command writes JSON, Markdown, and a typed TypeScript module containing documentation
summaries plus categorized bug fixes, enhancements, security changes, and other commits.
Repeat `--manuals-path` to restrict documentation ingestion to approved repository-relative
manual files or directories. When omitted, PortalCraft retains its repository-wide Markdown
discovery behavior. Paths outside the repository and non-Markdown manual files are rejected.
Manuals may declare an approved external source link in YAML front matter:

```markdown
---
sourceTitle: Product help manual
sourceUrl: https://contoso.sharepoint.com/sites/product/manual
---
```

`sourceTitle` controls the user-facing citation label. Only absolute HTTPS links are
retained in generated knowledge and invalid links fail generation.
It reads repository files and Git history without executing product code. Teams must review
the generated package before integration and continue to use authorized product APIs for
live business data.

### Generate a Copilot Studio assistant package

PortalCraft AI can combine repository discovery and reviewed knowledge into a Copilot Studio
onboarding package:

```powershell
dotnet run --project tools\PortalCraft.RepoTool -- copilot-studio C:\path\to\product `
  --product-name "Product Portal" `
  --assistant-name "Product Assistant" `
  --api-base-url https://product-api.contoso.com `
  --branch develop `
  --manuals-path docs\help `
  --output C:\path\to\product\.portalcraft-ai\copilot-studio
```

The package contains:

- natural-language agent instructions that treat paraphrases and business synonyms as the
  same intent when supported by evidence
- an OpenAPI document containing only discovered read-only query candidates
- a knowledge-source manifest for approved SharePoint links and repository manuals
- secure custom-application channel settings for a server-side token broker
- an import and product-review checklist

The generator does not publish to a Power Platform environment or create credentials.
Product owners must review API contracts, configure Microsoft Entra ID authentication,
add approved knowledge sources, test permissions, and publish the agent in Copilot Studio.

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
