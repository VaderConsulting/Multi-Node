using System.Net.Sockets;

namespace MultiNodeNetwork.Common.Events;
/// <summary>
/// Event arguments for connection lost events
/// </summary>
public class ConnectionLostEventArgs : EventArgs
{
    #region Properties

    /// <summary>
    /// The TCP client associated with the lost connection
    /// </summary>
    public TcpClient TcpClient
    {
        get;
    }

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the ConnectionLostEventArgs class
    /// </summary>
    /// <param name="tcpClient">The TCP client associated with the lost connection</param>
    public ConnectionLostEventArgs(TcpClient tcpClient)
    {
        TcpClient = tcpClient;
    }

    #endregion
}
