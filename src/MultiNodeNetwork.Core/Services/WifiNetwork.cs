namespace MultiNodeNetwork.Core.Services;

/// <summary>
/// Represents a WiFi network with its properties
/// </summary>
public class WifiNetwork
{
    #region Properties

    /// <summary>
    /// The frequency of the WiFi network in GHz
    /// </summary>
    public double Frequency { get; set; } = 2.4;

    /// <summary>
    /// The security type of the WiFi network (e.g., WPA2, WPA3, Open)
    /// </summary>
    public string Security { get; set; } = string.Empty;

    /// <summary>
    /// The signal strength of the WiFi network in dBm
    /// </summary>
    public int SignalStrength { get; set; } = 0;

    /// <summary>
    /// The SSID (Service Set Identifier) of the WiFi network
    /// </summary>
    public string Ssid { get; set; } = string.Empty;

    #endregion
}
