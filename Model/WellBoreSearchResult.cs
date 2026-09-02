using System.Collections.Generic;

namespace OSDC.Drilling.WellBore.Model;

/// <summary>A deterministic page of WellBores matching server-side filters.</summary>
public sealed class WellBoreSearchResult
{
    public List<WellBore> Items { get; set; } = [];
    public int Total { get; set; }
    public int Offset { get; set; }
    public int Limit { get; set; }
}
