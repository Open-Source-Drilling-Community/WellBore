# OSDC.Drilling.WellBore.WebPages

`OSDC.Drilling.WellBore.WebPages` is a Razor class library that packages the wellbore, identity catalogue, feature catalogue, and statistics pages together with their supporting utilities.

## Contents

- `WellBoreMain`
- `WellBoreEdit`
- `WellBoreIdentities`
- `WellBoreFeatures`
- `WellBoreBackupRestore`
- `StatisticsMain`
- Wellbore page support classes such as API access helpers and unit/reference helper models

`WellBoreEdit` manages identity and feature assignments. Catalogue pages support user-defined entries and reject removal of definitions or feature options that are still referenced by a wellbore.

`WellBoreBackupRestore` downloads logical JSON backups through a packaged JavaScript helper and restores them using the service's all-or-nothing batch endpoint. The page supports all or selected exports, previews uploaded documents, and requires an explicit conflict/catalogue policy before restore.

## Dependencies

The package depends on:

- `ModelSharedOut`
- `OSDC.DotnetLibraries.Drilling.WebAppUtils`
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
