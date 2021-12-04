using System.Text.Json.Serialization;

namespace TerneoIntegration.TerneoNet
{
    public class CloudDevice
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }
        [JsonPropertyName("sn")]
        public string SerialNumber { get; set; }
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("data")]
        public CloudTelemetry Telemetry { get; set; }
    }
}