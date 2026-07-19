using System.Text.Json;

using Xunit;

namespace MultiNodeNetwork.Tests;
public class NetworkMessageTests
{
    #region Public Methods

    [Fact]
    public void Deserialize_MissingFields_Defaults()
    {
        string json = "{\"messageType\":\"TestType\",\"senderNodeId\":16}";
        TestNetworkMessage? deserialized = JsonSerializer.Deserialize<TestNetworkMessage>(json);
        Assert.NotNull(deserialized);
        Assert.Equal("TestType", deserialized!.MessageType);
        Assert.Equal(16, deserialized.SenderNodeId);
        Assert.Equal(string.Empty, deserialized.SenderId); // default
        Assert.NotNull(deserialized.Data); // default empty dictionary
    }

    [Fact]
    public void GetMessageType_ReturnsCorrectType()
    {
        TestNetworkMessage msg = new TestNetworkMessage
        {
            MessageType = "CustomType"
        };
        Assert.Equal("CustomType", msg.GetMessageType());
    }

    [Fact]
    public void SerializeDeserialize_RoundTrip()
    {
        TestNetworkMessage msg = new TestNetworkMessage
        {
            MessageType = "TestType",
            SenderId = "Node001",
            SenderNodeId = 0x10,
            Timestamp = DateTime.UtcNow,
            MessageId = "test-123",
            Data = new Dictionary<string, object> { { "key", "test-value" } }
        };
        string json = JsonSerializer.Serialize(msg);
        TestNetworkMessage? deserialized = JsonSerializer.Deserialize<TestNetworkMessage>(json);
        Assert.NotNull(deserialized);
        Assert.Equal(msg.MessageType, deserialized!.MessageType);
        Assert.Equal(msg.SenderId, deserialized.SenderId);
        Assert.Equal(msg.SenderNodeId, deserialized.SenderNodeId);
        Assert.Equal(msg.MessageId, deserialized.MessageId);
        Assert.Equal("test-value", deserialized.Data["key"].ToString());
    }

    #endregion
}
