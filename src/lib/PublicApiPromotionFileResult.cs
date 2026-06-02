namespace BinkyLabs.PublicApi.Promoter;

/// <summary>
/// Describes the outcome of promoting one unshipped export file and its sibling shipped export file.
/// </summary>
public sealed class PublicApiPromotionFileResult
{
    internal PublicApiPromotionFileResult(
        string unshippedFilePath,
        string shippedFilePath,
        bool unshippedFileChanged,
        bool shippedFileChanged,
        int entriesPromoted,
        int entriesRemoved)
    {
        UnshippedFilePath = unshippedFilePath;
        ShippedFilePath = shippedFilePath;
        UnshippedFileChanged = unshippedFileChanged;
        ShippedFileChanged = shippedFileChanged;
        EntriesPromoted = entriesPromoted;
        EntriesRemoved = entriesRemoved;
    }

    /// <summary>
    /// Gets the relative path to the unshipped export file.
    /// </summary>
    public string UnshippedFilePath { get; }

    /// <summary>
    /// Gets the relative path to the shipped export file.
    /// </summary>
    public string ShippedFilePath { get; }

    /// <summary>
    /// Gets a value indicating whether the unshipped export file changed.
    /// </summary>
    public bool UnshippedFileChanged { get; }

    /// <summary>
    /// Gets a value indicating whether the shipped export file changed.
    /// </summary>
    public bool ShippedFileChanged { get; }

    /// <summary>
    /// Gets a value indicating whether either file changed for this promotion.
    /// </summary>
    public bool HasChanges => UnshippedFileChanged || ShippedFileChanged;

    /// <summary>
    /// Gets the number of lines appended to the shipped export for this file pair.
    /// </summary>
    public int EntriesPromoted { get; }

    /// <summary>
    /// Gets the number of lines removed from the shipped export for this file pair.
    /// </summary>
    public int EntriesRemoved { get; }

    /// <summary>
    /// Gets the relative file paths that changed for this file pair.
    /// </summary>
    public IReadOnlyList<string> ChangedFilePaths
    {
        get
        {
            List<string> changedFilePaths = [];
            if (ShippedFileChanged)
            {
                changedFilePaths.Add(ShippedFilePath);
            }

            if (UnshippedFileChanged)
            {
                changedFilePaths.Add(UnshippedFilePath);
            }

            return changedFilePaths;
        }
    }
}