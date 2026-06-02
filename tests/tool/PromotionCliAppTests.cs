using BinkyLabs.PublicApi.Promoter.Cli;

namespace BinkyLabs.PublicApi.Promoter.Cli.Tests;

public sealed class PromotionCliAppTests : IDisposable
{
    private readonly string _repositoryRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));

    public PromotionCliAppTests()
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
    public async Task RunAsync_WhenHelpRequested_WritesUsageAndSucceeds()
    {
        var output = new StringWriter();
        var error = new StringWriter();

        int exitCode = await PromotionCliApp.RunAsync(["--help"], output, error, new Dictionary<string, string?>(), TestContext.Current.CancellationToken);

        Assert.Equal(0, exitCode);
        Assert.Contains("Usage:", output.ToString(), StringComparison.Ordinal);
        Assert.Equal(string.Empty, error.ToString());
    }

    [Fact]
    public async Task RunAsync_WhenNoArgumentsProvided_ReturnsError()
    {
        var output = new StringWriter();
        var error = new StringWriter();

        int exitCode = await PromotionCliApp.RunAsync([], output, error, new Dictionary<string, string?>(), TestContext.Current.CancellationToken);

        Assert.Equal(1, exitCode);
        Assert.Contains("Usage:", error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunAsync_WhenUnknownArgumentProvided_ReturnsError()
    {
        var output = new StringWriter();
        var error = new StringWriter();

        int exitCode = await PromotionCliApp.RunAsync(["--unknown"], output, error, new Dictionary<string, string?>(), TestContext.Current.CancellationToken);

        Assert.Equal(1, exitCode);
        Assert.Contains("Unknown argument", error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunAsync_WhenPromotionSucceeds_WritesSummaryAndOutputs()
    {
        string outputFile = Path.Combine(_repositoryRoot, "github-output.txt");
        CreateFile("src/PublicApi.Unshipped.txt", "#nullable enable\nApi.One()\n");
        CreateFile("src/PublicApi.Shipped.txt", "Existing.Api()\n");
        var output = new StringWriter();
        var error = new StringWriter();
        var environment = new Dictionary<string, string?>
        {
            ["GITHUB_OUTPUT"] = outputFile
        };

        int exitCode = await PromotionCliApp.RunAsync(
            ["--repository-root", _repositoryRoot, "--unshipped-glob", "src/**/*.txt"],
            output,
            error,
            environment,
            TestContext.Current.CancellationToken);

        Assert.Equal(0, exitCode);
        Assert.Equal(string.Empty, error.ToString());
        Assert.Contains("Changed: True", output.ToString(), StringComparison.Ordinal);
        string githubOutput = await File.ReadAllTextAsync(outputFile, TestContext.Current.CancellationToken);
        Assert.Contains("changed=true", githubOutput, StringComparison.Ordinal);
        Assert.Contains("files-changed=2", githubOutput, StringComparison.Ordinal);
        Assert.Contains("entries-promoted=1", githubOutput, StringComparison.Ordinal);
        Assert.Contains("entries-removed=0", githubOutput, StringComparison.Ordinal);
        Assert.Contains("src/PublicApi.Shipped.txt", githubOutput, StringComparison.Ordinal);
        Assert.Contains("src/PublicApi.Unshipped.txt", githubOutput, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RunAsync_WhenArgumentValueMissing_Throws()
    {
        var output = new StringWriter();
        var error = new StringWriter();

        await Assert.ThrowsAsync<ArgumentException>(() => PromotionCliApp.RunAsync(
            ["--repository-root"],
            output,
            error,
            new Dictionary<string, string?>(),
            TestContext.Current.CancellationToken));
    }

    private string CreateFile(string relativePath, string content)
    {
        string fullPath = Path.Combine(_repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, content);
        return fullPath;
    }
}