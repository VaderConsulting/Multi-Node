namespace MultiNodeNetwork.Core.Services;

/// <summary>
/// Event arguments for signal strength change events
/// </summary>
public class SignalStrengthChangedEventArgs : EventArgs
{
    #region Properties

    /// <summary>
    /// The new signal strength value
    /// </summary>
    public int NewStrength
    {
        get;
    }

    /// <summary>
    /// The previous signal strength value
    /// </summary>
    public int OldStrength
    {
        get;
    }

    #endregion

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the SignalStrengthChangedEventArgs class
    /// </summary>
    /// <param name="oldStrength">The previous signal strength value</param>
    /// <param name="newStrength">The new signal strength value</param>
    public SignalStrengthChangedEventArgs(int oldStrength, int newStrength)
    {
        OldStrength = oldStrength;
        NewStrength = newStrength;
    }

    #endregion
}
