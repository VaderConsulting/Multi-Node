using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

namespace MultiNodeNetwork.Core.Services;
/// <summary>
/// Service for platform-specific functionality and detection
/// </summary>
public class PlatformService : IPlatformService
{
#region Fields

    private readonly ILogger<PlatformService> _logger;
    private readonly PlatformInfo _platformInfo;

#endregion

#region Constructors

    public PlatformService(ILogger<PlatformService> logger)
    {
        _logger = logger;
        _platformInfo = DetectPlatform();
        _logger.LogInformation("Platform detected: {Platform} on {Architecture}",
            _platformInfo.OperatingSystem, _platformInfo.Architecture);
    }

#endregion

#region Private Methods

    private PlatformInfo DetectPlatform()
    {
        return new PlatformInfo
        {
            OperatingSystem = RuntimeInformation.OSDescription,
            Architecture = RuntimeInformation.ProcessArchitecture.ToString(),
            Framework = RuntimeInformation.FrameworkDescription,
            IsWindows = IsWindows(),
            IsLinux = IsLinux(),
            IsRaspberryPi = IsRaspberryPi()
        };
    }

#endregion

#region Public Methods

    /// <summary>
    /// Create platform-specific directories
    /// </summary>
    public void CreatePlatformDirectories()
    {
        try
        {
            string logDir = GetLogDirectory();
            string configDir = GetConfigDirectory();

            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
                _logger.LogInformation("Created log directory: {LogDir}", logDir);
            }

            if (!Directory.Exists(configDir))
            {
                Directory.CreateDirectory(configDir);
                _logger.LogInformation("Created config directory: {ConfigDir}", configDir);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to create platform directories");
        }
    }

    /// <summary>
    /// Get platform-specific configuration directory
    /// </summary>
    public string GetConfigDirectory()
    {
        if (IsWindows())
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "MultiNodeNetwork", "config");
        }
        else if (IsLinux())
        {
            return "/etc/multinodenetwork";
        }

        return "config";
    }

    /// <summary>
    /// Get platform-specific log directory
    /// </summary>
    public string GetLogDirectory()
    {
        if (IsWindows())
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MultiNodeNetwork", "logs");
        }
        else if (IsLinux())
        {
            return "/var/log/multinodenetwork";
        }

        return "logs";
    }

    /// <summary>
    /// Get platform information
    /// </summary>
    public PlatformInfo GetPlatformInfo()
    {
        return _platformInfo;
    }

    /// <summary>
    /// Get platform-specific WiFi interface name
    /// </summary>
    public string GetWiFiInterfaceName()
    {
        if (IsWindows())
        {
            return "Wi-Fi"; // Windows WiFi interface name
        }
        else if (IsLinux())
        {
            return "wlan0"; // Linux WiFi interface name
        }

        return "unknown";
    }

    /// <summary>
    /// Check if running on Linux
    /// </summary>
    public bool IsLinux()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    }

    /// <summary>
    /// Check if running on Raspberry Pi (Linux ARM64)
    /// </summary>
    public bool IsRaspberryPi()
    {
        return IsLinux() && RuntimeInformation.ProcessArchitecture == Architecture.Arm64;
    }

    /// <summary>
    /// Check if running on Windows
    /// </summary>
    public bool IsWindows()
    {
        return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    }

#endregion
}
