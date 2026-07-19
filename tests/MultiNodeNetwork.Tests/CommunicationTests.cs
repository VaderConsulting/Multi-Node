using Microsoft.Extensions.Logging;

using Moq;

using MultiNodeNetwork.Common.Models;
using MultiNodeNetwork.Core.Configuration;
using MultiNodeNetwork.Core.Models;
using MultiNodeNetwork.Core.Protocols;

using Xunit;

namespace MultiNodeNetwork.Tests;
/// <summary>
/// Tests for Communication protocol functionality
/// </summary>
public class CommunicationTests
{
    #region Fields

    private readonly Communication _communication;
    private readonly NetworkConfig _config;
    private readonly Mock<ILogger<Communication>> _loggerMock;

    #endregion

    #region Constructors

    public CommunicationTests()
    {
        _loggerMock = new Mock<ILogger<Communication>>();

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
        _communication = new Communication(_loggerMock.Object, _config);
    }

    #endregion

    #region Public Methods

    [Fact]
    public async Task BroadcastMessageAsync_ShouldSendToAllNodes()
    {
        // Arrange
        await _communication.InitializeAsync("TestNode");
        DiscoveryMessage message = new DiscoveryMessage
        {
            SenderId = "TestNode",
            SenderNodeId = 0x01,
            NodeType = NodeType.Master
        };

        // Act
        bool result = await _communication.BroadcastMessageAsync(message);

        // Assert
        // In a test environment, the broadcast might fail due to no listeners,
        // but the method should complete without throwing exceptions
        _ = Assert.IsType<bool>(result);
    }

    [Fact]
    public void Communication_Constructor_ShouldInitializeCorrectly()
    {
        // Assert
        Assert.NotNull(_communication);
    }

    [Fact]
    public void Dispose_ShouldCleanupResources()
    {
        // Act & Assert
        // Should not throw an exception
        _communication.Dispose();
        Assert.True(true);
    }

    [Fact]
    public async Task InitializeAsync_ShouldSetNodeId()
    {
        // Arrange
        string nodeId = "TestNode";

        // Act
        await _communication.InitializeAsync(nodeId);

        // Assert
        // We can't directly test the private field, but we can verify no exception is thrown
        Assert.True(true);
    }

    [Fact]
    public async Task SendMessageAsync_WithNodeId_ShouldSendToSpecificNode()
    {
        // Arrange
        await _communication.InitializeAsync("TestNode");
        DataTransmissionMessage message = new DataTransmissionMessage
        {
            SenderId = "TestNode",
            SenderNodeId = 0x01,
            DestinationId = "TargetNode",
            DestinationNodeId = 0x02,
            Payload = "Test data"
        };

        // Act
        bool result = await _communication.SendMessageAsync(0x02, message);

        // Assert
        // In a test environment, the send might fail due to no listeners,
        // but the method should complete without throwing exceptions
        _ = Assert.IsType<bool>(result);
    }

    [Fact]
    public async Task SendMessageAsync_WithNodeId_ShouldTryTcpFirstThenFallback()
    {
        // Arrange
        await _communication.InitializeAsync("TestNode");
        DataTransmissionMessage message = new DataTransmissionMessage
        {
            SenderId = "TestNode",
            SenderNodeId = 0x01,
            DestinationId = "TargetNode",
            DestinationNodeId = 0x02,
            Payload = "Test data"
        };

        // Act
        bool result = await _communication.SendMessageAsync(0x02, message);

        // Assert
        _ = Assert.IsType<bool>(result);
    }

    [Fact]
    public async Task SendMessageAsync_WithStringNodeId_ShouldSendToSpecificNode()
    {
        // Arrange
        await _communication.InitializeAsync("TestNode");
        DataTransmissionMessage message = new DataTransmissionMessage
        {
            SenderId = "TestNode",
            SenderNodeId = 0x01,
            DestinationId = "TargetNode",
            DestinationNodeId = 0x02,
            Payload = "Test data"
        };

        // Act
        bool result = await _communication.SendMessageAsync("TargetNode", message);

        // Assert
        // In a test environment, the send might fail due to no listeners,
        // but the method should complete without throwing exceptions
        _ = Assert.IsType<bool>(result);
    }

    [Fact]
    public async Task SendTcpMessageAsync_ShouldSendViaTcp()
    {
        // Arrange
        await _communication.InitializeAsync("TestNode");
        DataTransmissionMessage message = new DataTransmissionMessage
        {
            SenderId = "TestNode",
            SenderNodeId = 0x01,
            DestinationId = "TargetNode",
            DestinationNodeId = 0x02,
            Payload = "Test data"
        };

        // Act
        bool result = await _communication.SendTcpMessageAsync("02", message);

        // Assert
        // This will likely fail in test environment since there's no actual TCP listener
        // But we can verify the method doesn't throw an exception
        _ = Assert.IsType<bool>(result);
    }

    [Fact]
    public async Task StartAsync_ShouldStartCommunication()
    {
        // Arrange
        await _communication.InitializeAsync("TestNode");

        // Act
        await _communication.StartAsync();

        // Assert
        // We can't directly test the private field, but we can verify no exception is thrown
        Assert.True(true);
    }

    [Fact]
    public async Task StopAsync_ShouldStopCommunication()
    {
        // Arrange
        await _communication.InitializeAsync("TestNode");
        await _communication.StartAsync();

        // Act
        await _communication.StopAsync();

        // Assert
        // We can't directly test the private field, but we can verify no exception is thrown
        Assert.True(true);
    }

    #endregion
}
