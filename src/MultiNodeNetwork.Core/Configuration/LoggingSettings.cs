using System.Text.Json.Serialization;

namespace MultiNodeNetwork.Core.Configuration;
public class LoggingSettings
{
    #region Properties

    [JsonPropertyName("backupCount")]
    public int BackupCount { get; set; } = 5;

    [JsonPropertyName("file")]
    public string File { get; set; } = "multinode.log";

    [JsonPropertyName("format")]
    public string Format { get; set; } = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}";

    [JsonPropertyName("level")]
    public string Level { get; set; } = "Information";

    [JsonPropertyName("maxSize")]
    public long MaxSize { get; set; } = 10 * 1024 * 1024; // 10MB

    #endregion
}
