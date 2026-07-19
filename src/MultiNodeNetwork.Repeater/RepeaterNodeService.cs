using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MultiNodeNetwork.Core.Configuration;
using MultiNodeNetwork.Core.Protocols;
using MultiNodeNetwork.Core.Services;

namespace MultiNodeNetwork.Repeater;
/// <summary>
/// Background service for repeater node functionality
/// </summary>
public class RepeaterNodeService : BackgroundService
{
#region Fields

    private readonly Communication _communication;
    private readonly NetworkConfig _config;
    private readonly IpAddressMonitorService _ipAddressMonitorService;
    private readonly ILogger<RepeaterNodeService> _logger;
    private readonly MasterElection _masterElection;
    private readonly MessageHandler _messageHandler;
    private readonly NodeDiscovery _nodeDiscovery;
    private readonly NodeIdManager _nodeIdManager;
    private readonly Topology _topology;
    private readonly WifiManager _wifiManager;

#endregion

#region Constructors

    public RepeaterNodeService(
        ILogger<RepeaterNodeService> logger,
        NetworkConfig config,
        WifiManager wifiManager,
        MessageHandler messageHandler,
        NodeIdManager nodeIdManager,
        MasterElection masterElection,
        NodeDiscovery nodeDiscovery,
        Communication communication,
        Topology topology,
        IpAddressMonitorService ipAddressMonitorService)
    {
        _logger = logger;
        _config = config;
        _wifiManager = wifiManager;
        _messageHandler = messageHandler;
        _nodeIdManager = nodeIdManager;
        _masterElection = masterElection;
        _nodeDiscovery = nodeDiscovery;
        _communication = communication;
        _topology = topology;
        _ipAddressMonitorService = ipAddressMonitorService;
    }

#endregion

#region Private Methods

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Repeater node service starting...");

        try
        {
            // Initialize WiFi connection
            await _wifiManager.ConnectAsync(_config.Network.Ssid, _config.Network.Password);

            // Allocate node ID
            byte? nodeIdByte = _nodeIdManager.AllocateNodeId(MultiNodeNetwork.Common.Models.NodeType.Repeater);
            if (!nodeIdByte.HasValue)
            {
                _logger.LogError("Failed to allocate node ID for repeater");
                return;
            }

            string nodeId = $"Repeater_{nodeIdByte:X2}";

            // Initialize protocols
            await _masterElection.InitializeAsync(nodeId, nodeIdByte.Value);
            await _nodeDiscovery.InitializeAsync(nodeId, nodeIdByte.Value);
            await _communication.InitializeAsync(nodeId);
            await _topology.InitializeAsync(nodeId, nodeIdByte.Value);

            // Start message handling
            await _messageHandler.StartAsync();

            // Start IP address monitoring
            await _ipAddressMonitorService.StartMonitoringAsync();

            _logger.LogInformation("Repeater node {NodeId} (0x{NodeIdByte:X2}) started successfully", nodeId, nodeIdByte.Value);

            // Keep the service running
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Repeater node service cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in repeater node service");
        }
        finally
        {
            _logger.LogInformation("Repeater node service stopping...");

            // Stop IP address monitoring
            await _ipAddressMonitorService.StopMonitoringAsync();
        }
    }

#endregion
}
