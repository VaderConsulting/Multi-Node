using MultiNodeNetwork.Common.Models;
using System.Text.Json.Serialization;

namespace MultiNodeNetwork.Core.Models;
/// <summary>
/// Message sent by master to update network topology
/// </summary>
public class TopologyUpdateMessage : NetworkMessage
{
#region Properties

    /// <summary>
    /// The updated topology map containing nodes and connections
    /// </summary>
    [JsonPropertyName("topology")]
    public TopologyMap Topology { get; set; } = new();

    /// <summary>
    /// The version number of the topology update
    /// </summary>
    [JsonPropertyName("version")]
    public int Version { get; set; } = 0;

#endregion

#region Constructors

    /// <summary>
    /// Initializes a new instance of the TopologyUpdateMessage class
    /// </summary>
    public TopologyUpdateMessage()
    {
        MessageType = GetMessageType();
    }

#endregion

#region Public Methods

    /// <summary>
    /// Gets the message type string for this message
    /// </summary>
    /// <returns>The message type string "TOPOLOGY_UPDATE"</returns>
    public override string GetMessageType()
    {
        return "TOPOLOGY_UPDATE";
    }

#endregion
}
