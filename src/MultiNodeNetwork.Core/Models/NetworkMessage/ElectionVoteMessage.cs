using MultiNodeNetwork.Common.Models;
using System.Text.Json.Serialization;

namespace MultiNodeNetwork.Core.Models;
/// <summary>
/// Message containing election vote
/// </summary>
public class ElectionVoteMessage : NetworkMessage
{
#region Properties

    /// <summary>
    /// The string identifier of the candidate being voted for
    /// </summary>
    [JsonPropertyName("candidateId")]
    public string CandidateId { get; set; } = string.Empty;

    /// <summary>
    /// The byte node ID of the candidate being voted for
    /// </summary>
    [JsonPropertyName("candidateNodeId")]
    public byte CandidateNodeId { get; set; } = 0;

    /// <summary>
    /// The unique identifier for this election round
    /// </summary>
    [JsonPropertyName("electionId")]
    public string ElectionId { get; set; } = string.Empty;

    /// <summary>
    /// The vote value (true for yes, false for no)
    /// </summary>
    [JsonPropertyName("vote")]
    public bool Vote { get; set; } = false;

#endregion

#region Constructors

    /// <summary>
    /// Initializes a new instance of the ElectionVoteMessage class
    /// </summary>
    public ElectionVoteMessage()
    {
        MessageType = GetMessageType();
    }

#endregion

#region Public Methods

    /// <summary>
    /// Gets the message type string for this message
    /// </summary>
    /// <returns>The message type string "ELECTION_VOTE"</returns>
    public override string GetMessageType()
    {
        return "ELECTION_VOTE";
    }

#endregion
}
