using MultiNodeNetwork.Common.Models;

namespace MultiNodeNetwork.Core.Services;
/// <summary>
/// Static utility class for node ID allocation and management
/// </summary>
public static class NodeIdAllocation
{
    #region Constants

    public const byte MASTER_BACKUP = 0x02;
    public const byte MASTER_END = 0x0F;

    // Reserved node IDs
    public const byte MASTER_PRIMARY = 0x01;

    // Node ID ranges
    public const byte MASTER_START = 0x01;
    public const byte REPEATER_END = 0x7F;
    public const byte REPEATER_START = 0x10;
    public const byte USER_DEVICE_END = 0xFF;
    public const byte USER_DEVICE_START = 0x80;

    #endregion

    #region Private Methods

    /// <summary>
    /// Get the start and end range for a node type
    /// </summary>
    private static (byte start, byte end) GetRange(NodeType nodeType)
    {
        return nodeType switch
        {
            NodeType.Master => (MASTER_START, MASTER_END),
            NodeType.Repeater => (REPEATER_START, REPEATER_END),
            NodeType.UserDevice => (USER_DEVICE_START, USER_DEVICE_END),
            _ => (0, 0)
        };
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Format a node ID for display
    /// </summary>
    public static string FormatNodeId(byte nodeId)
    {
        return $"0x{nodeId:X2} ({nodeId})";
    }

    /// <summary>
    /// Get the next available node ID for a specific type
    /// </summary>
    public static byte? GetNextAvailableNodeId(NodeType nodeType, HashSet<byte> allocatedIds)
    {
        (byte start, byte end) = GetRange(nodeType);

        for (byte id = start; id <= end; id++)
        {
            if (!allocatedIds.Contains(id))
            {
                return id;
            }
        }

        return null;
    }

    /// <summary>
    /// Get the node type for a given node ID
    /// </summary>
    public static NodeType GetNodeType(byte nodeId)
    {
        if (nodeId is >= MASTER_START and <= MASTER_END)
        {
            return NodeType.Master;
        }
        else if (nodeId is >= REPEATER_START and <= REPEATER_END)
        {
            return NodeType.Repeater;
        }
        else if (nodeId is >= USER_DEVICE_START and <= USER_DEVICE_END)
        {
            return NodeType.UserDevice;
        }
        else
        {
            return NodeType.Unknown;
        }
    }

    /// <summary>
    /// Get the range description for a node type
    /// </summary>
    public static string GetRangeDescription(NodeType nodeType)
    {
        return nodeType switch
        {
            NodeType.Master => $"0x{MASTER_START:X2}-0x{MASTER_END:X2} ({MASTER_START}-{MASTER_END})",
            NodeType.Repeater => $"0x{REPEATER_START:X2}-0x{REPEATER_END:X2} ({REPEATER_START}-{REPEATER_END})",
            NodeType.UserDevice => $"0x{USER_DEVICE_START:X2}-0x{USER_DEVICE_END:X2} ({USER_DEVICE_START}-{USER_DEVICE_END})",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Get the total number of slots available for a node type
    /// </summary>
    public static int GetTotalSlots(NodeType nodeType)
    {
        return nodeType switch
        {
            NodeType.Master => MASTER_END - MASTER_START + 1,
            NodeType.Repeater => REPEATER_END - REPEATER_START + 1,
            NodeType.UserDevice => USER_DEVICE_END - USER_DEVICE_START + 1,
            _ => 0
        };
    }

    /// <summary>
    /// Check if a node ID is valid for a specific node type
    /// </summary>
    public static bool IsValidNodeId(byte nodeId, NodeType nodeType)
    {
        NodeType actualType = GetNodeType(nodeId);
        return actualType == nodeType;
    }

    /// <summary>
    /// Parse a node ID from string (supports hex and decimal)
    /// </summary>
    public static byte? ParseNodeId(string nodeIdString)
    {
        if (string.IsNullOrWhiteSpace(nodeIdString))
        {
            return null;
        }

        // Remove any whitespace
        nodeIdString = nodeIdString.Trim();

        try
        {
            if (nodeIdString.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                // Hex format
                return Convert.ToByte(nodeIdString, 16);
            }
            else
            {
                // Decimal format
                return Convert.ToByte(nodeIdString);
            }
        }
        catch
        {
            return null;
        }
    }

    #endregion
}
