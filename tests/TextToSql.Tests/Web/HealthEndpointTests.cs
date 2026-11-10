using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TextToSql.Tests.Web;

public sealed class HealthEndpointTests : IClassFixture<HealthEndpointTests.Factory>
{
    // The "Testing" environment keeps the app from loading secrets.json.
    public sealed class Factory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.UseEnvironment("Testing");
    }

    private readonly Factory _factory;

    public HealthEndpointTests(Factory factory) => _factory = factory;

    [Fact]
    public async Task Health_returns_ok_status()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<HealthBody>();
        Assert.Equal("ok", body?.Status);
    }

    private sealed record HealthBody(string Status);
}
