using MultiNodeNetwork.Common.Models;
using System.Text.Json.Serialization;

namespace MultiNodeNetwork.Core.Models;
/// <summary>
/// Message sent by nodes to register with master
/// </summary>
public class NodeRegistrationMessage : NetworkMessage
{
#region Properties

    /// <summary>
    /// The name of the network interface used by the node
    /// </summary>
    [JsonPropertyName("interfaceName")]
    public string InterfaceName { get; set; } = string.Empty;

    /// <summary>
    /// The IP address of the registering node
    /// </summary>
    [JsonPropertyName("ipAddress")]
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// The MAC address of the registering node
    /// </summary>
    [JsonPropertyName("macAddress")]
    public string MacAddress { get; set; } = string.Empty;

    /// <summary>
    /// List of neighboring node IDs
    /// </summary>
    [JsonPropertyName("neighbors")]
    public List<byte> Neighbors { get; set; } = [];

    /// <summary>
    /// Complete information about the registering node
    /// </summary>
    [JsonPropertyName("nodeInfo")]
    public NodeInfo NodeInfo { get; set; } = new();

    /// <summary>
    /// The physical position of the node (optional)
    /// </summary>
    [JsonPropertyName("position")]
    public Location? Position { get; set; }

    /// <summary>
    /// The signal strength of the node in dBm
    /// </summary>
    [JsonPropertyName("signalStrength")]
    public int SignalStrength { get; set; } = 0;

#endregion

#region Constructors

    /// <summary>
    /// Initializes a new instance of the NodeRegistrationMessage class
    /// </summary>
    public NodeRegistrationMessage()
    {
        MessageType = GetMessageType();
    }

#endregion

#region Public Methods

    /// <summary>
    /// Gets the message type string for this message
    /// </summary>
    /// <returns>The message type string "NODE_REGISTRATION"</returns>
    public override string GetMessageType()
    {
        return "NODE_REGISTRATION";
    }

#endregion
}
