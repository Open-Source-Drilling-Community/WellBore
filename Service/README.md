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
- `GET /WellBore/Search` — Search and page WellBores by name, topology, assignments, or modification time.
- `GET /WellBore/{id}/ExternalReferences` — Validate one stored WellBore's Well and Rig references without modifying it.
- `POST /WellBore/ExternalReferenceAudit` — Audit a bounded page of all or selected WellBores with `Valid`, `Invalid`, and `Unavailable` outcomes.
- `POST /WellBore` — Create a WellBore (requires non-empty `MetaInfo.ID`).
- `PUT /WellBore/{id}?expectedModifiedUtc=...` — Full replacement protected by optimistic concurrency.
- `PUT /WellBore/{id}/Details?expectedModifiedUtc=...` — Replace only name and description.
- `PUT /WellBore/{id}/Topology?expectedModifiedUtc=...` — Replace Well/Rig and sidetrack topology fields.
- `POST|PUT|DELETE /WellBore/{wellBoreId}/IdentityAssignments/...` — Mutate one identity assignment.
- `POST|PUT|DELETE /WellBore/{wellBoreId}/FeatureAssignments/...` — Mutate one feature assignment.
- `DELETE /WellBore/{id}?expectedModifiedUtc=...` — Delete a current revision; parent WellBores with children are protected.
- `POST /WellBore/BatchExport` — Export all or selected WellBores with referenced catalogue dependencies.
- `POST /WellBore/BatchRestore` — Validate and atomically restore a versioned backup document.
- `GET|POST /WellBoreIdentity` and `GET|PUT|DELETE /WellBoreIdentity/{id}` — Discover and manage identity definitions.
- `GET|POST /WellBoreFeatureCategory` and `GET|PUT|DELETE /WellBoreFeatureCategory/{id}` — Discover and manage feature categories/options.
- `GET /WellBoreUsageStatistics` — Retrieve per-endpoint daily usage counters.

All WellBore updates, assignment mutations, deletes, and catalogue updates or deletes require `expectedModifiedUtc` from the latest `LastModificationDate`. Stale writes return a conflict without changing data. Referenced catalogues, feature options, and parent WellBores cannot be deleted. Writes validate assignment rules and sidetrack topology before committing. Legacy rows without timestamps use a stable Unix-epoch revision and are upgraded only when explicitly written; no database migration or bulk row rewrite is required.

Batch restore uses one transaction for catalogue mapping/creation, assignment-reference rewriting, validation, and all WellBore writes. Invalid input, unresolved or ambiguous catalogue definitions, UUID conflicts under `FailIfExists`, and storage errors roll back the complete operation. It never clears the database and preserves unrelated rows.

`WellID` and `RigID` remain externally owned references, so mutations do not call another service while holding a SQLite transaction. The read-only validation endpoints use `WellHostURL` and `RigHostURL`; confirmed 404 responses are `Invalid`, while missing configuration, transport failures, timeouts, non-success dependency responses, and malformed responses are `Unavailable`. Helm defaults these URLs to `http://osdcwellservice/` and `http://osdcrigservice/`. Diagnostic checks never block or alter WellBore writes.

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
    "IsSidetrack": false,
    "SidetrackType": "Undefined"
  }'
```

Get by ID
```
curl -k "$BASE/WellBore/11111111-1111-1111-1111-111111111111"
```

Update by ID
```
curl -k -X PUT "$BASE/WellBore/11111111-1111-1111-1111-111111111111?expectedModifiedUtc=2026-09-01T08:00:00Z" \
  -H "Content-Type: application/json" \
  -d '{
    "MetaInfo": { "ID": "11111111-1111-1111-1111-111111111111" },
    "Name": "WB-01-Updated"
  }'
```

Delete by ID
```
curl -k -X DELETE "$BASE/WellBore/11111111-1111-1111-1111-111111111111?expectedModifiedUtc=2026-09-01T08:05:00Z"
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

The service publishes 38 REST-backed MCP tools plus `ping`: 24 WellBore operations and 14 identity/feature-catalogue operations. The contract includes bounded `well_bore_search`, single and paged external-reference diagnostics, granular detail/topology and assignment mutations, concurrency-protected full update/delete, and batch export/restore. Usage statistics are excluded. Every tool publishes strict input and output schemas plus read-only, destructive, idempotent, and open-world behavior annotations. Protocol failures are returned as MCP errors with normalized structured details. `TieInPointAlongHoleDepth` is expressed in meters (SI) against the fixed WGS84 vertical datum.

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
- External diagnostics: `wellHostURL` and `rigHostURL` chart values, defaulting to the in-cluster OSDC services

Use `persistence.existingClaim=wellbore-claim` while the new release references the old release's PVC. Never uninstall the PVC-owning legacy release until the claim has the Helm keep annotation and an independent backup has been verified.
