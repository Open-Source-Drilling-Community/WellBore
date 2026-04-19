using NORCE.Drilling.WellBore.WebPages;

namespace NORCE.Drilling.WellBore.WebApp;

public class WebPagesHostConfiguration : IWellBoreWebPagesConfiguration
{
    public string WellBoreHostURL { get; set; } = string.Empty;
    public string WellHostURL { get; set; } = string.Empty;
    public string ClusterHostURL { get; set; } = string.Empty;
    public string FieldHostURL { get; set; } = string.Empty;
    public string RigHostURL { get; set; } = string.Empty;
    public string TrajectoryHostURL { get; set; } = string.Empty;
    public string UnitConversionHostURL { get; set; } = string.Empty;
}
