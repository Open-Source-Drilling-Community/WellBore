using OSDC.DotnetLibraries.Drilling.SemanticCatalogue;
using OSDC.DotnetLibraries.General.ResourceClassification;

namespace OSDC.Drilling.WellBore.Model;

/// <summary>WellBore Identity contract backed by the shared resource classification implementation.</summary>
[Semantic(Concepts.IdentityDefinition)]
public class WellBoreIdentity : IdentityDefinition
{
}
