# ModelSharedOut

`ModelSharedOut` merges the checked-in dependency OpenAPI documents with the current WellBore service schema and generates clients in the `OSDC.Drilling.WellBore.ModelShared` namespace.

Generated outputs:

- `ModelSharedOut/WellBoreMergedModel.cs`
- `Service/wwwroot/json-schema/WellBoreMergedModel.json`
- `ModelSharedOut/json-schemas/WellBoreFullName.json` from the Service Debug build

The generated contract includes search/pagination, optimistic-concurrency parameters, granular WellBore and assignment mutations, read-only Well/Rig reference validation and auditing, batch export/restore, identity/feature catalogues, inline assignment collections, and optional RigJob history. `RigJobs = null` identifies a legacy payload while an empty collection is authoritative. `RigID` remains a deprecated latest-job projection during migration. Sidetrack classification is represented by the exclusive `SidetrackClassification` feature; the generated `SidetrackType` property remains only as a deprecated transition field for older clients. Date-time query parameters use the round-trip format so concurrency revisions retain their fractional precision and UTC offset. The merger gives the current `WellBoreFullName.json` paths precedence over stale route copies carried by dependency documents. Do not hand-edit generated outputs. From the repository root, regenerate after REST or model changes:

Dependency inputs must be copied from each owning service's `ModelSharedOut/json-schemas/*FullName.json` artifact (including Field, Cluster, Well, Rig, and Trajectory), not from a served merged aggregate. This prevents stale transitive model and route copies from overriding the owning service's current contract.

`json-schemas/TrajectoryModel.json` is refreshed from Trajectory's service-owned
`ModelSharedOut/json-schemas/TrajectoryFullName.json`. Do not use Trajectory's
served merged aggregate here because it includes transitive snapshots of sibling schemas.

```powershell
dotnet build Service/Service.csproj --configuration Debug
dotnet run --project ModelSharedOut
```

Enter `Y` when prompted, then build the solution and run the tests. Commit the service schema, merged document, and generated C# client together.

## ResourceClassification 0.1.0 migration verification

The service-specific classification types now inherit shared implementations. A fresh Service OpenAPI export was compared with the pre-migration export and this generator's checked-in service input: all paths and schemas are identical. Existing merged contracts and generated clients remain valid, so no generated source changes are required for this implementation-only migration.

## SemanticCatalogue 0.16.0

The owning Model now consumes SemanticCatalogue 0.16.0. When OpenAPI inputs are refreshed, the merged schema and C# client preserve property-specific semantics through `allOf`; the generator processes the owning service last so its schemas take precedence over dependency snapshots. Generated whitespace is normalized. Continue using the documented generator; do not hand-edit the client.


## Canonical reference adoption (0.9.0)

The own-service and sibling Well/WellBore inputs, merged schema and generated C# client have been regenerated for 0.9.0. Physical quantities and payload fields are unchanged; bindings now include resolved canonical references and their definitions.

`ClientJsonSerializerSettings.cs` is a maintained partial-client extension, not generated output. It registers `JsonStringEnumConverter` because NSwag does not attach an item converter to arrays of string enums such as `StationKeepingSystem.Modes`. Keep it linked into WebPages when regenerating `WellBoreMergedModel.cs`.

## Bounded light discovery

Resource and catalog discovery returns light pages, with `offset` defaulting to 0 and `limit` defaulting to 50 (allowed range 1–200). Names and relevant resource-reference filters are evaluated on the server. Unbounded full-object `HeavyData` routes and tools, including filtered full-object lists, have been removed. Existing UUID and HTTP-metadata listings remain available; use named light pages for routine discovery.

REST routes and MCP tool names (relative to the service's existing API path base):

- `/WellBore/LightData` / `well_bore_discover`
- `/WellBoreFeatureCategory/LightData` / `well_bore_feature_category_discover`
- `/WellBoreIdentity/LightData` / `well_bore_identity_discover`

Pages contain `Items`, `Offset`, `Limit`, `TotalCount`, and `NextOffset`; `NextOffset` is null on the final page. Offset pages are individually consistent reads, not a snapshot spanning requests: concurrent inserts/deletes can change later pages. Refresh discovery when the underlying collection changes. EarthGeodesy's existing search pages instead expose `Items[].Value`, `Offset`, `Limit`, and `TotalCount`.

Single-record reads, result/chunk reads, and deliberate backup/export/restore operations are preserved. Backups can still contain complete resources and should be treated as explicit bulk operations.

This is a breaking REST/MCP contract change: update generated clients, UIs, and external integrations together with the service release. Removed heavy-list names are not compatibility aliases. No persisted engineering shapes, database names, or storage volumes are changed. Historical usage counters are retained; bounded discovery has a separate additive `DiscoveryRequestsPerDay` history in resource services.


Regenerate from the owning service's freshly compiled OpenAPI, refresh dependent schema snapshots, then regenerate consuming clients/DTOs. Do not edit generated C# to restore removed methods. Keep the hand-authored discovery client extensions alongside generated clients: they iterate bounded light pages when a complete name/reference list is needed. Reference projections are partial display/context objects; retrieve a selected complete record before full-replacement writes or engineering calculations. Schema-name normalization must also rewrite discriminator mappings.
