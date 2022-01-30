using System.Text.Json.Serialization;
using TerneoIntegration.TerneoNet.JsonConverters;

namespace TerneoIntegration.TerneoNet
{
    public class CloudTelemetry:ITerneoTelemetry
    {
        [JsonPropertyName("device_off")]
        public bool PowerOff{ get; set; }
        
        [JsonPropertyName("temp_current")]
        [JsonConverter(typeof(StringToDecimalConverter))]
        public decimal CurrentTemperature { get; set; }

        [JsonPropertyName("setpoint_state")]
        [JsonConverter(typeof(NullableBoolToBoolConverter))]
        public bool Heating { get; set; }

        [JsonPropertyName("temp_setpoint")]
        public int TargetTemperature { get; set; }
    }
}