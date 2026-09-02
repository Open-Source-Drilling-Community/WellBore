using System.Text.Json.Serialization;

namespace OSDC.Drilling.WellBore.Model;

/// <summary>Complete replacement of the independently mutable WellBore details.</summary>
public sealed class WellBoreDetailsUpdate
{
    [JsonRequired]
    public string? Name { get; set; }

    [JsonRequired]
    public string? Description { get; set; }
}
