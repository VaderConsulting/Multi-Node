namespace MultiNodeNetwork.Common.Models;
/// <summary>
/// Types of nodes in the network
/// </summary>
public enum NodeType
{
    /// <summary>
    /// Unknown node type
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// Master node (coordinates the network)
    /// </summary>
    Master = 1,

    /// <summary>
    /// Repeater node (extends network coverage)
    /// </summary>
    Repeater = 2,

    /// <summary>
    /// User device (end device)
    /// </summary>
    UserDevice = 3
}
