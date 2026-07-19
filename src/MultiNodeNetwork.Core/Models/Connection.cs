using System.Text.Json.Serialization;

namespace MultiNodeNetwork.Core.Models;
/// <summary>
/// Connection between nodes
/// </summary>
public class Connection
{
    #region Properties

    /// <summary>
    /// The node ID of the source node in the connection
    /// </summary>
    [JsonPropertyName("fromNode")]
    public byte FromNode { get; set; } = 0;

    /// <summary>
    /// The last time this connection was seen active
    /// </summary>
    [JsonPropertyName("lastSeen")]
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The quality of the connection (e.g., "GOOD", "POOR", "UNKNOWN")
    /// </summary>
    [JsonPropertyName("quality")]
    public string Quality { get; set; } = "UNKNOWN";

    /// <summary>
    /// The signal strength of the connection in dBm
    /// </summary>
    [JsonPropertyName("signalStrength")]
    public int SignalStrength { get; set; } = 0;

    /// <summary>
    /// The node ID of the destination node in the connection
    /// </summary>
    [JsonPropertyName("toNode")]
    public byte ToNode { get; set; } = 0;

    #endregion
}
