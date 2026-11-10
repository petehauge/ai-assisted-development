using Azure.Core;
using Azure.Identity;

namespace TextToSql.Web.Ai;

public static class AzureCredentialFactory
{
    public const string TenantIdKey = "Azure:TenantId";

    /// <summary>
    /// DefaultAzureCredential pinned to <c>Azure:TenantId</c> when it is set. Without the pin, a developer signed in to several tenants
    /// (for example Visual Studio in one tenant and az login in another) can get a token for the wrong tenant, which Foundry rejects with "404 Workspace not found".
    /// </summary>
    public static TokenCredential Create(IConfiguration configuration)
    {
        var tenantId = configuration[TenantIdKey];
        return new DefaultAzureCredential(new DefaultAzureCredentialOptions
        {
            TenantId = string.IsNullOrWhiteSpace(tenantId) ? null : tenantId,
        });
    }
}
