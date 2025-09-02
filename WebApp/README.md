# WellBore WebApp

Blazor Server web application providing a UI for managing WellBore data via the WellBore Service. Uses MudBlazor for UI, integrates with multiple domain microservices, and relies on the generated client from `ModelSharedOut`.

## Purpose in the Solution
- Frontend for CRUD operations and browsing of WellBore entities.
- Consumes the WellBore Service API and related services (UnitConversion, Field, Cluster, Well) through strongly-typed clients.
- Base path: `/WellBore/webapp` (reverse-proxy friendly).

## Installation
Prerequisites
- .NET 8 SDK
- Optional: Docker (for container deployment)

Build
```
dotnet build WebApp/WebApp.csproj -c Debug
```

Run locally
```
dotnet run --project WebApp/WebApp.csproj
```

Local URLs (see `WebApp/Properties/launchSettings.json`)
- HTTP: `http://localhost:5012/WellBore/webapp/WellBore`
- HTTPS: `https://localhost:5011/WellBore/webapp/WellBore`

Base path and configuration
- Path base set in `WebApp/Program.cs:18` via `app.UsePathBase("/WellBore/webapp")`.
- Service host URLs can be set via configuration keys or environment variables:
  - `WellBoreHostURL`
  - `UnitConversionHostURL`
  - `FieldHostURL`
  - `ClusterHostURL`
  - `WellHostURL`
- Defaults:
  - Development: `WebApp/appsettings.Development.json` contains localhost for WellBore and digiWells dev endpoints for others.
  - Production: `WebApp/appsettings.Production.json` targets in-cluster DNS names.

Example: override hosts at runtime
```
set WellBoreHostURL=https://localhost:5001/
set UnitConversionHostURL=https://dev.digiwells.no/
dotnet run --project WebApp/WebApp.csproj
```

## Usage
- Navigate to `/WellBore/webapp/WellBore` to access the main page.
- Select unit system and depth/position references via the unit/reference selector (from OSDC UnitConversion components).
- View, add, edit, and delete WellBores; associate with Field/Cluster/Well where applicable.

## Docker
Build the image
```
docker build -t norcedrillingwellborewebappclient ./WebApp
```

Run the container (map 8080 and configure service hosts)
```
docker run --rm -p 8080:8080 \
  -e WellBoreHostURL=https://my-wellbore-host/ \
  -e UnitConversionHostURL=https://my-unitconv-host/ \
  --name wellbore-webapp norcedrillingwellborewebappclient
```

Open: `http://localhost:8080/WellBore/webapp/WellBore`

## Dependencies
From `WebApp/WebApp.csproj`:
- `OSDC.DotnetLibraries.General.DataManagement` — common data primitives used in UI.
- `OSDC.UnitConversion.DrillingRazorMudComponents` — unit/reference selection UI components and services.

Transitive/runtime:
- MudBlazor — UI toolkit used via `AddMudServices` in `WebApp/Program.cs`.
- `ModelSharedOut` — project reference providing the generated typed client to call backend services.

## Integration with the Solution
- Service (`Service/`): Backend API the UI calls at `/WellBore/api`. Configure its host via `WellBoreHostURL`.
- ModelSharedOut (`ModelSharedOut/`): Generated client consumed by this WebApp for typed HTTP calls (see `WebApp/Shared/APIUtils.cs`).
- Model (`Model/`): Defines domain types the service exposes and the UI manipulates.
- Additional services: Field, Cluster, Well, and UnitConversion endpoints are referenced for richer context and unit handling.

## Public URLs
- WebApp (dev): https://dev.digiwells.no/WellBore/webapp/WellBore
- WebApp (prod): https://app.digiwells.no/WellBore/webapp/WellBore

## Funding
The current work has been funded by the [Research Council of Norway](https://www.forskningsradet.no/) and [Industry partners](https://www.digiwells.no/about/board/) in the framework of the centre for research-based innovation [SFI Digiwells (2020–2028)](https://www.digiwells.no/).

## Contributors
**Eric Cayeux**, NORCE Energy Modelling and Automation

**Gilles Pelfrene**, NORCE Energy Modelling and Automation

**Andrew Holsaeter**, NORCE Energy Modelling and Automation
