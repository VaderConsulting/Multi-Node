using Microsoft.Extensions.Logging;
using MultiNodeNetwork.Core.Configuration;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace MultiNodeNetwork.Core.Services;
/// <summary>
/// WiFi connection manager for the network nodes
/// </summary>
public class WifiManager : IDisposable, IWifiManager
{
    #region Events

    public event EventHandler<WifiConnectionEventArgs>? ConnectionEstablished;

    public event EventHandler<WifiConnectionEventArgs>? ConnectionLost;

    public event EventHandler<SignalStrengthChangedEventArgs>? SignalStrengthChanged;

    #endregion

    #region Fields

    private readonly NetworkConfig _Config;
    private string? _CurrentSsid;
    private bool _Disposed = false;
    private bool _IsConnected = false;
    private readonly ILogger<WifiManager> _Logger;
    private readonly PlatformService _PlatformService;
    private int _SignalStrength = 0;

    #endregion

    #region Properties

    /// <summary>
    /// Get current SSID
    /// </summary>
    public string? CurrentSsid
    {
        get
        {
            return _CurrentSsid;
        }
    }

    /// <summary>
    /// Check if connected to WiFi
    /// </summary>
    public bool IsConnected
    {
        get
        {
            return _IsConnected;
        }
    }

    /// <summary>
    /// Get current signal strength
    /// </summary>
    public int SignalStrength
    {
        get
        {
            return _SignalStrength;
        }
    }

    #endregion

    #region Constructors

    public WifiManager(ILogger<WifiManager> Logger, NetworkConfig Config, PlatformService PlatformService)
    {
        _Logger = Logger;
        _Config = Config;
        _PlatformService = PlatformService;

        _Logger.LogInformation("WiFi Manager initialized for platform: {Platform}",
            _PlatformService.GetPlatformInfo().OperatingSystem);
    }

    #endregion

    #region Private Methods

