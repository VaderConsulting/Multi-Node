namespace MultiNodeNetwork.Core.Protocols;
public class MasterAnnouncementEventArgs : EventArgs
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

    public MasterAnnouncementEventArgs(string masterId, byte masterNodeId)
    {
        MasterId = masterId;
        MasterNodeId = masterNodeId;
    }

    #endregion
}
