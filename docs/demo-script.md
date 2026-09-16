# Generic demo script: recent request to review queue

This journey uses illustrative identifiers and read-only actions.

The default installation uses **PortalCraft AI**. A deployment can set
`VITE_ASSISTANT_NAME=Workspace Assistant` or another product-owned name without
changing the reusable workflows.

## Scene 1: Find recent requests

**User:** I want to see my recent requests.

**Expected behavior:**

- Resolve the signed-in user.
- Search recent requests created by that user.
- Show request ID, request type, status, and review ticket when available.
- Ground the answer in the request-search API.

Repeat with:

> Show requests created by Alex.

## Scene 2: Open a request in search

**User:** Show request REQ-1042.

**Expected behavior:**

- Match the request returned in the prior turn.
- Prepare a validated product route and filter.
- Ask for confirmation before navigation.
- Open the correct request-search list with the result visible.

## Scene 3: Continue with the review ticket

**User:** List the review ticket for this request.

**Expected behavior:**

- Carry forward the selected request.
- Return `REV-8421` from grounded request data.
- Clearly report when a selected request has no review ticket.

## Scene 4: Locate the ticket in the review queue

**User:** Show this review ticket in the review queue.

**Expected behavior:**

- Resolve “this review ticket” to `REV-8421`.
- Open or filter the correct review queue.
- Assert that the visible row contains the same ticket and request.
- Stop before approve, reject, submit, delete, or discard.

## Playwright acceptance checks

- Natural-language variants resolve to the same registered workflow.
- Follow-up turns preserve the selected request and review ticket.
- Request-search and review-queue filters are visible and correct.
- Displayed identifiers come from grounded API data.
- Missing data produces an explicit message rather than a fabricated value.
- No state-changing control is invoked.
