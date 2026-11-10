using Microsoft.Data.SqlClient;

namespace TextToSql.Web.Data;

public sealed record TableRowCount(string Table, int Rows);

/// <summary>Runs the embedded seed.sql, which drops and recreates the sample tables with deterministic data.</summary>
public sealed class DatabaseSeeder(SqlConnectionFactory connections, ILogger<DatabaseSeeder> logger)
{
    private const string ResourceName = "TextToSql.Web.Data.seed.sql";

    public async Task<IReadOnlyList<TableRowCount>> SeedAsync(CancellationToken cancellationToken)
    {
        var script = await ReadScriptAsync(cancellationToken);

        await using var connection = connections.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(script, connection) { CommandTimeout = 120 };

        logger.LogInformation("Running seed script against database {Database}", connection.Database);
        var counts = new List<TableRowCount>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            counts.Add(new TableRowCount(reader.GetString(0), reader.GetInt32(1)));
        }

        return counts;
    }

    private static async Task<string> ReadScriptAsync(CancellationToken cancellationToken)
    {
        await using var stream = typeof(DatabaseSeeder).Assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Embedded resource '{ResourceName}' was not found.");
        using var reader = new StreamReader(stream);
        return await reader.ReadToEndAsync(cancellationToken);
    }
}
