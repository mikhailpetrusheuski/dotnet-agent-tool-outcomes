namespace AgentToolOutcomes;

public interface IOrderStore
{
    Task<string> UpdateAsync(int orderId, CancellationToken cancellationToken);
}

public sealed class InMemoryOrderStore : IOrderStore
{
    public Task<string> UpdateAsync(int orderId, CancellationToken cancellationToken) =>
        Task.FromResult($"audit/order-{orderId}");
}

public static class OrderTools
{
    public static async Task<ToolOutcome> UpdateOrderAsync(
        int orderId,
        bool hasApproval,
        IOrderStore store,
        CancellationToken cancellationToken)
    {
        if (!hasApproval)
        {
            return new ToolDenied(
                RequiredPermission: "orders.write",
                Reason: "A human approval is required.");
        }

        if (orderId <= 0)
        {
            return new ToolPermanentFailure(
                Code: "invalid_order_id",
                Message: "The order ID must be positive.");
        }

        try
        {
            string auditId = await store.UpdateAsync(orderId, cancellationToken);

            return new ToolSucceeded(
                Summary: $"Order {orderId} was updated.",
                EvidenceId: auditId);
        }
        catch (TimeoutException)
        {
            return new ToolRetryableFailure(
                Code: "order_store_timeout",
                Message: "The order store did not respond.",
                RetryAfterSeconds: 5);
        }
    }
}
