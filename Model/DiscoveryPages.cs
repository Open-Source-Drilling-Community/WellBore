using System;
using System.Collections.Generic;

namespace OSDC.Drilling.WellBore.Model;

/// <summary>Discovery projection; excludes engineering arrays and nested definitions.</summary>
public sealed class WellBoreLight
{
    public OSDC.DotnetLibraries.General.DataManagement.MetaInfo? MetaInfo { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public System.DateTimeOffset? CreationDate { get; set; }
    public System.DateTimeOffset? LastModificationDate { get; set; }
    public System.Guid? WellID { get; set; }
    public System.Guid? RigID { get; set; }
    public bool IsSidetrack { get; set; }
    public System.Guid? ParentWellBoreID { get; set; }
    public global::OSDC.Drilling.WellBore.Model.SidetrackType? SidetrackType { get; set; }
}

/// <summary>A bounded UUID-ordered page. NextOffset is null on the final page.</summary>
public sealed class WellBoreDiscoveryPage
{
    public List<WellBoreLight> Items { get; set; } = new();
    public int Offset { get; set; }
    public int Limit { get; set; }
    public int TotalCount { get; set; }
    public int? NextOffset { get; set; }
}

/// <summary>Discovery projection; excludes engineering arrays and nested definitions.</summary>
public sealed class WellBoreFeatureCategoryLight
{
    public OSDC.DotnetLibraries.General.DataManagement.MetaInfo? MetaInfo { get; set; }
    public string? Name { get; set; }
    public bool IsExclusive { get; set; }
    public bool HasValidityPeriod { get; set; }
    public System.DateTimeOffset? CreationDate { get; set; }
    public System.DateTimeOffset? LastModificationDate { get; set; }
}

/// <summary>A bounded UUID-ordered page. NextOffset is null on the final page.</summary>
public sealed class WellBoreFeatureCategoryDiscoveryPage
{
    public List<WellBoreFeatureCategoryLight> Items { get; set; } = new();
    public int Offset { get; set; }
    public int Limit { get; set; }
    public int TotalCount { get; set; }
    public int? NextOffset { get; set; }
}

/// <summary>Discovery projection; excludes engineering arrays and nested definitions.</summary>
public sealed class WellBoreIdentityLight
{
    public OSDC.DotnetLibraries.General.DataManagement.MetaInfo? MetaInfo { get; set; }
    public string? Name { get; set; }
    public System.DateTimeOffset? CreationDate { get; set; }
    public System.DateTimeOffset? LastModificationDate { get; set; }
}

/// <summary>A bounded UUID-ordered page. NextOffset is null on the final page.</summary>
public sealed class WellBoreIdentityDiscoveryPage
{
    public List<WellBoreIdentityLight> Items { get; set; } = new();
    public int Offset { get; set; }
    public int Limit { get; set; }
    public int TotalCount { get; set; }
    public int? NextOffset { get; set; }
}
