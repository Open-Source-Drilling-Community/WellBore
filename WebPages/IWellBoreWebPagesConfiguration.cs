using OSDC.DotnetLibraries.Drilling.WebAppUtils;

namespace OSDC.Drilling.WellBore.WebPages;

public interface IWellBoreWebPagesConfiguration :
    IFieldHostURL,
    IClusterHostURL,
    IRigHostURL,
    IWellHostURL,
    IWellBoreHostURL,
    ITrajectoryHostURL,
    IUnitConversionHostURL
{
    string? VerticalDatumHostURL { get; set; }
}
