using System.Text.Json.Serialization;

namespace MultiNodeNetwork.Core.Models;
/// <summary>
/// Location information for nodes
/// </summary>
public class Location
{
    #region Properties

    /// <summary>
    /// The X coordinate of the node's location
    /// </summary>
    [JsonPropertyName("x")]
    public int X
    {
        get; set;
    }

    /// <summary>
    /// The Y coordinate of the node's location
    /// </summary>
    [JsonPropertyName("y")]
    public int Y
    {
        get; set;
    }

    /// <summary>
    /// The Z coordinate of the node's location (optional, default 0)
    /// </summary>
    [JsonPropertyName("z")]
    public int Z { get; set; } = 0;

    #endregion
}
