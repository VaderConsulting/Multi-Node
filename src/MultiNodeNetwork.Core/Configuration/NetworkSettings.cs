using System.Text.Json.Serialization;

namespace MultiNodeNetwork.Core.Configuration;
public class NetworkSettings
{
    #region Properties

    [JsonPropertyName("broadcastPort")]
    public int BroadcastPort { get; set; } = 5000;

    [JsonPropertyName("coverageArea")]
    public int CoverageArea { get; set; } = 1000; // meters

    [JsonPropertyName("gridSize")]
    public int GridSize { get; set; } = 10; // 10x10 grid

    [JsonPropertyName("maxNodes")]
    public int MaxNodes { get; set; } = 100;

    [JsonPropertyName("password")]
    public string Password { get; set; } = "MultiNode2024!";

    [JsonPropertyName("repeaterSpacing")]
    public int RepeaterSpacing { get; set; } = 100; // meters

    [JsonPropertyName("ssid")]
    public string Ssid { get; set; } = "MultiNode_Network";

    [JsonPropertyName("tcpPort")]
    public int TcpPort { get; set; } = 5001;

    #endregion
}
