using System.Reflection;

namespace OSDC.Drilling.WellBore.WebApp;

public static class ExternalRazorAssemblies
{
    public static IReadOnlyList<Assembly> All { get; } =
    [
        typeof(OSDC.Drilling.WellBore.WebPages.WellBoreMain).Assembly,
        typeof(OSDC.Drilling.Well.WebPages.WellMain).Assembly,
        typeof(OSDC.Drilling.Cluster.WebPages.ClusterMain).Assembly,
        typeof(OSDC.Drilling.Field.WebPages.Field).Assembly,
        typeof(OSDC.Drilling.Rig.WebPages.Pages.RigMain).Assembly,
        typeof(OSDC.Drilling.EarthCartographicProjection.WebPages.ProjectionDefinitions).Assembly,
    ];
}
