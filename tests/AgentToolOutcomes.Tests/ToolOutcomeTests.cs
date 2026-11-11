using System.Text.Json;
using AgentToolOutcomes;

namespace AgentToolOutcomes.Tests;

public class ToolOutcomeTests
{
    [Fact]
    public void Each_outcome_maps_to_one_next_step()
    {
        Assert.Equal(NextStep.Continue, ToolPolicy.Decide(new ToolSucceeded("done", "audit/1")));
        Assert.Equal(NextStep.RequestApproval, ToolPolicy.Decide(new ToolDenied("orders.write", "approval")));
        Assert.Equal(NextStep.Retry, ToolPolicy.Decide(new ToolRetryableFailure("timeout", "slow", 5)));
        Assert.Equal(NextStep.Stop, ToolPolicy.Decide(new ToolPermanentFailure("invalid", "bad input")));
    }

    [Fact]
    public void Each_outcome_survives_a_json_round_trip()
    {
        ToolOutcome[] outcomes =
        [
            new ToolSucceeded("done", "audit/1"),
            new ToolDenied("orders.write", "approval"),
            new ToolRetryableFailure("timeout", "slow", 5),
            new ToolPermanentFailure("invalid", "bad input")
        ];

        foreach (ToolOutcome outcome in outcomes)
        {
            string json = JsonSerializer.Serialize(outcome, JsonSerializerOptions.Web);
            ToolOutcome back = JsonSerializer.Deserialize<ToolOutcome>(json, JsonSerializerOptions.Web);

            Assert.Equal(ToolPolicy.Decide(outcome), ToolPolicy.Decide(back));
        }
    }

    [Fact]
    public async Task Missing_approval_is_a_denial_not_a_failure()
    {
        ToolOutcome outcome = await OrderTools.UpdateOrderAsync(
            orderId: 42, hasApproval: false, new InMemoryOrderStore(), CancellationToken.None);

        Assert.Equal(NextStep.RequestApproval, ToolPolicy.Decide(outcome));
    }

    [Fact]
    public async Task A_store_timeout_is_retryable()
    {
        ToolOutcome outcome = await OrderTools.UpdateOrderAsync(
            orderId: 42, hasApproval: true, new TimingOutStore(), CancellationToken.None);

        Assert.Equal(NextStep.Retry, ToolPolicy.Decide(outcome));
    }

    private sealed class TimingOutStore : IOrderStore
    {
        public Task<string> UpdateAsync(int orderId, CancellationToken cancellationToken) =>
            throw new TimeoutException();
    }
}
