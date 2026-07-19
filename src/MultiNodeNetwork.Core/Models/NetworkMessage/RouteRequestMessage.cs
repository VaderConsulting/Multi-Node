using System.Text.Json.Serialization;

using MultiNodeNetwork.Common.Models;

namespace MultiNodeNetwork.Core.Models;
/// <summary>
/// Message to request routing information
/// </summary>
public class RouteRequestMessage : NetworkMessage
{
    #region Properties

    /// <summary>
    /// The string identifier of the destination node
    /// </summary>
    [JsonPropertyName("destinationId")]
    public string DestinationId { get; set; } = string.Empty;

    /// <summary>
    /// The byte node ID of the destination node
    /// </summary>
    [JsonPropertyName("destinationNodeId")]
    public byte DestinationNodeId { get; set; } = 0;

    /// <summary>
    /// The number of hops the request has traversed
    /// </summary>
    [JsonPropertyName("hopCount")]
    public int HopCount { get; set; } = 0;

    /// <summary>
    /// The path of node IDs the request has traversed
    /// </summary>
    [JsonPropertyName("path")]
    public List<byte> Path { get; set; } = [];

    /// <summary>
    /// Unique identifier for this route request
    /// </summary>
    [JsonPropertyName("requestId")]
    public string RequestId { get; set; } = Guid.NewGuid().ToString();

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the RouteRequestMessage class
    /// </summary>
    public RouteRequestMessage()
    {
        MessageType = GetMessageType();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Gets the message type string for this message
    /// </summary>
    /// <returns>The message type string "ROUTE_REQUEST"</returns>
    public override string GetMessageType()
    {
        return "ROUTE_REQUEST";
    }

    #endregion
}
