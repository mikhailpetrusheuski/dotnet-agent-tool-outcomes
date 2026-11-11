using System.Text.Json.Serialization;

namespace AgentToolOutcomes;

public sealed record ToolSucceeded(string Summary, string EvidenceId);

public sealed record ToolDenied(string RequiredPermission, string Reason);

public sealed record ToolRetryableFailure(
    string Code,
    string Message,
    [property: JsonRequired] int RetryAfterSeconds);

public sealed record ToolPermanentFailure(string Code, string Message);

// Every case is a JSON object, so the serializer needs a classifier to read one back.
[JsonUnion(TypeClassifier = typeof(JsonUnionTypeStructuralClassifier))]
public union ToolOutcome(
    ToolSucceeded,
    ToolDenied,
    ToolRetryableFailure,
    ToolPermanentFailure);

public enum NextStep
{
    Continue,
    RequestApproval,
    Retry,
    Stop
}

public static class ToolPolicy
{
    public static NextStep Decide(ToolOutcome outcome) =>
        outcome switch
        {
            ToolSucceeded => NextStep.Continue,
            ToolDenied => NextStep.RequestApproval,
            ToolRetryableFailure => NextStep.Retry,
            ToolPermanentFailure => NextStep.Stop
        };
}
