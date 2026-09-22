namespace PortalCraft.RepoTool;

public sealed record ApiOperation(
    string Id,
    string Method,
    string Route,
    string Source,
    bool ReadOnly,
    IReadOnlyList<string> Parameters);

public sealed record UiRoute(string Path, string Source);

public sealed record QueryCandidate(
    string Id,
    string Prompt,
    string OperationId,
    string? ViewPath,
    IReadOnlyList<string> Parameters);

public sealed record PortalCraftAssistantConfiguration
{
    public string AssistantName { get; init; } = "Portal Assistant";
    public string AssistantIcon { get; init; } = "portalcraft";
    public IReadOnlyList<PortalCraftLookupKey> LookupKeys { get; init; } = [];
    public IReadOnlyList<PortalCraftBusinessEntity> BusinessEntities { get; init; } = [];
}

public sealed record PortalCraftLookupKey
{
    public string Id { get; init; } = "";
    public string Label { get; init; } = "";
    public string Example { get; init; } = "";
    public IReadOnlyList<string> Aliases { get; init; } = [];
}

public sealed record PortalCraftBusinessEntity
{
    public string Id { get; init; } = "";
    public string Label { get; init; } = "";
    public string Group { get; init; } = "";
    public IReadOnlyList<string> Aliases { get; init; } = [];
    public string RequestRoute { get; init; } = "";
    public string? RequestActionLabel { get; init; }
    public IReadOnlyDictionary<string, string> RouteParameters { get; init; } =
        new Dictionary<string, string>();
}

public sealed record RepositoryProfile(
    string Repository,
    DateTimeOffset GeneratedAt,
    IReadOnlyList<ApiOperation> ApiOperations,
    IReadOnlyList<UiRoute> UiRoutes,
    IReadOnlyList<QueryCandidate> QueryCandidates,
    IReadOnlyList<string> Warnings);

public sealed record GenerationResult(
    RepositoryProfile Profile,
    string OutputDirectory,
    IReadOnlyList<string> Files);

public sealed record PullRequestChange(
    string Commit,
    string? PullRequestNumber,
    string Title,
    string Author,
    DateTimeOffset MergedAt,
    IReadOnlyList<string> ChangedFiles);

public sealed record TestScenario(
    string Id,
    string Priority,
    string Category,
    string Title,
    string Rationale,
    IReadOnlyList<string> Evidence);

public sealed record PullRequestScenarioReport(
    string Repository,
    string Branch,
    int SinceDays,
    IReadOnlyList<PullRequestChange> Changes,
    IReadOnlyList<TestScenario> Scenarios,
    IReadOnlyList<string> Warnings);

public sealed record RepositoryDocument(
    string Path,
    string Title,
    string Summary,
    string? SourceTitle,
    string? SourceUrl,
    IReadOnlyList<RepositoryDocumentSection> Sections);

public sealed record RepositoryDocumentSection(
    string Title,
    string Summary);

public sealed record RepositoryChangeInsight(
    string Commit,
    string? PullRequestNumber,
    string Category,
    string Title,
    DateTimeOffset ChangedAt,
    IReadOnlyList<string> ChangedFiles);

public sealed record RepositoryKnowledge(
    string Repository,
    string Branch,
    DateTimeOffset GeneratedAt,
    IReadOnlyList<RepositoryDocument> Documents,
    IReadOnlyList<RepositoryChangeInsight> Changes,
    IReadOnlyList<string> Warnings);

public sealed record CopilotStudioPackageResult(
    string ProductName,
    string AssistantName,
    string OutputDirectory,
    IReadOnlyList<string> Files,
    IReadOnlyList<string> Warnings);
