using System.Text.Json.Serialization;

namespace MultiNodeNetwork.Core.Configuration;
public class SecuritySettings
{
    #region Properties

    [JsonPropertyName("authenticationRequired")]
    public bool AuthenticationRequired { get; set; } = true;

    [JsonPropertyName("encryptionEnabled")]
    public bool EncryptionEnabled { get; set; } = true;

    [JsonPropertyName("keyRotationInterval")]
    public int KeyRotationInterval { get; set; } = 3600; // 1 hour

    [JsonPropertyName("messageSignature")]
    public bool MessageSignature { get; set; } = true;

    #endregion
}
