namespace MultiNodeNetwork.Core.Services;
/// <summary>
/// Platform information
/// </summary>
public class PlatformInfo
{
    #region Properties

    /// <summary>
    /// The architecture of the platform (e.g., x64, ARM64)
    /// </summary>
    public string Architecture { get; set; } = string.Empty;

    /// <summary>
    /// The .NET framework version being used
    /// </summary>
    public string Framework { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether the platform is Linux-based
    /// </summary>
    public bool IsLinux
    {
        get; set;
    }

    /// <summary>
    /// Indicates whether the platform is a Raspberry Pi
    /// </summary>
    public bool IsRaspberryPi
    {
        get; set;
    }

    /// <summary>
    /// Indicates whether the platform is Windows-based
    /// </summary>
    public bool IsWindows
    {
        get; set;
    }

    /// <summary>
    /// The operating system name and version
    /// </summary>
    public string OperatingSystem { get; set; } = string.Empty;

    #endregion
}
