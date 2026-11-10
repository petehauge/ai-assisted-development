using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TextToSql.Core.Query;
using TextToSql.Core.Schema;

namespace TextToSql.Tests.Web;

/// <summary>The HTTP contract of POST /api/query, with fakes for the schema, the model and the database.</summary>
public sealed class QueryEndpointTests : IDisposable
{
    private readonly FakeSchemaProvider _schema = new();
    private readonly FakeModelClient _model = new();
    private readonly FakeExecutor _executor = new();
    private readonly WebApplicationFactory<Program> _factory;

    public QueryEndpointTests()
    {
        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<ISchemaProvider>();
                services.RemoveAll<ISqlModelClient>();
                services.RemoveAll<ISqlExecutor>();
                services.AddSingleton<ISchemaProvider>(_schema);
                services.AddSingleton<ISqlModelClient>(_model);
                services.AddSingleton<ISqlExecutor>(_executor);
            });
        });
    }

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Success_returns_200_with_sql_columns_and_rows()
    {
        var (status, body) = await PostAsync(new { question = "How many customers are there?" });

        Assert.Equal(HttpStatusCode.OK, status);
        Assert.Equal("SELECT COUNT(*) AS CustomerCount FROM dbo.Customers;", body.GetProperty("sql").GetString());
        Assert.Equal("CustomerCount", body.GetProperty("columns")[0].GetString());
        Assert.Equal(100, body.GetProperty("rows")[0][0].GetInt32());
        Assert.Equal(JsonValueKind.Null, body.GetProperty("error").ValueKind);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Empty_question_returns_400_and_does_not_call_the_model(string question) // AC8
    {
        var (status, body) = await PostAsync(new { question });

        Assert.Equal(HttpStatusCode.BadRequest, status);
        Assert.False(string.IsNullOrEmpty(body.GetProperty("error").GetString()));
        Assert.Empty(_model.Prompts);
    }

    [Fact]
    public async Task Sql_failure_returns_422_with_sql_and_database_error_without_retry() // AC7
    {
        _model.Reply = "SELECT * FROM dbo.Nope;";
        _executor.Throw = new SqlExecutionException("Invalid object name 'dbo.Nope'.");

        var (status, body) = await PostAsync(new { question = "Show nope" });

        Assert.Equal((HttpStatusCode)422, status);
        Assert.Equal("SELECT * FROM dbo.Nope;", body.GetProperty("sql").GetString());
        Assert.Equal("Invalid object name 'dbo.Nope'.", body.GetProperty("error").GetString());
        Assert.Single(_model.Prompts);
        Assert.Single(_executor.Calls);
    }

    [Fact]
    public async Task Model_failure_returns_502_with_readable_error_and_no_stack_trace() // AC9
    {
        _model.Throw = new HttpRequestException("No such host is known.");

        var (status, body) = await PostAsync(new { question = "How many customers are there?" });

        Assert.Equal(HttpStatusCode.BadGateway, status);
        var error = body.GetProperty("error").GetString();
        Assert.Equal("The model call failed: No such host is known.", error);
        Assert.DoesNotContain(" at ", error);
        Assert.Empty(_executor.Calls);
    }

    private async Task<(HttpStatusCode Status, JsonElement Body)> PostAsync(object payload)
    {
        using var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/query", payload);
        return (response.StatusCode, await response.Content.ReadFromJsonAsync<JsonElement>());
    }
}
