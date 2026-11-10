using System.Diagnostics;
using Microsoft.Extensions.Logging;
using TextToSql.Core.Schema;

namespace TextToSql.Core.Query;

/// <summary>
/// Answers one question: validate it, read the schema, ask the model for SQL, run the SQL, return the SQL and the result.
/// Used by both the API endpoint and the Blazor page. There is no retry: a failure is returned to the caller as-is.
/// </summary>
public sealed class QueryService(
    ISchemaProvider schemaProvider,
    ISqlModelClient model,
    ISqlExecutor executor,
    QueryOptions options,
    ILogger<QueryService> logger)
{
    public async Task<QueryOutcome> AskAsync(string? question, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(question))
        {
            return new(QueryStatus.InvalidQuestion, QueryResult.Failed(null, "Please enter a question."));
        }

        if (question.Length > options.MaxQuestionLength)
        {
            return new(QueryStatus.InvalidQuestion, QueryResult.Failed(null, $"The question is too long ({question.Length} characters). The limit is {options.MaxQuestionLength}."));
        }

        logger.LogInformation("Question: {Question}", question);

        DatabaseSchema schema;
        try
        {
            schema = await schemaProvider.GetSchemaAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "Reading the database schema failed");
            return new(QueryStatus.SchemaFailed, QueryResult.Failed(null, $"Could not read the database schema: {ex.Message}"));
        }

        var prompt = SqlPrompt.Build(SchemaFormatter.Format(schema), question);

        string sql;
        var modelStarted = Stopwatch.GetTimestamp();
        try
        {
            sql = SqlExtractor.Extract(await model.CompleteAsync(prompt, cancellationToken));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "The model call failed after {ElapsedMs} ms", ElapsedMs(modelStarted));
            return new(QueryStatus.ModelFailed, QueryResult.Failed(null, $"The model call failed: {ex.Message}"));
        }

        logger.LogInformation("Generated SQL in {ElapsedMs} ms: {Sql}", ElapsedMs(modelStarted), sql);
        if (sql.Length == 0)
        {
            return new(QueryStatus.ModelFailed, QueryResult.Failed(null, "The model did not return any SQL."));
        }

        var sqlStarted = Stopwatch.GetTimestamp();
        try
        {
            var result = await executor.ExecuteAsync(sql, options.MaxRows, cancellationToken);
            logger.LogInformation(
                "Executed SQL in {ElapsedMs} ms: {RowCount} rows returned, truncated {Truncated}, rows affected {RowsAffected}",
                ElapsedMs(sqlStarted), result.Rows.Count, result.Truncated, result.RowsAffected);
            return new(QueryStatus.Success, new QueryResult(sql, result.Columns, result.Rows, result.RowsAffected, result.Truncated, null));
        }
        catch (SqlExecutionException ex)
        {
            logger.LogWarning("SQL execution failed after {ElapsedMs} ms: {Error}", ElapsedMs(sqlStarted), ex.Message);
            return new(QueryStatus.SqlFailed, QueryResult.Failed(sql, ex.Message));
        }
    }

    private static int ElapsedMs(long started) => (int)Stopwatch.GetElapsedTime(started).TotalMilliseconds;
}
