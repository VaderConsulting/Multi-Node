using System.Text.Json.Serialization;

namespace MultiNodeNetwork.Core.Configuration;
public class PerformanceSettings
{
    #region Properties

    [JsonPropertyName("bufferSize")]
    public int BufferSize { get; set; } = 8192;

    [JsonPropertyName("connectionTimeout")]
    public int ConnectionTimeout { get; set; } = 30; // seconds

    [JsonPropertyName("keepAliveInterval")]
    public int KeepAliveInterval { get; set; } = 60; // seconds

    [JsonPropertyName("maxConcurrentConnections")]
    public int MaxConcurrentConnections { get; set; } = 20;

    #endregion
}
