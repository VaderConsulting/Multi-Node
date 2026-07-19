using MultiNodeNetwork.Core.Models;

namespace MultiNodeNetwork.Core.Protocols;

/// <summary>
/// Event arguments for discovery request events
/// </summary>
public class DiscoveryRequestEventArgs : EventArgs
{
    #region Properties

    /// <summary>
    /// The discovery message that triggered the event
    /// </summary>
    public DiscoveryMessage Message
    {
        get;
    }

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the DiscoveryRequestEventArgs class
    /// </summary>
    /// <param name="message">The discovery message that triggered the event</param>
    public DiscoveryRequestEventArgs(DiscoveryMessage message)
    {
        Message = message;
    }

    #endregion
}
