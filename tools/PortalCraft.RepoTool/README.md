# PortalCraft AI repository tool

`portalcraft-ai-repo` discovers read-only API operations and UI routes, generates
React/.NET onboarding scaffolding, and derives test scenarios from recent
first-parent branch history.

```powershell
portalcraft-ai-repo analyze C:\path\to\product
portalcraft-ai-repo generate C:\path\to\product --output C:\path\to\product\.portalcraft-ai
portalcraft-ai-repo assistant C:\path\to\product --output C:\path\to\portalCraftAssistant.generated.ts
portalcraft-ai-repo pr-scenarios C:\path\to\product --branch main --since-days 30
portalcraft-ai-repo knowledge C:\path\to\product --branch main --since-days 180 --manuals-path docs\help
portalcraft-ai-repo serve C:\path\to\product\.portalcraft-ai
```

Generated output must be reviewed. The tool does not execute repository code,
expose mutation operations, or replace product authorization. GET operations and
clearly named read-only POST search/filter/lookup/query endpoints are eligible.

The `assistant` command writes a standalone TypeScript module containing the
discovered read-only query catalog, tagged portal views, and safe helpers for
building in-portal search and validator links.

The `knowledge` command accepts repeatable `--manuals-path` values for approved
repository-relative Markdown files or directories. It converts those manuals
into compact JSON, Markdown, and TypeScript catalogs with source paths. An
absolute HTTPS `sourceUrl` in YAML front matter is preserved as the manual's
external source link; optional `sourceTitle` supplies its user-facing citation
label.
