namespace MultiNodeNetwork.Core.Protocols;

/// <summary>
/// Represents a vote in a master election process
/// </summary>
public class ElectionVote
{
    #region Properties

    /// <summary>
    /// The identifier of the candidate being voted for
    /// </summary>
    public string CandidateId { get; set; } = string.Empty;

    /// <summary>
    /// The byte node ID of the candidate being voted for
    /// </summary>
    public byte CandidateNodeId { get; set; } = 0;

    /// <summary>
    /// The unique identifier for this election round
    /// </summary>
    public string ElectionId { get; set; } = string.Empty;

    /// <summary>
    /// The timestamp when this vote was cast
    /// </summary>
    public DateTime Timestamp
    {
        get; set;
    }

    /// <summary>
    /// The vote value (true for yes, false for no)
    /// </summary>
    public bool Vote
    {
        get; set;
    }

    /// <summary>
    /// The identifier of the node casting the vote
    /// </summary>
    public string VoterId { get; set; } = string.Empty;

    /// <summary>
    /// The byte node ID of the node casting the vote
    /// </summary>
    public byte VoterNodeId { get; set; } = 0;

    #endregion
}
