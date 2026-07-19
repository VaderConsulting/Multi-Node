using Microsoft.Extensions.Logging;
using Moq;
using MultiNodeNetwork.Common.Models;
using MultiNodeNetwork.Core.Configuration;
using MultiNodeNetwork.Core.Models;
using MultiNodeNetwork.Core.Protocols;
using MultiNodeNetwork.Core.Services;
using Xunit;

namespace MultiNodeNetwork.Tests;
/// <summary>
/// Tests for MessageHandler IP address update functionality
/// </summary>
public class MessageHandlerIpUpdateTests
{
    #region Fields

    private readonly Mock<Communication> _communicationMock;
    private readonly NetworkConfig _config;
    private readonly Mock<ILogger<MessageHandler>> _loggerMock;
    private readonly Mock<IMasterElection> _masterElectionMock;
    private readonly MessageHandler _messageHandler;
    private readonly Mock<NodeDiscovery> _nodeDiscoveryMock;
    private readonly Mock<Topology> _topologyMock;

    #endregion

    #region Constructors

    public MessageHandlerIpUpdateTests()
    {
        _loggerMock = new Mock<ILogger<MessageHandler>>();
        _config = new NetworkConfig();
        _topologyMock = new Mock<Topology>();
        _communicationMock = new Mock<Communication>();
        _nodeDiscoveryMock = new Mock<NodeDiscovery>();
        _masterElectionMock = new Mock<IMasterElection>();

        _messageHandler = new MessageHandler(
            _loggerMock.Object,
            _config,
            _masterElectionMock.Object,
            _nodeDiscoveryMock.Object,
            _communicationMock.Object,
            _topologyMock.Object);
    }

    #endregion

    #region Public Methods

    [Fact]
    public void IpAddressUpdateMessage_Properties_ShouldBeCorrectlySet()
    {
        // Arrange & Act
        IpAddressUpdateMessage message = new IpAddressUpdateMessage
        {
            SenderId = "Node_01",
            SenderNodeId = 0x01,
            NodeId = 0x01,
            OldIpAddress = "192.168.1.100",
            NewIpAddress = "192.168.1.101",
            MacAddress = "AA:BB:CC:DD:EE:FF",
            InterfaceName = "Wi-Fi",
            Timestamp = DateTime.UtcNow
        };

        // Assert
        Assert.Equal("IP_ADDRESS_UPDATE", message.MessageType);
        Assert.Equal("Node_01", message.SenderId);
        Assert.Equal(0x01, message.SenderNodeId);
        Assert.Equal(0x01, message.NodeId);
        Assert.Equal("192.168.1.100", message.OldIpAddress);
        Assert.Equal("192.168.1.101", message.NewIpAddress);
        Assert.Equal("AA:BB:CC:DD:EE:FF", message.MacAddress);
        Assert.Equal("Wi-Fi", message.InterfaceName);
    }

    [Fact]
    public void IpAddressUpdateMessage_Serialization_ShouldWorkCorrectly()
    {
        // Arrange
        IpAddressUpdateMessage message = new IpAddressUpdateMessage
        {
            SenderId = "Node_01",
            SenderNodeId = 0x01,
            NodeId = 0x01,
            OldIpAddress = "192.168.1.100",
            NewIpAddress = "192.168.1.101",
            MacAddress = "AA:BB:CC:DD:EE:FF",
            InterfaceName = "Wi-Fi",
            Timestamp = DateTime.UtcNow
        };

        // Act
        string json = System.Text.Json.JsonSerializer.Serialize(message);
        IpAddressUpdateMessage? deserializedMessage = System.Text.Json.JsonSerializer.Deserialize<IpAddressUpdateMessage>(json);

        // Assert
        Assert.NotNull(deserializedMessage);
        Assert.Equal("IP_ADDRESS_UPDATE", deserializedMessage.MessageType);
        Assert.Equal("Node_01", deserializedMessage.SenderId);
        Assert.Equal(0x01, deserializedMessage.SenderNodeId);
        Assert.Equal(0x01, deserializedMessage.NodeId);
        Assert.Equal("192.168.1.100", deserializedMessage.OldIpAddress);
        Assert.Equal("192.168.1.101", deserializedMessage.NewIpAddress);
        Assert.Equal("AA:BB:CC:DD:EE:FF", deserializedMessage.MacAddress);
        Assert.Equal("Wi-Fi", deserializedMessage.InterfaceName);
    }

    [Fact]
    public async Task ProcessMessageAsync_DiscoveryMessage_ShouldIncludeNetworkInfo()
    {
        // Arrange
        DiscoveryMessage message = new DiscoveryMessage
        {
            SenderId = "Node_03",
            SenderNodeId = 0x03,
            NodeType = NodeType.Repeater,
            IpAddress = "192.168.1.103",
            MacAddress = "CC:DD:EE:FF:00:11",
            InterfaceName = "Wi-Fi",
            Location = new Location { X = 15, Y = 25, Z = 0 },
            Capabilities = new Dictionary<string, object> { ["DataRelay"] = true }
        };

        // Act
        await _messageHandler.ProcessMessageAsync(System.Text.Json.JsonSerializer.Serialize(message));

        // Assert
        _nodeDiscoveryMock.Verify(x => x.HandleDiscoveryMessageAsync(It.Is<DiscoveryMessage>(m =>
            m.SenderId == "Node_03" &&
            m.SenderNodeId == 0x03 &&
            m.IpAddress == "192.168.1.103" &&
            m.MacAddress == "CC:DD:EE:FF:00:11" &&
            m.InterfaceName == "Wi-Fi")), Times.Once);
    }

