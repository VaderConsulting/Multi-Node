using Microsoft.Extensions.Logging;
using MultiNodeNetwork.Common.Models;
using MultiNodeNetwork.Core.Configuration;
using MultiNodeNetwork.Core.Services;
using Xunit;

namespace MultiNodeNetwork.Tests;
public class NodeIdManagerTests
{
#region Private Methods

    private NodeIdManager CreateManager()
    {
        NetworkConfig config = new NetworkConfig();
        ILoggerFactory loggerFactory = new LoggerFactory();
        ILogger<NodeIdManager> logger = loggerFactory.CreateLogger<NodeIdManager>();
        return new NodeIdManager(logger, config);
    }

#endregion

#region Public Methods

    [Fact]
    public void AllocateAndRelease_AllNodeTypes()
    {
        NodeIdManager manager = CreateManager();
        byte? master = manager.AllocateNodeId(NodeType.Master);
        byte? repeater = manager.AllocateNodeId(NodeType.Repeater);
        byte? user = manager.AllocateNodeId(NodeType.UserDevice);
        Assert.True(master.HasValue);
        Assert.True(repeater.HasValue);
        Assert.True(user.HasValue);
        Assert.True(manager.ReleaseNodeId(master.Value));
        Assert.True(manager.ReleaseNodeId(repeater.Value));
        Assert.True(manager.ReleaseNodeId(user.Value));
    }

    [Fact]
    public void DoubleAllocationAndRelease()
    {
        NodeIdManager manager = CreateManager();
        byte? id = manager.AllocateNodeId(NodeType.Master);
        Assert.True(id.HasValue);
        Assert.False(manager.AllocateSpecificNodeId(id.Value)); // Already allocated
        Assert.True(manager.ReleaseNodeId(id.Value));
        Assert.False(manager.ReleaseNodeId(id.Value)); // Already released
    }

    [Fact]
    public void Statistics_AfterOperations()
    {
        NodeIdManager manager = CreateManager();
        byte? m1 = manager.AllocateNodeId(NodeType.Master);
        byte? m2 = manager.AllocateNodeId(NodeType.Master);
        byte? r1 = manager.AllocateNodeId(NodeType.Repeater);
        Assert.True(m1.HasValue);
        manager.ReleaseNodeId(m1.Value);
        NodeIdAllocationStatistics stats = manager.GetStatistics();
        Assert.Equal(1, stats.AllocationInfo[NodeType.Master].AllocatedSlots);
        Assert.Equal(1, stats.AllocationInfo[NodeType.Repeater].AllocatedSlots);
    }

#endregion
}
