using Microsoft.Extensions.Logging;
using Moq;
using MultiNodeNetwork.Common.Events;
using MultiNodeNetwork.Common.Models;
using MultiNodeNetwork.Core.Configuration;
using MultiNodeNetwork.Core.Models;
using MultiNodeNetwork.Core.Protocols;
using MultiNodeNetwork.Core.Services;
using Xunit;

namespace MultiNodeNetwork.Tests;
/// <summary>
/// Tests for MessageHandler functionality
/// </summary>
public class MessageHandlerTests : IDisposable
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

    public MessageHandlerTests()
    {
        _loggerMock = new Mock<ILogger<MessageHandler>>();
        _masterElectionMock = new Mock<IMasterElection>();
        _nodeDiscoveryMock = new Mock<NodeDiscovery>();
        _communicationMock = new Mock<Communication>();
        _topologyMock = new Mock<Topology>();

        // Use unique ports for each test instance to avoid conflicts
        int uniqueId = Environment.TickCount % 1000;
        _config = new NetworkConfig
        {
            Network = new NetworkSettings
            {
                BroadcastPort = 8888 + uniqueId,
                TcpPort = 8889 + uniqueId
            },
            Performance = new PerformanceSettings
            {
                BufferSize = 4096
            }
        };

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

    public void Dispose()
    {
        _messageHandler?.Dispose();
    }

    [Fact]
    public async Task HandleDataTransmissionAsync_MessageForOtherNode_ShouldRouteMessage()
    {
        // Arrange
        await _messageHandler.StartAsync();
        DataTransmissionMessage dataMessage = new DataTransmissionMessage
        {
            SenderId = "Node_02",
            SenderNodeId = 0x02,
            DestinationId = "Node_03",
            DestinationNodeId = 0x03,
            Payload = "Test data"
        };

        _nodeDiscoveryMock.Setup(x => x.GetMyNodeId()).Returns("Node_01");
        _nodeDiscoveryMock.Setup(x => x.GetMyNodeIdByte()).Returns((byte)0x01);

        List<byte> route = [0x01, 0x02, 0x03];
        _topologyMock.Setup(x => x.GetRoute(0x01, 0x03)).Returns(route);

        _communicationMock.Setup(x => x.BroadcastMessageAsync(It.IsAny<NetworkMessage>()))
            .ReturnsAsync(true);

        // Act
        await _messageHandler.ProcessMessageAsync(System.Text.Json.JsonSerializer.Serialize(dataMessage));

        // Assert
        _communicationMock.Verify(x => x.BroadcastMessageAsync(It.IsAny<NetworkMessage>()), Times.Once);
    }

    [Fact]
    public async Task HandleDataTransmissionAsync_MessageForThisNode_ShouldInvokeDataReceivedEvent()
    {
        // Arrange
        await _messageHandler.StartAsync();
        DataTransmissionMessage dataMessage = new DataTransmissionMessage
        {
            SenderId = "Node_02",
            SenderNodeId = 0x02,
            DestinationId = "Node_01",
            DestinationNodeId = 0x01,
            Payload = "Test data"
        };

        _nodeDiscoveryMock.Setup(x => x.GetMyNodeId()).Returns("Node_01");
        _nodeDiscoveryMock.Setup(x => x.GetMyNodeIdByte()).Returns((byte)0x01);

        bool eventRaised = false;
        _messageHandler.DataReceived += (sender, e) => eventRaised = true;

        // Act
        await _messageHandler.ProcessMessageAsync(System.Text.Json.JsonSerializer.Serialize(dataMessage));

        // Assert
        Assert.True(eventRaised);
    }

    [Fact]
    public async Task HandleDataTransmissionAsync_NoRouteFound_ShouldSendRouteErrorMessage()
    {
        // Arrange
        await _messageHandler.StartAsync();
        DataTransmissionMessage dataMessage = new DataTransmissionMessage
        {
            SenderId = "Node_02",
            SenderNodeId = 0x02,
            DestinationId = "Node_03",
            DestinationNodeId = 0x03,
            Payload = "Test data"
        };

        _nodeDiscoveryMock.Setup(x => x.GetMyNodeId()).Returns("Node_01");
        _nodeDiscoveryMock.Setup(x => x.GetMyNodeIdByte()).Returns((byte)0x01);

        _topologyMock.Setup(x => x.GetRoute(0x01, 0x03)).Returns([]);

        _communicationMock.Setup(x => x.BroadcastMessageAsync(It.IsAny<NetworkMessage>()))
            .ReturnsAsync(true);

        // Act
        await _messageHandler.ProcessMessageAsync(System.Text.Json.JsonSerializer.Serialize(dataMessage));

        // Assert
        _communicationMock.Verify(x => x.BroadcastMessageAsync(It.Is<NetworkMessage>(m => m.MessageType == "ROUTE_ERROR")), Times.Once);
    }

    [Fact]
    public async Task HandleNodeRegistrationAsync_ShouldAddNodeToTopologyAndSendConfirmation()
    {
        // Arrange
        await _messageHandler.StartAsync();
        NodeRegistrationMessage registrationMessage = new NodeRegistrationMessage
        {
            SenderId = "Node_02",
            SenderNodeId = 0x02,
            NodeInfo = new NodeInfo
            {
                Id = "Node_02",
                NodeId = 0x02,
                Type = NodeType.Repeater,
                SignalStrength = -60,
                BatteryLevel = 85
            },
            SignalStrength = -60,
            Position = new Location { X = 100, Y = 200, Z = 0 }
        };

        _nodeDiscoveryMock.Setup(x => x.GetMyNodeId()).Returns("Node_01");
        _nodeDiscoveryMock.Setup(x => x.GetMyNodeIdByte()).Returns((byte)0x01);
        _nodeDiscoveryMock.Setup(x => x.GetMyNodeType()).Returns(NodeType.Master);
        _nodeDiscoveryMock.Setup(x => x.GetMyCapabilities()).Returns(new Dictionary<string, object> { ["canRoute"] = true });
        _nodeDiscoveryMock.Setup(x => x.GetMySignalStrength()).Returns(-55);
        _nodeDiscoveryMock.Setup(x => x.GetMyBatteryLevel()).Returns(90);
        _nodeDiscoveryMock.Setup(x => x.GetMyLocation()).Returns(new Location { X = 0, Y = 0, Z = 0 });

        _communicationMock.Setup(x => x.BroadcastMessageAsync(It.IsAny<NetworkMessage>()))
            .ReturnsAsync(true);

        // Act
        await _messageHandler.ProcessMessageAsync(System.Text.Json.JsonSerializer.Serialize(registrationMessage));

        // Assert
        _topologyMock.Verify(x => x.AddNode(It.IsAny<NodeInfo>()), Times.Once);
        _communicationMock.Verify(x => x.BroadcastMessageAsync(It.IsAny<NetworkMessage>()), Times.Once);
    }

    [Fact]
    public async Task HandleRouteRequestAsync_WithDirectRoute_ShouldSendRouteReplyWithHighQuality()
    {
        // Arrange
        await _messageHandler.StartAsync();
        RouteRequestMessage routeRequest = new RouteRequestMessage
        {
            SenderId = "Node_02",
            SenderNodeId = 0x02,
            DestinationId = "Node_03",
            DestinationNodeId = 0x03,
            MessageId = "req-123"
        };

        _nodeDiscoveryMock.Setup(x => x.GetMyNodeId()).Returns("Node_01");
        _nodeDiscoveryMock.Setup(x => x.GetMyNodeIdByte()).Returns((byte)0x01);

        List<byte> route = [0x01, 0x03]; // Direct connection
        _topologyMock.Setup(x => x.GetRoute(0x01, 0x03)).Returns(route);

        RouteReplyMessage? capturedMessage = null;
        _communicationMock.Setup(x => x.BroadcastMessageAsync(It.IsAny<NetworkMessage>()))
            .Callback<NetworkMessage>(msg => capturedMessage = msg as RouteReplyMessage)
            .ReturnsAsync(true);

        // Act
        await _messageHandler.ProcessMessageAsync(System.Text.Json.JsonSerializer.Serialize(routeRequest));

        // Assert
        Assert.NotNull(capturedMessage);
        Assert.Equal(1.0, capturedMessage.Cost); // Direct connection should have quality 1.0
    }

    [Fact]
    public async Task HandleRouteRequestAsync_WithoutRoute_ShouldForwardRequest()
    {
        // Arrange
        await _messageHandler.StartAsync();
        RouteRequestMessage routeRequest = new RouteRequestMessage
        {
            SenderId = "Node_02",
            SenderNodeId = 0x02,
            DestinationId = "Node_03",
            DestinationNodeId = 0x03,
            MessageId = "req-123"
        };

        _nodeDiscoveryMock.Setup(x => x.GetMyNodeId()).Returns("Node_01");
        _nodeDiscoveryMock.Setup(x => x.GetMyNodeIdByte()).Returns((byte)0x01);

        _topologyMock.Setup(x => x.GetRoute(0x01, 0x03)).Returns([]);

        _communicationMock.Setup(x => x.BroadcastMessageAsync(It.IsAny<NetworkMessage>()))
            .ReturnsAsync(true);

        // Act
        await _messageHandler.ProcessMessageAsync(System.Text.Json.JsonSerializer.Serialize(routeRequest));

        // Assert
        _communicationMock.Verify(x => x.BroadcastMessageAsync(It.IsAny<NetworkMessage>()), Times.Once);
    }

    [Fact]
    public async Task HandleRouteRequestAsync_WithRoute_ShouldSendRouteReplyWithCorrectCost()
    {
        // Arrange
        await _messageHandler.StartAsync();
        RouteRequestMessage routeRequest = new RouteRequestMessage
        {
            SenderId = "Node_02",
            SenderNodeId = 0x02,
            DestinationId = "Node_03",
            DestinationNodeId = 0x03,
            MessageId = "req-123"
        };

        _nodeDiscoveryMock.Setup(x => x.GetMyNodeId()).Returns("Node_01");
        _nodeDiscoveryMock.Setup(x => x.GetMyNodeIdByte()).Returns((byte)0x01);

        List<byte> route = [0x01, 0x02, 0x03];
        _topologyMock.Setup(x => x.GetRoute(0x01, 0x03)).Returns(route);

        RouteReplyMessage? capturedMessage = null;
        _communicationMock.Setup(x => x.BroadcastMessageAsync(It.IsAny<NetworkMessage>()))
            .Callback<NetworkMessage>(msg => capturedMessage = msg as RouteReplyMessage)
            .ReturnsAsync(true);

        // Act
        await _messageHandler.ProcessMessageAsync(System.Text.Json.JsonSerializer.Serialize(routeRequest));

        // Assert
        Assert.NotNull(capturedMessage);
        Assert.Equal("ROUTE_REPLY", capturedMessage.MessageType);
        Assert.Equal(route, capturedMessage.Route);
        Assert.Equal(0.25, capturedMessage.Cost); // 1.0 / (3 + 1) = 0.25
    }

    [Fact]
    public async Task HandleTopologyUpdateAsync_NewerVersion_ShouldUpdateTopology()
    {
        // Arrange
        await _messageHandler.StartAsync();
        TopologyUpdateMessage topologyUpdate = new TopologyUpdateMessage
        {
            SenderId = "Node_02",
            SenderNodeId = 0x02,
            Version = 5,
            Topology = new TopologyMap
            {
                Version = 5,
                Nodes = new Dictionary<byte, NodeInfo>
                {
                    [0x03] = new NodeInfo { Id = "Node_03", NodeId = 0x03, Type = NodeType.Repeater }
                },
                Connections =
                [
                    new Connection { FromNode = 0x01, ToNode = 0x03, SignalStrength = -60 }
                ]
            }
        };

        TopologyMap currentTopology = new TopologyMap { Version = 3 };
        _topologyMock.Setup(x => x.GetCurrentTopology()).Returns(currentTopology);

        _communicationMock.Setup(x => x.BroadcastMessageAsync(It.IsAny<NetworkMessage>()))
            .ReturnsAsync(true);

        // Act
        await _messageHandler.ProcessMessageAsync(System.Text.Json.JsonSerializer.Serialize(topologyUpdate));

        // Assert
        _topologyMock.Verify(x => x.AddNode(It.IsAny<NodeInfo>()), Times.Once);
        _topologyMock.Verify(x => x.AddConnection(It.IsAny<byte>(), It.IsAny<byte>(), It.IsAny<int>()), Times.Once);
        _communicationMock.Verify(x => x.BroadcastMessageAsync(It.IsAny<NetworkMessage>()), Times.Once);
    }

    [Fact]
    public async Task HandleTopologyUpdateAsync_OlderVersion_ShouldIgnore()
    {
        // Arrange
        await _messageHandler.StartAsync();
        TopologyUpdateMessage topologyUpdate = new TopologyUpdateMessage
        {
            SenderId = "Node_02",
            SenderNodeId = 0x02,
            Version = 3,
            Topology = new TopologyMap { Version = 3 }
        };

        TopologyMap currentTopology = new TopologyMap { Version = 5 };
        _topologyMock.Setup(x => x.GetCurrentTopology()).Returns(currentTopology);

        // Act
        await _messageHandler.ProcessMessageAsync(System.Text.Json.JsonSerializer.Serialize(topologyUpdate));

        // Assert
        _topologyMock.Verify(x => x.AddNode(It.IsAny<NodeInfo>()), Times.Never);
        _topologyMock.Verify(x => x.AddConnection(It.IsAny<byte>(), It.IsAny<byte>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task ProcessMessageAsync_WhenDisposed_ShouldReturnFalse()
    {
        // Arrange
        _messageHandler.Dispose();
        string messageJson = "{\"messageType\":\"DISCOVERY\",\"senderId\":\"Node_01\"}";

        // Act
        bool result = await _messageHandler.ProcessMessageAsync(messageJson);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ProcessMessageAsync_WithInvalidJson_ShouldReturnFalse()
    {
        // Arrange
        await _messageHandler.StartAsync();
        string invalidJson = "{ invalid json }";

        // Act
        bool result = await _messageHandler.ProcessMessageAsync(invalidJson);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ProcessMessageAsync_WithUnknownMessageType_ShouldReturnFalse()
    {
        // Arrange
        await _messageHandler.StartAsync();
        string messageJson = "{\"messageType\":\"UNKNOWN_TYPE\",\"senderId\":\"Node_01\"}";

        // Act
        bool result = await _messageHandler.ProcessMessageAsync(messageJson);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task ProcessMessageAsync_WithValidDiscoveryMessage_ShouldReturnTrue()
    {
        // Arrange
        await _messageHandler.StartAsync();
        DiscoveryMessage discoveryMessage = new DiscoveryMessage
        {
            SenderId = "Node_01",
            SenderNodeId = 0x01,
            NodeType = NodeType.Master
        };
        string messageJson = System.Text.Json.JsonSerializer.Serialize(discoveryMessage);

        _nodeDiscoveryMock.Setup(x => x.HandleDiscoveryMessageAsync(It.IsAny<DiscoveryMessage>()))
            .Returns(Task.CompletedTask);

        // Act
        bool result = await _messageHandler.ProcessMessageAsync(messageJson);

        // Assert
        Assert.True(result);
        _nodeDiscoveryMock.Verify(x => x.HandleDiscoveryMessageAsync(It.IsAny<DiscoveryMessage>()), Times.Once);
    }

    [Fact]
    public async Task StartAsync_ShouldStartTopologyService()
    {
        // Arrange
        _topologyMock.Setup(x => x.StartAsync()).Returns(Task.CompletedTask);

        // Act
        await _messageHandler.StartAsync();

        // Assert
        _topologyMock.Verify(x => x.StartAsync(), Times.Once);
    }

    [Fact]
    public async Task StopAsync_ShouldStopTopologyService()
    {
        // Arrange
        await _messageHandler.StartAsync();

        // Act
        await _messageHandler.StopAsync();

        // Assert
        _topologyMock.Verify(x => x.StopAsync(), Times.Once);
    }

    #endregion
}
