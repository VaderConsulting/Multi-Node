using Microsoft.Extensions.Logging;
using MultiNodeNetwork.Common.Models;
using MultiNodeNetwork.Core.Configuration;
using MultiNodeNetwork.Core.Models;
using MultiNodeNetwork.Core.Services;
using System.Collections.Concurrent;

namespace MultiNodeNetwork.Core.Protocols;
/// <summary>
/// Network topology management protocol
/// </summary>
public class Topology : IDisposable
{
#region Events

    public event EventHandler<NodeAddedEventArgs>? NodeAdded;

    public event EventHandler<NodeRemovedEventArgs>? NodeRemoved;

    public event EventHandler<TopologyUpdatedEventArgs>? TopologyUpdated;

#endregion

#region Fields

    private readonly NetworkConfig _config;
    private bool _disposed = false;
    private bool _isRunning = false;
    private readonly object _lockObject = new();
    private readonly ILogger<Topology> _logger;
    private string? _nodeId;
    private byte _nodeIdByte = 0;
    private readonly TopologyMap _topologyMap;

#endregion

#region Constructors

    public Topology(ILogger<Topology> logger, NetworkConfig config)
    {
        _logger = logger;
        _config = config;
        _topologyMap = new TopologyMap
        {
            Nodes = [],
            Connections = [],
            Version = 0,
            Timestamp = DateTime.UtcNow
        };
    }

#endregion

#region Private Methods

    private List<string> GetNodeCapabilities(byte nodeIdByte)
    {
        NodeType nodeType = NodeIdAllocation.GetNodeType(nodeIdByte);
        List<string> capabilities = ["version:1.0.0"];

        switch (nodeType)
        {
            case NodeType.Master:
                capabilities.AddRange(new[] { "canBeMaster", "canRoute", "maxConnections:50" });
                break;
            case NodeType.Repeater:
                capabilities.AddRange(new[] { "canRoute", "maxConnections:10" });
                break;
            case NodeType.UserDevice:
                capabilities.AddRange(new[] { "maxConnections:1" });
                break;
        }

        return capabilities;
    }

    private string GetSignalQuality(int signalStrength)
    {
        return signalStrength switch
        {
            >= -50 => "EXCELLENT",
            >= -60 => "GOOD",
            >= -70 => "FAIR",
            >= -80 => "POOR",
            _ => "VERY_POOR"
        };
    }

    private void IncrementTopologyVersion()
    {
        _topologyMap.Version++;
        _topologyMap.Timestamp = DateTime.UtcNow;

        TopologyUpdated?.Invoke(this, new TopologyUpdatedEventArgs(_topologyMap.Version, _topologyMap.Timestamp));
    }

#endregion

#region Public Methods

    /// <summary>
    /// Add a connection between nodes
    /// </summary>
    public void AddConnection(byte fromNode, byte toNode, int signalStrength)
    {
        if (_disposed)
        {
            return;
        }

        lock (_lockObject)
        {
            // Check if connection already exists
            Connection? existingConnection = _topologyMap.Connections
                .FirstOrDefault(c => (c.FromNode == fromNode && c.ToNode == toNode) ||
                                   (c.FromNode == toNode && c.ToNode == fromNode));

            if (existingConnection != null)
            {
                // Update existing connection
                existingConnection.SignalStrength = signalStrength;
                existingConnection.LastSeen = DateTime.UtcNow;
                existingConnection.Quality = GetSignalQuality(signalStrength);

                _logger.LogDebug("Updated connection: 0x{FromNode:X2} -> 0x{ToNode:X2} ({SignalStrength}dBm)",
                    fromNode, toNode, signalStrength);
            }
            else
            {
                // Add new connection
                Connection connection = new Connection
                {
                    FromNode = fromNode,
                    ToNode = toNode,
                    SignalStrength = signalStrength,
                    Quality = GetSignalQuality(signalStrength),
                    LastSeen = DateTime.UtcNow
                };

                _topologyMap.Connections.Add(connection);

                _logger.LogInformation("Added connection: 0x{FromNode:X2} -> 0x{ToNode:X2} ({SignalStrength}dBm)",
                    fromNode, toNode, signalStrength);
            }
        }
    }

    /// <summary>
    /// Add a node to the topology
    /// </summary>
    public void AddNode(DiscoveredNode discoveredNode)
    {
        if (_disposed)
        {
            return;
        }

        lock (_lockObject)
        {
            NodeInfo nodeInfo = new NodeInfo
            {
                Id = discoveredNode.NodeId,
                NodeId = discoveredNode.NodeIdByte,
                Name = $"Node_{discoveredNode.NodeIdByte:X2}",
                Type = discoveredNode.NodeType,
                Version = "1.0.0",
                Capabilities = discoveredNode.Capabilities.Keys.ToList(),
                Location = discoveredNode.Location,
                SignalStrength = discoveredNode.SignalStrength,
                BatteryLevel = discoveredNode.BatteryLevel,
                LastSeen = discoveredNode.LastSeen,
                Status = discoveredNode.Status
            };

            if (_topologyMap.Nodes.TryAdd(discoveredNode.NodeIdByte, nodeInfo))
            {
                _logger.LogInformation("Added node to topology: {NodeId} (0x{NodeIdByte:X2}) - {NodeType}",
                    discoveredNode.NodeId, discoveredNode.NodeIdByte, discoveredNode.NodeType);

                NodeAdded?.Invoke(this, new NodeAddedEventArgs(discoveredNode.NodeIdByte, nodeInfo));
                IncrementTopologyVersion();
            }
        }
    }

