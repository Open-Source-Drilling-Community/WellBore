# WellBore ServiceTest

This project validates the WellBore service API and its MCP surface.

## MCP coverage

- `McpToolRegistrationTests.cs` verifies the 11 WellBore REST tools and `ping`, including the exclusion of usage-statistics operations.
- `McpServerHttpTests.cs` exercises MCP initialization, tool discovery, and representative calls against a running service.

The live HTTP tests require the WellBore service at the configured test base URL. Run the suite with `dotnet test ServiceTest/ServiceTest.csproj`.
