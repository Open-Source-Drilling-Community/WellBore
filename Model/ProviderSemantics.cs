using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text.Json.Nodes;
using OSDC.DotnetLibraries.Drilling.SemanticCatalogue;
using OSDC.DotnetLibraries.General.DataManagement;
using OSDC.DotnetLibraries.General.Math;
using Catalogue = OSDC.DotnetLibraries.Drilling.SemanticCatalogue.SemanticCatalogue;

namespace OSDC.Drilling.WellBore.Model;

/// <summary>Property-context binding for a shared Gaussian representation; does not change serialization.</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class GaussianQuantityAttribute(string measurand, string uncertainty) : Attribute
{
    public string Measurand { get; } = measurand;
    public string Uncertainty { get; } = uncertainty;
    public string? Reference { get; set; }
}

/// <summary>
/// Provider-owned bindings for third-party properties and inherited classification members.
/// REST and MCP use this same registry. Domain types remain owned by their NuGet packages.
/// </summary>
public static class ProviderSemantics
{
    public const string NestedBindingsExtension = "x-osdc-semantic-bindings";

    public static JsonObject Metadata(string concept, string? role = null, string? reference = null)
    {
        return SemanticMetadata.Create(concept, role, reference,
            Catalogue.OsdcCanonicalDrilling, assertionSource: "provider-binding-registry");
    }

    public static JsonObject? ForType(Type type)
    {
        if (SemanticMetadata.For(type) is JsonObject direct) return direct;
        if (type == typeof(MetaInfo)) return Metadata(Concepts.ResourceMetadata);
        if (type == typeof(Point3DGlobalCoordinates)) return Metadata(Concepts.Position, reference: Concepts.Wgs84);
        return null;
    }

    public static JsonObject? ForProperty(PropertyInfo property)
    {
        if (SemanticMetadata.For(property) is JsonObject direct) return direct;
        Type type = property.DeclaringType!;
        if (typeof(Point3DGlobalCoordinates).IsAssignableFrom(type))
            return property.Name switch
            {
                "X" or "RiemannianNorth" => Metadata(Concepts.RiemannianNorth, reference: Concepts.Wgs84RiemannianCoordinates),
                "Y" or "RiemannianEast" => Metadata(Concepts.RiemannianEast, reference: Concepts.Wgs84RiemannianCoordinates),
                "Z" or "TVD" => Metadata(Concepts.EllipsoidalDepth, reference: Concepts.Wgs84),
                "Latitude" => Metadata(Concepts.Latitude, reference: Concepts.Wgs84),
                "Longitude" => Metadata(Concepts.Longitude, reference: Concepts.Wgs84),
                _ => null
            };
        bool classification = typeof(IIdentity).IsAssignableFrom(type) || typeof(IIdentityAssignment).IsAssignableFrom(type) ||
            typeof(IFeatureCategory).IsAssignableFrom(type) || typeof(IFeatureOption).IsAssignableFrom(type) || typeof(IFeatureAssignment).IsAssignableFrom(type) ||
            typeof(IMembershipCategory).IsAssignableFrom(type) || typeof(IMembershipOption).IsAssignableFrom(type) || typeof(IMembershipAssignment).IsAssignableFrom(type);
        if (type != typeof(MetaInfo) && !classification) return null;
        return property.Name switch
        {
            "MetaInfo" => Metadata(Concepts.ResourceMetadata),
            "ID" or "IdentityID" or "FeatureCategoryID" or "FeatureOptionID" or "MembershipCategoryID" or "MembershipOptionID" => Metadata(Concepts.ResourceIdentifier),
            "Name" => Metadata(Concepts.ResourceName),
            "Value" => Metadata(Concepts.IdentityValue),
            "CreationDate" => Metadata(Concepts.Instant, Concepts.CreationTime, Concepts.Utc),
            "LastModificationDate" => Metadata(Concepts.Instant, Concepts.LastModificationTime, Concepts.Utc),
            "FromDate" => Metadata(Concepts.Instant, Concepts.ValidityStart, Concepts.Utc),
            "ToDate" => Metadata(Concepts.Instant, Concepts.ValidityEnd, Concepts.Utc),
            "IsExclusive" => Metadata(Concepts.CategoryExclusivity),
            "HasValidityPeriod" => Metadata(Concepts.CategoryValidityPeriodEnabled),
            _ => null
        };
    }

