using Microsoft.Extensions.Logging;
using MultiNodeNetwork.Common.Events;
using MultiNodeNetwork.Common.Models;
using MultiNodeNetwork.Core.Configuration;
using MultiNodeNetwork.Core.Models;
using MultiNodeNetwork.Core.Protocols;
using System.Text.Json;

namespace MultiNodeNetwork.Core.Services;
/// <summary>
/// Handles message processing and routing for the network
/// </summary>
public class MessageHandler : IDisposable
{
    #region Events

    // Event for data received by this node
    public event EventHandler<DataReceivedEventArgs>? DataReceived;

    public event EventHandler<MessageProcessedEventArgs>? MessageProcessed;

    public event EventHandler<MessageReceivedEventArgs>? MessageReceived;

    #endregion

    #region Fields

    private readonly Communication _communication;
    private readonly NetworkConfig _config;
    private bool _disposed = false;
    private bool _isRunning = false;
    private readonly ILogger<MessageHandler> _logger;
    private readonly IMasterElection _masterElection;
    private readonly NodeDiscovery _nodeDiscovery;
    private readonly Topology _topology;

    #endregion

    #region Constructors

    public MessageHandler(
        ILogger<MessageHandler> logger,
        NetworkConfig config,
        IMasterElection masterElection,
        NodeDiscovery nodeDiscovery,
        Communication communication,
        Topology topology)
    {
        _logger = logger;
        _config = config;
        _masterElection = masterElection;
        _nodeDiscovery = nodeDiscovery;
        _communication = communication;
        _topology = topology;

        // Subscribe to communication events
        _communication.MessageReceived += OnMessageReceived;
    }

    #endregion

    #region Private Methods

    private double CalculateRouteQuality(List<byte> route)
    {
        if (route.Count <= 1)
        {
            return 1.0; // Direct connection
        }

        // Calculate route quality based on hop count and signal strength
        // For now, use a simple formula: 1.0 / (hop count + 1)
        return 1.0 / (route.Count + 1);
    }

