using Microsoft.Extensions.Logging;
using MultiNodeNetwork.Common.Models;
using MultiNodeNetwork.Core.Configuration;
using MultiNodeNetwork.Core.Models;
using MultiNodeNetwork.Core.Protocols;

namespace MultiNodeNetwork.Core.Services;
/// <summary>
/// Event arguments for IP address change events
/// </summary>
public class IpAddressChangedEventArgs : EventArgs
{
#region Properties

    public string NewIpAddress { get; }

    public string OldIpAddress { get; }

    public DateTime Timestamp { get; }

#endregion

#region Constructors

    public IpAddressChangedEventArgs(string oldIpAddress, string newIpAddress)
    {
        OldIpAddress = oldIpAddress;
        NewIpAddress = newIpAddress;
        Timestamp = DateTime.UtcNow;
    }

#endregion
}

/// <summary>
/// Service that monitors IP address changes and sends update messages
/// </summary>
public class IpAddressMonitorService : IDisposable
{
#region Events

    public event EventHandler<IpAddressChangedEventArgs>? IpAddressChanged;

#endregion

#region Fields

    private readonly Communication _communication;
    private readonly NetworkConfig _config;
    private string _currentInterfaceName = string.Empty;
    private string _currentIpAddress = string.Empty;
    private string _currentMacAddress = string.Empty;
    private bool _disposed = false;
    private readonly ILogger<IpAddressMonitorService> _logger;
    private readonly Timer _monitoringTimer;
    private readonly INetworkInterfaceService _networkInterfaceService;
    private readonly NodeDiscovery _nodeDiscovery;

#endregion

#region Constructors

    public IpAddressMonitorService(
        ILogger<IpAddressMonitorService> logger,
        NetworkConfig config,
        INetworkInterfaceService networkInterfaceService,
        Communication communication,
        NodeDiscovery nodeDiscovery)
    {
        _logger = logger;
        _config = config;
        _networkInterfaceService = networkInterfaceService;
        _communication = communication;
        _nodeDiscovery = nodeDiscovery;
        _monitoringTimer = new Timer(CheckForIpAddressChanges, null, Timeout.Infinite, Timeout.Infinite);
    }

#endregion

#region Private Methods

    private async void CheckForIpAddressChanges(object? state)
    {
        await CheckForIpAddressChangesAsync();
    }

#endregion

#region Public Methods

    /// <summary>
    /// Manually check for IP address changes and send update if needed
    /// </summary>
    public async Task CheckForIpAddressChangesAsync()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            NetworkInterfaceInfo networkInfo = _networkInterfaceService.GetNetworkInterfaceInfo();

            if (networkInfo.IpAddress != _currentIpAddress)
            {
                string oldIpAddress = _currentIpAddress;

                // Update current values
                _currentIpAddress = networkInfo.IpAddress;
                _currentMacAddress = networkInfo.MacAddress;
                _currentInterfaceName = networkInfo.InterfaceName;

                _logger.LogInformation("IP address changed: {OldIp} -> {NewIp}", oldIpAddress, _currentIpAddress);

                // Send IP address update message
                await SendIpAddressUpdateMessageAsync(oldIpAddress, _currentIpAddress);

                // Notify subscribers
                IpAddressChanged?.Invoke(this, new IpAddressChangedEventArgs(oldIpAddress, _currentIpAddress));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for IP address changes");
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        // Stop monitoring
        _ = StopMonitoringAsync();

        // Dispose timer
        _monitoringTimer?.Dispose();
    }

    /// <summary>
    /// Get current network interface information
    /// </summary>
    public NetworkInterfaceInfo GetCurrentNetworkInfo()
    {
        return new NetworkInterfaceInfo
        {
            IpAddress = _currentIpAddress,
            MacAddress = _currentMacAddress,
            InterfaceName = _currentInterfaceName,
            IsConnected = !string.IsNullOrEmpty(_currentIpAddress)
        };
    }

    /// <summary>
    /// Send an IP address update message to the network
    /// </summary>
    public async Task SendIpAddressUpdateMessageAsync(string oldIpAddress, string newIpAddress)
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            IpAddressUpdateMessage updateMessage = new IpAddressUpdateMessage
            {
                SenderId = _nodeDiscovery.GetMyNodeId(),
                SenderNodeId = _nodeDiscovery.GetMyNodeIdByte(),
                NodeId = _nodeDiscovery.GetMyNodeIdByte(),
                OldIpAddress = oldIpAddress,
                NewIpAddress = newIpAddress,
                MacAddress = _currentMacAddress,
                InterfaceName = _currentInterfaceName,
                Timestamp = DateTime.UtcNow
            };

            // Broadcast the IP address update message
            bool success = await _communication.BroadcastMessageAsync(updateMessage);

            if (success)
            {
                _logger.LogInformation("Successfully sent IP address update message: {OldIp} -> {NewIp}",
                    oldIpAddress, newIpAddress);
            }
            else
            {
                _logger.LogWarning("Failed to send IP address update message: {OldIp} -> {NewIp}",
                    oldIpAddress, newIpAddress);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending IP address update message");
        }
    }

    /// <summary>
    /// Start monitoring for IP address changes
    /// </summary>
    public async Task StartMonitoringAsync()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            // Get initial network interface information
            NetworkInterfaceInfo networkInfo = _networkInterfaceService.GetNetworkInterfaceInfo();
            _currentIpAddress = networkInfo.IpAddress;
            _currentMacAddress = networkInfo.MacAddress;
            _currentInterfaceName = networkInfo.InterfaceName;

            _logger.LogInformation("Started IP address monitoring. Current IP: {IpAddress}, MAC: {MacAddress}, Interface: {Interface}",
                _currentIpAddress, _currentMacAddress, _currentInterfaceName);

            // Start periodic monitoring
            _monitoringTimer.Change(
                TimeSpan.FromSeconds(_config.Timing.IpAddressCheckInterval),
                TimeSpan.FromSeconds(_config.Timing.IpAddressCheckInterval));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting IP address monitoring");
        }
    }

    /// <summary>
    /// Stop monitoring for IP address changes
    /// </summary>
    public async Task StopMonitoringAsync()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            _monitoringTimer.Change(Timeout.Infinite, Timeout.Infinite);
            _logger.LogInformation("Stopped IP address monitoring");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error stopping IP address monitoring");
        }
    }

#endregion
}
