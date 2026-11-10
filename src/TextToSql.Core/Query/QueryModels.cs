namespace TextToSql.Core.Query;

/// <summary>Limits for one question. Bound from the <c>Query</c> configuration section.</summary>
public sealed class QueryOptions
{
    public int MaxRows { get; set; } = 50;
    public int CommandTimeoutSeconds { get; set; } = 30;
    public int MaxQuestionLength { get; set; } = 1000;
}

/// <summary>What the API and the page return: the generated SQL and either rows, a rows-affected count, or an error.</summary>
/// <param name="Truncated">True when the result set had more than <see cref="QueryOptions.MaxRows"/> rows and only the first ones are returned.</param>
public sealed record QueryResult(
    string? Sql,
    IReadOnlyList<string> Columns,
    IReadOnlyList<IReadOnlyList<object?>> Rows,
    int? RowsAffected,
    bool Truncated,
    string? Error)
{
    public static QueryResult Failed(string? sql, string error) => new(sql, [], [], null, false, error);
}

public enum QueryStatus
{
    Success,
    InvalidQuestion,
    SchemaFailed,
    ModelFailed,
    SqlFailed,
}

public sealed record QueryOutcome(QueryStatus Status, QueryResult Result);

/// <summary>The rows of the first result set (capped), or the rows affected when the statement returned no result set.</summary>
public sealed record SqlExecutionResult(
    IReadOnlyList<string> Columns,
    IReadOnlyList<IReadOnlyList<object?>> Rows,
    int? RowsAffected,
    bool Truncated);

/// <summary>A database error while running generated SQL. The message is the database's own error text.</summary>
public sealed class SqlExecutionException(string message, Exception? inner = null) : Exception(message, inner);

/// <summary>Sends one prompt to the language model and returns its text reply.</summary>
public interface ISqlModelClient
{
    Task<string> CompleteAsync(string prompt, CancellationToken cancellationToken);
}

public interface ISqlExecutor
{
    /// <summary>Runs <paramref name="sql"/> and returns at most <paramref name="maxRows"/> rows. Throws <see cref="SqlExecutionException"/> on a database error.</summary>
    Task<SqlExecutionResult> ExecuteAsync(string sql, int maxRows, CancellationToken cancellationToken);
}
