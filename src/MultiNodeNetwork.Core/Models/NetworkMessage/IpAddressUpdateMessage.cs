using System.Text.Json.Serialization;

using MultiNodeNetwork.Common.Models;

namespace MultiNodeNetwork.Core.Models;
/// <summary>
/// Message sent by nodes when their IP address changes
/// </summary>
public class IpAddressUpdateMessage : NetworkMessage
{
    #region Properties

    /// <summary>
    /// The name of the network interface whose IP address changed
    /// </summary>
    [JsonPropertyName("interfaceName")]
    public string InterfaceName { get; set; } = string.Empty;

    /// <summary>
    /// The MAC address of the node (used for identification)
    /// </summary>
    [JsonPropertyName("macAddress")]
    public string MacAddress { get; set; } = string.Empty;

    /// <summary>
    /// The new IP address of the node
    /// </summary>
    [JsonPropertyName("newIpAddress")]
    public string NewIpAddress { get; set; } = string.Empty;

    /// <summary>
    /// The byte node ID of the node whose IP address changed
    /// </summary>
    [JsonPropertyName("nodeId")]
    public byte NodeId
    {
        get; set;
    }

    /// <summary>
    /// The previous IP address of the node
    /// </summary>
    [JsonPropertyName("oldIpAddress")]
    public string OldIpAddress { get; set; } = string.Empty;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the IpAddressUpdateMessage class
    /// </summary>
    public IpAddressUpdateMessage()
    {
        MessageType = GetMessageType();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Gets the message type string for this message
    /// </summary>
    /// <returns>The message type string "IP_ADDRESS_UPDATE"</returns>
    public override string GetMessageType()
    {
        return "IP_ADDRESS_UPDATE";
    }

    #endregion
}
