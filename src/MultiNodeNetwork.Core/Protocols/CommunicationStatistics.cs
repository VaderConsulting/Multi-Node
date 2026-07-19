namespace MultiNodeNetwork.Core.Protocols;

/// <summary>
/// Statistics for communication protocol performance and status
/// </summary>
public class CommunicationStatistics
{
    #region Properties

    /// <summary>
    /// Number of currently active TCP connections
    /// </summary>
    public int ActiveTcpConnections { get; set; }

    /// <summary>
    /// Indicates whether the communication service is currently running
    /// </summary>
    public bool IsRunning { get; set; }

    /// <summary>
    /// The node identifier for this communication instance
    /// </summary>
    public string NodeId { get; set; } = string.Empty;

    /// <summary>
    /// The TCP port number used for communication
    /// </summary>
    public int TcpPort { get; set; }

    /// <summary>
    /// The UDP port number used for broadcasting
    /// </summary>
    public int UdpPort { get; set; }

    #endregion
}
