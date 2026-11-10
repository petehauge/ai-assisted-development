using Microsoft.Data.SqlClient;
using TextToSql.Core.Schema;

namespace TextToSql.Web.Data;

/// <summary>Reads user tables, columns, types, primary keys and foreign keys from the database. The first successful read is cached for the app's lifetime.</summary>
public sealed class SqlSchemaProvider(SqlConnectionFactory connections, ILogger<SqlSchemaProvider> logger) : ISchemaProvider
{
    private const string Query = """
        SELECT c.TABLE_SCHEMA, c.TABLE_NAME, c.COLUMN_NAME, c.DATA_TYPE, c.CHARACTER_MAXIMUM_LENGTH, c.NUMERIC_PRECISION, c.NUMERIC_SCALE, c.IS_NULLABLE
        FROM INFORMATION_SCHEMA.COLUMNS c
        JOIN INFORMATION_SCHEMA.TABLES t ON t.TABLE_SCHEMA = c.TABLE_SCHEMA AND t.TABLE_NAME = c.TABLE_NAME
        WHERE t.TABLE_TYPE = 'BASE TABLE'
        ORDER BY c.TABLE_SCHEMA, c.TABLE_NAME, c.ORDINAL_POSITION;

        SELECT ku.TABLE_SCHEMA, ku.TABLE_NAME, ku.COLUMN_NAME
        FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
        JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE ku ON ku.CONSTRAINT_SCHEMA = tc.CONSTRAINT_SCHEMA AND ku.CONSTRAINT_NAME = tc.CONSTRAINT_NAME
        WHERE tc.CONSTRAINT_TYPE = 'PRIMARY KEY';

        SELECT SCHEMA_NAME(pt.schema_id), pt.name, pc.name, SCHEMA_NAME(rt.schema_id), rt.name, rc.name
        FROM sys.foreign_key_columns fkc
        JOIN sys.tables pt ON pt.object_id = fkc.parent_object_id
        JOIN sys.columns pc ON pc.object_id = fkc.parent_object_id AND pc.column_id = fkc.parent_column_id
        JOIN sys.tables rt ON rt.object_id = fkc.referenced_object_id
        JOIN sys.columns rc ON rc.object_id = fkc.referenced_object_id AND rc.column_id = fkc.referenced_column_id;
        """;

    private readonly SemaphoreSlim _gate = new(1, 1);
    private DatabaseSchema? _cached;

    public async Task<DatabaseSchema> GetSchemaAsync(CancellationToken cancellationToken)
    {
        if (_cached is { } cached)
        {
            return cached;
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            _cached ??= await ReadAsync(cancellationToken);
            return _cached;
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<DatabaseSchema> ReadAsync(CancellationToken cancellationToken)
    {
        var started = System.Diagnostics.Stopwatch.GetTimestamp();
        await using var connection = connections.Create();
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(Query, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var columns = new List<(string Schema, string Table, string Column, string Type, bool Nullable)>();
        while (await reader.ReadAsync(cancellationToken))
        {
            var type = SqlTypeText.Format(
                reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetInt32(4),
                reader.IsDBNull(5) ? null : reader.GetByte(5),
                reader.IsDBNull(6) ? null : reader.GetInt32(6));
            columns.Add((reader.GetString(0), reader.GetString(1), reader.GetString(2), type, reader.GetString(7) == "YES"));
        }

        await reader.NextResultAsync(cancellationToken);
        var primaryKeys = new HashSet<(string, string, string)>();
        while (await reader.ReadAsync(cancellationToken))
        {
            primaryKeys.Add((reader.GetString(0), reader.GetString(1), reader.GetString(2)));
        }

        await reader.NextResultAsync(cancellationToken);
        var foreignKeys = new Dictionary<(string, string, string), ForeignKeyReference>();
        while (await reader.ReadAsync(cancellationToken))
        {
            // A column in a composite foreign key keeps the first reference; the sample schema has only single-column keys.
            foreignKeys.TryAdd(
                (reader.GetString(0), reader.GetString(1), reader.GetString(2)),
                new ForeignKeyReference($"{reader.GetString(3)}.{reader.GetString(4)}", reader.GetString(5)));
        }

        var tables = columns
            .GroupBy(c => (c.Schema, c.Table))
            .Select(g => new TableSchema(g.Key.Schema, g.Key.Table, g
                .Select(c => new ColumnSchema(
                    c.Column,
                    c.Type,
                    c.Nullable,
                    primaryKeys.Contains((c.Schema, c.Table, c.Column)),
                    foreignKeys.GetValueOrDefault((c.Schema, c.Table, c.Column))))
                .ToList()))
            .ToList();

        logger.LogInformation("Read schema: {TableCount} tables in {ElapsedMs} ms", tables.Count, (int)System.Diagnostics.Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        return new DatabaseSchema(tables);
    }
}

/// <summary>Turns INFORMATION_SCHEMA type columns into T-SQL type text, for example <c>nvarchar(100)</c>, <c>nvarchar(max)</c> or <c>decimal(10,2)</c>.</summary>
public static class SqlTypeText
{
    private static readonly HashSet<string> LengthTypes = new(StringComparer.OrdinalIgnoreCase) { "char", "nchar", "varchar", "nvarchar", "binary", "varbinary" };
    private static readonly HashSet<string> PrecisionScaleTypes = new(StringComparer.OrdinalIgnoreCase) { "decimal", "numeric" };

    public static string Format(string dataType, int? maxLength, byte? precision, int? scale)
    {
        if (LengthTypes.Contains(dataType) && maxLength is { } length)
        {
            return length == -1 ? $"{dataType}(max)" : $"{dataType}({length})";
        }

        if (PrecisionScaleTypes.Contains(dataType) && precision is { } p)
        {
            return $"{dataType}({p},{scale ?? 0})";
        }

        return dataType;
    }
}
