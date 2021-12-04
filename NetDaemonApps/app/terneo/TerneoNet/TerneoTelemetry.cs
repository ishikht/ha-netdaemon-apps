using Newtonsoft.Json;
using TerneoIntegration.TerneoNet.JsonConverters;

namespace TerneoIntegration.TerneoNet
{
    public class TerneoTelemetry : ITerneoTelemetry
    {
        [JsonProperty("t.1")]
        [JsonConverter(typeof(RawTemperatureToDecimalConverter))]
        public decimal CurrentTemperature { get; set; }

        [JsonProperty("t.5")]
        [JsonConverter(typeof(RawTemperatureToIntConverter))]
        public int TargetTemperature { get; set; }

        [JsonProperty("f.0")]
        [JsonConverter(typeof(IntToBoolConverter))]
        public bool Heating { get; set; }
        
        [JsonProperty("f.16")]
        [JsonConverter(typeof(IntToBoolConverter))]
        public bool PowerOff { get; set; }
    }
}