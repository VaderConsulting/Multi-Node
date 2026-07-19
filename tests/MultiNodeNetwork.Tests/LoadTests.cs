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
/// Load tests for sustained high load conditions and performance measurement
/// </summary>
public class LoadTests : IDisposable
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

    public LoadTests()
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
    public async Task SustainedHighLoad_300Nodes_ShouldMaintainPerformance()
    {
        // Arrange
        const int nodeCount = 300;
        const int testDurationSeconds = 30;
        const int messagesPerSecond = 100;
        const int totalMessages = testDurationSeconds * messagesPerSecond;
        const double maxLatencyMs = 1000; // 1 second max latency

        await _communication.InitializeAsync("MasterNode");
        await _nodeDiscovery.StartDiscoveryAsync();
        await _topology.StartAsync();
        await _messageHandler.StartAsync();

        // Add 300 nodes
        for (int i = 1; i <= nodeCount; i++)
        {
            DiscoveryMessage discoveryMessage = new DiscoveryMessage
            {
                SenderId = $"LoadNode_{i:D3}",
                SenderNodeId = (byte)((i % 255) + 1),
                NodeType = i <= 15 ? NodeType.Master :
                          i <= 127 ? NodeType.Repeater : NodeType.UserDevice,
                IpAddress = $"192.168.1.{i % 255}",
                MacAddress = $"AA:BB:CC:DD:EE:{i % 255:X2}",
                SignalStrength = -60 + (i % 40),
                BatteryLevel = 50 + (i % 50)
            };

            await _messageHandler.ProcessMessageAsync(System.Text.Json.JsonSerializer.Serialize(discoveryMessage));
        }

        Stopwatch stopwatch = Stopwatch.StartNew();
        List<long> latencies = new List<long>();
        List<Task> tasks = new List<Task>();

        // Act - Sustained high load for 30 seconds
        for (int i = 0; i < totalMessages; i++)
        {
            int messageIndex = i;
            Task task = Task.Run(async () =>
            {
                Stopwatch messageStart = Stopwatch.StartNew();

                HeartbeatMessage heartbeatMessage = new HeartbeatMessage
                {
                    SenderId = $"LoadNode_{(messageIndex % nodeCount) + 1:D3}",
                    SenderNodeId = (byte)((messageIndex % nodeCount) + 1),
                    Load = messageIndex % 100 / 100.0,
                    Uptime = TimeSpan.FromMinutes(messageIndex % 1000),
                    Metrics = new Dictionary<string, object>
                    {
                        ["cpu"] = messageIndex % 100,
                        ["memory"] = messageIndex % 100,
                        ["network"] = messageIndex % 100
                    }
                };

                await _messageHandler.ProcessMessageAsync(System.Text.Json.JsonSerializer.Serialize(heartbeatMessage));

                messageStart.Stop();
                lock (latencies)
                {
                    latencies.Add(messageStart.ElapsedMilliseconds);
                }
            });
            tasks.Add(task);

            // Control message rate
            if (i % messagesPerSecond == 0 && i > 0)
            {
                await Task.Delay(1000); // Wait 1 second every 100 messages
            }
        }

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds <= (testDurationSeconds + 5) * 1000, // Allow 5 seconds buffer
            $"Sustained load test took {stopwatch.ElapsedMilliseconds}ms, expected <= {(testDurationSeconds + 5) * 1000}ms");

        // Check latency statistics
        double avgLatency = latencies.Average();
        long maxLatency = latencies.Max();
        long p95Latency = latencies.OrderBy(x => x).Skip((int)(latencies.Count * 0.95)).First();

        Assert.True(avgLatency <= maxLatencyMs * 0.5, // Average latency should be under 500ms
            $"Average latency: {avgLatency:F2}ms, expected <= {maxLatencyMs * 0.5}ms");
        Assert.True(maxLatency <= maxLatencyMs, // Max latency should be under 1 second
            $"Max latency: {maxLatency}ms, expected <= {maxLatencyMs}ms");
        Assert.True(p95Latency <= maxLatencyMs * 0.8, // 95th percentile should be under 800ms
            $"95th percentile latency: {p95Latency}ms, expected <= {maxLatencyMs * 0.8}ms");

        // Verify system stability
        TopologyMap topology = _topology.GetCurrentTopology();
        Assert.Equal(nodeCount, topology.Nodes.Count);
        int onlineNodes = topology.Nodes.Values.Count(n => n.Status == NodeStatus.Online);
        Assert.True(onlineNodes >= nodeCount * 0.95, // At least 95% nodes should be online
            $"Expected at least {nodeCount * 0.95} online nodes, got {onlineNodes}");
    }

    [Fact]
    public async Task BurstLoad_ShouldHandleSpikes()
    {
        // Arrange
        const int burstCount = 10;
        const int messagesPerBurst = 1000;
        const int burstIntervalMs = 2000; // 2 seconds between bursts
        const double maxLatencyMs = 2000; // 2 seconds max latency during bursts

        await _communication.InitializeAsync("MasterNode");
        await _nodeDiscovery.StartDiscoveryAsync();
        await _topology.StartAsync();
        await _messageHandler.StartAsync();

        // Add nodes
        for (int i = 1; i <= 200; i++)
        {
            DiscoveryMessage discoveryMessage = new DiscoveryMessage
            {
                SenderId = $"BurstNode_{i:D3}",
                SenderNodeId = (byte)((i % 255) + 1),
                NodeType = NodeType.Repeater,
                IpAddress = $"192.168.1.{i}",
                MacAddress = $"AA:BB:CC:DD:EE:{i:X2}"
            };

            await _messageHandler.ProcessMessageAsync(System.Text.Json.JsonSerializer.Serialize(discoveryMessage));
        }

        Stopwatch stopwatch = Stopwatch.StartNew();
        List<List<long>> burstLatencies = new List<List<long>>();

        // Act - Burst load pattern
        for (int burst = 0; burst < burstCount; burst++)
        {
            Stopwatch burstStart = Stopwatch.StartNew();
            List<long> latencies = new List<long>();
            List<Task> burstTasks = new List<Task>();

            // Send burst of messages
            for (int i = 0; i < messagesPerBurst; i++)
            {
                int messageIndex = (burst * messagesPerBurst) + i;
                Task task = Task.Run(async () =>
                {
                    Stopwatch messageStart = Stopwatch.StartNew();

                    DataTransmissionMessage dataMessage = new DataTransmissionMessage
                    {
                        SenderId = $"BurstNode_{(messageIndex % 200) + 1:D3}",
                        SenderNodeId = (byte)((messageIndex % 200) + 1),
                        DestinationId = $"BurstNode_{(messageIndex % 200) + 2:D3}",
                        DestinationNodeId = (byte)((messageIndex % 200) + 2),
                        Payload = $"BurstData_{messageIndex}",
                        Priority = messageIndex % 10,
                        HopCount = messageIndex % 5,
                        MaxHops = 10
                    };

                    await _messageHandler.ProcessMessageAsync(System.Text.Json.JsonSerializer.Serialize(dataMessage));

                    messageStart.Stop();
                    lock (latencies)
                    {
                        latencies.Add(messageStart.ElapsedMilliseconds);
                    }
                });
                burstTasks.Add(task);
            }

            await Task.WhenAll(burstTasks);
            burstStart.Stop();

            burstLatencies.Add(latencies);

            // Wait between bursts
            if (burst < burstCount - 1)
            {
                await Task.Delay(burstIntervalMs);
            }
        }

        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds <= (burstCount * burstIntervalMs) + 10000, // Allow 10 seconds buffer
            $"Burst load test took {stopwatch.ElapsedMilliseconds}ms, expected <= {(burstCount * burstIntervalMs) + 10000}ms");

        // Check burst performance
        for (int burst = 0; burst < burstCount; burst++)
        {
            List<long> latencies = burstLatencies[burst];
            double avgLatency = latencies.Average();
            long maxLatency = latencies.Max();

            Assert.True(avgLatency <= maxLatencyMs * 0.5, // Average latency during burst should be under 1 second
                $"Burst {burst} average latency: {avgLatency:F2}ms, expected <= {maxLatencyMs * 0.5}ms");
            Assert.True(maxLatency <= maxLatencyMs, // Max latency during burst should be under 2 seconds
                $"Burst {burst} max latency: {maxLatency}ms, expected <= {maxLatencyMs}ms");
        }

        // Verify system recovery
        TopologyMap topology = _topology.GetCurrentTopology();
        Assert.True(topology.Nodes.Count > 0, "System should still have nodes after burst load");
    }

    [Fact]
    public async Task GradualLoadIncrease_ShouldScaleGracefully()
    {
        // Arrange
        const int loadLevels = 5;
        const int messagesPerLevel = 500;
        const int levelDurationSeconds = 10;
        const int totalDurationSeconds = loadLevels * levelDurationSeconds;

        await _communication.InitializeAsync("MasterNode");
        await _nodeDiscovery.StartDiscoveryAsync();
        await _topology.StartAsync();
        await _messageHandler.StartAsync();

        // Add nodes
        for (int i = 1; i <= 150; i++)
        {
            DiscoveryMessage discoveryMessage = new DiscoveryMessage
            {
                SenderId = $"ScaleNode_{i:D3}",
                SenderNodeId = (byte)((i % 255) + 1),
                NodeType = NodeType.Repeater,
                IpAddress = $"192.168.1.{i}",
                MacAddress = $"AA:BB:CC:DD:EE:{i:X2}"
            };

            await _messageHandler.ProcessMessageAsync(System.Text.Json.JsonSerializer.Serialize(discoveryMessage));
        }

        Stopwatch stopwatch = Stopwatch.StartNew();
        List<List<long>> levelLatencies = new List<List<long>>();

        // Act - Gradually increase load
        for (int level = 0; level < loadLevels; level++)
        {
            Stopwatch levelStart = Stopwatch.StartNew();
            List<long> latencies = new List<long>();
            List<Task> levelTasks = new List<Task>();

            // Send messages for this level
            for (int i = 0; i < messagesPerLevel; i++)
            {
                int messageIndex = (level * messagesPerLevel) + i;
                Task task = Task.Run(async () =>
                {
                    Stopwatch messageStart = Stopwatch.StartNew();

                    TopologyUpdateMessage topologyMessage = new TopologyUpdateMessage
                    {
                        SenderId = $"ScaleNode_{(messageIndex % 150) + 1:D3}",
                        SenderNodeId = (byte)((messageIndex % 150) + 1),
                        Version = messageIndex,
                        Topology = new TopologyMap
                        {
                            Version = messageIndex,
                            Timestamp = DateTime.UtcNow.AddSeconds(messageIndex)
                        }
                    };

                    await _messageHandler.ProcessMessageAsync(System.Text.Json.JsonSerializer.Serialize(topologyMessage));

                    messageStart.Stop();
                    lock (latencies)
                    {
                        latencies.Add(messageStart.ElapsedMilliseconds);
                    }
                });
                levelTasks.Add(task);
            }

            await Task.WhenAll(levelTasks);
            levelStart.Stop();

            levelLatencies.Add(latencies);

            // Wait for level duration
            if (level < loadLevels - 1)
            {
                await Task.Delay(levelDurationSeconds * 1000);
            }
        }

        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds <= (totalDurationSeconds + 10) * 1000, // Allow 10 seconds buffer
            $"Gradual load test took {stopwatch.ElapsedMilliseconds}ms, expected <= {(totalDurationSeconds + 10) * 1000}ms");

        // Check that performance doesn't degrade significantly
        for (int level = 0; level < loadLevels; level++)
        {
            List<long> latencies = levelLatencies[level];
            double avgLatency = latencies.Average();
            long maxLatency = latencies.Max();

            // Later levels should not have significantly worse performance
            if (level > 0)
            {
                double previousAvgLatency = levelLatencies[level - 1].Average();
                double latencyIncrease = avgLatency - previousAvgLatency;
                Assert.True(latencyIncrease <= 100, // Latency should not increase by more than 100ms
                    $"Level {level} latency increased by {latencyIncrease:F2}ms, expected <= 100ms");
            }

            Assert.True(maxLatency <= 1000, // Max latency should be under 1 second
                $"Level {level} max latency: {maxLatency}ms, expected <= 1000ms");
        }

        // Verify system stability
        TopologyMap topology = _topology.GetCurrentTopology();
        Assert.True(topology.Nodes.Count > 0, "System should still have nodes after gradual load increase");
    }

    [Fact]
    public async Task MemoryLeakTest_ExtendedLoad_ShouldNotLeak()
    {
        // Arrange
        const int testDurationSeconds = 60;
        const int messagesPerSecond = 50;
        const int totalMessages = testDurationSeconds * messagesPerSecond;
        const long maxMemoryIncreaseMB = 50; // Allow 50MB increase

        long initialMemory = GC.GetTotalMemory(true);
        Stopwatch stopwatch = Stopwatch.StartNew();

        await _communication.InitializeAsync("MasterNode");
        await _nodeDiscovery.StartDiscoveryAsync();
        await _topology.StartAsync();
        await _messageHandler.StartAsync();

        // Add nodes
        for (int i = 1; i <= 100; i++)
        {
            DiscoveryMessage discoveryMessage = new DiscoveryMessage
            {
                SenderId = $"MemoryNode_{i:D3}",
                SenderNodeId = (byte)((i % 255) + 1),
                NodeType = NodeType.Repeater,
                IpAddress = $"192.168.1.{i}",
                MacAddress = $"AA:BB:CC:DD:EE:{i:X2}"
            };

            await _messageHandler.ProcessMessageAsync(System.Text.Json.JsonSerializer.Serialize(discoveryMessage));
        }

        List<Task> tasks = new List<Task>();

        // Act - Extended load test
        for (int i = 0; i < totalMessages; i++)
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
                            SenderId = $"MemoryNode_{(messageIndex % 100) + 1:D3}",
                            SenderNodeId = (byte)((messageIndex % 100) + 1),
                            Load = messageIndex % 100 / 100.0,
                            Uptime = TimeSpan.FromMinutes(messageIndex % 1000),
                            Metrics = new Dictionary<string, object>
                            {
                                ["cpu"] = messageIndex % 100,
                                ["memory"] = messageIndex % 100,
                                ["temperature"] = (messageIndex % 50) + 30
                            }
                        };
                        break;
                    case 1:
                        message = new DataTransmissionMessage
                        {
                            SenderId = $"MemoryNode_{(messageIndex % 100) + 1:D3}",
                            SenderNodeId = (byte)((messageIndex % 100) + 1),
                            DestinationId = $"MemoryNode_{(messageIndex % 100) + 2:D3}",
                            DestinationNodeId = (byte)((messageIndex % 100) + 2),
                            Payload = $"MemoryData_{messageIndex}_{Guid.NewGuid()}",
                            Priority = messageIndex % 10,
                            HopCount = messageIndex % 5,
                            MaxHops = 10
                        };
                        break;
                    case 2:
                        message = new TopologyUpdateMessage
                        {
                            SenderId = $"MemoryNode_{(messageIndex % 100) + 1:D3}",
                            SenderNodeId = (byte)((messageIndex % 100) + 1),
                            Version = messageIndex,
                            Topology = new TopologyMap
                            {
                                Version = messageIndex,
                                Timestamp = DateTime.UtcNow.AddSeconds(messageIndex)
                            }
                        };
                        break;
                    default:
                        message = new DiscoveryMessage
                        {
                            SenderId = $"MemoryNode_{(messageIndex % 100) + 1:D3}",
                            SenderNodeId = (byte)((messageIndex % 100) + 1),
                            NodeType = NodeType.Repeater,
                            SignalStrength = -60 + (messageIndex % 40),
                            BatteryLevel = 50 + (messageIndex % 50),
                            Capabilities = new Dictionary<string, object>
                            {
                                ["canRoute"] = true,
                                ["canStore"] = true,
                                ["canMonitor"] = true
                            }
                        };
                        break;
                }

                await _messageHandler.ProcessMessageAsync(System.Text.Json.JsonSerializer.Serialize(message));
            });
            tasks.Add(task);

            // Control message rate
            if (i % messagesPerSecond == 0 && i > 0)
            {
                await Task.Delay(1000);
            }
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
        Assert.True(stopwatch.ElapsedMilliseconds <= (testDurationSeconds + 10) * 1000, // Allow 10 seconds buffer
            $"Memory leak test took {stopwatch.ElapsedMilliseconds}ms, expected <= {(testDurationSeconds + 10) * 1000}ms");

        Assert.True(memoryIncreaseMB <= maxMemoryIncreaseMB,
            $"Memory increase: {memoryIncreaseMB}MB, expected <= {maxMemoryIncreaseMB}MB");

        // Verify system is still functional
        TopologyMap topology = _topology.GetCurrentTopology();
        Assert.True(topology.Nodes.Count > 0, "System should still have nodes after memory leak test");
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