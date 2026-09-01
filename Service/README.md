# WellBore Service

ASP.NET Core microservice exposing a REST API for managing WellBore domain data. It hosts OpenAPI/Swagger, persists data locally using SQLite, and integrates with the rest of the solution (Model, WebApp, ModelSharedOut).

## Purpose in the Solution
- Provides the backend API for wellbores, user-managed identity definitions, and user-managed feature categories.
- Serves a merged OpenAPI document and Swagger UI for discovery/testing.
- Persists data in a local SQLite DB at `home/WellBore.db` and tracks request usage in `home/history.json`.
- Powers the generated client in `ModelSharedOut`, which is then used by the `WebApp`.

### Base Path and Swagger
- Base path: `/WellBore/api` (set via `UsePathBase` in `Service/Program.cs:24`)
- Swagger UI: `/WellBore/api/swagger`
- Raw OpenAPI (merged): `/WellBore/api/swagger/merged/swagger.json`

## Installation
Prerequisites
- .NET 8 SDK
- Optional: Docker (for containerized deployment)

Restore tools (for `dotnet swagger` CLI)
```
dotnet tool restore
```

Build
```
dotnet build Service/Service.csproj -c Debug
```

Run locally
```
dotnet run --project Service/Service.csproj
```

Local URLs (see `Service/Properties/launchSettings.json`)
- HTTP: `http://localhost:5002/WellBore/api`
- HTTPS: `https://localhost:5001/WellBore/api`
- Swagger UI: `https://localhost:5001/WellBore/api/swagger`

SQLite storage
- Database file: `home/WellBore.db`
- Usage stats: `home/history.json`

Schema version 1 retains the existing `WellBoreTable` and transactionally adds `WellBoreIdentityTable`, `WellBoreFeatureCategoryTable`, and their indexes. Existing wellbore rows are neither rewritten nor deleted. A malformed, unknown, or newer schema aborts startup and is left unchanged; the service never drops tables as an automatic repair. In Kubernetes, preserve `wellbore-claim`, keep one writer, take and verify an independent SQLite or volume snapshot, and follow [../deployment/identity-cutover.md](../deployment/identity-cutover.md).

When their catalogues are initially empty, the service seeds the same 11 identity names used by Well: `OfficialAuthorityName`, `OperatorName`, `CompanyInternalName`, `PlanningName`, `DataManagementName`, `HistoricalName`, `ShortName`, `DisplayName`, `ReportingName`, `LegacyName`, and `ImportedName`. It also seeds feature categories for role, origin, sidetrack reason, geometry, trajectory intent, construction status, section context, completion context, data availability, and hazards. Users can add, edit, and remove catalogue entries, subject to reference-integrity and optimistic-concurrency checks.

## Docker
Build the image
```
docker build -t docker.io/digiwells/osdcdrillingwellboreservice:local -f Service/Dockerfile .
```

Run the container (map port 8080 and persist `/home` volume)
```
docker run --rm -p 8080:8080 -v wellbore-home:/home --name wellbore-service docker.io/digiwells/osdcdrillingwellboreservice:local
```

Public registry (digiwells org)
- Image name: `digiwells/osdcdrillingwellboreservice`
- Hub: https://hub.docker.com/?namespace=digiwells

## Endpoints
All routes are relative to `/WellBore/api`.

- `GET /WellBore` — List all WellBore IDs (`Guid`).
- `GET /WellBore/MetaInfo` — List `MetaInfo` of all WellBore.
- `GET /WellBore/{id}` — Get a WellBore by ID.
- `GET /WellBore/HeavyData` — Get all WellBore entities.
- `POST /WellBore` — Create a WellBore (requires non-empty `MetaInfo.ID`).
- `PUT /WellBore/{id}` — Update an existing WellBore by ID.
- `DELETE /WellBore/{id}` — Delete a WellBore by ID.
- `POST /WellBore/BatchExport` — Export all or selected WellBores with referenced catalogue dependencies.
- `POST /WellBore/BatchRestore` — Validate and atomically restore a versioned backup document.
- `GET|POST /WellBoreIdentity` and `GET|PUT|DELETE /WellBoreIdentity/{id}` — Discover and manage identity definitions.
- `GET|POST /WellBoreFeatureCategory` and `GET|PUT|DELETE /WellBoreFeatureCategory/{id}` — Discover and manage feature categories/options.
- `GET /WellBoreUsageStatistics` — Retrieve per-endpoint daily usage counters.

Catalogue update endpoints require `expectedModifiedUtc`. Stale writes return a conflict. Deleting a referenced identity/category, or removing a feature option that is in use, also returns a conflict rather than cascading or corrupting assignments. WellBore create/update validates assignment IDs, catalogue references, option membership, date ranges, validity rules, and overlapping exclusive assignments before committing.

Batch restore uses one transaction for catalogue mapping/creation, assignment-reference rewriting, validation, and all WellBore writes. Invalid input, unresolved or ambiguous catalogue definitions, UUID conflicts under `FailIfExists`, and storage errors roll back the complete operation. It never clears the database and preserves unrelated rows.

Swagger is served at `/WellBore/api/swagger` and is generated from a merged OpenAPI document: `Service/wwwroot/json-schema/WellBoreMergedModel.json`.

## Usage Examples
Set base URL
```
BASE="https://localhost:5001/WellBore/api"
```

