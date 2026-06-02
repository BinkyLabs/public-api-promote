namespace BinkyLabs.PublicApi.Promoter;

/// <summary>
/// Describes the files changed during public API promotion.
/// </summary>
public sealed class PublicApiPromotionResult
{
    internal PublicApiPromotionResult(IReadOnlyList<PublicApiPromotionFileResult> fileResults)
    {
        FileResults = fileResults;
    }

    /// <summary>
    /// Gets the per-file promotion results.
    /// </summary>
    public IReadOnlyList<PublicApiPromotionFileResult> FileResults { get; }

    /// <summary>
    /// Gets the relative file paths that changed during promotion.
    /// </summary>
    public IReadOnlyList<string> ChangedFilePaths =>
    [
        .. FileResults
            .SelectMany(static fileResult => fileResult.ChangedFilePaths)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(static path => path, StringComparer.OrdinalIgnoreCase)
    ];

    /// <summary>
    /// Gets the number of changed files.
    /// </summary>
    public int FilesChanged => ChangedFilePaths.Count;

    /// <summary>
    /// Gets a value indicating whether any files changed.
    /// </summary>
    public bool HasChanges => FileResults.Any(static fileResult => fileResult.HasChanges);

    /// <summary>
    /// Deconstructs the promotion result into its main components.
    /// </summary>
    /// <param name="fileResults">The per-file promotion results.</param>
    /// <param name="changedFilePaths">The relative file paths that changed.</param>
    /// <param name="filesChanged">The number of changed files.</param>
    /// <param name="hasChanges">Whether any files changed.</param>
    public void Deconstruct(
        out IReadOnlyList<PublicApiPromotionFileResult> fileResults,
        out IReadOnlyList<string> changedFilePaths,
        out int filesChanged,
        out bool hasChanges)
    {
        fileResults = FileResults;
        changedFilePaths = ChangedFilePaths;
        filesChanged = FilesChanged;
        hasChanges = HasChanges;
    }
}