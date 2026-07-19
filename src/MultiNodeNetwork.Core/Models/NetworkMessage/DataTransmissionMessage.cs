using System.Text.Json.Serialization;

using MultiNodeNetwork.Common.Models;

namespace MultiNodeNetwork.Core.Models;
/// <summary>
/// Message for data transmission between nodes
/// </summary>
public class DataTransmissionMessage : NetworkMessage
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
    /// The number of hops the message has traversed
    /// </summary>
    [JsonPropertyName("hopCount")]
    public int HopCount { get; set; } = 0;

    /// <summary>
    /// The maximum number of hops allowed for this message
    /// </summary>
    [JsonPropertyName("maxHops")]
    public int MaxHops { get; set; } = 5;

    /// <summary>
    /// The actual data payload being transmitted
    /// </summary>
    [JsonPropertyName("payload")]
    public object Payload { get; set; } = new();

    /// <summary>
    /// The priority level of this message (higher values have higher priority)
    /// </summary>
    [JsonPropertyName("priority")]
    public int Priority { get; set; } = 0;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the DataTransmissionMessage class
    /// </summary>
    public DataTransmissionMessage()
    {
        MessageType = GetMessageType();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Gets the message type string for this message
    /// </summary>
    /// <returns>The message type string "DATA_TRANSMISSION"</returns>
    public override string GetMessageType()
    {
        return "DATA_TRANSMISSION";
    }

    #endregion
}
