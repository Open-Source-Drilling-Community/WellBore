using System;
using System.Text.Json.Nodes;
using OSDC.DotnetLibraries.Drilling.SemanticCatalogue;

namespace OSDC.Drilling.WellBore.Service.Mcp;

internal static class McpOperationSemantics
{
    public static JsonNode Apply(string name, JsonNode schema)
    {
        string concept = name.Contains("feature_assignment", StringComparison.Ordinal) ? Concepts.FeatureAssignment
            : name.Contains("identity_assignment", StringComparison.Ordinal) ? Concepts.IdentityAssignment
            : name.Contains("feature_category", StringComparison.Ordinal) ? Concepts.FeatureCategory
            : name.Contains("identity", StringComparison.Ordinal) && !name.Contains("assignment", StringComparison.Ordinal) ? Concepts.IdentityDefinition : Concepts.WellBore;
        schema[SemanticMetadata.ExtensionName] = SemanticMetadata.Create(concept, Classify(name), assertionSource: "provider-mcp-operation");
        return schema;
    }
    private static string Classify(string name) =>
        name.Contains("validate", StringComparison.Ordinal) || name.Contains("audit", StringComparison.Ordinal) ? Concepts.StatelessEvaluation
        : name.Contains("batch_export", StringComparison.Ordinal) || name.Contains("get_all", StringComparison.Ordinal) || name.EndsWith("_search", StringComparison.Ordinal) ? Concepts.ResourceCollectionRetrieval
        : name.EndsWith("_get_by_id", StringComparison.Ordinal) ? Concepts.ResourceRetrieval
        : name.Contains("batch_restore", StringComparison.Ordinal) ? Concepts.ResourceOperation
        : name.EndsWith("_create", StringComparison.Ordinal) || name.EndsWith("_add", StringComparison.Ordinal) ? Concepts.ResourceCreation
        : name.EndsWith("_update_by_id", StringComparison.Ordinal) ? Concepts.ResourceReplacement
        : name.Contains("_update", StringComparison.Ordinal) || name.Contains("_patch", StringComparison.Ordinal) ? Concepts.ResourcePartialUpdate
        : name.Contains("_delete", StringComparison.Ordinal) ? Concepts.ResourceDeletion : Concepts.ResourceOperation;
}
