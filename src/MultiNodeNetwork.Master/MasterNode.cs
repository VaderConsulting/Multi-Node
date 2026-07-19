using Microsoft.Extensions.Logging;
using MultiNodeNetwork.Common.Models;
using MultiNodeNetwork.Core.Configuration;
using MultiNodeNetwork.Core.Models;
using MultiNodeNetwork.Core.Protocols;
using MultiNodeNetwork.Core.Services;

namespace MultiNodeNetwork.Master;
/// <summary>
/// Master node implementation for the 100-node WiFi network
/// </summary>
public class MasterNode : IDisposable
{
#region Fields

    private readonly Communication _communication;
    private readonly NetworkConfig _config;
    private bool _disposed = false;
    private bool _isRunning = false;
    private readonly ILogger<MasterNode> _logger;
    private readonly MasterElection _masterElection;
    private readonly MessageHandler _messageHandler;
    private readonly NodeDiscovery _nodeDiscovery;
    private string _nodeId;
    private byte _nodeIdByte;
    private readonly NodeIdManager _nodeIdManager;
    private readonly Topology _topology;
    private readonly WifiManager _wifiManager;

#endregion

#region Constructors

    public MasterNode(
        ILogger<MasterNode> logger,
        NetworkConfig config,
        MasterElection masterElection,
        NodeDiscovery nodeDiscovery,
        Communication communication,
        Topology topology,
        WifiManager wifiManager,
        MessageHandler messageHandler,
        NodeIdManager nodeIdManager)
    {
        _logger = logger;
        _config = config;
        _masterElection = masterElection;
        _nodeDiscovery = nodeDiscovery;
        _communication = communication;
        _topology = topology;
        _wifiManager = wifiManager;
        _messageHandler = messageHandler;
        _nodeIdManager = nodeIdManager;

        (_nodeId, _nodeIdByte) = GenerateNodeId();

        // Subscribe to events
        _masterElection.MasterElected += OnMasterElected;
        _masterElection.MasterAnnouncement += OnMasterAnnouncement;
        _nodeDiscovery.NodeDiscovered += OnNodeDiscovered;
        _nodeDiscovery.NodeLost += OnNodeLost;
    }

#endregion

#region Private Methods

    private double CalculateNetworkLoad(IEnumerable<DiscoveredNode> nodes)
    {
        if (!nodes.Any())
        {
            return 0.0;
        }

        double averageLoad = nodes.Average(n => n.Load);
        int totalConnections = nodes.Count();

        // Simple load calculation - can be enhanced with more sophisticated metrics
        return averageLoad * totalConnections / 100.0;
    }

    private (string nodeId, byte nodeIdByte) GenerateNodeId()
    {
        // Generate a unique node ID based on the allocation table
        // For master nodes, use the primary master ID (0x01)
        byte nodeIdByte = NodeIdAllocation.MASTER_PRIMARY;

        // Allocate the specific master node ID
        if (!_nodeIdManager.AllocateSpecificNodeId(nodeIdByte))
        {
            _logger.LogWarning("Primary master node ID 0x{NodeIdByte:X2} is already allocated, trying backup", nodeIdByte);
            nodeIdByte = NodeIdAllocation.MASTER_BACKUP;

            if (!_nodeIdManager.AllocateSpecificNodeId(nodeIdByte))
            {
                _logger.LogError("Both primary and backup master node IDs are allocated");
                throw new InvalidOperationException("No available master node IDs");
            }
        }

        string hostname = Environment.MachineName;
        string timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        string nodeId = $"MASTER_{hostname}_{timestamp}";

        return (nodeId, nodeIdByte);
    }

#endregion

#region Public Methods

    /// <summary>
    /// Allocate a node ID for a specific node type
    /// </summary>
    public byte? AllocateNodeId(NodeType nodeType)
    {
        return _nodeIdManager.AllocateNodeId(nodeType);
    }

