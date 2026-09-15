namespace ContextIq.RepoTool;

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
