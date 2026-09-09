using OSDC.Drilling.WellBore.ModelShared;
using ModelShared = OSDC.Drilling.WellBore.ModelShared;

namespace OSDC.Drilling.WellBore.WebPages;

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

        MeanSeaLevelToWgs84Request request = new()
        {
            Positions =
            [
                new EarthVerticalDatumPosition
                {
                    Latitude = latitude.Value,
                    Longitude = longitude.Value,
                    MeanSeaLevelDepth = 0
                }
            ]
        };
        MeanSeaLevelToWgs84Response response =
            await api.ClientVerticalDatum.ConvertMeanSeaLevelToWgs84Async(request);
        return -response.Samples?.FirstOrDefault()?.Wgs84EllipsoidalDepth;
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
