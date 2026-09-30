using OSDC.DotnetLibraries.Drilling.SemanticCatalogue;
using OSDC.DotnetLibraries.General.ResourceClassification;

namespace OSDC.Drilling.WellBore.Model;

/// <summary>WellBore FeatureCategory contract backed by the shared resource classification implementation.</summary>
[Semantic(Concepts.FeatureCategory)]
public class WellBoreFeatureCategory : FeatureCategory<WellBoreFeatureOption>
{
}
