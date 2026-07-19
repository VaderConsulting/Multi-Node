using Microsoft.Extensions.Logging;
using MultiNodeNetwork.Common.Models;
using MultiNodeNetwork.Core.Configuration;
using MultiNodeNetwork.Core.Models;
using MultiNodeNetwork.Core.Services;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;

namespace MultiNodeNetwork.Core.Protocols;
/// <summary>
/// Node discovery protocol implementation
/// </summary>
public class NodeDiscovery : IDisposable
{
    #region Events

    public event EventHandler<DiscoveryRequestEventArgs>? DiscoveryRequested;

    public event EventHandler<NodeDiscoveredEventArgs>? NodeDiscovered;

    public event EventHandler<NodeLostEventArgs>? NodeLost;

    #endregion

    #region Fields

    private readonly Timer _CleanupTimer;
    private readonly Communication _Communication;
    private readonly NetworkConfig _Config;
    private readonly ConcurrentDictionary<byte, DiscoveredNode> _DiscoveredNodes = new();
    private readonly Timer _DiscoveryTimer;
    private bool _Disposed = false;
    private readonly ILogger<NodeDiscovery> _Logger;
    private string? _MyNodeId;
    private byte _MyNodeIdByte = 0;
    private readonly INetworkInterfaceService _NetworkInterfaceService;
    private readonly IPlatformService _PlatformService;
    private readonly IWifiManager _WifiManager;

    #endregion

    #region Constructors

    public NodeDiscovery(ILogger<NodeDiscovery> Logger, NetworkConfig Config, IWifiManager WifiManager, IPlatformService PlatformService, Communication Communication, INetworkInterfaceService NetworkInterfaceService)
    {
        _Logger = Logger;
        _Config = Config;
        _WifiManager = WifiManager;
        _PlatformService = PlatformService;
        _Communication = Communication;
        _NetworkInterfaceService = NetworkInterfaceService;
        _DiscoveryTimer = new Timer(SendDiscoveryMessage, null, Timeout.Infinite, Timeout.Infinite);
        _CleanupTimer = new Timer(CleanupStaleNodes, null, Timeout.Infinite, Timeout.Infinite);
    }

    #endregion

    #region Private Methods

    private async Task BroadcastDiscoveryMessageAsync(DiscoveryMessage Message)
    {
        try
        {
            bool Success = await _Communication.BroadcastMessageAsync(Message);
            if (Success)
            {
                _Logger.LogDebug("Successfully broadcasted discovery message: {MessageId}", Message.MessageId);
            }
            else
            {
                _Logger.LogWarning("Failed to broadcast discovery message: {MessageId}", Message.MessageId);
            }
        }
        catch (Exception ex)
        {
            _Logger.LogError(ex, "Error broadcasting discovery message: {MessageId}", Message.MessageId);
        }
    }

    private double CalculateDistance(Location Loc1, Location Loc2)
    {
        double Dx = Loc1.X - Loc2.X;
        double Dy = Loc1.Y - Loc2.Y;
        double Dz = Loc1.Z - Loc2.Z;

        return Math.Sqrt((Dx * Dx) + (Dy * Dy) + (Dz * Dz));
    }

    private void CleanupStaleNodes(object? State)
    {
        if (_Disposed)
        {
            return;
        }

        DateTime Now = DateTime.UtcNow;
        List<KeyValuePair<byte, DiscoveredNode>> StaleNodes = _DiscoveredNodes
            .Where(kvp => Now - kvp.Value.LastSeen > TimeSpan.FromSeconds(_Config.Timing.NodeTimeout))
            .ToList();

        foreach (KeyValuePair<byte, DiscoveredNode> StaleNode in StaleNodes)
        {
            if (_DiscoveredNodes.TryRemove(StaleNode.Key, out DiscoveredNode? Node))
            {
                Node.Status = NodeStatus.Offline;
                _Logger.LogWarning("Node {NodeId} (0x{NodeIdByte:X2}) marked as offline (no heartbeat for {Timeout}s)",
                    StaleNode.Value.NodeId, StaleNode.Key, _Config.Timing.NodeTimeout);

                NodeLost?.Invoke(this, new NodeLostEventArgs(StaleNode.Key, Node));
            }
        }
    }

