using OSDC.UnitConversion.DrillingRazorMudComponents;

namespace NORCE.Drilling.WellBore.WebPages;

public class GroundMudLineDepthReferenceSource : IGroundMudLineDepthReferenceSource
{
    public double? GroundMudLineDepthReference { get; set; }
}

public class RotaryTableDepthReferenceSource : IRotaryTableDepthReferenceSource
{
    public double? RotaryTableDepthReference { get; set; }
}

public class SeaWaterLevelDepthReferenceSource : ISeaWaterLevelDepthReferenceSource
{
    public double? SeaWaterLevelDepthReference { get; set; }
}

public class MeanSeaLevelDepthReferenceSource : IMeanSeaLevelDepthReferenceSource
{
    public double? MeanSeaLevelDepthReference { get; set; }
}

public class WellHeadDepthReferenceSource : IWellHeadDepthReferenceSource
{
    public double? WellHeadDepthReference { get; set; }
}
