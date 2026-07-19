namespace MultiNodeNetwork.Core.Protocols;

/// <summary>
/// Information about a master election process
/// </summary>
public class ElectionInfo
{
    #region Properties

    /// <summary>
    /// List of candidate node IDs participating in the election
    /// </summary>
    public List<byte> Candidates { get; set; } = [];

    /// <summary>
    /// The unique identifier for this election round
    /// </summary>
    public string ElectionId { get; set; } = string.Empty;

    /// <summary>
    /// The time when the election was started
    /// </summary>
    public DateTime StartTime
    {
        get; set;
    }

    /// <summary>
    /// The current status of the election process
    /// </summary>
    public ElectionStatus Status
    {
        get; set;
    }

    #endregion
}
