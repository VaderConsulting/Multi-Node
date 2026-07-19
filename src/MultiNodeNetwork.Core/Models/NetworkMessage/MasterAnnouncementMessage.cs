using MultiNodeNetwork.Common.Models;
using System.Text.Json.Serialization;

namespace MultiNodeNetwork.Core.Models;
/// <summary>
/// Message broadcast by master node to announce its presence
/// </summary>
public class MasterAnnouncementMessage : NetworkMessage
{
    #region Properties

    /// <summary>
    /// The time when the master node started
    /// </summary>
    [JsonPropertyName("masterStartTime")]
    public DateTime MasterStartTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// General information about the network (configuration, statistics, etc.)
    /// </summary>
    [JsonPropertyName("networkInfo")]
    public Dictionary<string, object> NetworkInfo { get; set; } = [];

    /// <summary>
    /// The number of nodes currently in the network
    /// </summary>
    [JsonPropertyName("nodeCount")]
    public int NodeCount { get; set; } = 0;

    /// <summary>
    /// The current version of the network topology
    /// </summary>
    [JsonPropertyName("topologyVersion")]
    public int TopologyVersion { get; set; } = 0;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the MasterAnnouncementMessage class
    /// </summary>
    public MasterAnnouncementMessage()
    {
        MessageType = GetMessageType();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Gets the message type string for this message
    /// </summary>
    /// <returns>The message type string "MASTER_ANNOUNCEMENT"</returns>
    public override string GetMessageType()
    {
        return "MASTER_ANNOUNCEMENT";
    }

    #endregion
}
