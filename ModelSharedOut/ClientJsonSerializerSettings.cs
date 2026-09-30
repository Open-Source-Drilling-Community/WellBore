namespace OSDC.Drilling.WellBore.ModelShared;

public partial class Client
{
    static partial void UpdateJsonSerializerSettings(System.Text.Json.JsonSerializerOptions settings)
    {
        // NSwag cannot currently attach an item converter to collections of enums.
        // Rig payloads contain string-valued StationKeepingMode collections, so apply
        // the same string-enum convention globally to generated client payloads.
        settings.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    }
}
