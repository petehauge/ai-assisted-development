using Microsoft.Data.SqlClient;

namespace TextToSql.Web.Data;

/// <summary>Thrown when a required configuration key is missing; the message names the key so the user can fix it.</summary>
public sealed class MissingConfigurationException(string key)
    : InvalidOperationException($"Required configuration '{key}' is not set. Add it to appsettings, secrets.json, or an environment variable ('{key.Replace(":", "__")}').")
{
    public string Key { get; } = key;
}

/// <summary>Creates connections to the configured Azure SQL database (Entra ID authentication; no password in the connection string).</summary>
public sealed class SqlConnectionFactory(IConfiguration configuration)
{
    public const string ConnectionStringKey = "Sql:ConnectionString";

    public SqlConnection Create()
    {
        var connectionString = configuration[ConnectionStringKey];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new MissingConfigurationException(ConnectionStringKey);
        }

        return new SqlConnection(connectionString);
    }
}
