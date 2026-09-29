using OSDC.DotnetLibraries.Drilling.SemanticCatalogue;
using System.Text.Json.Serialization;

namespace OSDC.Drilling.WellBore.Model;

/// <summary>Complete replacement of the independently mutable WellBore details.</summary>
public sealed class WellBoreDetailsUpdate
{
    [JsonRequired]
    [Semantic(Concepts.ResourceName)]
    public string? Name { get; set; }

    [JsonRequired]
    [Semantic(Concepts.ResourceDescription)]
    public string? Description { get; set; }
}
