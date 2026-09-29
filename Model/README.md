# Model

Domain data model for the WellBore solution. This project defines the core types used across the microservice and clients to represent a wellbore and related metadata.

## Overview
- Purpose: Provide a shared, strongly-typed C# model for WellBore data used by the API service, tests, and generated clients.
- Target framework: .NET 8 (`net8.0`)
- Nullable reference types: enabled

### Key Types
- `WellBore`: Main entity with identity (`MetaInfo.ID`), descriptive fields, parent relationships for sidetracks, an optional `TieInPointAlongHoleDepth`, optional chronological `RigJobs`, and identity/feature assignment collections.
- `RigJob` and `DrillFloorDepthSource`: A rig-history entry with a stable ID, external Rig ID, start date, optional end date, and a discriminated drill-floor-depth source. `RigJob` source requires a Gaussian depth; `Rig` source forbids one because a fixed Platform Rig owns it.
- `WellBoreIdentity` and `WellBoreIdentityAssignment`: User-managed identity definitions and values assigned to a wellbore.
- `WellBoreFeatureCategory`, `WellBoreFeatureOption`, and `WellBoreFeatureAssignment`: User-managed classifications, options, exclusivity/validity rules, and assignments.
- `SidetrackType`: Deprecated compatibility projection; new clients use the exclusive `SidetrackClassification` feature.
- `UsageStatisticsWellBore`: Lightweight helper for usage telemetry (per-endpoint counters aggregated per day, persisted to `home/history.json`).
- `WellBoreBatchExport`: Versioned logical backup/restore requests, documents, policies, errors, responses, and catalogue mappings.
- `WellBoreExternalReferenceValidation` and audit request/result types: tri-state, read-only Well/Rig reference diagnostics with bounded all/selected paging.

Source files:
- `WellBore.cs`
- `WellBoreIdentity.cs`, `WellBoreIdentityAssignment.cs`
- `WellBoreFeatureCategory.cs`, `WellBoreFeatureOption.cs`, `WellBoreFeatureAssignment.cs`
- `UsageStatisticsWellBore.cs`
- `WellBoreBatchExport.cs`
- `WellBoreExternalReferenceValidation.cs`

## Backup and restore contracts

`WellBoreBatchExportRequest` exports `All` WellBores or a non-empty ordered `Selected` UUID list. The schema-version-1 document uses format identifier `OSDC.Drilling.WellBore.BatchExport` and contains complete WellBores plus only their referenced identity and feature-catalogue dependencies. Restore supports `FailIfExists` or `ReplaceExisting` and either maps existing compatible catalogues or creates missing definitions and options.

## Dependencies
Project file: `Model/Model.csproj`
- `OSDC.DotnetLibraries.Drilling.DrillingProperties` – drilling domain properties (incl. Gaussian properties used by `WellBore`).
- `OSDC.DotnetLibraries.General.Common` – common domain utilities.
- `OSDC.DotnetLibraries.General.DataManagement` – `MetaInfo` and related data management primitives.
- `OSDC.DotnetLibraries.General.Statistics` – statistical helpers.

Notes
- The model also uses DWIS vocabulary annotations (`DWIS.Vocabulary.Schemas`) and drilling engineering unit conversion types, brought in transitively by the OSDC packages.

## Build
- From the repo root: `dotnet build Model/Model.csproj`
- Or inside the folder: `dotnet build`

## Usage Examples

Create a new `WellBore` domain object in any consumer (service, tests, or tools):

```csharp
using System;
using OSDC.Drilling.WellBore.Model;
using OSDC.DotnetLibraries.General.DataManagement; // for MetaInfo

var wb = new WellBore
{
    MetaInfo = new MetaInfo { ID = Guid.NewGuid() },
    Name = "WB-01",
    Description = "Main bore for field X",
    CreationDate = DateTimeOffset.UtcNow,
    IsSidetrack = false
};

// Optional Gaussian drilling property for tie-in depth can be set if available.
// wb.TieInPointAlongHoleDepth = ...
```

Serialize/deserialize with `System.Text.Json`:

```csharp
using System.Text.Json;

string json = JsonSerializer.Serialize(wb);
var roundtrip = JsonSerializer.Deserialize<WellBore>(json);
```

Basic defaults validated by tests (see `ModelTest`):
- Most reference properties default to `null`.
- `IsSidetrack` defaults to `false`.
- Deprecated `SidetrackType` defaults to `Undefined`; classification belongs in a `SidetrackClassification` feature assignment.
- Identity and feature assignment collections are optional for backward compatibility with stored pre-version-1 wellbores.
- `RigJobs = null` is the legacy/unmigrated state; an empty list is an authoritative and valid statement that no rig history is known. Populated histories are stored in increasing start-date order, may contain gaps, and may leave only the latest job open-ended.
- A rig-job drill-floor depth is stored in SI metres relative to WGS84. Missing standard deviation is normalized to 0.5 m. Depth references transform the mean for presentation but never transform the uncertainty.
- Deprecated `RigID` is a temporary compatibility projection of the latest job and is preserved only when `RigJobs` is absent.

