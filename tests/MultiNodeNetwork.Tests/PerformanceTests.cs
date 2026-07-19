using Microsoft.Extensions.Logging;
using Moq;
using MultiNodeNetwork.Common.Models;
using MultiNodeNetwork.Core.Configuration;
using MultiNodeNetwork.Core.Models;
using MultiNodeNetwork.Core.Protocols;
using MultiNodeNetwork.Core.Services;
using System.Diagnostics;
using Xunit;

namespace MultiNodeNetwork.Tests;

/// <summary>
/// Performance and stress tests for high-volume scenarios with up to 300 nodes
/// </summary>
public class PerformanceTests : IDisposable
{
    #region Fields

    private readonly Mock<ILogger<Communication>> _communicationLoggerMock;
    private readonly Mock<ILogger<NodeDiscovery>> _nodeDiscoveryLoggerMock;
    private readonly Mock<ILogger<Topology>> _topologyLoggerMock;
    private readonly Mock<ILogger<MessageHandler>> _messageHandlerLoggerMock;
    private readonly Mock<ILogger<NodeIdManager>> _nodeIdManagerLoggerMock;
    private readonly Mock<ILogger<MasterElection>> _masterElectionLoggerMock;
    private readonly NetworkConfig _config;
    private readonly Communication _communication;
    private readonly NodeDiscovery _nodeDiscovery;
    private readonly Topology _topology;
    private readonly MessageHandler _messageHandler;
    private readonly NodeIdManager _nodeIdManager;
    private readonly Mock<IPlatformService> _platformServiceMock;
    private readonly Mock<INetworkInterfaceService> _networkInterfaceServiceMock;
    private readonly Mock<IWifiManager> _wifiManagerMock;

    #endregion

    #region Constructors

    public PerformanceTests()
    {
        _communicationLoggerMock = new Mock<ILogger<Communication>>();
        _nodeDiscoveryLoggerMock = new Mock<ILogger<NodeDiscovery>>();
        _topologyLoggerMock = new Mock<ILogger<Topology>>();
        _messageHandlerLoggerMock = new Mock<ILogger<MessageHandler>>();
        _nodeIdManagerLoggerMock = new Mock<ILogger<NodeIdManager>>();
        _masterElectionLoggerMock = new Mock<ILogger<MasterElection>>();
        _wifiManagerMock = new Mock<IWifiManager>();
        _platformServiceMock = new Mock<IPlatformService>();
        _networkInterfaceServiceMock = new Mock<INetworkInterfaceService>();

        // Set up mock returns
        _wifiManagerMock.Setup(x => x.SignalStrength).Returns(-60);
        _wifiManagerMock.Setup(x => x.IsConnected).Returns(true);
        _wifiManagerMock.Setup(x => x.CurrentSsid).Returns("TestNetwork");

        _platformServiceMock.Setup(x => x.IsWindows()).Returns(true);
        _platformServiceMock.Setup(x => x.IsLinux()).Returns(false);
        _platformServiceMock.Setup(x => x.GetPlatformInfo()).Returns(new PlatformInfo
        {
            OperatingSystem = "Windows",
            Architecture = "X64",
            Framework = ".NET 9.0",
            IsWindows = true,
            IsLinux = false,
            IsRaspberryPi = false
        });

        _networkInterfaceServiceMock.Setup(x => x.GetNetworkInterfaceInfo()).Returns(new NetworkInterfaceInfo
        {
            IpAddress = "192.168.1.100",
            MacAddress = "AA:BB:CC:DD:EE:FF",
            InterfaceName = "Wi-Fi",
            InterfaceType = System.Net.NetworkInformation.NetworkInterfaceType.Wireless80211,
            IsConnected = true
        });

        // Use unique ports for each test instance to avoid conflicts
        int uniqueId = Environment.TickCount % 1000;
        _config = new NetworkConfig
        {
            Network = new NetworkSettings
            {
                MaxNodes = 300,
                BroadcastPort = 5000 + uniqueId,
                TcpPort = 5001 + uniqueId
            },
            Performance = new PerformanceSettings
            {
                BufferSize = 8192,
                MaxConcurrentConnections = 100
            },
            Timing = new TimingSettings
            {
                DiscoveryInterval = 5,
                HeartbeatInterval = 10,
                NodeTimeout = 30,
                ElectionTimeout = 15
            }
        };

        _nodeIdManager = new NodeIdManager(_nodeIdManagerLoggerMock.Object, _config);
        _topology = new Topology(_topologyLoggerMock.Object, _config);
        _communication = new Communication(_communicationLoggerMock.Object, _config);
        _nodeDiscovery = new NodeDiscovery(
            _nodeDiscoveryLoggerMock.Object,
            _config,
            _wifiManagerMock.Object,
            _platformServiceMock.Object,
            _communication,
            _networkInterfaceServiceMock.Object);
        _messageHandler = new MessageHandler(
            _messageHandlerLoggerMock.Object,
            _config,
            new MasterElection(_masterElectionLoggerMock.Object, _config, _communication),
            _nodeDiscovery,
            _communication,
            _topology);
    }

