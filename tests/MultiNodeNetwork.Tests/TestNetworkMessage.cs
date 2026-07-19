using MultiNodeNetwork.Common.Models;

namespace MultiNodeNetwork.Tests;
// Concrete implementation for testing
public class TestNetworkMessage : NetworkMessage
{
    #region Public Methods

    public override string GetMessageType()
    {
        return MessageType;
    }

    #endregion
}
