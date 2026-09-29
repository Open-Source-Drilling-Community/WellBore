using OSDC.DotnetLibraries.Drilling.SemanticCatalogue;
using DWIS.Vocabulary.Schemas;
using OSDC.DotnetLibraries.Drilling.DrillingProperties;
using OSDC.DotnetLibraries.General.DataManagement;
using OSDC.UnitConversion.Conversion;
using OSDC.UnitConversion.Conversion.DrillingEngineering;
using System;
using System.Collections.Generic;

namespace OSDC.Drilling.WellBore.Model
{
    public enum SidetrackType { Undefined, Technical, Production, Appraisal, Lateral }
    [Semantic(Concepts.WellBore)]
    public class WellBore
    {
        /// <summary>
        /// a MetaInfo for the WellBore
        /// </summary>
        [Semantic(Concepts.ResourceMetadata)]
        public MetaInfo? MetaInfo { get; set; } = null;

        /// <summary>
        /// name of the data
        /// </summary>
        [Semantic(Concepts.ResourceName)]
        public string? Name { get; set; } = null;

        /// <summary>
        /// a description of the data
        /// </summary>
        [Semantic(Concepts.ResourceDescription)]
        public string? Description { get; set; } = null;

        /// <summary>
        /// the date when the data was created
        /// </summary>
        [Semantic(Concepts.Instant, Role = Concepts.CreationTime, Reference = Concepts.Utc)]
        public DateTimeOffset? CreationDate { get; set; } = null;

        /// <summary>
        /// the date when the data was last modified
        /// </summary>
        [Semantic(Concepts.Instant, Role = Concepts.LastModificationTime, Reference = Concepts.Utc)]
        public DateTimeOffset? LastModificationDate { get; set; } = null;

        /// <summary>
        ///  the ID of the well to which this wellBore belongs to
        /// </summary>
        [Semantic(Concepts.ResourceIdentifier)]
        public Guid? WellID { get; set; } = null;
        /// <summary>
        /// Deprecated compatibility projection of the most recent rig job's RigID.
        /// New clients should use RigJobs. A null RigJobs collection identifies a
        /// legacy/unmigrated payload; an empty collection explicitly means that no
        /// rig history is known.
        /// </summary>
        [Obsolete("Use RigJobs. RigID is retained temporarily for compatibility during migration.")]
        [Semantic(Concepts.ResourceIdentifier)]
        public Guid? RigID { get; set; } = null;
        /// <summary>
        /// Chronological rig-job history. Null is the legacy/unmigrated state,
        /// empty is a valid authoritative history with no known rig, and a
        /// non-empty list is authoritative and sorted by StartDate.
        /// </summary>
        public List<RigJob>? RigJobs { get; set; } = null;
        /// <summary>
        /// indicates whether the wellbore is a sidetrack or not
        /// </summary>
        [Semantic(Concepts.SidetrackFlag)]
        public bool IsSidetrack { get; set; }
        /// <summary>
        ///  For sideTrack's only: the ID of the wellBore to which this sideTrack belongs to
        /// </summary>
        [Semantic(Concepts.ResourceIdentifier)]
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
        [Semantic(Concepts.GaussianUncertainValue)]
        [GaussianQuantity(Concepts.TieInMeasuredDepth, Concepts.LinearStandardUncertainty, Reference = Concepts.Wgs84)]
        public GaussianDrillingProperty? TieInPointAlongHoleDepth { get; set; } = null;
        /// <summary>
        /// Deprecated compatibility projection of the SidetrackClassification feature assignment.
        /// New clients should manage that exclusive feature instead.
        /// </summary>
        public SidetrackType SidetrackType { get; set; } = SidetrackType.Undefined;
        /// <summary>
        /// identities assigned to this wellbore
        /// </summary>
        public List<WellBoreIdentityAssignment>? WellBoreIdentityAssignments { get; set; }
        /// <summary>
        /// features assigned to this wellbore
        /// </summary>
        public List<WellBoreFeatureAssignment>? WellBoreFeatureAssignments { get; set; }
        /// <summary>
        /// default constructor required for JSON serialization
        /// </summary>
        public WellBore() : base()
        {
        }

    }
}
