using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PortalCraft.RepoTool;

public sealed partial class PullRequestScenarioAnalyzer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public PullRequestScenarioReport Analyze(
        string repository,
        string? requestedBranch,
        int sinceDays,
        int limit)
    {
        var root = Path.GetFullPath(repository);
        var branch = ResolveBranch(root, requestedBranch);
        var log = Git(
            root,
            "log",
            "--first-parent",
            $"--since={sinceDays} days ago",
            $"--max-count={limit}",
            "--format=%H%x1f%P%x1f%an%x1f%aI%x1f%s%x1f%b%x1e",
            branch);
        var changes = ParseLog(root, log);
        var warnings = new List<string>();
        if (changes.Count == 0)
        {
            warnings.Add($"No first-parent changes were found on {branch} in the last {sinceDays} days.");
        }
        if (changes.Any(change => change.PullRequestNumber is null))
        {
            warnings.Add(
                "Some branch changes do not contain a PR number in local Git metadata; they are included as commit-level fallbacks.");
        }

        return new(
            root,
            branch,
            sinceDays,
            changes,
            BuildScenarios(changes),
            warnings);
    }

    public IReadOnlyList<string> Write(
        PullRequestScenarioReport report,
        string outputDirectory,
        bool force)
    {
        var output = Path.GetFullPath(outputDirectory);
        if (Directory.Exists(output)
            && Directory.EnumerateFileSystemEntries(output).Any()
            && !force)
        {
            throw new InvalidOperationException(
                $"Output directory is not empty: {output}. Pass --force to replace generated files.");
        }

        Directory.CreateDirectory(output);
        var json = Path.Combine(output, "pr-test-scenarios.json");
        var markdown = Path.Combine(output, "pr-test-scenarios.md");
        File.WriteAllText(json, JsonSerializer.Serialize(report, JsonOptions));
        File.WriteAllText(markdown, BuildMarkdown(report));
        return [json, markdown];
    }

    public static IReadOnlyList<TestScenario> BuildScenarios(
        IReadOnlyList<PullRequestChange> changes)
    {
        var scenarios = new Dictionary<string, TestScenario>(StringComparer.OrdinalIgnoreCase);
        foreach (var change in changes)
        {
            var evidence = change.ChangedFiles.Take(8).ToArray();
            Add(
                scenarios,
                new(
                    $"change-{change.Commit[..8]}",
                    "medium",
                    "regression",
                    $"Validate merged behavior: {change.Title}",
                    "Every merged change needs a focused happy-path and regression check.",
                    evidence));

            if (change.ChangedFiles.Any(IsFrontend))
            {
                Add(
                    scenarios,
                    new(
                        $"ui-{change.Commit[..8]}",
                        "high",
                        "ui",
                        $"Exercise affected UI flows for {DisplayChange(change)}",
                        "React, JavaScript, styling, or route files changed.",
                        change.ChangedFiles.Where(IsFrontend).Take(8).ToArray()));
            }
            if (change.ChangedFiles.Any(IsApi))
            {
                Add(
                    scenarios,
                    new(
                        $"api-{change.Commit[..8]}",
                        "high",
                        "api",
                        $"Verify API contracts and failure handling for {DisplayChange(change)}",
                        "API, controller, service, or contract files changed.",
                        change.ChangedFiles.Where(IsApi).Take(8).ToArray()));
            }
            if (change.ChangedFiles.Any(IsSecurity)
                || SecurityWords().IsMatch(change.Title))
            {
                Add(
                    scenarios,
                    new(
                        $"security-{change.Commit[..8]}",
                        "high",
                        "security",
                        $"Verify authorization boundaries for {DisplayChange(change)}",
                        "Authentication, authorization, identity, role, or permission behavior changed.",
                        evidence));
            }
            if (change.ChangedFiles.Any(IsData))
            {
                var migrationChange = change.ChangedFiles.Any(IsMigration);
                Add(
                    scenarios,
                    new(
                        $"data-{change.Commit[..8]}",
                        "high",
                        "data",
                        migrationChange
                            ? $"Validate data compatibility and rollback for {DisplayChange(change)}"
                            : $"Validate data queries and mappings for {DisplayChange(change)}",
                        migrationChange
                            ? "Schema or migration files changed."
                            : "Persistence, repository, or data mapping files changed.",
                        change.ChangedFiles.Where(IsData).Take(8).ToArray()));
            }
            if (change.ChangedFiles.Any(IsTest))
            {
                Add(
                    scenarios,
                    new(
                        $"tests-{change.Commit[..8]}",
                        "medium",
                        "regression",
                        $"Run and review changed automated tests for {DisplayChange(change)}",
                        "The change modified test coverage or test infrastructure.",
                        change.ChangedFiles.Where(IsTest).Take(8).ToArray()));
            }
        }

        return scenarios.Values
            .OrderBy(scenario => scenario.Priority == "high" ? 0 : 1)
            .ThenBy(scenario => scenario.Category)
            .ThenBy(scenario => scenario.Title)
            .ToArray();
    }

    private static IReadOnlyList<PullRequestChange> ParseLog(string root, string log)
    {
        var changes = new List<PullRequestChange>();
        foreach (var record in log.Split('\u001e', StringSplitOptions.RemoveEmptyEntries))
        {
            var fields = record.Trim().Split('\u001f');
            if (fields.Length < 6 || !DateTimeOffset.TryParse(fields[3], out var mergedAt))
            {
                continue;
            }
            var commit = fields[0];
            var parents = fields[1].Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var title = fields[4].Trim();
            var body = fields[5].Trim();
            var pr = PullRequestNumberPattern().Match($"{title}\n{body}");
            var files = ChangedFiles(root, commit, parents.FirstOrDefault());
            changes.Add(
                new(
                    commit,
                    pr.Success ? pr.Groups["number"].Value : null,
                    title,
                    fields[2],
                    mergedAt,
                    files));
        }
        return changes;
    }

    private static IReadOnlyList<string> ChangedFiles(
        string root,
        string commit,
        string? firstParent)
    {
        var output = firstParent is null
            ? Git(root, "show", "--pretty=format:", "--name-only", commit)
            : Git(root, "diff", "--name-only", firstParent, commit);
        return output
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static string ResolveBranch(string root, string? requested)
    {
        foreach (var candidate in requested is null
            ? new[] { "develop", "main", "master" }
            : new[] { requested })
        {
            if (GitSucceeds(root, "rev-parse", "--verify", candidate))
            {
                return candidate;
            }
            var remote = $"origin/{candidate}";
            if (GitSucceeds(root, "rev-parse", "--verify", remote))
            {
                return remote;
            }
        }
        throw new InvalidOperationException(
            requested is null
                ? "Could not find develop, main, or master."
                : $"Could not find branch '{requested}' locally or on origin.");
    }

    private static bool GitSucceeds(string root, params string[] arguments)
    {
        try
        {
            Git(root, arguments);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string Git(string root, params string[] arguments)
    {
        var startInfo = new ProcessStartInfo("git")
        {
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Unable to start git.");
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();
        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(error.Trim());
        }
        return output;
    }

    private static void Add(
        IDictionary<string, TestScenario> scenarios,
        TestScenario scenario) =>
        scenarios.TryAdd(scenario.Id, scenario);

    private static string DisplayChange(PullRequestChange change) =>
        change.PullRequestNumber is null
            ? change.Commit[..8]
            : $"PR #{change.PullRequestNumber}";

    private static bool IsFrontend(string path) =>
        path.EndsWith(".tsx", StringComparison.OrdinalIgnoreCase)
        || path.EndsWith(".jsx", StringComparison.OrdinalIgnoreCase)
        || path.EndsWith(".css", StringComparison.OrdinalIgnoreCase)
        || path.EndsWith(".scss", StringComparison.OrdinalIgnoreCase)
        || path.EndsWith(".html", StringComparison.OrdinalIgnoreCase)
        || ((path.Contains("/src/", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("src/", StringComparison.OrdinalIgnoreCase)
                || path.StartsWith("client/src/", StringComparison.OrdinalIgnoreCase))
            && (path.EndsWith(".ts", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".js", StringComparison.OrdinalIgnoreCase)));

    private static bool IsApi(string path) =>
        path.EndsWith("Controller.cs", StringComparison.OrdinalIgnoreCase)
        || path.EndsWith("Program.cs", StringComparison.OrdinalIgnoreCase)
        || path.Contains("/api/", StringComparison.OrdinalIgnoreCase)
        || path.Contains("/services/", StringComparison.OrdinalIgnoreCase)
        || path.Contains("/models/", StringComparison.OrdinalIgnoreCase);

    private static bool IsSecurity(string path) =>
        SecurityWords().IsMatch(path.Replace('\\', '/'));

    private static bool IsData(string path) =>
        IsMigration(path)
        || path.Contains("repository", StringComparison.OrdinalIgnoreCase)
        || path.Contains("mapping", StringComparison.OrdinalIgnoreCase);

    private static bool IsMigration(string path) =>
        path.EndsWith(".sql", StringComparison.OrdinalIgnoreCase)
        || path.Contains("migration", StringComparison.OrdinalIgnoreCase);

    private static bool IsTest(string path) =>
        path.Contains(".test.", StringComparison.OrdinalIgnoreCase)
        || path.Contains(".spec.", StringComparison.OrdinalIgnoreCase)
        || path.Contains("tests", StringComparison.OrdinalIgnoreCase);

    private static string BuildMarkdown(PullRequestScenarioReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Recent PR test scenarios");
        builder.AppendLine();
        builder.AppendLine($"Branch: `{report.Branch}`  ");
        builder.AppendLine($"Window: last {report.SinceDays} days  ");
        builder.AppendLine($"Changes analyzed: {report.Changes.Count}");
        builder.AppendLine();
        foreach (var scenario in report.Scenarios)
        {
            builder.AppendLine($"## [{scenario.Priority.ToUpperInvariant()}] {scenario.Title}");
            builder.AppendLine();
            builder.AppendLine($"Category: `{scenario.Category}`");
            builder.AppendLine();
            builder.AppendLine(scenario.Rationale);
            builder.AppendLine();
            foreach (var evidence in scenario.Evidence)
            {
                builder.AppendLine($"- `{evidence}`");
            }
            builder.AppendLine();
        }
        foreach (var warning in report.Warnings)
        {
            builder.AppendLine($"> Warning: {warning}");
        }
        return builder.ToString();
    }

    [GeneratedRegex(
        @"(?:merged?\s+(?:pull request|pr)\s*#?|pull request\s*#?|pr\s*#|\(#)(?<number>\d+)",
        RegexOptions.IgnoreCase)]
    private static partial Regex PullRequestNumberPattern();

    [GeneratedRegex(
        @"(?:auth|authorization|authentication|identity|permission|role|security|token|secret)",
        RegexOptions.IgnoreCase)]
    private static partial Regex SecurityWords();
}
