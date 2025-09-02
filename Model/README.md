# Model

Domain data model for the WellBore solution. This project defines the core types used across the microservice and clients to represent a wellbore and related metadata.

## Overview
- Purpose: Provide a shared, strongly-typed C# model for WellBore data used by the API service, tests, and generated clients.
- Target framework: .NET 8 (`net8.0`)
- Nullable reference types: enabled

### Key Types
- `WellBore`: Main entity with identity (`MetaInfo.ID`), descriptive fields, parent relationships for sidetracks, and an optional `TieInPointAlongHoleDepth` with Gaussian uncertainty and DWIS semantic annotations.
- `SidetrackType`: Enum classifying sidetrack wells (e.g., Technical, Production, Appraisal, Lateral).
- `UsageStatisticsWellBore`: Lightweight helper for usage telemetry (per-endpoint counters aggregated per day, persisted to `home/history.json`).

Source files:
- `WellBore.cs`
- `UsageStatisticsWellBore.cs`

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
using NORCE.Drilling.WellBore.Model;
using OSDC.DotnetLibraries.General.DataManagement; // for MetaInfo

var wb = new WellBore
{
    MetaInfo = new MetaInfo { ID = Guid.NewGuid() },
    Name = "WB-01",
    Description = "Main bore for field X",
    CreationDate = DateTimeOffset.UtcNow,
    IsSidetrack = true,
    SidetrackType = SidetrackType.Production,
    ParentWellBoreID = Guid.Parse("00000000-0000-0000-0000-000000000001")
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
- `SidetrackType` defaults to `Undefined`.

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

