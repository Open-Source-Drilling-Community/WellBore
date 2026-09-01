using System;
using System.Collections.Generic;

namespace OSDC.Drilling.WellBore.Model;

/// <summary>
/// Stable error envelope for WellBore and locally owned catalog mutations.
/// </summary>
public sealed class WellBoreMutationErrorEnvelope
{
    public string Error { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public List<WellBoreMutationError> Errors { get; set; } = [];
}

/// <summary>
/// Identifies an invalid reference, an active dependent reference, or a stale
/// optimistic-concurrency token.
/// </summary>
public sealed class WellBoreMutationError
{
    public string Property { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public List<Guid> ReferencingWellBoreIDs { get; set; } = [];
}


