namespace MultiNodeNetwork.Core.Services;

/// <summary>
/// Event arguments for WiFi connection events
/// </summary>
public class WifiConnectionEventArgs : EventArgs
{
    #region Properties

    /// <summary>
    /// The signal strength of the WiFi connection in dBm
    /// </summary>
    public int SignalStrength { get; }

    /// <summary>
    /// The SSID of the connected WiFi network
    /// </summary>
    public string Ssid { get; }

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the WifiConnectionEventArgs class
    /// </summary>
    /// <param name="ssid">The SSID of the connected WiFi network</param>
    /// <param name="signalStrength">The signal strength of the WiFi connection in dBm</param>
    public WifiConnectionEventArgs(string ssid, int signalStrength)
    {
        Ssid = ssid;
        SignalStrength = signalStrength;
    }

    #endregion
}
