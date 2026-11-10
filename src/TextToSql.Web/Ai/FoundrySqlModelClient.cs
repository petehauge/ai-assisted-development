using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.Core;
using OpenAI.Responses;
using TextToSql.Core.Query;
using TextToSql.Web.Data;

namespace TextToSql.Web.Ai;

/// <summary>
/// A plain model call through the Foundry project endpoint (Responses API), authenticated with Microsoft Entra ID.
/// Follows https://learn.microsoft.com/en-us/azure/foundry/quickstarts/get-started-code (C#, updated 2026-10-19).
/// </summary>
public sealed class FoundrySqlModelClient(IConfiguration configuration, TokenCredential credential) : ISqlModelClient
{
    public const string ProjectEndpointKey = "Foundry:ProjectEndpoint";
    public const string DeploymentKey = "Foundry:DefaultDeployment";

    private readonly Lazy<ProjectResponsesClient> _client = new(() => CreateClient(configuration, credential));

    public async Task<string> CompleteAsync(string prompt, CancellationToken cancellationToken)
    {
#pragma warning disable OPENAI001 // The Responses API types are marked experimental in the OpenAI SDK.
        ResponseResult response = await _client.Value.CreateResponseAsync(prompt, cancellationToken: cancellationToken);
        return response.GetOutputText() ?? string.Empty;
#pragma warning restore OPENAI001
    }

    private static ProjectResponsesClient CreateClient(IConfiguration configuration, TokenCredential credential)
    {
        var endpoint = configuration[ProjectEndpointKey];
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            throw new MissingConfigurationException(ProjectEndpointKey);
        }

        var deployment = configuration[DeploymentKey];
        if (string.IsNullOrWhiteSpace(deployment))
        {
            throw new MissingConfigurationException(DeploymentKey);
        }

        AIProjectClient projectClient = new(endpoint: new Uri(endpoint), tokenProvider: credential);
#pragma warning disable OPENAI001
        return projectClient.ProjectOpenAIClient.GetProjectResponsesClientForModel(deployment);
#pragma warning restore OPENAI001
    }
}
