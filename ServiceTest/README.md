# WellBore ServiceTest

This project validates the WellBore service API and its MCP surface.

## MCP coverage

- `McpToolRegistrationTests.cs` verifies the 13 core WellBore and 14 identity/feature-catalogue REST tools plus `ping`, including strict batch schemas and exclusion of usage-statistics operations.
- `McpServerHttpTests.cs` exercises MCP initialization, tool discovery, and representative calls against a running service.
- `SqlConnectionManagerSafetyTests.cs` proves that a legacy database is transactionally upgraded without changing its WellBore rows, that expected-schema startup preserves rows, and that an unexpected schema aborts without dropping data.
- `WellBoreCatalogTests.cs` verifies the default catalogues, assignment persistence (including punctuation in values), optimistic concurrency, and reference-safe deletion.
- `WellBoreBatchBackupRestoreTests.cs` verifies ordered dependency-closed exports, catalogue mapping, atomic restore, rollback on conflicts or invalid assignments, and preservation of unrelated legacy rows.

The controller and MCP HTTP tests require running services at their configured local URLs. Run the self-contained contract and database-safety tests with:

```powershell
dotnet test ServiceTest/ServiceTest.csproj --filter "FullyQualifiedName~McpToolRegistrationTests|FullyQualifiedName~SqlConnectionManagerSafetyTests|FullyQualifiedName~WellBoreCatalogTests|FullyQualifiedName~WellBoreBatchBackupRestoreTests"
```

Run the complete suite with `dotnet test ServiceTest/ServiceTest.csproj` after starting the WellBore service on its launch-profile ports and on `http://localhost:8080` for MCP.
