using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
namespace OSDC.Drilling.WellBore.ModelShared;
public static class SlotDiscoveryClientExtensions
{
    /// <summary>Loads only light slot location fields. These are read-only references, not replacement slot documents.</summary>
    public static async Task LoadClusterSlotReferencesAsync(this Client client, Cluster cluster, CancellationToken cancellationToken = default)
    {
        if (cluster.Slots != null) return;
        var slots = await client.ListClusterSlotsLightAsync(cluster.MetaInfo.ID, cancellationToken: cancellationToken);
        cluster.Slots = slots.Select(value => JsonSerializer.Deserialize<Slot>(JsonSerializer.Serialize(value))!).ToDictionary(value => value.ID.ToString());
    }
}
