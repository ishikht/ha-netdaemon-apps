using Newtonsoft.Json;
using TerneoIntegration.TerneoNet.JsonConverters;

namespace TerneoIntegration.TerneoNet
{
    public class TerneoTelemetry
    {
        [JsonProperty("sn")]
        public string SerialNumber { get; set; }
        
        [JsonProperty("t.1")]
        [JsonConverter(typeof(RawTemperatureConverter))]
        public double FloorTemperature { get; set; }
        //public double FloorTemperatureC => FloorTemperature / 16.0;
        
        [JsonProperty("t.5")]
        [JsonConverter(typeof(RawTemperatureConverter))]
        public double TargetTemperature { get; set; }
        //public double SetTemperatureC => SetTemperature / 16.0;

        [JsonProperty("f.0")]
        [JsonConverter(typeof(IntToBoolConverter))]
        public bool Heating { get; set; }
        
        [JsonProperty("f.16")]
        [JsonConverter(typeof(IntToBoolConverter))]
        public bool PowerOff { get; set; }
    }
}