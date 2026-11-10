using TextToSql.Web.Components;
using TextToSql.Web.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Tests run in the "Testing" environment and must never read secrets.json (CLAUDE.md).
SecretsLookup? secrets = builder.Environment.IsEnvironment("Testing")
    ? null
    : builder.Configuration.AddSecretsFile(
        Environment.GetEnvironmentVariable(SecretsFileLocator.EnvironmentVariable),
        AppContext.BaseDirectory);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

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

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
