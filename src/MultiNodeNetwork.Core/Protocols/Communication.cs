using Microsoft.Extensions.Logging;
using MultiNodeNetwork.Common.Events;
using MultiNodeNetwork.Common.Models;
using MultiNodeNetwork.Core.Configuration;
using MultiNodeNetwork.Core.Models;
using MultiNodeNetwork.Core.Services;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace MultiNodeNetwork.Core.Protocols;
/// <summary>
/// Communication protocol for network message transmission
/// </summary>
public class Communication : IDisposable
{
    #region Events

    public event EventHandler<ConnectionEstablishedEventArgs>? ConnectionEstablished;

    public event EventHandler<ConnectionLostEventArgs>? ConnectionLost;

    public event EventHandler<MessageReceivedEventArgs>? MessageReceived;

    #endregion

    #region Fields

    private readonly NetworkConfig _config;
    private bool _disposed = false;
    private bool _isRunning = false;
    private readonly object _lockObject = new();
    private readonly ILogger<Communication> _logger;
    private string? _nodeId;
    private readonly List<TcpClient> _tcpClients = [];
    private readonly TcpListener _tcpListener;
    private readonly UdpClient _udpClient;

    #endregion

    #region Constructors

    public Communication(ILogger<Communication> logger, NetworkConfig config)
    {
        _logger = logger;
        _config = config;
        _udpClient = new UdpClient();
        _tcpListener = new TcpListener(IPAddress.Any, _config.Network.TcpPort);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Look up the IP address for a given node ID.
    /// This is a stub and should be implemented to use the topology or discovery service.
    /// </summary>
    /// <param name="nodeId">The node ID (byte)</param>
    /// <returns>The IP address of the node, or null if not found</returns>
    private IPAddress? GetNodeIpAddress(byte nodeId)
    {
        // TODO: Implement actual lookup using topology/discovery
        // For now, return null to indicate not found
        return null;
    }

    private async Task HandleTcpConnectionAsync(TcpClient tcpClient)
    {
        try
        {
            byte[] buffer = new byte[_config.Performance.BufferSize];
            NetworkStream stream = tcpClient.GetStream();

            while (_isRunning && !_disposed && tcpClient.Connected)
            {
                try
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        break; // Connection closed
                    }

                    string messageJson = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                    _logger.LogDebug("Received TCP message: {MessageLength} characters", messageJson.Length);

                    MessageReceived?.Invoke(this, new MessageReceivedEventArgs(messageJson));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error reading from TCP connection");
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling TCP connection");
        }
        finally
        {
            try
            {
                lock (_lockObject)
                {
                    _tcpClients.Remove(tcpClient);
                }

                tcpClient.Close();

                _logger.LogInformation("TCP connection closed");
                ConnectionLost?.Invoke(this, new ConnectionLostEventArgs(tcpClient));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing TCP connection");
            }
        }
    }

    private async Task StartTcpListenerAsync()
    {
        try
        {
            while (_isRunning && !_disposed)
            {
                try
                {
                    TcpClient tcpClient = await _tcpListener.AcceptTcpClientAsync();

                    lock (_lockObject)
                    {
                        _tcpClients.Add(tcpClient);
                    }

                    _logger.LogInformation("TCP connection established from {RemoteEndPoint}",
                        tcpClient.Client.RemoteEndPoint);

                    ConnectionEstablished?.Invoke(this, new ConnectionEstablishedEventArgs(tcpClient));

                    // Start handling the TCP connection
                    _ = HandleTcpConnectionAsync(tcpClient);
                }
                catch (Exception ex)
                {
                    if (_isRunning && !_disposed)
                    {
                        _logger.LogError(ex, "Error accepting TCP connection");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TCP listener failed");
        }
    }

    private async Task StartUdpListenerAsync()
    {
        try
        {
            IPEndPoint broadcastEndPoint = new IPEndPoint(IPAddress.Any, _config.Network.BroadcastPort);

            while (_isRunning && !_disposed)
            {
                try
                {
                    UdpReceiveResult result = await _udpClient.ReceiveAsync();
                    string messageJson = Encoding.UTF8.GetString(result.Buffer);

                    _logger.LogDebug("Received UDP message from {SenderEndPoint}: {MessageLength} characters",
                        result.RemoteEndPoint, messageJson.Length);

                    MessageReceived?.Invoke(this, new MessageReceivedEventArgs(messageJson));
                }
                catch (Exception ex)
                {
                    if (_isRunning && !_disposed)
                    {
                        _logger.LogError(ex, "Error receiving UDP message");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UDP listener failed");
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Send a message via UDP broadcast
    /// </summary>
    public async Task<bool> BroadcastMessageAsync(NetworkMessage message)
    {
        if (_disposed || !_isRunning)
        {
            return false;
        }

        try
        {
            string messageJson = JsonSerializer.Serialize(message);
            byte[] messageBytes = Encoding.UTF8.GetBytes(messageJson);

            IPEndPoint broadcastEndPoint = new IPEndPoint(IPAddress.Broadcast, _config.Network.BroadcastPort);
            int bytesSent = await _udpClient.SendAsync(messageBytes, messageBytes.Length, broadcastEndPoint);

            _logger.LogDebug("Broadcasted message {MessageType} ({BytesSent} bytes)",
                message.MessageType, bytesSent);

            return bytesSent > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast message {MessageType}", message.MessageType);
            return false;
        }
    }

    /// <summary>
    /// Dispose of the communication protocol and clean up resources
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        try
        {
            _ = StopAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during disposal");
        }

        _udpClient?.Dispose();
        _tcpListener?.Stop();
    }

    /// <summary>
    /// Get connection statistics
    /// </summary>
    /// <returns>Current communication statistics</returns>
    public CommunicationStatistics GetStatistics()
    {
        lock (_lockObject)
        {
            return new CommunicationStatistics
            {
                IsRunning = _isRunning,
                UdpPort = _config.Network.BroadcastPort,
                TcpPort = _config.Network.TcpPort,
                ActiveTcpConnections = _tcpClients.Count,
                NodeId = _nodeId ?? string.Empty
            };
        }
    }

    /// <summary>
    /// Initialize the communication protocol
    /// </summary>
    public Task InitializeAsync(string nodeId)
    {
        _nodeId = nodeId;
        _logger.LogInformation("Initializing communication protocol for node {NodeId}", nodeId);

        try
        {
            // Configure UDP client
            _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);

            // Start TCP listener
            _tcpListener.Start();

            _logger.LogInformation("Communication protocol initialized successfully");
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize communication protocol");
            throw;
        }
    }

    /// <summary>
    /// Send a message to a specific node by NodeId.
    /// Attempts direct TCP delivery; falls back to broadcast if direct addressing is unavailable.
    /// </summary>
    /// <param name="nodeId">The target node ID</param>
    /// <param name="message">The message to send</param>
    /// <returns>True if the message was sent successfully, false otherwise</returns>
    public async Task<bool> SendMessageAsync(byte nodeId, NetworkMessage message)
    {
        if (_disposed || !_isRunning)
        {
            return false;
        }

        try
        {
            // Try to get the node's IP address from the topology/discovery service
            IPAddress? targetIp = GetNodeIpAddress(nodeId);
            if (targetIp != null)
            {
                // Try direct TCP send
                bool tcpSuccess = await SendTcpMessageAsync(nodeId.ToString("X2"), message);
                if (tcpSuccess)
                {
                    _logger.LogInformation("Sent message {MessageType} to node 0x{NodeId:X2} via TCP", message.MessageType, nodeId);
                    return true;
                }
                else
                {
                    _logger.LogWarning("TCP send failed for node 0x{NodeId:X2}, falling back to broadcast", nodeId);
                }
            }
            else
            {
                _logger.LogWarning("No IP address found for node 0x{NodeId:X2}, falling back to broadcast", nodeId);
            }

            // Fallback to broadcast
            bool broadcastSuccess = await BroadcastMessageAsync(message);
            if (broadcastSuccess)
            {
                _logger.LogInformation("Sent message {MessageType} to node 0x{NodeId:X2} via broadcast fallback", message.MessageType, nodeId);
            }
            else
            {
                _logger.LogError("Failed to send message {MessageType} to node 0x{NodeId:X2} via broadcast fallback", message.MessageType, nodeId);
            }
            return broadcastSuccess;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message {MessageType} to node 0x{NodeId:X2}", message.MessageType, nodeId);
            return false;
        }
    }

    /// <summary>
    /// Send a message (legacy method - now uses broadcast)
    /// </summary>
    /// <param name="message">The message to send</param>
    /// <returns>True if the message was sent successfully, false otherwise</returns>
    [Obsolete("Use BroadcastMessageAsync for broadcasting or SendMessageAsync(nodeId, message) for direct addressing")]
    public async Task<bool> SendMessageAsync(NetworkMessage message)
    {
        _logger.LogWarning("Using deprecated SendMessageAsync(NetworkMessage) - use BroadcastMessageAsync or SendMessageAsync(nodeId, message) instead");
        return await BroadcastMessageAsync(message);
    }

    /// <summary>
    /// Send a message to a specific node by string NodeId
    /// </summary>
    /// <param name="nodeId">The target node ID as string</param>
    /// <param name="message">The message to send</param>
    /// <returns>True if the message was sent successfully, false otherwise</returns>
    public async Task<bool> SendMessageAsync(string nodeId, NetworkMessage message)
    {
        if (_disposed || !_isRunning)
        {
            return false;
        }

        try
        {
            // Parse the node ID string to get the byte value
            byte? nodeIdByte = NodeIdAllocation.ParseNodeId(nodeId);
            if (!nodeIdByte.HasValue)
            {
                _logger.LogWarning("Invalid node ID format: {NodeId}", nodeId);
                return false;
            }

            return await SendMessageAsync(nodeIdByte.Value, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message {MessageType} to node {NodeId}", message.MessageType, nodeId);
            return false;
        }
    }

    /// <summary>
    /// Send a message via TCP to a specific node
    /// </summary>
    /// <param name="nodeId">The target node ID as string (hex or decimal)</param>
    /// <param name="message">The message to send</param>
    /// <returns>True if the message was sent successfully, false otherwise</returns>
    public async Task<bool> SendTcpMessageAsync(string nodeId, NetworkMessage message)
    {
        if (_disposed || !_isRunning)
        {
            return false;
        }

        try
        {
            // Parse nodeId to byte
            byte? nodeIdByte = NodeIdAllocation.ParseNodeId(nodeId);
            if (!nodeIdByte.HasValue)
            {
                _logger.LogWarning("Invalid node ID format: {NodeId}", nodeId);
                return false;
            }

            // Look up the node's IP address (stub for now)
            IPAddress? targetIp = GetNodeIpAddress(nodeIdByte.Value);
            if (targetIp == null)
            {
                _logger.LogWarning("Could not find IP address for node 0x{NodeId:X2}", nodeIdByte.Value);
                return false;
            }

            string messageJson = JsonSerializer.Serialize(message);
            byte[] messageBytes = Encoding.UTF8.GetBytes(messageJson);

            using TcpClient tcpClient = new TcpClient();
            await tcpClient.ConnectAsync(targetIp, _config.Network.TcpPort);
            using NetworkStream stream = tcpClient.GetStream();
            await stream.WriteAsync(messageBytes, 0, messageBytes.Length);
            await stream.FlushAsync();

            _logger.LogInformation("Sent TCP message {MessageType} to node 0x{NodeId:X2} at {IpAddress}:{Port}",
                message.MessageType, nodeIdByte.Value, targetIp, _config.Network.TcpPort);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send TCP message to {NodeId}", nodeId);
            return false;
        }
    }

    /// <summary>
    /// Start the communication service
    /// </summary>
    public Task StartAsync()
    {
        if (_disposed || _isRunning)
        {
            return Task.CompletedTask;
        }

        _logger.LogInformation("Starting communication service");

        try
        {
            // Start listening for UDP broadcasts
            _ = StartUdpListenerAsync();

            // Start listening for TCP connections
            _ = StartTcpListenerAsync();

            _isRunning = true;
            _logger.LogInformation("Communication service started successfully");
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start communication service");
            throw;
        }
    }

    /// <summary>
    /// Stop the communication service
    /// </summary>
    public Task StopAsync()
    {
        if (_disposed || !_isRunning)
        {
            return Task.CompletedTask;
        }

        _logger.LogInformation("Stopping communication service");

        try
        {
            // Close all TCP connections
            lock (_lockObject)
            {
                foreach (TcpClient client in _tcpClients)
                {
                    client.Close();
                }
                _tcpClients.Clear();
            }

            // Stop TCP listener
            _tcpListener.Stop();

            // Close UDP client
            _udpClient.Close();

            _isRunning = false;
            _logger.LogInformation("Communication service stopped successfully");
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping communication service");
            throw;
        }
    }

    #endregion
}
