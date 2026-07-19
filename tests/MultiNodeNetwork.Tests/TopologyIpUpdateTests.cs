using Microsoft.Extensions.Logging;
using Moq;
using MultiNodeNetwork.Common.Models;
using MultiNodeNetwork.Core.Configuration;
using MultiNodeNetwork.Core.Models;
using MultiNodeNetwork.Core.Protocols;
using Xunit;

namespace MultiNodeNetwork.Tests;
/// <summary>
/// Tests for Topology IP address update functionality
/// </summary>
public class TopologyIpUpdateTests
{
    #region Fields

    private readonly NetworkConfig _config;
    private readonly Mock<ILogger<Topology>> _loggerMock;
    private readonly Topology _topology;

    #endregion

    #region Constructors

    public TopologyIpUpdateTests()
    {
        _loggerMock = new Mock<ILogger<Topology>>();
        _config = new NetworkConfig();
        _topology = new Topology(_loggerMock.Object, _config);
    }

    #endregion

    #region Public Methods

    [Fact]
    public void AddNode_WithNetworkInfo_ShouldStoreIpAndMacAddress()
    {
        // Arrange
        NodeInfo node = new NodeInfo
        {
            Id = "Node_01",
            NodeId = 0x01,
            Type = NodeType.Repeater,
            IpAddress = "192.168.1.100",
            MacAddress = "AA:BB:CC:DD:EE:FF",
            InterfaceName = "Wi-Fi",
            Status = NodeStatus.Online
        };

        // Act
        _topology.AddNode(node);

        // Assert
        NodeInfo? retrievedNode = _topology.GetNode(0x01);
        Assert.NotNull(retrievedNode);
        Assert.Equal("192.168.1.100", retrievedNode.IpAddress);
        Assert.Equal("AA:BB:CC:DD:EE:FF", retrievedNode.MacAddress);
        Assert.Equal("Wi-Fi", retrievedNode.InterfaceName);
    }

    [Fact]
    public void GetCurrentTopology_ShouldReturnAllNodesWithNetworkInfo()
    {
        // Arrange
        NodeInfo node1 = new NodeInfo
        {
            Id = "Node_01",
            NodeId = 0x01,
            Type = NodeType.Repeater,
            IpAddress = "192.168.1.100",
            MacAddress = "AA:BB:CC:DD:EE:FF",
            InterfaceName = "Wi-Fi",
            Status = NodeStatus.Online
        };

        NodeInfo node2 = new NodeInfo
        {
            Id = "Node_02",
            NodeId = 0x02,
            Type = NodeType.Repeater,
            IpAddress = "192.168.1.101",
            MacAddress = "BB:CC:DD:EE:FF:00",
            InterfaceName = "Ethernet",
            Status = NodeStatus.Online
        };

        _topology.AddNode(node1);
        _topology.AddNode(node2);

        // Act
        TopologyMap topology = _topology.GetCurrentTopology();

        // Assert
        Assert.Equal(2, topology.Nodes.Count);
        Assert.Contains(topology.Nodes.Values, n => n.Id == "Node_01" && n.IpAddress == "192.168.1.100");
        Assert.Contains(topology.Nodes.Values, n => n.Id == "Node_02" && n.IpAddress == "192.168.1.101");
    }

    [Fact]
    public void GetNodesByType_ShouldReturnCorrectNodes()
    {
        // Arrange
        NodeInfo masterNode = new NodeInfo
        {
            Id = "Node_01",
            NodeId = 0x01,
            Type = NodeType.Master,
            IpAddress = "192.168.1.100",
            MacAddress = "AA:BB:CC:DD:EE:FF",
            InterfaceName = "Wi-Fi",
            Status = NodeStatus.Online
        };

        NodeInfo repeaterNode = new NodeInfo
        {
            Id = "Node_02",
            NodeId = 0x02,
            Type = NodeType.Repeater,
            IpAddress = "192.168.1.101",
            MacAddress = "BB:CC:DD:EE:FF:00",
            InterfaceName = "Ethernet",
            Status = NodeStatus.Online
        };

        _topology.AddNode(masterNode);
        _topology.AddNode(repeaterNode);

        // Act
        IEnumerable<NodeInfo> masterNodes = _topology.GetMasterNodes();
        IEnumerable<NodeInfo> repeaterNodes = _topology.GetRepeaterNodes();

        // Assert
        Assert.Single(masterNodes);
        Assert.Single(repeaterNodes);
        Assert.Contains(masterNodes, n => n.Id == "Node_01");
        Assert.Contains(repeaterNodes, n => n.Id == "Node_02");
    }

