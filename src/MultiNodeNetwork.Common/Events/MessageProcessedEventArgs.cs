using MultiNodeNetwork.Common.Models;

namespace MultiNodeNetwork.Common.Events;
/// <summary>
/// Event arguments for message processed events
/// </summary>
public class MessageProcessedEventArgs : EventArgs
{
    #region Properties

    public NetworkMessage Message
    {
        get;
    }

    public bool Success
    {
        get;
    }

    #endregion

    #region Constructors

    public MessageProcessedEventArgs(NetworkMessage message, bool success)
    {
        Message = message;
        Success = success;
    }

    #endregion
}
