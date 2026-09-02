using System;
using System.Text.Json.Serialization;
using OSDC.DotnetLibraries.Drilling.DrillingProperties;

namespace OSDC.Drilling.WellBore.Model;

/// <summary>Complete replacement of WellBore ownership, rig, and sidetrack relationships.</summary>
public sealed class WellBoreTopologyUpdate
{
    [JsonRequired]
    public Guid? WellID { get; set; }

    [JsonRequired]
    public Guid? RigID { get; set; }

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
