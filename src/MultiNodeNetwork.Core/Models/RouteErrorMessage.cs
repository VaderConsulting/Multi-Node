using System.Text.Json.Serialization;

using MultiNodeNetwork.Common.Models;

namespace MultiNodeNetwork.Core.Models;
/// <summary>
/// Message sent when a route error occurs
/// </summary>
public class RouteErrorMessage : NetworkMessage
{
    #region Properties

    /// <summary>
    /// Error code indicating the type of route error
    /// </summary>
    [JsonPropertyName("errorCode")]
    public RouteErrorCode ErrorCode
    {
        get; set;
    }

    /// <summary>
    /// Human-readable error message
    /// </summary>
    [JsonPropertyName("errorMessage")]
    public string ErrorMessage { get; set; } = string.Empty;

    /// <summary>
    /// Node that encountered the error (for debugging)
    /// </summary>
    [JsonPropertyName("errorNodeId")]
    public string ErrorNodeId { get; set; } = string.Empty;

    /// <summary>
    /// Node that encountered the error in byte format
    /// </summary>
    [JsonPropertyName("errorNodeIdByte")]
    public byte ErrorNodeIdByte
    {
        get; set;
    }

    /// <summary>
    /// Original destination that couldn't be reached
    /// </summary>
    [JsonPropertyName("originalDestinationId")]
    public string OriginalDestinationId { get; set; } = string.Empty;

    /// <summary>
    /// Original destination node ID in byte format
    /// </summary>
    [JsonPropertyName("originalDestinationNodeId")]
    public byte OriginalDestinationNodeId
    {
        get; set;
    }

    #endregion

    #region Constructors

    public RouteErrorMessage()
    {
        MessageType = GetMessageType();
    }

    #endregion

    #region Public Methods

    public override string GetMessageType()
    {
        return "ROUTE_ERROR";
    }

    #endregion
}

/// <summary>
/// Error codes for route errors
/// </summary>
public enum RouteErrorCode
{
    /// <summary>
    /// No route found to destination
    /// </summary>
    NoRouteToDestination,

    /// <summary>
    /// Destination node is unreachable
    /// </summary>
    DestinationUnreachable,

    /// <summary>
    /// Route timeout occurred
    /// </summary>
    RouteTimeout,

    /// <summary>
    /// Route loop detected
    /// </summary>
    RouteLoop,

    /// <summary>
    /// Insufficient bandwidth for route
    /// </summary>
    InsufficientBandwidth,

    /// <summary>
    /// Unknown route error
    /// </summary>
    Unknown
}
