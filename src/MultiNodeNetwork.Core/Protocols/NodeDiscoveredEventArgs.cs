namespace MultiNodeNetwork.Core.Protocols;
public class NodeDiscoveredEventArgs : EventArgs
{
    #region Properties

    public DiscoveredNode Node
    {
        get;
    }

    #endregion

    #region Constructors

    public NodeDiscoveredEventArgs(DiscoveredNode node)
    {
        Node = node;
    }

    #endregion
}
