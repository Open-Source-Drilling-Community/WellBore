# WellBore ServiceTest

This project validates the WellBore service API and its MCP surface.

## MCP coverage

- `McpToolRegistrationTests.cs` verifies the 24 core WellBore and 14 identity/feature-catalogue REST tools plus `ping`, including schemas, behavior annotations, strict batch/audit contracts, and exclusion of usage-statistics operations.
- `WellBoreContractStrengtheningTests.cs` verifies optimistic concurrency, granular mutations, deterministic search, RigJob chronology/source rules and legacy update preservation, sidetrack integrity, reference-safe deletion, external audit paging, and non-destructive handling of legacy timestamp-free rows.
- `WellBoreExternalReferenceValidatorTests.cs` verifies Well/Rig existence checks, RigJob depth ownership against resolved Rig types, per-batch read deduplication, no-reference behavior, and the distinction between missing and unavailable dependencies.
- `McpServerHttpTests.cs` exercises MCP initialization, tool discovery, and representative calls against a running service.
- `SqlConnectionManagerSafetyTests.cs` proves that legacy schema upgrades preserve rows, that v2 transactionally maps deprecated sidetrack types to feature assignments without losing unrelated data, that expected-schema startup is idempotent, and that an unexpected schema aborts without dropping data.
- `WellBoreCatalogTests.cs` verifies the default catalogues, assignment persistence (including punctuation in values), optimistic concurrency, and reference-safe deletion.
- `WellBoreBatchBackupRestoreTests.cs` verifies ordered dependency-closed exports, catalogue mapping, atomic restore, normalized RigJob persistence, rollback on conflicts or invalid assignments, and preservation of unrelated legacy rows.

The controller and MCP HTTP tests require running services at their configured local URLs. Run the self-contained contract and database-safety tests with:

```powershell
dotnet test ServiceTest/ServiceTest.csproj --filter "FullyQualifiedName~McpToolRegistrationTests|FullyQualifiedName~SqlConnectionManagerSafetyTests|FullyQualifiedName~WellBoreCatalogTests|FullyQualifiedName~WellBoreBatchBackupRestoreTests"
```

Run the complete suite with `dotnet test ServiceTest/ServiceTest.csproj` after starting the WellBore service on its launch-profile ports and on `http://localhost:8080` for MCP.

## Shared classification regression checks

The ResourceClassification 0.1.0 adoption is covered by ModelTest/ClassificationContractTests (stored JSON compatibility, nullable references, concrete options and interface conversion), plus the existing isolated catalogue, backup/restore, database safety and MCP registration tests. Live HTTP tests require a separately started test service.

## SemanticCatalogue 0.15.0

`SemanticContractTests` verifies REST/MCP binding parity, inherited classification semantics and that published MCP bindings resolve to reviewed SemanticCatalogue 0.15.0 concepts. It also checks parent-path along-hole depth versus drill-floor depth, WGS84 references, LengthStandard uncertainties, rig-job endpoints/ownership and the absence of a new tie-in reference field.


## Canonical reference adoption (0.9.0)

Semantic contract tests verify the 0.9.0 bindings and reviewed concepts. WellBore also asserts the WGS84 path-intersection origin, rejects a vertical-reference substitution and retains separate uncertainty semantics.
