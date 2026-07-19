using System.Text.Json.Serialization;

using MultiNodeNetwork.Common.Models;

namespace MultiNodeNetwork.Core.Models;
/// <summary>
/// Message announcing election result
/// </summary>
public class ElectionResultMessage : NetworkMessage
{
    #region Properties

    /// <summary>
    /// The unique identifier for this election round
    /// </summary>
    [JsonPropertyName("electionId")]
    public string ElectionId { get; set; } = string.Empty;

    /// <summary>
    /// The total number of votes cast in the election
    /// </summary>
    [JsonPropertyName("totalVotes")]
    public int TotalVotes { get; set; } = 0;

    /// <summary>
    /// The number of votes received by the winner
    /// </summary>
    [JsonPropertyName("voteCount")]
    public int VoteCount { get; set; } = 0;

    /// <summary>
    /// The string identifier of the winning candidate
    /// </summary>
    [JsonPropertyName("winnerId")]
    public string WinnerId { get; set; } = string.Empty;

    /// <summary>
    /// The byte node ID of the winning candidate
    /// </summary>
    [JsonPropertyName("winnerNodeId")]
    public byte WinnerNodeId { get; set; } = 0;

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the ElectionResultMessage class
    /// </summary>
    public ElectionResultMessage()
    {
        MessageType = GetMessageType();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Gets the message type string for this message
    /// </summary>
    /// <returns>The message type string "ELECTION_RESULT"</returns>
    public override string GetMessageType()
    {
        return "ELECTION_RESULT";
    }

    #endregion
}
