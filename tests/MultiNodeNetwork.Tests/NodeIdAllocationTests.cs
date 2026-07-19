using Microsoft.Extensions.Logging;
using MultiNodeNetwork.Common.Models;
using MultiNodeNetwork.Core.Configuration;
using MultiNodeNetwork.Core.Services;
using Xunit;

namespace MultiNodeNetwork.Tests;
/// <summary>
/// Tests for the node ID allocation system
/// </summary>
public class NodeIdAllocationTests
{
#region Public Methods

    [Fact]
    public void TestInvalidNodeIdOperations()
    {
        NetworkConfig config = new NetworkConfig();
        ILoggerFactory loggerFactory = new LoggerFactory();
        ILogger<NodeIdManager> logger = loggerFactory.CreateLogger<NodeIdManager>();
        NodeIdManager manager = new NodeIdManager(logger, config);

        // Test releasing non-allocated ID
        Assert.False(manager.ReleaseNodeId((byte)0x01), "Should not be able to release non-allocated ID");

        // Test allocating invalid specific ID - use values outside the valid ranges
        // 0x00 is actually valid (just not in any specific range), so we'll test with a different approach
        // Allocate a valid ID first, then try to allocate it again
        byte? validId = manager.AllocateNodeId(NodeType.Master);
        Assert.True(validId.HasValue);
        Assert.False(manager.AllocateSpecificNodeId(validId.Value), "Should not be able to allocate already allocated ID");
    }

    [Fact]
    public void TestNodeIdAllocation()
    {
        // Test range descriptions
        Assert.Equal("0x01-0x0F (1-15)", NodeIdAllocation.GetRangeDescription(NodeType.Master));
        Assert.Equal("0x10-0x7F (16-127)", NodeIdAllocation.GetRangeDescription(NodeType.Repeater));
        Assert.Equal("0x80-0xFF (128-255)", NodeIdAllocation.GetRangeDescription(NodeType.UserDevice));

        // Test total slots
        Assert.Equal(15, NodeIdAllocation.GetTotalSlots(NodeType.Master));
        Assert.Equal(112, NodeIdAllocation.GetTotalSlots(NodeType.Repeater));
        Assert.Equal(128, NodeIdAllocation.GetTotalSlots(NodeType.UserDevice));

        // Test formatting
        Assert.Equal("0x01 (1)", NodeIdAllocation.FormatNodeId((byte)0x01));
        Assert.Equal("0xFF (255)", NodeIdAllocation.FormatNodeId((byte)0xFF));

        // Test parsing
        Assert.Equal((byte)0x01, NodeIdAllocation.ParseNodeId("0x01"));
        Assert.Equal((byte)0x01, NodeIdAllocation.ParseNodeId("1"));
        Assert.Equal((byte)0xFF, NodeIdAllocation.ParseNodeId("0xFF"));
    }

    [Fact]
    public void TestNodeIdManager()
    {
        NetworkConfig config = new NetworkConfig();
        ILoggerFactory loggerFactory = new LoggerFactory();
        ILogger<NodeIdManager> logger = loggerFactory.CreateLogger<NodeIdManager>();
        NodeIdManager manager = new NodeIdManager(logger, config);

        // Test allocation
        byte? masterIdNullable = manager.AllocateNodeId(NodeType.Master);
        Assert.True(masterIdNullable.HasValue, "Master ID allocation should succeed");
        byte masterId = masterIdNullable.Value;
        Assert.Equal((byte)0x01, masterId);

        byte? repeaterIdNullable = manager.AllocateNodeId(NodeType.Repeater);
        Assert.True(repeaterIdNullable.HasValue, "Repeater ID allocation should succeed");
        byte repeaterId = repeaterIdNullable.Value;
        Assert.Equal((byte)0x10, repeaterId);

        byte? userIdNullable = manager.AllocateNodeId(NodeType.UserDevice);
        Assert.True(userIdNullable.HasValue, "User device ID allocation should succeed");
        byte userId = userIdNullable.Value;
        Assert.Equal((byte)0x80, userId);

        // Test specific allocation
        Assert.True(manager.AllocateSpecificNodeId((byte)0x02), "Should be able to allocate 0x02");
        Assert.False(manager.AllocateSpecificNodeId((byte)0x01), "Should not be able to allocate already allocated 0x01");

        // Test release
        Assert.True(manager.ReleaseNodeId((byte)0x01), "Should be able to release 0x01");
        Assert.True(manager.AllocateSpecificNodeId((byte)0x01), "Should be able to re-allocate 0x01 after release");

        // Test statistics
        NodeIdAllocationStatistics stats = manager.GetStatistics();
        Assert.Equal(4, stats.TotalAllocated);
        Assert.Equal(2, stats.AllocationInfo[NodeType.Master].AllocatedSlots);
    }

