using System;
using System.Collections.Generic;

namespace OSDC.Drilling.WellBore.Model;

public enum WellBoreBatchExportScope
{
    Unspecified = 0,
    All = 1,
    Selected = 2
}

public sealed class WellBoreBatchExportRequest
{
    public WellBoreBatchExportScope Scope { get; set; }
    public List<Guid>? WellBoreIDs { get; set; }
}

/// <summary>A portable, versioned backup of WellBores and their referenced local catalogs.</summary>
public sealed class WellBoreBatchExportDocument
{
    public const string CurrentFormatIdentifier = "OSDC.Drilling.WellBore.BatchExport";
    public const int CurrentSchemaVersion = 1;

    public string FormatIdentifier { get; set; } = CurrentFormatIdentifier;
    public int SchemaVersion { get; set; } = CurrentSchemaVersion;
    public DateTimeOffset ExportedAtUtc { get; set; }
    public WellBoreBatchCatalogDependencies CatalogDependencies { get; set; } = new();
    public List<WellBore> WellBores { get; set; } = [];
}

public sealed class WellBoreBatchCatalogDependencies
{
    public List<WellBoreIdentity> Identities { get; set; } = [];
    public List<WellBoreFeatureCategory> FeatureCategories { get; set; } = [];
}

public sealed class WellBoreBatchErrorEnvelope
{
    public string Error { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public List<WellBoreBatchError> Errors { get; set; } = [];
}

public sealed class WellBoreBatchError
{
    public int? PositionIndex { get; set; }
    public string Property { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public enum WellBoreBatchRestoreConflictPolicy
{
    Unspecified = 0,
    FailIfExists = 1,
    ReplaceExisting = 2
}

public enum WellBoreBatchCatalogRestorePolicy
{
    Unspecified = 0,
    MapExisting = 1,
    MapOrCreateMissing = 2
}

public sealed class WellBoreBatchRestoreRequest
{
    public WellBoreBatchRestoreConflictPolicy ConflictPolicy { get; set; }
    public WellBoreBatchCatalogRestorePolicy CatalogPolicy { get; set; }
    public WellBoreBatchExportDocument? Document { get; set; }
}

public sealed class WellBoreBatchRestoreResponse
{
    public DateTimeOffset RestoredAtUtc { get; set; }
    public int CreatedCount { get; set; }
    public int ReplacedCount { get; set; }
    public int CreatedCatalogDefinitionCount { get; set; }
    public int CreatedCatalogOptionCount { get; set; }
    public List<WellBoreBatchCatalogMapping> CatalogMappings { get; set; } = [];
    public List<Guid> WellBoreIDs { get; set; } = [];
}

public sealed class WellBoreBatchCatalogMapping
{
    public string Catalog { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid SourceID { get; set; }
    public Guid LocalID { get; set; }
    public string Resolution { get; set; } = string.Empty;
}
