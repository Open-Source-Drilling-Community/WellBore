# WellBore

End-to-end solution for WellBore management consisting of a backend microservice (REST API), a Blazor Server web application, a shared domain model, and generated client SDK. Targets .NET 8.

## Projects
- Model (`Model/`)
  - Shared C# domain types (e.g., `WellBore`, `SidetrackType`, usage stats helper).
- Service (`Service/`)
  - ASP.NET Core API exposing CRUD endpoints for `WellBore`; persists to SQLite in `home/WellBore.db` and serves Swagger at `/WellBore/api/swagger`.
- ModelSharedOut (`ModelSharedOut/`)
  - Generates a merged OpenAPI bundle and a typed C# client for consumers of the Service.
- WebApp (`WebApp/`)
  - Blazor Server UI that consumes the Service and related microservices via the generated client.
- Tests (`ModelTest/`, `ServiceTest/`)
  - Unit and integration tests for model and service.
- Data (`home/`)
  - Local storage used by the Service: `home/WellBore.db` (SQLite) and `home/history.json` (usage stats).

## Prerequisites
- .NET 8 SDK
- Optional: Docker (for containerized builds)

## Quick Start
Build the entire solution
```
dotnet build WellBore.sln -c Debug
```

Run the Service (API)
```
dotnet run --project Service/Service.csproj
# Swagger UI:   https://localhost:5001/WellBore/api/swagger
# API base:     https://localhost:5001/WellBore/api
# Main route:   https://localhost:5001/WellBore/api/WellBore
```

Run the WebApp (UI)
```
dotnet run --project WebApp/WebApp.csproj
# UI:           https://localhost:5011/WellBore/webapp/WellBore
```

Configure backend hosts for the WebApp (optional)
```
# Example overrides for local dev
set WellBoreHostURL=https://localhost:5001/
set UnitConversionHostURL=https://dev.digiwells.no/
dotnet run --project WebApp/WebApp.csproj
```

Run tests
```
dotnet test WellBore.sln
```

## Usage Examples (API)
Set base URL
```
BASE="https://localhost:5001/WellBore/api"
```

List WellBore IDs
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

Get a WellBore by ID
```
curl -k "$BASE/WellBore/11111111-1111-1111-1111-111111111111"
```

## Docker
Service
```
docker build -t norcedrillingwellboreservice ./Service
docker run --rm -p 8080:8080 -v wellbore-home:/home --name wellbore-service norcedrillingwellboreservice
# Swagger: http://localhost:8080/WellBore/api/swagger
```

WebApp
```
docker build -t norcedrillingwellborewebappclient ./WebApp
docker run --rm -p 8081:8080 \
  -e WellBoreHostURL=http://host.docker.internal:8080/ \
  --name wellbore-webapp norcedrillingwellborewebappclient
# UI:     http://localhost:8081/WellBore/webapp/WellBore
```

Public registry (digiwells org)
- https://hub.docker.com/?namespace=digiwells

## Dependencies
Solution-wide
- .NET 8; ASP.NET Core; SQLite via `Microsoft.Data.Sqlite`; OpenAPI tooling via `Swashbuckle.AspNetCore` and `Microsoft.OpenApi`.
- Domain/utility libraries from OSDC (DrillingProperties, General.*) used in the Model and Service.
- MudBlazor UI toolkit and OSDC UnitConversion components used by the WebApp.

Project references
- `Service` → `Model`
- `WebApp` → `ModelSharedOut`

## Deployment
Microservice
- Dev: https://dev.digiwells.no/WellBore/api/WellBore
- Prod: https://app.digiwells.no/WellBore/api/WellBore

WebApp
- Dev: https://dev.digiwells.no/WellBore/webapp/WellBore
- Prod: https://app.digiwells.no/WellBore/webapp/WellBore

Swagger
- Dev: https://dev.digiwells.no/WellBore/api/swagger
- Prod: https://app.digiwells.no/WellBore/api/swagger

The microservice and webapp are typically deployed as Docker containers using Kubernetes and Helm. More info: https://github.com/NORCE-DrillingAndWells/DrillingAndWells/wiki

## Security/Confidentiality
- Data persist in clear text within a single SQLite DB inside the Service container: `home/WellBore.db`.
- No authentication/authorization is included by default. If protection is required, deploy behind an ingress with auth and secure persistence.

## Funding

The current work has been funded by the [Research Council of Norway](https://www.forskningsradet.no/) and [Industry partners](https://www.digiwells.no/about/board/) in the framework of the cent for research-based innovation [SFI Digiwells (2020-2028)](https://www.digiwells.no/) focused on Digitalization, Drilling Engineering and GeoSteering. 

## Contributors

**Eric Cayeux**, *NORCE Energy Modelling and Automation*

**Gilles Pelfrene**, *NORCE Energy Modelling and Automation*

**Andrew Holsaeter**, *NORCE Energy Modelling and Automation*

**Lucas Volpi**, *NORCE Energy Modelling and Automation*
