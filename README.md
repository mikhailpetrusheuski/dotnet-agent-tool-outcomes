# .NET Agent Tool Outcomes

A small .NET 11 sample that models an agent tool's result as a C# 15 union, so the agent host has to handle every outcome and the same contract crosses an ASP.NET Core boundary.

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET 11](https://img.shields.io/badge/.NET-11-512BD4)](https://dotnet.microsoft.com/)
[![Stars](https://img.shields.io/github/stars/mikhailpetrusheuski/dotnet-agent-tool-outcomes?style=social)](https://github.com/mikhailpetrusheuski/dotnet-agent-tool-outcomes/stargazers)

Read the write-up: [.NET 11 Makes Agent Tool Outcomes Compiler-Checked](https://medium.com/@mikhail.petrusheuski/net-11-makes-agent-tool-outcomes-compiler-checked-6bbdd48b4995).

## Outcomes

| Case | Next step | HTTP status |
| --- | --- | --- |
| `ToolSucceeded` | Continue | 200 |
| `ToolDenied` | Request approval | 403 |
| `ToolRetryableFailure` | Retry within a bounded policy | 503 |
| `ToolPermanentFailure` | Stop or ask for corrected input | 422 |

## Three details that make the contract hold

- **CS8509 is an error.** An unhandled union case is only a compiler warning by default. `AgentToolOutcomes.csproj` turns it into an error, so a new case fails the build until every switch handles it.
- **The union has a classifier.** Every case is a JSON object, so System.Text.Json can't deserialize the union without one. `[JsonUnion(TypeClassifier = typeof(JsonUnionTypeStructuralClassifier))]` picks the case by property names, and `RetryAfterSeconds` is required so the two failure cases don't overlap.
- **The endpoint maps status codes.** Returning a union gives HTTP 200 for every case. The endpoint maps each case to its own status code.

## Requirements

- .NET 11 SDK

## Run

```bash
dotnet test
dotnet run --project src/AgentToolOutcomes
```

Then send a request:

```bash
curl -X POST http://localhost:5139/agent-tools/orders/42 \
  -H "Content-Type: application/json" \
  -d '{"hasApproval":false}'
```

The OpenAPI document is at `/openapi/v1.json`, where `ToolOutcome` is described as `anyOf` over the four cases.

## Status

Built and tested on SDK 11.0.100: the 4 tests pass, and the endpoint returned 200, 403 and 422 for the matching cases.

## License

[MIT](LICENSE)
