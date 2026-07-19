using MultiNodeNetwork.Core.Models;

namespace MultiNodeNetwork.Core.Services;
/// <summary>
/// Event arguments for data received by this node
/// </summary>
public class DataReceivedEventArgs : EventArgs
{
    #region Properties

    /// <summary>
    /// The data transmission message that was received
    /// </summary>
    public DataTransmissionMessage Message
    {
        get;
    }

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the DataReceivedEventArgs class
    /// </summary>
    /// <param name="message">The data transmission message that was received</param>
    public DataReceivedEventArgs(DataTransmissionMessage message)
    {
        Message = message;
    }

    #endregion
}
