using System.Text.Json.Serialization;

using MultiNodeNetwork.Common.Models;

namespace MultiNodeNetwork.Core.Models;
/// <summary>
/// Message to initiate master election
/// </summary>
public class MasterElectionMessage : NetworkMessage
{
    #region Properties

    /// <summary>
    /// Information about the candidate node (capabilities, metrics, etc.)
    /// </summary>
    [JsonPropertyName("candidateInfo")]
    public Dictionary<string, object> CandidateInfo { get; set; } = [];

    /// <summary>
    /// Unique identifier for this election round
    /// </summary>
    [JsonPropertyName("electionId")]
    public string ElectionId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// The priority value of the candidate (higher values have higher priority)
    /// </summary>
    [JsonPropertyName("priority")]
    public int Priority { get; set; } = 0;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the MasterElectionMessage class
    /// </summary>
    public MasterElectionMessage()
    {
        MessageType = GetMessageType();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Gets the message type string for this message
    /// </summary>
    /// <returns>The message type string "MASTER_ELECTION"</returns>
    public override string GetMessageType()
    {
        return "MASTER_ELECTION";
    }

    #endregion
}
