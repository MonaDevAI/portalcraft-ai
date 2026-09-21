using System.Text.Json;

namespace PortalCraft.RepoTool;

public static class RepoToolCli
{
    public static Task<int> RunAsync(string[] args)
    {
        try
        {
            return Task.FromResult(Run(args));
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"error: {exception.Message}");
            return Task.FromResult(1);
        }
    }

    private static int Run(string[] args)
    {
        if (args.Length == 0 || args.Contains("--help", StringComparer.OrdinalIgnoreCase))
        {
            PrintUsage();
            return args.Length == 0 ? 1 : 0;
        }

        var command = args[0].ToLowerInvariant();
        if (command is not ("analyze" or "generate" or "assistant" or "copilot-studio" or "knowledge" or "pr-scenarios" or "serve"))
        {
            throw new ArgumentException($"Unknown command '{args[0]}'.");
        }
        if (args.Length < 2)
        {
            throw new ArgumentException("A repository directory is required.");
        }

        var repository = Path.GetFullPath(args[1]);
        if (command == "serve")
        {
            DashboardServer.Run(repository, GetIntOption(args, "--port", 4318));
            return 0;
        }

        if (command == "pr-scenarios")
        {
            var branch = GetOption(args, "--branch");
            var sinceDays = GetIntOption(args, "--since-days", 30);
            var limit = GetIntOption(args, "--limit", 50);
            var analyzer = new PullRequestScenarioAnalyzer();
            var report = analyzer.Analyze(repository, branch, sinceDays, limit);
            var scenarioOutput = GetOption(args, "--output")
                ?? Path.Combine(repository, ".portalcraft-ai", "pr-scenarios");
            var files = analyzer.Write(
                report,
                scenarioOutput,
                args.Contains("--force", StringComparer.OrdinalIgnoreCase));
            Console.WriteLine($"Branch: {report.Branch}");
            Console.WriteLine($"Changes analyzed: {report.Changes.Count}");
            Console.WriteLine($"Test scenarios: {report.Scenarios.Count}");
            Console.WriteLine($"Generated {files.Count} files in {Path.GetFullPath(scenarioOutput)}");
            foreach (var warning in report.Warnings)
            {
                Console.WriteLine($"warning: {warning}");
            }
            return 0;
        }

        if (command == "knowledge")
        {
            var branch = GetOption(args, "--branch");
            var sinceDays = GetIntOption(args, "--since-days", 180);
            var limit = GetIntOption(args, "--limit", 100);
            var manualPaths = GetOptions(args, "--manuals-path");
            var knowledge = new RepositoryKnowledgeBuilder().Build(
                repository,
                branch,
                sinceDays,
                limit,
                manualPaths);
            var knowledgeOutput = GetOption(args, "--output")
                ?? Path.Combine(repository, ".portalcraft-ai", "knowledge");
            var files = new RepositoryKnowledgeBuilder().Write(
                knowledge,
                knowledgeOutput,
                args.Contains("--force", StringComparer.OrdinalIgnoreCase));
            Console.WriteLine($"Branch: {knowledge.Branch}");
            if (manualPaths.Count > 0)
            {
                Console.WriteLine($"Help manual paths: {manualPaths.Count}");
            }
            Console.WriteLine($"Documentation sources: {knowledge.Documents.Count}");
            Console.WriteLine($"Recent changes: {knowledge.Changes.Count}");
            Console.WriteLine($"Generated {files.Count} files in {Path.GetFullPath(knowledgeOutput)}");
            foreach (var warning in knowledge.Warnings)
            {
                Console.WriteLine($"warning: {warning}");
            }
            return 0;
        }

        var profile = new RepositoryScanner().Scan(repository);
        if (command == "copilot-studio")
        {
            var branch = GetOption(args, "--branch");
            var sinceDays = GetIntOption(args, "--since-days", 180);
            var limit = GetIntOption(args, "--limit", 100);
            var manualPaths = GetOptions(args, "--manuals-path");
            var productName = GetOption(args, "--product-name")
                ?? new DirectoryInfo(repository).Name;
            var assistantName = GetOption(args, "--assistant-name")
                ?? $"{productName} Assistant";
            var knowledge = new RepositoryKnowledgeBuilder().Build(
                repository,
                branch,
                sinceDays,
                limit,
                manualPaths);
            var packageOutput = GetOption(args, "--output")
                ?? Path.Combine(repository, ".portalcraft-ai", "copilot-studio");
            var copilotResult = new CopilotStudioPackageBuilder().Write(
                profile,
                knowledge,
                packageOutput,
                productName,
                assistantName,
                GetOption(args, "--api-base-url"),
                args.Contains("--force", StringComparer.OrdinalIgnoreCase));
            PrintSummary(profile);
            Console.WriteLine($"Branch: {knowledge.Branch}");
            Console.WriteLine($"Knowledge sources: {knowledge.Documents.Count}");
            Console.WriteLine(
                "Validation status: NOT VALIDATED in Copilot Studio; no licensed environment is available.");
            Console.WriteLine($"Generated {copilotResult.Files.Count} Copilot Studio files in {copilotResult.OutputDirectory}");
            foreach (var file in copilotResult.Files)
            {
                Console.WriteLine($"  {Path.GetRelativePath(copilotResult.OutputDirectory, file)}");
            }
            foreach (var warning in copilotResult.Warnings.Concat(knowledge.Warnings))
            {
                Console.WriteLine($"warning: {warning}");
            }
            return 0;
        }

        if (command == "analyze")
        {
            PrintSummary(profile);
            return 0;
        }

        if (command == "assistant")
        {
            var outputFile = GetOption(args, "--output")
                ?? Path.Combine(repository, ".portalcraft-ai", "client", "portalCraftAssistant.generated.ts");
            var manualPaths = GetOptions(args, "--manuals-path");
            var helpDocuments = manualPaths.Count > 0
                ? RepositoryKnowledgeBuilder.ReadDocumentation(repository, manualPaths)
                : [];
            var assistantConfiguration = ReadAssistantConfiguration(
                repository,
                GetOption(args, "--assistant-config"));
            var file = new IntegrationGenerator().GenerateAssistantIntegration(
                profile,
                outputFile,
                helpDocuments,
                args.Contains("--force", StringComparer.OrdinalIgnoreCase),
                assistantConfiguration);
            PrintSummary(profile);
            Console.WriteLine($"Help documents: {helpDocuments.Count}");
            Console.WriteLine(
                $"Assistant requirements: {assistantConfiguration?.LookupKeys.Count ?? 0} lookup keys, " +
                $"{assistantConfiguration?.BusinessEntities.Count ?? 0} business entities");
            var helpSections = helpDocuments
                .SelectMany(document => document.Sections.Count > 0
                    ? document.Sections
                    : [new RepositoryDocumentSection("Overview", document.Summary)])
                .ToArray();
            Console.WriteLine($"Help topics: {helpSections.Length}");
            if (helpSections.Length > 0)
            {
                Console.WriteLine("Sample help questions:");
                foreach (var section in helpSections.Take(5))
                {
                    Console.WriteLine($"  Explain {section.Title}");
                }
            }
            Console.WriteLine($"Generated assistant integration: {file}");
            return 0;
        }

        var output = GetOption(args, "--output")
            ?? Path.Combine(repository, ".portalcraft-ai");
        var result = new IntegrationGenerator().Generate(
            profile,
            output,
            args.Contains("--force", StringComparer.OrdinalIgnoreCase));

        PrintSummary(profile);
        Console.WriteLine($"Generated {result.Files.Count} files in {result.OutputDirectory}");
        foreach (var file in result.Files)
        {
            Console.WriteLine($"  {Path.GetRelativePath(result.OutputDirectory, file)}");
        }
        return 0;
    }

    private static string? GetOption(IReadOnlyList<string> args, string name)
    {
        for (var index = 0; index < args.Count; index++)
        {
            if (args[index].Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                if (index + 1 >= args.Count)
                {
                    throw new ArgumentException($"{name} requires a value.");
                }
                return args[index + 1];
            }
        }
        return null;
    }

    private static int GetIntOption(IReadOnlyList<string> args, string name, int defaultValue)
    {
        var value = GetOption(args, name);
        if (value is null)
        {
            return defaultValue;
        }
        if (!int.TryParse(value, out var parsed) || parsed <= 0)
        {
            throw new ArgumentException($"{name} must be a positive integer.");
        }
        return parsed;
    }

    private static IReadOnlyList<string> GetOptions(
        IReadOnlyList<string> args,
        string name)
    {
        var values = new List<string>();
        for (var index = 0; index < args.Count; index++)
        {
            if (!args[index].Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            if (index + 1 >= args.Count)
            {
                throw new ArgumentException($"{name} requires a value.");
            }
            values.Add(args[index + 1]);
        }
        return values;
    }

    private static PortalCraftAssistantConfiguration? ReadAssistantConfiguration(
        string repository,
        string? configurationPath)
    {
        if (configurationPath is null)
        {
            return null;
        }

        var path = Path.IsPathRooted(configurationPath)
            ? configurationPath
            : Path.Combine(repository, configurationPath);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Assistant configuration not found: {path}");
        }

        return JsonSerializer.Deserialize<PortalCraftAssistantConfiguration>(
            File.ReadAllText(path),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidOperationException(
                $"Assistant configuration is empty or invalid: {path}");
    }

    private static void PrintSummary(RepositoryProfile profile)
    {
        Console.WriteLine($"Repository: {profile.Repository}");
        Console.WriteLine($"API operations: {profile.ApiOperations.Count}");
        Console.WriteLine($"Read-only operations: {profile.ApiOperations.Count(operation => operation.ReadOnly)}");
        Console.WriteLine($"UI routes: {profile.UiRoutes.Count}");
        Console.WriteLine($"Query candidates: {profile.QueryCandidates.Count}");
        foreach (var warning in profile.Warnings)
        {
            Console.WriteLine($"warning: {warning}");
        }
    }

    private static void PrintUsage() =>
        Console.WriteLine(
            """
            PortalCraft.RepoTool

              analyze  <repository>                          Inspect without writing files.
              generate <repository> [--output <dir>] [--force]
                                                               Create a manifest and React/.NET scaffolding.
              assistant <repository> [--manuals-path <repo-relative-path>]...
                        [--assistant-config <repo-relative-json>] [--output <file>] [--force]
                                                               Generate typed assistant query, route, and optional
                                                               reviewed help-document and portal requirement integration.
              copilot-studio <repository> [--product-name <name>] [--assistant-name <name>]
                             [--api-base-url <https-url>] [--branch <name>]
                             [--since-days <n>] [--limit <n>]
                             [--manuals-path <repo-relative-path>]...
                             [--output <dir>] [--force]
                                                               Generate an unvalidated, review-only Copilot Studio preview package.
              pr-scenarios <repository> [--branch <name>] [--since-days <n>]
                           [--limit <n>] [--output <dir>] [--force]
                                                               Derive test scenarios from recent branch changes.
              knowledge <repository> [--branch <name>] [--since-days <n>]
                        [--limit <n>] [--manuals-path <repo-relative-path>]...
                        [--output <dir>] [--force]
                                                               Generate reviewed app-documentation and change knowledge.
              serve <generated-directory> [--port <n>]          Run the generated repository dashboard.

            Only discovered GET operations become automatic query candidates.
            Generated output must be reviewed before product integration.
            """);
}