## Integration In The Solution
This model is the contract shared across projects:
- Service: Exposes REST endpoints that accept/return `Model.WellBore` and related types.
  - Project reference: `Service/Service.csproj` → `..\Model\Model.csproj`
  - Controllers use `Model.WellBore` in request/response payloads (e.g., `Service/Controllers/WellBoreController.cs`).
- Tests: `ModelTest` references the model to validate defaults and behavior.
  - Project reference: `ModelTest/ModelTest.csproj` → `..\Model\Model.csproj`
- Client generation: `ModelSharedOut` consumes the service's OpenAPI to generate a typed client and a merged JSON schema; it depends on the service contract defined here.
- WebApp: Uses `ModelSharedOut` (generated client) to call the service; the underlying shapes map to the types defined in this model.

High-level data flow
- Model (this project) defines WellBore domain shapes.
- Service references Model and exposes these shapes via the API.
- ModelSharedOut generates a client and consolidated OpenAPI using the service.
- WebApp and ServiceTest consume the generated client to interact with the service.

## Documentation
This project includes a DocFX configuration (`Model/docfx.json`) to build API docs for the model types.
- Build locally (if DocFX is installed):
  - `docfx build` (from `Model/`)
  - Output goes to `Model/_site`.

## Contributing & Testing
- Run unit tests: `dotnet test ModelTest/ModelTest.csproj`
- Keep types backward compatible where possible, as they are shared contracts across the service and clients.

## ResourceClassification ownership

`WellBoreIdentity`, `WellBoreIdentityAssignment`, `WellBoreFeatureCategory`, `WellBoreFeatureOption` and `WellBoreFeatureAssignment` inherit their implementations from the published `OSDC.DotnetLibraries.General.ResourceClassification` 0.1.0 NuGet. The package reference is unconditional. `WellBoreFeatureCategory` uses `FeatureCategory<WellBoreFeatureOption>`, retaining concrete mutable options and the existing `IFeatureCategory` adapter. DataManagement 2.2.0 remains the owner of `MetaInfo` and the classification interfaces.

Property names, nullability, timestamps and caller-assigned IDs retain their serialized shape. Constructors do not generate IDs or timestamps. Model contract tests round-trip existing JSON and exercise the interface adapter. The service continues to own catalogue contents, persistence and validation.

## SemanticCatalogue 0.9.0

`OSDC.DotnetLibraries.Drilling.SemanticCatalogue` 0.9.0 supplies the reviewed vocabulary. Local model attributes bind service-owned types and properties; `ProviderSemantics` binds inherited ResourceClassification 0.1.0 members and shared metadata without redefining those package-owned types. Resource identifiers remain UUIDs. Creation/modification timestamps and classification validity endpoints use UTC instant semantics; classification validity retains its inclusive endpoints.

`TieInPointAlongHoleDepth` remains the existing Gaussian parent-wellbore along-hole property, in SI metres under the OSDC WGS84 convention. `ParentWellBoreID` identifies the path. No additional reference property is introduced, and measured depth is not described as vertical depth. Drill-floor depth is vertical depth relative to WGS84. Means use `DepthDrilling`; standard uncertainties use `LengthStandard` in metres, without a coordinate origin. `MinValue`/`MaxValue` are domain-limit metadata, not confidence limits or Gaussian truncation instructions.

Rig-job periods retain inclusive starts and exclusive ends (`[StartDate, EndDate)`), unlike classification validity intervals. Rig-owned depth is absent; job-owned depth remains required. Null versus empty RigJobs and the legacy projections retain their existing meanings.


## Canonical reference adoption (0.9.0)

The provider registry uses the shared SemanticMetadata.Create factory and OSDC canonical drilling profile. A contradictory explicit reference fails. Along-hole and vertical coordinates have distinct references; uncertainties remain origin-free. Publish SemanticCatalogue 0.9.0 before CI or Docker restore. Local verification uses a packed 0.9.0 package without a permanent local-feed configuration.

TieInPointAlongHoleDepth retains its JSON name. Its preferred concept is TieInAlongHoleDepth, with zero at the parent path intersection (or defined extension) with the WGS84 ellipsoid. No new reference field or stored-value migration is introduced. Historical values cannot be corrected from a scalar alone; their provenance and applicable parent path must first be established.


Canonical reference metadata describes storage and REST/MCP payloads (`referenceScope: canonical-storage-and-api`), not a restriction on display choices (`presentationReferencesAllowed: true`). Web editors convert between the canonical reference and the supported reference selected by the user. Reference changes apply to coordinate values, not their standard uncertainties.
