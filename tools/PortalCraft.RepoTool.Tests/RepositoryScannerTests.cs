using PortalCraft.RepoTool;

namespace PortalCraft.RepoTool.Tests;

public sealed class RepositoryScannerTests : IDisposable
{
    private readonly string directory =
        Path.Combine(Path.GetTempPath(), $"portalcraft-ai-repo-tool-{Guid.NewGuid():N}");

    [Fact]
    public void DiscoversReadOnlyApisRoutesAndQueries()
    {
        Write(
            "Api/Program.cs",
            """
            app.MapGet("/api/requests/{requestId}", (string requestId, string? requester) => Results.Ok());
            app.MapPost("/api/requests", () => Results.Ok());
            app.MapPost("/api/requests/search", () => Results.Ok());
            app.MapPost("/api/requests/create", () => Results.Ok());
            """);
        Write(
            "client/routes.tsx",
            """
            export const routes = [{ path: "/requests" }];
            fetch("/api/review-queue");
            fetch("/api/requests", { method: "POST" });
            onOpenView("reviewQueue");
            """);

        var profile = new RepositoryScanner().Scan(directory);

        Assert.Contains(
            profile.ApiOperations,
            operation => operation.Method == "GET"
                && operation.Route == "/api/requests/{requestId}");
        Assert.Contains(
            profile.ApiOperations,
            operation => operation.Method == "POST" && !operation.ReadOnly);
        Assert.Contains(
            profile.ApiOperations,
            operation => operation.Route == "/api/requests/search" && operation.ReadOnly);
        Assert.DoesNotContain(
            profile.QueryCandidates,
            candidate => candidate.OperationId == "post-api-requests-create");
        Assert.Contains(
            profile.QueryCandidates,
            candidate => candidate.OperationId == "post-api-requests-search");
        Assert.Contains(profile.UiRoutes, route => route.Path == "/requests");
        Assert.Contains(profile.UiRoutes, route => route.Path == "/reviewQueue");
        Assert.DoesNotContain(
            profile.QueryCandidates,
            candidate => candidate.OperationId == "get-api-requests");
        Assert.All(
            profile.QueryCandidates,
            candidate => Assert.Contains(
                profile.ApiOperations,
                operation => operation.Id == candidate.OperationId && operation.ReadOnly));
        Assert.Contains(
            profile.QueryCandidates,
            candidate => candidate.Parameters.SequenceEqual(["requestId"])
                && candidate.ViewPath == "/requests");
        Assert.Single(
            profile.QueryCandidates,
            candidate => candidate.Prompt == "Find requests by request id");
        Assert.Contains(
            profile.QueryCandidates,
            candidate => candidate.Parameters.SequenceEqual(["requester"]));
        Assert.Contains(
            profile.QueryCandidates,
            candidate => candidate.OperationId == "get-api-review-queue"
                && candidate.ViewPath == "/reviewQueue");
    }

    [Fact]
    public void GeneratesSeparateReactAndDotNetScaffolding()
    {
        Write("Program.cs", """app.MapGet("/api/orders", () => Results.Ok());""");
        var profile = new RepositoryScanner().Scan(directory);
        var output = Path.Combine(directory, "generated");

        var result = new IntegrationGenerator().Generate(profile, output);

        Assert.Equal(6, result.Files.Count);
        Assert.True(File.Exists(Path.Combine(output, "portalcraft-ai.manifest.json")));
        Assert.Contains(
            "MapPortalCraftGeneratedCatalog",
            File.ReadAllText(Path.Combine(output, "server", "PortalCraftGeneratedCatalog.cs")));
        Assert.Contains(
            "PortalCraftGeneratedPanel",
            File.ReadAllText(Path.Combine(output, "client", "PortalCraftGeneratedPanel.tsx")));
        Assert.Contains(
            "PortalCraft AI repository dashboard",
            File.ReadAllText(Path.Combine(output, "dashboard", "index.html")));
    }

    [Fact]
    public void RefusesToOverwriteNonEmptyOutputWithoutForce()
    {
        Write("Program.cs", """app.MapGet("/api/orders", () => Results.Ok());""");
        var output = Path.Combine(directory, "generated");
        Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(output, "owned.txt"), "product-owned");
        var profile = new RepositoryScanner().Scan(directory);

        var exception = Assert.Throws<InvalidOperationException>(
            () => new IntegrationGenerator().Generate(profile, output));

        Assert.Contains("--force", exception.Message);
    }

    [Fact]
    public void BuildsUiApiSecurityAndDataScenarios()
    {
        var changes = new[]
        {
            new PullRequestChange(
                "1234567890abcdef",
                "42",
                "Secure request workflow",
                "developer",
                DateTimeOffset.UtcNow,
                [
                    "client/src/RequestPage.tsx",
                    "server/Api/RequestController.cs",
                    "server/Auth/RequestPolicy.cs",
                    "server/Migrations/20260915_Request.sql",
                    "tests/RequestFlow.test.tsx"
                ])
        };

        var scenarios = PullRequestScenarioAnalyzer.BuildScenarios(changes);

        Assert.Contains(scenarios, scenario => scenario.Category == "ui");
        Assert.Contains(scenarios, scenario => scenario.Category == "api");
        Assert.Contains(scenarios, scenario => scenario.Category == "security");
        Assert.Contains(scenarios, scenario => scenario.Category == "data");
        Assert.Contains(scenarios, scenario => scenario.Category == "regression");
    }

    private void Write(string relativePath, string content)
    {
        var path = Path.Combine(directory, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
    }

    public void Dispose()
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
