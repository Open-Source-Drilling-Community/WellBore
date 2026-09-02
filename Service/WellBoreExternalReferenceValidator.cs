using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using OSDC.Drilling.WellBore.Model;
using WellBoreModel = OSDC.Drilling.WellBore.Model.WellBore;

namespace OSDC.Drilling.WellBore.Service;

public interface IWellBoreExternalReferenceValidator
{
    Task<IReadOnlyList<WellBoreExternalReferenceValidation>> ValidateAsync(
        IReadOnlyCollection<WellBoreModel> wellBores, CancellationToken cancellationToken);
}

internal sealed class UnavailableWellBoreExternalReferenceValidator : IWellBoreExternalReferenceValidator
{
    public Task<IReadOnlyList<WellBoreExternalReferenceValidation>> ValidateAsync(
        IReadOnlyCollection<WellBoreModel> wellBores, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        DateTimeOffset checkedAt = DateTimeOffset.UtcNow;
        IReadOnlyList<WellBoreExternalReferenceValidation> results = wellBores.Select(wellBore =>
            new WellBoreExternalReferenceValidation
            {
                WellBoreID = wellBore.MetaInfo?.ID ?? Guid.Empty,
                WellID = wellBore.WellID,
                RigID = wellBore.RigID,
                CheckedAtUtc = checkedAt,
                Status = WellBoreExternalReferenceValidationStatus.Unavailable,
                Issues = [new WellBoreExternalReferenceIssue
                {
                    Property = "WellID/RigID",
                    Code = "external_reference_validation_unavailable",
                    Message = "Well and Rig reference validation is unavailable in this host."
                }]
            }).ToList();
        return Task.FromResult(results);
    }
}

