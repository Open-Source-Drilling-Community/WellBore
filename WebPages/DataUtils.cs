using OSDC.Drilling.WellBore.ModelShared;
using OSDC.UnitConversion.DrillingRazorMudComponents;

namespace OSDC.Drilling.WellBore.WebPages;

public static class DataUtils
{
    public const double DEFAULT_VALUE = 999.25;
    public static string DEFAULT_NAME_WellBore = "Default WellBore Name";
    public static string DEFAULT_DESCR_WellBore = "Default WellBore Description";

    public static class UnitAndReferenceParameters
    {
        public static string? UnitSystemName { get; set; } = "Metric";
        public static string? DepthReferenceName { get; set; } = "WGS84";
        public static string? PositionReferenceName { get; set; } = "WGS84";
        public static string? AzimuthReferenceName { get; set; }
        public static string? PressureReferenceName { get; set; }
        public static string? DateReferenceName { get; set; }
        public static WellHeadPositionReferenceSource WellHeadPositionReferenceSource { get; set; } = new();
        public static ClusterPositionReferenceSource ClusterPositionReferenceSource { get; set; } = new();
        public static FieldPositionReferenceSource FieldPositionReferenceSource { get; set; } = new();
        public static CartographicGridPositionReferenceSource CartographicGridPositionReferenceSource { get; set; } = new();
    }

    public static void UpdateUnitSystemName(string val)
    {
        UnitAndReferenceParameters.UnitSystemName = val;
    }

    public static void UpdateDepthReferenceName(string val)
    {
        UnitAndReferenceParameters.DepthReferenceName = val;
    }

    public static void UpdatePositionReferenceName(string val)
    {
        UnitAndReferenceParameters.PositionReferenceName = val;
    }

    public static void ApplyPositionReferenceValues(Well? well, IEnumerable<Field>? fields, IEnumerable<Cluster>? clusters)
    {
        UnitAndReferenceParameters.WellHeadPositionReferenceSource.WellHeadNorthPositionReference = null;
        UnitAndReferenceParameters.WellHeadPositionReferenceSource.WellHeadEastPositionReference = null;
        UnitAndReferenceParameters.ClusterPositionReferenceSource.ClusterNorthPositionReference = null;
        UnitAndReferenceParameters.ClusterPositionReferenceSource.ClusterEastPositionReference = null;
        UnitAndReferenceParameters.FieldPositionReferenceSource.FieldNorthPositionReference = null;
        UnitAndReferenceParameters.FieldPositionReferenceSource.FieldEastPositionReference = null;

        Cluster? cluster = clusters?.FirstOrDefault(item => item?.MetaInfo?.ID == well?.ClusterID);
        UnitAndReferenceParameters.ClusterPositionReferenceSource.ClusterNorthPositionReference = -cluster?.ReferencePoint?.RiemannianNorth;
        UnitAndReferenceParameters.ClusterPositionReferenceSource.ClusterEastPositionReference = -cluster?.ReferencePoint?.RiemannianEast;

        Field? field = fields?.FirstOrDefault(item => item?.MetaInfo?.ID == cluster?.FieldID);
        UnitAndReferenceParameters.FieldPositionReferenceSource.FieldNorthPositionReference = -field?.ReferencePoint?.RiemannianNorth;
        UnitAndReferenceParameters.FieldPositionReferenceSource.FieldEastPositionReference = -field?.ReferencePoint?.RiemannianEast;

        Slot? slot = cluster?.Slots?.Values.FirstOrDefault(item => item.ID == well?.SlotID);
        if (slot?.Latitude?.GaussianValue?.Mean is not double latitude ||
            slot.Longitude?.GaussianValue?.Mean is not double longitude)
        {
            return;
        }

        var wellHead = new OSDC.DotnetLibraries.General.Math.Point3DGlobalCoordinates
        {
            Latitude = latitude,
            Longitude = longitude
        };
        UnitAndReferenceParameters.WellHeadPositionReferenceSource.WellHeadNorthPositionReference = -wellHead.RiemannianNorth;
        UnitAndReferenceParameters.WellHeadPositionReferenceSource.WellHeadEastPositionReference = -wellHead.RiemannianEast;
    }

    public static readonly string WellBoreMyBaseDataListLabel = "MyBaseDataList";
    public static readonly string WellBoreOutputParamLabel = "WellBoreOutputParam";
    public static readonly string WellBoreNameLabel = "WellBore name";
    public static readonly string WellBoreDescrLabel = "WellBore description";
    public static readonly string WellBoreOutputParamQty = "DepthDrilling";

    public class WellHeadPositionReferenceSource : IWellHeadPositionReferenceSource
    {
        public double? WellHeadNorthPositionReference { get; set; }
        public double? WellHeadEastPositionReference { get; set; }
    }

    public class ClusterPositionReferenceSource : IClusterPositionReferenceSource
    {
        public double? ClusterNorthPositionReference { get; set; }
        public double? ClusterEastPositionReference { get; set; }
    }

    public class FieldPositionReferenceSource : IFieldPositionReferenceSource
    {
        public double? FieldNorthPositionReference { get; set; }
        public double? FieldEastPositionReference { get; set; }
    }

    public class CartographicGridPositionReferenceSource : ICartographicGridPositionReferenceSource
    {
        public double? CartographicGridNorthPositionReference { get; set; }
        public double? CartographicGridEastPositionReference { get; set; }
    }
}