    [Fact]
    public void TestNodeIdManagerExhaustion()
    {
        NetworkConfig config = new NetworkConfig();
        ILoggerFactory loggerFactory = new LoggerFactory();
        ILogger<NodeIdManager> logger = loggerFactory.CreateLogger<NodeIdManager>();
        NodeIdManager manager = new NodeIdManager(logger, config);

        // Exhaust all master IDs
        for (int i = 0; i < 15; i++)
        {
            byte? id = manager.AllocateNodeId(NodeType.Master);
            Assert.True(id.HasValue, $"Master ID allocation {i + 1} should succeed");
        }

        // Try to allocate one more master ID
        byte? exhaustedId = manager.AllocateNodeId(NodeType.Master);
        Assert.False(exhaustedId.HasValue, "Should not be able to allocate more master IDs when exhausted");

        // Verify statistics
        NodeIdAllocationStatistics stats = manager.GetStatistics();
        Assert.Equal(15, stats.AllocationInfo[NodeType.Master].AllocatedSlots);
        Assert.Equal(15, stats.AllocationInfo[NodeType.Master].TotalSlots);
    }

    [Fact]
    public void TestNodeIdValidation()
    {
        // Test valid node IDs
        Assert.True(NodeIdAllocation.IsValidNodeId((byte)0x01, NodeType.Master));
        Assert.True(NodeIdAllocation.IsValidNodeId((byte)0x10, NodeType.Repeater));
        Assert.True(NodeIdAllocation.IsValidNodeId((byte)0x80, NodeType.UserDevice));

        // Test invalid node IDs
        Assert.False(NodeIdAllocation.IsValidNodeId((byte)0x10, NodeType.Master));
        Assert.False(NodeIdAllocation.IsValidNodeId((byte)0x80, NodeType.Repeater));
        Assert.False(NodeIdAllocation.IsValidNodeId((byte)0x01, NodeType.UserDevice));
    }

    [Fact]
    public void TestNodeTypeIdentification()
    {
        // Test master nodes (0x01-0x0F)
        Assert.Equal(NodeType.Master, NodeIdAllocation.GetNodeType((byte)0x01));
        Assert.Equal(NodeType.Master, NodeIdAllocation.GetNodeType((byte)0x0F));
        Assert.NotEqual(NodeType.Master, NodeIdAllocation.GetNodeType((byte)0x10));

        // Test repeater nodes (0x10-0x7F)
        Assert.Equal(NodeType.Repeater, NodeIdAllocation.GetNodeType((byte)0x10));
        Assert.Equal(NodeType.Repeater, NodeIdAllocation.GetNodeType((byte)0x73));
        Assert.Equal(NodeType.Repeater, NodeIdAllocation.GetNodeType((byte)0x7F));

        // Test user devices (0x80-0xFF)
        Assert.Equal(NodeType.UserDevice, NodeIdAllocation.GetNodeType((byte)0x80));
        Assert.Equal(NodeType.UserDevice, NodeIdAllocation.GetNodeType((byte)0xFF));
    }

#endregion
}