    private int GetBatteryLevel()
    {
        // This would be implemented by the hardware interface
        // For now, return a simulated value based on platform
        if (_PlatformService.IsWindows())
        {
            return GetWindowsBatteryLevel();
        }
        else if (_PlatformService.IsLinux())
        {
            return GetLinuxBatteryLevel();
        }
        else
        {
            return 100; // Default fallback
        }
    }

    private int GetLinuxBatteryLevel()
    {
        try
        {
            // Try to read from /sys/class/power_supply/BAT0/capacity
            string BatteryPath = "/sys/class/power_supply/BAT0/capacity";
            if (File.Exists(BatteryPath))
            {
                string Content = File.ReadAllText(BatteryPath);
                if (int.TryParse(Content.Trim(), out int Level))
                {
                    return Level;
                }
            }

            // Fallback to upower command
            ProcessStartInfo Info = new ProcessStartInfo
            {
                FileName = "upower",
                Arguments = "-i /org/freedesktop/UPower/devices/battery_BAT0",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using Process Process = Process.Start(Info)!;
            string Output = Process.StandardOutput.ReadToEnd();
            Process.WaitForExit();

            // Parse battery level from output
            string[] Lines = Output.Split('\n');
            foreach (string Line in Lines)
            {
                if (Line.Contains("percentage:"))
                {
                    string[] Parts = Line.Split(':');
                    if (Parts.Length > 1)
                    {
                        string PercentageStr = Parts[1].Trim().Replace("%", "");
                        if (int.TryParse(PercentageStr, out int Level))
                        {
                            return Level;
                        }
                    }
                }
            }

            return 100; // Default if parsing fails
        }
        catch (Exception ex)
        {
            _Logger.LogError(ex, "Error getting Linux battery level");
            return 100;
        }
    }

    private Dictionary<string, object> GetNodeCapabilities()
    {
        NodeType NodeType = NodeIdAllocation.GetNodeType(_MyNodeIdByte);

        Dictionary<string, object> Capabilities = new Dictionary<string, object>
        {
            ["version"] = "1.0.0",
            ["signalStrength"] = GetSignalStrength(),
            ["batteryLevel"] = GetBatteryLevel()
        };

        // Add type-specific capabilities
        switch (NodeType)
        {
            case NodeType.Master:
                Capabilities["canBeMaster"] = true;
                Capabilities["canRoute"] = true;
                Capabilities["maxConnections"] = 50;
                break;
            case NodeType.Repeater:
                Capabilities["canRoute"] = true;
                Capabilities["maxConnections"] = 10;
                break;
            case NodeType.UserDevice:
                Capabilities["canRoute"] = false;
                Capabilities["maxConnections"] = 1;
                break;
        }

        return Capabilities;
    }

    private Location? GetNodeLocation()
    {
        // This would be implemented by GPS or manual configuration
        // For now, return a default location based on node ID
        // In a real implementation, this would use GPS coordinates or manual configuration
        return new Location
        {
            X = (int)_MyNodeIdByte % 10 * 100, // Simulate grid layout
            Y = (int)_MyNodeIdByte / 10 * 100,
            Z = 0
        };
    }

    private int GetSignalStrength()
    {
        // Use the WiFi manager to get actual signal strength
        return _WifiManager.SignalStrength;
    }

    private int GetWindowsBatteryLevel()
    {
        try
        {
            ProcessStartInfo Info = new ProcessStartInfo
            {
                FileName = "wmic",
                Arguments = "path Win32_Battery get EstimatedChargeRemaining",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using Process Process = Process.Start(Info)!;
            string Output = Process.StandardOutput.ReadToEnd();
            Process.WaitForExit();

            // Parse battery level from output
            string[] Lines = Output.Split('\n');
            foreach (string Line in Lines)
            {
                if (int.TryParse(Line.Trim(), out int Level))
                {
                    return Level;
                }
            }

            return 100; // Default if parsing fails
        }
        catch (Exception ex)
        {
            _Logger.LogError(ex, "Error getting Windows battery level");
            return 100;
        }
    }

    private async void SendDiscoveryMessage(object? State)
    {
        if (_Disposed)
        {
            return;
        }

        try
        {
            await SendDiscoveryMessageAsync();
        }
        catch (Exception ex)
        {
            _Logger.LogError(ex, "Error sending discovery message");
        }
    }

    private async Task SendDiscoveryMessageAsync()
    {
        NetworkInterfaceInfo networkInfo = _NetworkInterfaceService.GetNetworkInterfaceInfo();

        DiscoveryMessage DiscoveryMessage = new DiscoveryMessage
        {
            SenderId = _MyNodeId!,
            SenderNodeId = _MyNodeIdByte,
            NodeType = NodeIdAllocation.GetNodeType(_MyNodeIdByte),
            Capabilities = GetNodeCapabilities(),
            SignalStrength = GetSignalStrength(),
            BatteryLevel = GetBatteryLevel(),
            Location = GetNodeLocation(),
            IpAddress = networkInfo.IpAddress,
            MacAddress = networkInfo.MacAddress,
            InterfaceName = networkInfo.InterfaceName
        };

        await BroadcastDiscoveryMessageAsync(DiscoveryMessage);
        DiscoveryRequested?.Invoke(this, new DiscoveryRequestEventArgs(DiscoveryMessage));
    }

    private async Task SendDiscoveryMessageToNodeAsync(byte NodeIdByte, DiscoveryMessage Message)
    {
        try
        {
            // For now, use broadcast since we don't have direct node addressing
            // In a real implementation, this would send to a specific node
            bool Success = await _Communication.BroadcastMessageAsync(Message);
            if (Success)
            {
                _Logger.LogDebug("Successfully sent discovery message to 0x{NodeIdByte:X2}: {MessageId}", NodeIdByte, Message.MessageId);
            }
            else
            {
                _Logger.LogWarning("Failed to send discovery message to 0x{NodeIdByte:X2}: {MessageId}", NodeIdByte, Message.MessageId);
            }
        }
        catch (Exception ex)
        {
            _Logger.LogError(ex, "Error sending discovery message to 0x{NodeIdByte:X2}: {MessageId}", NodeIdByte, Message.MessageId);
        }
    }

    private async Task SendDiscoveryResponseAsync(byte TargetNodeIdByte)
    {
        NetworkInterfaceInfo networkInfo = _NetworkInterfaceService.GetNetworkInterfaceInfo();

        DiscoveryMessage ResponseMessage = new DiscoveryMessage
        {
            SenderId = _MyNodeId!,
            SenderNodeId = _MyNodeIdByte,
            NodeType = NodeIdAllocation.GetNodeType(_MyNodeIdByte),
            Capabilities = GetNodeCapabilities(),
            SignalStrength = GetSignalStrength(),
            BatteryLevel = GetBatteryLevel(),
            Location = GetNodeLocation(),
            IpAddress = networkInfo.IpAddress,
            MacAddress = networkInfo.MacAddress,
            InterfaceName = networkInfo.InterfaceName
        };

        await SendDiscoveryMessageToNodeAsync(TargetNodeIdByte, ResponseMessage);
    }

    #endregion

    #region Public Methods

    public void Dispose()
    {
        if (_Disposed)
        {
            return;
        }

        _Disposed = true;
        _DiscoveryTimer?.Dispose();
        _CleanupTimer?.Dispose();
    }

    /// <summary>
    /// Get all discovered nodes
    /// </summary>
    public IEnumerable<DiscoveredNode> GetDiscoveredNodes()
    {
        return _DiscoveredNodes.Values.ToList();
    }

    /// <summary>
    /// Get the current node's battery level
    /// </summary>
    public int GetMyBatteryLevel()
    {
        return GetBatteryLevel();
    }

    /// <summary>
    /// Get the current node's capabilities
    /// </summary>
    public Dictionary<string, object> GetMyCapabilities()
    {
        return GetNodeCapabilities();
    }

    /// <summary>
    /// Get the current node's location
    /// </summary>
    public Location? GetMyLocation()
    {
        return GetNodeLocation();
    }

    /// <summary>
    /// Get the current node's ID
    /// </summary>
    public string GetMyNodeId()
    {
        return _MyNodeId ?? string.Empty;
    }

    /// <summary>
    /// Get the current node's byte ID
    /// </summary>
    public byte GetMyNodeIdByte()
    {
        return _MyNodeIdByte;
    }

    /// <summary>
    /// Get the current node's type
    /// </summary>
    public NodeType GetMyNodeType()
    {
        return NodeIdAllocation.GetNodeType(_MyNodeIdByte);
    }

    /// <summary>
    /// Get the current node's signal strength
    /// </summary>
    public int GetMySignalStrength()
    {
        return GetSignalStrength();
    }

    /// <summary>
    /// Get node information
    /// </summary>
    public DiscoveredNode? GetNode(byte NodeIdByte)
    {
        _DiscoveredNodes.TryGetValue(NodeIdByte, out DiscoveredNode? Node);
        return Node;
    }

    /// <summary>
    /// Get nodes by signal strength range
    /// </summary>
    public IEnumerable<DiscoveredNode> GetNodesBySignalStrength(int MinStrength, int MaxStrength)
    {
        return _DiscoveredNodes.Values.Where(n => n.SignalStrength >= MinStrength && n.SignalStrength <= MaxStrength);
    }

    /// <summary>
    /// Get nodes by type
    /// </summary>
    public IEnumerable<DiscoveredNode> GetNodesByType(NodeType NodeType)
    {
        return _DiscoveredNodes.Values.Where(n => n.NodeType == NodeType);
    }

    /// <summary>
    /// Get nodes within a certain distance
    /// </summary>
    public IEnumerable<DiscoveredNode> GetNodesWithinDistance(Location Center, int MaxDistance)
    {
        return _DiscoveredNodes.Values.Where(n =>
            n.Location != null && CalculateDistance(Center, n.Location) <= MaxDistance);
    }

    /// <summary>
    /// Handle incoming discovery message
    /// </summary>
    public async Task HandleDiscoveryMessageAsync(DiscoveryMessage Message)
    {
        if (_Disposed || Message.SenderNodeId == _MyNodeIdByte)
        {
            return;
        }

        _Logger.LogDebug("Received discovery message from {SenderId} (0x{SenderNodeId:X2}) at IP {IpAddress}",
            Message.SenderId, Message.SenderNodeId, Message.IpAddress);

        DiscoveredNode DiscoveredNode = new DiscoveredNode
        {
            NodeId = Message.SenderId,
            NodeIdByte = Message.SenderNodeId,
            NodeType = Message.NodeType,
            IpAddress = Message.IpAddress,
            MacAddress = Message.MacAddress,
            InterfaceName = Message.InterfaceName,
            Capabilities = Message.Capabilities,
            SignalStrength = Message.SignalStrength,
            BatteryLevel = Message.BatteryLevel,
            Location = Message.Location,
            LastSeen = DateTime.UtcNow,
            Status = NodeStatus.Online
        };

        bool IsNewNode = false;
        if (_DiscoveredNodes.TryAdd(Message.SenderNodeId, DiscoveredNode))
        {
            IsNewNode = true;
            _Logger.LogInformation("Discovered new node: {NodeId} (0x{SenderNodeId:X2}) - {NodeType} at {IpAddress}",
                Message.SenderId, Message.SenderNodeId, Message.NodeType, Message.IpAddress);
        }
        else
        {
            // Update existing node
            if (_DiscoveredNodes.TryGetValue(Message.SenderNodeId, out DiscoveredNode? ExistingNode))
            {
                ExistingNode.LastSeen = DateTime.UtcNow;
                ExistingNode.SignalStrength = Message.SignalStrength;
                ExistingNode.BatteryLevel = Message.BatteryLevel;
                ExistingNode.Status = NodeStatus.Online;

                // Update network interface information if it has changed
                if (ExistingNode.IpAddress != Message.IpAddress)
                {
                    _Logger.LogInformation("IP address changed for node {NodeId} (0x{SenderNodeId:X2}): {OldIp} -> {NewIp}",
                        Message.SenderId, Message.SenderNodeId, ExistingNode.IpAddress, Message.IpAddress);
                    ExistingNode.IpAddress = Message.IpAddress;
                }

                ExistingNode.MacAddress = Message.MacAddress;
                ExistingNode.InterfaceName = Message.InterfaceName;

                _Logger.LogDebug("Updated node: {NodeId} (0x{SenderNodeId:X2}) at {IpAddress}",
                    Message.SenderId, Message.SenderNodeId, Message.IpAddress);
            }
        }

        if (IsNewNode)
        {
            NodeDiscovered?.Invoke(this, new NodeDiscoveredEventArgs(DiscoveredNode));
        }

        // Respond with our own discovery message
        await SendDiscoveryResponseAsync(Message.SenderNodeId);
    }

    /// <summary>
    /// Handle incoming heartbeat message
    /// </summary>
    public async Task HandleHeartbeatMessageAsync(HeartbeatMessage Message)
    {
        if (_Disposed || Message.SenderNodeId == _MyNodeIdByte)
        {
            return;
        }

        _Logger.LogDebug("Received heartbeat from {SenderId} (0x{SenderNodeId:X2})",
            Message.SenderId, Message.SenderNodeId);

        if (_DiscoveredNodes.TryGetValue(Message.SenderNodeId, out DiscoveredNode? Node))
        {
            Node.LastSeen = DateTime.UtcNow;
            Node.Status = Message.Status;
            Node.Uptime = Message.Uptime;
            Node.Load = Message.Load;

            // Update metrics
            foreach (KeyValuePair<string, object> Metric in Message.Metrics)
            {
                Node.Metrics[Metric.Key] = Metric.Value;
            }
        }
        else
        {
            // Node not in discovery list, add it
            DiscoveredNode DiscoveredNode = new DiscoveredNode
            {
                NodeId = Message.SenderId,
                NodeIdByte = Message.SenderNodeId,
                NodeType = NodeIdAllocation.GetNodeType(Message.SenderNodeId),
                LastSeen = DateTime.UtcNow,
                Status = Message.Status,
                Uptime = Message.Uptime,
                Load = Message.Load,
                Metrics = new Dictionary<string, object>(Message.Metrics)
            };

            _DiscoveredNodes.TryAdd(Message.SenderNodeId, DiscoveredNode);
            _Logger.LogInformation("Added node from heartbeat: {NodeId} (0x{SenderNodeId:X2})",
                Message.SenderId, Message.SenderNodeId);

            NodeDiscovered?.Invoke(this, new NodeDiscoveredEventArgs(DiscoveredNode));
        }
    }

    /// <summary>
    /// Initialize the node discovery protocol
    /// </summary>
    public async Task InitializeAsync(string NodeId, byte NodeIdByte)
    {
        _MyNodeId = NodeId;
        _MyNodeIdByte = NodeIdByte;
        _Logger.LogInformation("Initializing node discovery protocol for node {NodeId} (0x{NodeIdByte:X2})",
            NodeId, NodeIdByte);

        // Start discovery process
        await StartDiscoveryAsync();
    }

    /// <summary>
    /// Check if a node is online
    /// </summary>
    public bool IsNodeOnline(byte NodeIdByte)
    {
        return _DiscoveredNodes.TryGetValue(NodeIdByte, out DiscoveredNode? Node) &&
               Node.Status == NodeStatus.Online;
    }

    /// <summary>
    /// Remove a node from discovery
    /// </summary>
    public bool RemoveNode(byte NodeIdByte)
    {
        if (_DiscoveredNodes.TryRemove(NodeIdByte, out DiscoveredNode? Node))
        {
            Node.Status = NodeStatus.Offline;
            _Logger.LogInformation("Removed node from discovery: {NodeId} (0x{NodeIdByte:X2})",
                Node.NodeId, NodeIdByte);
            NodeLost?.Invoke(this, new NodeLostEventArgs(NodeIdByte, Node));
            return true;
        }
        return false;
    }

    /// <summary>
    /// Start the discovery process
    /// </summary>
    public async Task StartDiscoveryAsync()
    {
        if (_Disposed)
        {
            return;
        }

        _Logger.LogInformation("Starting node discovery process");

        // Send initial discovery message
        await SendDiscoveryMessageAsync();

        // Start periodic discovery
        _DiscoveryTimer.Change(
            TimeSpan.Zero,
            TimeSpan.FromSeconds(_Config.Timing.DiscoveryInterval));

        // Start cleanup timer
        _CleanupTimer.Change(
            TimeSpan.FromSeconds(_Config.Timing.NodeTimeout),
            TimeSpan.FromSeconds(_Config.Timing.NodeTimeout));
    }

    /// <summary>
    /// Stop the discovery process
    /// </summary>
    public async Task StopDiscoveryAsync()
    {
        if (_Disposed)
        {
            return;
        }

        _Logger.LogInformation("Stopping node discovery process");

        _DiscoveryTimer.Change(Timeout.Infinite, Timeout.Infinite);
        _CleanupTimer.Change(Timeout.Infinite, Timeout.Infinite);
    }

    #endregion
}
