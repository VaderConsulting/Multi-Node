using MultiNodeNetwork.Common.Models;

namespace MultiNodeNetwork.Core.Services;

/// <summary>
/// Information about node type allocation statistics
/// </summary>
public class NodeTypeAllocationInfo
{
    #region Properties

    /// <summary>
    /// Number of slots that have been allocated for this node type
    /// </summary>
    public int AllocatedSlots { get; set; }

    /// <summary>
    /// Number of slots that are still available for this node type
    /// </summary>
    public int AvailableSlots { get; set; }

    /// <summary>
    /// The type of node this allocation info represents
    /// </summary>
    public NodeType NodeType { get; set; }

    /// <summary>
    /// The range of node IDs allocated for this node type (e.g., "0x01-0x0F")
    /// </summary>
    public string Range { get; set; } = string.Empty;

    /// <summary>
    /// Total number of slots available for this node type
    /// </summary>
    public int TotalSlots { get; set; }

    #endregion
}
