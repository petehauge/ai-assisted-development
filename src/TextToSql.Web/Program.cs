using Azure.Core;
using Microsoft.Data.SqlClient;
using TextToSql.Core.Query;
using TextToSql.Core.Schema;
using TextToSql.Web.Ai;
using TextToSql.Web.Api;
using TextToSql.Web.Components;
using TextToSql.Web.Configuration;
using TextToSql.Web.Data;

var builder = WebApplication.CreateBuilder(args);

// Tests run in the "Testing" environment and must never read secrets.json (CLAUDE.md).
SecretsLookup? secrets = builder.Environment.IsEnvironment("Testing")
    ? null
    : builder.Configuration.AddSecretsFile(
        Environment.GetEnvironmentVariable(SecretsFileLocator.EnvironmentVariable),
        AppContext.BaseDirectory);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSingleton<SqlConnectionFactory>();
builder.Services.AddSingleton<ISchemaProvider, SqlSchemaProvider>();
builder.Services.AddSingleton<DatabaseSeeder>();

var queryOptions = builder.Configuration.GetSection("Query").Get<QueryOptions>() ?? new QueryOptions();
builder.Services.AddSingleton(queryOptions);
builder.Services.AddSingleton<TokenCredential>(_ => AzureCredentialFactory.Create(builder.Configuration));
builder.Services.AddSingleton<ISqlModelClient, FoundrySqlModelClient>();
builder.Services.AddSingleton<ISqlExecutor, SqlServerExecutor>();
builder.Services.AddSingleton<QueryService>();

var app = builder.Build();

if (secrets is not null)
{
    // Log only the location, never the contents.
    if (secrets.Path is null)
    {
        app.Logger.LogWarning("secrets.json not loaded: {Reason}", secrets.Description);
    }
    else
    {
        app.Logger.LogInformation("{Description}", secrets.Description);
    }
}

// "dotnet run -- seed" recreates the sample database and exits instead of starting the web server.
if (args.Length > 0 && string.Equals(args[0], "seed", StringComparison.OrdinalIgnoreCase))
{
    return await SeedCommand.RunAsync(app.Services, app.Logger);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));
app.MapQueryEndpoint();

if (app.Environment.IsDevelopment())
{
    // Development aid: shows exactly the schema text the model receives.
    app.MapGet("/api/schema", async (ISchemaProvider schemaProvider, ILogger<Program> logger, CancellationToken cancellationToken) =>
    {
        try
        {
            var schema = await schemaProvider.GetSchemaAsync(cancellationToken);
            return Results.Text(SchemaFormatter.Format(schema));
        }
        catch (Exception ex) when (ex is SqlException or MissingConfigurationException)
        {
            logger.LogError(ex, "Reading the database schema failed");
            return Results.Problem(detail: ex.Message, statusCode: StatusCodes.Status502BadGateway, title: "Could not read the database schema");
        }
    });
}

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await app.RunAsync();
return 0;
