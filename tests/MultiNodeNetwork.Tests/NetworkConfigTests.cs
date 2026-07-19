using Microsoft.Extensions.Configuration;

using MultiNodeNetwork.Core.Configuration;

using Xunit;

namespace MultiNodeNetwork.Tests;
public class NetworkConfigTests
{
    #region Public Methods

    [Fact]
    public void CanLoadFromConfigurationSection()
    {
        Dictionary<string, string?> inMemorySettings = new Dictionary<string, string?>
        {
            {"NetworkConfig:Network:Ssid", "TestSSID"},
            {"NetworkConfig:Network:Password", "TestPassword"},
            {"NetworkConfig:Network:BroadcastPort", "5001"},
            {"NetworkConfig:Network:TcpPort", "5002"},
            {"NetworkConfig:Network:MaxNodes", "42"},
            {"NetworkConfig:Network:CoverageArea", "500"},
            {"NetworkConfig:Network:RepeaterSpacing", "50"},
            {"NetworkConfig:Network:GridSize", "8"},
            {"NetworkConfig:Timing:DiscoveryInterval", "30"},
            {"NetworkConfig:Timing:HeartbeatInterval", "5"},
            {"NetworkConfig:Timing:MasterBroadcastInterval", "20"},
            {"NetworkConfig:Timing:NodeTimeout", "45"},
            {"NetworkConfig:Timing:ElectionTimeout", "15"},
            {"NetworkConfig:Timing:RetryInterval", "3"},
            {"NetworkConfig:Timing:MaxRetries", "5"},
        };
        IConfiguration config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
        NetworkConfig networkConfig = new NetworkConfig();
        config.GetSection("NetworkConfig").Bind(networkConfig);
        Assert.Equal("TestSSID", networkConfig.Network.Ssid);
        Assert.Equal("TestPassword", networkConfig.Network.Password);
        Assert.Equal(5001, networkConfig.Network.BroadcastPort);
        Assert.Equal(5002, networkConfig.Network.TcpPort);
        Assert.Equal(42, networkConfig.Network.MaxNodes);
        Assert.Equal(500, networkConfig.Network.CoverageArea);
        Assert.Equal(50, networkConfig.Network.RepeaterSpacing);
        Assert.Equal(8, networkConfig.Network.GridSize);
        Assert.Equal(30, networkConfig.Timing.DiscoveryInterval);
        Assert.Equal(5, networkConfig.Timing.HeartbeatInterval);
        Assert.Equal(20, networkConfig.Timing.MasterBroadcastInterval);
        Assert.Equal(45, networkConfig.Timing.NodeTimeout);
        Assert.Equal(15, networkConfig.Timing.ElectionTimeout);
        Assert.Equal(3, networkConfig.Timing.RetryInterval);
        Assert.Equal(5, networkConfig.Timing.MaxRetries);
    }

    #endregion
}
