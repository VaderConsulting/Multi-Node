using System.Text.Json.Serialization;

namespace MultiNodeNetwork.Core.Configuration;
public class TimingSettings
{
    #region Properties

    [JsonPropertyName("discoveryInterval")]
    public int DiscoveryInterval { get; set; } = 30; // seconds

    [JsonPropertyName("electionTimeout")]
    public int ElectionTimeout { get; set; } = 15; // seconds

    [JsonPropertyName("heartbeatInterval")]
    public int HeartbeatInterval { get; set; } = 10; // seconds

    [JsonPropertyName("ipAddressCheckInterval")]
    public int IpAddressCheckInterval { get; set; } = 60; // seconds

    [JsonPropertyName("masterBroadcastInterval")]
    public int MasterBroadcastInterval { get; set; } = 30; // seconds

    [JsonPropertyName("masterTimeout")]
    public int MasterTimeout { get; set; } = 90; // seconds

    [JsonPropertyName("maxRetries")]
    public int MaxRetries { get; set; } = 3;

    [JsonPropertyName("nodeTimeout")]
    public int NodeTimeout { get; set; } = 60; // seconds

    [JsonPropertyName("retryInterval")]
    public int RetryInterval { get; set; } = 5; // seconds

    #endregion
}
