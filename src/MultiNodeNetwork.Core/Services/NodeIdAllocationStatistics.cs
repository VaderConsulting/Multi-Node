using MultiNodeNetwork.Common.Models;
using MultiNodeNetwork.Core.Protocols;

namespace MultiNodeNetwork.Core.Services;

/// <summary>
/// Statistics about node ID allocation across the network
/// </summary>
public class NodeIdAllocationStatistics
{
    #region Properties

    /// <summary>
    /// Dictionary of allocation information by node type
    /// </summary>
    public Dictionary<NodeType, NodeTypeAllocationInfo> AllocationInfo { get; set; } = [];

    /// <summary>
    /// Total number of node IDs that have been allocated
    /// </summary>
    public int TotalAllocated { get; set; }

    /// <summary>
    /// Total number of node IDs that are still available
    /// </summary>
    public int TotalAvailable { get; set; }

    #endregion
}
