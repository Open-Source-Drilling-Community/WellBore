using Microsoft.Extensions.Logging;
using OSDC.Drilling.WellBore.ModelShared;

namespace OSDC.Drilling.WellBore.WebPages;

internal static class CartographicPositionReferenceUtils
{
    public static async Task ApplyAsync(IWellBoreAPIUtils api, Field? field, ILogger logger)
    {
        DataUtils.CartographicGridPositionReferenceSource source =
            DataUtils.UnitAndReferenceParameters.CartographicGridPositionReferenceSource;
        source.CartographicGridNorthPositionReference = null;
        source.CartographicGridEastPositionReference = null;

        if (field?.MetaInfo?.ID is not Guid fieldId || fieldId == Guid.Empty ||
            field.ReferencePoint?.Latitude is not double latitude ||
            field.ReferencePoint.Longitude is not double longitude ||
            field.ReferencePoint.RiemannianNorth is not double riemannianNorth ||
            field.ReferencePoint.RiemannianEast is not double riemannianEast)
        {
            ResetUnavailableSelection();
            return;
        }

        try
        {
            FieldCoordinateConversionResponse response = await api.ClientField.ForwardFieldCoordinatesAsync(new FieldForwardConversionRequest
            {
                FieldID = fieldId,
                SourceGeographicReference = FieldGeographicReference.Wgs84,
                ProjectionApplicabilityPolicy = FieldApplicabilityPolicy.AllowUnknown,
                Transformation = new FieldTransformationOptions
                {
                    SelectionPolicy = FieldTransformationSelectionPolicy.FirstAvailable,
                    ApplicabilityPolicy = FieldApplicabilityPolicy.AllowUnknown,
                    DepthPolicy = FieldDepthTransformationPolicy.AllowUntransformedDepthFor2D
                },
                Positions =
                [
                    new FieldForwardConversionPosition
                    {
                        Latitude = latitude,
                        Longitude = longitude,
                        VerticalDepth = field.ReferencePoint.TVD ?? 0
                    }
                ]
            });
            FieldCoordinateConversionPositionResult? result = response.Positions?.FirstOrDefault();
            if (result?.ProjectedCoordinate == null)
            {
                ResetUnavailableSelection();
                return;
            }

            source.CartographicGridNorthPositionReference = result.ProjectedCoordinate.Northing - riemannianNorth;
            source.CartographicGridEastPositionReference = result.ProjectedCoordinate.Easting - riemannianEast;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Unable to resolve the cartographic position reference for field {FieldId}", fieldId);
            ResetUnavailableSelection();
        }
    }

    private static void ResetUnavailableSelection()
    {
        if (string.Equals(DataUtils.UnitAndReferenceParameters.PositionReferenceName, "Cartographic", StringComparison.Ordinal))
        {
            DataUtils.UnitAndReferenceParameters.PositionReferenceName = "WGS84";
        }
    }
}
