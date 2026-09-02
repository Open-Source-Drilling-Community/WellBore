using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OSDC.Drilling.WellBore.Model;

public enum WellBoreExternalReferenceValidationStatus
{
    Valid,
    Invalid,
    Unavailable
}

public sealed class WellBoreExternalReferenceIssue
{
    public string Property { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public sealed class WellBoreExternalReferenceValidation
{
    public Guid WellBoreID { get; set; }
    public Guid? WellID { get; set; }
    public Guid? RigID { get; set; }
    public bool? WellExists { get; set; }
    public bool? RigExists { get; set; }
    public WellBoreExternalReferenceValidationStatus Status { get; set; }
    public DateTimeOffset CheckedAtUtc { get; set; }
    public List<WellBoreExternalReferenceIssue> Issues { get; set; } = [];
}

public enum WellBoreExternalReferenceAuditScope
{
    All,
    Selected
}

public sealed class WellBoreExternalReferenceAuditRequest
{
    [JsonRequired]
    public WellBoreExternalReferenceAuditScope Scope { get; set; }
    public List<Guid>? WellBoreIDs { get; set; }
    public int Offset { get; set; }
    public int Limit { get; set; } = 100;
}

public sealed class WellBoreExternalReferenceAuditResult
{
    public DateTimeOffset CheckedAtUtc { get; set; }
    public int Total { get; set; }
    public int Offset { get; set; }
    public int Limit { get; set; }
    public int ValidCount { get; set; }
    public int InvalidCount { get; set; }
    public int UnavailableCount { get; set; }
    public List<WellBoreExternalReferenceValidation> Items { get; set; } = [];
}