    [Fact]
    public void GetNode_ShouldReturnCorrectNode()
    {
        // Arrange
        NodeInfo node = new NodeInfo
        {
            Id = "Node_01",
            NodeId = 0x01,
            Type = NodeType.Repeater,
            IpAddress = "192.168.1.100",
            MacAddress = "AA:BB:CC:DD:EE:FF",
            InterfaceName = "Wi-Fi",
            Status = NodeStatus.Online
        };

        _topology.AddNode(node);

        // Act
        NodeInfo? foundNode = _topology.GetNode(0x01);

        // Assert
        Assert.NotNull(foundNode);
        Assert.Equal("Node_01", foundNode.Id);
        Assert.Equal(0x01, foundNode.NodeId);
    }

    [Fact]
    public void GetNode_WithNonExistentNode_ShouldReturnNull()
    {
        // Act
        NodeInfo? foundNode = _topology.GetNode(0x99);

        // Assert
        Assert.Null(foundNode);
    }

    [Fact]
    public void HasNode_ShouldReturnCorrectResult()
    {
        // Arrange
        NodeInfo node = new NodeInfo
        {
            Id = "Node_01",
            NodeId = 0x01,
            Type = NodeType.Repeater,
            IpAddress = "192.168.1.100",
            MacAddress = "AA:BB:CC:DD:EE:FF",
            InterfaceName = "Wi-Fi",
            Status = NodeStatus.Online
        };

        _topology.AddNode(node);

        // Act & Assert
        Assert.True(_topology.HasNode(0x01));
        Assert.False(_topology.HasNode(0x99));
    }

    [Fact]
    public void RemoveNode_ShouldRemoveNode()
    {
        // Arrange
        NodeInfo node = new NodeInfo
        {
            Id = "Node_01",
            NodeId = 0x01,
            Type = NodeType.Repeater,
            IpAddress = "192.168.1.100",
            MacAddress = "AA:BB:CC:DD:EE:FF",
            InterfaceName = "Wi-Fi",
            Status = NodeStatus.Online
        };

        _topology.AddNode(node);

        // Act
        _topology.RemoveNode(0x01);

        // Assert
        NodeInfo? removedNode = _topology.GetNode(0x01);
        Assert.Null(removedNode);
    }

    [Fact]
    public void UpdateNodeIpAddress_ShouldUpdateIpAndMacAddress()
    {
        // Arrange
        NodeInfo node = new NodeInfo
        {
            Id = "Node_01",
            NodeId = 0x01,
            Type = NodeType.Repeater,
            IpAddress = "192.168.1.100",
            MacAddress = "AA:BB:CC:DD:EE:FF",
            InterfaceName = "Wi-Fi",
            Status = NodeStatus.Online
        };

        _topology.AddNode(node);

        // Act
        bool result = _topology.UpdateNodeIpAddress(0x01, "192.168.1.101", "AA:BB:CC:DD:EE:FF", "Wi-Fi");

        // Assert
        Assert.True(result);
        NodeInfo? updatedNode = _topology.GetNode(0x01);
        Assert.NotNull(updatedNode);
        Assert.Equal("192.168.1.101", updatedNode.IpAddress);
        Assert.Equal("AA:BB:CC:DD:EE:FF", updatedNode.MacAddress);
    }

    [Fact]
    public void UpdateNodeIpAddress_WithNonExistentNode_ShouldReturnFalse()
    {
        // Act
        bool result = _topology.UpdateNodeIpAddress(0x99, "192.168.1.101", "AA:BB:CC:DD:EE:FF", "Wi-Fi");

        // Assert
        Assert.False(result);
    }

    #endregion
}
