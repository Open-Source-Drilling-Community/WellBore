public static class DataUtils
{
    // default values
    public const double DEFAULT_VALUE = 999.25;
    public static string DEFAULT_NAME_WellBore = "Default WellBore Name";
    public static string DEFAULT_DESCR_WellBore = "Default WellBore Description";

    // unit management
    public static class UnitAndReferenceParameters
    {
        public static string? UnitSystemName { get; set; } = "Metric";
        public static string? DepthReferenceName { get; set; }
        public static string? PositionReferenceName { get; set; }
        public static string? AzimuthReferenceName { get; set; }
        public static string? PressureReferenceName { get; set; }
        public static string? DateReferenceName { get; set; }
    }

    public static void UpdateUnitSystemName(string val)
    {
        UnitAndReferenceParameters.UnitSystemName = (string)val;
    }

    // units and labels
    public static readonly string WellBoreMyBaseDataListLabel = "MyBaseDataList";
    public static readonly string WellBoreOutputParamLabel = "WellBoreOutputParam";
    public static readonly string WellBoreNameLabel = "WellBore name";
    public static readonly string WellBoreDescrLabel = "WellBore description";
    public static readonly string WellBoreOutputParamQty = "DepthDrilling";
}