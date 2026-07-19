using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;

namespace MultiNodeNetwork.Core.Services;
/// <summary>
/// Network interface information
/// </summary>
public class NetworkInterfaceInfo
{
    #region Properties

    public string InterfaceName { get; set; } = string.Empty;

    public NetworkInterfaceType InterfaceType { get; set; }

    public string IpAddress { get; set; } = string.Empty;

    public bool IsConnected { get; set; }

    public string MacAddress { get; set; } = string.Empty;

    #endregion
}

/// <summary>
/// Service for retrieving network interface information including IP and MAC addresses
/// </summary>
public class NetworkInterfaceService : INetworkInterfaceService
{
    #region Fields

    private readonly ILogger<NetworkInterfaceService> _logger;
    private readonly IPlatformService _platformService;

    #endregion

    #region Constructors

    public NetworkInterfaceService(ILogger<NetworkInterfaceService> logger, IPlatformService platformService)
    {
        _logger = logger;
        _platformService = platformService;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Check if the IP address has changed and return the new address if it has
    /// </summary>
    public string? CheckForIpAddressChange(string currentIpAddress)
    {
        string newIpAddress = GetPrimaryIpAddress();

        if (!string.IsNullOrEmpty(newIpAddress) && newIpAddress != currentIpAddress)
        {
            _logger.LogInformation("IP address changed from {OldIp} to {NewIp}", currentIpAddress, newIpAddress);
            return newIpAddress;
        }

        return null;
    }

    /// <summary>
    /// Get all available network interfaces with their information
    /// </summary>
    public List<NetworkInterfaceInfo> GetAllNetworkInterfaces()
    {
        List<NetworkInterfaceInfo> interfaces = [];

        try
        {
            NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (NetworkInterface iface in networkInterfaces)
            {
                if (iface.OperationalStatus == OperationalStatus.Up)
                {
                    IPAddress? ipAddress = iface.GetIPProperties()
                        .UnicastAddresses
                        .FirstOrDefault(addr => addr.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        ?.Address;

                    PhysicalAddress? macAddress = iface.GetPhysicalAddress();

                    if (ipAddress != null && macAddress != null)
                    {
                        interfaces.Add(new NetworkInterfaceInfo
                        {
                            IpAddress = ipAddress.ToString(),
                            MacAddress = string.Join(":", macAddress.GetAddressBytes().Select(b => b.ToString("X2"))),
                            InterfaceName = iface.Name,
                            InterfaceType = iface.NetworkInterfaceType,
                            IsConnected = true
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all network interfaces");
        }

        return interfaces;
    }

    /// <summary>
    /// Get network interface information including IP and MAC addresses
    /// </summary>
    public NetworkInterfaceInfo GetNetworkInterfaceInfo()
    {
        string ipAddress = GetPrimaryIpAddress();
        string macAddress = GetPrimaryMacAddress();

        return new NetworkInterfaceInfo
        {
            IpAddress = ipAddress,
            MacAddress = macAddress,
            InterfaceName = GetPrimaryInterfaceName(),
            InterfaceType = GetPrimaryInterfaceType(),
            IsConnected = !string.IsNullOrEmpty(ipAddress)
        };
    }

    /// <summary>
    /// Get the name of the primary network interface
    /// </summary>
    public string GetPrimaryInterfaceName()
    {
        try
        {
            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();

            NetworkInterface? primaryInterface = interfaces.FirstOrDefault(iface =>
                (iface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ||
                 iface.NetworkInterfaceType == NetworkInterfaceType.Ethernet) &&
                iface.OperationalStatus == OperationalStatus.Up);

            return primaryInterface?.Name ?? "Unknown";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting primary interface name");
            return "Unknown";
        }
    }

    /// <summary>
    /// Get the type of the primary network interface
    /// </summary>
    public NetworkInterfaceType GetPrimaryInterfaceType()
    {
        try
        {
            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();

            NetworkInterface? primaryInterface = interfaces.FirstOrDefault(iface =>
                (iface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ||
                 iface.NetworkInterfaceType == NetworkInterfaceType.Ethernet) &&
                iface.OperationalStatus == OperationalStatus.Up);

            return primaryInterface?.NetworkInterfaceType ?? NetworkInterfaceType.Unknown;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting primary interface type");
            return NetworkInterfaceType.Unknown;
        }
    }

    /// <summary>
    /// Get the primary IP address for the current node
    /// </summary>
    public string GetPrimaryIpAddress()
    {
        try
        {
            // Get all network interfaces
            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();

            // Find the primary interface (WiFi or Ethernet that's up and has an IP)
            NetworkInterface? primaryInterface = interfaces.FirstOrDefault(iface =>
                (iface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ||
                 iface.NetworkInterfaceType == NetworkInterfaceType.Ethernet) &&
                iface.OperationalStatus == OperationalStatus.Up &&
                iface.GetIPProperties().UnicastAddresses.Any(addr =>
                    addr.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork));

            if (primaryInterface != null)
            {
                IPAddress? ipAddress = primaryInterface.GetIPProperties()
                    .UnicastAddresses
                    .FirstOrDefault(addr => addr.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    ?.Address;

                if (ipAddress != null)
                {
                    _logger.LogDebug("Found primary IP address: {IpAddress} on interface: {InterfaceName}",
                        ipAddress, primaryInterface.Name);
                    return ipAddress.ToString();
                }
            }

            // Fallback: get any available IP address
            foreach (NetworkInterface iface in interfaces)
            {
                if (iface.OperationalStatus == OperationalStatus.Up)
                {
                    IPAddress? ipAddress = iface.GetIPProperties()
                        .UnicastAddresses
                        .FirstOrDefault(addr => addr.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        ?.Address;

                    if (ipAddress != null && !IPAddress.IsLoopback(ipAddress))
                    {
                        _logger.LogDebug("Using fallback IP address: {IpAddress} on interface: {InterfaceName}",
                            ipAddress, iface.Name);
                        return ipAddress.ToString();
                    }
                }
            }

            _logger.LogWarning("No suitable IP address found");
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting primary IP address");
            return string.Empty;
        }
    }

    /// <summary>
    /// Get the MAC address for the primary network interface
    /// </summary>
    public string GetPrimaryMacAddress()
    {
        try
        {
            // Get all network interfaces
            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();

            // Find the primary interface (WiFi or Ethernet that's up)
            NetworkInterface? primaryInterface = interfaces.FirstOrDefault(iface =>
                (iface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 ||
                 iface.NetworkInterfaceType == NetworkInterfaceType.Ethernet) &&
                iface.OperationalStatus == OperationalStatus.Up);

            if (primaryInterface != null)
            {
                PhysicalAddress? macAddress = primaryInterface.GetPhysicalAddress();
                if (macAddress != null)
                {
                    string macString = string.Join(":", macAddress.GetAddressBytes().Select(b => b.ToString("X2")));
                    _logger.LogDebug("Found primary MAC address: {MacAddress} on interface: {InterfaceName}",
                        macString, primaryInterface.Name);
                    return macString;
                }
            }

            // Fallback: get any available MAC address
            foreach (NetworkInterface iface in interfaces)
            {
                if (iface.OperationalStatus == OperationalStatus.Up)
                {
                    PhysicalAddress? macAddress = iface.GetPhysicalAddress();
                    if (macAddress != null && macAddress.GetAddressBytes().Length > 0)
                    {
                        string macString = string.Join(":", macAddress.GetAddressBytes().Select(b => b.ToString("X2")));
                        _logger.LogDebug("Using fallback MAC address: {MacAddress} on interface: {InterfaceName}",
                            macString, iface.Name);
                        return macString;
                    }
                }
            }

            _logger.LogWarning("No suitable MAC address found");
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting primary MAC address");
            return string.Empty;
        }
    }

    #endregion
}
