namespace MultiNodeNetwork.Core.Services;

/// <summary>
/// Interface for platform-specific services
/// </summary>
public interface IPlatformService
{
    PlatformInfo GetPlatformInfo();
    string GetConfigDirectory();
    string GetLogDirectory();
    string GetWiFiInterfaceName();
    bool IsWindows();
    bool IsLinux();
    bool IsRaspberryPi();
    void CreatePlatformDirectories();
    // Add other platform-specific methods as needed
}