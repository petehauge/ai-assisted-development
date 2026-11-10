using System.Globalization;
using Microsoft.Data.SqlClient;
using TextToSql.Core.Query;

namespace TextToSql.Web.Data;

/// <summary>
/// Runs generated SQL as-is (all statement types are allowed by design; see Known limitations in docs/plan.md).
/// Returns the first result set capped at maxRows, or the rows affected when the statement returns no result set.
/// </summary>
public sealed class SqlServerExecutor(SqlConnectionFactory connections, QueryOptions options) : ISqlExecutor
{
    public async Task<SqlExecutionResult> ExecuteAsync(string sql, int maxRows, CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = connections.Create();
            await connection.OpenAsync(cancellationToken);
            await using var command = new SqlCommand(sql, connection) { CommandTimeout = options.CommandTimeoutSeconds };
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            // Skip leading statements that return no result set (for example an UPDATE before a SELECT).
            while (reader.FieldCount == 0 && await reader.NextResultAsync(cancellationToken))
            {
            }

            if (reader.FieldCount == 0)
            {
                await reader.CloseAsync();
                return new SqlExecutionResult([], [], reader.RecordsAffected, Truncated: false);
            }

            var columns = Enumerable.Range(0, reader.FieldCount).Select(i => reader.GetName(i)).ToList();
            var rows = new List<IReadOnlyList<object?>>();
            while (rows.Count < maxRows && await reader.ReadAsync(cancellationToken))
            {
                var row = new object?[reader.FieldCount];
                for (var i = 0; i < row.Length; i++)
                {
                    row[i] = ToJsonFriendly(reader.GetValue(i));
                }

                rows.Add(row);
            }

            var truncated = rows.Count == maxRows && await reader.ReadAsync(cancellationToken);
            if (truncated)
            {
                // Stop the server streaming the rest of a large result instead of draining it on close.
                command.Cancel();
            }

            return new SqlExecutionResult(columns, rows, null, truncated);
        }
        catch (SqlException ex)
        {
            throw new SqlExecutionException(ex.Message, ex);
        }
    }

    // Keep values that serialize cleanly to JSON; render anything else as text.
    private static object? ToJsonFriendly(object value) => value switch
    {
        DBNull => null,
        string or bool or byte or short or int or long or decimal or double or float or Guid or DateTime or DateTimeOffset or TimeSpan => value,
        byte[] bytes => "0x" + Convert.ToHexString(bytes),
        _ => Convert.ToString(value, CultureInfo.InvariantCulture),
    };
}
