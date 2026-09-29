using OSDC.DotnetLibraries.Drilling.SemanticCatalogue;
using DWIS.Vocabulary.Schemas;
using OSDC.DotnetLibraries.Drilling.DrillingProperties;
using OSDC.UnitConversion.Conversion;
using OSDC.UnitConversion.Conversion.DrillingEngineering;
using System;

namespace OSDC.Drilling.WellBore.Model;

/// <summary>Identifies where the drill-floor depth for a rig job is owned.</summary>
public enum DrillFloorDepthSource
{
    Undefined,
    Rig,
    RigJob
}

/// <summary>
/// A dated period during which a rig worked on the wellbore. A null end date is
/// reserved for the last, open-ended job; gaps between jobs are allowed.
/// </summary>
[Semantic(Concepts.RigJob)]
public sealed class RigJob
{
    /// <summary>Stable identifier used to address this history entry.</summary>
    [Semantic(Concepts.ResourceIdentifier)]
    public Guid RigJobID { get; set; }

    /// <summary>Identifier of the externally owned Rig resource.</summary>
    [Semantic(Concepts.ResourceIdentifier)]
    public Guid RigID { get; set; }

    /// <summary>Inclusive start of the rig job.</summary>
    [Semantic(Concepts.Instant, Role = Concepts.RigJobStart, Reference = Concepts.Utc)]
    public DateTimeOffset StartDate { get; set; }

    /// <summary>Exclusive end of the rig job, or null for the last open-ended job.</summary>
    [Semantic(Concepts.Instant, Role = Concepts.RigJobEnd, Reference = Concepts.Utc)]
    public DateTimeOffset? EndDate { get; set; }

    /// <summary>
    /// Rig for a fixed platform whose depth is owned by the Rig, or RigJob when
    /// the depth varies between jobs and is stored in this entry.
    /// </summary>
    [Semantic(Concepts.DrillFloorDepthSource)]
    public DrillFloorDepthSource DrillFloorDepthSource { get; set; }

    /// <summary>
    /// Drill-floor vertical depth in SI metres relative to WGS84. Required only
    /// when DrillFloorDepthSource is RigJob and forbidden when it is Rig.
    /// </summary>
    [AccessToVariable(CommonProperty.VariableAccessType.Assignable)]
    [Mandatory(CommonProperty.MandatoryType.General)]
    [SemanticGaussianVariable("drill_floor_depth_rig_job", "sigma_drill_floor_depth_rig_job")]
    [SemanticFact("drill_floor_depth_rig_job", Nouns.Enum.DrillingSignal)]
    [SemanticFact("drill_floor_depth_rig_job#01", Nouns.Enum.PhysicalData)]
    [SemanticFact("drill_floor_depth_rig_job#01", Nouns.Enum.ContinuousDataType)]
    [SemanticFact("drill_floor_depth_rig_job#01", Verbs.Enum.HasDynamicValue, "drill_floor_depth_rig_job")]
    [SemanticFact("drill_floor_depth_rig_job#01", Verbs.Enum.IsOfMeasurableQuantity, DrillingPhysicalQuantity.QuantityEnum.DepthDrilling)]
    [SemanticFact("MovingAverage", Nouns.Enum.MovingAverage)]
    [SemanticFact("drill_floor_depth_rig_job#01", Verbs.Enum.IsTransformationOutput, "MovingAverage")]
    [SemanticFact("sigma_drill_floor_depth_rig_job", Nouns.Enum.DrillingSignal)]
    [SemanticFact("sigma_drill_floor_depth_rig_job#01", Nouns.Enum.DrillingDataPoint)]
    [SemanticFact("sigma_drill_floor_depth_rig_job#01", Verbs.Enum.HasValue, "sigma_drill_floor_depth_rig_job")]
    [SemanticFact("GaussianUncertainty#01", Nouns.Enum.GaussianUncertainty)]
    [SemanticFact("drill_floor_depth_rig_job#01", Verbs.Enum.HasUncertainty, "GaussianUncertainty#01")]
    [SemanticFact("GaussianUncertainty#01", Verbs.Enum.HasUncertaintyStandardDeviation, "sigma_drill_floor_depth_rig_job#01")]
    [SemanticFact("GaussianUncertainty#01", Verbs.Enum.HasUncertaintyMean, "drill_floor_depth_rig_job#01")]
    [DefaultStandardDeviation(0.5)]
    [Semantic(Concepts.GaussianUncertainValue)]
    [GaussianQuantity(Concepts.DrillFloorDepth, Concepts.LinearStandardUncertainty, Reference = Concepts.Wgs84)]
    public GaussianDrillingProperty? DrillFloorDepth { get; set; }
}
