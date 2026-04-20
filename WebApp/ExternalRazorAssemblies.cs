using System.Reflection;

namespace NORCE.Drilling.WellBore.WebApp;

public static class ExternalRazorAssemblies
{
    public static IReadOnlyList<Assembly> All { get; } =
    [
        typeof(NORCE.Drilling.WellBore.WebPages.WellBoreMain).Assembly,
        typeof(NORCE.Drilling.Well.WebPages.WellMain).Assembly,
        typeof(NORCE.Drilling.Cluster.WebPages.ClusterMain).Assembly,
        typeof(NORCE.Drilling.Field.WebPages.Field).Assembly,
        typeof(NORCE.Drilling.CartographicProjection.WebPages.CartographicProjection).Assembly,
        typeof(NORCE.Drilling.GeodeticDatum.WebPages.GeodeticDatumMain).Assembly,
    ];
}
