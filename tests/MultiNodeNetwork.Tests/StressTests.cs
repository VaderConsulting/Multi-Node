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
/// Stress tests for extreme scenarios and system overload conditions
/// </summary>
public class StressTests : IDisposable
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
    private readonly Mock<IWifiManager> _wifiManagerMock;
    private readonly Mock<IPlatformService> _platformServiceMock;
    private readonly Mock<INetworkInterfaceService> _networkInterfaceServiceMock;

    #endregion

    #region Constructors

    public StressTests()
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
    public async Task RapidNodeJoinLeave_ShouldMaintainStability()
    {
        // Arrange
        const int cycles = 50;
        const int nodesPerCycle = 20;
        const int timeLimitMs = 30000; // 30 seconds

        await _communication.InitializeAsync("MasterNode");
        await _nodeDiscovery.StartDiscoveryAsync();
        await _topology.StartAsync();
        await _messageHandler.StartAsync();

        Stopwatch stopwatch = Stopwatch.StartNew();
        List<Task> allTasks = new List<Task>();

        // Act - Rapid join/leave cycles
        for (int cycle = 0; cycle < cycles; cycle++)
        {
            List<Task> cycleTasks = new List<Task>();

            // Join phase
            for (int i = 0; i < nodesPerCycle; i++)
            {
                int nodeIndex = (cycle * nodesPerCycle) + i;
                Task task = Task.Run(async () =>
                {
                    DiscoveryMessage discoveryMessage = new DiscoveryMessage
                    {
                        SenderId = $"CycleNode_{nodeIndex:D4}",
                        SenderNodeId = (byte)((nodeIndex % 255) + 1),
                        NodeType = NodeType.Repeater,
                        IpAddress = $"192.168.1.{nodeIndex % 255}",
                        MacAddress = $"AA:BB:CC:DD:EE:{nodeIndex % 255:X2}",
                        SignalStrength = -60 + (nodeIndex % 40),
                        BatteryLevel = 50 + (nodeIndex % 50)
                    };

                    await _messageHandler.ProcessMessageAsync(System.Text.Json.JsonSerializer.Serialize(discoveryMessage));
                });
                cycleTasks.Add(task);
            }

            await Task.WhenAll(cycleTasks);

            // Leave phase (simulate nodes going offline)
            cycleTasks.Clear();
            for (int i = 0; i < nodesPerCycle; i++)
            {
                int nodeIndex = (cycle * nodesPerCycle) + i;
                Task task = Task.Run(async () =>
                {
                    HeartbeatMessage heartbeatMessage = new HeartbeatMessage
                    {
                        SenderId = $"CycleNode_{nodeIndex:D4}",
                        SenderNodeId = (byte)((nodeIndex % 255) + 1),
                        Status = NodeStatus.Offline,
                        Load = 0.0,
                        Uptime = TimeSpan.Zero
                    };

                    await _messageHandler.ProcessMessageAsync(System.Text.Json.JsonSerializer.Serialize(heartbeatMessage));
                });
                cycleTasks.Add(task);
            }

            await Task.WhenAll(cycleTasks);
            allTasks.AddRange(cycleTasks);
        }

        await Task.WhenAll(allTasks);
        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds <= timeLimitMs,
            $"Rapid join/leave cycles took {stopwatch.ElapsedMilliseconds}ms, expected <= {timeLimitMs}ms");

        TopologyMap topology = _topology.GetCurrentTopology();
        int onlineNodes = topology.Nodes.Values.Count(n => n.Status == NodeStatus.Online);

        // After all cycles, most nodes should be offline
        Assert.True(onlineNodes <= nodesPerCycle, // Only last cycle nodes might still be online
            $"Expected <= {nodesPerCycle} online nodes after cycles, got {onlineNodes}");
    }

    [Fact]
    public async Task MassiveMessageFlood_ShouldNotCrash()
    {
        // Arrange
        const int messageCount = 50000;
        const int timeLimitMs = 60000; // 60 seconds
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

        // Act - Send massive flood of messages
        List<Task> tasks = new List<Task>();
        Random random = new Random(42); // Fixed seed for reproducibility

        for (int i = 0; i < messageCount; i++)
        {
            int messageIndex = i;
            Task task = Task.Run(async () =>
            {
                try
                {
                    int messageType = random.Next(5);
                    NetworkMessage message;

                    switch (messageType)
                    {
                        case 0:
                            message = new HeartbeatMessage
                            {
                                SenderId = $"Node_{(messageIndex % 100) + 1:D3}",
                                SenderNodeId = (byte)((messageIndex % 100) + 1),
                                Load = random.NextDouble(),
                                Uptime = TimeSpan.FromMinutes(random.Next(1000)),
                                Metrics = new Dictionary<string, object>
                                {
                                    ["cpu"] = random.Next(100),
                                    ["memory"] = random.Next(100),
                                    ["temperature"] = random.Next(50, 80)
                                }
                            };
                            break;
                        case 1:
                            message = new DataTransmissionMessage
                            {
                                SenderId = $"Node_{(messageIndex % 100) + 1:D3}",
                                SenderNodeId = (byte)((messageIndex % 100) + 1),
                                DestinationId = $"Node_{(messageIndex % 100) + 2:D3}",
                                DestinationNodeId = (byte)((messageIndex % 100) + 2),
                                Payload = $"FloodData_{messageIndex}_{Guid.NewGuid()}",
                                Priority = random.Next(10),
                                HopCount = random.Next(5),
                                MaxHops = 10
                            };
                            break;
                        case 2:
                            message = new TopologyUpdateMessage
                            {
                                SenderId = $"Node_{(messageIndex % 100) + 1:D3}",
                                SenderNodeId = (byte)((messageIndex % 100) + 1),
                                Version = messageIndex,
                                Topology = new TopologyMap
                                {
                                    Version = messageIndex,
                                    Timestamp = DateTime.UtcNow.AddSeconds(random.Next(-3600, 3600))
                                }
                            };
                            break;
                        case 3:
                            message = new MasterElectionMessage
                            {
                                SenderId = $"Node_{(messageIndex % 100) + 1:D3}",
                                SenderNodeId = (byte)((messageIndex % 100) + 1),
                                ElectionId = $"FloodElection_{messageIndex}",
                                Priority = random.Next(100),
                                CandidateInfo = new Dictionary<string, object>
                                {
                                    ["capabilities"] = new[] { "routing", "monitoring", "storage" },
                                    ["uptime"] = TimeSpan.FromHours(random.Next(1, 1000)),
                                    ["load"] = random.NextDouble()
                                }
                            };
                            break;
                        default:
                            message = new DiscoveryMessage
                            {
                                SenderId = $"Node_{(messageIndex % 100) + 1:D3}",
                                SenderNodeId = (byte)((messageIndex % 100) + 1),
                                NodeType = (NodeType)(random.Next(3) + 1),
                                SignalStrength = random.Next(-100, -20),
                                BatteryLevel = random.Next(0, 101),
                                Capabilities = new Dictionary<string, object>
                                {
                                    ["canRoute"] = random.Next(2) == 1,
                                    ["canStore"] = random.Next(2) == 1,
                                    ["canMonitor"] = random.Next(2) == 1
                                }
                            };
                            break;
                    }

                    await _messageHandler.ProcessMessageAsync(System.Text.Json.JsonSerializer.Serialize(message));
                }
                catch (Exception ex)
                {
                    // Log but don't fail the test - we're testing resilience
                    Console.WriteLine($"Message {messageIndex} failed: {ex.Message}");
                }
            });
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds <= timeLimitMs,
            $"Massive message flood took {stopwatch.ElapsedMilliseconds}ms, expected <= {timeLimitMs}ms");

        // Verify system is still functional
        TopologyMap topology = _topology.GetCurrentTopology();
        Assert.NotNull(topology);
        Assert.True(topology.Nodes.Count > 0, "System should still have nodes after message flood");
    }

    [Fact]
    public async Task NetworkPartitionChaos_ShouldRecoverGracefully()
    {
        // Arrange
        const int partitionCount = 5;
        const int nodesPerPartition = 60;
        const int timeLimitMs = 45000; // 45 seconds

        await _communication.InitializeAsync("MasterNode");
        await _nodeDiscovery.StartDiscoveryAsync();
        await _topology.StartAsync();
        await _messageHandler.StartAsync();

        Stopwatch stopwatch = Stopwatch.StartNew();
        List<Task> allTasks = new List<Task>();

        // Act - Create multiple network partitions and merge them chaotically
        for (int partition = 0; partition < partitionCount; partition++)
        {
            List<Task> partitionTasks = new List<Task>();

            // Create partition
            for (int i = 0; i < nodesPerPartition; i++)
            {
                int nodeIndex = (partition * nodesPerPartition) + i;
                Task task = Task.Run(async () =>
                {
                    DiscoveryMessage discoveryMessage = new DiscoveryMessage
                    {
                        SenderId = $"Partition{partition}_Node_{i:D3}",
                        SenderNodeId = (byte)((nodeIndex % 255) + 1),
                        NodeType = NodeType.Repeater,
                        IpAddress = $"192.168.{partition + 1}.{i + 1}",
                        MacAddress = $"AA:BB:CC:DD:EE:{nodeIndex:X2}",
                        SignalStrength = -60 + (nodeIndex % 40),
                        BatteryLevel = 50 + (nodeIndex % 50)
                    };

                    await _messageHandler.ProcessMessageAsync(System.Text.Json.JsonSerializer.Serialize(discoveryMessage));
                });
                partitionTasks.Add(task);
            }

            await Task.WhenAll(partitionTasks);

            // Simulate partition isolation (nodes can't see each other)
            await Task.Delay(100);

            // Merge partitions chaotically
            List<Task> mergeTasks = new List<Task>();
            for (int i = 0; i < nodesPerPartition / 2; i++)
            {
                int nodeIndex = (partition * nodesPerPartition) + i;
                Task task = Task.Run(async () =>
                {
                    // Simulate node moving to different partition
                    DiscoveryMessage reconnectionMessage = new DiscoveryMessage
                    {
                        SenderId = $"Partition{partition}_Node_{i:D3}",
                        SenderNodeId = (byte)((nodeIndex % 255) + 1),
                        NodeType = NodeType.Repeater,
                        IpAddress = $"192.168.{((partition + 2) % partitionCount) + 1}.{i + 1}",
                        MacAddress = $"AA:BB:CC:DD:EE:{nodeIndex:X2}",
                        SignalStrength = -50 + (nodeIndex % 30),
                        BatteryLevel = 60 + (nodeIndex % 40)
                    };

                    await _messageHandler.ProcessMessageAsync(System.Text.Json.JsonSerializer.Serialize(reconnectionMessage));
                });
                mergeTasks.Add(task);
            }

            await Task.WhenAll(mergeTasks);
            allTasks.AddRange(partitionTasks);
            allTasks.AddRange(mergeTasks);
        }

        await Task.WhenAll(allTasks);
        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds <= timeLimitMs,
            $"Network partition chaos took {stopwatch.ElapsedMilliseconds}ms, expected <= {timeLimitMs}ms");

        TopologyMap topology = _topology.GetCurrentTopology();
        int totalNodes = topology.Nodes.Count;
        Assert.True(totalNodes >= partitionCount * nodesPerPartition * 0.8, // Allow 20% loss
            $"Expected at least {partitionCount * nodesPerPartition * 0.8} nodes, got {totalNodes}");
    }

    [Fact]
    public async Task ResourceExhaustion_ShouldHandleGracefully()
    {
        // Arrange
        const int nodeCount = 300;
        const int messageCount = 10000;
        const int timeLimitMs = 30000; // 30 seconds

        await _communication.InitializeAsync("MasterNode");
        await _nodeDiscovery.StartDiscoveryAsync();
        await _topology.StartAsync();
        await _messageHandler.StartAsync();

        Stopwatch stopwatch = Stopwatch.StartNew();

        // Act - Exhaust resources by creating maximum load
        List<Task> tasks = new List<Task>();

        // Add maximum nodes
        for (int i = 1; i <= nodeCount; i++)
        {
            int nodeIndex = i;
            Task task = Task.Run(async () =>
            {
                DiscoveryMessage discoveryMessage = new DiscoveryMessage
                {
                    SenderId = $"StressNode_{nodeIndex:D3}",
                    SenderNodeId = (byte)((nodeIndex % 255) + 1),
                    NodeType = nodeIndex <= 15 ? NodeType.Master :
                              nodeIndex <= 127 ? NodeType.Repeater : NodeType.UserDevice,
                    IpAddress = $"192.168.1.{nodeIndex % 255}",
                    MacAddress = $"AA:BB:CC:DD:EE:{nodeIndex % 255:X2}",
                    SignalStrength = -60 + (nodeIndex % 40),
                    BatteryLevel = 50 + (nodeIndex % 50),
                    Capabilities = new Dictionary<string, object>
                    {
                        ["canRoute"] = true,
                        ["canStore"] = true,
                        ["canMonitor"] = true,
                        ["canProcess"] = true,
                        ["canTransmit"] = true
                    }
                };

                await _messageHandler.ProcessMessageAsync(System.Text.Json.JsonSerializer.Serialize(discoveryMessage));
            });
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);

        // Send messages with large payloads to stress memory
        tasks.Clear();
        for (int i = 0; i < messageCount; i++)
        {
            int messageIndex = i;
            Task task = Task.Run(async () =>
            {
                Dictionary<string, object> largePayload = new Dictionary<string, object>();
                for (int j = 0; j < 100; j++)
                {
                    largePayload[$"key_{j}"] = $"value_{j}_{messageIndex}_{Guid.NewGuid()}";
                }

                DataTransmissionMessage dataMessage = new DataTransmissionMessage
                {
                    SenderId = $"StressNode_{(messageIndex % nodeCount) + 1:D3}",
                    SenderNodeId = (byte)((messageIndex % nodeCount) + 1),
                    DestinationId = $"StressNode_{(messageIndex % nodeCount) + 2:D3}",
                    DestinationNodeId = (byte)((messageIndex % nodeCount) + 2),
                    Payload = largePayload,
                    Priority = messageIndex % 10,
                    HopCount = messageIndex % 5,
                    MaxHops = 10
                };

                await _messageHandler.ProcessMessageAsync(System.Text.Json.JsonSerializer.Serialize(dataMessage));
            });
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds <= timeLimitMs,
            $"Resource exhaustion test took {stopwatch.ElapsedMilliseconds}ms, expected <= {timeLimitMs}ms");

        // Verify system is still functional
        TopologyMap topology = _topology.GetCurrentTopology();
        Assert.NotNull(topology);
        Assert.True(topology.Nodes.Count > 0, "System should still have nodes after resource exhaustion");

        // Check memory usage
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        long memoryUsage = GC.GetTotalMemory(false);
        Assert.True(memoryUsage < 500 * 1024 * 1024, // Should use less than 500MB
            $"Memory usage: {memoryUsage / (1024 * 1024)}MB, expected < 500MB");
    }

    [Fact]
    public async Task ConcurrentFailures_ShouldMaintainConsistency()
    {
        // Arrange
        const int failureCount = 100;
        const int timeLimitMs = 20000; // 20 seconds

        await _communication.InitializeAsync("MasterNode");
        await _nodeDiscovery.StartDiscoveryAsync();
        await _topology.StartAsync();
        await _messageHandler.StartAsync();

        // Add nodes first
        for (int i = 1; i <= 200; i++)
        {
            DiscoveryMessage discoveryMessage = new DiscoveryMessage
            {
                SenderId = $"FailureNode_{i:D3}",
                SenderNodeId = (byte)((i % 255) + 1),
                NodeType = NodeType.Repeater,
                IpAddress = $"192.168.1.{i}",
                MacAddress = $"AA:BB:CC:DD:EE:{i:X2}"
            };

            await _messageHandler.ProcessMessageAsync(System.Text.Json.JsonSerializer.Serialize(discoveryMessage));
        }

        Stopwatch stopwatch = Stopwatch.StartNew();
        List<Task> failureTasks = new List<Task>();

        // Act - Simulate concurrent failures
        for (int i = 0; i < failureCount; i++)
        {
            int failureIndex = i;
            Task task = Task.Run(async () =>
            {
                try
                {
                    // Simulate different types of failures
                    int failureType = failureIndex % 4;
                    NetworkMessage failureMessage;

                    switch (failureType)
                    {
                        case 0: // Node goes offline
                            failureMessage = new HeartbeatMessage
                            {
                                SenderId = $"FailureNode_{(failureIndex % 200) + 1:D3}",
                                SenderNodeId = (byte)((failureIndex % 200) + 1),
                                Status = NodeStatus.Offline,
                                Load = 0.0,
                                Uptime = TimeSpan.Zero
                            };
                            break;
                        case 1: // Node goes into error state
                            failureMessage = new HeartbeatMessage
                            {
                                SenderId = $"FailureNode_{(failureIndex % 200) + 1:D3}",
                                SenderNodeId = (byte)((failureIndex % 200) + 1),
                                Status = NodeStatus.Error,
                                Load = 1.0,
                                Uptime = TimeSpan.FromHours(1)
                            };
                            break;
                        case 2: // IP address change
                            failureMessage = new IpAddressUpdateMessage
                            {
                                SenderId = $"FailureNode_{(failureIndex % 200) + 1:D3}",
                                SenderNodeId = (byte)((failureIndex % 200) + 1),
                                OldIpAddress = $"192.168.1.{(failureIndex % 200) + 1}",
                                NewIpAddress = $"192.168.2.{(failureIndex % 200) + 1}",
                                MacAddress = $"AA:BB:CC:DD:EE:{(failureIndex % 200) + 1:X2}",
                                InterfaceName = "Wi-Fi"
                            };
                            break;
                        default: // Route error
                            failureMessage = new RouteErrorMessage
                            {
                                SenderId = $"FailureNode_{(failureIndex % 200) + 1:D3}",
                                SenderNodeId = (byte)((failureIndex % 200) + 1),
                                ErrorCode = RouteErrorCode.DestinationUnreachable,
                                ErrorNodeId = $"FailureNode_{(failureIndex % 200) + 1:D3}",
                                ErrorNodeIdByte = (byte)((failureIndex % 200) + 1),
                                OriginalDestinationId = $"FailureNode_{(failureIndex % 200) + 2:D3}",
                                OriginalDestinationNodeId = (byte)((failureIndex % 200) + 2),
                                ErrorMessage = $"Concurrent failure {failureIndex}"
                            };
                            break;
                    }

                    await _messageHandler.ProcessMessageAsync(System.Text.Json.JsonSerializer.Serialize(failureMessage));
                }
                catch (Exception ex)
                {
                    // Log but don't fail the test
                    Console.WriteLine($"Failure {failureIndex} failed: {ex.Message}");
                }
            });
            failureTasks.Add(task);
        }

        await Task.WhenAll(failureTasks);
        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds <= timeLimitMs,
            $"Concurrent failures took {stopwatch.ElapsedMilliseconds}ms, expected <= {timeLimitMs}ms");

        // Verify topology consistency
        TopologyMap topology = _topology.GetCurrentTopology();
        Assert.NotNull(topology);

        // Check that offline/error nodes are properly marked
        int offlineNodes = topology.Nodes.Values.Count(n => n.Status == NodeStatus.Offline);
        int errorNodes = topology.Nodes.Values.Count(n => n.Status == NodeStatus.Error);
        int totalFailedNodes = offlineNodes + errorNodes;

        Assert.True(totalFailedNodes >= failureCount * 0.5, // At least 50% of failures should be recorded
            $"Expected at least {failureCount * 0.5} failed nodes, got {totalFailedNodes}");
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