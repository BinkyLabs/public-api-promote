using System.Text;

using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

namespace BinkyLabs.PublicApi.Promoter;

/// <summary>
/// Moves public API entries from unshipped exports into shipped exports.
/// </summary>
public static class PublicApiPromoter
{
    private const string NullableEnableDirective = "#nullable enable";
    private const string RemovedPrefix = "*REMOVED*";
    private const string UnshippedMarker = ".Unshipped";
    private const string ShippedMarker = ".Shipped";
    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);

    /// <summary>
    /// Promotes all matching unshipped export entries into their sibling shipped export files.
    /// </summary>
    /// <param name="options">The repository root and optional glob filter to use.</param>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    /// <returns>A summary of the file changes and promoted lines.</returns>
    public static async Task<PublicApiPromotionResult> PromoteAsync(
        PublicApiPromotionOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        string repositoryRoot = GetRepositoryRoot(options.RepositoryRoot);
        IReadOnlyList<string> unshippedFiles = FindUnshippedFiles(repositoryRoot, options.UnshippedGlob);

        List<PublicApiPromotionFileResult> fileResults = [];

        foreach (string unshippedFilePath in unshippedFiles)
        {
            PromotionChange promotionChange = await PromoteFileAsync(repositoryRoot, unshippedFilePath, cancellationToken).ConfigureAwait(false);
            if (!promotionChange.HasChanges)
            {
                continue;
            }

            fileResults.Add(new PublicApiPromotionFileResult(
                promotionChange.UnshippedFilePath,
                promotionChange.ShippedFilePath,
                promotionChange.UnshippedFileChanged,
                promotionChange.ShippedFileChanged,
                promotionChange.EntriesPromoted,
                promotionChange.EntriesRemoved));
        }

        List<PublicApiPromotionFileResult> orderedResults =
        [
            .. fileResults
                .OrderBy(static result => result.UnshippedFilePath, StringComparer.OrdinalIgnoreCase)
        ];

        return new PublicApiPromotionResult(orderedResults);
    }

    private static string GetRepositoryRoot(string repositoryRoot)
    {
        if (string.IsNullOrWhiteSpace(repositoryRoot))
        {
            throw new ArgumentException("Repository root is required.", nameof(repositoryRoot));
        }

        string fullPath = Path.GetFullPath(repositoryRoot);
        if (!Directory.Exists(fullPath))
        {
            throw new DirectoryNotFoundException($"Repository root '{fullPath}' does not exist.");
        }

        return fullPath;
    }

    private static IReadOnlyList<string> FindUnshippedFiles(string repositoryRoot, string? unshippedGlob)
    {
        if (string.IsNullOrWhiteSpace(unshippedGlob))
        {
            List<string> discoveredFiles =
            [
                .. Directory.EnumerateFiles(repositoryRoot, "*.txt", SearchOption.AllDirectories)
                    .Where(static path => Path.GetFileName(path).Contains(UnshippedMarker, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase)
            ];

            return discoveredFiles;
        }

        var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
        matcher.AddInclude(unshippedGlob.Replace('\\', '/'));

        var directoryInfo = new DirectoryInfo(repositoryRoot);
        PatternMatchingResult result = matcher.Execute(new DirectoryInfoWrapper(directoryInfo));

        List<string> matchedFiles =
        [
            .. result.Files
                .Select(static file => file.Path.Replace('/', Path.DirectorySeparatorChar))
                .Select(path => Path.GetFullPath(Path.Combine(repositoryRoot, path)))
                .Where(static path => path.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                .Where(static path => Path.GetFileName(path).Contains(UnshippedMarker, StringComparison.OrdinalIgnoreCase))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase)
        ];

        return matchedFiles;
    }

    private static async Task<PromotionChange> PromoteFileAsync(
        string repositoryRoot,
        string unshippedFilePath,
        CancellationToken cancellationToken)
    {
        string unshippedRawContent = await File.ReadAllTextAsync(unshippedFilePath, cancellationToken).ConfigureAwait(false);
        string normalizedUnshippedContent = StripNullableDirective(unshippedRawContent);
        if (string.IsNullOrWhiteSpace(normalizedUnshippedContent))
        {
            return PromotionChange.None;
        }

        List<string> unshippedLines = SplitLines(normalizedUnshippedContent);
        List<string> removedEntries =
        [
            .. unshippedLines
                .Where(static line => line.StartsWith(RemovedPrefix, StringComparison.Ordinal))
                .Select(static line => line[RemovedPrefix.Length..])
        ];

        string shippedFilePath = GetShippedFilePath(unshippedFilePath);
        List<string> shippedLines = [];
        string shippedRawContent = string.Empty;
        if (File.Exists(shippedFilePath))
        {
            shippedRawContent = await File.ReadAllTextAsync(shippedFilePath, cancellationToken).ConfigureAwait(false);
            shippedLines = SplitLines(shippedRawContent);
        }
        else if (removedEntries.Count > 0)
        {
            throw new FileNotFoundException($"Cannot remove entries because shipped file '{shippedFilePath}' does not exist.", shippedFilePath);
        }

        int entriesRemoved = 0;
        foreach (string removedEntry in removedEntries)
        {
            int beforeCount = shippedLines.Count;
            shippedLines.RemoveAll(line => string.Equals(line, removedEntry, StringComparison.Ordinal));
            entriesRemoved += beforeCount - shippedLines.Count;
        }

        List<string> remainingUnshippedLines =
        [
            .. unshippedLines.Where(static line => !line.StartsWith(RemovedPrefix, StringComparison.Ordinal))
        ];

        List<string> promotedEntries =
        [
            .. remainingUnshippedLines.Where(static line => !string.IsNullOrWhiteSpace(line))
        ];

        shippedLines.AddRange(promotedEntries);

        string newShippedContent = BuildFileContent(shippedLines);
        string newUnshippedContent = $"{NullableEnableDirective}\n";

        bool shippedChanged = !string.Equals(shippedRawContent, newShippedContent, StringComparison.Ordinal);
        bool unshippedChanged = !string.Equals(unshippedRawContent, newUnshippedContent, StringComparison.Ordinal);

        if (!shippedChanged && !unshippedChanged)
        {
            return PromotionChange.None;
        }

        if (shippedChanged)
        {
            await File.WriteAllTextAsync(shippedFilePath, newShippedContent, Utf8WithoutBom, cancellationToken).ConfigureAwait(false);
        }

        if (unshippedChanged)
        {
            await File.WriteAllTextAsync(unshippedFilePath, newUnshippedContent, Utf8WithoutBom, cancellationToken).ConfigureAwait(false);
        }

        string relativeShippedFilePath = NormalizeRelativePath(Path.GetRelativePath(repositoryRoot, shippedFilePath));
        string relativeUnshippedFilePath = NormalizeRelativePath(Path.GetRelativePath(repositoryRoot, unshippedFilePath));

        return new PromotionChange(
            relativeUnshippedFilePath,
            relativeShippedFilePath,
            unshippedChanged,
            shippedChanged,
            promotedEntries.Count,
            entriesRemoved);
    }

    private static string StripNullableDirective(string content) =>
        content.Replace(NullableEnableDirective, string.Empty, StringComparison.OrdinalIgnoreCase).Trim();

    private static List<string> SplitLines(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return [];
        }

        List<string> lines =
        [
            .. content.Split('\n').Select(static line => line.TrimEnd())
        ];

        while (lines.Count > 0 && string.IsNullOrEmpty(lines[^1]))
        {
            lines.RemoveAt(lines.Count - 1);
        }

        return lines;
    }

    private static string BuildFileContent(List<string> lines)
    {
        List<string> normalizedLines = [.. lines];
        while (normalizedLines.Count > 0 && string.IsNullOrEmpty(normalizedLines[^1]))
        {
            normalizedLines.RemoveAt(normalizedLines.Count - 1);
        }

        return normalizedLines.Count == 0
            ? string.Empty
            : string.Join("\n", normalizedLines) + "\n";
    }

    private static string GetShippedFilePath(string unshippedFilePath)
    {
        string fileName = Path.GetFileName(unshippedFilePath);
        int markerIndex = fileName.IndexOf(UnshippedMarker, StringComparison.OrdinalIgnoreCase);
        if (markerIndex < 0)
        {
            throw new InvalidOperationException($"File '{unshippedFilePath}' is not a supported unshipped export.");
        }

        string shippedMarker = MatchMarkerCasing(fileName.AsSpan(markerIndex, UnshippedMarker.Length), ShippedMarker);
        string shippedFileName = string.Concat(
            fileName.AsSpan(0, markerIndex),
            shippedMarker,
            fileName.AsSpan(markerIndex + UnshippedMarker.Length));

        return Path.Combine(Path.GetDirectoryName(unshippedFilePath)!, shippedFileName);
    }

    private static string NormalizeRelativePath(string relativePath) =>
        relativePath.Replace(Path.DirectorySeparatorChar, '/').Replace(Path.AltDirectorySeparatorChar, '/');

    private static string MatchMarkerCasing(ReadOnlySpan<char> sourceMarker, string targetMarker)
    {
        char[] result = targetMarker.ToCharArray();
        for (int index = 0; index < result.Length && index < sourceMarker.Length; index++)
        {
            result[index] = char.IsLetter(sourceMarker[index]) && char.IsLower(sourceMarker[index])
                ? char.ToLowerInvariant(result[index])
                : char.ToUpperInvariant(result[index]);
        }

        return new string(result);
    }

    private sealed record PromotionChange(
        string UnshippedFilePath,
        string ShippedFilePath,
        bool UnshippedFileChanged,
        bool ShippedFileChanged,
        int EntriesPromoted,
        int EntriesRemoved)
    {
        public static PromotionChange None { get; } = new(string.Empty, string.Empty, false, false, 0, 0);

        public bool HasChanges => UnshippedFileChanged || ShippedFileChanged;
    }
}