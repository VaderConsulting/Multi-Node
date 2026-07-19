using System.Text.Json.Serialization;

using MultiNodeNetwork.Common.Models;

namespace MultiNodeNetwork.Core.Models;
/// <summary>
/// Periodic heartbeat message from nodes
/// </summary>
public class HeartbeatMessage : NetworkMessage
{
    #region Properties

    /// <summary>
    /// The current load on the node (e.g., CPU, memory usage)
    /// </summary>
    [JsonPropertyName("load")]
    public double Load { get; set; } = 0.0;

    /// <summary>
    /// Metrics reported by the node (e.g., custom statistics)
    /// </summary>
    [JsonPropertyName("metrics")]
    public Dictionary<string, object> Metrics { get; set; } = [];

    /// <summary>
    /// The current status of the node
    /// </summary>
    [JsonPropertyName("status")]
    public NodeStatus Status { get; set; } = NodeStatus.Online;

    /// <summary>
    /// The uptime of the node since last restart
    /// </summary>
    [JsonPropertyName("uptime")]
    public TimeSpan Uptime { get; set; } = TimeSpan.Zero;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the HeartbeatMessage class
    /// </summary>
    public HeartbeatMessage()
    {
        MessageType = GetMessageType();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Gets the message type string for this message
    /// </summary>
    /// <returns>The message type string "HEARTBEAT"</returns>
    public override string GetMessageType()
    {
        return "HEARTBEAT";
    }

    #endregion
}
