namespace MultiNodeNetwork.Core.Protocols;

/// <summary>
/// Event arguments for topology update events
/// </summary>
public class TopologyUpdatedEventArgs : EventArgs
{
    #region Properties

    /// <summary>
    /// The timestamp when the topology was updated
    /// </summary>
    public DateTime Timestamp
    {
        get;
    }

    /// <summary>
    /// The version number of the updated topology
    /// </summary>
    public int Version
    {
        get;
    }

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the TopologyUpdatedEventArgs class
    /// </summary>
    /// <param name="version">The version number of the updated topology</param>
    /// <param name="timestamp">The timestamp when the topology was updated</param>
    public TopologyUpdatedEventArgs(int version, DateTime timestamp)
    {
        Version = version;
        Timestamp = timestamp;
    }

    #endregion
}
