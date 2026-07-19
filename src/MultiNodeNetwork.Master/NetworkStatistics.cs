using MultiNodeNetwork.Common.Models;
using MultiNodeNetwork.Core.Protocols;
using MultiNodeNetwork.Core.Services;

namespace MultiNodeNetwork.Master;

/// <summary>
/// Comprehensive network statistics for the master node
/// </summary>
public class NetworkStatistics
{
    #region Properties

    /// <summary>
    /// The string identifier of the current master node
    /// </summary>
    public string? CurrentMasterId { get; set; }

    /// <summary>
    /// The byte node ID of the current master node
    /// </summary>
    public byte CurrentMasterNodeId { get; set; } = 0;

    /// <summary>
    /// Indicates whether this node is currently the master
    /// </summary>
    public bool IsMaster { get; set; }

    /// <summary>
    /// The current network load (e.g., as a percentage)
    /// </summary>
    public double NetworkLoad { get; set; }

    /// <summary>
    /// The string identifier of this node
    /// </summary>
    public string NodeId { get; set; } = string.Empty;

    /// <summary>
    /// Statistics about node ID allocation across the network
    /// </summary>
    public NodeIdAllocationStatistics NodeIdAllocationStats { get; set; } = new();

    /// <summary>
    /// The byte node ID of this node
    /// </summary>
    public byte NodeIdByte { get; set; } = 0;

    /// <summary>
    /// Count of nodes by type (Master, Repeater, UserDevice)
    /// </summary>
    public Dictionary<NodeType, int> NodeTypeCounts { get; set; } = [];

    /// <summary>
    /// Number of nodes currently online
    /// </summary>
    public int OnlineNodes { get; set; }

    /// <summary>
    /// The current version of the network topology
    /// </summary>
    public int TopologyVersion { get; set; }

    /// <summary>
    /// Total number of nodes in the network
    /// </summary>
    public int TotalNodes { get; set; }

    /// <summary>
    /// The uptime of this node since last restart
    /// </summary>
    public TimeSpan Uptime { get; set; }

    #endregion
}
