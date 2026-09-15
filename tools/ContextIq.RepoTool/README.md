# Context IQ repository tool

`context-iq-repo` discovers read-only API operations and UI routes, generates
React/.NET onboarding scaffolding, and derives test scenarios from recent
first-parent branch history.

```powershell
context-iq-repo analyze C:\path\to\product
context-iq-repo generate C:\path\to\product --output C:\path\to\product\.context-iq
context-iq-repo pr-scenarios C:\path\to\product --branch main --since-days 30
```

Generated output must be reviewed. The tool does not execute repository code,
expose mutation operations, or replace product authorization. GET operations and
clearly named read-only POST search/filter/lookup/query endpoints are eligible.
