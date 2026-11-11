using AgentToolOutcomes;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSingleton<IOrderStore, InMemoryOrderStore>();

var app = builder.Build();

app.MapOpenApi();

app.MapPost(
    "/agent-tools/orders/{orderId:int}",
    async (
        int orderId,
        UpdateOrderRequest request,
        IOrderStore store,
        CancellationToken cancellationToken) =>
    {
        ToolOutcome outcome = await OrderTools.UpdateOrderAsync(
            orderId, request.HasApproval, store, cancellationToken);

        // The union carries the meaning; the status code is still the endpoint's job.
        int statusCode = outcome switch
        {
            ToolSucceeded => StatusCodes.Status200OK,
            ToolDenied => StatusCodes.Status403Forbidden,
            ToolRetryableFailure => StatusCodes.Status503ServiceUnavailable,
            ToolPermanentFailure => StatusCodes.Status422UnprocessableEntity
        };

        return TypedResults.Json(outcome, statusCode: statusCode);
    })
    .Produces<ToolOutcome>(StatusCodes.Status200OK)
    .Produces<ToolOutcome>(StatusCodes.Status403Forbidden)
    .Produces<ToolOutcome>(StatusCodes.Status422UnprocessableEntity)
    .Produces<ToolOutcome>(StatusCodes.Status503ServiceUnavailable);

app.Run();

public sealed record UpdateOrderRequest(bool HasApproval);
