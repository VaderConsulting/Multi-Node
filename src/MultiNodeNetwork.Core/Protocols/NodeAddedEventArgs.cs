using MultiNodeNetwork.Core.Models;

namespace MultiNodeNetwork.Core.Protocols;
public class NodeAddedEventArgs : EventArgs
{
    #region Properties

    public byte NodeIdByte
    {
        get;
    }

    public NodeInfo NodeInfo
    {
        get;
    }

    #endregion

    #region Constructors

    public NodeAddedEventArgs(byte nodeIdByte, NodeInfo nodeInfo)
    {
        NodeIdByte = nodeIdByte;
        NodeInfo = nodeInfo;
    }

    #endregion
}
