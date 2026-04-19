using OSDC.DotnetLibraries.Drilling.WebAppUtils;

namespace NORCE.Drilling.WellBore.WebPages;

public interface IWellBoreWebPagesConfiguration :
    IFieldHostURL,
    IClusterHostURL,
    IRigHostURL,
    IWellHostURL,
    IWellBoreHostURL,
    ITrajectoryHostURL,
    IUnitConversionHostURL
{
}
