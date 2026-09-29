using System.Collections.Generic;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using OSDC.DotnetLibraries.Drilling.SemanticCatalogue;
using OSDC.Drilling.WellBore.Model;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace OSDC.Drilling.WellBore.Service;

/// <summary>Publishes model and contextual third-party bindings without redefining shared domain types.</summary>
public sealed class SemanticSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (ProviderSemantics.ForType(context.Type) is { } metadata)
            schema.Extensions[SemanticMetadata.ExtensionName] = OpenApiAnyFactory.CreateFromJson(metadata.ToJsonString());
        foreach (var property in context.Type.GetProperties())
        {
            if (!schema.Properties.TryGetValue(property.Name, out var target)) continue;
            var binding = ProviderSemantics.ForProperty(property);
            var nested = ProviderSemantics.NestedBindings(property);
            var description = ProviderSemantics.Description(property, binding);
            if (binding == null && nested == null && description == null) continue;
            // OpenAPI 3.0 ignores siblings of $ref. Keep the original reference within allOf.
            if (target.Reference != null)
            {
                target = new OpenApiSchema { AllOf = new List<OpenApiSchema> { target } };
                schema.Properties[property.Name] = target;
            }
            if (description != null) target.Description = description;
            if (binding != null) target.Extensions[SemanticMetadata.ExtensionName] = OpenApiAnyFactory.CreateFromJson(binding.ToJsonString());
            if (nested != null) target.Extensions[ProviderSemantics.NestedBindingsExtension] = OpenApiAnyFactory.CreateFromJson(nested.ToJsonString());
        }
    }
}
