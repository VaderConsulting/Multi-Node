namespace MultiNodeNetwork.Core.Protocols;

/// <summary>
/// Event arguments for node removal events
/// </summary>
public class NodeRemovedEventArgs : EventArgs
{
    #region Properties

    /// <summary>
    /// The byte identifier of the removed node
    /// </summary>
    public byte NodeIdByte
    {
        get;
    }

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the NodeRemovedEventArgs class
    /// </summary>
    /// <param name="nodeIdByte">The byte identifier of the removed node</param>
    public NodeRemovedEventArgs(byte nodeIdByte)
    {
        NodeIdByte = nodeIdByte;
    }

    #endregion
}