List IDs
```
curl -k "$BASE/WellBore"
```

Create a WellBore
```
curl -k -X POST "$BASE/WellBore" \
  -H "Content-Type: application/json" \
  -d '{
    "MetaInfo": { "ID": "11111111-1111-1111-1111-111111111111" },
    "Name": "WB-01",
    "Description": "Main bore for field X",
    "IsSidetrack": true,
    "SidetrackType": "Production"
  }'
```

Get by ID
```
curl -k "$BASE/WellBore/11111111-1111-1111-1111-111111111111"
```

Update by ID
```
curl -k -X PUT "$BASE/WellBore/11111111-1111-1111-1111-111111111111" \
  -H "Content-Type: application/json" \
  -d '{
    "MetaInfo": { "ID": "11111111-1111-1111-1111-111111111111" },
    "Name": "WB-01-Updated"
  }'
```

Delete by ID
```
curl -k -X DELETE "$BASE/WellBore/11111111-1111-1111-1111-111111111111"
```

Get usage statistics
```
curl -k "$BASE/WellBoreUsageStatistics"
```

## Dependencies
From `Service/Service.csproj`:
- `Microsoft.Data.Sqlite` — SQLite database provider.
- `Microsoft.OpenApi` and `Microsoft.OpenApi.Readers` — OpenAPI model and reader.
- `Swashbuckle.AspNetCore.SwaggerGen` and `Swashbuckle.AspNetCore.SwaggerUI` — Swagger generation and UI.
- Project reference: `..\Model\Model.csproj` — shared domain types (`WellBore`, `MetaInfo`, etc.).

Tooling
- Local tool: `swashbuckle.aspnetcore.cli` (`dotnet swagger`) from `.config/dotnet-tools.json`.

Runtime behavior
- Base path and forwarded headers configured in `Service/Program.cs` for reverse proxy compatibility.
- CORS is permissive by default (all origins/headers/methods; credentials allowed).
- OpenAPI served dynamically with server URL adjusted from request headers (`SwaggerMiddlewareExtensions`).

## Integration with the Solution
- Model (`Model/`): Defines `WellBore` and related types returned/accepted by this API.
- Service (`Service/`): This project; references `Model` and persists to SQLite in `home/`.
- ModelSharedOut (`ModelSharedOut/`): Consumes the service OpenAPI to generate a merged bundle and typed C# client for downstream consumers.
  - Build target in `Service.csproj` produces `ModelSharedOut/json-schemas/WellBoreFullName.json` in Debug via `dotnet swagger`.
- WebApp (`WebApp/`): Uses the generated client from `ModelSharedOut` to call this service.
- Tests (`ServiceTest/`, `ModelTest/`): Validate behavior and contracts.

## Public URLs
- Swagger (dev): https://dev.digiwells.no/WellBore/api/swagger
- Swagger (prod): https://app.digiwells.no/WellBore/api/swagger
- API (dev): https://dev.digiwells.no/WellBore/api/WellBore
- API (prod): https://app.digiwells.no/WellBore/api/WellBore

## Source Code Template
This microservice and webapp solution was generated from a NORCE Drilling and Well Modelling team .NET template.
- Creation date: 12.06.2025
- Template version: 4.0.8
- Template repo: https://github.com/NORCE-DrillingAndWells/Templates
- Template docs: https://github.com/NORCE-DrillingAndWells/DrillingAndWells/wiki/.NET-Templates

## Funding
The current work has been funded by the [Research Council of Norway](https://www.forskningsradet.no/) and [Industry partners](https://www.digiwells.no/about/board/) in the framework of the centre for research-based innovation [SFI Digiwells (2020–2028)](https://www.digiwells.no/).

## Contributors
**Eric Cayeux**, NORCE Energy Modelling and Automation

**Gilles Pelfrene**, NORCE Energy Modelling and Automation

**Andrew Holsaeter**, NORCE Energy Modelling and Automation

**Lucas Volpi**, NORCE Energy Modelling and Automation

## MCP server

The service publishes all 13 core WellBore and 14 identity/feature-catalogue REST operations as MCP tools. This includes `well_bore_batch_export` and `well_bore_batch_restore`. Tool names use the underscore convention directly, and access-statistics operations are deliberately not registered. Each tool includes an explicit JSON input schema; create and update describe the complete WellBore payload, including identity/feature assignments, well/rig associations, sidetrack relationships, tie-in depth uncertainty, and sidetrack classification. `TieInPointAlongHoleDepth` values are always expressed in meters (SI) and referenced to the fixed WGS84 vertical datum; MCP clients must convert other units or vertical references before submitting them.

- Streamable HTTP: `/wellbore/api/mcp`
- WebSocket: `/wellbore/api/mcp/ws`
- Utility tool: `ping`
- Optional external MCP-hub registration: configured in `appsettings.json`, disabled by default

## Helm

- Service chart: `Service/charts/osdcdrillingwellboreservice`
- Image: `docker.io/digiwells/osdcdrillingwellboreservice:stable`
- Deployment/Service name: `osdcwellboreservice`
- Persistent claim: `wellbore-claim` (unchanged from the legacy deployment)
- Strategy: `Recreate`, because SQLite must not have overlapping writer pods

Use `persistence.existingClaim=wellbore-claim` while the new release references the old release's PVC. Never uninstall the PVC-owning legacy release until the claim has the Helm keep annotation and an independent backup has been verified.
