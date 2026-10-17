using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace OSDC.Drilling.WellBore.ModelShared;

/// <summary>Collects bounded discovery pages. Reference projections are for display/context only;
/// fetch a selected full record before editing or accessing engineering collections.</summary>
public static class DiscoveryClientExtensions
{
    public static async Task<List<SlotLight>> ListClusterSlotsLightAsync(this Client client, System.Guid clusterId, string? name = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
        var items = new List<SlotLight>();
        int offset = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SlotDiscoveryPage page = await client.DiscoverClusterSlotsAsync(offset: offset, limit: 200, clusterId: clusterId, name: name, cancellationToken: cancellationToken);
            items.AddRange(page.Items);
            if (page.NextOffset is not int next) return items;
            if (next <= offset) throw new InvalidOperationException("Discovery pagination did not advance.");
            offset = next;
        }
    }

    public static async Task<List<ClusterLight>> ListClusterLightAsync(this Client client, string? name = null, System.Guid? fieldId = null, System.Guid? rigId = null, bool? isSingleWell = null, bool? isFixedPlatform = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
        var items = new List<ClusterLight>();
        int offset = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ClusterDiscoveryPage page = await client.DiscoverClusterAsync(offset: offset, limit: 200, name: name, fieldId: fieldId, rigId: rigId, isSingleWell: isSingleWell, isFixedPlatform: isFixedPlatform, cancellationToken: cancellationToken);
            items.AddRange(page.Items);
            if (page.NextOffset is not int next) return items;
            if (next <= offset) throw new InvalidOperationException("Discovery pagination did not advance.");
            offset = next;
        }
    }

    public static Task<List<ClusterLight>> ListClusterLightAsync(this Client client, CancellationToken cancellationToken) =>
        client.ListClusterLightAsync(cancellationToken: cancellationToken, name: null);

    /// <summary>Light fields projected into the existing UI reference type. Never submit these partial references as replacements.</summary>
    public static async Task<List<Cluster>> ListClusterReferencesAsync(this Client client, string? name = null, System.Guid? fieldId = null, System.Guid? rigId = null, bool? isSingleWell = null, bool? isFixedPlatform = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
        var summaries = await client.ListClusterLightAsync(name: name, fieldId: fieldId, rigId: rigId, isSingleWell: isSingleWell, isFixedPlatform: isFixedPlatform, cancellationToken: cancellationToken);
        return summaries.ConvertAll(summary => JsonSerializer.Deserialize<Cluster>(JsonSerializer.Serialize(summary))
            ?? throw new JsonException("A discovery reference is null."));
    }

    public static Task<List<Cluster>> ListClusterReferencesAsync(this Client client, CancellationToken cancellationToken) =>
        client.ListClusterReferencesAsync(cancellationToken: cancellationToken, name: null);

    /// <summary>Replace one reference with its complete selected record before editing or reading nested data.</summary>
    public static async Task<Cluster> LoadClusterDetailsAsync(this Client client, IList<Cluster> references, Guid id, CancellationToken cancellationToken = default)
    {
        var value = await client.GetClusterByIdAsync(id, cancellationToken: cancellationToken);
        for (int index = 0; index < references.Count; index++)
            if (DiscoveryIdentifier(references[index]) == id) { references[index] = value; return value; }
        references.Add(value);
        return value;
    }

    public static async Task<List<ClusterFeatureCategoryLight>> ListClusterFeatureCategoryLightAsync(this Client client, string? name = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
        var items = new List<ClusterFeatureCategoryLight>();
        int offset = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ClusterFeatureCategoryDiscoveryPage page = await client.DiscoverClusterFeatureCategoryAsync(offset: offset, limit: 200, name: name, cancellationToken: cancellationToken);
            items.AddRange(page.Items);
            if (page.NextOffset is not int next) return items;
            if (next <= offset) throw new InvalidOperationException("Discovery pagination did not advance.");
            offset = next;
        }
    }

    public static Task<List<ClusterFeatureCategoryLight>> ListClusterFeatureCategoryLightAsync(this Client client, CancellationToken cancellationToken) =>
        client.ListClusterFeatureCategoryLightAsync(cancellationToken: cancellationToken, name: null);

    /// <summary>Light fields projected into the existing UI reference type. Never submit these partial references as replacements.</summary>
    public static async Task<List<ClusterFeatureCategory>> ListClusterFeatureCategoryReferencesAsync(this Client client, string? name = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
        var summaries = await client.ListClusterFeatureCategoryLightAsync(name: name, cancellationToken: cancellationToken);
        return summaries.ConvertAll(summary => JsonSerializer.Deserialize<ClusterFeatureCategory>(JsonSerializer.Serialize(summary))
            ?? throw new JsonException("A discovery reference is null."));
    }

    public static Task<List<ClusterFeatureCategory>> ListClusterFeatureCategoryReferencesAsync(this Client client, CancellationToken cancellationToken) =>
        client.ListClusterFeatureCategoryReferencesAsync(cancellationToken: cancellationToken, name: null);

    /// <summary>Replace one reference with its complete selected record before editing or reading nested data.</summary>
    public static async Task<ClusterFeatureCategory> LoadClusterFeatureCategoryDetailsAsync(this Client client, IList<ClusterFeatureCategory> references, Guid id, CancellationToken cancellationToken = default)
    {
        var value = await client.GetClusterFeatureCategoryByIdAsync(id, cancellationToken: cancellationToken);
        for (int index = 0; index < references.Count; index++)
            if (DiscoveryIdentifier(references[index]) == id) { references[index] = value; return value; }
        references.Add(value);
        return value;
    }

    public static async Task<List<ClusterIdentityLight>> ListClusterIdentityLightAsync(this Client client, string? name = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
        var items = new List<ClusterIdentityLight>();
        int offset = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ClusterIdentityDiscoveryPage page = await client.DiscoverClusterIdentityAsync(offset: offset, limit: 200, name: name, cancellationToken: cancellationToken);
            items.AddRange(page.Items);
            if (page.NextOffset is not int next) return items;
            if (next <= offset) throw new InvalidOperationException("Discovery pagination did not advance.");
            offset = next;
        }
    }

    public static Task<List<ClusterIdentityLight>> ListClusterIdentityLightAsync(this Client client, CancellationToken cancellationToken) =>
        client.ListClusterIdentityLightAsync(cancellationToken: cancellationToken, name: null);

    /// <summary>Light fields projected into the existing UI reference type. Never submit these partial references as replacements.</summary>
    public static async Task<List<ClusterIdentity>> ListClusterIdentityReferencesAsync(this Client client, string? name = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
        var summaries = await client.ListClusterIdentityLightAsync(name: name, cancellationToken: cancellationToken);
        return summaries.ConvertAll(summary => JsonSerializer.Deserialize<ClusterIdentity>(JsonSerializer.Serialize(summary))
            ?? throw new JsonException("A discovery reference is null."));
    }

    public static Task<List<ClusterIdentity>> ListClusterIdentityReferencesAsync(this Client client, CancellationToken cancellationToken) =>
        client.ListClusterIdentityReferencesAsync(cancellationToken: cancellationToken, name: null);

    /// <summary>Replace one reference with its complete selected record before editing or reading nested data.</summary>
    public static async Task<ClusterIdentity> LoadClusterIdentityDetailsAsync(this Client client, IList<ClusterIdentity> references, Guid id, CancellationToken cancellationToken = default)
    {
        var value = await client.GetClusterIdentityByIdAsync(id, cancellationToken: cancellationToken);
        for (int index = 0; index < references.Count; index++)
            if (DiscoveryIdentifier(references[index]) == id) { references[index] = value; return value; }
        references.Add(value);
        return value;
    }

    public static async Task<List<SlotFeatureCategoryLight>> ListSlotFeatureCategoryLightAsync(this Client client, string? name = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
        var items = new List<SlotFeatureCategoryLight>();
        int offset = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SlotFeatureCategoryDiscoveryPage page = await client.DiscoverSlotFeatureCategoryAsync(offset: offset, limit: 200, name: name, cancellationToken: cancellationToken);
            items.AddRange(page.Items);
            if (page.NextOffset is not int next) return items;
            if (next <= offset) throw new InvalidOperationException("Discovery pagination did not advance.");
            offset = next;
        }
    }

    public static Task<List<SlotFeatureCategoryLight>> ListSlotFeatureCategoryLightAsync(this Client client, CancellationToken cancellationToken) =>
        client.ListSlotFeatureCategoryLightAsync(cancellationToken: cancellationToken, name: null);

    /// <summary>Light fields projected into the existing UI reference type. Never submit these partial references as replacements.</summary>
    public static async Task<List<SlotFeatureCategory>> ListSlotFeatureCategoryReferencesAsync(this Client client, string? name = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
        var summaries = await client.ListSlotFeatureCategoryLightAsync(name: name, cancellationToken: cancellationToken);
        return summaries.ConvertAll(summary => JsonSerializer.Deserialize<SlotFeatureCategory>(JsonSerializer.Serialize(summary))
            ?? throw new JsonException("A discovery reference is null."));
    }

    public static Task<List<SlotFeatureCategory>> ListSlotFeatureCategoryReferencesAsync(this Client client, CancellationToken cancellationToken) =>
        client.ListSlotFeatureCategoryReferencesAsync(cancellationToken: cancellationToken, name: null);

    /// <summary>Replace one reference with its complete selected record before editing or reading nested data.</summary>
    public static async Task<SlotFeatureCategory> LoadSlotFeatureCategoryDetailsAsync(this Client client, IList<SlotFeatureCategory> references, Guid id, CancellationToken cancellationToken = default)
    {
        var value = await client.GetSlotFeatureCategoryByIdAsync(id, cancellationToken: cancellationToken);
        for (int index = 0; index < references.Count; index++)
            if (DiscoveryIdentifier(references[index]) == id) { references[index] = value; return value; }
        references.Add(value);
        return value;
    }

    public static async Task<List<FieldLight>> ListFieldLightAsync(this Client client, string? name = null, System.Guid? projectionDefinitionId = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
        var items = new List<FieldLight>();
        int offset = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            FieldDiscoveryPage page = await client.DiscoverFieldAsync(offset: offset, limit: 200, name: name, projectionDefinitionId: projectionDefinitionId, cancellationToken: cancellationToken);
            items.AddRange(page.Items);
            if (page.NextOffset is not int next) return items;
            if (next <= offset) throw new InvalidOperationException("Discovery pagination did not advance.");
            offset = next;
        }
    }

    public static Task<List<FieldLight>> ListFieldLightAsync(this Client client, CancellationToken cancellationToken) =>
        client.ListFieldLightAsync(cancellationToken: cancellationToken, name: null);

    /// <summary>Light fields projected into the existing UI reference type. Never submit these partial references as replacements.</summary>
    public static async Task<List<Field>> ListFieldReferencesAsync(this Client client, string? name = null, System.Guid? projectionDefinitionId = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
        var summaries = await client.ListFieldLightAsync(name: name, projectionDefinitionId: projectionDefinitionId, cancellationToken: cancellationToken);
        return summaries.ConvertAll(summary => JsonSerializer.Deserialize<Field>(JsonSerializer.Serialize(summary))
            ?? throw new JsonException("A discovery reference is null."));
    }

    public static Task<List<Field>> ListFieldReferencesAsync(this Client client, CancellationToken cancellationToken) =>
        client.ListFieldReferencesAsync(cancellationToken: cancellationToken, name: null);

    /// <summary>Replace one reference with its complete selected record before editing or reading nested data.</summary>
    public static async Task<Field> LoadFieldDetailsAsync(this Client client, IList<Field> references, Guid id, CancellationToken cancellationToken = default)
    {
        var value = await client.GetFieldByIdAsync(id, cancellationToken: cancellationToken);
        for (int index = 0; index < references.Count; index++)
            if (DiscoveryIdentifier(references[index]) == id) { references[index] = value; return value; }
        references.Add(value);
        return value;
    }

    public static async Task<List<FieldDelineationLineTypeLight>> ListFieldDelineationLineTypeLightAsync(this Client client, string? name = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
        var items = new List<FieldDelineationLineTypeLight>();
        int offset = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            FieldDelineationLineTypeDiscoveryPage page = await client.DiscoverFieldDelineationLineTypeAsync(offset: offset, limit: 200, name: name, cancellationToken: cancellationToken);
            items.AddRange(page.Items);
            if (page.NextOffset is not int next) return items;
            if (next <= offset) throw new InvalidOperationException("Discovery pagination did not advance.");
            offset = next;
        }
    }

    public static Task<List<FieldDelineationLineTypeLight>> ListFieldDelineationLineTypeLightAsync(this Client client, CancellationToken cancellationToken) =>
        client.ListFieldDelineationLineTypeLightAsync(cancellationToken: cancellationToken, name: null);

    /// <summary>Light fields projected into the existing UI reference type. Never submit these partial references as replacements.</summary>
    public static async Task<List<FieldDelineationLineType>> ListFieldDelineationLineTypeReferencesAsync(this Client client, string? name = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
        var summaries = await client.ListFieldDelineationLineTypeLightAsync(name: name, cancellationToken: cancellationToken);
        return summaries.ConvertAll(summary => JsonSerializer.Deserialize<FieldDelineationLineType>(JsonSerializer.Serialize(summary))
            ?? throw new JsonException("A discovery reference is null."));
    }

    public static Task<List<FieldDelineationLineType>> ListFieldDelineationLineTypeReferencesAsync(this Client client, CancellationToken cancellationToken) =>
        client.ListFieldDelineationLineTypeReferencesAsync(cancellationToken: cancellationToken, name: null);

    /// <summary>Replace one reference with its complete selected record before editing or reading nested data.</summary>
    public static async Task<FieldDelineationLineType> LoadFieldDelineationLineTypeDetailsAsync(this Client client, IList<FieldDelineationLineType> references, Guid id, CancellationToken cancellationToken = default)
    {
        var value = await client.GetFieldDelineationLineTypeByIdAsync(id, cancellationToken: cancellationToken);
        for (int index = 0; index < references.Count; index++)
            if (DiscoveryIdentifier(references[index]) == id) { references[index] = value; return value; }
        references.Add(value);
        return value;
    }

    public static async Task<List<FieldFeatureCategoryLight>> ListFieldFeatureCategoryLightAsync(this Client client, string? name = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
        var items = new List<FieldFeatureCategoryLight>();
        int offset = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            FieldFeatureCategoryDiscoveryPage page = await client.DiscoverFieldFeatureCategoryAsync(offset: offset, limit: 200, name: name, cancellationToken: cancellationToken);
            items.AddRange(page.Items);
            if (page.NextOffset is not int next) return items;
            if (next <= offset) throw new InvalidOperationException("Discovery pagination did not advance.");
            offset = next;
        }
    }

    public static Task<List<FieldFeatureCategoryLight>> ListFieldFeatureCategoryLightAsync(this Client client, CancellationToken cancellationToken) =>
        client.ListFieldFeatureCategoryLightAsync(cancellationToken: cancellationToken, name: null);

    /// <summary>Light fields projected into the existing UI reference type. Never submit these partial references as replacements.</summary>
    public static async Task<List<FieldFeatureCategory>> ListFieldFeatureCategoryReferencesAsync(this Client client, string? name = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
        var summaries = await client.ListFieldFeatureCategoryLightAsync(name: name, cancellationToken: cancellationToken);
        return summaries.ConvertAll(summary => JsonSerializer.Deserialize<FieldFeatureCategory>(JsonSerializer.Serialize(summary))
            ?? throw new JsonException("A discovery reference is null."));
    }

    public static Task<List<FieldFeatureCategory>> ListFieldFeatureCategoryReferencesAsync(this Client client, CancellationToken cancellationToken) =>
        client.ListFieldFeatureCategoryReferencesAsync(cancellationToken: cancellationToken, name: null);

    /// <summary>Replace one reference with its complete selected record before editing or reading nested data.</summary>
    public static async Task<FieldFeatureCategory> LoadFieldFeatureCategoryDetailsAsync(this Client client, IList<FieldFeatureCategory> references, Guid id, CancellationToken cancellationToken = default)
    {
        var value = await client.GetFieldFeatureCategoryByIdAsync(id, cancellationToken: cancellationToken);
        for (int index = 0; index < references.Count; index++)
            if (DiscoveryIdentifier(references[index]) == id) { references[index] = value; return value; }
        references.Add(value);
        return value;
    }

    public static async Task<List<FieldIdentityLight>> ListFieldIdentityLightAsync(this Client client, string? name = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
        var items = new List<FieldIdentityLight>();
        int offset = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            FieldIdentityDiscoveryPage page = await client.DiscoverFieldIdentityAsync(offset: offset, limit: 200, name: name, cancellationToken: cancellationToken);
            items.AddRange(page.Items);
            if (page.NextOffset is not int next) return items;
            if (next <= offset) throw new InvalidOperationException("Discovery pagination did not advance.");
            offset = next;
        }
    }

    public static Task<List<FieldIdentityLight>> ListFieldIdentityLightAsync(this Client client, CancellationToken cancellationToken) =>
        client.ListFieldIdentityLightAsync(cancellationToken: cancellationToken, name: null);

    /// <summary>Light fields projected into the existing UI reference type. Never submit these partial references as replacements.</summary>
    public static async Task<List<FieldIdentity>> ListFieldIdentityReferencesAsync(this Client client, string? name = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
        var summaries = await client.ListFieldIdentityLightAsync(name: name, cancellationToken: cancellationToken);
        return summaries.ConvertAll(summary => JsonSerializer.Deserialize<FieldIdentity>(JsonSerializer.Serialize(summary))
            ?? throw new JsonException("A discovery reference is null."));
    }

    public static Task<List<FieldIdentity>> ListFieldIdentityReferencesAsync(this Client client, CancellationToken cancellationToken) =>
        client.ListFieldIdentityReferencesAsync(cancellationToken: cancellationToken, name: null);

    /// <summary>Replace one reference with its complete selected record before editing or reading nested data.</summary>
    public static async Task<FieldIdentity> LoadFieldIdentityDetailsAsync(this Client client, IList<FieldIdentity> references, Guid id, CancellationToken cancellationToken = default)
    {
        var value = await client.GetFieldIdentityByIdAsync(id, cancellationToken: cancellationToken);
        for (int index = 0; index < references.Count; index++)
            if (DiscoveryIdentifier(references[index]) == id) { references[index] = value; return value; }
        references.Add(value);
        return value;
    }

    public static async Task<List<FieldMembershipCategoryLight>> ListFieldMembershipCategoryLightAsync(this Client client, string? name = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
        var items = new List<FieldMembershipCategoryLight>();
        int offset = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            FieldMembershipCategoryDiscoveryPage page = await client.DiscoverFieldMembershipCategoryAsync(offset: offset, limit: 200, name: name, cancellationToken: cancellationToken);
            items.AddRange(page.Items);
            if (page.NextOffset is not int next) return items;
            if (next <= offset) throw new InvalidOperationException("Discovery pagination did not advance.");
            offset = next;
        }
    }

    public static Task<List<FieldMembershipCategoryLight>> ListFieldMembershipCategoryLightAsync(this Client client, CancellationToken cancellationToken) =>
        client.ListFieldMembershipCategoryLightAsync(cancellationToken: cancellationToken, name: null);

    /// <summary>Light fields projected into the existing UI reference type. Never submit these partial references as replacements.</summary>
    public static async Task<List<FieldMembershipCategory>> ListFieldMembershipCategoryReferencesAsync(this Client client, string? name = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
        var summaries = await client.ListFieldMembershipCategoryLightAsync(name: name, cancellationToken: cancellationToken);
        return summaries.ConvertAll(summary => JsonSerializer.Deserialize<FieldMembershipCategory>(JsonSerializer.Serialize(summary))
            ?? throw new JsonException("A discovery reference is null."));
    }

    public static Task<List<FieldMembershipCategory>> ListFieldMembershipCategoryReferencesAsync(this Client client, CancellationToken cancellationToken) =>
        client.ListFieldMembershipCategoryReferencesAsync(cancellationToken: cancellationToken, name: null);

    /// <summary>Replace one reference with its complete selected record before editing or reading nested data.</summary>
    public static async Task<FieldMembershipCategory> LoadFieldMembershipCategoryDetailsAsync(this Client client, IList<FieldMembershipCategory> references, Guid id, CancellationToken cancellationToken = default)
    {
        var value = await client.GetFieldMembershipCategoryByIdAsync(id, cancellationToken: cancellationToken);
        for (int index = 0; index < references.Count; index++)
            if (DiscoveryIdentifier(references[index]) == id) { references[index] = value; return value; }
        references.Add(value);
        return value;
    }

    public static async Task<List<RigLight>> ListRigLightAsync(this Client client, string? name = null, System.Guid? clusterId = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
        var items = new List<RigLight>();
        int offset = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RigDiscoveryPage page = await client.DiscoverRigAsync(offset: offset, limit: 200, name: name, clusterId: clusterId, cancellationToken: cancellationToken);
            items.AddRange(page.Items);
            if (page.NextOffset is not int next) return items;
            if (next <= offset) throw new InvalidOperationException("Discovery pagination did not advance.");
            offset = next;
        }
    }

    public static Task<List<RigLight>> ListRigLightAsync(this Client client, CancellationToken cancellationToken) =>
        client.ListRigLightAsync(cancellationToken: cancellationToken, name: null);

    /// <summary>Light fields projected into the existing UI reference type. Never submit these partial references as replacements.</summary>
    public static async Task<List<RigReadResponse>> ListRigReferencesAsync(this Client client, string? name = null, System.Guid? clusterId = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
        var summaries = await client.ListRigLightAsync(name: name, clusterId: clusterId, cancellationToken: cancellationToken);
        return summaries.ConvertAll(summary => JsonSerializer.Deserialize<RigReadResponse>(JsonSerializer.Serialize(summary))
            ?? throw new JsonException("A discovery reference is null."));
    }

    public static Task<List<RigReadResponse>> ListRigReferencesAsync(this Client client, CancellationToken cancellationToken) =>
        client.ListRigReferencesAsync(cancellationToken: cancellationToken, name: null);

    /// <summary>Replace one reference with its complete selected record before editing or reading nested data.</summary>
    public static async Task<RigReadResponse> LoadRigDetailsAsync(this Client client, IList<RigReadResponse> references, Guid id, CancellationToken cancellationToken = default)
    {
        var value = await client.GetRigByIdAsync(id, cancellationToken: cancellationToken);
        for (int index = 0; index < references.Count; index++)
            if (DiscoveryIdentifier(references[index]) == id) { references[index] = value; return value; }
        references.Add(value);
        return value;
    }

    public static async Task<List<RigFeatureCategoryLight>> ListRigFeatureCategoryLightAsync(this Client client, string? name = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
        var items = new List<RigFeatureCategoryLight>();
        int offset = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RigFeatureCategoryDiscoveryPage page = await client.DiscoverRigFeatureCategoryAsync(offset: offset, limit: 200, name: name, cancellationToken: cancellationToken);
            items.AddRange(page.Items);
            if (page.NextOffset is not int next) return items;
            if (next <= offset) throw new InvalidOperationException("Discovery pagination did not advance.");
            offset = next;
        }
    }

    public static Task<List<RigFeatureCategoryLight>> ListRigFeatureCategoryLightAsync(this Client client, CancellationToken cancellationToken) =>
        client.ListRigFeatureCategoryLightAsync(cancellationToken: cancellationToken, name: null);

    /// <summary>Light fields projected into the existing UI reference type. Never submit these partial references as replacements.</summary>
    public static async Task<List<RigFeatureCategory>> ListRigFeatureCategoryReferencesAsync(this Client client, string? name = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
        var summaries = await client.ListRigFeatureCategoryLightAsync(name: name, cancellationToken: cancellationToken);
        return summaries.ConvertAll(summary => JsonSerializer.Deserialize<RigFeatureCategory>(JsonSerializer.Serialize(summary))
            ?? throw new JsonException("A discovery reference is null."));
    }

    public static Task<List<RigFeatureCategory>> ListRigFeatureCategoryReferencesAsync(this Client client, CancellationToken cancellationToken) =>
        client.ListRigFeatureCategoryReferencesAsync(cancellationToken: cancellationToken, name: null);

    /// <summary>Replace one reference with its complete selected record before editing or reading nested data.</summary>
    public static async Task<RigFeatureCategory> LoadRigFeatureCategoryDetailsAsync(this Client client, IList<RigFeatureCategory> references, Guid id, CancellationToken cancellationToken = default)
    {
        var value = await client.GetRigFeatureCategoryByIdAsync(id, cancellationToken: cancellationToken);
        for (int index = 0; index < references.Count; index++)
            if (DiscoveryIdentifier(references[index]) == id) { references[index] = value; return value; }
        references.Add(value);
        return value;
    }

    public static async Task<List<AntiCollisionPolicyRevisionLight>> ListAntiCollisionPolicyRevisionLightAsync(this Client client, string? name = null, System.Guid? policyId = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
        var items = new List<AntiCollisionPolicyRevisionLight>();
        int offset = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AntiCollisionPolicyRevisionDiscoveryPage page = await client.DiscoverAntiCollisionPolicyRevisionAsync(offset: offset, limit: 200, name: name, policyId: policyId, cancellationToken: cancellationToken);
            items.AddRange(page.Items);
            if (page.NextOffset is not int next) return items;
            if (next <= offset) throw new InvalidOperationException("Discovery pagination did not advance.");
            offset = next;
        }
    }

    public static Task<List<AntiCollisionPolicyRevisionLight>> ListAntiCollisionPolicyRevisionLightAsync(this Client client, CancellationToken cancellationToken) =>
        client.ListAntiCollisionPolicyRevisionLightAsync(cancellationToken: cancellationToken, name: null);

    /// <summary>Light fields projected into the existing UI reference type. Never submit these partial references as replacements.</summary>
    public static async Task<List<AntiCollisionPolicyRevision>> ListAntiCollisionPolicyRevisionReferencesAsync(this Client client, string? name = null, System.Guid? policyId = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
        var summaries = await client.ListAntiCollisionPolicyRevisionLightAsync(name: name, policyId: policyId, cancellationToken: cancellationToken);
        return summaries.ConvertAll(summary => JsonSerializer.Deserialize<AntiCollisionPolicyRevision>(JsonSerializer.Serialize(summary))
            ?? throw new JsonException("A discovery reference is null."));
    }

    public static Task<List<AntiCollisionPolicyRevision>> ListAntiCollisionPolicyRevisionReferencesAsync(this Client client, CancellationToken cancellationToken) =>
        client.ListAntiCollisionPolicyRevisionReferencesAsync(cancellationToken: cancellationToken, name: null);

    /// <summary>Replace one reference with its complete selected record before editing or reading nested data.</summary>
    public static async Task<AntiCollisionPolicyRevision> LoadAntiCollisionPolicyRevisionDetailsAsync(this Client client, IList<AntiCollisionPolicyRevision> references, Guid id, CancellationToken cancellationToken = default)
    {
        var value = await client.GetAntiCollisionPolicyRevisionByIdAsync(id, cancellationToken: cancellationToken);
        for (int index = 0; index < references.Count; index++)
            if (DiscoveryIdentifier(references[index]) == id) { references[index] = value; return value; }
        references.Add(value);
        return value;
    }

    public static async Task<List<DirectionalControlEvaluationCaseLight>> ListDirectionalControlEvaluationCaseLightAsync(this Client client, string? name = null, System.Guid? referenceTrajectoryId = null, System.Guid? actualTrajectoryId = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
        var items = new List<DirectionalControlEvaluationCaseLight>();
        int offset = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            DirectionalControlEvaluationCaseDiscoveryPage page = await client.DiscoverDirectionalControlEvaluationCaseAsync(offset: offset, limit: 200, name: name, referenceTrajectoryId: referenceTrajectoryId, actualTrajectoryId: actualTrajectoryId, cancellationToken: cancellationToken);
            items.AddRange(page.Items);
            if (page.NextOffset is not int next) return items;
            if (next <= offset) throw new InvalidOperationException("Discovery pagination did not advance.");
            offset = next;
        }
    }

    public static Task<List<DirectionalControlEvaluationCaseLight>> ListDirectionalControlEvaluationCaseLightAsync(this Client client, CancellationToken cancellationToken) =>
        client.ListDirectionalControlEvaluationCaseLightAsync(cancellationToken: cancellationToken, name: null);

    /// <summary>Light fields projected into the existing UI reference type. Never submit these partial references as replacements.</summary>
    public static async Task<List<DirectionalControlEvaluationCase>> ListDirectionalControlEvaluationCaseReferencesAsync(this Client client, string? name = null, System.Guid? referenceTrajectoryId = null, System.Guid? actualTrajectoryId = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
        var summaries = await client.ListDirectionalControlEvaluationCaseLightAsync(name: name, referenceTrajectoryId: referenceTrajectoryId, actualTrajectoryId: actualTrajectoryId, cancellationToken: cancellationToken);
        return summaries.ConvertAll(summary => JsonSerializer.Deserialize<DirectionalControlEvaluationCase>(JsonSerializer.Serialize(summary))
            ?? throw new JsonException("A discovery reference is null."));
    }

    public static Task<List<DirectionalControlEvaluationCase>> ListDirectionalControlEvaluationCaseReferencesAsync(this Client client, CancellationToken cancellationToken) =>
        client.ListDirectionalControlEvaluationCaseReferencesAsync(cancellationToken: cancellationToken, name: null);

    /// <summary>Replace one reference with its complete selected record before editing or reading nested data.</summary>
    public static async Task<DirectionalControlEvaluationCase> LoadDirectionalControlEvaluationCaseDetailsAsync(this Client client, IList<DirectionalControlEvaluationCase> references, Guid id, CancellationToken cancellationToken = default)
    {
        var value = await client.GetDirectionalControlEvaluationCaseByIdAsync(id, cancellationToken: cancellationToken);
        for (int index = 0; index < references.Count; index++)
            if (DiscoveryIdentifier(references[index]) == id) { references[index] = value; return value; }
        references.Add(value);
        return value;
    }

    public static async Task<List<FieldAntiCollisionPolicyAssignment>> ListFieldAntiCollisionPolicyAssignmentLightAsync(this Client client, System.Guid? fieldId = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
        var items = new List<FieldAntiCollisionPolicyAssignment>();
        int offset = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            FieldAntiCollisionPolicyAssignmentDiscoveryPage page = await client.DiscoverFieldAntiCollisionPolicyAssignmentAsync(offset: offset, limit: 200, fieldId: fieldId, cancellationToken: cancellationToken);
            items.AddRange(page.Items);
            if (page.NextOffset is not int next) return items;
            if (next <= offset) throw new InvalidOperationException("Discovery pagination did not advance.");
            offset = next;
        }
    }

    public static Task<List<FieldAntiCollisionPolicyAssignment>> ListFieldAntiCollisionPolicyAssignmentLightAsync(this Client client, CancellationToken cancellationToken) =>
        client.ListFieldAntiCollisionPolicyAssignmentLightAsync(cancellationToken: cancellationToken, fieldId: null);

    /// <summary>Light fields projected into the existing UI reference type. Never submit these partial references as replacements.</summary>
    public static async Task<List<FieldAntiCollisionPolicyAssignment>> ListFieldAntiCollisionPolicyAssignmentReferencesAsync(this Client client, System.Guid? fieldId = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
        var summaries = await client.ListFieldAntiCollisionPolicyAssignmentLightAsync(fieldId: fieldId, cancellationToken: cancellationToken);
        return summaries.ConvertAll(summary => JsonSerializer.Deserialize<FieldAntiCollisionPolicyAssignment>(JsonSerializer.Serialize(summary))
            ?? throw new JsonException("A discovery reference is null."));
    }

    public static Task<List<FieldAntiCollisionPolicyAssignment>> ListFieldAntiCollisionPolicyAssignmentReferencesAsync(this Client client, CancellationToken cancellationToken) =>
        client.ListFieldAntiCollisionPolicyAssignmentReferencesAsync(cancellationToken: cancellationToken, fieldId: null);

    /// <summary>Replace one reference with its complete selected record before editing or reading nested data.</summary>
    public static async Task<FieldAntiCollisionPolicyAssignment> LoadFieldAntiCollisionPolicyAssignmentDetailsAsync(this Client client, IList<FieldAntiCollisionPolicyAssignment> references, Guid id, CancellationToken cancellationToken = default)
    {
        var value = await client.GetFieldAntiCollisionPolicyAssignmentByIdAsync(id, cancellationToken: cancellationToken);
        for (int index = 0; index < references.Count; index++)
            if (DiscoveryIdentifier(references[index]) == id) { references[index] = value; return value; }
        references.Add(value);
        return value;
    }

    public static async Task<List<GlobalAntiCollisionLight>> ListGlobalAntiCollisionsLightAsync(this Client client, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
        var items = new List<GlobalAntiCollisionLight>();
        int offset = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            GlobalAntiCollisionsDiscoveryPage page = await client.DiscoverGlobalAntiCollisionsAsync(offset: offset, limit: 200, cancellationToken: cancellationToken);
            items.AddRange(page.Items);
            if (page.NextOffset is not int next) return items;
            if (next <= offset) throw new InvalidOperationException("Discovery pagination did not advance.");
            offset = next;
        }
    }

    public static async Task<List<InterpolatedTrajectoryLight>> ListInterpolatedTrajectoryLightAsync(this Client client, string? name = null, System.Guid? trajectoryId = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
        var items = new List<InterpolatedTrajectoryLight>();
        int offset = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            InterpolatedTrajectoryDiscoveryPage page = await client.DiscoverInterpolatedTrajectoryAsync(offset: offset, limit: 200, name: name, trajectoryId: trajectoryId, cancellationToken: cancellationToken);
            items.AddRange(page.Items);
            if (page.NextOffset is not int next) return items;
            if (next <= offset) throw new InvalidOperationException("Discovery pagination did not advance.");
            offset = next;
        }
    }

    public static Task<List<InterpolatedTrajectoryLight>> ListInterpolatedTrajectoryLightAsync(this Client client, CancellationToken cancellationToken) =>
        client.ListInterpolatedTrajectoryLightAsync(cancellationToken: cancellationToken, name: null);

    /// <summary>Light fields projected into the existing UI reference type. Never submit these partial references as replacements.</summary>
    public static async Task<List<InterpolatedTrajectory>> ListInterpolatedTrajectoryReferencesAsync(this Client client, string? name = null, System.Guid? trajectoryId = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
        var summaries = await client.ListInterpolatedTrajectoryLightAsync(name: name, trajectoryId: trajectoryId, cancellationToken: cancellationToken);
        return summaries.ConvertAll(summary => JsonSerializer.Deserialize<InterpolatedTrajectory>(JsonSerializer.Serialize(summary))
            ?? throw new JsonException("A discovery reference is null."));
    }

    public static Task<List<InterpolatedTrajectory>> ListInterpolatedTrajectoryReferencesAsync(this Client client, CancellationToken cancellationToken) =>
        client.ListInterpolatedTrajectoryReferencesAsync(cancellationToken: cancellationToken, name: null);

    /// <summary>Replace one reference with its complete selected record before editing or reading nested data.</summary>
    public static async Task<InterpolatedTrajectory> LoadInterpolatedTrajectoryDetailsAsync(this Client client, IList<InterpolatedTrajectory> references, Guid id, CancellationToken cancellationToken = default)
    {
        var value = await client.GetInterpolatedTrajectoryByIdAsync(id, cancellationToken: cancellationToken);
        for (int index = 0; index < references.Count; index++)
            if (DiscoveryIdentifier(references[index]) == id) { references[index] = value; return value; }
        references.Add(value);
        return value;
    }

    public static async Task<List<OctreeLight>> ListOctreesLightAsync(this Client client, TrajectoryType? trajectoryType = null, bool? isDefinitive = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
        var items = new List<OctreeLight>();
        int offset = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            OctreesDiscoveryPage page = await client.DiscoverOctreesAsync(offset: offset, limit: 200, trajectoryType: trajectoryType, isDefinitive: isDefinitive, cancellationToken: cancellationToken);
            items.AddRange(page.Items);
            if (page.NextOffset is not int next) return items;
            if (next <= offset) throw new InvalidOperationException("Discovery pagination did not advance.");
            offset = next;
        }
    }

    public static Task<List<OctreeLight>> ListOctreesLightAsync(this Client client, CancellationToken cancellationToken) =>
        client.ListOctreesLightAsync(cancellationToken: cancellationToken, trajectoryType: null);

    public static async Task<List<SurveyRunLight>> ListSurveyRunLightAsync(this Client client, string? name = null, System.Guid? fieldId = null, System.Guid? clusterId = null, System.Guid? wellId = null, System.Guid? wellBoreId = null, System.Guid? surveyInstrumentId = null, System.Guid? parentSurveyRunId = null, SurveyRunType? surveyRunType = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
        var items = new List<SurveyRunLight>();
        int offset = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SurveyRunDiscoveryPage page = await client.DiscoverSurveyRunAsync(offset: offset, limit: 200, name: name, fieldId: fieldId, clusterId: clusterId, wellId: wellId, wellBoreId: wellBoreId, surveyInstrumentId: surveyInstrumentId, parentSurveyRunId: parentSurveyRunId, surveyRunType: surveyRunType, cancellationToken: cancellationToken);
            items.AddRange(page.Items);
            if (page.NextOffset is not int next) return items;
            if (next <= offset) throw new InvalidOperationException("Discovery pagination did not advance.");
            offset = next;
        }
    }

    public static Task<List<SurveyRunLight>> ListSurveyRunLightAsync(this Client client, CancellationToken cancellationToken) =>
        client.ListSurveyRunLightAsync(cancellationToken: cancellationToken, name: null);

    /// <summary>Light fields projected into the existing UI reference type. Never submit these partial references as replacements.</summary>
    public static async Task<List<SurveyRun>> ListSurveyRunReferencesAsync(this Client client, string? name = null, System.Guid? fieldId = null, System.Guid? clusterId = null, System.Guid? wellId = null, System.Guid? wellBoreId = null, System.Guid? surveyInstrumentId = null, System.Guid? parentSurveyRunId = null, SurveyRunType? surveyRunType = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
        var summaries = await client.ListSurveyRunLightAsync(name: name, fieldId: fieldId, clusterId: clusterId, wellId: wellId, wellBoreId: wellBoreId, surveyInstrumentId: surveyInstrumentId, parentSurveyRunId: parentSurveyRunId, surveyRunType: surveyRunType, cancellationToken: cancellationToken);
        return summaries.ConvertAll(summary => JsonSerializer.Deserialize<SurveyRun>(JsonSerializer.Serialize(summary))
            ?? throw new JsonException("A discovery reference is null."));
    }

    public static Task<List<SurveyRun>> ListSurveyRunReferencesAsync(this Client client, CancellationToken cancellationToken) =>
        client.ListSurveyRunReferencesAsync(cancellationToken: cancellationToken, name: null);

    /// <summary>Replace one reference with its complete selected record before editing or reading nested data.</summary>
    public static async Task<SurveyRun> LoadSurveyRunDetailsAsync(this Client client, IList<SurveyRun> references, Guid id, CancellationToken cancellationToken = default)
    {
        var value = await client.GetSurveyRunByIdAsync(id, includeCalculatedStations: true, cancellationToken: cancellationToken);
        for (int index = 0; index < references.Count; index++)
            if (DiscoveryIdentifier(references[index]) == id) { references[index] = value; return value; }
        references.Add(value);
        return value;
    }

    public static async Task<List<SurveyRunBatchImportLight>> ListSurveyRunBatchImportLightAsync(this Client client, string? name = null, System.Guid? selectedFieldId = null, System.Guid? selectedClusterId = null, System.Guid? selectedWellId = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
        var items = new List<SurveyRunBatchImportLight>();
        int offset = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SurveyRunBatchImportDiscoveryPage page = await client.DiscoverSurveyRunBatchImportAsync(offset: offset, limit: 200, name: name, selectedFieldId: selectedFieldId, selectedClusterId: selectedClusterId, selectedWellId: selectedWellId, cancellationToken: cancellationToken);
            items.AddRange(page.Items);
            if (page.NextOffset is not int next) return items;
            if (next <= offset) throw new InvalidOperationException("Discovery pagination did not advance.");
            offset = next;
        }
    }

    public static Task<List<SurveyRunBatchImportLight>> ListSurveyRunBatchImportLightAsync(this Client client, CancellationToken cancellationToken) =>
        client.ListSurveyRunBatchImportLightAsync(cancellationToken: cancellationToken, name: null);

    /// <summary>Light fields projected into the existing UI reference type. Never submit these partial references as replacements.</summary>
    public static async Task<List<SurveyRunBatchImport>> ListSurveyRunBatchImportReferencesAsync(this Client client, string? name = null, System.Guid? selectedFieldId = null, System.Guid? selectedClusterId = null, System.Guid? selectedWellId = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
        var summaries = await client.ListSurveyRunBatchImportLightAsync(name: name, selectedFieldId: selectedFieldId, selectedClusterId: selectedClusterId, selectedWellId: selectedWellId, cancellationToken: cancellationToken);
        return summaries.ConvertAll(summary => JsonSerializer.Deserialize<SurveyRunBatchImport>(JsonSerializer.Serialize(summary))
            ?? throw new JsonException("A discovery reference is null."));
    }

    public static Task<List<SurveyRunBatchImport>> ListSurveyRunBatchImportReferencesAsync(this Client client, CancellationToken cancellationToken) =>
        client.ListSurveyRunBatchImportReferencesAsync(cancellationToken: cancellationToken, name: null);

    /// <summary>Replace one reference with its complete selected record before editing or reading nested data.</summary>
    public static async Task<SurveyRunBatchImport> LoadSurveyRunBatchImportDetailsAsync(this Client client, IList<SurveyRunBatchImport> references, Guid id, CancellationToken cancellationToken = default)
    {
        var value = await client.GetSurveyRunBatchImportByIdAsync(id, cancellationToken: cancellationToken);
        for (int index = 0; index < references.Count; index++)
            if (DiscoveryIdentifier(references[index]) == id) { references[index] = value; return value; }
        references.Add(value);
        return value;
    }

    public static async Task<List<SurveyRunMinimumDistanceCalculationLight>> ListSurveyRunMinimumDistanceCalculationLightAsync(this Client client, string? name = null, System.Guid? referenceSurveyRunId = null, System.Guid? globalMinimumComparisonSurveyRunId = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
        var items = new List<SurveyRunMinimumDistanceCalculationLight>();
        int offset = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SurveyRunMinimumDistanceCalculationDiscoveryPage page = await client.DiscoverSurveyRunMinimumDistanceCalculationAsync(offset: offset, limit: 200, name: name, referenceSurveyRunId: referenceSurveyRunId, globalMinimumComparisonSurveyRunId: globalMinimumComparisonSurveyRunId, cancellationToken: cancellationToken);
            items.AddRange(page.Items);
            if (page.NextOffset is not int next) return items;
            if (next <= offset) throw new InvalidOperationException("Discovery pagination did not advance.");
            offset = next;
        }
    }

    public static Task<List<SurveyRunMinimumDistanceCalculationLight>> ListSurveyRunMinimumDistanceCalculationLightAsync(this Client client, CancellationToken cancellationToken) =>
        client.ListSurveyRunMinimumDistanceCalculationLightAsync(cancellationToken: cancellationToken, name: null);

    /// <summary>Light fields projected into the existing UI reference type. Never submit these partial references as replacements.</summary>
    public static async Task<List<SurveyRunMinimumDistanceCalculation>> ListSurveyRunMinimumDistanceCalculationReferencesAsync(this Client client, string? name = null, System.Guid? referenceSurveyRunId = null, System.Guid? globalMinimumComparisonSurveyRunId = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
        var summaries = await client.ListSurveyRunMinimumDistanceCalculationLightAsync(name: name, referenceSurveyRunId: referenceSurveyRunId, globalMinimumComparisonSurveyRunId: globalMinimumComparisonSurveyRunId, cancellationToken: cancellationToken);
        return summaries.ConvertAll(summary => JsonSerializer.Deserialize<SurveyRunMinimumDistanceCalculation>(JsonSerializer.Serialize(summary))
            ?? throw new JsonException("A discovery reference is null."));
    }

    public static Task<List<SurveyRunMinimumDistanceCalculation>> ListSurveyRunMinimumDistanceCalculationReferencesAsync(this Client client, CancellationToken cancellationToken) =>
        client.ListSurveyRunMinimumDistanceCalculationReferencesAsync(cancellationToken: cancellationToken, name: null);

    /// <summary>Replace one reference with its complete selected record before editing or reading nested data.</summary>
    public static async Task<SurveyRunMinimumDistanceCalculation> LoadSurveyRunMinimumDistanceCalculationDetailsAsync(this Client client, IList<SurveyRunMinimumDistanceCalculation> references, Guid id, CancellationToken cancellationToken = default)
    {
        var value = await client.GetSurveyRunMinimumDistanceCalculationByIdAsync(id, cancellationToken: cancellationToken);
        for (int index = 0; index < references.Count; index++)
            if (DiscoveryIdentifier(references[index]) == id) { references[index] = value; return value; }
        references.Add(value);
        return value;
    }

    public static async Task<List<SurveyStationEllipseCalculationLight>> ListSurveyStationEllipseCalculationLightAsync(this Client client, string? name = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
        var items = new List<SurveyStationEllipseCalculationLight>();
        int offset = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            SurveyStationEllipseCalculationDiscoveryPage page = await client.DiscoverSurveyStationEllipseCalculationAsync(offset: offset, limit: 200, name: name, cancellationToken: cancellationToken);
            items.AddRange(page.Items);
            if (page.NextOffset is not int next) return items;
            if (next <= offset) throw new InvalidOperationException("Discovery pagination did not advance.");
            offset = next;
        }
    }

    public static Task<List<SurveyStationEllipseCalculationLight>> ListSurveyStationEllipseCalculationLightAsync(this Client client, CancellationToken cancellationToken) =>
        client.ListSurveyStationEllipseCalculationLightAsync(cancellationToken: cancellationToken, name: null);

    /// <summary>Light fields projected into the existing UI reference type. Never submit these partial references as replacements.</summary>
    public static async Task<List<SurveyStationEllipseCalculation>> ListSurveyStationEllipseCalculationReferencesAsync(this Client client, string? name = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
        var summaries = await client.ListSurveyStationEllipseCalculationLightAsync(name: name, cancellationToken: cancellationToken);
        return summaries.ConvertAll(summary => JsonSerializer.Deserialize<SurveyStationEllipseCalculation>(JsonSerializer.Serialize(summary))
            ?? throw new JsonException("A discovery reference is null."));
    }

    public static Task<List<SurveyStationEllipseCalculation>> ListSurveyStationEllipseCalculationReferencesAsync(this Client client, CancellationToken cancellationToken) =>
        client.ListSurveyStationEllipseCalculationReferencesAsync(cancellationToken: cancellationToken, name: null);

    /// <summary>Replace one reference with its complete selected record before editing or reading nested data.</summary>
    public static async Task<SurveyStationEllipseCalculation> LoadSurveyStationEllipseCalculationDetailsAsync(this Client client, IList<SurveyStationEllipseCalculation> references, Guid id, CancellationToken cancellationToken = default)
    {
        var value = await client.GetSurveyStationEllipseCalculationByIdAsync(id, cancellationToken: cancellationToken);
        for (int index = 0; index < references.Count; index++)
            if (DiscoveryIdentifier(references[index]) == id) { references[index] = value; return value; }
        references.Add(value);
        return value;
    }

    public static async Task<List<TargetLandingCaseLight>> ListTargetLandingCaseLightAsync(this Client client, string? name = null, System.Guid? sourceTrajectoryId = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
        var items = new List<TargetLandingCaseLight>();
        int offset = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            TargetLandingCaseDiscoveryPage page = await client.DiscoverTargetLandingCaseAsync(offset: offset, limit: 200, name: name, sourceTrajectoryId: sourceTrajectoryId, cancellationToken: cancellationToken);
            items.AddRange(page.Items);
            if (page.NextOffset is not int next) return items;
            if (next <= offset) throw new InvalidOperationException("Discovery pagination did not advance.");
            offset = next;
        }
    }

    public static Task<List<TargetLandingCaseLight>> ListTargetLandingCaseLightAsync(this Client client, CancellationToken cancellationToken) =>
        client.ListTargetLandingCaseLightAsync(cancellationToken: cancellationToken, name: null);

    /// <summary>Light fields projected into the existing UI reference type. Never submit these partial references as replacements.</summary>
    public static async Task<List<TargetLandingCase>> ListTargetLandingCaseReferencesAsync(this Client client, string? name = null, System.Guid? sourceTrajectoryId = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
        var summaries = await client.ListTargetLandingCaseLightAsync(name: name, sourceTrajectoryId: sourceTrajectoryId, cancellationToken: cancellationToken);
        return summaries.ConvertAll(summary => JsonSerializer.Deserialize<TargetLandingCase>(JsonSerializer.Serialize(summary))
            ?? throw new JsonException("A discovery reference is null."));
    }

    public static Task<List<TargetLandingCase>> ListTargetLandingCaseReferencesAsync(this Client client, CancellationToken cancellationToken) =>
        client.ListTargetLandingCaseReferencesAsync(cancellationToken: cancellationToken, name: null);

    /// <summary>Replace one reference with its complete selected record before editing or reading nested data.</summary>
    public static async Task<TargetLandingCase> LoadTargetLandingCaseDetailsAsync(this Client client, IList<TargetLandingCase> references, Guid id, CancellationToken cancellationToken = default)
    {
        var value = await client.GetTargetLandingCaseByIdAsync(id, cancellationToken: cancellationToken);
        for (int index = 0; index < references.Count; index++)
            if (DiscoveryIdentifier(references[index]) == id) { references[index] = value; return value; }
        references.Add(value);
        return value;
    }

    public static async Task<List<TrajectoryLight>> ListTrajectoryLightAsync(this Client client, string? name = null, System.Guid? fieldId = null, System.Guid? clusterId = null, System.Guid? wellId = null, System.Guid? wellBoreId = null, TrajectoryType? trajectoryType = null, bool? isDefinitive = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
        var items = new List<TrajectoryLight>();
        int offset = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            TrajectoryDiscoveryPage page = await client.DiscoverTrajectoryAsync(offset: offset, limit: 200, name: name, fieldId: fieldId, clusterId: clusterId, wellId: wellId, wellBoreId: wellBoreId, trajectoryType: trajectoryType, isDefinitive: isDefinitive, cancellationToken: cancellationToken);
            items.AddRange(page.Items);
            if (page.NextOffset is not int next) return items;
            if (next <= offset) throw new InvalidOperationException("Discovery pagination did not advance.");
            offset = next;
        }
    }

    public static Task<List<TrajectoryLight>> ListTrajectoryLightAsync(this Client client, CancellationToken cancellationToken) =>
        client.ListTrajectoryLightAsync(cancellationToken: cancellationToken, name: null);

    /// <summary>Light fields projected into the existing UI reference type. Never submit these partial references as replacements.</summary>
    public static async Task<List<Trajectory>> ListTrajectoryReferencesAsync(this Client client, string? name = null, System.Guid? fieldId = null, System.Guid? clusterId = null, System.Guid? wellId = null, System.Guid? wellBoreId = null, TrajectoryType? trajectoryType = null, bool? isDefinitive = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
        var summaries = await client.ListTrajectoryLightAsync(name: name, fieldId: fieldId, clusterId: clusterId, wellId: wellId, wellBoreId: wellBoreId, trajectoryType: trajectoryType, isDefinitive: isDefinitive, cancellationToken: cancellationToken);
        return summaries.ConvertAll(summary => JsonSerializer.Deserialize<Trajectory>(JsonSerializer.Serialize(summary))
            ?? throw new JsonException("A discovery reference is null."));
    }

    public static Task<List<Trajectory>> ListTrajectoryReferencesAsync(this Client client, CancellationToken cancellationToken) =>
        client.ListTrajectoryReferencesAsync(cancellationToken: cancellationToken, name: null);

    /// <summary>Replace one reference with its complete selected record before editing or reading nested data.</summary>
    public static async Task<Trajectory> LoadTrajectoryDetailsAsync(this Client client, IList<Trajectory> references, Guid id, CancellationToken cancellationToken = default)
    {
        var value = await client.GetTrajectoryByIdAsync(id, cancellationToken: cancellationToken);
        for (int index = 0; index < references.Count; index++)
            if (DiscoveryIdentifier(references[index]) == id) { references[index] = value; return value; }
        references.Add(value);
        return value;
    }

    public static async Task<List<TrajectoryAggregationCaseLight>> ListTrajectoryAggregationCaseLightAsync(this Client client, string? name = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
        var items = new List<TrajectoryAggregationCaseLight>();
        int offset = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            TrajectoryAggregationCaseDiscoveryPage page = await client.DiscoverTrajectoryAggregationCaseAsync(offset: offset, limit: 200, name: name, cancellationToken: cancellationToken);
            items.AddRange(page.Items);
            if (page.NextOffset is not int next) return items;
            if (next <= offset) throw new InvalidOperationException("Discovery pagination did not advance.");
            offset = next;
        }
    }

    public static Task<List<TrajectoryAggregationCaseLight>> ListTrajectoryAggregationCaseLightAsync(this Client client, CancellationToken cancellationToken) =>
        client.ListTrajectoryAggregationCaseLightAsync(cancellationToken: cancellationToken, name: null);

    /// <summary>Light fields projected into the existing UI reference type. Never submit these partial references as replacements.</summary>
    public static async Task<List<TrajectoryAggregationCase>> ListTrajectoryAggregationCaseReferencesAsync(this Client client, string? name = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
        var summaries = await client.ListTrajectoryAggregationCaseLightAsync(name: name, cancellationToken: cancellationToken);
        return summaries.ConvertAll(summary => JsonSerializer.Deserialize<TrajectoryAggregationCase>(JsonSerializer.Serialize(summary))
            ?? throw new JsonException("A discovery reference is null."));
    }

    public static Task<List<TrajectoryAggregationCase>> ListTrajectoryAggregationCaseReferencesAsync(this Client client, CancellationToken cancellationToken) =>
        client.ListTrajectoryAggregationCaseReferencesAsync(cancellationToken: cancellationToken, name: null);

    /// <summary>Replace one reference with its complete selected record before editing or reading nested data.</summary>
    public static async Task<TrajectoryAggregationCase> LoadTrajectoryAggregationCaseDetailsAsync(this Client client, IList<TrajectoryAggregationCase> references, Guid id, CancellationToken cancellationToken = default)
    {
        var value = await client.GetTrajectoryAggregationCaseByIdAsync(id, cancellationToken: cancellationToken);
        for (int index = 0; index < references.Count; index++)
            if (DiscoveryIdentifier(references[index]) == id) { references[index] = value; return value; }
        references.Add(value);
        return value;
    }

    public static async Task<List<TrajectoryExtrapolationCaseLight>> ListTrajectoryExtrapolationCaseLightAsync(this Client client, string? name = null, System.Guid? sourceTrajectoryId = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
        var items = new List<TrajectoryExtrapolationCaseLight>();
        int offset = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            TrajectoryExtrapolationCaseDiscoveryPage page = await client.DiscoverTrajectoryExtrapolationCaseAsync(offset: offset, limit: 200, name: name, sourceTrajectoryId: sourceTrajectoryId, cancellationToken: cancellationToken);
            items.AddRange(page.Items);
            if (page.NextOffset is not int next) return items;
            if (next <= offset) throw new InvalidOperationException("Discovery pagination did not advance.");
            offset = next;
        }
    }

    public static Task<List<TrajectoryExtrapolationCaseLight>> ListTrajectoryExtrapolationCaseLightAsync(this Client client, CancellationToken cancellationToken) =>
        client.ListTrajectoryExtrapolationCaseLightAsync(cancellationToken: cancellationToken, name: null);

    /// <summary>Light fields projected into the existing UI reference type. Never submit these partial references as replacements.</summary>
    public static async Task<List<TrajectoryExtrapolationCase>> ListTrajectoryExtrapolationCaseReferencesAsync(this Client client, string? name = null, System.Guid? sourceTrajectoryId = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
        var summaries = await client.ListTrajectoryExtrapolationCaseLightAsync(name: name, sourceTrajectoryId: sourceTrajectoryId, cancellationToken: cancellationToken);
        return summaries.ConvertAll(summary => JsonSerializer.Deserialize<TrajectoryExtrapolationCase>(JsonSerializer.Serialize(summary))
            ?? throw new JsonException("A discovery reference is null."));
    }

    public static Task<List<TrajectoryExtrapolationCase>> ListTrajectoryExtrapolationCaseReferencesAsync(this Client client, CancellationToken cancellationToken) =>
        client.ListTrajectoryExtrapolationCaseReferencesAsync(cancellationToken: cancellationToken, name: null);

    /// <summary>Replace one reference with its complete selected record before editing or reading nested data.</summary>
    public static async Task<TrajectoryExtrapolationCase> LoadTrajectoryExtrapolationCaseDetailsAsync(this Client client, IList<TrajectoryExtrapolationCase> references, Guid id, CancellationToken cancellationToken = default)
    {
        var value = await client.GetTrajectoryExtrapolationCaseByIdAsync(id, cancellationToken: cancellationToken);
        for (int index = 0; index < references.Count; index++)
            if (DiscoveryIdentifier(references[index]) == id) { references[index] = value; return value; }
        references.Add(value);
        return value;
    }

    public static async Task<List<TrajectoryFeatureCategoryLight>> ListTrajectoryFeatureCategoryLightAsync(this Client client, string? name = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
        var items = new List<TrajectoryFeatureCategoryLight>();
        int offset = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            TrajectoryFeatureCategoryDiscoveryPage page = await client.DiscoverTrajectoryFeatureCategoryAsync(offset: offset, limit: 200, name: name, cancellationToken: cancellationToken);
            items.AddRange(page.Items);
            if (page.NextOffset is not int next) return items;
            if (next <= offset) throw new InvalidOperationException("Discovery pagination did not advance.");
            offset = next;
        }
    }

    public static Task<List<TrajectoryFeatureCategoryLight>> ListTrajectoryFeatureCategoryLightAsync(this Client client, CancellationToken cancellationToken) =>
        client.ListTrajectoryFeatureCategoryLightAsync(cancellationToken: cancellationToken, name: null);

    /// <summary>Light fields projected into the existing UI reference type. Never submit these partial references as replacements.</summary>
    public static async Task<List<TrajectoryFeatureCategory>> ListTrajectoryFeatureCategoryReferencesAsync(this Client client, string? name = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
        var summaries = await client.ListTrajectoryFeatureCategoryLightAsync(name: name, cancellationToken: cancellationToken);
        return summaries.ConvertAll(summary => JsonSerializer.Deserialize<TrajectoryFeatureCategory>(JsonSerializer.Serialize(summary))
            ?? throw new JsonException("A discovery reference is null."));
    }

    public static Task<List<TrajectoryFeatureCategory>> ListTrajectoryFeatureCategoryReferencesAsync(this Client client, CancellationToken cancellationToken) =>
        client.ListTrajectoryFeatureCategoryReferencesAsync(cancellationToken: cancellationToken, name: null);

    /// <summary>Replace one reference with its complete selected record before editing or reading nested data.</summary>
    public static async Task<TrajectoryFeatureCategory> LoadTrajectoryFeatureCategoryDetailsAsync(this Client client, IList<TrajectoryFeatureCategory> references, Guid id, CancellationToken cancellationToken = default)
    {
        var value = await client.GetTrajectoryFeatureCategoryByIdAsync(id, cancellationToken: cancellationToken);
        for (int index = 0; index < references.Count; index++)
            if (DiscoveryIdentifier(references[index]) == id) { references[index] = value; return value; }
        references.Add(value);
        return value;
    }

    public static async Task<List<TrajectoryIdentityLight>> ListTrajectoryIdentityLightAsync(this Client client, string? name = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
        var items = new List<TrajectoryIdentityLight>();
        int offset = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            TrajectoryIdentityDiscoveryPage page = await client.DiscoverTrajectoryIdentityAsync(offset: offset, limit: 200, name: name, cancellationToken: cancellationToken);
            items.AddRange(page.Items);
            if (page.NextOffset is not int next) return items;
            if (next <= offset) throw new InvalidOperationException("Discovery pagination did not advance.");
            offset = next;
        }
    }

    public static Task<List<TrajectoryIdentityLight>> ListTrajectoryIdentityLightAsync(this Client client, CancellationToken cancellationToken) =>
        client.ListTrajectoryIdentityLightAsync(cancellationToken: cancellationToken, name: null);

    /// <summary>Light fields projected into the existing UI reference type. Never submit these partial references as replacements.</summary>
    public static async Task<List<TrajectoryIdentity>> ListTrajectoryIdentityReferencesAsync(this Client client, string? name = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
        var summaries = await client.ListTrajectoryIdentityLightAsync(name: name, cancellationToken: cancellationToken);
        return summaries.ConvertAll(summary => JsonSerializer.Deserialize<TrajectoryIdentity>(JsonSerializer.Serialize(summary))
            ?? throw new JsonException("A discovery reference is null."));
    }

    public static Task<List<TrajectoryIdentity>> ListTrajectoryIdentityReferencesAsync(this Client client, CancellationToken cancellationToken) =>
        client.ListTrajectoryIdentityReferencesAsync(cancellationToken: cancellationToken, name: null);

    /// <summary>Replace one reference with its complete selected record before editing or reading nested data.</summary>
    public static async Task<TrajectoryIdentity> LoadTrajectoryIdentityDetailsAsync(this Client client, IList<TrajectoryIdentity> references, Guid id, CancellationToken cancellationToken = default)
    {
        var value = await client.GetTrajectoryIdentityByIdAsync(id, cancellationToken: cancellationToken);
        for (int index = 0; index < references.Count; index++)
            if (DiscoveryIdentifier(references[index]) == id) { references[index] = value; return value; }
        references.Add(value);
        return value;
    }

    public static async Task<List<TrajectoryMinimumDistanceCalculationLight>> ListTrajectoryMinimumDistanceCalculationLightAsync(this Client client, string? name = null, System.Guid? referenceTrajectoryId = null, System.Guid? globalMinimumComparisonTrajectoryId = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
        var items = new List<TrajectoryMinimumDistanceCalculationLight>();
        int offset = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            TrajectoryMinimumDistanceCalculationDiscoveryPage page = await client.DiscoverTrajectoryMinimumDistanceCalculationAsync(offset: offset, limit: 200, name: name, referenceTrajectoryId: referenceTrajectoryId, globalMinimumComparisonTrajectoryId: globalMinimumComparisonTrajectoryId, cancellationToken: cancellationToken);
            items.AddRange(page.Items);
            if (page.NextOffset is not int next) return items;
            if (next <= offset) throw new InvalidOperationException("Discovery pagination did not advance.");
            offset = next;
        }
    }

    public static Task<List<TrajectoryMinimumDistanceCalculationLight>> ListTrajectoryMinimumDistanceCalculationLightAsync(this Client client, CancellationToken cancellationToken) =>
        client.ListTrajectoryMinimumDistanceCalculationLightAsync(cancellationToken: cancellationToken, name: null);

    /// <summary>Light fields projected into the existing UI reference type. Never submit these partial references as replacements.</summary>
    public static async Task<List<TrajectoryMinimumDistanceCalculation>> ListTrajectoryMinimumDistanceCalculationReferencesAsync(this Client client, string? name = null, System.Guid? referenceTrajectoryId = null, System.Guid? globalMinimumComparisonTrajectoryId = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
        var summaries = await client.ListTrajectoryMinimumDistanceCalculationLightAsync(name: name, referenceTrajectoryId: referenceTrajectoryId, globalMinimumComparisonTrajectoryId: globalMinimumComparisonTrajectoryId, cancellationToken: cancellationToken);
        return summaries.ConvertAll(summary => JsonSerializer.Deserialize<TrajectoryMinimumDistanceCalculation>(JsonSerializer.Serialize(summary))
            ?? throw new JsonException("A discovery reference is null."));
    }

    public static Task<List<TrajectoryMinimumDistanceCalculation>> ListTrajectoryMinimumDistanceCalculationReferencesAsync(this Client client, CancellationToken cancellationToken) =>
        client.ListTrajectoryMinimumDistanceCalculationReferencesAsync(cancellationToken: cancellationToken, name: null);

    /// <summary>Replace one reference with its complete selected record before editing or reading nested data.</summary>
    public static async Task<TrajectoryMinimumDistanceCalculation> LoadTrajectoryMinimumDistanceCalculationDetailsAsync(this Client client, IList<TrajectoryMinimumDistanceCalculation> references, Guid id, CancellationToken cancellationToken = default)
    {
        var value = await client.GetTrajectoryMinimumDistanceCalculationByIdAsync(id, cancellationToken: cancellationToken);
        for (int index = 0; index < references.Count; index++)
            if (DiscoveryIdentifier(references[index]) == id) { references[index] = value; return value; }
        references.Add(value);
        return value;
    }

    public static async Task<List<TrajectoryRealizationCaseLight>> ListTrajectoryRealizationCaseLightAsync(this Client client, string? name = null, System.Guid? trajectoryId = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
        var items = new List<TrajectoryRealizationCaseLight>();
        int offset = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            TrajectoryRealizationCaseDiscoveryPage page = await client.DiscoverTrajectoryRealizationCaseAsync(offset: offset, limit: 200, name: name, trajectoryId: trajectoryId, cancellationToken: cancellationToken);
            items.AddRange(page.Items);
            if (page.NextOffset is not int next) return items;
            if (next <= offset) throw new InvalidOperationException("Discovery pagination did not advance.");
            offset = next;
        }
    }

    public static Task<List<TrajectoryRealizationCaseLight>> ListTrajectoryRealizationCaseLightAsync(this Client client, CancellationToken cancellationToken) =>
        client.ListTrajectoryRealizationCaseLightAsync(cancellationToken: cancellationToken, name: null);

    /// <summary>Light fields projected into the existing UI reference type. Never submit these partial references as replacements.</summary>
    public static async Task<List<TrajectoryRealizationCase>> ListTrajectoryRealizationCaseReferencesAsync(this Client client, string? name = null, System.Guid? trajectoryId = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
        var summaries = await client.ListTrajectoryRealizationCaseLightAsync(name: name, trajectoryId: trajectoryId, cancellationToken: cancellationToken);
        return summaries.ConvertAll(summary => JsonSerializer.Deserialize<TrajectoryRealizationCase>(JsonSerializer.Serialize(summary))
            ?? throw new JsonException("A discovery reference is null."));
    }

    public static Task<List<TrajectoryRealizationCase>> ListTrajectoryRealizationCaseReferencesAsync(this Client client, CancellationToken cancellationToken) =>
        client.ListTrajectoryRealizationCaseReferencesAsync(cancellationToken: cancellationToken, name: null);

    /// <summary>Replace one reference with its complete selected record before editing or reading nested data.</summary>
    public static async Task<TrajectoryRealizationCase> LoadTrajectoryRealizationCaseDetailsAsync(this Client client, IList<TrajectoryRealizationCase> references, Guid id, CancellationToken cancellationToken = default)
    {
        var value = await client.GetTrajectoryRealizationCaseByIdAsync(id, cancellationToken: cancellationToken);
        for (int index = 0; index < references.Count; index++)
            if (DiscoveryIdentifier(references[index]) == id) { references[index] = value; return value; }
        references.Add(value);
        return value;
    }

    public static async Task<List<WellLight>> ListWellLightAsync(this Client client, string? name = null, System.Guid? slotId = null, System.Guid? clusterId = null, bool? isSingleWell = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
        var items = new List<WellLight>();
        int offset = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            WellDiscoveryPage page = await client.DiscoverWellAsync(offset: offset, limit: 200, name: name, slotId: slotId, clusterId: clusterId, isSingleWell: isSingleWell, cancellationToken: cancellationToken);
            items.AddRange(page.Items);
            if (page.NextOffset is not int next) return items;
            if (next <= offset) throw new InvalidOperationException("Discovery pagination did not advance.");
            offset = next;
        }
    }

    public static Task<List<WellLight>> ListWellLightAsync(this Client client, CancellationToken cancellationToken) =>
        client.ListWellLightAsync(cancellationToken: cancellationToken, name: null);

    /// <summary>Light fields projected into the existing UI reference type. Never submit these partial references as replacements.</summary>
    public static async Task<List<Well>> ListWellReferencesAsync(this Client client, string? name = null, System.Guid? slotId = null, System.Guid? clusterId = null, bool? isSingleWell = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
        var summaries = await client.ListWellLightAsync(name: name, slotId: slotId, clusterId: clusterId, isSingleWell: isSingleWell, cancellationToken: cancellationToken);
        return summaries.ConvertAll(summary => JsonSerializer.Deserialize<Well>(JsonSerializer.Serialize(summary))
            ?? throw new JsonException("A discovery reference is null."));
    }

    public static Task<List<Well>> ListWellReferencesAsync(this Client client, CancellationToken cancellationToken) =>
        client.ListWellReferencesAsync(cancellationToken: cancellationToken, name: null);

    /// <summary>Replace one reference with its complete selected record before editing or reading nested data.</summary>
    public static async Task<Well> LoadWellDetailsAsync(this Client client, IList<Well> references, Guid id, CancellationToken cancellationToken = default)
    {
        var value = await client.GetWellByIdAsync(id, cancellationToken: cancellationToken);
        for (int index = 0; index < references.Count; index++)
            if (DiscoveryIdentifier(references[index]) == id) { references[index] = value; return value; }
        references.Add(value);
        return value;
    }

    public static async Task<List<WellFeatureCategoryLight>> ListWellFeatureCategoryLightAsync(this Client client, string? name = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
        var items = new List<WellFeatureCategoryLight>();
        int offset = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            WellFeatureCategoryDiscoveryPage page = await client.DiscoverWellFeatureCategoryAsync(offset: offset, limit: 200, name: name, cancellationToken: cancellationToken);
            items.AddRange(page.Items);
            if (page.NextOffset is not int next) return items;
            if (next <= offset) throw new InvalidOperationException("Discovery pagination did not advance.");
            offset = next;
        }
    }

    public static Task<List<WellFeatureCategoryLight>> ListWellFeatureCategoryLightAsync(this Client client, CancellationToken cancellationToken) =>
        client.ListWellFeatureCategoryLightAsync(cancellationToken: cancellationToken, name: null);

    /// <summary>Light fields projected into the existing UI reference type. Never submit these partial references as replacements.</summary>
    public static async Task<List<WellFeatureCategory>> ListWellFeatureCategoryReferencesAsync(this Client client, string? name = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
        var summaries = await client.ListWellFeatureCategoryLightAsync(name: name, cancellationToken: cancellationToken);
        return summaries.ConvertAll(summary => JsonSerializer.Deserialize<WellFeatureCategory>(JsonSerializer.Serialize(summary))
            ?? throw new JsonException("A discovery reference is null."));
    }

    public static Task<List<WellFeatureCategory>> ListWellFeatureCategoryReferencesAsync(this Client client, CancellationToken cancellationToken) =>
        client.ListWellFeatureCategoryReferencesAsync(cancellationToken: cancellationToken, name: null);

    /// <summary>Replace one reference with its complete selected record before editing or reading nested data.</summary>
    public static async Task<WellFeatureCategory> LoadWellFeatureCategoryDetailsAsync(this Client client, IList<WellFeatureCategory> references, Guid id, CancellationToken cancellationToken = default)
    {
        var value = await client.GetWellFeatureCategoryByIdAsync(id, cancellationToken: cancellationToken);
        for (int index = 0; index < references.Count; index++)
            if (DiscoveryIdentifier(references[index]) == id) { references[index] = value; return value; }
        references.Add(value);
        return value;
    }

    public static async Task<List<WellIdentityLight>> ListWellIdentityLightAsync(this Client client, string? name = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
        var items = new List<WellIdentityLight>();
        int offset = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            WellIdentityDiscoveryPage page = await client.DiscoverWellIdentityAsync(offset: offset, limit: 200, name: name, cancellationToken: cancellationToken);
            items.AddRange(page.Items);
            if (page.NextOffset is not int next) return items;
            if (next <= offset) throw new InvalidOperationException("Discovery pagination did not advance.");
            offset = next;
        }
    }

    public static Task<List<WellIdentityLight>> ListWellIdentityLightAsync(this Client client, CancellationToken cancellationToken) =>
        client.ListWellIdentityLightAsync(cancellationToken: cancellationToken, name: null);

    /// <summary>Light fields projected into the existing UI reference type. Never submit these partial references as replacements.</summary>
    public static async Task<List<WellIdentity>> ListWellIdentityReferencesAsync(this Client client, string? name = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
        var summaries = await client.ListWellIdentityLightAsync(name: name, cancellationToken: cancellationToken);
        return summaries.ConvertAll(summary => JsonSerializer.Deserialize<WellIdentity>(JsonSerializer.Serialize(summary))
            ?? throw new JsonException("A discovery reference is null."));
    }

    public static Task<List<WellIdentity>> ListWellIdentityReferencesAsync(this Client client, CancellationToken cancellationToken) =>
        client.ListWellIdentityReferencesAsync(cancellationToken: cancellationToken, name: null);

    /// <summary>Replace one reference with its complete selected record before editing or reading nested data.</summary>
    public static async Task<WellIdentity> LoadWellIdentityDetailsAsync(this Client client, IList<WellIdentity> references, Guid id, CancellationToken cancellationToken = default)
    {
        var value = await client.GetWellIdentityByIdAsync(id, cancellationToken: cancellationToken);
        for (int index = 0; index < references.Count; index++)
            if (DiscoveryIdentifier(references[index]) == id) { references[index] = value; return value; }
        references.Add(value);
        return value;
    }

    public static async Task<List<WellBoreLight>> ListWellBoreLightAsync(this Client client, string? name = null, System.Guid? wellId = null, System.Guid? rigId = null, System.Guid? parentWellBoreId = null, bool? isSidetrack = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
        var items = new List<WellBoreLight>();
        int offset = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            WellBoreDiscoveryPage page = await client.DiscoverWellBoreAsync(offset: offset, limit: 200, name: name, wellId: wellId, rigId: rigId, parentWellBoreId: parentWellBoreId, isSidetrack: isSidetrack, cancellationToken: cancellationToken);
            items.AddRange(page.Items);
            if (page.NextOffset is not int next) return items;
            if (next <= offset) throw new InvalidOperationException("Discovery pagination did not advance.");
            offset = next;
        }
    }

    public static Task<List<WellBoreLight>> ListWellBoreLightAsync(this Client client, CancellationToken cancellationToken) =>
        client.ListWellBoreLightAsync(cancellationToken: cancellationToken, name: null);

    /// <summary>Light fields projected into the existing UI reference type. Never submit these partial references as replacements.</summary>
    public static async Task<List<WellBore>> ListWellBoreReferencesAsync(this Client client, string? name = null, System.Guid? wellId = null, System.Guid? rigId = null, System.Guid? parentWellBoreId = null, bool? isSidetrack = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
        var summaries = await client.ListWellBoreLightAsync(name: name, wellId: wellId, rigId: rigId, parentWellBoreId: parentWellBoreId, isSidetrack: isSidetrack, cancellationToken: cancellationToken);
        return summaries.ConvertAll(summary => JsonSerializer.Deserialize<WellBore>(JsonSerializer.Serialize(summary))
            ?? throw new JsonException("A discovery reference is null."));
    }

    public static Task<List<WellBore>> ListWellBoreReferencesAsync(this Client client, CancellationToken cancellationToken) =>
        client.ListWellBoreReferencesAsync(cancellationToken: cancellationToken, name: null);

    /// <summary>Replace one reference with its complete selected record before editing or reading nested data.</summary>
    public static async Task<WellBore> LoadWellBoreDetailsAsync(this Client client, IList<WellBore> references, Guid id, CancellationToken cancellationToken = default)
    {
        var value = await client.GetWellBoreByIdAsync(id, cancellationToken: cancellationToken);
        for (int index = 0; index < references.Count; index++)
            if (DiscoveryIdentifier(references[index]) == id) { references[index] = value; return value; }
        references.Add(value);
        return value;
    }

    public static async Task<List<WellBoreFeatureCategoryLight>> ListWellBoreFeatureCategoryLightAsync(this Client client, string? name = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
        var items = new List<WellBoreFeatureCategoryLight>();
        int offset = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            WellBoreFeatureCategoryDiscoveryPage page = await client.DiscoverWellBoreFeatureCategoryAsync(offset: offset, limit: 200, name: name, cancellationToken: cancellationToken);
            items.AddRange(page.Items);
            if (page.NextOffset is not int next) return items;
            if (next <= offset) throw new InvalidOperationException("Discovery pagination did not advance.");
            offset = next;
        }
    }

    public static Task<List<WellBoreFeatureCategoryLight>> ListWellBoreFeatureCategoryLightAsync(this Client client, CancellationToken cancellationToken) =>
        client.ListWellBoreFeatureCategoryLightAsync(cancellationToken: cancellationToken, name: null);

    /// <summary>Light fields projected into the existing UI reference type. Never submit these partial references as replacements.</summary>
    public static async Task<List<WellBoreFeatureCategory>> ListWellBoreFeatureCategoryReferencesAsync(this Client client, string? name = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
        var summaries = await client.ListWellBoreFeatureCategoryLightAsync(name: name, cancellationToken: cancellationToken);
        return summaries.ConvertAll(summary => JsonSerializer.Deserialize<WellBoreFeatureCategory>(JsonSerializer.Serialize(summary))
            ?? throw new JsonException("A discovery reference is null."));
    }

    public static Task<List<WellBoreFeatureCategory>> ListWellBoreFeatureCategoryReferencesAsync(this Client client, CancellationToken cancellationToken) =>
        client.ListWellBoreFeatureCategoryReferencesAsync(cancellationToken: cancellationToken, name: null);

    /// <summary>Replace one reference with its complete selected record before editing or reading nested data.</summary>
    public static async Task<WellBoreFeatureCategory> LoadWellBoreFeatureCategoryDetailsAsync(this Client client, IList<WellBoreFeatureCategory> references, Guid id, CancellationToken cancellationToken = default)
    {
        var value = await client.GetWellBoreFeatureCategoryByIdAsync(id, cancellationToken: cancellationToken);
        for (int index = 0; index < references.Count; index++)
            if (DiscoveryIdentifier(references[index]) == id) { references[index] = value; return value; }
        references.Add(value);
        return value;
    }

    public static async Task<List<WellBoreIdentityLight>> ListWellBoreIdentityLightAsync(this Client client, string? name = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
        var items = new List<WellBoreIdentityLight>();
        int offset = 0;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            WellBoreIdentityDiscoveryPage page = await client.DiscoverWellBoreIdentityAsync(offset: offset, limit: 200, name: name, cancellationToken: cancellationToken);
            items.AddRange(page.Items);
            if (page.NextOffset is not int next) return items;
            if (next <= offset) throw new InvalidOperationException("Discovery pagination did not advance.");
            offset = next;
        }
    }

    public static Task<List<WellBoreIdentityLight>> ListWellBoreIdentityLightAsync(this Client client, CancellationToken cancellationToken) =>
        client.ListWellBoreIdentityLightAsync(cancellationToken: cancellationToken, name: null);

    /// <summary>Light fields projected into the existing UI reference type. Never submit these partial references as replacements.</summary>
    public static async Task<List<WellBoreIdentity>> ListWellBoreIdentityReferencesAsync(this Client client, string? name = null, System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken))
    {
        var summaries = await client.ListWellBoreIdentityLightAsync(name: name, cancellationToken: cancellationToken);
        return summaries.ConvertAll(summary => JsonSerializer.Deserialize<WellBoreIdentity>(JsonSerializer.Serialize(summary))
            ?? throw new JsonException("A discovery reference is null."));
    }

    public static Task<List<WellBoreIdentity>> ListWellBoreIdentityReferencesAsync(this Client client, CancellationToken cancellationToken) =>
        client.ListWellBoreIdentityReferencesAsync(cancellationToken: cancellationToken, name: null);

    /// <summary>Replace one reference with its complete selected record before editing or reading nested data.</summary>
    public static async Task<WellBoreIdentity> LoadWellBoreIdentityDetailsAsync(this Client client, IList<WellBoreIdentity> references, Guid id, CancellationToken cancellationToken = default)
    {
        var value = await client.GetWellBoreIdentityByIdAsync(id, cancellationToken: cancellationToken);
        for (int index = 0; index < references.Count; index++)
            if (DiscoveryIdentifier(references[index]) == id) { references[index] = value; return value; }
        references.Add(value);
        return value;
    }

    private static Guid? DiscoveryIdentifier(object value)
    {
        object metadata = value.GetType().GetProperty("MetaInfo")?.GetValue(value);
        return (metadata?.GetType().GetProperty("ID")?.GetValue(metadata) ?? value.GetType().GetProperty("ID")?.GetValue(value)) as Guid?;
    }
}
