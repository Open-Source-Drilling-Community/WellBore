using OSDC.DotnetLibraries.Drilling.SemanticCatalogue;
using OSDC.DotnetLibraries.General.ResourceClassification;

namespace OSDC.Drilling.WellBore.Model;

/// <summary>WellBore FeatureOption contract backed by the shared resource classification implementation.</summary>
[Semantic(Concepts.FeatureOption)]
public class WellBoreFeatureOption : FeatureOption
{
}
