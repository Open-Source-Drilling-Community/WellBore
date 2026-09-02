using Microsoft.Extensions.DependencyInjection;

namespace OSDC.Drilling.WellBore.WebApp;

public static class ExternalWebPagesServiceCollectionExtensions
{
    public static IServiceCollection AddExternalWebPages(this IServiceCollection services, WebPagesHostConfiguration configuration)
    {
        services.AddSingleton<OSDC.Drilling.Well.WebPages.IWellWebPagesConfiguration>(configuration);
        services.AddSingleton<
            OSDC.Drilling.Well.WebPages.IWellAPIUtils,
            OSDC.Drilling.Well.WebPages.WellAPIUtils>();
        services.AddSingleton<OSDC.Drilling.Cluster.WebPages.IClusterWebPagesConfiguration>(configuration);
        services.AddSingleton<
            OSDC.Drilling.Cluster.WebPages.IClusterAPIUtils,
            OSDC.Drilling.Cluster.WebPages.ClusterAPIUtils>();
        services.AddSingleton<OSDC.Drilling.Field.WebPages.IFieldWebPagesConfiguration>(configuration);
        services.AddSingleton<
            OSDC.Drilling.Field.WebPages.IFieldAPIUtils,
            OSDC.Drilling.Field.WebPages.FieldAPIUtils>();
        services.AddSingleton<OSDC.Drilling.Rig.WebPages.IRigWebPagesConfiguration>(configuration);
        services.AddSingleton<
            OSDC.Drilling.Rig.WebPages.IRigAPIUtils,
            OSDC.Drilling.Rig.WebPages.RigAPIUtils>();
        services.AddScoped<OSDC.Drilling.Rig.WebPages.Shared.RigApiClient>();
        services.AddScoped<OSDC.Drilling.Rig.WebPages.Shared.FieldClusterApiClient>();
        services.AddSingleton<OSDC.Drilling.EarthCartographicProjection.WebPages.IEarthCartographicProjectionConfiguration>(configuration);
        services.AddSingleton<
            OSDC.Drilling.EarthCartographicProjection.WebPages.IEarthCartographicProjectionApi,
            OSDC.Drilling.EarthCartographicProjection.WebPages.EarthCartographicProjectionApi>();
        services.AddSingleton<OSDC.Drilling.EarthGeodesy.WebPages.IEarthGeodesyWebPagesConfiguration>(configuration);
        services.AddSingleton<OSDC.Drilling.EarthGeodesy.WebPages.IEarthGeodesyAPIUtils, OSDC.Drilling.EarthGeodesy.WebPages.APIUtils>();
        services.AddSingleton<OSDC.Drilling.EarthGravity.WebPages.IEarthGravityWebPagesConfiguration>(configuration);
        services.AddSingleton<OSDC.Drilling.EarthGravity.WebPages.IEarthGravityAPIUtils, OSDC.Drilling.EarthGravity.WebPages.APIUtils>();
        services.AddSingleton<OSDC.Drilling.EarthMagneticField.WebPages.IEarthMagneticFieldWebPagesConfiguration>(configuration);
        services.AddSingleton<OSDC.Drilling.EarthMagneticField.WebPages.IEarthMagneticFieldAPIUtils, OSDC.Drilling.EarthMagneticField.WebPages.APIUtils>();
        services.AddSingleton<OSDC.Drilling.EarthVerticalDatum.WebPages.IEarthVerticalDatumWebPagesConfiguration>(configuration);
        services.AddSingleton<OSDC.Drilling.EarthVerticalDatum.WebPages.IEarthVerticalDatumAPIUtils, OSDC.Drilling.EarthVerticalDatum.WebPages.APIUtils>();
        return services;
    }
}
