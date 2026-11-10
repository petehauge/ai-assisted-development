using Microsoft.Extensions.Configuration.EnvironmentVariables;

namespace TextToSql.Web.Configuration;

public static class SecretsConfigurationExtensions
{
    /// <summary>
    /// Adds <c>secrets.json</c> so the order is: appsettings*.json → secrets.json → environment variables → command line (later wins).
    /// The file is optional: a missing file is reported in the returned lookup, not thrown.
    /// </summary>
    public static SecretsLookup AddSecretsFile(this IConfigurationManager configuration, string? secretsPathVariable, string startDirectory)
    {
        var lookup = SecretsFileLocator.Locate(secretsPathVariable, startDirectory);
        if (lookup.Path is null)
        {
            return lookup;
        }

        var tempBuilder = new ConfigurationBuilder().AddJsonFile(lookup.Path, optional: false, reloadOnChange: false);
        var source = tempBuilder.Sources[0];

        // Insert before the first environment-variables source so env vars and the command line still override secrets.json.
        var index = configuration.Sources.ToList().FindIndex(s => s is EnvironmentVariablesConfigurationSource);
        if (index < 0)
        {
            configuration.Sources.Add(source);
        }
        else
        {
            configuration.Sources.Insert(index, source);
        }

        return lookup;
    }
}