    private NetworkMessage? DeserializeMessage(string messageJson)
    {
        try
        {
            // First, deserialize to get the message type
            using JsonDocument document = JsonDocument.Parse(messageJson);
            string? messageType = document.RootElement.GetProperty("messageType").GetString();

            if (string.IsNullOrEmpty(messageType))
            {
                _logger.LogWarning("Message type is null or empty");
                return null;
            }

            // Deserialize to the appropriate message type
            return messageType switch
            {
                "DISCOVERY" => JsonSerializer.Deserialize<DiscoveryMessage>(messageJson),
                "HEARTBEAT" => JsonSerializer.Deserialize<HeartbeatMessage>(messageJson),
                "MASTER_ANNOUNCEMENT" => JsonSerializer.Deserialize<MasterAnnouncementMessage>(messageJson),
                "NODE_REGISTRATION" => JsonSerializer.Deserialize<NodeRegistrationMessage>(messageJson),
                "TOPOLOGY_UPDATE" => JsonSerializer.Deserialize<TopologyUpdateMessage>(messageJson),
                "DATA_TRANSMISSION" => JsonSerializer.Deserialize<DataTransmissionMessage>(messageJson),
                "ROUTE_REQUEST" => JsonSerializer.Deserialize<RouteRequestMessage>(messageJson),
                "ROUTE_REPLY" => JsonSerializer.Deserialize<RouteReplyMessage>(messageJson),
                "ROUTE_ERROR" => JsonSerializer.Deserialize<RouteErrorMessage>(messageJson),
                "MASTER_ELECTION" => JsonSerializer.Deserialize<MasterElectionMessage>(messageJson),
                "ELECTION_VOTE" => JsonSerializer.Deserialize<ElectionVoteMessage>(messageJson),
                "ELECTION_RESULT" => JsonSerializer.Deserialize<ElectionResultMessage>(messageJson),
                "IP_ADDRESS_UPDATE" => JsonSerializer.Deserialize<IpAddressUpdateMessage>(messageJson),
                _ => null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deserialize message");
            return null;
        }
    }

    private string GetDestinationId(NetworkMessage message)
    {
        return message switch
        {
            DataTransmissionMessage dataMessage => dataMessage.DestinationId,
            RouteRequestMessage routeMessage => routeMessage.DestinationId,
            _ => "BROADCAST"
        };
    }

    private async Task HandleDataTransmissionAsync(DataTransmissionMessage message)
    {
        _logger.LogDebug("Data transmission received from {SenderId} to {DestinationId}",
            message.SenderId, message.DestinationId);

        try
        {
            // Check if this message is for us
            if (message.DestinationId == _nodeDiscovery.GetMyNodeId() ||
                message.DestinationNodeId == _nodeDiscovery.GetMyNodeIdByte())
            {
                // Message is for this node - process it
                _logger.LogInformation("Processing data message for this node: {PayloadType}",
                    message.Payload?.GetType().Name ?? "Unknown");

                // Here you would typically invoke a callback or event to handle the data
                // For now, we'll just log it
                DataReceived?.Invoke(this, new DataReceivedEventArgs(message));
            }
            else
            {
                // Message is for another node - route it
                List<byte> route = _topology.GetRoute(_nodeDiscovery.GetMyNodeIdByte(), message.DestinationNodeId);

                if (route.Count > 0)
                {
                    // Forward the message to the next hop
                    byte nextHop = route[1]; // First element is current node, second is next hop

                    _logger.LogDebug("Forwarding data message to next hop 0x{NextHop:X2} for destination 0x{Destination:X2}",
                        nextHop, message.DestinationNodeId);

                    await _communication.SendMessageAsync(nextHop, message);
                }
                else
                {
                    _logger.LogWarning("No route found to destination 0x{Destination:X2}", message.DestinationNodeId);

                    // Send route error message back to sender
                    RouteErrorMessage errorMessage = new RouteErrorMessage
                    {
                        SenderId = _nodeDiscovery.GetMyNodeId(),
                        SenderNodeId = _nodeDiscovery.GetMyNodeIdByte(),
                        OriginalDestinationId = message.DestinationId,
                        OriginalDestinationNodeId = message.DestinationNodeId,
                        ErrorCode = RouteErrorCode.NoRouteToDestination,
                        ErrorMessage = $"No route found to destination 0x{message.DestinationNodeId:X2}",
                        ErrorNodeId = _nodeDiscovery.GetMyNodeId(),
                        ErrorNodeIdByte = _nodeDiscovery.GetMyNodeIdByte()
                    };

                    await _communication.BroadcastMessageAsync(errorMessage);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing data transmission from {SenderId} to {DestinationId}",
                message.SenderId, message.DestinationId);
        }
    }

    /// <summary>
    /// Handle IP address update messages from nodes
    /// </summary>
    private async Task HandleIpAddressUpdateAsync(IpAddressUpdateMessage message)
    {
        _logger.LogInformation("IP address update received from node 0x{NodeId:X2}: {OldIp} -> {NewIp}",
            message.NodeId, message.OldIpAddress, message.NewIpAddress);

        try
        {
            // Update the node's IP address in the topology
            bool updated = _topology.UpdateNodeIpAddress(message.NodeId, message.NewIpAddress, message.MacAddress, message.InterfaceName);

            if (updated)
            {
                _logger.LogInformation("Successfully updated IP address for node 0x{NodeId:X2} from {OldIp} to {NewIp}",
                    message.NodeId, message.OldIpAddress, message.NewIpAddress);

                // Send confirmation back to the node
                IpAddressUpdateMessage confirmationMessage = new IpAddressUpdateMessage
                {
                    SenderId = _nodeDiscovery.GetMyNodeId(),
                    SenderNodeId = _nodeDiscovery.GetMyNodeIdByte(),
                    NodeId = message.NodeId,
                    OldIpAddress = message.OldIpAddress,
                    NewIpAddress = message.NewIpAddress,
                    MacAddress = message.MacAddress,
                    InterfaceName = message.InterfaceName,
                    Timestamp = DateTime.UtcNow
                };

                await _communication.BroadcastMessageAsync(confirmationMessage);
            }
            else
            {
                _logger.LogWarning("Failed to update IP address for node 0x{NodeId:X2} - node not found in topology",
                    message.NodeId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing IP address update from node 0x{NodeId:X2}", message.NodeId);
        }
    }

    private async Task HandleNodeRegistrationAsync(NodeRegistrationMessage message)
    {
        _logger.LogInformation("Node registration received from {NodeId} (0x{NodeIdByte:X2}) at IP {IpAddress}",
            message.SenderId, message.SenderNodeId, message.IpAddress);

        try
        {
            // Create node info from registration message
            NodeInfo nodeInfo = new NodeInfo
            {
                Id = message.SenderId,
                NodeId = message.SenderNodeId,
                Name = message.SenderId,
                Type = message.NodeInfo.Type,
                Capabilities = message.NodeInfo.Capabilities,
                SignalStrength = message.SignalStrength,
                BatteryLevel = message.NodeInfo.BatteryLevel,
                Location = message.Position ?? message.NodeInfo.Location,
                LastSeen = DateTime.UtcNow,
                Status = NodeStatus.Online,
                IpAddress = message.IpAddress,
                MacAddress = message.MacAddress,
                InterfaceName = message.InterfaceName
            };

            // Add node to topology
            _topology.AddNode(nodeInfo);

            // Send registration confirmation
            NodeRegistrationMessage confirmationMessage = new NodeRegistrationMessage
            {
                SenderId = _nodeDiscovery.GetMyNodeId(),
                SenderNodeId = _nodeDiscovery.GetMyNodeIdByte(),
                NodeInfo = new NodeInfo
                {
                    Id = _nodeDiscovery.GetMyNodeId(),
                    NodeId = _nodeDiscovery.GetMyNodeIdByte(),
                    Name = _nodeDiscovery.GetMyNodeId(),
                    Type = _nodeDiscovery.GetMyNodeType(),
                    Capabilities = _nodeDiscovery.GetMyCapabilities().Keys.ToList(),
                    SignalStrength = _nodeDiscovery.GetMySignalStrength(),
                    BatteryLevel = _nodeDiscovery.GetMyBatteryLevel(),
                    Location = _nodeDiscovery.GetMyLocation(),
                    LastSeen = DateTime.UtcNow,
                    Status = NodeStatus.Online
                },
                SignalStrength = _nodeDiscovery.GetMySignalStrength(),
                Position = _nodeDiscovery.GetMyLocation()
            };

            await _communication.BroadcastMessageAsync(confirmationMessage);

            _logger.LogInformation("Node registration processed and confirmation sent to {NodeId} at {IpAddress}",
                message.SenderId, message.IpAddress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing node registration from {NodeId}", message.SenderId);
        }
    }

    private async Task HandleRouteRequestAsync(RouteRequestMessage message)
    {
        _logger.LogDebug("Route request received from {SenderId} for {DestinationId}",
            message.SenderId, message.DestinationId);

        try
        {
            // Check if we have a route to the destination
            List<byte> route = _topology.GetRoute(_nodeDiscovery.GetMyNodeIdByte(), message.DestinationNodeId);

            if (route.Count > 0)
            {
                // We have a route - send route reply
                RouteReplyMessage replyMessage = new RouteReplyMessage
                {
                    SenderId = _nodeDiscovery.GetMyNodeId(),
                    SenderNodeId = _nodeDiscovery.GetMyNodeIdByte(),
                    RequestId = message.MessageId,
                    Route = route,
                    Cost = CalculateRouteQuality(route)
                };

                await _communication.BroadcastMessageAsync(replyMessage);

                _logger.LogDebug("Route reply sent to {SenderId} for destination {DestinationId} with {HopCount} hops",
                    message.SenderId, message.DestinationId, route.Count - 1);
            }
            else
            {
                // We don't have a route - forward the request to our neighbors
                _logger.LogDebug("No route found for {DestinationId}, forwarding route request", message.DestinationId);

                // Add our node to the route and forward
                message.HopCount++;

                await _communication.BroadcastMessageAsync(message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing route request from {SenderId} for {DestinationId}",
                message.SenderId, message.DestinationId);
        }
    }

    private async Task HandleTopologyUpdateAsync(TopologyUpdateMessage message)
    {
        _logger.LogDebug("Topology update received from {SenderId}, version {Version}",
            message.SenderId, message.Version);

        try
        {
            // Check if this topology update is newer than our current version
            TopologyMap currentTopology = _topology.GetCurrentTopology();

            if (message.Version > currentTopology.Version)
            {
                // Update our topology with the new information
                foreach (KeyValuePair<byte, NodeInfo> nodeEntry in message.Topology.Nodes)
                {
                    _topology.AddNode(nodeEntry.Value);
                }

                foreach (Connection connection in message.Topology.Connections)
                {
                    _topology.AddConnection(connection.FromNode, connection.ToNode, connection.SignalStrength);
                }

                _logger.LogInformation("Topology updated to version {Version} with {NodeCount} nodes and {ConnectionCount} connections",
                    message.Version, message.Topology.Nodes.Count, message.Topology.Connections.Count);

                // Forward the topology update to other nodes
                await _communication.BroadcastMessageAsync(message);
            }
            else
            {
                _logger.LogDebug("Ignoring topology update version {ReceivedVersion} (current: {CurrentVersion})",
                    message.Version, currentTopology.Version);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing topology update from {SenderId}", message.SenderId);
        }
    }

    private async Task<bool> RouteMessageAsync(NetworkMessage message)
    {
        try
        {
            switch (message.MessageType)
            {
                case "DISCOVERY":
                    if (message is DiscoveryMessage discoveryMessage)
                    {
                        await _nodeDiscovery.HandleDiscoveryMessageAsync(discoveryMessage);
                        return true;
                    }

                    break;

                case "HEARTBEAT":
                    if (message is HeartbeatMessage heartbeatMessage)
                    {
                        await _nodeDiscovery.HandleHeartbeatMessageAsync(heartbeatMessage);
                        return true;
                    }

                    break;

                case "MASTER_ANNOUNCEMENT":
                    if (message is MasterAnnouncementMessage announcementMessage)
                    {
                        await _masterElection.HandleMasterAnnouncementAsync(announcementMessage);
                        return true;
                    }

                    break;

                case "MASTER_ELECTION":
                    if (message is MasterElectionMessage electionMessage)
                    {
                        await _masterElection.HandleElectionMessageAsync(electionMessage);
                        return true;
                    }

                    break;

                case "ELECTION_VOTE":
                    if (message is ElectionVoteMessage voteMessage)
                    {
                        await _masterElection.HandleVoteMessageAsync(voteMessage);
                        return true;
                    }

                    break;

                case "NODE_REGISTRATION":
                    if (message is NodeRegistrationMessage registrationMessage)
                    {
                        await HandleNodeRegistrationAsync(registrationMessage);
                        return true;
                    }

                    break;

                case "DATA_TRANSMISSION":
                    if (message is DataTransmissionMessage dataMessage)
                    {
                        await HandleDataTransmissionAsync(dataMessage);
                        return true;
                    }

                    break;

                case "ROUTE_REQUEST":
                    if (message is RouteRequestMessage routeRequestMessage)
                    {
                        await HandleRouteRequestAsync(routeRequestMessage);
                        return true;
                    }

                    break;

                case "TOPOLOGY_UPDATE":
                    if (message is TopologyUpdateMessage topologyMessage)
                    {
                        await HandleTopologyUpdateAsync(topologyMessage);
                        return true;
                    }

                    break;

                case "IP_ADDRESS_UPDATE":
                    if (message is IpAddressUpdateMessage ipUpdateMessage)
                    {
                        await HandleIpAddressUpdateAsync(ipUpdateMessage);
                        return true;
                    }

                    break;

                default:
                    _logger.LogWarning("Unknown message type: {MessageType}", message.MessageType);
                    return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error routing message of type {MessageType}", message.MessageType);
            return false;
        }

        return false;
    }

    #endregion

    #region Public Methods

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // Stop the handler
        _ = StopAsync().ConfigureAwait(false);
    }

    /// <summary>
    /// Process a message from JSON
    /// </summary>
    public async Task<bool> ProcessMessageAsync(string messageJson)
    {
        if (_disposed || !_isRunning)
        {
            return false;
        }

        try
        {
            _logger.LogDebug("Processing message: {MessageLength} characters", messageJson.Length);

            NetworkMessage? message = DeserializeMessage(messageJson);
            if (message == null)
            {
                _logger.LogWarning("Failed to deserialize message");
                return false;
            }

            MessageReceived?.Invoke(this, new MessageReceivedEventArgs(messageJson));

            bool success = await RouteMessageAsync(message);

            MessageProcessed?.Invoke(this, new MessageProcessedEventArgs(message, success));

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message");
            return false;
        }
    }

    /// <summary>
    /// Send a message to a specific node
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
            _logger.LogDebug("Sending message: {MessageType} to node 0x{NodeId:X2}",
                message.MessageType, nodeId);

            return await _communication.SendMessageAsync(nodeId, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to node 0x{NodeId:X2}", nodeId);
            return false;
        }
    }

    /// <summary>
    /// Send a message (legacy method - now uses broadcast)
    /// </summary>
    /// <param name="message">The message to send</param>
    /// <returns>True if the message was sent successfully, false otherwise</returns>
    [Obsolete("Use SendMessageAsync(nodeId, message) for direct addressing or BroadcastMessageAsync for broadcasting")]
    public async Task<bool> SendMessageAsync(NetworkMessage message)
    {
        if (_disposed || !_isRunning)
        {
            return false;
        }

        try
        {
            _logger.LogDebug("Sending message: {MessageType} via broadcast",
                message.MessageType);

            return await _communication.BroadcastMessageAsync(message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message");
            return false;
        }
    }

    /// <summary>
    /// Send a message to a specific node by string ID
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
            _logger.LogDebug("Sending message: {MessageType} to node {NodeId}",
                message.MessageType, nodeId);

            return await _communication.SendMessageAsync(nodeId, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to node {NodeId}", nodeId);
            return false;
        }
    }

    /// <summary>
    /// Start the message handler
    /// </summary>
    public async Task StartAsync()
    {
        if (_disposed)
        {
            return;
        }

        _logger.LogInformation("Starting message handler");
        _isRunning = true;

        // Start topology service if not already running
        await _topology.StartAsync();
    }

    /// <summary>
    /// Stop the message handler
    /// </summary>
    public Task StopAsync()
    {
        if (_disposed)
        {
            return Task.CompletedTask;
        }

        _logger.LogInformation("Stopping message handler");
        _isRunning = false;

        // Unsubscribe from communication events
        _communication.MessageReceived -= OnMessageReceived;
        return Task.CompletedTask;
    }

    #endregion

    #region Event Handlers

    private void OnMessageReceived(object? sender, MessageReceivedEventArgs e)
    {
        _ = ProcessMessageAsync(e.MessageJson);
    }

    #endregion
}
