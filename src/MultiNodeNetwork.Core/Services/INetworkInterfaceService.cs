using MultiNodeNetwork.Core.Models;
using System.Collections.Generic;
using System.Net.NetworkInformation;

namespace MultiNodeNetwork.Core.Services;

/// <summary>
/// Interface for network interface services
/// </summary>
public interface INetworkInterfaceService
{
    NetworkInterfaceInfo GetNetworkInterfaceInfo();
    List<NetworkInterfaceInfo> GetAllNetworkInterfaces();
    string? GetPrimaryIpAddress();
    string? GetPrimaryMacAddress();
    string? GetPrimaryInterfaceName();
    NetworkInterfaceType GetPrimaryInterfaceType();
}