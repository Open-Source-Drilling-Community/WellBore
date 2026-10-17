# OSDC.Drilling.WellBore.WebPages

This release targets MudBlazor 9.9.0 and the matching OSDC shared web component packages.

`OSDC.Drilling.WellBore.WebPages` is a Razor class library that packages the wellbore, identity catalogue, feature catalogue, and statistics pages together with their supporting utilities.

## Contents

- `WellBoreMain`
- `WellBoreEdit`
- `WellBoreIdentities`
- `WellBoreFeatures`
- `WellBoreBackupRestore`
- `StatisticsMain`
- Wellbore page support classes such as API access helpers and unit/reference helper models

`WellBoreEdit` manages identity and feature assignments plus an optional chronological rig-job history. The history may intentionally be empty for planned or incomplete historical wellbores. It selects drill-floor-depth ownership from the Rig type: Platform Rig jobs use the depth stored on the Rig, while mobile-rig jobs edit a Gaussian WGS84 depth whose standard uncertainty defaults to 0.5 m. Catalogue pages support user-defined entries and reject removal of definitions or feature options that are still referenced by a wellbore.

`WellBoreBackupRestore` downloads logical JSON backups through a packaged JavaScript helper and restores them using the service's all-or-nothing batch endpoint. The page supports all or selected exports, previews uploaded documents, and requires an explicit conflict/catalogue policy before restore.

The trajectory and survey-run pages provide complete, searchable Field, Cluster, Well, and WellBore selectors. Typing any part of a name filters the applicable hierarchy level case-insensitively.
The same pages convert plotted North/East coordinates between WGS84, the selected Field reference point, the selected Cluster reference point, the selected Well-head slot, and the owning Field's cartographic projection. WGS84 metres remain the canonical wire values.
Uncertainty overlays use the Trajectory service's resource-specific ellipse routes so tied and sidetrack Wolff-de Wardt ancestry is reconstructed before display.
Their shared `Rotary table`/`RTE` depth choice resolves the latest rig job: it uses the job's Gaussian depth for a mobile rig or `Rig.FixedPlatformProperties.DrillFloorDepth` for a Platform Rig. The old direct WellBore Rig and Cluster fallbacks apply only when `RigJobs` is absent on a legacy record; an authoritative empty history does not infer a rig.

## Dependencies

The package depends on:

- `ModelSharedOut`

The linked generated client is accompanied by `ModelSharedOut/ClientJsonSerializerSettings.cs`, which preserves OpenAPI string-enum semantics for enum collections in dependency responses, including Rig station-keeping modes.
- `OSDC.DotnetLibraries.Drilling.WebAppUtils`
- `OSDC.DotnetLibraries.General.Math`
- `MudBlazor`
- `OSDC.UnitConversion.DrillingRazorMudComponents`

## Host application requirements

The consuming web app is expected to:

1. Reference this package.
2. Provide an implementation of `IWellBoreWebPagesConfiguration`.
3. Register that configuration and `IWellBoreAPIUtils` in dependency injection.
4. Include the library assembly in Blazor routing via `AdditionalAssemblies`.

Example registration:

```csharp
builder.Services.AddSingleton<IWellBoreWebPagesConfiguration>(new WebPagesHostConfiguration
{
    WellBoreHostURL = builder.Configuration["WellBoreHostURL"] ?? string.Empty,
    WellHostURL = builder.Configuration["WellHostURL"] ?? string.Empty,
    ClusterHostURL = builder.Configuration["ClusterHostURL"] ?? string.Empty,
    FieldHostURL = builder.Configuration["FieldHostURL"] ?? string.Empty,
    RigHostURL = builder.Configuration["RigHostURL"] ?? string.Empty,
    UnitConversionHostURL = builder.Configuration["UnitConversionHostURL"] ?? string.Empty
});
builder.Services.AddSingleton<IWellBoreAPIUtils, WellBoreAPIUtils>();
```

Example routing:

```razor
<Router AppAssembly="@typeof(App).Assembly"
        AdditionalAssemblies="new[] { typeof(OSDC.Drilling.WellBore.WebPages.WellBoreMain).Assembly }">
```

