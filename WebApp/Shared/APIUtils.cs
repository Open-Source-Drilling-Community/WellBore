using OSDC.UnitConversion.DrillingRazorMudComponents;

public static class APIUtils
{
    // API parameters
    public static readonly string HostNameWellBore = NORCE.Drilling.WellBore.WebApp.Configuration.WellBoreHostURL!;
    public static readonly string HostBasePathWellBore = "WellBore/api/";
    public static readonly HttpClient HttpClientWellBore = APIUtils.SetHttpClient(HostNameWellBore, HostBasePathWellBore);
    public static readonly NORCE.Drilling.WellBore.ModelShared.Client ClientWellBore = new NORCE.Drilling.WellBore.ModelShared.Client(APIUtils.HttpClientWellBore.BaseAddress!.ToString(), APIUtils.HttpClientWellBore);

    public static readonly string HostNameWell = NORCE.Drilling.WellBore.WebApp.Configuration.WellHostURL!;
    public static readonly string HostBasePathWell = "Well/api/";
    public static readonly HttpClient HttpClientWell = APIUtils.SetHttpClient(HostNameWell, HostBasePathWell);
    public static readonly NORCE.Drilling.WellBore.ModelShared.Client ClientWell = new NORCE.Drilling.WellBore.ModelShared.Client(APIUtils.HttpClientWell.BaseAddress!.ToString(), APIUtils.HttpClientWell);

    public static readonly string HostNameCluster = NORCE.Drilling.WellBore.WebApp.Configuration.ClusterHostURL!;
    public static readonly string HostBasePathCluster = "Cluster/api/";
    public static readonly HttpClient HttpClientCluster = APIUtils.SetHttpClient(HostNameCluster, HostBasePathCluster);
    public static readonly NORCE.Drilling.WellBore.ModelShared.Client ClientCluster = new NORCE.Drilling.WellBore.ModelShared.Client(APIUtils.HttpClientCluster.BaseAddress!.ToString(), APIUtils.HttpClientCluster);

    public static readonly string HostNameField = NORCE.Drilling.WellBore.WebApp.Configuration.FieldHostURL!;
    public static readonly string HostBasePathField = "Field/api/";
    public static readonly HttpClient HttpClientField = APIUtils.SetHttpClient(HostNameField, HostBasePathField);
    public static readonly NORCE.Drilling.WellBore.ModelShared.Client ClientField = new NORCE.Drilling.WellBore.ModelShared.Client(APIUtils.HttpClientField.BaseAddress!.ToString(), APIUtils.HttpClientField);

    public static readonly string HostNameUnitConversion = NORCE.Drilling.WellBore.WebApp.Configuration.UnitConversionHostURL!;
    public static readonly string HostBasePathUnitConversion = "UnitConversion/api/";

    // API utility methods
    public static HttpClient SetHttpClient(string host, string microServiceUri)
    {
        var handler = new HttpClientHandler();
        handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => { return true; }; // temporary workaround for testing purposes: bypass certificate validation (not recommended for production environments due to security risks)
        HttpClient httpClient = new(handler)
        {
            BaseAddress = new Uri(host + microServiceUri)
        };
        httpClient.DefaultRequestHeaders.Accept.Clear();
        httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        return httpClient;
    }
}
public class GroundMudLineDepthReferenceSource : IGroundMudLineDepthReferenceSource
{
    public double? GroundMudLineDepthReference { get; set; }
}

public class RotaryTableDepthReferenceSource : IRotaryTableDepthReferenceSource
{
    public double? RotaryTableDepthReference { get; set; }
}

public class SeaWaterLevelDepthReferenceSource : ISeaWaterLevelDepthReferenceSource
{
    public double? SeaWaterLevelDepthReference { get; set; }
}

public class WellHeadDepthReferenceSource : IWellHeadDepthReferenceSource
{
    public double? WellHeadDepthReference { get; set; }
}