namespace BinkyLabs.PublicApi.Promoter;

/// <summary>
/// Describes how public API promotion should scan the repository.
/// </summary>
public sealed class PublicApiPromotionOptions
{
    /// <summary>
    /// Gets or sets the repository root that will be scanned for unshipped exports.
    /// </summary>
    public required string RepositoryRoot { get; init; }

    /// <summary>
    /// Gets or sets the optional glob used to find unshipped export files.
    /// </summary>
    public string? UnshippedGlob { get; init; }
}