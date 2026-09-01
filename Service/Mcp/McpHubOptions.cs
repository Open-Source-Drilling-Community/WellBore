namespace OSDC.Drilling.WellBore.Service.Mcp;

public sealed class McpHubOptions
{
    public const string SectionName = "McpHub";
    public bool Enabled { get; set; }
    public string? HubBaseUrl { get; set; }
    public string RegistrationEndpoint { get; set; } = "McpMicroservice";
    public int RetryIntervalSeconds { get; set; } = 60;
    public string? PublicBaseUrl { get; set; }
    public string ServiceName { get; set; } = "WellBore";
    public string? InstanceId { get; set; }
    public bool UnregisterOnShutdown { get; set; } = true;
}