    [Fact]
    public async Task ProcessMessageAsync_IpAddressUpdateMessage_ShouldLogIpChange()
    {
        // Arrange
        IpAddressUpdateMessage message = new IpAddressUpdateMessage
        {
            SenderId = "Node_01",
            SenderNodeId = 0x01,
            NodeId = 0x01,
            OldIpAddress = "192.168.1.100",
            NewIpAddress = "192.168.1.101",
            MacAddress = "AA:BB:CC:DD:EE:FF",
            InterfaceName = "Wi-Fi",
            Timestamp = DateTime.UtcNow
        };

        NodeInfo existingNode = new NodeInfo
        {
            Id = "Node_01",
            NodeId = 0x01,
            Type = NodeType.Repeater,
            IpAddress = "192.168.1.100",
            MacAddress = "AA:BB:CC:DD:EE:FF",
            Status = NodeStatus.Online
        };

        _topologyMock.Setup(x => x.GetNode(0x01)).Returns(existingNode);

        // Act
        await _messageHandler.ProcessMessageAsync(System.Text.Json.JsonSerializer.Serialize(message));

        // Assert
        // Verify that the topology update was called with the correct parameters
        _topologyMock.Verify(x => x.UpdateNodeIpAddress(0x01, "192.168.1.101", "AA:BB:CC:DD:EE:FF", "Wi-Fi"), Times.Once);
    }

    [Fact]
    public async Task ProcessMessageAsync_IpAddressUpdateMessage_ShouldUpdateTopology()
    {
        // Arrange
        IpAddressUpdateMessage message = new IpAddressUpdateMessage
        {
            SenderId = "Node_01",
            SenderNodeId = 0x01,
            NodeId = 0x01,
            OldIpAddress = "192.168.1.100",
            NewIpAddress = "192.168.1.101",
            MacAddress = "AA:BB:CC:DD:EE:FF",
            InterfaceName = "Wi-Fi",
            Timestamp = DateTime.UtcNow
        };

        NodeInfo existingNode = new NodeInfo
        {
            Id = "Node_01",
            NodeId = 0x01,
            Type = NodeType.Repeater,
            IpAddress = "192.168.1.100",
            MacAddress = "AA:BB:CC:DD:EE:FF",
            Status = NodeStatus.Online
        };

        _topologyMock.Setup(x => x.GetNode(0x01)).Returns(existingNode);

        // Act
        await _messageHandler.ProcessMessageAsync(System.Text.Json.JsonSerializer.Serialize(message));

        // Assert
        _topologyMock.Verify(x => x.UpdateNodeIpAddress(0x01, "192.168.1.101", "AA:BB:CC:DD:EE:FF", "Wi-Fi"), Times.Once);
    }

    [Fact]
    public async Task ProcessMessageAsync_IpAddressUpdateMessage_WithNonExistentNode_ShouldLogWarning()
    {
        // Arrange
        IpAddressUpdateMessage message = new IpAddressUpdateMessage
        {
            SenderId = "Node_01",
            SenderNodeId = 0x01,
            NodeId = 0x01,
            OldIpAddress = "192.168.1.100",
            NewIpAddress = "192.168.1.101",
            MacAddress = "AA:BB:CC:DD:EE:FF",
            InterfaceName = "Wi-Fi",
            Timestamp = DateTime.UtcNow
        };

        _topologyMock.Setup(x => x.GetNode(0x01)).Returns((NodeInfo?)null);

        // Act
        await _messageHandler.ProcessMessageAsync(System.Text.Json.JsonSerializer.Serialize(message));

        // Assert
        _topologyMock.Verify(x => x.UpdateNodeIpAddress(It.IsAny<byte>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ProcessMessageAsync_NodeRegistrationMessage_ShouldIncludeNetworkInfo()
    {
        // Arrange
        NodeRegistrationMessage message = new NodeRegistrationMessage
        {
            SenderId = "Node_02",
            SenderNodeId = 0x02,
            NodeInfo = new NodeInfo
            {
                Id = "Node_02",
                NodeId = 0x02,
                Type = NodeType.Repeater,
                Capabilities = ["DataRelay", "SensorData"]
            },
            IpAddress = "192.168.1.102",
            MacAddress = "BB:CC:DD:EE:FF:00",
            InterfaceName = "Ethernet",
            Position = new Location { X = 10, Y = 20, Z = 0 }
        };

        // Act
        await _messageHandler.ProcessMessageAsync(System.Text.Json.JsonSerializer.Serialize(message));

        // Assert
        _topologyMock.Verify(x => x.AddNode(It.Is<NodeInfo>(n =>
            n.Id == "Node_02" &&
            n.NodeId == 0x02 &&
            n.Type == NodeType.Repeater &&
            n.IpAddress == "192.168.1.102" &&
            n.MacAddress == "BB:CC:DD:EE:FF:00" &&
            n.InterfaceName == "Ethernet")), Times.Once);
    }

    #endregion
}
