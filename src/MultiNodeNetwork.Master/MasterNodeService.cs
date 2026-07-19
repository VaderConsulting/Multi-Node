using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using MultiNodeNetwork.Common.Models;
using MultiNodeNetwork.Core.Services;

namespace MultiNodeNetwork.Master;
/// <summary>
/// Background service for the master node
/// </summary>
public class MasterNodeService : BackgroundService
{
    #region Fields

    private readonly IpAddressMonitorService _ipAddressMonitorService;
    private readonly ILogger<MasterNodeService> _logger;
    private readonly MasterNode _masterNode;
    private readonly Timer _statusTimer;

    #endregion

    #region Constructors

    public MasterNodeService(ILogger<MasterNodeService> logger, MasterNode masterNode, IpAddressMonitorService ipAddressMonitorService)
    {
        _logger = logger;
        _masterNode = masterNode;
        _ipAddressMonitorService = ipAddressMonitorService;
        _statusTimer = new Timer(LogStatus, null, Timeout.Infinite, Timeout.Infinite);
    }

    #endregion

    #region Private Methods

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("Starting Master Node Service");

            // Start the master node
            await _masterNode.StartAsync();

            // Start IP address monitoring
            await _ipAddressMonitorService.StartMonitoringAsync();

            // Start status logging timer (every 30 seconds)
            _ = _statusTimer.Change(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));

            _logger.LogInformation("Master Node Service started successfully");

            // Keep the service running until cancellation is requested
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Master Node Service cancellation requested");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in Master Node Service");
            throw;
        }
        finally
        {
            _logger.LogInformation("Stopping Master Node Service");

            // Stop the status timer
            _ = _statusTimer.Change(Timeout.Infinite, Timeout.Infinite);

            // Stop IP address monitoring
            await _ipAddressMonitorService.StopMonitoringAsync();

            // Stop the master node
            await _masterNode.StopAsync();

            _logger.LogInformation("Master Node Service stopped");
        }
    }

    private void LogStatus(object? state)
    {
        try
        {
            NetworkStatistics stats = _masterNode.GetNetworkStatistics();

            _logger.LogInformation(
                "Network Status - Node: 0x{NodeIdByte:X2}, Master: {IsMaster}, Current Master: 0x{CurrentMasterNodeId:X2}, " +
                "Nodes: {OnlineNodes}/{TotalNodes}, Topology Version: {TopologyVersion}, " +
                "Uptime: {Uptime:hh\\:mm\\:ss}, Load: {Load:F2}%",
                stats.NodeIdByte,
                stats.IsMaster ? "YES" : "NO",
                stats.CurrentMasterNodeId,
                stats.OnlineNodes,
                stats.TotalNodes,
                stats.TopologyVersion,
                stats.Uptime,
                stats.NetworkLoad);

            // Log node type distribution
            if (stats.NodeTypeCounts.Any())
            {
                string typeSummary = string.Join(", ", stats.NodeTypeCounts.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
                _logger.LogInformation("Node Type Distribution: {TypeSummary}", typeSummary);
            }

            // Log node ID allocation statistics every 2 minutes
            if (DateTime.UtcNow.Minute % 2 == 0)
            {
                Core.Services.NodeIdAllocationStatistics allocationStats = stats.NodeIdAllocationStats;
                _logger.LogInformation("Node ID Allocation - Total: {TotalAllocated}/{TotalAvailable}, " +
                    "Masters: {MasterAllocated}/{MasterTotal}, Repeaters: {RepeaterAllocated}/{RepeaterTotal}, " +
                    "Users: {UserAllocated}/{UserTotal}",
                    allocationStats.TotalAllocated, allocationStats.TotalAvailable,
                    allocationStats.AllocationInfo.GetValueOrDefault(NodeType.Master)?.AllocatedSlots ?? 0,
                    allocationStats.AllocationInfo.GetValueOrDefault(NodeType.Master)?.TotalSlots ?? 0,
                    allocationStats.AllocationInfo.GetValueOrDefault(NodeType.Repeater)?.AllocatedSlots ?? 0,
                    allocationStats.AllocationInfo.GetValueOrDefault(NodeType.Repeater)?.TotalSlots ?? 0,
                    allocationStats.AllocationInfo.GetValueOrDefault(NodeType.UserDevice)?.AllocatedSlots ?? 0,
                    allocationStats.AllocationInfo.GetValueOrDefault(NodeType.UserDevice)?.TotalSlots ?? 0);
            }

            // Log detailed node information every 5 minutes
            if (DateTime.UtcNow.Minute % 5 == 0)
            {
                List<Core.Protocols.DiscoveredNode> allNodes = _masterNode.GetAllNodes().ToList();
                if (allNodes.Any())
                {
                    string nodeList = string.Join(", ", allNodes.Select(n => $"0x{n.NodeIdByte:X2}({n.NodeType})"));
                    _logger.LogInformation("Active Nodes ({Count}): {NodeList}", allNodes.Count, nodeList);
                }

                // Log topology statistics
                Core.Protocols.TopologyStatistics topologyStats = _masterNode.GetTopologyStatistics();
                _logger.LogInformation("Topology Stats - Connections: {Connections}, Avg Connections/Node: {AvgConnections:F1}",
                    topologyStats.TotalConnections, topologyStats.AverageConnectionsPerNode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging network status");
        }
    }

    #endregion

    #region Public Methods

    public override void Dispose()
    {
        _statusTimer?.Dispose();
        base.Dispose();
    }

    #endregion
}