    #endregion

    #region Public Methods

    [Fact]
    public async Task HighVolumeSimultaneousJoins_300Nodes_ShouldCompleteWithinTimeLimit()
    {
        // Arrange
        const int nodeCount = 300;
        const int timeLimitMs = 30000; // 30 seconds
        Stopwatch stopwatch = Stopwatch.StartNew();
        List<Task> tasks = new List<Task>();

        await _communication.InitializeAsync("MasterNode");
        await _nodeDiscovery.StartDiscoveryAsync();
        await _topology.StartAsync();
        await _messageHandler.StartAsync();

        // Act - Simulate 300 nodes joining simultaneously
        for (int i = 1; i <= nodeCount; i++)
        {
            int nodeIndex = i;
            Task task = Task.Run(async () =>
            {
                DiscoveryMessage discoveryMessage = new DiscoveryMessage
                {
                    SenderId = $"Node_{nodeIndex:D3}",
                    SenderNodeId = (byte)((nodeIndex % 255) + 1),
                    NodeType = nodeIndex <= 15 ? NodeType.Master :
                              nodeIndex <= 127 ? NodeType.Repeater : NodeType.UserDevice,
                    IpAddress = $"192.168.1.{nodeIndex}",
                    MacAddress = $"AA:BB:CC:DD:EE:{nodeIndex:X2}",
                    SignalStrength = -60 + (nodeIndex % 40),
                    BatteryLevel = 50 + (nodeIndex % 50)
                };

                await _messageHandler.ProcessMessageAsync(System.Text.Json.JsonSerializer.Serialize(discoveryMessage));
            });
            tasks.Add(task);
        }

        // Wait for all nodes to join
        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds <= timeLimitMs,
            $"High volume join took {stopwatch.ElapsedMilliseconds}ms, expected <= {timeLimitMs}ms");

