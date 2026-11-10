using TextToSql.Core.Query;
using TextToSql.Core.Schema;

namespace TextToSql.Tests;

internal sealed class FakeSchemaProvider : ISchemaProvider
{
    public static readonly DatabaseSchema Sample = new(
    [
        new TableSchema("dbo", "Customers",
        [
            new ColumnSchema("CustomerId", "int", false, true),
            new ColumnSchema("Email", "nvarchar(100)", false, false),
        ]),
    ]);

    public Exception? Throw { get; set; }

    public Task<DatabaseSchema> GetSchemaAsync(CancellationToken cancellationToken) =>
        Throw is null ? Task.FromResult(Sample) : Task.FromException<DatabaseSchema>(Throw);
}

internal sealed class FakeModelClient : ISqlModelClient
{
    public string Reply { get; set; } = "SELECT COUNT(*) AS CustomerCount FROM dbo.Customers;";
    public Exception? Throw { get; set; }
    public List<string> Prompts { get; } = [];

    public Task<string> CompleteAsync(string prompt, CancellationToken cancellationToken)
    {
        Prompts.Add(prompt);
        return Throw is null ? Task.FromResult(Reply) : Task.FromException<string>(Throw);
    }
}

internal sealed class FakeExecutor : ISqlExecutor
{
    public SqlExecutionResult Result { get; set; } = new(["CustomerCount"], [[100]], null, false);
    public Exception? Throw { get; set; }
    public List<(string Sql, int MaxRows)> Calls { get; } = [];

    public Task<SqlExecutionResult> ExecuteAsync(string sql, int maxRows, CancellationToken cancellationToken)
    {
        Calls.Add((sql, maxRows));
        return Throw is null ? Task.FromResult(Result) : Task.FromException<SqlExecutionResult>(Throw);
    }
}
