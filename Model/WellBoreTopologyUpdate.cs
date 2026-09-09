using System;
using System.Text.Json.Serialization;
using System.Collections.Generic;
using OSDC.DotnetLibraries.Drilling.DrillingProperties;

namespace OSDC.Drilling.WellBore.Model;

/// <summary>Complete replacement of WellBore ownership, rig, and sidetrack relationships.</summary>
public sealed class WellBoreTopologyUpdate
{
    [JsonRequired]
    public Guid? WellID { get; set; }

    [JsonRequired]
    [Obsolete("Use RigJobs. RigID is retained temporarily for compatibility during migration.")]
    public Guid? RigID { get; set; }

    /// <summary>
    /// Chronological rig-job history. Null preserves the compatibility behavior
    /// for legacy clients; an empty list explicitly clears the history.
    /// </summary>
    public List<RigJob>? RigJobs { get; set; }

    [JsonRequired]
    public bool IsSidetrack { get; set; }

    [JsonRequired]
    public Guid? ParentWellBoreID { get; set; }

    [JsonRequired]
    public GaussianDrillingProperty? TieInPointAlongHoleDepth { get; set; }

    /// <summary>Deprecated compatibility fallback; use a SidetrackClassification feature assignment.</summary>
    [JsonRequired]
    public SidetrackType SidetrackType { get; set; }
}
