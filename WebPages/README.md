# NORCE.Drilling.WellBore.WebPages

`NORCE.Drilling.WellBore.WebPages` is a Razor class library that packages the `WellBoreMain`, `WellBoreEdit`, and `StatisticsMain` pages together with the supporting page utilities they need.

## Contents

- `WellBoreMain`
- `WellBoreEdit`
- `StatisticsMain`
- Wellbore page support classes such as API access helpers and unit/reference helper models

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
        AdditionalAssemblies="new[] { typeof(NORCE.Drilling.WellBore.WebPages.WellBoreMain).Assembly }">
```
