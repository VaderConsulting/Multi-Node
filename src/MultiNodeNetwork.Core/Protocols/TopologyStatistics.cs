namespace MultiNodeNetwork.Core.Protocols;
/// <summary>
/// Statistics for topology management
/// </summary>
public class TopologyStatistics
{
    #region Properties

    /// <summary>
    /// Number of currently active connections in the topology
    /// </summary>
    public int ActiveConnections
    {
        get; set;
    }

    /// <summary>
    /// Average number of connections per node
    /// </summary>
    public double AverageConnectionsPerNode
    {
        get; set;
    }

    /// <summary>
    /// Average signal strength across all nodes
    /// </summary>
    public double AverageSignalStrength
    {
        get; set;
    }

    /// <summary>
    /// Number of nodes currently connected
    /// </summary>
    public int ConnectedNodes
    {
        get; set;
    }

    /// <summary>
    /// Number of nodes currently disconnected
    /// </summary>
    public int DisconnectedNodes
    {
        get; set;
    }

    /// <summary>
    /// The timestamp of the last update
    /// </summary>
    public DateTime LastUpdate
    {
        get; set;
    }

    /// <summary>
    /// The timestamp of the most recent update
    /// </summary>
    public DateTime LastUpdated
    {
        get; set;
    }

    /// <summary>
    /// The current network load (e.g., as a percentage)
    /// </summary>
    public double NetworkLoad
    {
        get; set;
    }

    /// <summary>
    /// Dictionary of node type counts (e.g., Master, Repeater, UserDevice)
    /// </summary>
    public Dictionary<string, int> NodeTypeCounts { get; set; } = [];

    /// <summary>
    /// The version number of the topology
    /// </summary>
    public int TopologyVersion
    {
        get; set;
    }

    /// <summary>
    /// Total number of connections in the topology
    /// </summary>
    public int TotalConnections
    {
        get; set;
    }

    /// <summary>
    /// Total number of nodes in the topology
    /// </summary>
    public int TotalNodes
    {
        get; set;
    }

    #endregion
}
