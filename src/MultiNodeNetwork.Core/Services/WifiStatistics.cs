namespace MultiNodeNetwork.Core.Services;

/// <summary>
/// Statistics for WiFi connection and performance
/// </summary>
public class WifiStatistics
{
    #region Properties

    /// <summary>
    /// The time when the WiFi connection was established
    /// </summary>
    public DateTime? ConnectionTime
    {
        get; set;
    }

    /// <summary>
    /// Indicates whether the WiFi is currently connected
    /// </summary>
    public bool IsConnected
    {
        get; set;
    }

    /// <summary>
    /// The quality rating of the WiFi signal (e.g., "Excellent", "Good", "Poor")
    /// </summary>
    public string SignalQuality { get; set; } = string.Empty;

    /// <summary>
    /// The signal strength of the WiFi connection in dBm
    /// </summary>
    public int SignalStrength { get; set; } = 0;

    /// <summary>
    /// The SSID of the connected WiFi network
    /// </summary>
    public string Ssid { get; set; } = string.Empty;

    #endregion
}