/// <summary>Reads Well and Rig resources for diagnostics only; it never participates in WellBore writes.</summary>
public sealed class WellBoreExternalReferenceValidator : IWellBoreExternalReferenceValidator
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly IHttpClientFactory _clients;
    private readonly IConfiguration _configuration;

    public WellBoreExternalReferenceValidator(IHttpClientFactory clients, IConfiguration configuration)
    {
        _clients = clients;
        _configuration = configuration;
    }

    public async Task<IReadOnlyList<WellBoreExternalReferenceValidation>> ValidateAsync(
        IReadOnlyCollection<WellBoreModel> wellBores, CancellationToken cancellationToken)
    {
        DateTimeOffset checkedAt = DateTimeOffset.UtcNow;
        Dictionary<Guid, ReferenceResolution> wells = await ResolveDistinctAsync(wellBores
            .Where(value => value.WellID is Guid id && id != Guid.Empty)
            .Select(value => value.WellID!.Value), "WellHostURL", "Well/api/Well", "well", cancellationToken);
        Dictionary<Guid, ReferenceResolution> rigs = await ResolveDistinctAsync(wellBores
            .Where(value => value.RigID is Guid id && id != Guid.Empty)
            .Select(value => value.RigID!.Value), "RigHostURL", "Rig/api/Rig", "rig", cancellationToken);
        return wellBores.Select(value => Validate(value, checkedAt, wells, rigs)).ToList();
    }

    private async Task<Dictionary<Guid, ReferenceResolution>> ResolveDistinctAsync(IEnumerable<Guid> identifiers,
        string configurationKey, string endpoint, string resourceName, CancellationToken cancellationToken)
    {
        Dictionary<Guid, ReferenceResolution> results = [];
        foreach (Guid id in identifiers.Distinct())
            results[id] = await ReadAsync(id, configurationKey, endpoint, resourceName, cancellationToken);
        return results;
    }

    private async Task<ReferenceResolution> ReadAsync(Guid id, string configurationKey, string endpoint,
        string resourceName, CancellationToken cancellationToken)
    {
        string? host = _configuration[configurationKey];
        if (string.IsNullOrWhiteSpace(host))
            return ReferenceResolution.Unavailable($"{resourceName}_service_not_configured", $"{configurationKey} is not configured.");
        try
        {
            using HttpClient client = _clients.CreateClient(nameof(WellBoreExternalReferenceValidator));
            client.BaseAddress = new Uri(host.EndsWith('/') ? host : host + "/");
            using HttpResponseMessage response = await client.GetAsync($"{endpoint}/{id:D}", cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound) return ReferenceResolution.NotFound();
            if (!response.IsSuccessStatusCode)
                return ReferenceResolution.Unavailable($"{resourceName}_service_error",
                    $"{ToTitle(resourceName)} service returned HTTP {(int)response.StatusCode}.");
            ExternalResourceDto? resource = await response.Content.ReadFromJsonAsync<ExternalResourceDto>(JsonOptions, cancellationToken);
            return resource?.MetaInfo?.ID == id
                ? ReferenceResolution.Found()
                : ReferenceResolution.Unavailable($"{resourceName}_response_invalid",
                    $"{ToTitle(resourceName)} service returned a malformed or mismatched resource.");
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or NotSupportedException or TaskCanceledException)
        {
            return ReferenceResolution.Unavailable($"{resourceName}_service_unavailable",
                $"{ToTitle(resourceName)} reference validation is temporarily unavailable.");
        }
    }

    private static WellBoreExternalReferenceValidation Validate(WellBoreModel wellBore, DateTimeOffset checkedAt,
        IReadOnlyDictionary<Guid, ReferenceResolution> wells, IReadOnlyDictionary<Guid, ReferenceResolution> rigs)
    {
        var result = new WellBoreExternalReferenceValidation
        {
            WellBoreID = wellBore.MetaInfo?.ID ?? Guid.Empty,
            WellID = wellBore.WellID,
            RigID = wellBore.RigID,
            CheckedAtUtc = checkedAt,
            Status = WellBoreExternalReferenceValidationStatus.Valid
        };
        ValidateReference(result, "WellID", "well", wellBore.WellID, wells, value => result.WellExists = value);
        ValidateReference(result, "RigID", "rig", wellBore.RigID, rigs, value => result.RigExists = value);
        return result;
    }

    private static void ValidateReference(WellBoreExternalReferenceValidation result, string property,
        string resourceName, Guid? id, IReadOnlyDictionary<Guid, ReferenceResolution> resolutions, Action<bool?> setExists)
    {
        if (id is null) return;
        if (id == Guid.Empty)
        {
            AddInvalid(result, property, "empty_uuid", $"{property} is empty.");
            return;
        }
        if (!resolutions.TryGetValue(id.Value, out ReferenceResolution? resolution) || resolution.IsUnavailable)
        {
            if (result.Status != WellBoreExternalReferenceValidationStatus.Invalid)
                result.Status = WellBoreExternalReferenceValidationStatus.Unavailable;
            result.Issues.Add(new WellBoreExternalReferenceIssue
            {
                Property = property,
                Code = resolution?.Code ?? $"{resourceName}_service_unavailable",
                Message = resolution?.Message ?? $"{ToTitle(resourceName)} reference validation is unavailable."
            });
            return;
        }
        setExists(resolution.Exists);
        if (!resolution.Exists)
            AddInvalid(result, property, $"{resourceName}_not_found",
                $"{ToTitle(resourceName)} UUID '{id}' does not exist.");
    }

    private static void AddInvalid(WellBoreExternalReferenceValidation result, string property, string code, string message)
    {
        result.Status = WellBoreExternalReferenceValidationStatus.Invalid;
        result.Issues.Add(new WellBoreExternalReferenceIssue { Property = property, Code = code, Message = message });
    }

    private static string ToTitle(string value) => char.ToUpperInvariant(value[0]) + value[1..];
    private sealed class ExternalResourceDto { public MetaInfoDto? MetaInfo { get; set; } }
    private sealed class MetaInfoDto { public Guid ID { get; set; } }
    private sealed record ReferenceResolution(bool Exists, bool IsUnavailable, string? Code, string? Message)
    {
        public static ReferenceResolution Found() => new(true, false, null, null);
        public static ReferenceResolution NotFound() => new(false, false, null, null);
        public static ReferenceResolution Unavailable(string code, string message) => new(false, true, code, message);
    }
}
