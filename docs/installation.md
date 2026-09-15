# Installation and repository onboarding

Context IQ can be used as a reference application, a local .NET tool, or a
repository-scaffolding step.

## Prerequisites

- Git
- .NET 8 SDK
- Node.js 20 or later
- An authenticated Git remote when recent branch history must be fetched

## 1. Clone and validate

```powershell
git clone https://github.com/MonaDevAI/context-iq.git
cd context-iq
dotnet test server\ContextIq.sln

cd client
npm install
npm test
npm run build
cd ..
```

## 2. Run the reference application

Use two terminals:

```powershell
dotnet run --project server\ContextIq.Api
```

```powershell
cd client
npm run dev
```

Open `http://localhost:5173`.

## 3. Choose installation branding

Copy `client\.env.example` to `client\.env`:

```dotenv
VITE_PRODUCT_NAME=Service Workspace
VITE_ASSISTANT_NAME=Workspace Assistant
```

Restart or rebuild the client after changing these values.

## 4. Analyze another repository

Analysis does not write to the target repository:

```powershell
dotnet run --project tools\ContextIq.RepoTool -- analyze C:\path\to\product
```

## 5. Generate integration scaffolding

```powershell
dotnet run --project tools\ContextIq.RepoTool -- generate C:\path\to\product `
  --output C:\path\to\product\.context-iq
```

Review the generated manifest before copying the React and .NET files into the
product. Keep authentication, authorization, data classification, and auditing
in product-owned backend code.

## 6. Generate recent-branch test scenarios

```powershell
dotnet run --project tools\ContextIq.RepoTool -- pr-scenarios C:\path\to\product `
  --branch main --since-days 30 `
  --output C:\path\to\product\.context-iq\pr-scenarios
```

Use `develop` instead of `main` when that is the product's integration branch.

## One-command onboarding

The installation helper runs generation and branch analysis together:

```powershell
.\scripts\install-context-iq.ps1 `
  -TargetRepository C:\path\to\product `
  -Branch main `
  -ProductName "Service Workspace" `
  -AssistantName "Workspace Assistant"
```

It writes only under the target repository's `.context-iq` directory. Existing
non-empty output is protected unless `-Force` is explicitly supplied.

## Preview the generated dashboard

```powershell
dotnet run --project tools\ContextIq.RepoTool -- serve C:\path\to\product\.context-iq
```

Open `http://127.0.0.1:4318`. The dashboard shows discovered API operations,
read-only classifications, UI routes, and suggested queries. It reads only the
generated manifest and does not execute the target repository.

## Integration checklist

1. Review every discovered operation and remove false positives.
2. Approve only read-only queries required by the product.
3. Implement typed handlers against product-owned APIs.
4. Enforce authorization and data filtering in the backend.
5. Add the generated prompt panel to the product shell.
6. Connect navigation actions to validated product routes.
7. Run unit, API, integration, and browser tests.
8. Review scenarios generated from recent branch changes before release.
