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
/// Tests for IP address update functionality
/// </summary>
public class IpAddressUpdateTests
{
    #region Fields

    private readonly Mock<Communication> _communicationMock;
    private readonly NetworkConfig _config;
    private readonly IpAddressMonitorService _ipAddressMonitorService;
    private readonly Mock<ILogger<IpAddressMonitorService>> _loggerMock;
    private readonly Mock<INetworkInterfaceService> _networkInterfaceServiceMock;
    private readonly Mock<NodeDiscovery> _nodeDiscoveryMock;

    #endregion

    #region Constructors

    public IpAddressUpdateTests()
    {
        _loggerMock = new Mock<ILogger<IpAddressMonitorService>>();
        _networkInterfaceServiceMock = new Mock<INetworkInterfaceService>();
        _communicationMock = new Mock<Communication>();
        _nodeDiscoveryMock = new Mock<NodeDiscovery>();

        // Create a real config object instead of mocking
        _config = new NetworkConfig
        {
            Timing = new TimingSettings
            {
                IpAddressCheckInterval = 60
            }
        };

        _ipAddressMonitorService = new IpAddressMonitorService(
            _loggerMock.Object,
            _config,
            _networkInterfaceServiceMock.Object,
            _communicationMock.Object,
            _nodeDiscoveryMock.Object);
    }

    #endregion

    #region Public Methods

    [Fact]
    public void IpAddressChangedEventArgs_ShouldContainCorrectData()
    {
        // Arrange
        string oldIp = "192.168.1.100";
        string newIp = "192.168.1.101";

        // Act
        IpAddressChangedEventArgs args = new IpAddressChangedEventArgs(oldIp, newIp);

        // Assert
        Assert.Equal(oldIp, args.OldIpAddress);
        Assert.Equal(newIp, args.NewIpAddress);
        Assert.True(args.Timestamp <= DateTime.UtcNow);
    }

    [Fact]
    public async Task IpAddressMonitorService_CheckForIpAddressChangesAsync_WithChange_ShouldSendMessage()
    {
        // Arrange
        NetworkInterfaceInfo networkInfo = new NetworkInterfaceInfo
        {
            IpAddress = "192.168.1.101", // Different IP
            MacAddress = "AA:BB:CC:DD:EE:FF",
            InterfaceName = "Wi-Fi",
            IsConnected = true
        };

        _networkInterfaceServiceMock.Setup(x => x.GetNetworkInterfaceInfo()).Returns(networkInfo);
        _nodeDiscoveryMock.Setup(x => x.GetMyNodeId()).Returns("Node_01");
        _nodeDiscoveryMock.Setup(x => x.GetMyNodeIdByte()).Returns((byte)0x01);
        _communicationMock.Setup(x => x.BroadcastMessageAsync(It.IsAny<IpAddressUpdateMessage>())).ReturnsAsync(true);

        // Act
        await _ipAddressMonitorService.CheckForIpAddressChangesAsync();

        // Assert
        _communicationMock.Verify(x => x.BroadcastMessageAsync(It.IsAny<IpAddressUpdateMessage>()), Times.Once);
    }

    [Fact]
    public async Task IpAddressMonitorService_CheckForIpAddressChangesAsync_WithNoChange_ShouldNotSendMessage()
    {
        // Arrange
        NetworkInterfaceInfo networkInfo = new NetworkInterfaceInfo
        {
            IpAddress = "192.168.1.100",
            MacAddress = "AA:BB:CC:DD:EE:FF",
            InterfaceName = "Wi-Fi",
            IsConnected = true
        };

        _networkInterfaceServiceMock.Setup(x => x.GetNetworkInterfaceInfo()).Returns(networkInfo);

        // Act
        await _ipAddressMonitorService.CheckForIpAddressChangesAsync();

        // Assert
        _communicationMock.Verify(x => x.BroadcastMessageAsync(It.IsAny<IpAddressUpdateMessage>()), Times.Never);
    }

    [Fact]
    public void IpAddressMonitorService_GetCurrentNetworkInfo_ShouldReturnInfo()
    {
        // Act
        NetworkInterfaceInfo info = _ipAddressMonitorService.GetCurrentNetworkInfo();

        // Assert
        Assert.NotNull(info);
        Assert.NotNull(info.IpAddress);
        Assert.NotNull(info.MacAddress);
        Assert.NotNull(info.InterfaceName);
    }

    [Fact]
    public async Task IpAddressMonitorService_SendIpAddressUpdateMessageAsync_ShouldSendCorrectMessage()
    {
        // Arrange
        string oldIp = "192.168.1.100";
        string newIp = "192.168.1.101";

        _nodeDiscoveryMock.Setup(x => x.GetMyNodeId()).Returns("Node_01");
        _nodeDiscoveryMock.Setup(x => x.GetMyNodeIdByte()).Returns((byte)0x01);
        _communicationMock.Setup(x => x.BroadcastMessageAsync(It.IsAny<IpAddressUpdateMessage>())).ReturnsAsync(true);

        // Act
        await _ipAddressMonitorService.SendIpAddressUpdateMessageAsync(oldIp, newIp);

        // Assert
        _communicationMock.Verify(x => x.BroadcastMessageAsync(It.Is<IpAddressUpdateMessage>(m =>
            m.OldIpAddress == oldIp &&
            m.NewIpAddress == newIp &&
            m.SenderId == "Node_01" &&
            m.SenderNodeId == 0x01)), Times.Once);
    }

    [Fact]
    public async Task IpAddressMonitorService_StartMonitoringAsync_ShouldInitializeCorrectly()
    {
        // Arrange
        NetworkInterfaceInfo networkInfo = new NetworkInterfaceInfo
        {
            IpAddress = "192.168.1.100",
            MacAddress = "AA:BB:CC:DD:EE:FF",
            InterfaceName = "Wi-Fi",
            IsConnected = true
        };

        _networkInterfaceServiceMock.Setup(x => x.GetNetworkInterfaceInfo()).Returns(networkInfo);

        // Act
        await _ipAddressMonitorService.StartMonitoringAsync();

        // Assert
        _networkInterfaceServiceMock.Verify(x => x.GetNetworkInterfaceInfo(), Times.Once);
    }

    [Fact]
    public void IpAddressUpdateMessage_ShouldHaveCorrectMessageType()
    {
        // Arrange & Act
        IpAddressUpdateMessage message = new IpAddressUpdateMessage();

        // Assert
        Assert.Equal("IP_ADDRESS_UPDATE", message.MessageType);
    }

    [Fact]
    public void IpAddressUpdateMessage_ShouldSerializeCorrectly()
    {
        // Arrange
        IpAddressUpdateMessage message = new IpAddressUpdateMessage
        {
            SenderId = "Node_01",
            SenderNodeId = 0x01,
            NodeId = 0x02,
            OldIpAddress = "192.168.1.100",
            NewIpAddress = "192.168.1.101",
            MacAddress = "AA:BB:CC:DD:EE:FF",
            InterfaceName = "Wi-Fi",
            Timestamp = DateTime.UtcNow
        };

        // Act
        string json = System.Text.Json.JsonSerializer.Serialize(message);

        // Assert
        Assert.Contains("IP_ADDRESS_UPDATE", json);
        Assert.Contains("192.168.1.100", json);
        Assert.Contains("192.168.1.101", json);
        Assert.Contains("AA:BB:CC:DD:EE:FF", json);
        Assert.Contains("Wi-Fi", json);
    }

    #endregion
}
