using System.Text.Json.Serialization;

namespace MultiNodeNetwork.Core.Models;
/// <summary>
/// Network topology map
/// </summary>
public class TopologyMap
{
    #region Properties

    /// <summary>
    /// List of connections between nodes in the topology
    /// </summary>
    [JsonPropertyName("connections")]
    public List<Connection> Connections { get; set; } = [];

    /// <summary>
    /// Dictionary of nodes in the topology, keyed by node ID
    /// </summary>
    [JsonPropertyName("nodes")]
    public Dictionary<byte, NodeInfo> Nodes { get; set; } = [];

    /// <summary>
    /// The timestamp when the topology map was last updated
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The version number of the topology map
    /// </summary>
    [JsonPropertyName("version")]
    public int Version { get; set; } = 0;

    #endregion
}
