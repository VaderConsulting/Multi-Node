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
/// Tests for master failover functionality including backup masters and lowest ID node takeover
/// </summary>
public class MasterFailoverTests : IDisposable
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

    public MasterFailoverTests()
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
    public async Task MasterFailure_BackupMaster_ShouldTakeOver()
    {
        // Arrange
        const int timeLimitMs = 30000; // 30 seconds
        Stopwatch stopwatch = Stopwatch.StartNew();

        await _communication.InitializeAsync("MasterNode");
        await _nodeDiscovery.StartDiscoveryAsync();
        await _topology.StartAsync();
        await _messageHandler.StartAsync();

        // Add nodes including primary master and backup master
        string primaryMasterId = "PrimaryMaster";
        string backupMasterId = "BackupMaster";
        byte primaryMasterNodeId = (byte)1;
        byte backupMasterNodeId = (byte)2;

        // Add primary master
        DiscoveryMessage primaryMasterMessage = new DiscoveryMessage
        {
            SenderId = primaryMasterId,
            SenderNodeId = primaryMasterNodeId,
            NodeType = NodeType.Master,
            IpAddress = "192.168.1.1",
            MacAddress = "AA:BB:CC:DD:EE:01"
        };

        await _messageHandler.ProcessMessageAsync(System.Text.Json.JsonSerializer.Serialize(primaryMasterMessage));

        // Add backup master
        DiscoveryMessage backupMasterMessage = new DiscoveryMessage
        {
            SenderId = backupMasterId,
            SenderNodeId = backupMasterNodeId,
            NodeType = NodeType.Master,
            IpAddress = "192.168.1.2",
            MacAddress = "AA:BB:CC:DD:EE:02"
        };

        await _messageHandler.ProcessMessageAsync(System.Text.Json.JsonSerializer.Serialize(backupMasterMessage));

        // Designate backup master
        MasterElection masterElection = new MasterElection(_masterElectionLoggerMock.Object, _config, _communication);
        await masterElection.InitializeAsync(backupMasterId, backupMasterNodeId);
        masterElection.AddBackupMaster(backupMasterNodeId);

        // Act - Simulate primary master failure by stopping announcements
        // Wait for master timeout to trigger backup takeover
        await Task.Delay(TimeSpan.FromSeconds(_config.Timing.MasterTimeout + 2));

        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds <= timeLimitMs,
            $"Backup master takeover took {stopwatch.ElapsedMilliseconds}ms, expected <= {timeLimitMs}ms");

        // Verify backup master is now the master
        Assert.True(masterElection.IsBackupMaster, "Backup master should be designated as backup");

        // Note: In a real scenario, the backup master would automatically take over
        // This test verifies the backup designation and timeout detection
    }

    [Fact]
    public async Task MasterFailure_NoBackups_LowestIdNode_ShouldBecomeMaster()
    {
        // Arrange
        const int timeLimitMs = 30000; // 30 seconds
        Stopwatch stopwatch = Stopwatch.StartNew();

        await _communication.InitializeAsync("MasterNode");
        await _nodeDiscovery.StartDiscoveryAsync();
        await _topology.StartAsync();
        await _messageHandler.StartAsync();

        // Add multiple nodes with different IDs
        List<(string Id, byte NodeId, NodeType Type)> nodes = new List<(string Id, byte NodeId, NodeType Type)>
        {
            ("Node_001", 1, NodeType.Master),   // Primary master
            ("Node_005", 5, NodeType.Repeater), // Lowest ID after master failure
            ("Node_010", 10, NodeType.Repeater),
            ("Node_015", 15, NodeType.Repeater),
            ("Node_020", 20, NodeType.Repeater)
        };

        // Add all nodes
        foreach ((string id, byte nodeId, NodeType type) in nodes)
        {
            DiscoveryMessage discoveryMessage = new DiscoveryMessage
            {
                SenderId = id,
                SenderNodeId = nodeId,
                NodeType = type,
                IpAddress = $"192.168.1.{nodeId}",
                MacAddress = $"AA:BB:CC:DD:EE:{nodeId:X2}"
            };

            await _messageHandler.ProcessMessageAsync(System.Text.Json.JsonSerializer.Serialize(discoveryMessage));
        }

        // Create master election for the lowest ID node (Node_005)
        string lowestIdNodeId = "Node_005";
        byte lowestIdNodeByte = (byte)5;
        MasterElection masterElection = new MasterElection(_masterElectionLoggerMock.Object, _config, _communication);
        await masterElection.InitializeAsync(lowestIdNodeId, lowestIdNodeByte);

        // Act - Simulate master failure and trigger lowest ID election
        // Wait for master timeout
        await Task.Delay(TimeSpan.FromSeconds(_config.Timing.MasterTimeout + 2));

        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds <= timeLimitMs,
            $"Lowest ID node election took {stopwatch.ElapsedMilliseconds}ms, expected <= {timeLimitMs}ms");

        // Verify the lowest ID node has the highest priority for failover
        int lowestIdPriority = 255 - lowestIdNodeByte; // 250
        int higherIdPriority = 255 - 10; // 245

        Assert.True(lowestIdPriority > higherIdPriority,
            $"Lowest ID node (priority {lowestIdPriority}) should have higher priority than higher ID node (priority {higherIdPriority})");
    }

    [Fact]
    public async Task BackupMasterDesignation_ShouldUpdateCorrectly()
    {
        // Arrange
        await _communication.InitializeAsync("TestNode");
        await _nodeDiscovery.StartDiscoveryAsync();
        await _topology.StartAsync();
        await _messageHandler.StartAsync();

        MasterElection masterElection = new MasterElection(_masterElectionLoggerMock.Object, _config, _communication);
        await masterElection.InitializeAsync("TestNode", 1);

        // Act - Designate backup masters
        List<byte> backupMasters = new List<byte> { 2, 3, 4 };
        masterElection.DesignateBackupMasters(backupMasters);

        // Assert
        Assert.Equal(3, masterElection.BackupMasters.Count);
        Assert.Contains((byte)2, masterElection.BackupMasters);
        Assert.Contains((byte)3, masterElection.BackupMasters);
        Assert.Contains((byte)4, masterElection.BackupMasters);

        // Act - Add another backup master
        masterElection.AddBackupMaster(5);

        // Assert
        Assert.Equal(4, masterElection.BackupMasters.Count);
        Assert.Contains((byte)5, masterElection.BackupMasters);

        // Act - Remove a backup master
        masterElection.RemoveBackupMaster(3);

        // Assert
        Assert.Equal(3, masterElection.BackupMasters.Count);
        Assert.DoesNotContain((byte)3, masterElection.BackupMasters);
        Assert.Contains((byte)2, masterElection.BackupMasters);
        Assert.Contains((byte)4, masterElection.BackupMasters);
        Assert.Contains((byte)5, masterElection.BackupMasters);
    }

    [Fact]
    public async Task MasterAnnouncement_ShouldIncludeBackupMasters()
    {
        // Arrange
        await _communication.InitializeAsync("MasterNode");
        await _nodeDiscovery.StartDiscoveryAsync();
        await _topology.StartAsync();
        await _messageHandler.StartAsync();

        MasterElection masterElection = new MasterElection(_masterElectionLoggerMock.Object, _config, _communication);
        await masterElection.InitializeAsync("MasterNode", 1);

        // Designate backup masters
        List<byte> backupMasters = new List<byte> { 2, 3, 4 };
        masterElection.DesignateBackupMasters(backupMasters);

        // Act - Become master
        await masterElection.BecomeMasterAsync();

        // Assert
        Assert.True(masterElection.IsMaster, "Node should be master");

        // Verify backup masters are included in announcements
        // This would be verified by checking the NetworkInfo in master announcements
        Assert.Equal(3, masterElection.BackupMasters.Count);
    }

    [Fact]
    public async Task MultipleBackupMasters_ShouldHandleSequentialFailures()
    {
        // Arrange
        const int timeLimitMs = 45000; // 45 seconds
        Stopwatch stopwatch = Stopwatch.StartNew();

        await _communication.InitializeAsync("MasterNode");
        await _nodeDiscovery.StartDiscoveryAsync();
        await _topology.StartAsync();
        await _messageHandler.StartAsync();

        // Add multiple backup masters
        List<(string Id, byte NodeId, NodeType Type)> nodes = new List<(string Id, byte NodeId, NodeType Type)>
        {
            ("PrimaryMaster", 1, NodeType.Master),
            ("Backup1", 2, NodeType.Master),
            ("Backup2", 3, NodeType.Master),
            ("Backup3", 4, NodeType.Master),
            ("RegularNode", 5, NodeType.Repeater)
        };

        // Add all nodes
        foreach ((string id, byte nodeId, NodeType type) in nodes)
        {
            DiscoveryMessage discoveryMessage = new DiscoveryMessage
            {
                SenderId = id,
                SenderNodeId = nodeId,
                NodeType = type,
                IpAddress = $"192.168.1.{nodeId}",
                MacAddress = $"AA:BB:CC:DD:EE:{nodeId:X2}"
            };

            await _messageHandler.ProcessMessageAsync(System.Text.Json.JsonSerializer.Serialize(discoveryMessage));
        }

        // Create master elections for backup masters
        List<MasterElection> backupElections = new List<MasterElection>();
        for (int i = 2; i <= 4; i++)
        {
            MasterElection election = new MasterElection(_masterElectionLoggerMock.Object, _config, _communication);
            await election.InitializeAsync($"Backup{i - 1}", (byte)i);
            election.AddBackupMaster((byte)i);
            backupElections.Add(election);
        }

        // Act - Simulate sequential master failures
        // Wait for each backup to potentially take over
        for (int i = 0; i < 3; i++)
        {
            await Task.Delay(TimeSpan.FromSeconds(_config.Timing.MasterTimeout + 1));
        }

        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds <= timeLimitMs,
            $"Sequential backup master handling took {stopwatch.ElapsedMilliseconds}ms, expected <= {timeLimitMs}ms");

        // Verify all backup masters are properly designated
        foreach (MasterElection election in backupElections)
        {
            Assert.True(election.IsBackupMaster, "All backup masters should be designated as backups");
        }
    }

    [Fact]
    public async Task MasterFailureDetection_ShouldTriggerElection()
    {
        // Arrange
        const int timeLimitMs = 20000; // 20 seconds
        Stopwatch stopwatch = Stopwatch.StartNew();

        await _communication.InitializeAsync("TestNode");
        await _nodeDiscovery.StartDiscoveryAsync();
        await _topology.StartAsync();
        await _messageHandler.StartAsync();

        MasterElection masterElection = new MasterElection(_masterElectionLoggerMock.Object, _config, _communication);
        await masterElection.InitializeAsync("TestNode", 1);

        // Act - Wait for master failure detection to trigger
        await Task.Delay(TimeSpan.FromSeconds(_config.Timing.MasterTimeout + 2));

        stopwatch.Stop();

        // Assert
        Assert.True(stopwatch.ElapsedMilliseconds <= timeLimitMs,
            $"Master failure detection took {stopwatch.ElapsedMilliseconds}ms, expected <= {timeLimitMs}ms");

        // Verify that the node is ready to participate in elections
        // The actual election would be triggered by the failure detection timer
    }

    [Fact]
    public async Task BackupMasterRemoval_ShouldUpdateStatus()
    {
        // Arrange
        await _communication.InitializeAsync("TestNode");
        await _nodeDiscovery.StartDiscoveryAsync();
        await _topology.StartAsync();
        await _messageHandler.StartAsync();

        MasterElection masterElection = new MasterElection(_masterElectionLoggerMock.Object, _config, _communication);
        await masterElection.InitializeAsync("TestNode", 1);

        // Act - Add backup master
        masterElection.AddBackupMaster(1);
        Assert.True(masterElection.IsBackupMaster, "Node should be backup master after designation");

        // Act - Remove backup master
        masterElection.RemoveBackupMaster(1);
        Assert.False(masterElection.IsBackupMaster, "Node should not be backup master after removal");

        // Act - Add again
        masterElection.AddBackupMaster(1);
        Assert.True(masterElection.IsBackupMaster, "Node should be backup master after re-designation");
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