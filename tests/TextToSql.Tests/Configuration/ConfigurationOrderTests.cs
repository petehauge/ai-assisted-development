using Microsoft.Extensions.Configuration;
using TextToSql.Web.Configuration;

namespace TextToSql.Tests.Configuration;

/// <summary>Verifies the order from CLAUDE.md: appsettings.json → secrets.json → environment variables → command line (later wins).</summary>
public sealed class ConfigurationOrderTests : IDisposable
{
    private readonly TempDirectory _temp = new();
    private readonly string _envPrefix = "TTSQLTEST_" + Guid.NewGuid().ToString("N") + "_";

    public void Dispose()
    {
        Environment.SetEnvironmentVariable(_envPrefix + "Query__MaxRows", null);
        _temp.Dispose();
    }

    [Fact]
    public void Secrets_override_appsettings_and_are_overridden_by_env_vars_and_command_line()
    {
        var secrets = _temp.File("secrets.json", """
            { "Foundry": { "DefaultDeployment": "from-secrets" }, "Query": { "MaxRows": "2", "CommandTimeoutSeconds": "2" } }
            """);
        Environment.SetEnvironmentVariable(_envPrefix + "Query__MaxRows", "3");

        // Same source order WebApplication.CreateBuilder produces: json files, env vars, command line.
        var config = new ConfigurationManager();
        config.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Foundry:DefaultDeployment"] = "from-appsettings",
            ["Foundry:ProjectEndpoint"] = "from-appsettings",
            ["Query:MaxRows"] = "1",
            ["Query:CommandTimeoutSeconds"] = "1",
            ["Query:MaxQuestionLength"] = "1",
        });
        config.AddEnvironmentVariables(_envPrefix);
        config.AddCommandLine(["--Query:CommandTimeoutSeconds=4"]);

        var lookup = config.AddSecretsFile(secrets, _temp.Root);

        Assert.Equal(secrets, lookup.Path);
        Assert.Equal("from-appsettings", config["Foundry:ProjectEndpoint"]); // only in appsettings
        Assert.Equal("from-secrets", config["Foundry:DefaultDeployment"]); // secrets beat appsettings
        Assert.Equal("3", config["Query:MaxRows"]); // env beats secrets
        Assert.Equal("4", config["Query:CommandTimeoutSeconds"]); // command line beats secrets
    }

    [Fact]
    public void Missing_secrets_file_leaves_configuration_unchanged()
    {
        _temp.Dir(".git");
        var config = new ConfigurationManager();
        config.AddInMemoryCollection(new Dictionary<string, string?> { ["Query:MaxRows"] = "50" });
        var before = config.Sources.Count;

        var lookup = config.AddSecretsFile(null, _temp.Root);

        Assert.Null(lookup.Path);
        Assert.Equal(before, config.Sources.Count);
        Assert.Equal("50", config["Query:MaxRows"]);
    }
}