        TopologyMap topology = _topology.GetCurrentTopology();
        Assert.True(topology.Nodes.Count >= nodeCount * 0.95, // Allow 5% tolerance
            $"Expected at least {nodeCount * 0.95} nodes, got {topology.Nodes.Count}");
    }

    [Fact]
    public async Task SuddenRepeaterFailure_ShouldHandleGracefully()
    {
        // Arrange
        const int totalNodes = 200;
        const int repeaterNodes = 50;
        const int nodesToFail = 10;
        List<byte> failedRepeaters = new List<byte>();

        await _communication.InitializeAsync("MasterNode");
        await _nodeDiscovery.StartDiscoveryAsync();
        await _topology.StartAsync();
        await _messageHandler.StartAsync();

        // Add 200 nodes (50 repeaters, 150 user devices)
        for (int i = 1; i <= totalNodes; i++)
        {
            NodeType nodeType = i <= repeaterNodes ? NodeType.Repeater : NodeType.UserDevice;
            byte nodeId = (byte)((i % 255) + 1);

            DiscoveryMessage discoveryMessage = new DiscoveryMessage
            {
                SenderId = $"Node_{i:D3}",
                SenderNodeId = nodeId,
                NodeType = nodeType,
                IpAddress = $"192.168.1.{i}",
                MacAddress = $"AA:BB:CC:DD:EE:{i:X2}",
                SignalStrength = -60,
                BatteryLevel = 80
            };

            await _messageHandler.ProcessMessageAsync(System.Text.Json.JsonSerializer.Serialize(discoveryMessage));
        }

        TopologyMap initialTopology = _topology.GetCurrentTopology();
        Assert.True(initialTopology.Nodes.Count >= totalNodes * 0.95);

        // Act - Simulate sudden repeater failures
        Stopwatch stopwatch = Stopwatch.StartNew();
        List<Task> failureTasks = new List<Task>();

        for (int i = 1; i <= nodesToFail; i++)
        {
            int repeaterIndex = i;
            Task task = Task.Run(async () =>
            {
                // Simulate repeater going offline by sending offline status
                HeartbeatMessage heartbeatMessage = new HeartbeatMessage
                {
                    SenderId = $"Node_{repeaterIndex:D3}",
                    SenderNodeId = (byte)repeaterIndex,
                    Status = NodeStatus.Offline,
                    Load = 0.0,
                    Uptime = TimeSpan.Zero
                };

                await _messageHandler.ProcessMessageAsync(System.Text.Json.JsonSerializer.Serialize(heartbeatMessage));
                failedRepeaters.Add((byte)repeaterIndex);
            });
            failureTasks.Add(task);
        }

        await Task.WhenAll(failureTasks);
        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds <= 5000, // Should handle failures within 5 seconds
            $"Repeater failure handling took {stopwatch.ElapsedMilliseconds}ms, expected <= 5000ms");

        TopologyMap finalTopology = _topology.GetCurrentTopology();
        int onlineNodes = finalTopology.Nodes.Values.Count(n => n.Status == NodeStatus.Online);
        Assert.True(onlineNodes >= (totalNodes - nodesToFail) * 0.9, // Allow 10% tolerance
            $"Expected at least {(totalNodes - nodesToFail) * 0.9} online nodes, got {onlineNodes}");
    }

    [Fact]
    public async Task HighMessageVolume_ShouldMaintainPerformance()
    {
        // Arrange
        const int messageCount = 10000;
        const int timeLimitMs = 15000; // 15 seconds
        Stopwatch stopwatch = Stopwatch.StartNew();

        await _communication.InitializeAsync("MasterNode");
        await _nodeDiscovery.StartDiscoveryAsync();
        await _topology.StartAsync();
        await _messageHandler.StartAsync();

        // Add some nodes first
        for (int i = 1; i <= 100; i++)
        {
            DiscoveryMessage discoveryMessage = new DiscoveryMessage
            {
                SenderId = $"Node_{i:D3}",
                SenderNodeId = (byte)((i % 255) + 1),
                NodeType = NodeType.Repeater,
                IpAddress = $"192.168.1.{i}",
                MacAddress = $"AA:BB:CC:DD:EE:{i:X2}"
            };

            await _messageHandler.ProcessMessageAsync(System.Text.Json.JsonSerializer.Serialize(discoveryMessage));
        }

        // Act - Send high volume of messages
        List<Task> messageTasks = new List<Task>();

        for (int i = 0; i < messageCount; i++)
        {
            int messageIndex = i;
            Task task = Task.Run(async () =>
            {
                int messageType = messageIndex % 4;
                NetworkMessage message;

                switch (messageType)
                {
                    case 0:
                        message = new HeartbeatMessage
                        {
                            SenderId = $"Node_{(messageIndex % 100) + 1:D3}",
                            SenderNodeId = (byte)((messageIndex % 100) + 1),
                            Load = messageIndex % 100 / 100.0,
                            Uptime = TimeSpan.FromMinutes(messageIndex)
                        };
                        break;
                    case 1:
                        message = new DataTransmissionMessage
                        {
                            SenderId = $"Node_{(messageIndex % 100) + 1:D3}",
                            SenderNodeId = (byte)((messageIndex % 100) + 1),
                            DestinationId = $"Node_{(messageIndex % 100) + 2:D3}",
                            DestinationNodeId = (byte)((messageIndex % 100) + 2),
                            Payload = $"Data_{messageIndex}"
                        };
                        break;
                    case 2:
                        message = new TopologyUpdateMessage
                        {
                            SenderId = $"Node_{(messageIndex % 100) + 1:D3}",
                            SenderNodeId = (byte)((messageIndex % 100) + 1),
                            Version = messageIndex,
                            Topology = new TopologyMap { Version = messageIndex }
                        };
                        break;
                    default:
                        message = new DiscoveryMessage
                        {
                            SenderId = $"Node_{(messageIndex % 100) + 1:D3}",
                            SenderNodeId = (byte)((messageIndex % 100) + 1),
                            NodeType = NodeType.Repeater,
                            SignalStrength = -60 + (messageIndex % 40)
                        };
                        break;
                }

                await _messageHandler.ProcessMessageAsync(System.Text.Json.JsonSerializer.Serialize(message));
            });
            messageTasks.Add(task);
        }

        await Task.WhenAll(messageTasks);
        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds <= timeLimitMs,
            $"High message volume processing took {stopwatch.ElapsedMilliseconds}ms, expected <= {timeLimitMs}ms");

        double messagesPerSecond = messageCount / (stopwatch.ElapsedMilliseconds / 1000.0);
        Assert.True(messagesPerSecond >= 500, // Should handle at least 500 messages per second
            $"Message processing rate: {messagesPerSecond:F0} msg/sec, expected >= 500 msg/sec");
    }

    [Fact]
    public async Task NetworkPartitionRecovery_ShouldReestablishConnections()
    {
        // Arrange
        const int partitionSize = 50;
        const int recoveryTimeLimitMs = 10000; // 10 seconds

        await _communication.InitializeAsync("MasterNode");
        await _nodeDiscovery.StartDiscoveryAsync();
        await _topology.StartAsync();
        await _messageHandler.StartAsync();

        // Create two network partitions
        List<byte> partition1Nodes = new List<byte>();
        List<byte> partition2Nodes = new List<byte>();

        // Add nodes to partition 1
        for (int i = 1; i <= partitionSize; i++)
        {
            byte nodeId = (byte)i;
            partition1Nodes.Add(nodeId);

            DiscoveryMessage discoveryMessage = new DiscoveryMessage
            {
                SenderId = $"Partition1_Node_{i:D3}",
                SenderNodeId = nodeId,
                NodeType = NodeType.Repeater,
                IpAddress = $"192.168.1.{i}",
                MacAddress = $"AA:BB:CC:DD:EE:{i:X2}"
            };

            await _messageHandler.ProcessMessageAsync(System.Text.Json.JsonSerializer.Serialize(discoveryMessage));
        }

        // Add nodes to partition 2
        for (int i = 1; i <= partitionSize; i++)
        {
            byte nodeId = (byte)(i + partitionSize);
            partition2Nodes.Add(nodeId);

            DiscoveryMessage discoveryMessage = new DiscoveryMessage
            {
                SenderId = $"Partition2_Node_{i:D3}",
                SenderNodeId = nodeId,
                NodeType = NodeType.Repeater,
                IpAddress = $"192.168.2.{i}",
                MacAddress = $"BB:CC:DD:EE:FF:{i:X2}"
            };

            await _messageHandler.ProcessMessageAsync(System.Text.Json.JsonSerializer.Serialize(discoveryMessage));
        }

        TopologyMap initialTopology = _topology.GetCurrentTopology();
        Assert.Equal(partitionSize * 2, initialTopology.Nodes.Count);

        // Act - Simulate network partition recovery
        Stopwatch stopwatch = Stopwatch.StartNew();

        // Simulate nodes from partition 2 rejoining partition 1
        List<Task> recoveryTasks = new List<Task>();

        foreach (byte nodeId in partition2Nodes)
        {
            Task task = Task.Run(async () =>
            {
                // Simulate node reconnecting with updated IP
                DiscoveryMessage reconnectionMessage = new DiscoveryMessage
                {
                    SenderId = $"Partition2_Node_{nodeId:D3}",
                    SenderNodeId = nodeId,
                    NodeType = NodeType.Repeater,
                    IpAddress = $"192.168.1.{nodeId + 100}", // New IP in partition 1
                    MacAddress = $"BB:CC:DD:EE:FF:{nodeId:X2}",
                    SignalStrength = -50
                };

                await _messageHandler.ProcessMessageAsync(System.Text.Json.JsonSerializer.Serialize(reconnectionMessage));
            });
            recoveryTasks.Add(task);
        }

        await Task.WhenAll(recoveryTasks);
        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds <= recoveryTimeLimitMs,
            $"Network partition recovery took {stopwatch.ElapsedMilliseconds}ms, expected <= {recoveryTimeLimitMs}ms");

        TopologyMap finalTopology = _topology.GetCurrentTopology();
        int onlineNodes = finalTopology.Nodes.Values.Count(n => n.Status == NodeStatus.Online);
        Assert.True(onlineNodes >= partitionSize * 2 * 0.9, // Allow 10% tolerance
            $"Expected at least {partitionSize * 2 * 0.9} online nodes after recovery, got {onlineNodes}");
    }

    [Fact]
    public async Task MemoryUsage_UnderLoad_ShouldRemainStable()
    {
        // Arrange
        const int nodeCount = 300;
        const int messageCount = 5000;
        const long maxMemoryIncreaseMB = 100; // Allow 100MB increase

        long initialMemory = GC.GetTotalMemory(true);
        Stopwatch stopwatch = Stopwatch.StartNew();

        await _communication.InitializeAsync("MasterNode");
        await _nodeDiscovery.StartDiscoveryAsync();
        await _topology.StartAsync();
        await _messageHandler.StartAsync();

        // Act - Add nodes and send messages
        List<Task> tasks = new List<Task>();

        // Add nodes
        for (int i = 1; i <= nodeCount; i++)
        {
            int nodeIndex = i;
            Task task = Task.Run(async () =>
            {
                DiscoveryMessage discoveryMessage = new DiscoveryMessage
                {
                    SenderId = $"Node_{nodeIndex:D3}",
                    SenderNodeId = (byte)((nodeIndex % 255) + 1),
                    NodeType = nodeIndex <= 15 ? NodeType.Master :
                              nodeIndex <= 127 ? NodeType.Repeater : NodeType.UserDevice,
                    IpAddress = $"192.168.1.{nodeIndex}",
                    MacAddress = $"AA:BB:CC:DD:EE:{nodeIndex:X2}",
                    SignalStrength = -60 + (nodeIndex % 40),
                    BatteryLevel = 50 + (nodeIndex % 50)
                };

                await _messageHandler.ProcessMessageAsync(System.Text.Json.JsonSerializer.Serialize(discoveryMessage));
            });
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);

        // Send messages
        for (int i = 0; i < messageCount; i++)
        {
            int messageIndex = i;
            Task task = Task.Run(async () =>
            {
                HeartbeatMessage heartbeatMessage = new HeartbeatMessage
                {
                    SenderId = $"Node_{(messageIndex % nodeCount) + 1:D3}",
                    SenderNodeId = (byte)((messageIndex % nodeCount) + 1),
                    Load = messageIndex % 100 / 100.0,
                    Uptime = TimeSpan.FromMinutes(messageIndex)
                };

                await _messageHandler.ProcessMessageAsync(System.Text.Json.JsonSerializer.Serialize(heartbeatMessage));
            });
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Force garbage collection and measure final memory
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        long finalMemory = GC.GetTotalMemory(true);

        long memoryIncreaseMB = (finalMemory - initialMemory) / (1024 * 1024);

        // Assert
        Assert.True(memoryIncreaseMB <= maxMemoryIncreaseMB,
            $"Memory increase: {memoryIncreaseMB}MB, expected <= {maxMemoryIncreaseMB}MB");

        Assert.True(stopwatch.ElapsedMilliseconds <= 30000, // Should complete within 30 seconds
            $"Load test took {stopwatch.ElapsedMilliseconds}ms, expected <= 30000ms");
    }

    [Fact]
    public async Task ConcurrentElections_ShouldPreventConflicts()
    {
        // Arrange
        const int electionCount = 10;
        const int timeLimitMs = 10000; // 10 seconds

        await _communication.InitializeAsync("MasterNode");
        await _nodeDiscovery.StartDiscoveryAsync();
        await _topology.StartAsync();
        await _messageHandler.StartAsync();

        // Add some nodes first
        for (int i = 1; i <= 50; i++)
        {
            DiscoveryMessage discoveryMessage = new DiscoveryMessage
            {
                SenderId = $"Node_{i:D3}",
                SenderNodeId = (byte)i,
                NodeType = NodeType.Master,
                IpAddress = $"192.168.1.{i}",
                MacAddress = $"AA:BB:CC:DD:EE:{i:X2}"
            };

            await _messageHandler.ProcessMessageAsync(System.Text.Json.JsonSerializer.Serialize(discoveryMessage));
        }

        Stopwatch stopwatch = Stopwatch.StartNew();
        List<Task> electionTasks = new List<Task>();

        // Act - Start multiple elections simultaneously
        for (int i = 0; i < electionCount; i++)
        {
            int electionIndex = i;
            Task task = Task.Run(async () =>
            {
                MasterElectionMessage electionMessage = new MasterElectionMessage
                {
                    SenderId = $"Node_{(electionIndex % 50) + 1:D3}",
                    SenderNodeId = (byte)((electionIndex % 50) + 1),
                    ElectionId = $"Election_{electionIndex}",
                    Priority = 100 - electionIndex,
                    CandidateInfo = new Dictionary<string, object>
                    {
                        ["capabilities"] = new[] { "routing", "monitoring" },
                        ["uptime"] = TimeSpan.FromHours(electionIndex + 1)
                    }
                };

                await _messageHandler.ProcessMessageAsync(System.Text.Json.JsonSerializer.Serialize(electionMessage));
            });
            electionTasks.Add(task);
        }

        await Task.WhenAll(electionTasks);
        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds <= timeLimitMs,
            $"Concurrent elections took {stopwatch.ElapsedMilliseconds}ms, expected <= {timeLimitMs}ms");

        // Verify no duplicate master elections occurred
        TopologyMap topology = _topology.GetCurrentTopology();
        int masterNodes = topology.Nodes.Values.Count(n => n.Type == NodeType.Master);
        Assert.True(masterNodes <= 15, // Should not exceed master node limit
            $"Found {masterNodes} master nodes, expected <= 15");
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        _communication?.Dispose();
        _nodeDiscovery?.Dispose();
        _topology?.Dispose();
        _messageHandler?.Dispose();
        GC.SuppressFinalize(this);
    }

    #endregion
}