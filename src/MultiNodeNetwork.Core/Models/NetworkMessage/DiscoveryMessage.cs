using MultiNodeNetwork.Common.Models;
using System.Text.Json.Serialization;

namespace MultiNodeNetwork.Core.Models;
/// <summary>
/// Message sent by nodes to discover the network
/// </summary>
public class DiscoveryMessage : NetworkMessage
{
    #region Properties

    /// <summary>
    /// The battery level of the discovering node (percentage)
    /// </summary>
    [JsonPropertyName("batteryLevel")]
    public int BatteryLevel { get; set; } = 100;

    /// <summary>
    /// Capabilities supported by the discovering node
    /// </summary>
    [JsonPropertyName("capabilities")]
    public Dictionary<string, object> Capabilities { get; set; } = [];

    /// <summary>
    /// The name of the network interface used by the node
    /// </summary>
    [JsonPropertyName("interfaceName")]
    public string InterfaceName { get; set; } = string.Empty;

    /// <summary>
    /// The IP address of the discovering node
    /// </summary>
    [JsonPropertyName("ipAddress")]
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// The physical location of the node (optional)
    /// </summary>
    [JsonPropertyName("location")]
    public Location? Location { get; set; }

    /// <summary>
    /// The MAC address of the discovering node
    /// </summary>
    [JsonPropertyName("macAddress")]
    public string MacAddress { get; set; } = string.Empty;

    /// <summary>
    /// The type of the discovering node (master, repeater, etc.)
    /// </summary>
    [JsonPropertyName("nodeType")]
    public NodeType NodeType { get; set; } = NodeType.Unknown;

    /// <summary>
    /// The signal strength of the node in dBm
    /// </summary>
    [JsonPropertyName("signalStrength")]
    public int SignalStrength { get; set; } = 0;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the DiscoveryMessage class
    /// </summary>
    public DiscoveryMessage()
    {
        MessageType = GetMessageType();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Gets the message type string for this message
    /// </summary>
    /// <returns>The message type string "DISCOVERY"</returns>
    public override string GetMessageType()
    {
        return "DISCOVERY";
    }

    #endregion
}
