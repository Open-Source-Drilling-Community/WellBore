using OSDC.DotnetLibraries.Drilling.SemanticCatalogue;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace OSDC.Drilling.WellBore.Service.Mcp.Tools;

/// <summary>Registers bounded discovery from the same controller actions used by REST.</summary>
public static class DiscoveryMcpTools
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = false,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) } };

    public static IServiceCollection AddDiscoveryMcpTools(this IServiceCollection services)
    {
        foreach (Type controller in typeof(DiscoveryMcpTools).Assembly.GetTypes()
            .Where(type => typeof(ControllerBase).IsAssignableFrom(type) && !type.IsAbstract))
        foreach (MethodInfo method in controller.GetMethods().Where(method => method.Name.StartsWith("Discover", StringComparison.Ordinal) && method.GetCustomAttribute<HttpGetAttribute>() != null))
        {
            string entity = method.Name["Discover".Length..];
            string name = System.Text.RegularExpressions.Regex.Replace(entity, "(?<!^)([A-Z])", "_$1").ToLowerInvariant() + "_discover";
            ParameterInfo[] parameters = method.GetParameters();
            var properties = new JsonObject();
            foreach (ParameterInfo parameter in parameters)
            {
                JsonObject schema = Schema(parameter.ParameterType);
                if (parameter.Name == "offset") { schema["minimum"] = 0; schema["default"] = 0; }
                if (parameter.Name == "limit") { schema["minimum"] = 1; schema["maximum"] = 200; schema["default"] = 50; }
                if (parameter.Name == "name") schema["maxLength"] = 200;
                properties[parameter.Name!] = schema;
            }
            var input = new JsonObject { ["type"] = "object", ["properties"] = properties, ["required"] = new JsonArray(parameters.Where(parameter => !parameter.HasDefaultValue).Select(parameter => (JsonNode)JsonValue.Create(parameter.Name)!).ToArray()), ["additionalProperties"] = false };
            Type page = method.ReturnType.GenericTypeArguments.Single();
            Type itemType = page.GetProperty("Items")!.PropertyType.GenericTypeArguments.Single();
            if (Metadata(itemType) is JsonObject operationMetadata)
            {
                operationMetadata["role"] = Concepts.ResourceCollectionRetrieval;
                input[SemanticMetadata.ExtensionName] = operationMetadata;
            }
            foreach (ParameterInfo parameter in parameters)
            {
                PropertyInfo? property = itemType.GetProperties().FirstOrDefault(property => string.Equals(property.Name, parameter.Name, StringComparison.OrdinalIgnoreCase));
                if (property != null && Metadata(property) is JsonObject filterMetadata)
                    properties[parameter.Name!]![SemanticMetadata.ExtensionName] = filterMetadata;
            }

            var output = new JsonObject { ["type"] = "object", ["properties"] = new JsonObject {
                ["status"] = new JsonObject { ["type"] = "integer" }, ["data"] = Schema(page) },
                ["required"] = new JsonArray("status", "data"), ["additionalProperties"] = false };
            services.AddLegacyMcpTool(name, $"Discover {entity} using bounded light pages. offset defaults to 0; limit defaults to 50 and cannot exceed 200. NextOffset is null at the end. UUID ordering is deterministic; concurrent changes can affect later pages. Retrieve a selected complete record by ID.", input, output,
                new McpToolBehavior($"Discover {entity}", true, false, true, false),
                (sp, args, ct) => Invoke(sp, controller, method, args, ct));
        }
        return services;
    }

    private static Task<JsonNode?> Invoke(IServiceProvider services, Type controller, MethodInfo method,
        JsonObject? arguments, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ParameterInfo[] parameters = method.GetParameters();
        if (arguments?.Any(pair => parameters.All(parameter => parameter.Name != pair.Key)) == true)
            return Task.FromResult<JsonNode?>(Error(400, "Unexpected discovery argument."));
        object?[] values = new object?[parameters.Length];
        try
        {
            for (int i = 0; i < parameters.Length; i++)
            {
                ParameterInfo parameter = parameters[i];
                bool supplied = arguments?.ContainsKey(parameter.Name!) == true;
                JsonNode? node = arguments?[parameter.Name!];
                if (!supplied && !parameter.HasDefaultValue) return Task.FromResult<JsonNode?>(Error(400, "A required discovery argument is missing."));
                if (supplied && node == null && Nullable.GetUnderlyingType(parameter.ParameterType) == null)
                    return Task.FromResult<JsonNode?>(Error(400, "Discovery arguments cannot be null."));
                values[i] = node == null ? parameter.DefaultValue : node.Deserialize(parameter.ParameterType, JsonOptions);
                Type type = Nullable.GetUnderlyingType(parameter.ParameterType) ?? parameter.ParameterType;
                if (values[i] != null && type.IsEnum && !Enum.IsDefined(type, values[i]!))
                    return Task.FromResult<JsonNode?>(Error(400, "Unknown discovery enum value."));
            }
        }
        catch (JsonException) { return Task.FromResult<JsonNode?>(Error(400, "Invalid discovery argument type.")); }
        int offset = (int)values[Array.FindIndex(parameters, parameter => parameter.Name == "offset")]!;
        int limit = (int)values[Array.FindIndex(parameters, parameter => parameter.Name == "limit")]!;
        if (offset < 0 || limit is < 1 or > 200 ||
            values.OfType<Guid>().Any(value => value == Guid.Empty) ||
            (arguments?["name"] is JsonValue name && name.TryGetValue<string>(out string? text) && text?.Length > 200))
            return Task.FromResult<JsonNode?>(Error(400, "Invalid discovery pagination or filter."));
        using IServiceScope scope = services.CreateScope();
        object instance = ActivatorUtilities.CreateInstance(scope.ServiceProvider, controller);
        try
        {
            object response = method.Invoke(instance, values)!;
            ObjectResult? result = response.GetType().GetProperty("Result")?.GetValue(response) as ObjectResult;
            object? data = result?.Value ?? response.GetType().GetProperty("Value")?.GetValue(response);
            int status = result?.StatusCode ?? 200;
            return Task.FromResult<JsonNode?>(new JsonObject { ["status"] = status, ["data"] = JsonSerializer.SerializeToNode(data, JsonOptions) });
        }
        catch (TargetInvocationException) { return Task.FromResult<JsonNode?>(Error(500, "Discovery could not be completed.")); }
    }

    private static JsonObject Error(int status, string message) => new() { ["status"] = status,
        ["data"] = new JsonObject { ["error"] = status == 400 ? "invalid_discovery_query" : "discovery_failed", ["message"] = message } };

    private static JsonObject Schema(Type declared)
    {
        Type type = Nullable.GetUnderlyingType(declared) ?? declared;
        JsonObject schema;
        if (type == typeof(Guid)) schema = new() { ["type"] = "string", ["format"] = "uuid" };
        else if (type == typeof(DateTimeOffset) || type == typeof(DateTime)) schema = new() { ["type"] = "string", ["format"] = "date-time" };
        else if (type == typeof(string)) schema = new() { ["type"] = "string" };
        else if (type == typeof(bool)) schema = new() { ["type"] = "boolean" };
        else if (type.IsEnum) schema = new() { ["type"] = "string", ["enum"] = new JsonArray(Enum.GetNames(type).Select(value => (JsonNode)JsonValue.Create(value)!).ToArray()) };
        else if (type == typeof(int) || type == typeof(long)) schema = new() { ["type"] = "integer" };
        else if (type.IsPrimitive || type == typeof(decimal)) schema = new() { ["type"] = "number" };
        else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            schema = new() { ["type"] = "array", ["items"] = Schema(type.GenericTypeArguments[0]) };
        else
        {
            var properties = new JsonObject();
            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(property => property.GetIndexParameters().Length == 0 && property.GetCustomAttribute<JsonIgnoreAttribute>() == null))
            {
                JsonObject child = Schema(property.PropertyType);
                if (!property.PropertyType.IsValueType && property.Name != "Items") child = new JsonObject { ["anyOf"] = new JsonArray(child, new JsonObject { ["type"] = "null" }) };
                if (Metadata(property) is JsonObject propertyMetadata) child[SemanticMetadata.ExtensionName] = propertyMetadata;
                if (property.Name == "Items") child["maxItems"] = 200;
                properties[property.Name] = child;
            }
            schema = new() { ["type"] = "object", ["properties"] = properties, ["additionalProperties"] = false };
            if (Metadata(type) is JsonObject typeMetadata) schema[SemanticMetadata.ExtensionName] = typeMetadata;
            if (type.Name.EndsWith("DiscoveryPage", StringComparison.Ordinal))
                schema["required"] = new JsonArray("Items", "Offset", "Limit", "TotalCount", "NextOffset");
        }
        if (Nullable.GetUnderlyingType(declared) != null)
            return new JsonObject { ["anyOf"] = new JsonArray(schema, new JsonObject { ["type"] = "null" }) };
        return schema;
    }

    private static JsonObject? Metadata(MemberInfo member)
    {
        Type declared = member is Type type ? type : member.DeclaringType!;
        Type owner = declared;
        if (declared.Name.EndsWith("Light", StringComparison.Ordinal))
        {
            string fullName = declared.Name[..^5];
            owner = declared.Assembly.GetTypes().FirstOrDefault(type => type.Name == fullName) ?? declared;
            if (owner == declared)
            {
                foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies().Where(assembly => assembly.GetName().Name?.StartsWith("OSDC.", StringComparison.Ordinal) == true))
                {
                    var candidate = assembly.GetTypes().FirstOrDefault(type => type.Name == fullName && type.Namespace?.Contains("ModelShared", StringComparison.Ordinal) != true);
                    if (candidate != null) { owner = candidate; break; }
                }
            }
        }
        MemberInfo binding = member is PropertyInfo property ? owner.GetProperty(property.Name) ?? member : owner;
        Type? provider = declared.Assembly.GetTypes().FirstOrDefault(type => type.Name.EndsWith("ProviderSemantics", StringComparison.Ordinal));
        MethodInfo? method = provider?.GetMethod(member is PropertyInfo ? "ForProperty" : "ForType", BindingFlags.Public | BindingFlags.Static, null, new[] { member is PropertyInfo ? typeof(PropertyInfo) : typeof(Type) }, null);
        JsonObject? metadata = method?.Invoke(null, [binding]) as JsonObject ?? SemanticMetadata.For(binding);
        return metadata?.DeepClone() as JsonObject;
    }
}