    public static string? Description(PropertyInfo property, JsonObject? metadata)
    {
        string? explicitDescription = property.GetCustomAttribute<DescriptionAttribute>()?.Description;
        if (metadata == null) return explicitDescription;
        var definition = Catalogue.Default.Get(metadata["concept"]!.GetValue<string>());
        string? referenceDescription = property.Name switch
        {
            "WellID" => "UUID of the referenced Well resource owned by the Well service.",
            "WellBoreID" or "ParentWellBoreID" => "UUID of the referenced WellBore resource; ParentWellBoreID identifies the parent path for tie-in along-hole depth.",
            "ClusterID" => "UUID of the associated Cluster service resource.",
            "SlotID" => "UUID of a Slot owned by the associated Cluster.",
            "RigJobID" => "Stable UUID of this job within the containing WellBore history.",
            "FieldID" => "UUID of the referenced resource owned by the Field service.",
            "RigID" => "UUID of the associated resource owned by the Rig service.",
            "ProjectionDefinitionID" => "UUID of the definition owned by EarthCartographicProjection, not an EPSG code.",
            "IdentityID" => "UUID of the selected identity definition in the owning service catalogue.",
            "FeatureCategoryID" or "MembershipCategoryID" => "UUID of the selected category in the owning service catalogue.",
            "FeatureOptionID" or "MembershipOptionID" => "UUID of an option belonging to the selected category.",
            "DelineationLineTypeID" => "UUID of a line-type definition owned by the Field service.",
            _ => null
        };
        string text = explicitDescription ?? referenceDescription ?? definition.Definition;
        if (property.PropertyType == typeof(Point3DGlobalCoordinates))
            text += " Geographic angles use SI radians; linear coordinates and ellipsoidal depth use SI metres. X/Y are Riemannian north/east arc coordinates, not projected easting/northing.";
        if (property.GetCustomAttribute<GaussianQuantityAttribute>() is { } gaussian)
        {
            string unit = Catalogue.Default.SiUnit(gaussian.Measurand) == "rad" ? "radians (rad)" : "metres (m)";
            text = Catalogue.Default.Get(gaussian.Measurand).Definition +
                $" GaussianValue.Mean is the expected value in SI {unit}, " +
                (gaussian.Measurand == Concepts.TieInAlongHoleDepth ? "Along the parent wellbore identified by ParentWellBoreID, using the OSDC WGS84 path-intersection along-hole convention; this is not a vertical depth. " : "Relative to the WGS84 ellipsoid, positive downward. ") +
                $"StandardDeviation is a non-negative standard uncertainty in SI {unit}; it has no coordinate origin. " +
                "MinValue and MaxValue are provider domain-limit metadata in the mean's unit and reference, not confidence limits or instructions to truncate the Gaussian distribution.";
        }
        if (metadata["physicalQuantity"]?["name"] is JsonNode quantity)
            text += $" Physical quantity: {quantity.GetValue<string>()}; SI unit: {(metadata["siUnit"]!.GetValue<string>() switch { "rad" => "radians (rad)", "m" => "metres (m)", var unit => unit })}.";
        if (metadata["reference"] is JsonNode reference)
            text += $" Reference: {Catalogue.Default.Get(reference.GetValue<string>()).Label}.";
        return text;
    }

    /// <summary>Relative JSON Pointer bindings. Quantities belong to scalars, never to the Gaussian wrapper globally.</summary>
    public static JsonObject? NestedBindings(PropertyInfo property)
    {
        var gaussian = property.GetCustomAttribute<GaussianQuantityAttribute>();
        if (gaussian == null) return null;
        return new JsonObject
        {
            ["/GaussianValue/Mean"] = Metadata(gaussian.Measurand, Concepts.ExpectedValue, gaussian.Reference),
            ["/GaussianValue/StandardDeviation"] = Metadata(gaussian.Uncertainty),
            ["/GaussianValue/MinValue"] = Metadata(gaussian.Measurand, Concepts.DistributionLowerBound, gaussian.Reference),
            ["/GaussianValue/MaxValue"] = Metadata(gaussian.Measurand, Concepts.DistributionUpperBound, gaussian.Reference)
        };
    }

    /// <summary>Annotate an existing JSON schema without changing validation keywords or accepted payload shape.</summary>
    public static JsonObject Annotate(JsonObject schema, Type type)
    {
        Walk(schema, type, schema, new HashSet<(JsonObject, Type)>());
        return schema;
    }

    private static void Walk(JsonObject schema, Type type, JsonObject root, HashSet<(JsonObject, Type)> seen)
    {
        type = Nullable.GetUnderlyingType(type) ?? type;
        if (!seen.Add((schema, type))) return;
        if (schema["$ref"]?.GetValue<string>() is string reference && reference.StartsWith("#/"))
        {
            JsonNode? resolved = root;
            foreach (string part in reference[2..].Split('/')) resolved = resolved?[part.Replace("~1", "/").Replace("~0", "~")];
            if (resolved is JsonObject target) Walk(target, type, root, seen);
            return;
        }
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
        {
            if (schema["additionalProperties"] is JsonObject values) Walk(values, type.GetGenericArguments()[1], root, seen);
            return;
        }
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
        {
            if (schema["items"] is JsonObject items) Walk(items, type.GetGenericArguments()[0], root, seen);
            return;
        }
        if (schema[SemanticMetadata.ExtensionName] == null && ForType(type) is JsonObject typeMetadata)
            schema[SemanticMetadata.ExtensionName] = typeMetadata;
        foreach (string keyword in new[] { "oneOf", "anyOf", "allOf" })
            if (schema[keyword] is JsonArray variants)
                foreach (var variant in variants.OfType<JsonObject>()) Walk(variant, type, root, seen);
        if (schema["properties"] is not JsonObject properties) return;
        foreach (var property in type.GetProperties())
        {
            if (properties[property.Name] is not JsonObject target) continue;
            var metadata = ForProperty(property);
            if (metadata != null) target[SemanticMetadata.ExtensionName] = metadata;
            if (Description(property, metadata) is string description)
            {
                string? prior = target["description"]?.GetValue<string>();
                target["description"] = property.GetCustomAttribute<GaussianQuantityAttribute>() != null || string.IsNullOrWhiteSpace(prior)
                    ? description : prior + " " + description;
            }
            if (NestedBindings(property) is JsonObject nested)
            {
                target[NestedBindingsExtension] = nested;
                foreach (var binding in nested)
                {
                    JsonNode? scalar = target;
                    foreach (string part in binding.Key[1..].Split('/')) scalar = scalar?["properties"]?[part];
                    if (scalar is JsonObject scalarSchema)
                    {
                        scalarSchema[SemanticMetadata.ExtensionName] = binding.Value!.DeepClone();
                        var value = binding.Value!;
                        scalarSchema["description"] = Catalogue.Default.Get(value["concept"]!.GetValue<string>()).Definition +
                            $" Physical quantity: {value["physicalQuantity"]!["name"]}; SI unit: {value["siUnit"]}.";
                    }
                }
            }
            Walk(target, property.PropertyType, root, seen);
        }
    }
}