    private async Task ConnectLinuxAsync(string Ssid, string Password)
    {
        try
        {
            string InterfaceName = _PlatformService.GetWiFiInterfaceName();

            // Create wpa_supplicant configuration
            string WpaConfig = $@"ctrl_interface=/var/run/wpa_supplicant
ctrl_interface_group=0
update_config=1

network={{
    ssid=""{Ssid}""
    psk=""{Password}""
    key_mgmt=WPA-PSK
}}";

            string ConfigFile = "/tmp/wpa_supplicant.conf";
            await File.WriteAllTextAsync(ConfigFile, WpaConfig);

            try
            {
                // Kill existing wpa_supplicant
                ProcessStartInfo KillInfo = new ProcessStartInfo
                {
                    FileName = "sudo",
                    Arguments = "pkill wpa_supplicant",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using Process KillProcess = Process.Start(KillInfo)!;
                await KillProcess.WaitForExitAsync();

                // Start wpa_supplicant
                ProcessStartInfo WpaInfo = new ProcessStartInfo
                {
                    FileName = "sudo",
                    Arguments = $"wpa_supplicant -B -i {InterfaceName} -c {ConfigFile}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using Process WpaProcess = Process.Start(WpaInfo)!;
                await WpaProcess.WaitForExitAsync();

                if (WpaProcess.ExitCode != 0)
                {
                    string Error = await WpaProcess.StandardError.ReadToEndAsync();
                    throw new Exception($"Failed to start wpa_supplicant: {Error}");
                }

                // Get IP address via DHCP
                ProcessStartInfo DhcpInfo = new ProcessStartInfo
                {
                    FileName = "sudo",
                    Arguments = $"dhclient {InterfaceName}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using Process DhcpProcess = Process.Start(DhcpInfo)!;
                await DhcpProcess.WaitForExitAsync();

                _Logger.LogInformation("Successfully connected to WiFi network: {Ssid}", Ssid);
            }
            finally
            {
                // Clean up config file
                if (File.Exists(ConfigFile))
                {
                    File.Delete(ConfigFile);
                }
            }
        }
        catch (Exception ex)
        {
            _Logger.LogError(ex, "Error connecting to WiFi on Linux");
            throw;
        }
    }

    private async Task ConnectWindowsAsync(string Ssid, string Password)
    {
        try
        {
            // Use netsh to connect to WiFi network
            string ProfileXml = $@"<?xml version=""1.0""?>
<WLANProfile xmlns=""http://www.microsoft.com/networking/WLAN/profile/v1"">
    <name>{Ssid}</name>
    <SSIDConfig>
        <SSID>
            <name>{Ssid}</name>
        </SSID>
    </SSIDConfig>
    <connectionType>ESS</connectionType>
    <connectionMode>auto</connectionMode>
    <MSM>
        <security>
            <authEncryption>
                <authentication>WPA2PSK</authentication>
                <encryption>AES</encryption>
                <useOneX>false</useOneX>
            </authEncryption>
            <sharedKey>
                <keyType>passPhrase</keyType>
                <protected>false</protected>
                <keyMaterial>{Password}</keyMaterial>
            </sharedKey>
        </security>
    </MSM>
</WLANProfile>";

            string TempFile = Path.GetTempFileName();
            await File.WriteAllTextAsync(TempFile, ProfileXml);

            try
            {
                // Add the profile
                ProcessStartInfo AddProfileInfo = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = $"wlan add profile filename=\"{TempFile}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using Process AddProfileProcess = Process.Start(AddProfileInfo)!;
                await AddProfileProcess.WaitForExitAsync();

                if (AddProfileProcess.ExitCode != 0)
                {
                    string Error = await AddProfileProcess.StandardError.ReadToEndAsync();
                    _Logger.LogWarning("Failed to add WiFi profile: {Error}", Error);
                }

                // Connect to the network
                ProcessStartInfo ConnectInfo = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = $"wlan connect name=\"{Ssid}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using Process ConnectProcess = Process.Start(ConnectInfo)!;
                await ConnectProcess.WaitForExitAsync();

                if (ConnectProcess.ExitCode != 0)
                {
                    string Error = await ConnectProcess.StandardError.ReadToEndAsync();
                    throw new Exception($"Failed to connect to WiFi: {Error}");
                }

                _Logger.LogInformation("Successfully connected to WiFi network: {Ssid}", Ssid);
            }
            finally
            {
                // Clean up temp file
                if (File.Exists(TempFile))
                {
                    File.Delete(TempFile);
                }
            }
        }
        catch (Exception ex)
        {
            _Logger.LogError(ex, "Error connecting to WiFi on Windows");
            throw;
        }
    }

    private async Task DisconnectLinuxAsync()
    {
        try
        {
            string InterfaceName = _PlatformService.GetWiFiInterfaceName();

            // Kill wpa_supplicant
            ProcessStartInfo KillInfo = new ProcessStartInfo
            {
                FileName = "sudo",
                Arguments = "pkill wpa_supplicant",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using Process KillProcess = Process.Start(KillInfo)!;
            await KillProcess.WaitForExitAsync();

            // Release DHCP lease
            ProcessStartInfo ReleaseInfo = new ProcessStartInfo
            {
                FileName = "sudo",
                Arguments = $"dhclient -r {InterfaceName}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using Process ReleaseProcess = Process.Start(ReleaseInfo)!;
            await ReleaseProcess.WaitForExitAsync();

            _Logger.LogInformation("Disconnected from WiFi network");
        }
        catch (Exception ex)
        {
            _Logger.LogError(ex, "Error disconnecting from WiFi on Linux");
        }
    }

    private async Task DisconnectWindowsAsync()
    {
        try
        {
            ProcessStartInfo DisconnectInfo = new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = "wlan disconnect",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using Process DisconnectProcess = Process.Start(DisconnectInfo)!;
            await DisconnectProcess.WaitForExitAsync();

            _Logger.LogInformation("Disconnected from WiFi network");
        }
        catch (Exception ex)
        {
            _Logger.LogError(ex, "Error disconnecting from WiFi on Windows");
        }
    }

    private int GetLinuxSignalStrength()
    {
        try
        {
            string InterfaceName = _PlatformService.GetWiFiInterfaceName();

            ProcessStartInfo Info = new ProcessStartInfo
            {
                FileName = "iwconfig",
                Arguments = InterfaceName,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using Process Process = Process.Start(Info)!;
            string Output = Process.StandardOutput.ReadToEnd();
            Process.WaitForExit();

            // Parse signal strength from output
            string[] Lines = Output.Split('\n');
            foreach (string Line in Lines)
            {
                if (Line.Contains("Link Quality") && Line.Contains("Signal level"))
                {
                    // Extract signal level (usually in dBm)
                    int SignalIndex = Line.IndexOf("Signal level=");
                    if (SignalIndex > 0)
                    {
                        string SignalPart = Line.Substring(SignalIndex + 13);
                        string[] SignalParts = SignalPart.Split(' ');
                        if (SignalParts.Length > 0)
                        {
                            string SignalStr = SignalParts[0];
                            if (int.TryParse(SignalStr, out int Signal))
                            {
                                return Signal;
                            }
                        }
                    }
                }
            }

            return -60; // Default if parsing fails
        }
        catch (Exception ex)
        {
            _Logger.LogError(ex, "Error getting Linux signal strength");
            return -60;
        }
    }

    private int GetPlatformSignalStrength()
    {
        try
        {
            if (_PlatformService.IsWindows())
            {
                return GetWindowsSignalStrength();
            }
            else if (_PlatformService.IsLinux())
            {
                return GetLinuxSignalStrength();
            }
            else
            {
                // Fallback to simulated signal strength
                return -60;
            }
        }
        catch (Exception ex)
        {
            _Logger.LogError(ex, "Error getting signal strength");
            return -60; // Fallback value
        }
    }

    private int GetWindowsSignalStrength()
    {
        try
        {
            ProcessStartInfo Info = new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = "wlan show interfaces",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using Process Process = Process.Start(Info)!;
            string Output = Process.StandardOutput.ReadToEnd();
            Process.WaitForExit();

            // Parse signal strength from output
            string[] Lines = Output.Split('\n');
            foreach (string Line in Lines)
            {
                if (Line.Contains("Signal") && Line.Contains("%"))
                {
                    string[] Parts = Line.Split(':');
                    if (Parts.Length > 1)
                    {
                        string SignalStr = Parts[1].Trim().Replace("%", "");
                        if (int.TryParse(SignalStr, out int SignalPercent))
                        {
                            // Convert percentage to dBm (approximate)
                            return SignalPercent switch
                            {
                                100 => -30,
                                90 => -40,
                                80 => -50,
                                70 => -55,
                                60 => -60,
                                50 => -65,
                                40 => -70,
                                30 => -75,
                                20 => -80,
                                10 => -85,
                                _ => -90
                            };
                        }
                    }
                }
            }

            return -60; // Default if parsing fails
        }
        catch (Exception ex)
        {
            _Logger.LogError(ex, "Error getting Windows signal strength");
            return -60;
        }
    }

    private async Task<List<WifiNetwork>> ScanNetworksLinuxAsync()
    {
        List<WifiNetwork> Networks = [];

        try
        {
            string InterfaceName = _PlatformService.GetWiFiInterfaceName();

            ProcessStartInfo Info = new ProcessStartInfo
            {
                FileName = "sudo",
                Arguments = $"iwlist {InterfaceName} scan",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using Process Process = Process.Start(Info)!;
            string Output = await Process.StandardOutput.ReadToEndAsync();
            await Process.WaitForExitAsync();

            // Parse networks from output
            string[] Lines = Output.Split('\n');
            string CurrentSsid = "";
            string CurrentSecurity = "";
            int CurrentSignal = -60;

            foreach (string Line in Lines)
            {
                string TrimmedLine = Line.Trim();
                if (TrimmedLine.Contains("ESSID:"))
                {
                    string[] Parts = TrimmedLine.Split('"');
                    if (Parts.Length > 1)
                    {
                        CurrentSsid = Parts[1];
                    }
                }
                else if (TrimmedLine.Contains("Encryption key:"))
                {
                    CurrentSecurity = TrimmedLine.Contains("on") ? "WPA2" : "Open";
                }
                else if (TrimmedLine.Contains("Signal level="))
                {
                    string[] Parts = TrimmedLine.Split('=');
                    if (Parts.Length > 1)
                    {
                        string SignalStr = Parts[1].Trim();
                        if (int.TryParse(SignalStr, out int Signal))
                        {
                            CurrentSignal = Signal;
                        }
                    }
                }
                else if (TrimmedLine.StartsWith("Cell") && !string.IsNullOrEmpty(CurrentSsid))
                {
                    // End of network entry, add to list
                    Networks.Add(new WifiNetwork
                    {
                        Ssid = CurrentSsid,
                        SignalStrength = CurrentSignal,
                        Security = CurrentSecurity,
                        Frequency = 2.4 // Default, could be parsed from output
                    });

                    // Reset for next network
                    CurrentSsid = "";
                    CurrentSecurity = "";
                    CurrentSignal = -60;
                }
            }
        }
        catch (Exception ex)
        {
            _Logger.LogError(ex, "Error scanning WiFi networks on Linux");
        }

        return Networks;
    }

    private async Task<List<WifiNetwork>> ScanNetworksWindowsAsync()
    {
        List<WifiNetwork> Networks = [];

        try
        {
            ProcessStartInfo Info = new ProcessStartInfo
            {
                FileName = "netsh",
                Arguments = "wlan show networks",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using Process Process = Process.Start(Info)!;
            string Output = await Process.StandardOutput.ReadToEndAsync();
            await Process.WaitForExitAsync();

            // Parse networks from output
            string[] Lines = Output.Split('\n');
            string CurrentSsid = "";
            string CurrentSecurity = "";
            int CurrentSignal = -60;

            foreach (string Line in Lines)
            {
                string TrimmedLine = Line.Trim();
                if (TrimmedLine.StartsWith("SSID"))
                {
                    string[] Parts = TrimmedLine.Split(':');
                    if (Parts.Length > 1)
                    {
                        CurrentSsid = Parts[1].Trim();
                    }
                }
                else if (TrimmedLine.StartsWith("Authentication"))
                {
                    string[] Parts = TrimmedLine.Split(':');
                    if (Parts.Length > 1)
                    {
                        CurrentSecurity = Parts[1].Trim();
                    }
                }
                else if (TrimmedLine.StartsWith("Signal"))
                {
                    string[] Parts = TrimmedLine.Split(':');
                    if (Parts.Length > 1)
                    {
                        string SignalStr = Parts[1].Trim().Replace("%", "");
                        if (int.TryParse(SignalStr, out int SignalPercent))
                        {
                            CurrentSignal = SignalPercent switch
                            {
                                100 => -30,
                                90 => -40,
                                80 => -50,
                                70 => -55,
                                60 => -60,
                                50 => -65,
                                40 => -70,
                                30 => -75,
                                20 => -80,
                                10 => -85,
                                _ => -90
                            };
                        }
                    }
                }
                else if (TrimmedLine.StartsWith("Network type"))
                {
                    // End of network entry, add to list
                    if (!string.IsNullOrEmpty(CurrentSsid))
                    {
                        Networks.Add(new WifiNetwork
                        {
                            Ssid = CurrentSsid,
                            SignalStrength = CurrentSignal,
                            Security = CurrentSecurity,
                            Frequency = 2.4 // Default, could be parsed from output
                        });

                        // Reset for next network
                        CurrentSsid = "";
                        CurrentSecurity = "";
                        CurrentSignal = -60;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _Logger.LogError(ex, "Error scanning WiFi networks on Windows");
        }

        return Networks;
    }

    private async Task StartSignalStrengthMonitoringAsync()
    {
        while (_IsConnected && !_Disposed)
        {
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(10)); // Check every 10 seconds

                int NewSignalStrength = GetPlatformSignalStrength();
                if (Math.Abs(NewSignalStrength - _SignalStrength) > 5) // Significant change
                {
                    int OldStrength = _SignalStrength;
                    _SignalStrength = NewSignalStrength;

                    _Logger.LogDebug("Signal strength changed: {OldStrength}dBm -> {NewStrength}dBm",
                        OldStrength, NewSignalStrength);

                    SignalStrengthChanged?.Invoke(this, new SignalStrengthChangedEventArgs(OldStrength, NewSignalStrength));
                }
            }
            catch (Exception ex)
            {
                _Logger.LogError(ex, "Error monitoring signal strength");
            }
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Connect to a WiFi network
    /// </summary>
    public async Task<bool> ConnectAsync(string Ssid, string Password)
    {
        if (_Disposed)
        {
            return false;
        }

        try
        {
            string InterfaceName = _PlatformService.GetWiFiInterfaceName();
            _Logger.LogInformation("Connecting to WiFi network: {Ssid} using interface: {Interface}",
                Ssid, InterfaceName);

            // Platform-specific connection logic
            if (_PlatformService.IsWindows())
            {
                // Windows-specific WiFi connection
                await ConnectWindowsAsync(Ssid, Password);
            }
            else if (_PlatformService.IsLinux())
            {
                // Linux-specific WiFi connection
                await ConnectLinuxAsync(Ssid, Password);
            }
            else
            {
                // Fallback to simulated connection
                await Task.Delay(2000); // Simulate connection time
            }

            _IsConnected = true;
            _CurrentSsid = Ssid;
            _SignalStrength = GetPlatformSignalStrength();

            _Logger.LogInformation("Successfully connected to WiFi network: {Ssid} (Signal: {SignalStrength}dBm)",
                Ssid, _SignalStrength);

            ConnectionEstablished?.Invoke(this, new WifiConnectionEventArgs(Ssid, _SignalStrength));

            // Start monitoring signal strength
            _ = StartSignalStrengthMonitoringAsync();

            return true;
        }
        catch (Exception ex)
        {
            _Logger.LogError(ex, "Failed to connect to WiFi network: {Ssid}", Ssid);
            return false;
        }
    }

    /// <summary>
    /// Disconnect from the current WiFi network
    /// </summary>
    public async Task<bool> DisconnectAsync()
    {
        if (_Disposed || !_IsConnected)
        {
            return false;
        }

        try
        {
            _Logger.LogInformation("Disconnecting from WiFi network: {Ssid}", _CurrentSsid);

            // Platform-specific disconnection
            if (_PlatformService.IsWindows())
            {
                await DisconnectWindowsAsync();
            }
            else if (_PlatformService.IsLinux())
            {
                await DisconnectLinuxAsync();
            }
            else
            {
                // Simulate disconnection process
                await Task.Delay(500);
            }

            string? Ssid = _CurrentSsid;
            _IsConnected = false;
            _CurrentSsid = null;
            _SignalStrength = 0;

            _Logger.LogInformation("Successfully disconnected from WiFi network: {Ssid}", Ssid);

            ConnectionLost?.Invoke(this, new WifiConnectionEventArgs(Ssid ?? "", 0));

            return true;
        }
        catch (Exception ex)
        {
            _Logger.LogError(ex, "Failed to disconnect from WiFi network");
            return false;
        }
    }

    public void Dispose()
    {
        if (_Disposed)
        {
            return;
        }

        _Disposed = true;

        // Disconnect if still connected
        if (_IsConnected)
        {
            _ = DisconnectAsync();
        }
    }

    /// <summary>
    /// Get signal quality description
    /// </summary>
    public string GetSignalQuality()
    {
        return _SignalStrength switch
        {
            >= -50 => "Excellent",
            >= -60 => "Good",
            >= -70 => "Fair",
            >= -80 => "Poor",
            _ => "Very Poor"
        };
    }

    /// <summary>
    /// Get network statistics
    /// </summary>
    public WifiStatistics GetStatistics()
    {
        return new WifiStatistics
        {
            IsConnected = _IsConnected,
            Ssid = _CurrentSsid ?? "",
            SignalStrength = _SignalStrength,
            SignalQuality = GetSignalQuality(),
            ConnectionTime = _IsConnected ? DateTime.UtcNow : null
        };
    }

    /// <summary>
    /// Scan for available networks
    /// </summary>
    public async Task<List<WifiNetwork>> ScanForNetworksAsync()
    {
        if (_Disposed)
        {
            return [];
        }

        try
        {
            _Logger.LogDebug("Scanning for available WiFi networks");

            List<WifiNetwork> Networks = [];

            if (_PlatformService.IsWindows())
            {
                Networks = await ScanNetworksWindowsAsync();
            }
            else if (_PlatformService.IsLinux())
            {
                Networks = await ScanNetworksLinuxAsync();
            }
            else
            {
                // Fallback to simulated networks
                await Task.Delay(1000);
                Networks =
                [
                    new WifiNetwork
                    {
                        Ssid = _Config.Network.Ssid,
                        SignalStrength = -55,
                        Security = "WPA2",
                        Frequency = 2.4
                    },
                    new WifiNetwork
                    {
                        Ssid = "Other_Network",
                        SignalStrength = -70,
                        Security = "WPA2",
                        Frequency = 2.4
                    }
                ];
            }

            _Logger.LogDebug("Found {Count} WiFi networks", Networks.Count);
            return Networks;
        }
        catch (Exception ex)
        {
            _Logger.LogError(ex, "Failed to scan for WiFi networks");
            return [];
        }
    }

    #endregion
}
