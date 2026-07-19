using System.Text.Json.Serialization;

namespace MultiNodeNetwork.Common.Models;
/// <summary>
/// Base message class for all network communications
/// </summary>
public abstract class NetworkMessage
{
    #region Properties

    /// <summary>
    /// Arbitrary data payload for the message
    /// </summary>
    [JsonPropertyName("data")]
    public Dictionary<string, object> Data { get; set; } = [];

    /// <summary>
    /// Unique identifier for this message
    /// </summary>
    [JsonPropertyName("messageId")]
    public string MessageId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// The type of the message (e.g., HEARTBEAT, DISCOVERY)
    /// </summary>
    [JsonPropertyName("messageType")]
    public string MessageType { get; set; } = string.Empty;

    /// <summary>
    /// The string identifier of the sender node
    /// </summary>
    [JsonPropertyName("senderId")]
    public string SenderId { get; set; } = string.Empty;

    /// <summary>
    /// The byte node ID of the sender
    /// </summary>
    [JsonPropertyName("senderNodeId")]
    public byte SenderNodeId { get; set; } = 0;

    /// <summary>
    /// The UTC timestamp when the message was created
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    #endregion

    #region Public Methods

    /// <summary>
    /// Gets the message type string for this message
    /// </summary>
    /// <returns>The message type string</returns>
    public abstract string GetMessageType();

    #endregion
}
