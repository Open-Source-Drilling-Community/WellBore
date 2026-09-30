using OSDC.DotnetLibraries.Drilling.SemanticCatalogue;
using OSDC.DotnetLibraries.General.ResourceClassification;

namespace OSDC.Drilling.WellBore.Model;

/// <summary>WellBore FeatureAssignment contract backed by the shared resource classification implementation.</summary>
[Semantic(Concepts.FeatureAssignment)]
public class WellBoreFeatureAssignment : FeatureAssignment
{
}
