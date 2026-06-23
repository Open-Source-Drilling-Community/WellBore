using NORCE.Drilling.WellBore.ModelShared;
using ModelShared = NORCE.Drilling.WellBore.ModelShared;

namespace NORCE.Drilling.WellBore.WebPages;

public static class DepthReferenceUtils
{
    public static async Task<double?> ResolveMeanSeaLevelDepthReferenceAsync(
        IWellBoreAPIUtils api,
        ModelShared.WellBore? wellBore,
        IEnumerable<ModelShared.WellBore>? wellBores,
        IEnumerable<ModelShared.Well>? wells,
        IEnumerable<ModelShared.Cluster>? clusters)
    {
        Slot? slot = ResolveRootSlot(wellBore, wellBores, wells, clusters);
        double? latitude = slot?.Latitude?.GaussianValue?.Mean;
        double? longitude = slot?.Longitude?.GaussianValue?.Mean;
        if (latitude == null || longitude == null)
        {
            return null;
        }

        Guid orderId = Guid.NewGuid();
        VerticalDatumOrder order = new()
        {
            MetaInfo = new ModelShared.MetaInfo()
            {
                ID = orderId,
                HttpHostName = api.HostNameVerticalDatum,
                HttpHostBasePath = api.HostBasePathVerticalDatum,
                HttpEndPoint = "VerticalDatumOrder/"
            },
            Name = $"MSL reference at wellbore slot {orderId}",
            Description = "Temporary MSL-to-WGS84 conversion for WellBore depth reference.",
            CreationDate = DateTimeOffset.UtcNow,
            LastModificationDate = DateTimeOffset.UtcNow,
            VerticalDatum = new VerticalDatum()
            {
                MetaInfo = new ModelShared.MetaInfo()
                {
                    ID = Guid.NewGuid(),
                    HttpHostName = api.HostNameVerticalDatum,
                    HttpHostBasePath = api.HostBasePathVerticalDatum,
                    HttpEndPoint = "VerticalDatum/"
                },
                Name = $"MSL reference {orderId}",
                Description = "Temporary MSL-to-WGS84 conversion.",
                CreationDate = DateTimeOffset.UtcNow,
                LastModificationDate = DateTimeOffset.UtcNow,
                ConversionFrom = VerticalDatumConversion.FromMeanSeaLevel,
                Type = VerticalDatumType.Raw,
                DatumSet =
                [
                    new VerticalDatumSet()
                    {
                        Latitude = latitude.Value,
                        Longitude = longitude.Value,
                        GenericVerticalDatum = 0
                    }
                ]
            }
        };

        try
        {
            await api.ClientVerticalDatum.PostVerticalDatumOrderAsync(order);
            VerticalDatumOrder calculatedOrder = await api.ClientVerticalDatum.GetVerticalDatumOrderByIdAsync(orderId);
            double? meanSeaLevelInWgs84 = calculatedOrder.VerticalDatum?.DatumSet?.FirstOrDefault()?.VerticalDatumWGS64;
            return meanSeaLevelInWgs84 == null ? null : -meanSeaLevelInWgs84;
        }
        finally
        {
            try
            {
                await api.ClientVerticalDatum.DeleteVerticalDatumOrderByIdAsync(orderId);
            }
            catch
            {
                // Best-effort cleanup of a temporary calculation order.
            }
        }
    }

    private static Slot? ResolveRootSlot(
        ModelShared.WellBore? wellBore,
        IEnumerable<ModelShared.WellBore>? wellBores,
        IEnumerable<ModelShared.Well>? wells,
        IEnumerable<ModelShared.Cluster>? clusters)
    {
        ModelShared.WellBore? rootWellBore = ResolveRootWellBore(wellBore, wellBores);
        if (rootWellBore?.WellID is not Guid wellId || wells == null)
        {
            return null;
        }

        Well? well = wells.FirstOrDefault(item => item?.MetaInfo?.ID == wellId);
        if (well?.SlotID is not Guid slotId || clusters == null)
        {
            return null;
        }

        Cluster? cluster = null;
        if (well.ClusterID is Guid clusterId)
        {
            cluster = clusters.FirstOrDefault(item => item?.MetaInfo?.ID == clusterId);
        }

        cluster ??= clusters.FirstOrDefault(item => item?.Slots?.Values.Any(slot => slot?.ID == slotId) == true);
        return cluster?.Slots?.Values.FirstOrDefault(slot => slot?.ID == slotId);
    }

    private static ModelShared.WellBore? ResolveRootWellBore(ModelShared.WellBore? wellBore, IEnumerable<ModelShared.WellBore>? wellBores)
    {
        ModelShared.WellBore? current = wellBore;
        HashSet<Guid> visited = [];
        while (current?.IsSidetrack == true &&
               current.ParentWellBoreID is Guid parentId &&
               parentId != Guid.Empty &&
               wellBores != null &&
               visited.Add(parentId))
        {
            ModelShared.WellBore? parent = wellBores.FirstOrDefault(item => item?.MetaInfo?.ID == parentId);
            if (parent == null)
            {
                break;
            }

            current = parent;
        }

        return current;
    }
}
