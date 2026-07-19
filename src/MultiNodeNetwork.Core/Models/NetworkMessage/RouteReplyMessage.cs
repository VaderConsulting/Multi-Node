using System.Text.Json.Serialization;

using MultiNodeNetwork.Common.Models;

namespace MultiNodeNetwork.Core.Models;
/// <summary>
/// Message containing routing information
/// </summary>
public class RouteReplyMessage : NetworkMessage
{
    #region Properties

    /// <summary>
    /// The cost of the route (e.g., hop count, signal strength)
    /// </summary>
    [JsonPropertyName("cost")]
    public double Cost { get; set; } = 0.0;

    /// <summary>
    /// The ID of the route request this reply corresponds to
    /// </summary>
    [JsonPropertyName("requestId")]
    public string RequestId { get; set; } = string.Empty;

    /// <summary>
    /// The route as a list of node IDs from source to destination
    /// </summary>
    [JsonPropertyName("route")]
    public List<byte> Route { get; set; } = [];

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the RouteReplyMessage class
    /// </summary>
    public RouteReplyMessage()
    {
        MessageType = GetMessageType();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Gets the message type string for this message
    /// </summary>
    /// <returns>The message type string "ROUTE_REPLY"</returns>
    public override string GetMessageType()
    {
        return "ROUTE_REPLY";
    }

    #endregion
}
