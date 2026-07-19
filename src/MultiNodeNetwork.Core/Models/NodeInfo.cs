using MultiNodeNetwork.Common.Models;
using System.Text.Json.Serialization;

namespace MultiNodeNetwork.Core.Models;
/// <summary>
/// Node information
/// </summary>
public class NodeInfo
{
    #region Properties

    /// <summary>
    /// The battery level of the node (percentage)
    /// </summary>
    [JsonPropertyName("batteryLevel")]
    public int BatteryLevel { get; set; }

    /// <summary>
    /// List of capabilities supported by the node
    /// </summary>
    [JsonPropertyName("capabilities")]
    public List<string> Capabilities { get; set; } = [];

    /// <summary>
    /// The unique string identifier for the node
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// The name of the network interface used by the node
    /// </summary>
    [JsonPropertyName("interfaceName")]
    public string InterfaceName { get; set; } = string.Empty;

    /// <summary>
    /// The IP address of the node
    /// </summary>
    [JsonPropertyName("ipAddress")]
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// The last time the node was seen active
    /// </summary>
    [JsonPropertyName("lastSeen")]
    public DateTime LastSeen { get; set; }

    /// <summary>
    /// The physical location of the node (optional)
    /// </summary>
    [JsonPropertyName("location")]
    public Location? Location { get; set; }

    /// <summary>
    /// The MAC address of the node
    /// </summary>
    [JsonPropertyName("macAddress")]
    public string MacAddress { get; set; } = string.Empty;

    /// <summary>
    /// The display name of the node
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The byte node ID
    /// </summary>
    [JsonPropertyName("nodeId")]
    public byte NodeId { get; set; }

    /// <summary>
    /// The signal strength of the node in dBm
    /// </summary>
    [JsonPropertyName("signalStrength")]
    public int SignalStrength { get; set; }

    /// <summary>
    /// The current status of the node
    /// </summary>
    [JsonPropertyName("status")]
    public NodeStatus Status { get; set; } = NodeStatus.Offline;

    /// <summary>
    /// The type of the node (master, repeater, etc.)
    /// </summary>
    [JsonPropertyName("type")]
    public NodeType Type { get; set; } = NodeType.Repeater;

    /// <summary>
    /// The software version running on the node
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0.0";

    #endregion
}
