namespace MultiNodeNetwork.Core.Protocols;
public class MasterElectedEventArgs : EventArgs
{
    #region Properties

    public string MasterId
    {
        get;
    }

    public byte MasterNodeId
    {
        get;
    }

    #endregion

    #region Constructors

    public MasterElectedEventArgs(string masterId, byte masterNodeId)
    {
        MasterId = masterId;
        MasterNodeId = masterNodeId;
    }

    #endregion
}
