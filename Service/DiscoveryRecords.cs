using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace OSDC.Drilling.WellBore.Service;

internal static class DiscoveryRecords
{
    /// <summary>Reads at most one page of persisted documents into light DTOs. Count and items share one read transaction.</summary>
    public static List<T> ReadPage<T>(SqliteConnection? connection, string table, string document, int offset,
        int limit, string? name, IDictionary<string, object?> filters, out int total)
    {
        if (connection == null) throw new InvalidOperationException("Discovery storage is unavailable.");
        using (connection)
        using (var transaction = connection.BeginTransaction())
        {
            var columns = new HashSet<string>(StringComparer.Ordinal);
            using (var schema = connection.CreateCommand())
            {
                schema.Transaction = transaction; schema.CommandText = $"PRAGMA table_info({table})";
                using var schemaReader = schema.ExecuteReader();
                while (schemaReader.Read()) columns.Add(schemaReader.GetString(1));
            }
            string identifier = columns.Contains("ID") ? "ID" : columns.Contains("MetaInfo")
                ? "json_extract(MetaInfo, '$.ID')" : $"json_extract({document}, '$.ID')";
            connection.CreateFunction<string?, string?, bool>("discovery_name_contains", (value, query) =>
                value?.Contains(query ?? "", StringComparison.OrdinalIgnoreCase) == true);
            string expression = $"json_extract({document}, '$.{{0}}')";
            List<string> clauses = new();
            var parameters = new Dictionary<string, object>();
            if (!string.IsNullOrWhiteSpace(name))
            {
                string nameField = columns.Contains("Name") ? "Name" : table == "ErrorSourceTable"
                    ? $"COALESCE(json_extract({document}, '$.Name'), json_extract({document}, '$.ErrorCode'))" : string.Format(expression, "Name");
                clauses.Add($"discovery_name_contains({nameField}, $name)");
                parameters["$name"] = name;
            }
            int index = 0;
            foreach (var filter in filters.Where(pair => pair.Value != null))
            {
                string parameter = "$filter" + index++;
                string field = columns.Contains(filter.Key) ? filter.Key : string.Format(expression, filter.Key);
                if (filter.Value is bool) field = $"COALESCE({field}, 0)";
                // Include every historical rig job, matching the existing WellBore search semantics.
                if (table == "WellBoreTable" && filter.Key == "RigID")
                    clauses.Add($"({field} = {parameter} COLLATE NOCASE OR EXISTS (SELECT 1 FROM json_each({document}, '$.RigJobs') job WHERE json_extract(job.value, '$.RigID') = {parameter} COLLATE NOCASE))");
                else if (filter.Value is Enum enumerationFilter)
                {
                    clauses.Add($"({field} = {parameter} COLLATE NOCASE OR {field} = {parameter}_number)");
                    parameters[parameter + "_number"] = Convert.ToInt32(enumerationFilter);
                }
                else clauses.Add($"{field} = {parameter} COLLATE NOCASE");
                parameters[parameter] = filter.Value is Guid uuid ? uuid.ToString() :
                    filter.Value is Enum enumeration ? enumeration.ToString() : filter.Value!;
            }
            string where = clauses.Count == 0 ? "" : " WHERE " + string.Join(" AND ", clauses);
            using var count = connection.CreateCommand(); count.Transaction = transaction;
            count.CommandText = $"SELECT COUNT(*) FROM {table}{where}";
            foreach (var parameter in parameters) count.Parameters.AddWithValue(parameter.Key, parameter.Value);
            total = checked((int)(long)count.ExecuteScalar()!);
            using var query = connection.CreateCommand(); query.Transaction = transaction;
            query.CommandText = $"SELECT {Projection<T>(document)} FROM {table}{where} ORDER BY {identifier} COLLATE BINARY LIMIT $limit OFFSET $offset";
            foreach (var parameter in parameters) query.Parameters.AddWithValue(parameter.Key, parameter.Value);
            query.Parameters.AddWithValue("$limit", limit); query.Parameters.AddWithValue("$offset", offset);
            var items = new List<T>();
            using var reader = query.ExecuteReader();
            while (reader.Read())
            {
                T? item = JsonSerializer.Deserialize<T>(reader.GetString(0), JsonSettings.Options);
                if (item == null) throw new JsonException("A discovery document is null.");
                // Match the established stable revision fallback for legacy documents.
                var revision = typeof(T).GetProperty("LastModificationDate");
                if (revision?.CanWrite == true && revision.GetValue(item) == null)
                {
                    var creationRevision = typeof(T).GetProperty("CreationDate")?.GetValue(item);
                    revision.SetValue(item, creationRevision ?? DateTimeOffset.UnixEpoch);
                }
                items.Add(item);
            }
            return items;
        }
    }

    private static string Projection<T>(string document)
    {
        object? defaults = Activator.CreateInstance<T>();
        var fields = new List<string>();
        foreach (var property in typeof(T).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Where(property => property.CanWrite && property.GetIndexParameters().Length == 0 &&
                property.GetCustomAttributes(typeof(System.Text.Json.Serialization.JsonIgnoreAttribute), true).Length == 0))
        {
            string path = "$." + property.Name;
            string value = $"json_extract({document}, '{path}')";
            Type actualType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            object? fallback = property.GetValue(defaults);
            if (actualType == typeof(bool))
            {
                string missing = fallback is true ? "true" : fallback is false ? "false" : "null";
                value = $"json(CASE WHEN json_type({document}, '{path}') IS NULL THEN '{missing}' WHEN {value} = 1 THEN 'true' WHEN {value} = 0 THEN 'false' ELSE 'null' END)";
            }
            else if (property.PropertyType.IsValueType && Nullable.GetUnderlyingType(property.PropertyType) == null)
            {
                string literal = fallback is Guid or Enum or DateTimeOffset or DateTime
                    ? "'" + (fallback is DateTimeOffset timestamp ? timestamp.ToString("O") : fallback?.ToString())?.Replace("'", "''") + "'"
                    : Convert.ToString(fallback, System.Globalization.CultureInfo.InvariantCulture) ?? "NULL";
                value = $"COALESCE({value}, {literal})";
            }
            fields.Add($"'{property.Name}', {value}");
        }
        return "json_object(" + string.Join(", ", fields) + ")";
    }
}
