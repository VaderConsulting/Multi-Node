using System.Text.Json.Serialization;

namespace MultiNodeNetwork.Core.Configuration;
public class RoutingSettings
{
    #region Properties

    [JsonPropertyName("backupRoutes")]
    public int BackupRoutes { get; set; } = 2;

    [JsonPropertyName("dynamicRouting")]
    public bool DynamicRouting { get; set; } = true;

    [JsonPropertyName("loadBalance")]
    public bool LoadBalance { get; set; } = true;

    [JsonPropertyName("maxHops")]
    public int MaxHops { get; set; } = 5;

    [JsonPropertyName("routeTimeout")]
    public int RouteTimeout { get; set; } = 30; // seconds

    #endregion
}
