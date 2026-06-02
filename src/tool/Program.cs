using BinkyLabs.PublicApi.Promoter.Cli;

Dictionary<string, string?> environmentVariables = Environment.GetEnvironmentVariables()
    .Cast<System.Collections.DictionaryEntry>()
    .ToDictionary(
        static entry => (string)entry.Key,
        static entry => entry.Value?.ToString(),
        StringComparer.Ordinal);

return await PromotionCliApp.RunAsync(
    args,
    Console.Out,
    Console.Error,
    environmentVariables,
    CancellationToken.None);