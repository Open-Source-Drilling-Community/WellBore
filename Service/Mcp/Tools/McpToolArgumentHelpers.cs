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
                }
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
