using OSDC.DotnetLibraries.Drilling.SemanticCatalogue;
using OSDC.DotnetLibraries.General.ResourceClassification;

namespace OSDC.Drilling.WellBore.Model;

/// <summary>WellBore IdentityAssignment contract backed by the shared resource classification implementation.</summary>
[Semantic(Concepts.IdentityAssignment)]
public class WellBoreIdentityAssignment : IdentityAssignment
{
}