## SemanticCatalogue 0.16.0

The owning Model now consumes SemanticCatalogue 0.16.0. Semantic annotations do not add payload fields or change classification editing workflows in the generated client. The tie-in editor labels parent-wellbore MD explicitly and uses LengthStandard for tie-in and drill-floor standard uncertainties. Existing depth reference transformations remain in place.


## Canonical reference adoption (0.9.0)

The tie-in editor retains user-selected presentation references (including MSL and mud line/ground level) through the existing reference-aware component. Internal/API values remain WGS84-referenced along-hole coordinates; the selected display reference is converted at the presentation boundary.


Canonical reference metadata describes storage and REST/MCP payloads (`referenceScope: canonical-storage-and-api`), not a restriction on display choices (`presentationReferencesAllowed: true`). Web editors convert between the canonical reference and the supported reference selected by the user. Reference changes apply to coordinate values, not their standard uncertainties.

## Bounded light discovery

Resource and catalog discovery returns light pages, with `offset` defaulting to 0 and `limit` defaulting to 50 (allowed range 1–200). Names and relevant resource-reference filters are evaluated on the server. Unbounded full-object `HeavyData` routes and tools, including filtered full-object lists, have been removed. Existing UUID and HTTP-metadata listings remain available; use named light pages for routine discovery.

REST routes and MCP tool names (relative to the service's existing API path base):

- `/WellBore/LightData` / `well_bore_discover`
- `/WellBoreFeatureCategory/LightData` / `well_bore_feature_category_discover`
- `/WellBoreIdentity/LightData` / `well_bore_identity_discover`

Pages contain `Items`, `Offset`, `Limit`, `TotalCount`, and `NextOffset`; `NextOffset` is null on the final page. Offset pages are individually consistent reads, not a snapshot spanning requests: concurrent inserts/deletes can change later pages. Refresh discovery when the underlying collection changes. EarthGeodesy's existing search pages instead expose `Items[].Value`, `Offset`, `Limit`, and `TotalCount`.

Single-record reads, result/chunk reads, and deliberate backup/export/restore operations are preserved. Backups can still contain complete resources and should be treated as explicit bulk operations.

This is a breaking REST/MCP contract change: update generated clients, UIs, and external integrations together with the service release. Removed heavy-list names are not compatibility aliases. No persisted engineering shapes, database names, or storage volumes are changed. Historical usage counters are retained; bounded discovery has a separate additive `DiscoveryRequestsPerDay` history in resource services.


Tables/selectors use light discovery. Editors retrieve the selected complete resource; category options load only for chosen or already assigned definitions. Slot context uses paginated light locations, and rig-history context reads the selected wellbore. Backup selection uses discovery while the dedicated export/restore APIs retain full backup semantics.

## Package release 2.0.0

This release packages the current WebPages and generated contracts. Update consuming WebApps to `OSDC.Drilling.WellBore.WebPages` version `2.0.0` after it is available on nuget.org. This is a major update for the bounded light discovery contracts; rebuild consumers together with their updated service clients.

## Trusted NuGet publishing

The `publish-webpages-nuget.yml` workflow authenticates with GitHub OIDC through `NuGet/login@v1`; no long-lived `NUGET_API_KEY` secret is used. Set `NUGET_USER` to the nuget.org profile username as a repository variable (a repository secret is also accepted). The publishing job requests `contents: read` and `id-token: write` and obtains its temporary credential immediately before uploading.

On nuget.org, the package owner must configure a trusted-publishing policy for `Open-Source-Drilling-Community/WellBore`, workflow filename `publish-webpages-nuget.yml`, with permission to push new versions of `OSDC.Drilling.WellBore.WebPages`. Leave the policy environment empty because the workflow does not declare a GitHub environment. Trigger the workflow manually with the intended version or push a `webpages-v*` tag. Already published versions are skipped.

## Package release 2.0.1

Pins OSDC.UnitConversion.DrillingRazorMudComponents 3.4.8 explicitly so embedded unit selectors consume paginated UnitSystem discovery. This replaces the obsolete indirect dependency inherited through WebAppUtils.
