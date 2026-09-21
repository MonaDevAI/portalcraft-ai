using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PortalCraft.RepoTool;

public sealed partial class CopilotStudioPackageBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public CopilotStudioPackageResult Write(
        RepositoryProfile profile,
        RepositoryKnowledge knowledge,
        string outputDirectory,
        string productName,
        string assistantName,
        string? apiBaseUrl,
        bool force)
    {
        productName = RequireName(productName, nameof(productName));
        assistantName = RequireName(assistantName, nameof(assistantName));
        var output = Path.GetFullPath(outputDirectory);
        if (Directory.Exists(output)
            && Directory.EnumerateFileSystemEntries(output).Any()
            && !force)
        {
            throw new InvalidOperationException(
                $"Output directory is not empty: {output}. Pass --force to replace generated files.");
        }

        var warnings = new List<string>
        {
            "Copilot Studio import and runtime behavior are not validated by PortalCraft. " +
            "The project does not currently have a licensed Copilot Studio environment; " +
            "treat this package as a review-only preview until a licensed product team imports and tests it."
        };
        var baseUri = ResolveApiBaseUri(apiBaseUrl, warnings);
        var approvedOperations = SelectApprovedOperations(profile);
        if (approvedOperations.Count == 0)
        {
            warnings.Add(
                "No reviewed read-only query operations were discovered. Add product-owned actions before importing.");
        }
        if (knowledge.Documents.Count == 0)
        {
            warnings.Add(
                "No approved knowledge documents were found. Add SharePoint sources or upload reviewed manuals.");
        }

        Directory.CreateDirectory(output);
        var files = new List<string>();
        WriteFile(output, "agent-instructions.md", BuildAgentInstructions(
            productName,
            assistantName,
            approvedOperations,
            knowledge), files);
        WriteFile(output, "actions.openapi.json", BuildOpenApi(
            productName,
            assistantName,
            baseUri,
            approvedOperations,
            profile.QueryCandidates), files);
        WriteFile(output, "knowledge-sources.json", JsonSerializer.Serialize(
            new
            {
                productName,
                assistantName,
                generatedAt = DateTimeOffset.UtcNow,
                repositoryBranch = knowledge.Branch,
                sources = knowledge.Documents.Select(document => new
                {
                    document.Title,
                    document.SourceTitle,
                    document.SourceUrl,
                    repositoryPath = document.Path,
                    sections = document.Sections.Select(section => section.Title)
                }),
                guidance = new
                {
                    retrieval = "semantic",
                    answerFromApprovedSourcesOnly = true,
                    requireCitations = true,
                    respectUserPermissions = true
                }
            },
            JsonOptions), files);
        WriteFile(output, "portal-integration.json", JsonSerializer.Serialize(
            new
            {
                productName,
                assistantName,
                channel = "customApplication",
                client = "Microsoft 365 Agents SDK or Bot Framework Web Chat",
                tokenBrokerRoute = "/api/portalcraft-ai/copilot-token",
                tokenEndpointEnvironmentVariable = "COPILOT_STUDIO_TOKEN_ENDPOINT",
                security = new
                {
                    exposeSecretsToBrowser = false,
                    useShortLivedConversationTokens = true,
                    authenticatePortalUser = true,
                    enforceAuthorizationInProductApis = true
                }
            },
            JsonOptions), files);
        WriteFile(output, "README.md", BuildReadme(
            productName,
            assistantName,
            approvedOperations.Count,
            knowledge.Documents.Count,
            warnings), files);

        return new(productName, assistantName, output, files, warnings);
    }

    private static IReadOnlyList<ApiOperation> SelectApprovedOperations(
        RepositoryProfile profile)
    {
        var candidateIds = profile.QueryCandidates
            .Select(candidate => candidate.OperationId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return profile.ApiOperations
            .Where(operation => operation.ReadOnly && candidateIds.Contains(operation.Id))
            .OrderBy(operation => operation.Route, StringComparer.OrdinalIgnoreCase)
            .ThenBy(operation => operation.Method, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static Uri ResolveApiBaseUri(
        string? apiBaseUrl,
        ICollection<string> warnings)
    {
        if (string.IsNullOrWhiteSpace(apiBaseUrl))
        {
            warnings.Add(
                "The generated OpenAPI document uses a placeholder API URL. Replace it before importing.");
            return new Uri("https://replace-with-portal-api.example/");
        }
        if (!Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var uri)
            || !uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(uri.Host))
        {
            throw new ArgumentException("--api-base-url must be an absolute HTTPS URL.");
        }
        return uri;
    }

    private static string BuildOpenApi(
        string productName,
        string assistantName,
        Uri baseUri,
        IReadOnlyList<ApiOperation> operations,
        IReadOnlyList<QueryCandidate> candidates)
    {
        var prompts = candidates
            .GroupBy(candidate => candidate.OperationId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Select(candidate => candidate.Prompt).Distinct().ToArray(),
                StringComparer.OrdinalIgnoreCase);
        var paths = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        foreach (var routeGroup in operations.GroupBy(operation => operation.Route))
        {
            var methods = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (var operation in routeGroup)
            {
                var routeParameters = RouteParameterPattern()
                    .Matches(operation.Route)
                    .Select(match => match.Groups["parameter"].Value)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                var parameters = operation.Parameters
                    .Concat(routeParameters)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Select(parameter => new
                    {
                        name = parameter,
                        @in = routeParameters.Contains(parameter) ? "path" : "query",
                        required = routeParameters.Contains(parameter),
                        schema = new { type = "string" },
                        description = $"Reviewed {Humanize(parameter)} value."
                    })
                    .ToArray();
                var summaries = prompts.GetValueOrDefault(operation.Id) ?? [];
                methods[operation.Method.ToLowerInvariant()] = new Dictionary<string, object?>
                {
                    ["operationId"] = ToOperationId(operation.Id),
                    ["summary"] = summaries.FirstOrDefault() ?? $"Query {Humanize(operation.Id)}",
                    ["description"] = BuildOperationDescription(operation, summaries),
                    ["parameters"] = parameters,
                    ["responses"] = new Dictionary<string, object>
                    {
                        ["200"] = new
                        {
                            description = "Successful authorized response.",
                            content = new Dictionary<string, object>
                            {
                                ["application/json"] = new
                                {
                                    schema = new
                                    {
                                        type = "object",
                                        additionalProperties = true
                                    }
                                }
                            }
                        },
                        ["400"] = new { description = "Invalid or incomplete input." },
                        ["401"] = new { description = "Authentication required." },
                        ["403"] = new { description = "The user is not authorized for this data." }
                    },
                    ["x-portalcraft-read-only"] = true,
                    ["x-portalcraft-source"] = operation.Source
                };
            }
            paths[routeGroup.Key] = methods;
        }

        var basePath = baseUri.AbsolutePath.TrimEnd('/');
        var serverUrl = $"{baseUri.Scheme}://{baseUri.Authority}{basePath}";
        return JsonSerializer.Serialize(
            new Dictionary<string, object?>
            {
                ["openapi"] = "3.0.1",
                ["info"] = new
                {
                    title = $"{assistantName} read-only actions",
                    version = "1.0.0",
                    description =
                        $"PortalCraft-generated, review-required actions for {productName}. " +
                        "Product APIs remain the authentication and authorization boundary."
                },
                ["servers"] = new[] { new { url = serverUrl } },
                ["paths"] = paths,
                ["x-portalcraft"] = new
                {
                    generated = true,
                    reviewRequired = true,
                    authentication =
                        "Configure Microsoft Entra ID in Copilot Studio before enabling these actions."
                }
            },
            JsonOptions);
    }

    private static string BuildOperationDescription(
        ApiOperation operation,
        IReadOnlyList<string> prompts)
    {
        var builder = new StringBuilder(
            "Use only for a read-only query after extracting and confirming required values.");
        if (prompts.Count > 0)
        {
            builder.Append(" Example user requests: ");
            builder.Append(string.Join("; ", prompts));
            builder.Append('.');
        }
        builder.Append(" Never infer identifiers or bypass product authorization.");
        return builder.ToString();
    }

    private static string BuildAgentInstructions(
        string productName,
        string assistantName,
        IReadOnlyList<ApiOperation> operations,
        RepositoryKnowledge knowledge)
    {
        var actionLines = operations.Count == 0
            ? ["- No actions were generated. Add reviewed product actions before publishing."]
            : operations.Select(operation =>
                $"- `{ToOperationId(operation.Id)}`: {operation.Method} {operation.Route}").ToArray();
        var knowledgeLines = knowledge.Documents.Count == 0
            ? ["- No knowledge sources were generated."]
            : knowledge.Documents.Select(document =>
                $"- {document.SourceTitle ?? document.Title} ({document.SourceUrl ?? document.Path})").ToArray();
        return $"""
            # {assistantName} instructions

            You are the grounded assistant for {productName}.

            ## Natural-language behavior

            - Interpret meaning rather than requiring an exact trigger phrase.
            - Treat paraphrases and common business synonyms as the same intent when the evidence supports it.
            - Preserve identifiers, codes, dates, environments, hierarchy levels, and request types exactly.
            - Ask one concise clarification when a required value is missing or ambiguous.
            - Do not claim that a request, hierarchy record, role, or access assignment exists until a product action confirms it.

            ## Knowledge answers

            - Search only the approved knowledge sources configured for this agent.
            - Answer from the most relevant passages and include clickable citations.
            - Prefer the current document version and state when sources conflict.
            - If no verified passage supports the answer, say that no verified source was found.

            ## Product actions

            - Use the generated actions only for read-only queries.
            - Pass the signed-in user's identity through the configured authentication flow.
            - Never invent required parameters or retry with broader access.
            - Summarize returned data without exposing tokens, credentials, or hidden fields.
            - Any future create, update, submit, approve, reject, or delete action requires separate product review and explicit user confirmation.

            ## Generated read-only actions

            {string.Join(Environment.NewLine, actionLines)}

            ## Approved knowledge candidates

            {string.Join(Environment.NewLine, knowledgeLines)}
            """;
    }

    private static string BuildReadme(
        string productName,
        string assistantName,
        int actionCount,
        int knowledgeCount,
        IReadOnlyList<string> warnings)
    {
        var warningText = warnings.Count == 0
            ? "- None."
            : string.Join(Environment.NewLine, warnings.Select(warning => $"- {warning}"));
        return $"""
            # {assistantName} Copilot Studio package

            PortalCraft generated this review package for {productName}. It contains
            {actionCount} read-only action definitions and {knowledgeCount} knowledge candidates.
            It does not publish an agent or create tenant resources.

            > **Validation status: NOT VALIDATED IN COPILOT STUDIO.**
            > PortalCraft does not currently have access to a licensed Copilot Studio environment.
            > Generation tests verify file structure and safety filtering only; they do not prove
            > that this package imports successfully or behaves correctly at runtime.

            ## Import and configure

            1. Create an agent in Microsoft Copilot Studio and enable generative orchestration.
            2. Copy `agent-instructions.md` into the agent instructions.
            3. Add the approved SharePoint URLs from `knowledge-sources.json` as knowledge sources.
               Upload repository-only manuals separately or move them to an approved SharePoint library.
            4. Import `actions.openapi.json` as a REST API tool or custom connector.
            5. Configure Microsoft Entra ID authentication and validate every generated route,
               parameter location, response shape, and permission before enabling the tool.
            6. Publish the custom-application channel. Keep the token endpoint in the portal
               backend and use short-lived conversation tokens as described by `portal-integration.json`.
            7. Test paraphrases, missing parameters, unauthorized access, empty results, citations,
               and each action against a non-production environment before rollout.

            ## Required product work

            Generated discovery is evidence, not an API contract. Product owners must review:

            - API host and route prefixes
            - parameter locations and schemas
            - Entra scopes and delegated permissions
            - user-level authorization and data classification
            - response filtering and citation quality

            ## Warnings

            {warningText}
            """;
    }

    private static string RequireName(string value, string parameterName)
    {
        var trimmed = value.Trim();
        if (trimmed.Length == 0)
        {
            throw new ArgumentException($"{parameterName} is required.");
        }
        return trimmed;
    }

    private static string ToOperationId(string value)
    {
        var parts = Regex.Split(value, "[^A-Za-z0-9]+")
            .Where(part => part.Length > 0)
            .ToArray();
        if (parts.Length == 0)
        {
            return "portalCraftQuery";
        }
        return char.ToLowerInvariant(parts[0][0]) + parts[0][1..]
            + string.Concat(parts.Skip(1).Select(part =>
                char.ToUpperInvariant(part[0]) + part[1..]));
    }

    private static string Humanize(string value) =>
        Regex.Replace(value.Replace('-', ' ').Replace('_', ' '), "([a-z0-9])([A-Z])", "$1 $2")
            .Trim()
            .ToLowerInvariant();

    private static void WriteFile(
        string output,
        string relativePath,
        string content,
        ICollection<string> files)
    {
        var path = Path.Combine(output, relativePath);
        File.WriteAllText(path, content);
        files.Add(path);
    }

    [GeneratedRegex(@"\{(?<parameter>[^}:]+)(?::[^}]+)?\}")]
    private static partial Regex RouteParameterPattern();
}
