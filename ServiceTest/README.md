# WellBore ServiceTest

This project validates the WellBore service API and its MCP surface.

## MCP coverage

- `McpToolRegistrationTests.cs` verifies the 11 WellBore REST tools and `ping`, including the exclusion of usage-statistics operations.
- `McpServerHttpTests.cs` exercises MCP initialization, tool discovery, and representative calls against a running service.
- `SqlConnectionManagerSafetyTests.cs` proves that expected-schema startup preserves rows and that an unexpected schema aborts without dropping its data.

The controller and MCP HTTP tests require running services at their configured local URLs. Run the self-contained contract and database-safety tests with:

```powershell
dotnet test ServiceTest/ServiceTest.csproj --filter "FullyQualifiedName~McpToolRegistrationTests|FullyQualifiedName~SqlConnectionManagerSafetyTests"
```

Run the complete suite with `dotnet test ServiceTest/ServiceTest.csproj` after starting the WellBore service on its launch-profile ports and on `http://localhost:8080` for MCP.
