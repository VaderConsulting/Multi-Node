namespace MultiNodeNetwork.Common.Models;
/// <summary>
/// Status of a node in the network
/// </summary>
public enum NodeStatus
{
    /// <summary>
    /// Node is offline
    /// </summary>
    Offline = 0,

    /// <summary>
    /// Node is online and active
    /// </summary>
    Online = 1,

    /// <summary>
    /// Node is in sleep mode
    /// </summary>
    Sleep = 2,

    /// <summary>
    /// Node is in maintenance mode
    /// </summary>
    Maintenance = 3,

    /// <summary>
    /// Node is in error state
    /// </summary>
    Error = 4
}
