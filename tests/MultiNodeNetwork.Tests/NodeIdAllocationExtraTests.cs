using MultiNodeNetwork.Common.Models;
using MultiNodeNetwork.Core.Services;
using Xunit;

namespace MultiNodeNetwork.Tests;
public class NodeIdAllocationExtraTests
{
#region Public Methods

    [Theory]
    [InlineData((byte)0x01, "0x01 (1)")]
    [InlineData((byte)0xFF, "0xFF (255)")]
    public void FormatNodeId_Valid(byte id, string expected)
    {
        Assert.Equal(expected, NodeIdAllocation.FormatNodeId(id));
    }

    [Theory]
    [InlineData((byte)0x00, NodeType.Master, false)]
    [InlineData((byte)0x01, NodeType.Master, true)]
    [InlineData((byte)0x10, NodeType.Repeater, true)]
    [InlineData((byte)0x7F, NodeType.Repeater, true)]
    [InlineData((byte)0x80, NodeType.UserDevice, true)]
    [InlineData((byte)0xFF, NodeType.UserDevice, true)]
    [InlineData((byte)0x0F, NodeType.Repeater, false)]
    public void IsValidNodeId_EdgeCases(byte id, NodeType type, bool expected)
    {
        Assert.Equal(expected, NodeIdAllocation.IsValidNodeId(id, type));
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("0x100")]
    [InlineData("256")]
    [InlineData("abc")]
    [InlineData("")]
    public void ParseNodeId_InvalidInputs(string input)
    {
        Assert.Null(NodeIdAllocation.ParseNodeId(input));
    }

    [Theory]
    [InlineData("0x01", (byte)0x01)]
    [InlineData("1", (byte)0x01)]
    [InlineData("0xFF", (byte)0xFF)]
    [InlineData("255", (byte)0xFF)]
    public void ParseNodeId_ValidInputs(string input, byte expected)
    {
        Assert.Equal(expected, NodeIdAllocation.ParseNodeId(input));
    }

#endregion
}
