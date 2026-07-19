using MultiNodeNetwork.Common.Models;
using MultiNodeNetwork.Core.Models;

namespace MultiNodeNetwork.Core.Protocols;
/// <summary>
/// Represents a discovered node in the network
/// </summary>
public class DiscoveredNode
{
    #region Properties

    /// <summary>
    /// The battery level of the discovered node (percentage)
    /// </summary>
    public int BatteryLevel
    {
        get; set;
    }

    /// <summary>
    /// Capabilities supported by the discovered node
    /// </summary>
    public Dictionary<string, object> Capabilities { get; set; } = [];

    /// <summary>
    /// The time when the node was discovered
    /// </summary>
    public DateTime DiscoveryTime
    {
        get; set;
    }

    /// <summary>
    /// The name of the network interface used by the node
    /// </summary>
    public string InterfaceName { get; set; } = string.Empty;

    /// <summary>
    /// The IP address of the discovered node
    /// </summary>
    public string IpAddress { get; set; } = string.Empty;

    /// <summary>
    /// The last time the node was seen active
    /// </summary>
    public DateTime LastSeen
    {
        get; set;
    }

    /// <summary>
    /// The current load on the node (e.g., CPU, memory usage)
    /// </summary>
    public double Load { get; set; } = 0.0;

    /// <summary>
    /// The physical location of the node (optional)
    /// </summary>
    public Location? Location
    {
        get; set;
    }

    /// <summary>
    /// The MAC address of the discovered node
    /// </summary>
    public string MacAddress { get; set; } = string.Empty;

    /// <summary>
    /// Metrics reported by the node (e.g., custom statistics)
    /// </summary>
    public Dictionary<string, object> Metrics { get; set; } = [];

    /// <summary>
    /// The unique string identifier for the node
    /// </summary>
    public string NodeId { get; set; } = string.Empty;

    /// <summary>
    /// The byte node ID
    /// </summary>
    public byte NodeIdByte
    {
        get; set;
    }

    /// <summary>
    /// The type of the node (master, repeater, etc.)
    /// </summary>
    public NodeType NodeType
    {
        get; set;
    }

    /// <summary>
    /// The port number used by the node for communication
    /// </summary>
    public int Port
    {
        get; set;
    }

    /// <summary>
    /// The signal strength of the node in dBm
    /// </summary>
    public int SignalStrength
    {
        get; set;
    }

    /// <summary>
    /// The current status of the node
    /// </summary>
    public NodeStatus Status { get; set; } = NodeStatus.Offline;

    /// <summary>
    /// The uptime of the node since last restart
    /// </summary>
    public TimeSpan Uptime { get; set; } = TimeSpan.Zero;

    #endregion
}
