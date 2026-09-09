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
Their shared `Rotary table`/`RTE` depth choice resolves the latest rig job: it uses the job's Gaussian depth for a mobile rig or `Rig.FixedPlatformProperties.DrillFloorDepth` for a Platform Rig. The old direct WellBore Rig and Cluster fallbacks apply only when `RigJobs` is absent on a legacy record; an authoritative empty history does not infer a rig.

## Dependencies

The package depends on:

- `ModelSharedOut`
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
