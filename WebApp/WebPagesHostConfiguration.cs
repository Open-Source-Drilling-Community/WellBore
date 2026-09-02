using OSDC.Drilling.WellBore.WebPages;

namespace OSDC.Drilling.WellBore.WebApp;

public class WebPagesHostConfiguration :
    IWellBoreWebPagesConfiguration,
    OSDC.Drilling.Well.WebPages.IWellWebPagesConfiguration,
    OSDC.Drilling.Cluster.WebPages.IClusterWebPagesConfiguration,
    OSDC.Drilling.Field.WebPages.IFieldWebPagesConfiguration,
    OSDC.Drilling.Rig.WebPages.IRigWebPagesConfiguration,
    OSDC.Drilling.EarthCartographicProjection.WebPages.IEarthCartographicProjectionConfiguration,
    OSDC.Drilling.EarthGeodesy.WebPages.IEarthGeodesyWebPagesConfiguration,
    OSDC.Drilling.EarthGravity.WebPages.IEarthGravityWebPagesConfiguration,
    OSDC.Drilling.EarthMagneticField.WebPages.IEarthMagneticFieldWebPagesConfiguration,
    OSDC.Drilling.EarthVerticalDatum.WebPages.IEarthVerticalDatumWebPagesConfiguration
{
    public string? WellBoreHostURL { get; set; } = string.Empty;
    public string? WellHostURL { get; set; } = string.Empty;
    public string? ClusterHostURL { get; set; } = string.Empty;
    public string? FieldHostURL { get; set; } = string.Empty;
    public string? RigHostURL { get; set; } = string.Empty;
    public string? TrajectoryHostURL { get; set; } = string.Empty;
    public string? EarthCartographicProjectionHostURL { get; set; } = string.Empty;
    public string? EarthGeodesyHostURL { get; set; } = string.Empty;
    public string EarthGravityHostURL { get; set; } = string.Empty;
    public string EarthMagneticFieldHostURL { get; set; } = string.Empty;
    public string? EarthVerticalDatumHostURL { get; set; } = string.Empty;
    public string? VerticalDatumHostURL
    {
        get => EarthVerticalDatumHostURL;
        set => EarthVerticalDatumHostURL = value;
    }
    public string? UnitConversionHostURL { get; set; } = string.Empty;

    string OSDC.Drilling.EarthCartographicProjection.WebPages.IEarthCartographicProjectionConfiguration.ServiceUrl => EarthCartographicProjectionHostURL ?? string.Empty;
    string OSDC.Drilling.EarthCartographicProjection.WebPages.IEarthCartographicProjectionConfiguration.EarthGeodesyUrl => EarthGeodesyHostURL ?? string.Empty;
    string OSDC.Drilling.EarthCartographicProjection.WebPages.IEarthCartographicProjectionConfiguration.UnitConversionUrl => UnitConversionHostURL ?? string.Empty;
    string OSDC.Drilling.EarthGeodesy.WebPages.IEarthGeodesyWebPagesConfiguration.EarthGeodesyHostURL => EarthGeodesyHostURL ?? string.Empty;
    string OSDC.Drilling.EarthVerticalDatum.WebPages.IEarthVerticalDatumWebPagesConfiguration.EarthVerticalDatumHostURL => EarthVerticalDatumHostURL ?? string.Empty;
    string OSDC.Drilling.Rig.WebPages.IRigWebPagesConfiguration.VerticalDatumHostURL
    {
        get => EarthVerticalDatumHostURL ?? string.Empty;
        set => EarthVerticalDatumHostURL = value;
    }
    string? OSDC.Drilling.Field.WebPages.IFieldWebPagesConfiguration.VerticalDatumHostURL
    {
        get => EarthVerticalDatumHostURL;
        set => EarthVerticalDatumHostURL = value;
    }
}
