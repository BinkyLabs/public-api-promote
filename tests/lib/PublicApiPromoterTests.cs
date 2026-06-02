using BinkyLabs.PublicApi.Promoter;

namespace BinkyLabs.PublicApi.Promoter.Tests;

public sealed class PublicApiPromoterTests : IDisposable
{
    private readonly string _repositoryRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

    public PublicApiPromoterTests()
    {
        Directory.CreateDirectory(_repositoryRoot);
    }

    public void Dispose()
    {
        if (Directory.Exists(_repositoryRoot))
        {
            Directory.Delete(_repositoryRoot, recursive: true);
        }
    }

    [Fact]
    public async Task PromoteAsync_WhenUnshippedContainsEntries_AppendsToShipped()
    {
        string unshippedPath = CreateFile("src/PublicApi.Unshipped.txt", "#nullable enable\nApi.One()\nApi.Two()\n");
        string shippedPath = CreateFile("src/PublicApi.Shipped.txt", "Existing.Api()\n");

        PublicApiPromotionResult result = await PublicApiPromoter.PromoteAsync(new PublicApiPromotionOptions
        {
            RepositoryRoot = _repositoryRoot
        }, TestContext.Current.CancellationToken);

        Assert.True(result.HasChanges);
        Assert.Equal(2, result.FilesChanged);
        Assert.Equal(["src/PublicApi.Shipped.txt", "src/PublicApi.Unshipped.txt"], result.ChangedFilePaths);
        PublicApiPromotionFileResult fileResult = Assert.Single(result.FileResults);
        Assert.Equal(2, fileResult.EntriesPromoted);
        Assert.Equal(0, fileResult.EntriesRemoved);
        Assert.Equal("src/PublicApi.Unshipped.txt", fileResult.UnshippedFilePath);
        Assert.Equal("src/PublicApi.Shipped.txt", fileResult.ShippedFilePath);
        Assert.Equal("Existing.Api()\nApi.One()\nApi.Two()\n", await File.ReadAllTextAsync(shippedPath, TestContext.Current.CancellationToken));
        Assert.Equal("#nullable enable\n", await File.ReadAllTextAsync(unshippedPath, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task PromoteAsync_WhenOnlyNullableDirectiveExists_SkipsFile()
    {
        CreateFile("src/PublicApi.Unshipped.txt", "#nullable enable\n");
        CreateFile("src/PublicApi.Shipped.txt", "Existing.Api()\n");

        PublicApiPromotionResult result = await PublicApiPromoter.PromoteAsync(new PublicApiPromotionOptions
        {
            RepositoryRoot = _repositoryRoot
        }, TestContext.Current.CancellationToken);

        Assert.False(result.HasChanges);
        Assert.Equal(0, result.FilesChanged);
        Assert.Empty(result.ChangedFilePaths);
        Assert.Empty(result.FileResults);
    }

    [Fact]
    public async Task PromoteAsync_ResultCanBeDeconstructed()
    {
        CreateFile("src/PublicApi.Unshipped.txt", "#nullable enable\nApi.One()\n");
        CreateFile("src/PublicApi.Shipped.txt", string.Empty);

        PublicApiPromotionResult result = await PublicApiPromoter.PromoteAsync(new PublicApiPromotionOptions
        {
            RepositoryRoot = _repositoryRoot
        }, TestContext.Current.CancellationToken);

        var (fileResults, changedFilePaths, filesChanged, hasChanges) = result;

        Assert.True(hasChanges);
        Assert.Equal(2, filesChanged);
        Assert.Equal(["src/PublicApi.Shipped.txt", "src/PublicApi.Unshipped.txt"], changedFilePaths);
        PublicApiPromotionFileResult fileResult = Assert.Single(fileResults);
        Assert.Equal(1, fileResult.EntriesPromoted);
    }

    [Fact]
    public async Task PromoteAsync_WhenRemovedEntriesExist_DeletesThemFromShipped()
    {
        string unshippedPath = CreateFile("src/PublicApi.Unshipped.txt", "#nullable enable\n*REMOVED*Api.One()\n");
        string shippedPath = CreateFile("src/PublicApi.Shipped.txt", "Api.One()\nApi.Two()\n");

        PublicApiPromotionResult result = await PublicApiPromoter.PromoteAsync(new PublicApiPromotionOptions
        {
            RepositoryRoot = _repositoryRoot
        }, TestContext.Current.CancellationToken);

        Assert.True(result.HasChanges);
        Assert.Equal(2, result.FilesChanged);
        PublicApiPromotionFileResult fileResult = Assert.Single(result.FileResults);
        Assert.Equal(0, fileResult.EntriesPromoted);
        Assert.Equal(1, fileResult.EntriesRemoved);
        Assert.Equal("Api.Two()\n", await File.ReadAllTextAsync(shippedPath, TestContext.Current.CancellationToken));
        Assert.Equal("#nullable enable\n", await File.ReadAllTextAsync(unshippedPath, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task PromoteAsync_WhenMixedRemovedAndAddedEntries_ProcessesBoth()
    {
        CreateFile("src/PublicApi.Unshipped.txt", "#nullable enable\n*REMOVED*Api.One()\nApi.Three()\n");
        string shippedPath = CreateFile("src/PublicApi.Shipped.txt", "Api.One()\nApi.Two()\n");

        PublicApiPromotionResult result = await PublicApiPromoter.PromoteAsync(new PublicApiPromotionOptions
        {
            RepositoryRoot = _repositoryRoot
        }, TestContext.Current.CancellationToken);

        PublicApiPromotionFileResult fileResult = Assert.Single(result.FileResults);
        Assert.Equal(1, fileResult.EntriesPromoted);
        Assert.Equal(1, fileResult.EntriesRemoved);
        Assert.Equal("Api.Two()\nApi.Three()\n", await File.ReadAllTextAsync(shippedPath, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task PromoteAsync_WhenRemovedEntryDoesNotExist_StillResetsUnshipped()
    {
        string unshippedPath = CreateFile("src/PublicApi.Unshipped.txt", "#nullable enable\n*REMOVED*Missing.Api()\n");
        string shippedPath = CreateFile("src/PublicApi.Shipped.txt", "Existing.Api()\n");

        PublicApiPromotionResult result = await PublicApiPromoter.PromoteAsync(new PublicApiPromotionOptions
        {
            RepositoryRoot = _repositoryRoot
        }, TestContext.Current.CancellationToken);

        Assert.True(result.HasChanges);
        Assert.Equal(0, Assert.Single(result.FileResults).EntriesRemoved);
        Assert.Equal("Existing.Api()\n", await File.ReadAllTextAsync(shippedPath, TestContext.Current.CancellationToken));
        Assert.Equal("#nullable enable\n", await File.ReadAllTextAsync(unshippedPath, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task PromoteAsync_WhenShippedFileMissingForRemoval_Throws()
    {
        CreateFile("src/PublicApi.Unshipped.txt", "#nullable enable\n*REMOVED*Missing.Api()\n");

        await Assert.ThrowsAsync<FileNotFoundException>(() => PublicApiPromoter.PromoteAsync(new PublicApiPromotionOptions
        {
            RepositoryRoot = _repositoryRoot
        }, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task PromoteAsync_WhenShippedFileDoesNotExistForAddition_CreatesIt()
    {
        string shippedPath = Path.Combine(_repositoryRoot, "src", "PublicApi.Shipped.txt");
        CreateFile("src/PublicApi.Unshipped.txt", "#nullable enable\nApi.One()\n");

        PublicApiPromotionResult result = await PublicApiPromoter.PromoteAsync(new PublicApiPromotionOptions
        {
            RepositoryRoot = _repositoryRoot
        }, TestContext.Current.CancellationToken);

        Assert.True(result.HasChanges);
        Assert.Equal(1, Assert.Single(result.FileResults).EntriesPromoted);
        Assert.Equal("Api.One()\n", await File.ReadAllTextAsync(shippedPath, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task PromoteAsync_WhenGlobProvided_OnlyMatchesSelectedFiles()
    {
        CreateFile("src/first/PublicApi.Unshipped.txt", "#nullable enable\nFirst.Api()\n");
        CreateFile("src/first/PublicApi.Shipped.txt", string.Empty);
        CreateFile("src/second/PublicApi.Unshipped.txt", "#nullable enable\nSecond.Api()\n");
        CreateFile("src/second/PublicApi.Shipped.txt", string.Empty);

        PublicApiPromotionResult result = await PublicApiPromoter.PromoteAsync(new PublicApiPromotionOptions
        {
            RepositoryRoot = _repositoryRoot,
            UnshippedGlob = "src/first/**/*.txt"
        }, TestContext.Current.CancellationToken);

        Assert.Equal(["src/first/PublicApi.Shipped.txt", "src/first/PublicApi.Unshipped.txt"], result.ChangedFilePaths);
        Assert.Equal(1, Assert.Single(result.FileResults).EntriesPromoted);
        Assert.Equal("First.Api()\n", await File.ReadAllTextAsync(Path.Combine(_repositoryRoot, "src", "first", "PublicApi.Shipped.txt"), TestContext.Current.CancellationToken));
        Assert.Equal(string.Empty, await File.ReadAllTextAsync(Path.Combine(_repositoryRoot, "src", "second", "PublicApi.Shipped.txt"), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task PromoteAsync_WhenRepositoryRootMissing_Throws()
    {
        await Assert.ThrowsAsync<DirectoryNotFoundException>(() => PublicApiPromoter.PromoteAsync(new PublicApiPromotionOptions
        {
            RepositoryRoot = Path.Combine(_repositoryRoot, "missing")
        }, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task PromoteAsync_WhenRepositoryRootBlank_Throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => PublicApiPromoter.PromoteAsync(new PublicApiPromotionOptions
        {
            RepositoryRoot = " "
        }, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task PromoteAsync_WhenFilesUseCrLf_NormalizesOutputToLf()
    {
        string unshippedPath = CreateFile("src/PublicApi.Unshipped.txt", "#nullable enable\r\nApi.One()\r\n");
        string shippedPath = CreateFile("src/PublicApi.Shipped.txt", "Existing.Api()");

        await PublicApiPromoter.PromoteAsync(new PublicApiPromotionOptions
        {
            RepositoryRoot = _repositoryRoot
        }, TestContext.Current.CancellationToken);

        Assert.Equal("Existing.Api()\nApi.One()\n", await File.ReadAllTextAsync(shippedPath, TestContext.Current.CancellationToken));
        Assert.Equal("#nullable enable\n", await File.ReadAllTextAsync(unshippedPath, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task PromoteAsync_WhenGlobMatchesLowercaseUnshippedName_FindsFileCaseInsensitively()
    {
        string shippedPath = CreateFile("src/publicapi.shipped.txt", string.Empty);
        CreateFile("src/publicapi.unshipped.txt", "#nullable enable\nApi.One()\n");

        PublicApiPromotionResult result = await PublicApiPromoter.PromoteAsync(new PublicApiPromotionOptions
        {
            RepositoryRoot = _repositoryRoot,
            UnshippedGlob = "src/**/*.txt"
        }, TestContext.Current.CancellationToken);

        Assert.True(result.HasChanges);
        Assert.Equal("Api.One()\n", await File.ReadAllTextAsync(shippedPath, TestContext.Current.CancellationToken));
    }

    private string CreateFile(string relativePath, string content)
    {
        string fullPath = Path.Combine(_repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content);
        return fullPath;
    }
}