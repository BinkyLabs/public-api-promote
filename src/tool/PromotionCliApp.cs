using BinkyLabs.PublicApi.Promoter;

namespace BinkyLabs.PublicApi.Promoter.Cli;

/// <summary>
/// Parses command-line arguments and runs public API promotion.
/// </summary>
public static class PromotionCliApp
{
    /// <summary>
    /// Runs the command-line application.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    /// <param name="standardOutput">The writer used for standard output.</param>
    /// <param name="standardError">The writer used for standard error.</param>
    /// <param name="environmentVariables">The environment variables visible to the process.</param>
    /// <param name="cancellationToken">The cancellation token to observe.</param>
    /// <returns>The process exit code.</returns>
    public static async Task<int> RunAsync(
        string[] args,
        TextWriter standardOutput,
        TextWriter standardError,
        IReadOnlyDictionary<string, string?> environmentVariables,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(standardOutput);
        ArgumentNullException.ThrowIfNull(standardError);
        ArgumentNullException.ThrowIfNull(environmentVariables);

        if (args.Length == 0)
        {
            await WriteUsageAsync(standardError, cancellationToken).ConfigureAwait(false);
            return 1;
        }

        if (args.Contains("--help", StringComparer.Ordinal) || args.Contains("-h", StringComparer.Ordinal))
        {
            await WriteUsageAsync(standardOutput, cancellationToken).ConfigureAwait(false);
            return 0;
        }

        string repositoryRoot = Directory.GetCurrentDirectory();
        string? unshippedGlob = null;

        for (int index = 0; index < args.Length; index++)
        {
            switch (args[index])
            {
                case "--repository-root":
                    repositoryRoot = GetRequiredValue(args, ref index);
                    break;
                case "--unshipped-glob":
                    unshippedGlob = GetRequiredValue(args, ref index);
                    break;
                default:
                    await standardError.WriteLineAsync($"Unknown argument '{args[index]}'.".AsMemory(), cancellationToken).ConfigureAwait(false);
                    await WriteUsageAsync(standardError, cancellationToken).ConfigureAwait(false);
                    return 1;
            }
        }

        var options = new PublicApiPromotionOptions
        {
            RepositoryRoot = repositoryRoot,
            UnshippedGlob = unshippedGlob
        };

        PublicApiPromotionResult result = await PublicApiPromoter.PromoteAsync(options, cancellationToken).ConfigureAwait(false);
        int totalEntriesPromoted = result.FileResults.Sum(static fileResult => fileResult.EntriesPromoted);
        int totalEntriesRemoved = result.FileResults.Sum(static fileResult => fileResult.EntriesRemoved);

        await standardOutput.WriteLineAsync($"Changed: {result.HasChanges}".AsMemory(), cancellationToken).ConfigureAwait(false);
        await standardOutput.WriteLineAsync($"Files changed: {result.FilesChanged}".AsMemory(), cancellationToken).ConfigureAwait(false);
        await standardOutput.WriteLineAsync($"Entries promoted: {totalEntriesPromoted}".AsMemory(), cancellationToken).ConfigureAwait(false);
        await standardOutput.WriteLineAsync($"Entries removed: {totalEntriesRemoved}".AsMemory(), cancellationToken).ConfigureAwait(false);

        foreach (PublicApiPromotionFileResult fileResult in result.FileResults)
        {
            string summary = $" - {fileResult.UnshippedFilePath} => {fileResult.ShippedFilePath} (promoted: {fileResult.EntriesPromoted}, removed: {fileResult.EntriesRemoved})";
            await standardOutput.WriteLineAsync(summary.AsMemory(), cancellationToken).ConfigureAwait(false);
        }

        if (environmentVariables.TryGetValue("GITHUB_OUTPUT", out string? githubOutputPath)
            && !string.IsNullOrWhiteSpace(githubOutputPath))
        {
            await WriteGitHubOutputsAsync(githubOutputPath, result, cancellationToken).ConfigureAwait(false);
        }

        return 0;
    }

    private static string GetRequiredValue(string[] args, ref int index)
    {
        if (index + 1 >= args.Length)
        {
            throw new ArgumentException($"Missing value for '{args[index]}'.", nameof(args));
        }

        index++;
        return args[index];
    }

    private static async Task WriteUsageAsync(TextWriter writer, CancellationToken cancellationToken)
    {
        string usage =
            """
            Usage:
              public-api-promote --repository-root <path> [--unshipped-glob <glob>]

            Options:
              --repository-root  Repository root to scan.
              --unshipped-glob   Optional glob used to match unshipped export files.
              --help, -h         Show help.
            """;

        await writer.WriteLineAsync(usage.AsMemory(), cancellationToken).ConfigureAwait(false);
    }

    private static async Task WriteGitHubOutputsAsync(
        string githubOutputPath,
        PublicApiPromotionResult result,
        CancellationToken cancellationToken)
    {
        int totalEntriesPromoted = result.FileResults.Sum(static fileResult => fileResult.EntriesPromoted);
        int totalEntriesRemoved = result.FileResults.Sum(static fileResult => fileResult.EntriesRemoved);

        await using var stream = new FileStream(githubOutputPath, FileMode.Append, FileAccess.Write, FileShare.Read);
        await using var writer = new StreamWriter(stream);

        await writer.WriteLineAsync($"changed={result.HasChanges.ToString().ToLowerInvariant()}".AsMemory(), cancellationToken).ConfigureAwait(false);
        await writer.WriteLineAsync($"files-changed={result.FilesChanged}".AsMemory(), cancellationToken).ConfigureAwait(false);
        await writer.WriteLineAsync($"entries-promoted={totalEntriesPromoted}".AsMemory(), cancellationToken).ConfigureAwait(false);
        await writer.WriteLineAsync($"entries-removed={totalEntriesRemoved}".AsMemory(), cancellationToken).ConfigureAwait(false);
        await writer.WriteLineAsync("changed-files<<EOF".AsMemory(), cancellationToken).ConfigureAwait(false);

        foreach (string path in result.ChangedFilePaths)
        {
            await writer.WriteLineAsync(path.AsMemory(), cancellationToken).ConfigureAwait(false);
        }

        await writer.WriteLineAsync("EOF".AsMemory(), cancellationToken).ConfigureAwait(false);
    }
}