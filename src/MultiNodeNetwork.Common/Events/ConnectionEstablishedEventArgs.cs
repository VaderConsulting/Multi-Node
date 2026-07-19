using System.Net.Sockets;

namespace MultiNodeNetwork.Common.Events;
/// <summary>
/// Event arguments for connection established events
/// </summary>
public class ConnectionEstablishedEventArgs : EventArgs
{
    #region Properties

    public TcpClient TcpClient
    {
        get;
    }

    #endregion

    #region Constructors

    public ConnectionEstablishedEventArgs(TcpClient tcpClient)
    {
        TcpClient = tcpClient;
    }

    #endregion
}
