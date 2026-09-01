# ModelSharedOut

`ModelSharedOut` merges the checked-in dependency OpenAPI documents with the current WellBore service schema and generates clients in the `OSDC.Drilling.WellBore.ModelShared` namespace.

Generated outputs:

- `ModelSharedOut/WellBoreMergedModel.cs`
- `Service/wwwroot/json-schema/WellBoreMergedModel.json`
- `ModelSharedOut/json-schemas/WellBoreFullName.json` from the Service Debug build

The generated contract includes WellBore batch export/restore, identity/feature catalogues, and inline assignment collections. Do not hand-edit generated outputs. From the repository root, regenerate after REST or model changes:

```powershell
dotnet build Service/Service.csproj --configuration Debug
dotnet run --project ModelSharedOut
```

Enter `Y` when prompted, then build the solution and run the tests. Commit the service schema, merged document, and generated C# client together.
