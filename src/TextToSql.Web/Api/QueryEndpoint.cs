using TextToSql.Core.Query;

namespace TextToSql.Web.Api;

public sealed record QueryRequest(string? Question);

public static class QueryEndpoint
{
    /// <summary>
    /// <c>POST /api/query</c> with <c>{ "question": "..." }</c>. Always returns a <see cref="QueryResult"/> body:
    /// 200 success, 400 invalid question (the model is not called), 422 the SQL failed (SQL and database error included),
    /// 502 the schema read or the model call failed.
    /// </summary>
    public static IEndpointRouteBuilder MapQueryEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/query", async (QueryRequest? request, QueryService service, CancellationToken cancellationToken) =>
        {
            var outcome = await service.AskAsync(request?.Question, cancellationToken);
            return Results.Json(outcome.Result, statusCode: ToStatusCode(outcome.Status));
        });

        return app;
    }

    public static int ToStatusCode(QueryStatus status) => status switch
    {
        QueryStatus.Success => StatusCodes.Status200OK,
        QueryStatus.InvalidQuestion => StatusCodes.Status400BadRequest,
        QueryStatus.SqlFailed => StatusCodes.Status422UnprocessableEntity,
        QueryStatus.SchemaFailed or QueryStatus.ModelFailed => StatusCodes.Status502BadGateway,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown query status."),
    };
}
