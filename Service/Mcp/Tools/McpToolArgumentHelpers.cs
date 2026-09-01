using System;
using System.Text.Json.Nodes;

namespace OSDC.Drilling.WellBore.Service.Mcp.Tools;

internal static class McpToolArgumentHelpers
{
    public static JsonObject CreateEmptySchema()
    {
        return new JsonObject
        {
            ["type"] = "object",
            ["additionalProperties"] = false
        };
    }

    public static JsonObject CreateGuidSchema(string key, string description)
    {
        return new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject
            {
                [key] = new JsonObject
                {
                    ["type"] = "string",
                    ["format"] = "uuid",
                    ["description"] = description
                }
            },
            ["required"] = new JsonArray
            {
                key
            },
            ["additionalProperties"] = false
        };
    }

    public static JsonObject CreateWellBoreSchema(bool includeId = false)
    {
        var properties = new JsonObject { ["wellBore"] = CreateWellBoreObjectSchema() };
        var required = new JsonArray { "wellBore" };
        if (includeId)
        {
            properties["id"] = new JsonObject
            {
                ["type"] = "string",
                ["format"] = "uuid",
                ["description"] = "Identifier of the stored wellbore to update. It must equal wellBore.MetaInfo.ID."
            };
            required.Add("id");
        }
        return new JsonObject
        {
            ["type"] = "object",
            ["properties"] = properties,
            ["required"] = required,
            ["additionalProperties"] = false
        };
    }

    public static JsonObject CreateWellBoreIdentitySchema(bool includeId = false) =>
        WrapCatalogBody("wellBoreIdentity", CreateIdentityDefinitionSchema(), includeId, "wellBoreIdentity.MetaInfo.ID");

    public static JsonObject CreateWellBoreFeatureCategorySchema(bool includeId = false) =>
        WrapCatalogBody("wellBoreFeatureCategory", CreateFeatureCategoryDefinitionSchema(), includeId, "wellBoreFeatureCategory.MetaInfo.ID");

    public static JsonObject CreateWellBoreBatchExportSchema() => WrapRequest(new JsonObject
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["Scope"] = new JsonObject { ["type"] = "string", ["enum"] = new JsonArray("All", "Selected") },
            ["WellBoreIDs"] = new JsonObject
            {
                ["type"] = new JsonArray("array", "null"), ["uniqueItems"] = true,
                ["items"] = new JsonObject { ["type"] = "string", ["format"] = "uuid" }
            }
        },
        ["required"] = new JsonArray("Scope"), ["additionalProperties"] = false
    });

    public static JsonObject CreateWellBoreBatchRestoreSchema() => WrapRequest(new JsonObject
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["ConflictPolicy"] = new JsonObject { ["type"] = "string", ["enum"] = new JsonArray("FailIfExists", "ReplaceExisting") },
            ["CatalogPolicy"] = new JsonObject { ["type"] = "string", ["enum"] = new JsonArray("MapExisting", "MapOrCreateMissing") },
            ["Document"] = CreateBatchDocumentSchema(1)
        },
        ["required"] = new JsonArray("ConflictPolicy", "CatalogPolicy", "Document"),
        ["additionalProperties"] = false
    });

    private static JsonObject CreateBatchDocumentSchema(int minimumWellBores) => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["FormatIdentifier"] = new JsonObject { ["type"] = "string", ["const"] = "OSDC.Drilling.WellBore.BatchExport" },
            ["SchemaVersion"] = new JsonObject { ["type"] = "integer", ["const"] = 1 },
            ["ExportedAtUtc"] = new JsonObject { ["type"] = "string", ["format"] = "date-time" },
            ["CatalogDependencies"] = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject
                {
                    ["Identities"] = new JsonObject { ["type"] = "array", ["items"] = CreateIdentityDefinitionSchema() },
                    ["FeatureCategories"] = new JsonObject { ["type"] = "array", ["items"] = CreateFeatureCategoryDefinitionSchema() }
                },
                ["required"] = new JsonArray("Identities", "FeatureCategories"), ["additionalProperties"] = false
            },
            ["WellBores"] = new JsonObject { ["type"] = "array", ["minItems"] = minimumWellBores, ["items"] = CreateWellBoreObjectSchema() }
        },
        ["required"] = new JsonArray("FormatIdentifier", "SchemaVersion", "ExportedAtUtc", "CatalogDependencies", "WellBores"),
        ["additionalProperties"] = false
    };

    private static JsonObject WrapRequest(JsonObject request) => new()
    {
        ["type"] = "object", ["properties"] = new JsonObject { ["request"] = request },
        ["required"] = new JsonArray("request"), ["additionalProperties"] = false
    };

    private static JsonObject CreateWellBoreObjectSchema()
    {
        return new JsonObject
        {
            ["type"] = "object",
            ["description"] = "Complete WellBore resource. MetaInfo.ID must be a non-empty UUID; the service does not generate an identifier.",
            ["properties"] = new JsonObject
            {
                ["MetaInfo"] = new JsonObject
                {
                    ["type"] = "object",
                    ["description"] = "Identity and optional HTTP location metadata for the wellbore.",
                    ["properties"] = new JsonObject
                    {
                        ["ID"] = new JsonObject
                        {
                            ["type"] = "string",
                            ["format"] = "uuid",
                            ["description"] = "Non-empty unique identifier of the wellbore."
                        },
                        ["HttpHostName"] = NullableString("Optional host name from which the wellbore can be retrieved."),
                        ["HttpHostBasePath"] = NullableString("Optional service base path from which the wellbore can be retrieved."),
                        ["HttpEndPoint"] = NullableString("Optional HTTP endpoint for this wellbore resource.")
                    },
                    ["required"] = new JsonArray { "ID" },
                    ["additionalProperties"] = false
                },
                ["Name"] = NullableString("Human-readable wellbore name."),
                ["Description"] = NullableString("Human-readable description of the wellbore."),
                ["CreationDate"] = NullableDateTime("UTC or offset timestamp at which the wellbore record was created."),
                ["LastModificationDate"] = NullableDateTime("UTC or offset timestamp of the most recent modification."),
                ["WellID"] = NullableUuid("Identifier of the well to which this wellbore belongs."),
                ["RigID"] = NullableUuid("Identifier of the rig used to work on this wellbore."),
                ["IsSidetrack"] = new JsonObject
                {
                    ["type"] = "boolean",
                    ["description"] = "True when this wellbore is a sidetrack from a parent wellbore.",
                    ["default"] = false
                },
                ["ParentWellBoreID"] = NullableUuid("For a sidetrack, the identifier of its parent wellbore; otherwise normally null."),
                ["TieInPointAlongHoleDepth"] = CreateTieInPointSchema(),
                ["SidetrackType"] = new JsonObject
                {
                    ["type"] = "string",
                    ["description"] = "Classification of the sidetrack. Use Undefined for a non-sidetrack or when the type is not known.",
                    ["enum"] = new JsonArray { "Undefined", "Technical", "Production", "Appraisal", "Lateral" },
                    ["default"] = "Undefined"
                },
                ["WellBoreIdentityAssignments"] = NullableArray(CreateIdentityAssignmentSchema()),
                ["WellBoreFeatureAssignments"] = NullableArray(CreateFeatureAssignmentSchema())
            },
            ["required"] = new JsonArray { "MetaInfo" },
            ["additionalProperties"] = false
        };
    }

    private static JsonObject CreateTieInPointSchema()
    {
        return new JsonObject
        {
            ["type"] = new JsonArray { "object", "null" },
            ["description"] = "For a sidetrack, the along-hole depth of the tie-in point in its parent wellbore, represented as a Gaussian drilling property. Values are always expressed in meters (SI) and referenced to the fixed WGS84 vertical datum; convert values from any other unit or vertical datum before calling the service.",
            ["properties"] = new JsonObject
            {
                ["GaussianValue"] = new JsonObject
                {
                    ["type"] = "object",
                    ["description"] = "Gaussian value and uncertainty for the WGS84-referenced tie-in depth in meters (SI).",
                    ["properties"] = new JsonObject
                    {
                        ["MinValue"] = new JsonObject
                        {
                            ["type"] = "number",
                            ["description"] = "Minimum tie-in depth in meters (SI), referenced to the fixed WGS84 vertical datum."
                        },
                        ["MaxValue"] = new JsonObject
                        {
                            ["type"] = "number",
                            ["description"] = "Maximum tie-in depth in meters (SI), referenced to the fixed WGS84 vertical datum."
                        },
                        ["Mean"] = NullableNumber("Mean tie-in depth in meters (SI), referenced to the fixed WGS84 vertical datum."),
                        ["StandardDeviation"] = NullableNumber("Standard deviation expressing uncertainty in the tie-in depth, in meters (SI).")
                    },
                    ["additionalProperties"] = false
                }
            },
            ["required"] = new JsonArray { "GaussianValue" },
            ["additionalProperties"] = false
        };
    }

    private static JsonObject WrapCatalogBody(string key, JsonObject body, bool includeId, string idPath)
    {
        JsonObject properties = new() { [key] = body };
        JsonArray required = new(key);
        if (includeId)
        {
            properties["id"] = new JsonObject
            {
                ["type"] = "string", ["format"] = "uuid",
                ["description"] = $"Identifier of the stored definition to update. It must equal {idPath}."
            };
            properties["expectedModifiedUtc"] = new JsonObject
            {
                ["type"] = "string", ["format"] = "date-time",
                ["description"] = "Optimistic-concurrency token matching the latest LastModificationDate."
            };
            required.Add("id");
            required.Add("expectedModifiedUtc");
        }
        return new JsonObject
        {
            ["type"] = "object", ["properties"] = properties, ["required"] = required,
            ["additionalProperties"] = false
        };
    }

    private static JsonObject CreateIdentityDefinitionSchema() => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["MetaInfo"] = CreateMetaInfoSchema("identity definition"),
            ["Name"] = new JsonObject { ["type"] = "string", ["minLength"] = 1 },
            ["CreationDate"] = NullableDateTime("Server-owned creation timestamp."),
            ["LastModificationDate"] = NullableDateTime("Server-owned optimistic-concurrency timestamp.")
        },
        ["required"] = new JsonArray("MetaInfo", "Name"),
        ["additionalProperties"] = false
    };

    private static JsonObject CreateFeatureCategoryDefinitionSchema() => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["MetaInfo"] = CreateMetaInfoSchema("feature category definition"),
            ["Name"] = new JsonObject { ["type"] = "string", ["minLength"] = 1 },
            ["IsExclusive"] = new JsonObject { ["type"] = "boolean" },
            ["HasValidityPeriod"] = new JsonObject { ["type"] = "boolean" },
            ["Options"] = NullableArray(new JsonObject
            {
                ["type"] = "object",
                ["properties"] = new JsonObject
                {
                    ["ID"] = new JsonObject { ["type"] = "string", ["format"] = "uuid" },
                    ["Name"] = new JsonObject { ["type"] = "string", ["minLength"] = 1 }
                },
                ["required"] = new JsonArray("ID", "Name"), ["additionalProperties"] = false
            }),
            ["CreationDate"] = NullableDateTime("Server-owned creation timestamp."),
            ["LastModificationDate"] = NullableDateTime("Server-owned optimistic-concurrency timestamp.")
        },
        ["required"] = new JsonArray("MetaInfo", "Name", "IsExclusive", "HasValidityPeriod", "Options"),
        ["additionalProperties"] = false
    };

    private static JsonObject CreateIdentityAssignmentSchema() => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["ID"] = new JsonObject { ["type"] = "string", ["format"] = "uuid" },
            ["IdentityID"] = new JsonObject { ["type"] = "string", ["format"] = "uuid" },
            ["Value"] = new JsonObject { ["type"] = "string", ["minLength"] = 1 }
        },
        ["required"] = new JsonArray("ID", "IdentityID", "Value"), ["additionalProperties"] = false
    };

    private static JsonObject CreateFeatureAssignmentSchema() => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject
        {
            ["ID"] = new JsonObject { ["type"] = "string", ["format"] = "uuid" },
            ["FeatureCategoryID"] = new JsonObject { ["type"] = "string", ["format"] = "uuid" },
            ["FeatureOptionID"] = new JsonObject { ["type"] = "string", ["format"] = "uuid" },
            ["FromDate"] = NullableDateTime("Validity start."),
            ["ToDate"] = NullableDateTime("Validity end.")
        },
        ["required"] = new JsonArray("ID", "FeatureCategoryID", "FeatureOptionID"), ["additionalProperties"] = false
    };

    private static JsonObject CreateMetaInfoSchema(string resource) => new()
    {
        ["type"] = "object",
        ["description"] = $"Identity and optional HTTP location metadata for the {resource}.",
        ["properties"] = new JsonObject
        {
            ["ID"] = new JsonObject { ["type"] = "string", ["format"] = "uuid" },
            ["HttpHostName"] = NullableString("Optional host name."),
            ["HttpHostBasePath"] = NullableString("Optional service base path."),
            ["HttpEndPoint"] = NullableString("Optional resource endpoint.")
        },
        ["required"] = new JsonArray("ID"), ["additionalProperties"] = false
    };

    private static JsonObject NullableArray(JsonObject item) => new()
    {
        ["type"] = new JsonArray { "array", "null" }, ["items"] = item
    };

    private static JsonObject NullableString(string description) => new()
    {
        ["type"] = new JsonArray { "string", "null" },
        ["description"] = description
    };

    private static JsonObject NullableDateTime(string description) => new()
    {
        ["type"] = new JsonArray { "string", "null" },
        ["format"] = "date-time",
        ["description"] = description
    };

    private static JsonObject NullableUuid(string description) => new()
    {
        ["type"] = new JsonArray { "string", "null" },
        ["format"] = "uuid",
        ["description"] = description
    };

    private static JsonObject NullableNumber(string description) => new()
    {
        ["type"] = new JsonArray { "number", "null" },
        ["description"] = description
    };

    public static bool TryParseGuid(JsonObject? arguments, string key, out Guid value, out JsonNode? error)
    {
        value = Guid.Empty;
        error = null;

        var node = arguments?[key];
        if (node is null)
        {
            error = McpToolResponses.CreateValidationError($"Argument '{key}' is required.");
            return false;
        }

        if (!Guid.TryParse(node.ToString(), out value))
        {
            error = McpToolResponses.CreateValidationError($"Argument '{key}' must be a valid UUID.");
            return false;
        }

        return true;
    }

    public static bool TryParseDouble(JsonObject? arguments, string key, out double value, out JsonNode? error)
    {
        value = 0d;
        error = null;

        var node = arguments?[key];
        if (node is null)
        {
            error = McpToolResponses.CreateValidationError($"Argument '{key}' is required.");
            return false;
        }

        try
        {
            value = node.GetValue<double>();
        }
        catch (Exception ex) when (ex is InvalidOperationException or FormatException)
        {
            error = McpToolResponses.CreateValidationError($"Argument '{key}' must be a number.");
            return false;
        }

        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            error = McpToolResponses.CreateValidationError($"Argument '{key}' must be a finite number.");
            return false;
        }

        return true;
    }
}
