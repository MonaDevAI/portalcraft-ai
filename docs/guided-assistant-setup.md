# Guided assistant setup

PortalCraft AI should guide a product owner from repository discovery to a
validated assistant without requiring them to understand the generated code
first. The resulting integration combines a reusable generated assistant core
with small, product-owned adapters for authorization and data access.

## Setup journey

### 1. Discover the portal

Scan the selected React and ASP.NET Core source and present the discovered:

- application routes
- read-only API operations
- candidate lookup identifiers
- business entities
- excluded mutation operations

The product owner reviews this inventory before generation. PortalCraft does
not infer authorization from route or operation names.

### 2. Add reviewed knowledge

Select approved manuals and preview:

- parsed documents and topics
- source titles and links
- generated sample questions
- sections that could not be parsed

Only reviewed content becomes Assistant knowledge. Generated answers retain a
source citation.

### 3. Configure entities and navigation

For each supported business entity, collect:

- display name and aliases
- clarification group
- lookup identifiers and examples
- existing portal request route
- fixed route parameters
- highlighted-record action label

PortalCraft validates these values and generates safe internal navigation
actions.

### 4. Connect authorized data

Show each generated query contract beside the discovered API evidence. The
product owner selects or implements the approved read-only executor.

PortalCraft generates parameter validation, command parsing, result formatting,
and safe actions. The host product remains responsible for authentication,
authorization, data sensitivity, and API execution.

### 5. Define safety boundaries

Record:

- required roles and authentication
- prohibited mutations
- approved non-production environments
- confirmation requirements
- logging and evidence restrictions

Missing security information remains an explicit integration blocker rather
than being guessed.

### 6. Choose the experience

Configure:

- Assistant name
- PortalCraft or host-default icon
- greeting and capability description
- supported modes
- suggested prompts
- answer formatting
- clarification behavior

The generated branding contract can be rendered in the launcher and Assistant
header while preserving accessible labels.

### 7. Provide approved answer examples

Import reviewed question-and-answer examples when a product needs a particular
answer style. PortalCraft uses them as semantic and formatting acceptance
examples, not exact-string responses.

Generated answers should:

1. lead with the direct answer;
2. synthesize only reviewed document facts or authorized query results;
3. use concise Markdown when it improves readability;
4. ask a focused clarification when required facts are missing;
5. retain the source citation; and
6. avoid copying an entire document section when a shorter answer is sufficient.

### 8. Generate and validate

Generate:

- the Assistant configuration and typed integration module
- query contracts and product-adapter stubs
- branding metadata
- grounded help topics and citations
- focused unit tests
- a natural-language question bank
- an integration report

The report classifies each item as:

- **Generated** — ready to integrate;
- **Connected** — backed by an approved product adapter;
- **Manual action required** — needs product-owned authorization, data mapping,
  authentication, or review.

Validation runs the question bank and records every question, rendered answer,
grounding source, status, and diagnostic. Failures are grouped by systemic
cause so shared routing or answer-composition improvements can address multiple
paraphrases.

## Runtime user guidance

The integrated Assistant should expose:

- a concise capability statement
- representative suggested prompts
- clarification choices for missing entity or identifier information
- direct, formatted, grounded answers
- visible source citations
- safe actions such as **Open highlighted request**
- explicit scope guidance for unsupported questions

The user should not need to understand generated commands, API operation IDs,
or adapter implementation details.

