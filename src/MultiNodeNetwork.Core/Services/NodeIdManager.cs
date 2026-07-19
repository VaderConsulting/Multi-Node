using Microsoft.Extensions.Logging;

using MultiNodeNetwork.Common.Models;
using MultiNodeNetwork.Core.Configuration;

namespace MultiNodeNetwork.Core.Services;
/// <summary>
/// Node ID manager for handling node ID allocation and validation
/// </summary>
public class NodeIdManager
{
    #region Fields

    private readonly HashSet<byte> _allocatedNodeIds = [];
    private readonly NetworkConfig _config;
    private readonly object _lockObject = new();
    private readonly ILogger<NodeIdManager> _logger;

    #endregion

    #region Constructors

    public NodeIdManager(ILogger<NodeIdManager> logger, NetworkConfig config)
    {
        _logger = logger;
        _config = config;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Allocate a node ID for a specific node type
    /// </summary>
    public byte? AllocateNodeId(NodeType nodeType)
    {
        lock (_lockObject)
        {
            byte? availableId = NodeIdAllocation.GetNextAvailableNodeId(nodeType, _allocatedNodeIds);

            if (availableId.HasValue)
            {
                _ = _allocatedNodeIds.Add(availableId.Value);
                _logger.LogInformation("Allocated node ID 0x{NodeId:X2} for {NodeType}", availableId.Value, nodeType);
                return availableId.Value;
            }
            else
            {
                _logger.LogWarning("No available node IDs for {NodeType}", nodeType);
                return null;
            }
        }
    }

    /// <summary>
    /// Allocate a specific node ID (for reserved IDs)
    /// </summary>
    public bool AllocateSpecificNodeId(byte nodeId)
    {
        lock (_lockObject)
        {
            if (_allocatedNodeIds.Contains(nodeId))
            {
                _logger.LogWarning("Node ID 0x{NodeId:X2} is already allocated", nodeId);
                return false;
            }

            NodeType nodeType = NodeIdAllocation.GetNodeType(nodeId);
            if (!NodeIdAllocation.IsValidNodeId(nodeId, nodeType))
            {
                _logger.LogWarning("Node ID 0x{NodeId:X2} is not valid for {NodeType}", nodeId, nodeType);
                return false;
            }

            _ = _allocatedNodeIds.Add(nodeId);
            _logger.LogInformation("Allocated specific node ID 0x{NodeId:X2} for {NodeType}", nodeId, nodeType);
            return true;
        }
    }

    /// <summary>
    /// Clear all allocations (for testing/reset purposes)
    /// </summary>
    public void ClearAllocations()
    {
        lock (_lockObject)
        {
            int count = _allocatedNodeIds.Count;
            _allocatedNodeIds.Clear();
            _logger.LogInformation("Cleared {Count} node ID allocations", count);
        }
    }

    /// <summary>
    /// Format node ID for display
    /// </summary>
    public string FormatNodeId(byte nodeId)
    {
        return NodeIdAllocation.FormatNodeId(nodeId);
    }

    /// <summary>
    /// Get all allocated node IDs
    /// </summary>
    public IEnumerable<byte> GetAllocatedNodeIds()
    {
        lock (_lockObject)
        {
            return _allocatedNodeIds.ToList();
        }
    }

    /// <summary>
    /// Get allocated node IDs by type
    /// </summary>
    public IEnumerable<byte> GetAllocatedNodeIdsByType(NodeType nodeType)
    {
        lock (_lockObject)
        {
            return _allocatedNodeIds.Where(id => NodeIdAllocation.GetNodeType(id) == nodeType);
        }
    }

    /// <summary>
    /// Get the next available node ID for a type
    /// </summary>
    public byte? GetNextAvailableNodeId(NodeType nodeType)
    {
        lock (_lockObject)
        {
            return NodeIdAllocation.GetNextAvailableNodeId(nodeType, _allocatedNodeIds);
        }
    }

    /// <summary>
    /// Get node type for a node ID
    /// </summary>
    public NodeType GetNodeType(byte nodeId)
    {
        return NodeIdAllocation.GetNodeType(nodeId);
    }

    /// <summary>
    /// Get allocation statistics
    /// </summary>
    public NodeIdAllocationStatistics GetStatistics()
    {
        lock (_lockObject)
        {
            NodeIdAllocationStatistics stats = new NodeIdAllocationStatistics();

            foreach (NodeType nodeType in Enum.GetValues(typeof(NodeType)))
            {
                if (nodeType == NodeType.Unknown)
                {
                    continue;
                }

                int allocated = GetAllocatedNodeIdsByType(nodeType).Count();
                int total = NodeIdAllocation.GetTotalSlots(nodeType);
                int available = total - allocated;

                stats.AllocationInfo[nodeType] = new NodeTypeAllocationInfo
                {
                    NodeType = nodeType,
                    TotalSlots = total,
                    AllocatedSlots = allocated,
                    AvailableSlots = available,
                    Range = NodeIdAllocation.GetRangeDescription(nodeType)
                };
            }

            stats.TotalAllocated = _allocatedNodeIds.Count;
            stats.TotalAvailable = 255 - _allocatedNodeIds.Count;

            return stats;
        }
    }

    /// <summary>
    /// Check if a node ID is allocated
    /// </summary>
    public bool IsNodeIdAllocated(byte nodeId)
    {
        lock (_lockObject)
        {
            return _allocatedNodeIds.Contains(nodeId);
        }
    }

    /// <summary>
    /// Parse node ID from string
    /// </summary>
    public byte? ParseNodeId(string nodeIdString)
    {
        return NodeIdAllocation.ParseNodeId(nodeIdString);
    }

    /// <summary>
    /// Release a node ID
    /// </summary>
    public bool ReleaseNodeId(byte nodeId)
    {
        lock (_lockObject)
        {
            if (_allocatedNodeIds.Remove(nodeId))
            {
                _logger.LogInformation("Released node ID 0x{NodeId:X2}", nodeId);
                return true;
            }
            else
            {
                _logger.LogWarning("Node ID 0x{NodeId:X2} was not allocated", nodeId);
                return false;
            }
        }
    }

    /// <summary>
    /// Reserve a range of node IDs for a specific purpose
    /// </summary>
    public bool ReserveNodeIdRange(byte startId, byte endId, string purpose)
    {
        lock (_lockObject)
        {
            List<byte> reservedIds = [];

            for (byte id = startId; id <= endId; id++)
            {
                if (_allocatedNodeIds.Contains(id))
                {
                    _logger.LogWarning("Cannot reserve range 0x{StartId:X2}-0x{EndId:X2}, node ID 0x{Id:X2} is already allocated",
                        startId, endId, id);

                    // Release any IDs we already reserved
                    foreach (byte reservedId in reservedIds)
                    {
                        _ = _allocatedNodeIds.Remove(reservedId);
                    }

                    return false;
                }

                reservedIds.Add(id);
            }

            // Allocate all IDs in the range
            foreach (byte id in reservedIds)
            {
                _ = _allocatedNodeIds.Add(id);
            }

            _logger.LogInformation("Reserved node ID range 0x{StartId:X2}-0x{EndId:X2} for {Purpose}",
                startId, endId, purpose);
            return true;
        }
    }

    /// <summary>
    /// Validate a node ID
    /// </summary>
    public bool ValidateNodeId(byte nodeId)
    {
        NodeType nodeType = NodeIdAllocation.GetNodeType(nodeId);
        return NodeIdAllocation.IsValidNodeId(nodeId, nodeType);
    }

    #endregion
}
