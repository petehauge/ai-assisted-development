using Microsoft.Extensions.Logging.Abstractions;
using TextToSql.Core.Query;
using TextToSql.Web.Ai;
using TextToSql.Web.Data;

namespace TextToSql.Tests.Live;

/// <summary>Real Azure SQL and Foundry calls (Entra ID through az login). Each test reseeds first, because generated SQL may change data.</summary>
[Trait("Category", "Live")]
[Collection(LiveDatabaseCollection.Name)]
public sealed class QueryLiveTests
{
    private readonly SqlConnectionFactory _connections = new(LiveConfiguration.Current);
    private readonly QueryOptions _options = new();

    private SqlServerExecutor Executor => new(_connections, _options);

    private Task ReseedAsync() => new DatabaseSeeder(_connections, NullLogger<DatabaseSeeder>.Instance).SeedAsync(CancellationToken.None);

    [LiveFact(SqlConnectionFactory.ConnectionStringKey)]
    public async Task Executor_returns_only_the_first_50_rows_and_flags_truncation() // AC5
    {
        await ReseedAsync();

        var result = await Executor.ExecuteAsync("SELECT * FROM dbo.OrderItems ORDER BY OrderItemId;", 50, CancellationToken.None);

        Assert.Equal(50, result.Rows.Count);
        Assert.True(result.Truncated);
        Assert.Equal(["OrderItemId", "OrderId", "ProductId", "Quantity", "UnitPrice"], result.Columns);
        Assert.Equal(1, Convert.ToInt32(result.Rows[0][0]));
    }

    [LiveFact(SqlConnectionFactory.ConnectionStringKey)]
    public async Task Executor_returns_rows_affected_for_a_non_query() // AC6
    {
        await ReseedAsync();

        // A no-op update: 5 beverages, data unchanged.
        var result = await Executor.ExecuteAsync("UPDATE dbo.Products SET UnitPrice = UnitPrice WHERE CategoryId = 1;", 50, CancellationToken.None);

        Assert.Equal(5, result.RowsAffected);
        Assert.Empty(result.Columns);
    }

    [LiveFact(SqlConnectionFactory.ConnectionStringKey)]
    public async Task Executor_reports_the_database_error_message() // AC7
    {
        var ex = await Assert.ThrowsAsync<SqlExecutionException>(() => Executor.ExecuteAsync("SELECT * FROM dbo.DoesNotExist;", 50, CancellationToken.None));

        Assert.Contains("Invalid object name 'dbo.DoesNotExist'", ex.Message);
    }

    [LiveFact(SqlConnectionFactory.ConnectionStringKey, FoundrySqlModelClient.ProjectEndpointKey, FoundrySqlModelClient.DeploymentKey)]
    public async Task How_many_customers_returns_the_seeded_count() // AC11
    {
        await ReseedAsync();
        var service = new QueryService(
            new SqlSchemaProvider(_connections, NullLogger<SqlSchemaProvider>.Instance),
            new FoundrySqlModelClient(LiveConfiguration.Current, AzureCredentialFactory.Create(LiveConfiguration.Current)),
            Executor,
            _options,
            NullLogger<QueryService>.Instance);

        var outcome = await service.AskAsync("How many customers are there?", CancellationToken.None);

        Assert.True(outcome.Status == QueryStatus.Success, $"Status {outcome.Status}: {outcome.Result.Error} (SQL: {outcome.Result.Sql})");
        Assert.Contains("Customers", outcome.Result.Sql);
        var row = Assert.Single(outcome.Result.Rows);
        Assert.Equal(100, Convert.ToInt32(Assert.Single(row)));
    }
}