    /// <summary>
    /// Send data to all discovered nodes
    /// </summary>
    /// <param name="data">The data to broadcast</param>
    /// <param name="priority">Message priority (0-255)</param>
    /// <returns>True if the data was sent successfully to at least one node, false otherwise</returns>
    public async Task<bool> BroadcastDataAsync(object data, int priority = 0)
    {
        if (!_isRunning)
        {
            return false;
        }

        try
        {
            List<DiscoveredNode> discoveredNodes = _nodeDiscovery.GetDiscoveredNodes()
                .ToList();

            int successCount = 0;
            foreach (DiscoveredNode node in discoveredNodes)
            {
                if (await SendDataAsync(node.NodeIdByte, data, priority))
                {
                    successCount++;
                }
            }

            _logger.LogInformation("Broadcasted data to {SuccessCount}/{TotalCount} nodes",
                successCount, discoveredNodes.Count);

            return successCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast data");
            return false;
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // Unsubscribe from events
        _masterElection.MasterElected -= OnMasterElected;
        _masterElection.MasterAnnouncement -= OnMasterAnnouncement;
        _nodeDiscovery.NodeDiscovered -= OnNodeDiscovered;
        _nodeDiscovery.NodeLost -= OnNodeLost;

        // Stop the node
        _ = StopAsync();
    }

    /// <summary>
    /// Get all discovered nodes
    /// </summary>
    public IEnumerable<DiscoveredNode> GetAllNodes()
    {
        return _nodeDiscovery.GetDiscoveredNodes();
    }

    /// <summary>
    /// Get connected nodes
    /// </summary>
    public List<byte> GetConnectedNodes(byte nodeIdByte)
    {
        return _topology.GetConnectedNodes(nodeIdByte);
    }

    /// <summary>
    /// Get network statistics
    /// </summary>
    public NetworkStatistics GetNetworkStatistics()
    {
        List<DiscoveredNode> discoveredNodes = _nodeDiscovery.GetDiscoveredNodes().ToList();
        TopologyMap topology = _topology.GetCurrentTopology();
        TopologyStatistics topologyStats = _topology.GetStatistics();
        NodeIdAllocationStatistics nodeIdStats = _nodeIdManager.GetStatistics();

        return new NetworkStatistics
        {
            NodeId = _nodeId,
            NodeIdByte = _nodeIdByte,
            IsMaster = _masterElection.IsMaster,
            CurrentMasterId = _masterElection.CurrentMasterId,
            CurrentMasterNodeId = _masterElection.CurrentMasterNodeId,
            TotalNodes = discoveredNodes.Count,
            OnlineNodes = discoveredNodes.Count(n => n.Status == NodeStatus.Online),
            TopologyVersion = topology.Version,
            Uptime = DateTime.UtcNow - _masterElection.MasterStartTime,
            NetworkLoad = CalculateNetworkLoad(discoveredNodes),
            NodeTypeCounts = topologyStats.NodeTypeCounts.ToDictionary(
                kvp => (NodeType)Enum.Parse(typeof(NodeType), kvp.Key),
                kvp => kvp.Value),
            NodeIdAllocationStats = nodeIdStats
        };
    }

    /// <summary>
    /// Get network topology
    /// </summary>
    public TopologyMap GetNetworkTopology()
    {
        return _topology.GetCurrentTopology();
    }

    /// <summary>
    /// Get node ID allocation statistics
    /// </summary>
    public NodeIdAllocationStatistics GetNodeIdAllocationStatistics()
    {
        return _nodeIdManager.GetStatistics();
    }

    /// <summary>
    /// Get node information
    /// </summary>
    public DiscoveredNode? GetNodeInfo(byte nodeIdByte)
    {
        return _nodeDiscovery.GetNode(nodeIdByte);
    }

    /// <summary>
    /// Get node information by string ID
    /// </summary>
    public DiscoveredNode? GetNodeInfo(string nodeId)
    {
        // Try to find by string ID (fallback for backward compatibility)
        return _nodeDiscovery.GetDiscoveredNodes().FirstOrDefault(n => n.NodeId == nodeId);
    }

    /// <summary>
    /// Get nodes by type
    /// </summary>
    public IEnumerable<DiscoveredNode> GetNodesByType(NodeType nodeType)
    {
        return _nodeDiscovery.GetNodesByType(nodeType);
    }

    /// <summary>
    /// Get route to a specific node
    /// </summary>
    public List<byte> GetRouteToNode(byte destinationNodeId)
    {
        return _topology.GetRoute(_nodeIdByte, destinationNodeId);
    }

    /// <summary>
    /// Get topology statistics
    /// </summary>
    public TopologyStatistics GetTopologyStatistics()
    {
        return _topology.GetStatistics();
    }

    /// <summary>
    /// Check if node is online
    /// </summary>
    public bool IsNodeOnline(byte nodeIdByte)
    {
        return _nodeDiscovery.IsNodeOnline(nodeIdByte);
    }

    /// <summary>
    /// Release a node ID
    /// </summary>
    public bool ReleaseNodeId(byte nodeIdByte)
    {
        return _nodeIdManager.ReleaseNodeId(nodeIdByte);
    }

    /// <summary>
    /// Send data to a specific node
    /// </summary>
    /// <param name="destinationNodeId">The target node ID</param>
    /// <param name="data">The data to send</param>
    /// <param name="priority">Message priority (0-255)</param>
    /// <returns>True if the data was sent successfully, false otherwise</returns>
    public async Task<bool> SendDataAsync(byte destinationNodeId, object data, int priority = 0)
    {
        if (!_isRunning)
        {
            return false;
        }

        try
        {
            DataTransmissionMessage message = new DataTransmissionMessage
            {
                SenderId = _nodeId,
                SenderNodeId = _nodeIdByte,
                DestinationId = $"Node_{destinationNodeId:X2}",
                DestinationNodeId = destinationNodeId,
                Payload = data,
                Priority = priority
            };

            return await _messageHandler.SendMessageAsync(destinationNodeId, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send data to node 0x{DestinationNodeId:X2}", destinationNodeId);
            return false;
        }
    }

    /// <summary>
    /// Send data to a specific node by string ID
    /// </summary>
    /// <param name="destinationNodeId">The target node ID as string</param>
    /// <param name="data">The data to send</param>
    /// <param name="priority">Message priority (0-255)</param>
    /// <returns>True if the data was sent successfully, false otherwise</returns>
    public async Task<bool> SendDataAsync(string destinationNodeId, object data, int priority = 0)
    {
        if (!_isRunning)
        {
            return false;
        }

        try
        {
            DataTransmissionMessage message = new DataTransmissionMessage
            {
                SenderId = _nodeId,
                SenderNodeId = _nodeIdByte,
                DestinationId = destinationNodeId,
                Payload = data,
                Priority = priority
            };

            return await _messageHandler.SendMessageAsync(destinationNodeId, message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send data to node {DestinationNodeId}", destinationNodeId);
            return false;
        }
    }

    /// <summary>
    /// Start the master node
    /// </summary>
    public async Task StartAsync()
    {
        if (_disposed || _isRunning)
        {
            return;
        }

        _logger.LogInformation("Starting master node {NodeId} (0x{NodeIdByte:X2})", _nodeId, _nodeIdByte);

        try
        {
            // Initialize WiFi connection
            await _wifiManager.ConnectAsync(_config.Network.Ssid, _config.Network.Password);

            // Initialize protocols
            await _masterElection.InitializeAsync(_nodeId, _nodeIdByte);
            await _nodeDiscovery.InitializeAsync(_nodeId, _nodeIdByte);
            await _communication.InitializeAsync(_nodeId);
            await _topology.InitializeAsync(_nodeId, _nodeIdByte);

            // Start message handling
            await _messageHandler.StartAsync();

            _isRunning = true;
            _logger.LogInformation("Master node {NodeId} (0x{NodeIdByte:X2}) started successfully", _nodeId, _nodeIdByte);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start master node {NodeId} (0x{NodeIdByte:X2})", _nodeId, _nodeIdByte);
            throw;
        }
    }

    /// <summary>
    /// Stop the master node
    /// </summary>
    public async Task StopAsync()
    {
        if (_disposed || !_isRunning)
        {
            return;
        }

        _logger.LogInformation("Stopping master node {NodeId} (0x{NodeIdByte:X2})", _nodeId, _nodeIdByte);

        try
        {
            // Stop all services
            await _messageHandler.StopAsync();
            await _topology.StopAsync();
            await _communication.StopAsync();
            await _nodeDiscovery.StopDiscoveryAsync();
            await _wifiManager.DisconnectAsync();

            // Release the node ID
            _nodeIdManager.ReleaseNodeId(_nodeIdByte);

            _isRunning = false;
            _logger.LogInformation("Master node {NodeId} (0x{NodeIdByte:X2}) stopped successfully", _nodeId, _nodeIdByte);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping master node {NodeId} (0x{NodeIdByte:X2})", _nodeId, _nodeIdByte);
            throw;
        }
    }

#endregion

#region Event Handlers

    private void OnMasterAnnouncement(object? sender, MasterAnnouncementEventArgs e)
    {
        _logger.LogDebug("Master announcement received from {MasterId} (0x{MasterNodeId:X2})",
            e.MasterId, e.MasterNodeId);
    }

    private void OnMasterElected(object? sender, MasterElectedEventArgs e)
    {
        _logger.LogInformation("Master election completed. New master: {MasterId} (0x{MasterNodeId:X2})",
            e.MasterId, e.MasterNodeId);

        if (e.MasterId == _nodeId)
        {
            _logger.LogInformation("This node is now the master");
            // Additional master-specific initialization can be done here
        }
    }

    private void OnNodeDiscovered(object? sender, NodeDiscoveredEventArgs e)
    {
        _logger.LogInformation("Node discovered: {NodeId} (0x{NodeIdByte:X2}) - {NodeType} at signal strength {SignalStrength}dBm",
            e.Node.NodeId, e.Node.NodeIdByte, e.Node.NodeType, e.Node.SignalStrength);

        // Update topology with new node
        _topology.AddNode(e.Node);
    }

    private void OnNodeLost(object? sender, NodeLostEventArgs e)
    {
        _logger.LogWarning("Node lost: 0x{NodeIdByte:X2}", e.NodeIdByte);

        // Update topology to remove lost node
        _topology.RemoveNode(e.NodeIdByte);
    }

#endregion
}
