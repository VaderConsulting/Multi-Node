namespace MultiNodeNetwork.Common.Events;
/// <summary>
/// Event arguments for message received events
/// </summary>
public class MessageReceivedEventArgs : EventArgs
{
    #region Properties

    public string MessageJson
    {
        get;
    }

    #endregion

    #region Constructors

    public MessageReceivedEventArgs(string messageJson)
    {
        MessageJson = messageJson;
    }

    #endregion
}
