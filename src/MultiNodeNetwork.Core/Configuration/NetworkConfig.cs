using System.Text.Json.Serialization;

namespace MultiNodeNetwork.Core.Configuration;
/// <summary>
/// Network configuration settings for the 100-node WiFi network
/// </summary>
public class NetworkConfig
{
    #region Properties

    [JsonPropertyName("logging")]
    public LoggingSettings Logging { get; set; } = new();

    [JsonPropertyName("network")]
    public NetworkSettings Network { get; set; } = new();

    [JsonPropertyName("performance")]
    public PerformanceSettings Performance { get; set; } = new();

    [JsonPropertyName("routing")]
    public RoutingSettings Routing { get; set; } = new();

    [JsonPropertyName("security")]
    public SecuritySettings Security { get; set; } = new();

    [JsonPropertyName("timing")]
    public TimingSettings Timing { get; set; } = new();

    #endregion
}
