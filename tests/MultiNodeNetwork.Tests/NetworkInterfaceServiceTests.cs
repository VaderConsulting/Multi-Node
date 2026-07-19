using Microsoft.Extensions.Logging;
using Moq;
using MultiNodeNetwork.Core.Services;
using Xunit;

namespace MultiNodeNetwork.Tests;
/// <summary>
/// Tests for NetworkInterfaceService functionality
/// </summary>
public class NetworkInterfaceServiceTests
{
    #region Fields

    private readonly Mock<ILogger<NetworkInterfaceService>> _loggerMock;
    private readonly NetworkInterfaceService _networkInterfaceService;
    private readonly Mock<IPlatformService> _platformServiceMock;

    #endregion

    #region Constructors

    public NetworkInterfaceServiceTests()
    {
        _loggerMock = new Mock<ILogger<NetworkInterfaceService>>();
        _platformServiceMock = new Mock<IPlatformService>();
        _networkInterfaceService = new NetworkInterfaceService(_loggerMock.Object, _platformServiceMock.Object);
    }

    #endregion

    #region Public Methods

    [Fact]
    public void CheckForIpAddressChange_WithSameIp_ShouldReturnNull()
    {
        // Arrange
        string currentIp = "192.168.1.100";

        // Act
        string? newIp = _networkInterfaceService.CheckForIpAddressChange(currentIp);

        // Assert
        Assert.Null(newIp);
    }

    [Fact]
    public void GetAllNetworkInterfaces_ShouldReturnList()
    {
        // Act
        List<NetworkInterfaceInfo> interfaces = _networkInterfaceService.GetAllNetworkInterfaces();

        // Assert
        Assert.NotNull(interfaces);
        Assert.IsType<List<NetworkInterfaceInfo>>(interfaces);
    }

    [Fact]
    public void GetNetworkInterfaceInfo_ShouldReturnCompleteInfo()
    {
        // Act
        NetworkInterfaceInfo info = _networkInterfaceService.GetNetworkInterfaceInfo();

        // Assert
        Assert.NotNull(info);
        Assert.NotNull(info.IpAddress);
        Assert.NotNull(info.MacAddress);
        Assert.NotNull(info.InterfaceName);

        // If we have an IP address, we should be connected
        if (!string.IsNullOrEmpty(info.IpAddress))
        {
            Assert.True(info.IsConnected);
        }
    }

    [Fact]
    public void GetPrimaryInterfaceName_ShouldReturnValidName()
    {
        // Act
        string interfaceName = _networkInterfaceService.GetPrimaryInterfaceName();

        // Assert
        Assert.NotNull(interfaceName);
        Assert.NotEqual("Unknown", interfaceName);
    }

    [Fact]
    public void GetPrimaryInterfaceType_ShouldReturnValidType()
    {
        // Act
        System.Net.NetworkInformation.NetworkInterfaceType interfaceType = _networkInterfaceService.GetPrimaryInterfaceType();

        // Assert
        Assert.NotEqual(System.Net.NetworkInformation.NetworkInterfaceType.Unknown, interfaceType);
    }

    [Fact]
    public void GetPrimaryIpAddress_ShouldReturnValidIpAddress()
    {
        // Act
        string ipAddress = _networkInterfaceService.GetPrimaryIpAddress();

        // Assert
        Assert.NotNull(ipAddress);
        // Should be either empty (if no network) or a valid IP format
        if (!string.IsNullOrEmpty(ipAddress))
        {
            Assert.Matches(@"^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}$", ipAddress);
        }
    }

    [Fact]
    public void GetPrimaryMacAddress_ShouldReturnValidMacAddress()
    {
        // Act
        string macAddress = _networkInterfaceService.GetPrimaryMacAddress();

        // Assert
        Assert.NotNull(macAddress);
        // Should be either empty (if no network) or a valid MAC format
        if (!string.IsNullOrEmpty(macAddress))
        {
            Assert.Matches(@"^([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})$", macAddress);
        }
    }

    #endregion
}
