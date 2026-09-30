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

## SemanticCatalogue 0.9.0

The checked-in service OpenAPI and sibling Well/WellBore input were refreshed for SemanticCatalogue 0.9.0, then the merged schema and C# client were regenerated. The generator processes the owning service last so its schemas take precedence over dependency snapshots; property-specific semantics on referenced types are preserved through `allOf`. Generated whitespace is normalized. Continue using the documented generator; do not hand-edit the client.


## Canonical reference adoption (0.9.0)

The own-service and sibling Well/WellBore inputs, merged schema and generated C# client have been regenerated for 0.9.0. Physical quantities and payload fields are unchanged; bindings now include resolved canonical references and their definitions.

`ClientJsonSerializerSettings.cs` is a maintained partial-client extension, not generated output. It registers `JsonStringEnumConverter` because NSwag does not attach an item converter to arrays of string enums such as `StationKeepingSystem.Modes`. Keep it linked into WebPages when regenerating `WellBoreMergedModel.cs`.
