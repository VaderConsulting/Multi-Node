namespace MultiNodeNetwork.Core.Services;

/// <summary>
/// Interface for WiFi management functionality
/// </summary>
public interface IWifiManager : IDisposable
{
    /// <summary>
    /// Event fired when WiFi connection is established
    /// </summary>
    event EventHandler<WifiConnectionEventArgs>? ConnectionEstablished;

    /// <summary>
    /// Event fired when WiFi connection is lost
    /// </summary>
    event EventHandler<WifiConnectionEventArgs>? ConnectionLost;

    /// <summary>
    /// Event fired when signal strength changes
    /// </summary>
    event EventHandler<SignalStrengthChangedEventArgs>? SignalStrengthChanged;

    /// <summary>
    /// Get current SSID
    /// </summary>
    string? CurrentSsid { get; }

    /// <summary>
    /// Check if connected to WiFi
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Get current signal strength
    /// </summary>
    int SignalStrength { get; }

    /// <summary>
    /// Connect to a WiFi network
    /// </summary>
    /// <param name="Ssid">Network SSID</param>
    /// <param name="Password">Network password</param>
    /// <returns>True if connection successful</returns>
    Task<bool> ConnectAsync(string Ssid, string Password);

    /// <summary>
    /// Disconnect from current WiFi network
    /// </summary>
    /// <returns>True if disconnection successful</returns>
    Task<bool> DisconnectAsync();

    /// <summary>
    /// Scan for available networks
    /// </summary>
    /// <returns>List of available networks</returns>
    Task<List<WifiNetwork>> ScanForNetworksAsync();

    /// <summary>
    /// Get signal quality description
    /// </summary>
    /// <returns>Signal quality string</returns>
    string GetSignalQuality();

    /// <summary>
    /// Get WiFi statistics
    /// </summary>
    /// <returns>WiFi statistics</returns>
    WifiStatistics GetStatistics();
}