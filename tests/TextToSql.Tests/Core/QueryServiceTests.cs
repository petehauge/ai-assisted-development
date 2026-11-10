using Microsoft.Extensions.Logging.Abstractions;
using TextToSql.Core.Query;
using TextToSql.Core.Schema;

namespace TextToSql.Tests.Core;

public sealed class QueryServiceTests
{
    private readonly FakeSchemaProvider _schema = new();
    private readonly FakeModelClient _model = new();
    private readonly FakeExecutor _executor = new();
    private readonly QueryOptions _options = new() { MaxRows = 50, MaxQuestionLength = 1000 };

    private QueryService CreateService() => new(_schema, _model, _executor, _options, NullLogger<QueryService>.Instance);

    [Fact]
    public async Task Prompt_contains_schema_and_question_and_no_row_data() // AC3
    {
        _executor.Result = new(["Email"], [["row-data@example.com"]], null, false);

        await CreateService().AskAsync("Which customers have an email?", CancellationToken.None);

        var prompt = Assert.Single(_model.Prompts);
        Assert.Contains(SchemaFormatter.Format(FakeSchemaProvider.Sample).TrimEnd(), prompt);
        Assert.Contains("Question: Which customers have an email?", prompt);
        Assert.DoesNotContain("row-data@example.com", prompt);
        Assert.Equal(SqlPrompt.Build(SchemaFormatter.Format(FakeSchemaProvider.Sample), "Which customers have an email?"), prompt);
    }

    [Fact]
    public async Task Success_returns_extracted_sql_and_rows_and_applies_the_row_limit() // AC4, AC5
    {
        _model.Reply = "```sql\nSELECT * FROM dbo.Customers;\n```";
        _executor.Result = new(["CustomerId"], Enumerable.Range(1, 50).Select(i => (IReadOnlyList<object?>)[i]).ToList(), null, Truncated: true);

        var outcome = await CreateService().AskAsync("List customers", CancellationToken.None);

        Assert.Equal(QueryStatus.Success, outcome.Status);
        Assert.Equal("SELECT * FROM dbo.Customers;", outcome.Result.Sql);
        Assert.Equal(("SELECT * FROM dbo.Customers;", 50), Assert.Single(_executor.Calls));
        Assert.Equal(50, outcome.Result.Rows.Count);
        Assert.True(outcome.Result.Truncated);
        Assert.Null(outcome.Result.Error);
    }

    [Fact]
    public async Task Non_query_returns_rows_affected() // AC6
    {
        _model.Reply = "DELETE FROM dbo.Customers WHERE CustomerId > 90;";
        _executor.Result = new([], [], RowsAffected: 10, Truncated: false);

        var outcome = await CreateService().AskAsync("Delete customers above 90", CancellationToken.None);

        Assert.Equal(QueryStatus.Success, outcome.Status);
        Assert.Equal("DELETE FROM dbo.Customers WHERE CustomerId > 90;", outcome.Result.Sql);
        Assert.Equal(10, outcome.Result.RowsAffected);
        Assert.Empty(outcome.Result.Rows);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Empty_question_is_rejected_without_calling_the_model(string? question) // AC8
    {
        var outcome = await CreateService().AskAsync(question, CancellationToken.None);

        Assert.Equal(QueryStatus.InvalidQuestion, outcome.Status);
        Assert.Empty(_model.Prompts);
        Assert.Empty(_executor.Calls);
    }

    [Fact]
    public async Task Too_long_question_is_rejected_without_calling_the_model()
    {
        var outcome = await CreateService().AskAsync(new string('x', 1001), CancellationToken.None);

        Assert.Equal(QueryStatus.InvalidQuestion, outcome.Status);
        Assert.Contains("1000", outcome.Result.Error);
        Assert.Empty(_model.Prompts);
    }

    [Fact]
    public async Task Model_reply_without_sql_is_a_model_failure_and_nothing_executes()
    {
        _model.Reply = "   ";

        var outcome = await CreateService().AskAsync("How many customers?", CancellationToken.None);

        Assert.Equal(QueryStatus.ModelFailed, outcome.Status);
        Assert.Empty(_executor.Calls);
    }

    [Fact]
    public async Task Schema_failure_is_reported_without_calling_the_model()
    {
        _schema.Throw = new InvalidOperationException("Database 'appdb' is not currently available.");

        var outcome = await CreateService().AskAsync("How many customers?", CancellationToken.None);

        Assert.Equal(QueryStatus.SchemaFailed, outcome.Status);
        Assert.Contains("not currently available", outcome.Result.Error);
        Assert.Empty(_model.Prompts);
    }

    [Fact]
    public async Task Cancellation_is_not_turned_into_an_error_result()
    {
        _model.Throw = new OperationCanceledException();

        await Assert.ThrowsAsync<OperationCanceledException>(() => CreateService().AskAsync("How many customers?", CancellationToken.None));
    }
}
