using DWIS.Vocabulary.Schemas;
using OSDC.DotnetLibraries.Drilling.DrillingProperties;
using OSDC.DotnetLibraries.General.DataManagement;
using OSDC.UnitConversion.Conversion;
using OSDC.UnitConversion.Conversion.DrillingEngineering;
using System;

namespace NORCE.Drilling.WellBore.Model
{
    public enum SidetrackType { Undefined, Technical, Production, Appraisal, Lateral }
    public class WellBore
    {
        /// <summary>
        /// a MetaInfo for the WellBore
        /// </summary>
        public MetaInfo? MetaInfo { get; set; } = null;

        /// <summary>
        /// name of the data
        /// </summary>
        public string? Name { get; set; } = null;

        /// <summary>
        /// a description of the data
        /// </summary>
        public string? Description { get; set; } = null;

        /// <summary>
        /// the date when the data was created
        /// </summary>
        public DateTimeOffset? CreationDate { get; set; } = null;

        /// <summary>
        /// the date when the data was last modified
        /// </summary>
        public DateTimeOffset? LastModificationDate { get; set; } = null;

        /// <summary>
        ///  the ID of the well to which this wellBore belongs to
        /// </summary>
        public Guid? WellID { get; set; } = null;
        /// <summary>
        /// the ID of the rig used to work on this wellbore
        /// </summary>
        public Guid? RigID { get; set; } = null;
        /// <summary>
        /// indicates whether the wellbore is a sidetrack or not
        /// </summary>
        public bool IsSidetrack { get; set; }
        /// <summary>
        ///  For sideTrack's only: the ID of the wellBore to which this sideTrack belongs to
        /// </summary>
        public Guid? ParentWellBoreID { get; set; } = null;
        /// <summary>
        ///  For sideTrack's only: the tie in point along hole depth of the sideTrack provided in the parent wellBore corresponding to the wellboreID
        /// </summary>
        [AccessToVariable(CommonProperty.VariableAccessType.Assignable)]
        [Mandatory(CommonProperty.MandatoryType.General)]
        [SemanticGaussianVariable("tie_in_point_along_hole_depth", "sigma_tie_in_point_along_hole_depth")]
        [SemanticFact("tie_in_point_along_hole_depth", Nouns.Enum.DrillingSignal)]
        [SemanticFact("tie_in_point_along_hole_depth#01", Nouns.Enum.PhysicalData)]
        [SemanticFact("tie_in_point_along_hole_depth#01", Nouns.Enum.ContinuousDataType)]
        [SemanticFact("tie_in_point_along_hole_depth#01", Verbs.Enum.HasDynamicValue, "tie_in_point_along_hole_depth")]
        [SemanticFact("tie_in_point_along_hole_depth#01", Verbs.Enum.IsOfMeasurableQuantity, DrillingPhysicalQuantity.QuantityEnum.DepthDrilling)]
        [SemanticFact("MovingAverage", Nouns.Enum.MovingAverage)]
        [SemanticFact("tie_in_point_along_hole_depth#01", Verbs.Enum.IsTransformationOutput, "MovingAverage")]
        [SemanticFact("sigma_tie_in_point_along_hole_depth", Nouns.Enum.DrillingSignal)]
        [SemanticFact("sigma_tie_in_point_along_hole_depth#01", Nouns.Enum.DrillingDataPoint)]
        [SemanticFact("sigma_tie_in_point_along_hole_depth#01", Verbs.Enum.HasValue, "sigma_tie_in_point_along_hole_depth")]
        [SemanticFact("GaussianUncertainty#01", Nouns.Enum.GaussianUncertainty)]
        [SemanticFact("tie_in_point_along_hole_depth#01", Verbs.Enum.HasUncertainty, "GaussianUncertainty#01")]
        [SemanticFact("GaussianUncertainty#01", Verbs.Enum.HasUncertaintyStandardDeviation, "sigma_tie_in_point_along_hole_depth#01")]
        [SemanticFact("GaussianUncertainty#01", Verbs.Enum.HasUncertaintyMean, "tie_in_point_along_hole_depth#01")]
        [DefaultStandardDeviation(0.01)] // 1 cm
        public GaussianDrillingProperty? TieInPointAlongHoleDepth { get; set; } = null;
        /// <summary>
        /// the type of the sidetack, when the wellbore is a sidetrack
        /// </summary>
        public SidetrackType SidetrackType { get; set; } = SidetrackType.Undefined;
        /// <summary>
        /// default constructor required for JSON serialization
        /// </summary>
        public WellBore() : base()
        {
        }

    }
}