    /// <summary>
    /// Add a node to the topology
    /// </summary>
    public void AddNode(NodeInfo nodeInfo)
    {
        if (_disposed)
        {
            return;
        }

        lock (_lockObject)
        {
            if (_topologyMap.Nodes.TryAdd(nodeInfo.NodeId, nodeInfo))
            {
                _logger.LogInformation("Added node to topology: {NodeId} (0x{NodeIdByte:X2}) - {NodeType}",
                    nodeInfo.Id, nodeInfo.NodeId, nodeInfo.Type);

                NodeAdded?.Invoke(this, new NodeAddedEventArgs(nodeInfo.NodeId, nodeInfo));
                IncrementTopologyVersion();
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // Stop the service
        _ = StopAsync();
    }

    /// <summary>
    /// Get nodes connected to a specific node
    /// </summary>
    public List<byte> GetConnectedNodes(byte nodeIdByte)
    {
        lock (_lockObject)
        {
            return _topologyMap.Connections
                .Where(c => c.FromNode == nodeIdByte || c.ToNode == nodeIdByte)
                .Select(c => c.FromNode == nodeIdByte ? c.ToNode : c.FromNode)
                .Distinct()
                .ToList();
        }
    }

    /// <summary>
    /// Get current topology map
    /// </summary>
    public TopologyMap GetCurrentTopology()
    {
        lock (_lockObject)
        {
            return new TopologyMap
            {
                Nodes = new Dictionary<byte, NodeInfo>(_topologyMap.Nodes),
                Connections = new List<Connection>(_topologyMap.Connections),
                Version = _topologyMap.Version,
                Timestamp = _topologyMap.Timestamp
            };
        }
    }

    /// <summary>
    /// Get all master nodes
    /// </summary>
    public IEnumerable<NodeInfo> GetMasterNodes()
    {
        return GetNodesByType(NodeType.Master);
    }

    /// <summary>
    /// Get node information
    /// </summary>
    public NodeInfo? GetNode(byte nodeIdByte)
    {
        lock (_lockObject)
        {
            _topologyMap.Nodes.TryGetValue(nodeIdByte, out NodeInfo? nodeInfo);
            return nodeInfo;
        }
    }

    /// <summary>
    /// Get nodes by type
    /// </summary>
    public IEnumerable<NodeInfo> GetNodesByType(NodeType nodeType)
    {
        lock (_lockObject)
        {
            return _topologyMap.Nodes.Values.Where(n => n.Type == nodeType);
        }
    }

    /// <summary>
    /// Get all repeater nodes
    /// </summary>
    public IEnumerable<NodeInfo> GetRepeaterNodes()
    {
        return GetNodesByType(NodeType.Repeater);
    }

    /// <summary>
    /// Get route between two nodes
    /// </summary>
    public List<byte> GetRoute(byte sourceNode, byte destinationNode)
    {
        if (sourceNode == destinationNode)
        {
            return [sourceNode];
        }

        lock (_lockObject)
        {
            // Simple BFS implementation for routing
            HashSet<byte> visited = [];
            Queue<List<byte>> queue = new Queue<List<byte>>();
            queue.Enqueue([sourceNode]);

            while (queue.Count > 0)
            {
                List<byte> path = queue.Dequeue();
                byte currentNode = path[^1];

                if (currentNode == destinationNode)
                {
                    return path;
                }

                if (visited.Contains(currentNode))
                {
                    continue;
                }

                visited.Add(currentNode);

                List<byte> neighbors = GetConnectedNodes(currentNode);
                foreach (byte neighbor in neighbors)
                {
                    if (!visited.Contains(neighbor))
                    {
                        List<byte> newPath = new List<byte>(path) { neighbor };
                        queue.Enqueue(newPath);
                    }
                }
            }

            return []; // No route found
        }
    }

    /// <summary>
    /// Get topology statistics
    /// </summary>
    public TopologyStatistics GetStatistics()
    {
        lock (_lockObject)
        {
            int totalNodes = _topologyMap.Nodes.Count;
            int totalConnections = _topologyMap.Connections.Count;
            double averageConnections = totalNodes > 0 ? (double)totalConnections / totalNodes : 0;

            Dictionary<string, int> nodeTypeCounts = _topologyMap.Nodes.Values
                .GroupBy(n => n.Type.ToString())
                .ToDictionary(g => g.Key, g => g.Count());

            return new TopologyStatistics
            {
                TotalNodes = totalNodes,
                TotalConnections = totalConnections,
                AverageConnectionsPerNode = averageConnections,
                TopologyVersion = _topologyMap.Version,
                LastUpdated = _topologyMap.Timestamp,
                NodeTypeCounts = nodeTypeCounts
            };
        }
    }

    /// <summary>
    /// Get all user devices
    /// </summary>
    public IEnumerable<NodeInfo> GetUserDevices()
    {
        return GetNodesByType(NodeType.UserDevice);
    }

    /// <summary>
    /// Check if a node exists in the topology
    /// </summary>
    public bool HasNode(byte nodeIdByte)
    {
        lock (_lockObject)
        {
            return _topologyMap.Nodes.ContainsKey(nodeIdByte);
        }
    }

    /// <summary>
    /// Initialize the topology protocol
    /// </summary>
    public async Task InitializeAsync(string nodeId, byte nodeIdByte)
    {
        _nodeId = nodeId;
        _nodeIdByte = nodeIdByte;
        _logger.LogInformation("Initializing topology protocol for node {NodeId} (0x{NodeIdByte:X2})",
            nodeId, nodeIdByte);

        // Add this node to the topology
        AddNode(new NodeInfo
        {
            Id = nodeId,
            NodeId = nodeIdByte,
            Name = $"Node_{nodeIdByte:X2}",
            Type = NodeIdAllocation.GetNodeType(nodeIdByte),
            Version = "1.0.0",
            Capabilities = GetNodeCapabilities(nodeIdByte)
        });
    }

    /// <summary>
    /// Remove a connection between nodes
    /// </summary>
    public void RemoveConnection(byte fromNode, byte toNode)
    {
        if (_disposed)
        {
            return;
        }

        lock (_lockObject)
        {
            int removed = _topologyMap.Connections.RemoveAll(c =>
                (c.FromNode == fromNode && c.ToNode == toNode) ||
                (c.FromNode == toNode && c.ToNode == fromNode));

            if (removed > 0)
            {
                _logger.LogInformation("Removed connection: 0x{FromNode:X2} -> 0x{ToNode:X2}", fromNode, toNode);
                IncrementTopologyVersion();
            }
        }
    }

    /// <summary>
    /// Remove a node from the topology
    /// </summary>
    public void RemoveNode(byte nodeIdByte)
    {
        if (_disposed)
        {
            return;
        }

        lock (_lockObject)
        {
            if (_topologyMap.Nodes.Remove(nodeIdByte))
            {
                _logger.LogInformation("Removed node from topology: 0x{NodeIdByte:X2}", nodeIdByte);

                // Remove all connections involving this node
                _topologyMap.Connections.RemoveAll(c => c.FromNode == nodeIdByte || c.ToNode == nodeIdByte);

                NodeRemoved?.Invoke(this, new NodeRemovedEventArgs(nodeIdByte));
                IncrementTopologyVersion();
            }
        }
    }

    /// <summary>
    /// Start the topology service
    /// </summary>
    public async Task StartAsync()
    {
        if (_disposed || _isRunning)
        {
            return;
        }

        _logger.LogInformation("Starting topology service");
        _isRunning = true;
    }

    /// <summary>
    /// Stop the topology service
    /// </summary>
    public async Task StopAsync()
    {
        if (_disposed || !_isRunning)
        {
            return;
        }

        _logger.LogInformation("Stopping topology service");
        _isRunning = false;
    }

    /// <summary>
    /// Update node information
    /// </summary>
    public void UpdateNode(byte nodeIdByte, NodeInfo updatedInfo)
    {
        if (_disposed)
        {
            return;
        }

        lock (_lockObject)
        {
            if (_topologyMap.Nodes.TryGetValue(nodeIdByte, out NodeInfo? existingNode))
            {
                _topologyMap.Nodes[nodeIdByte] = updatedInfo;
                _logger.LogDebug("Updated node information: 0x{NodeIdByte:X2}", nodeIdByte);
                IncrementTopologyVersion();
            }
        }
    }

    /// <summary>
    /// Update a node's IP address, MAC address, and interface name
    /// </summary>
    public bool UpdateNodeIpAddress(byte nodeIdByte, string newIpAddress, string macAddress, string interfaceName)
    {
        if (_disposed)
        {
            return false;
        }

        lock (_lockObject)
        {
            if (_topologyMap.Nodes.TryGetValue(nodeIdByte, out NodeInfo? existingNode))
            {
                string oldIpAddress = existingNode.IpAddress;
                
                // Update the node's network information
                existingNode.IpAddress = newIpAddress;
                existingNode.MacAddress = macAddress;
                existingNode.InterfaceName = interfaceName;
                existingNode.LastSeen = DateTime.UtcNow;

                _logger.LogInformation("Updated IP address for node 0x{NodeIdByte:X2}: {OldIp} -> {NewIp}",
                    nodeIdByte, oldIpAddress, newIpAddress);

                IncrementTopologyVersion();
                return true;
            }
            else
            {
                _logger.LogWarning("Attempted to update IP address for non-existent node: 0x{NodeIdByte:X2}", nodeIdByte);
                return false;
            }
        }
    }

#endregion
}
