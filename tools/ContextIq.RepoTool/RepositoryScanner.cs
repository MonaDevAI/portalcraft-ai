using System.Text.RegularExpressions;

namespace ContextIq.RepoTool;

public sealed partial class RepositoryScanner
{
    private static readonly HashSet<string> SupportedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".cs", ".ts", ".tsx", ".js", ".jsx" };

    private static readonly HashSet<string> ExcludedDirectories =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".git", ".context-iq", "bin", "obj", "node_modules", "dist", "build",
            "coverage", ".next", "artifacts", "test", "tests", "tools"
        };

    public RepositoryProfile Scan(string repository)
    {
        var root = Path.GetFullPath(repository);
        if (!Directory.Exists(root))
        {
            throw new DirectoryNotFoundException($"Repository directory not found: {root}");
        }

        var operations = new Dictionary<string, ApiOperation>(StringComparer.OrdinalIgnoreCase);
        var routes = new Dictionary<string, UiRoute>(StringComparer.OrdinalIgnoreCase);
        var warnings = new List<string>();

        foreach (var file in EnumerateSourceFiles(root))
        {
            var relativePath = Path.GetRelativePath(root, file).Replace('\\', '/');
            var text = File.ReadAllText(file);
            DiscoverApiOperations(text, relativePath, operations);
            DiscoverUiRoutes(text, relativePath, routes);
        }

        if (operations.Count == 0)
        {
            warnings.Add("No API operations were discovered. Add custom descriptors before generating an integration.");
        }
        if (routes.Count == 0)
        {
            warnings.Add("No frontend routes were discovered. Generated queries will not include navigation targets.");
        }

        var orderedOperations = operations.Values
            .OrderBy(operation => operation.Route, StringComparer.OrdinalIgnoreCase)
            .ThenBy(operation => operation.Method, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var orderedRoutes = routes.Values
            .OrderBy(route => route.Path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var candidates = CreateCandidates(orderedOperations, orderedRoutes);

        if (orderedOperations.Any(operation => !operation.ReadOnly))
        {
            warnings.Add("Write operations were inventoried but excluded from generated query candidates.");
        }

        return new(
            root,
            DateTimeOffset.UtcNow,
            orderedOperations,
            orderedRoutes,
            candidates,
            warnings);
    }

    private static IEnumerable<string> EnumerateSourceFiles(string root)
    {
        var pending = new Stack<string>();
        pending.Push(root);

        while (pending.Count > 0)
        {
            var directory = pending.Pop();
            foreach (var child in Directory.EnumerateDirectories(directory))
            {
                var name = Path.GetFileName(child);
                if (!ExcludedDirectories.Contains(name)
                    && !name.EndsWith(".Tests", StringComparison.OrdinalIgnoreCase))
                {
                    pending.Push(child);
                }
            }

            foreach (var file in Directory.EnumerateFiles(directory))
            {
                var name = Path.GetFileName(file);
                if (!name.Contains(".test.", StringComparison.OrdinalIgnoreCase)
                    && !name.Contains(".spec.", StringComparison.OrdinalIgnoreCase)
                    && SupportedExtensions.Contains(Path.GetExtension(file))
                    && new FileInfo(file).Length <= 1_000_000)
                {
                    yield return file;
                }
            }
        }
    }

    private static void DiscoverApiOperations(
        string text,
        string source,
        IDictionary<string, ApiOperation> operations)
    {
        foreach (Match match in MinimalApiPattern().Matches(text))
        {
            AddOperation(
                operations,
                match.Groups["method"].Value,
                match.Groups["route"].Value,
                source,
                ExtractHandlerParameters(match.Groups["handlerParameters"].Value));
        }
        foreach (Match match in ControllerApiPattern().Matches(text))
        {
            var route = match.Groups["route"].Success ? match.Groups["route"].Value : "/";
            AddOperation(operations, match.Groups["method"].Value, route, source);
        }
        foreach (Match match in FetchWithMethodPattern().Matches(text))
        {
            AddOperation(
                operations,
                match.Groups["method"].Value,
                match.Groups["route"].Value,
                source);
        }
        foreach (Match match in FetchPattern().Matches(text))
        {
            AddOperation(operations, "GET", match.Groups["route"].Value, source);
        }
        foreach (Match match in AxiosPattern().Matches(text))
        {
            AddOperation(operations, match.Groups["method"].Value, match.Groups["route"].Value, source);
        }
    }

    private static void DiscoverUiRoutes(
        string text,
        string source,
        IDictionary<string, UiRoute> routes)
    {
        foreach (Match match in ReactRoutePattern().Matches(text))
        {
            AddRoute(routes, match.Groups["route"].Value, source);
        }
        foreach (Match match in ObjectRoutePattern().Matches(text))
        {
            AddRoute(routes, match.Groups["route"].Value, source);
        }
        foreach (Match match in NavigationCallPattern().Matches(text))
        {
            AddRoute(routes, match.Groups["route"].Value, source);
        }
    }

    private static void AddOperation(
        IDictionary<string, ApiOperation> operations,
        string method,
        string route,
        string source,
        IReadOnlyList<string>? parameters = null)
    {
        method = method.ToUpperInvariant();
        route = NormalizeRoute(route);
        var key = $"{method}:{route}";
        operations.TryAdd(
            key,
            new(
                ToIdentifier(method, route),
                method,
                route,
                source,
                IsReadOnlyOperation(method, route),
                parameters ?? []));
    }

    private static void AddRoute(
        IDictionary<string, UiRoute> routes,
        string route,
        string source)
    {
        route = NormalizeRoute(route);
        routes.TryAdd(route, new(route, source));
    }

    private static IReadOnlyList<QueryCandidate> CreateCandidates(
        IReadOnlyList<ApiOperation> operations,
        IReadOnlyList<UiRoute> routes) =>
        operations
            .Where(operation => operation.ReadOnly && IsBusinessCandidate(operation.Route))
            .SelectMany(operation =>
            {
                var routeParameters = RouteParameterPattern()
                    .Matches(operation.Route)
                    .Select(match => match.Groups["parameter"].Value)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                var queryParameters = operation.Parameters
                    .Except(routeParameters, StringComparer.OrdinalIgnoreCase)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();
                var label = HumanizeRoute(operation.Route);
                var view = FindBestView(operation.Route, routes);
                var candidates = new List<QueryCandidate>();
                if (routeParameters.Length == 0)
                {
                    candidates.Add(new(
                        $"query-{operation.Id}",
                        $"Show {label}",
                        operation.Id,
                        view,
                        []));
                }
                foreach (var parameter in routeParameters.Concat(queryParameters))
                {
                    candidates.Add(new(
                        $"query-{operation.Id}-by-{ToIdentifierPart(parameter)}",
                        $"Find {label} by {Humanize(parameter)}",
                        operation.Id,
                        view,
                        [parameter]));
                }
                return candidates;
            })
            .GroupBy(
                candidate => $"{candidate.Prompt}\n{candidate.ViewPath}",
                StringComparer.OrdinalIgnoreCase)
            .Select(group => group.Last())
            .ToArray();

    private static bool IsBusinessCandidate(string route)
    {
        var value = route.ToLowerInvariant();
        return value != "/"
            && !value.Contains("/health")
            && !value.Contains("/metrics")
            && !value.Contains("/swagger")
            && !value.Contains("/openapi")
            && !value.Contains("/authorize")
            && !value.Contains("/callback");
    }

    private static bool IsReadOnlyOperation(string method, string route)
    {
        if (method.Equals("GET", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        if (!method.Equals("POST", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var value = route.ToLowerInvariant();
        var readWords = new[] { "/search", "/filter", "/lookup", "/query" };
        var writeWords = new[]
        {
            "/create", "/update", "/submit", "/approve", "/reject", "/delete",
            "/generate", "/upload", "/import"
        };
        return readWords.Any(value.Contains) && !writeWords.Any(value.Contains);
    }

    private static IReadOnlyList<string> ExtractHandlerParameters(string handlerParameters) =>
        HandlerParameterPattern()
            .Matches(handlerParameters)
            .Select(match => match.Groups["parameter"].Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static string? FindBestView(string apiRoute, IReadOnlyList<UiRoute> routes)
    {
        var apiWords = Words(apiRoute)
            .Select(NormalizeComparisonWord)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return routes
            .Select(route => new
            {
                route.Path,
                Score = Words(route.Path)
                    .Select(NormalizeComparisonWord)
                    .Count(word => apiWords.Contains(word))
            })
            .Where(candidate => candidate.Score > 0)
            .OrderByDescending(candidate => candidate.Score)
            .ThenBy(candidate => candidate.Path.Length)
            .Select(candidate => candidate.Path)
            .FirstOrDefault();
    }

    private static string NormalizeComparisonWord(string value) =>
        value.Length > 3 && value.EndsWith('s')
            ? value[..^1]
            : value;

    private static string HumanizeRoute(string route)
    {
        var routeWithoutParameters = RouteParameterPattern().Replace(route, string.Empty);
        var words = Words(routeWithoutParameters)
            .Where(word => !word.Equals("api", StringComparison.OrdinalIgnoreCase))
            .Where(word => !word.Equals("db", StringComparison.OrdinalIgnoreCase))
            .Where(word => !Regex.IsMatch(word, @"^v\d+$", RegexOptions.IgnoreCase))
            .ToArray();
        return words.Length == 0 ? "repository data" : string.Join(' ', words.Select(Humanize));
    }

    private static IEnumerable<string> Words(string value) =>
        Regex.Replace(value, "([a-z0-9])([A-Z])", "$1 $2")
            .Split(['/', '-', '_', ' '], StringSplitOptions.RemoveEmptyEntries);

    private static string Humanize(string value) =>
        Regex.Replace(value.Trim('{', '}'), "([a-z0-9])([A-Z])", "$1 $2").ToLowerInvariant();

    private static string NormalizeRoute(string route)
    {
        var normalized = route.Trim();
        if (Uri.TryCreate(normalized, UriKind.Absolute, out var uri))
        {
            normalized = uri.PathAndQuery;
        }
        normalized = normalized.Split('?', 2)[0];
        return normalized.StartsWith('/') ? normalized : $"/{normalized}";
    }

    private static string ToIdentifier(string method, string route)
    {
        var value = $"{method}-{string.Join('-', Words(route).Select(part => part.Trim('{', '}')))}";
        return Regex.Replace(value.ToLowerInvariant(), "[^a-z0-9-]+", "-").Trim('-');
    }

    private static string ToIdentifierPart(string value) =>
        Regex.Replace(Humanize(value), "[^a-z0-9]+", "-").Trim('-');

    [GeneratedRegex(
        @"Map(?<method>Get|Post|Put|Delete|Patch)\s*\(\s*""(?<route>[^""]+)""\s*,\s*\((?<handlerParameters>[^)]*)\)\s*=>",
        RegexOptions.Singleline)]
    private static partial Regex MinimalApiPattern();

    [GeneratedRegex(@"\[Http(?<method>Get|Post|Put|Delete|Patch)(?:\(\s*""(?<route>[^""]*)""\s*\))?\]")]
    private static partial Regex ControllerApiPattern();

    [GeneratedRegex(
        @"fetch\s*\(\s*[""'`](?<route>/api/[^""'`]+)[""'`]\s*,[\s\S]{0,300}?\bmethod\s*:\s*[""'`](?<method>get|post|put|delete|patch)[""'`]",
        RegexOptions.IgnoreCase)]
    private static partial Regex FetchWithMethodPattern();

    [GeneratedRegex(@"fetch\s*\(\s*[""'`](?<route>/api/[^""'`]+)[""'`]\s*\)")]
    private static partial Regex FetchPattern();

    [GeneratedRegex(@"axios\.(?<method>get|post|put|delete|patch)\s*\(\s*[""'`](?<route>/api/[^""'`]+)[""'`]")]
    private static partial Regex AxiosPattern();

    [GeneratedRegex(@"<Route\b[^>]*\bpath\s*=\s*[""'](?<route>[^""']+)[""']")]
    private static partial Regex ReactRoutePattern();

    [GeneratedRegex(@"\bpath\s*:\s*[""'](?<route>/[^""']+)[""']")]
    private static partial Regex ObjectRoutePattern();

    [GeneratedRegex(@"\b(?:navigate|setView|onOpenView)\s*\(\s*[""'](?<route>[a-zA-Z0-9/_-]+)[""']")]
    private static partial Regex NavigationCallPattern();

    [GeneratedRegex(@"\{(?<parameter>[^}:]+)(?::[^}]+)?\}")]
    private static partial Regex RouteParameterPattern();

    [GeneratedRegex(
        @"\b(?:string|int|long|Guid|bool|DateTime|DateTimeOffset)(?:\?)?\s+(?<parameter>[a-zA-Z_][a-zA-Z0-9_]*)")]
    private static partial Regex HandlerParameterPattern();
}
