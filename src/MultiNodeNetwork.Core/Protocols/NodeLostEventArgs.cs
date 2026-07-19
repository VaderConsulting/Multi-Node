namespace MultiNodeNetwork.Core.Protocols;

/// <summary>
/// Event arguments for node lost events
/// </summary>
public class NodeLostEventArgs : EventArgs
{
    #region Properties

    /// <summary>
    /// The discovered node that was lost
    /// </summary>
    public DiscoveredNode Node
    {
        get;
    }

    /// <summary>
    /// The byte identifier of the lost node
    /// </summary>
    public byte NodeIdByte
    {
        get;
    }

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the NodeLostEventArgs class
    /// </summary>
    /// <param name="nodeIdByte">The byte identifier of the lost node</param>
    /// <param name="node">The discovered node that was lost</param>
    public NodeLostEventArgs(byte nodeIdByte, DiscoveredNode node)
    {
        NodeIdByte = nodeIdByte;
        Node = node;
    }

    #endregion
}
