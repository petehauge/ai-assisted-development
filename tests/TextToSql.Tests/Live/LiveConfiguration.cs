using Microsoft.Extensions.Configuration;
using TextToSql.Web.Configuration;

namespace TextToSql.Tests.Live;

/// <summary>
/// Configuration for Live tests, assembled the same way the app does in Development:
/// the web project's appsettings.json and appsettings.Development.json, then secrets.json, then environment variables.
/// Only Live tests use this; unit tests never read secrets.json.
/// </summary>
internal static class LiveConfiguration
{
    private static readonly Lazy<IConfiguration> Instance = new(Build);

    public static IConfiguration Current => Instance.Value;

    public static string? MissingKey(params string[] keys) => keys.FirstOrDefault(k => string.IsNullOrWhiteSpace(Current[k]));

    private static IConfiguration Build()
    {
        var webProject = FindWebProjectDirectory();
        var config = new ConfigurationManager();
        if (webProject is not null)
        {
            config.AddJsonFile(Path.Combine(webProject, "appsettings.json"), optional: true);
            config.AddJsonFile(Path.Combine(webProject, "appsettings.Development.json"), optional: true);
        }

        config.AddEnvironmentVariables();
        config.AddSecretsFile(Environment.GetEnvironmentVariable(SecretsFileLocator.EnvironmentVariable), AppContext.BaseDirectory);
        return config;
    }

    private static string? FindWebProjectDirectory()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "src", "TextToSql.Web");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }
}

/// <summary>A fact tagged <c>Category=Live</c> that is skipped, naming the key, when the configuration it needs is missing.</summary>
internal sealed class LiveFactAttribute : FactAttribute
{
    public LiveFactAttribute(params string[] requiredKeys)
    {
        if (LiveConfiguration.MissingKey(requiredKeys) is { } missing)
        {
            Skip = $"Live test skipped: configuration '{missing}' is not set.";
        }
    }
}
