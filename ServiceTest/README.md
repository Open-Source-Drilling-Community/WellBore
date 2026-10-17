# WellBore ServiceTest

This project validates the WellBore service API and its MCP surface.

## MCP coverage

- `McpToolRegistrationTests.cs` verifies the 24 core WellBore and 14 identity/feature-catalogue REST tools plus `ping`, including schemas, behavior annotations, strict batch/audit contracts, and exclusion of usage-statistics operations.
- `WellBoreContractStrengtheningTests.cs` verifies optimistic concurrency, granular mutations, deterministic search, RigJob chronology/source rules and legacy update preservation, sidetrack integrity, reference-safe deletion, external audit paging, and non-destructive handling of legacy timestamp-free rows.
- `WellBoreExternalReferenceValidatorTests.cs` verifies Well/Rig existence checks, RigJob depth ownership against resolved Rig types, per-batch read deduplication, no-reference behavior, and the distinction between missing and unavailable dependencies.
- `McpServerHttpTests.cs` exercises MCP initialization, tool discovery, and representative calls against the in-process service.
- `SqlConnectionManagerSafetyTests.cs` proves that legacy schema upgrades preserve rows, that v2 transactionally maps deprecated sidetrack types to feature assignments without losing unrelated data, that expected-schema startup is idempotent, and that an unexpected schema aborts without dropping data.
- `WellBoreCatalogTests.cs` verifies the default catalogues, assignment persistence (including punctuation in values), optimistic concurrency, and reference-safe deletion.
- `WellBoreBatchBackupRestoreTests.cs` verifies ordered dependency-closed exports, catalogue mapping, atomic restore, normalized RigJob persistence, rollback on conflicts or invalid assignments, and preservation of unrelated legacy rows.

Run selected contract and database-safety tests with:

```powershell
dotnet test ServiceTest/ServiceTest.csproj --filter "FullyQualifiedName~McpToolRegistrationTests|FullyQualifiedName~SqlConnectionManagerSafetyTests|FullyQualifiedName~WellBoreCatalogTests|FullyQualifiedName~WellBoreBatchBackupRestoreTests"
```

Run the complete self-contained suite with `dotnet test ServiceTest/ServiceTest.csproj`. Controller and MCP fixtures use `WebApplicationFactory`; no external service or listening port is required.

## Shared classification regression checks

The ResourceClassification 0.1.0 adoption is covered by ModelTest/ClassificationContractTests (stored JSON compatibility, nullable references, concrete options and interface conversion), plus the existing isolated catalogue, backup/restore, database safety and MCP registration tests.

## SemanticCatalogue 0.18.0

`SemanticContractTests` verifies REST/MCP binding parity, inherited classification semantics and that published MCP bindings resolve to reviewed SemanticCatalogue 0.18.0 concepts. It also checks parent-path along-hole depth versus drill-floor depth, WGS84 references, LengthStandard uncertainties, rig-job endpoints/ownership and the absence of a new tie-in reference field.


## Canonical reference adoption

Semantic contract tests verify SemanticCatalogue 0.18.0 bindings and reviewed concepts. WellBore also asserts the WGS84 path-intersection origin, rejects a vertical-reference substitution and retains separate uncertainty semantics. Rig relationship UUIDs and by-id wellbore reads declare their target resource nouns in REST/MCP contracts.

## Bounded light discovery

Resource and catalog discovery returns light pages, with `offset` defaulting to 0 and `limit` defaulting to 50 (allowed range 1–200). Names and relevant resource-reference filters are evaluated on the server. Unbounded full-object `HeavyData` routes and tools, including filtered full-object lists, have been removed. Existing UUID and HTTP-metadata listings remain available; use named light pages for routine discovery.

REST routes and MCP tool names (relative to the service's existing API path base):

- `/WellBore/LightData` / `well_bore_discover`
- `/WellBoreFeatureCategory/LightData` / `well_bore_feature_category_discover`
- `/WellBoreIdentity/LightData` / `well_bore_identity_discover`

Pages contain `Items`, `Offset`, `Limit`, `TotalCount`, and `NextOffset`; `NextOffset` is null on the final page. Offset pages are individually consistent reads, not a snapshot spanning requests: concurrent inserts/deletes can change later pages. Refresh discovery when the underlying collection changes. EarthGeodesy's existing search pages instead expose `Items[].Value`, `Offset`, `Limit`, and `TotalCount`.

Single-record reads, result/chunk reads, and deliberate backup/export/restore operations are preserved. Backups can still contain complete resources and should be treated as explicit bulk operations.

This is a breaking REST/MCP contract change: update generated clients, UIs, and external integrations together with the service release. Removed heavy-list names are not compatibility aliases. No persisted engineering shapes, database names, or storage volumes are changed. Historical usage counters are retained; bounded discovery has a separate additive `DiscoveryRequestsPerDay` history in resource services.


Contract checks cover removed heavy routes/tools, bounded pagination and strict invalid-argument rejection, and omission of engineering collections from discovery. Persistence tests exercise ordering, filters, totals, continuation, empty results, and retained selected reads. Run transport tests against isolated test storage.
