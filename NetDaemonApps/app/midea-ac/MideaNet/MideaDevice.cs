using System.Text.Json.Serialization;

namespace MideaAcIntegration.MideaNet;

public class MideaDevice
{
    [JsonPropertyName("id")] public string Id { get; set; }

    [JsonPropertyName("name")] public string Name { get; set; }

    [JsonPropertyName("userId")] public string UserId { get; set; }

    [JsonPropertyName("type")] public string DeviceType { get; set; }

    [JsonPropertyName("sn")] public string SerialNumber { get; set; }
}