using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TerneoIntegration.TerneoNet
{
    public class TerneoDevice
    {
        private const string TelemetryCommand = "{\"cmd\":4}";
        private const string SetTemperatureCommandTemplate = "{\"sn\":\"{0}\", \"par\":[[5,1,\"{1}\"]]}";
        private readonly string _apiUri;

        public TerneoDevice(string ip, string serialNumber)
        {
            SerialNumber = serialNumber;
            Ip = ip;
            _apiUri = $"http://{Ip}/api.cgi";
        }

        public string Ip { get; }
        public string SerialNumber { get; }

        public async Task<TerneoTelemetry?> GetTelemetry()
        {
            var httpClient = new HttpClient();

            var response = await httpClient.PostAsync(_apiUri, new StringContent(TelemetryCommand));
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<TerneoTelemetry>(result);
            }

            return null;
        }

        public async Task SetTemperature(int temperature)
        {
            var httpClient = new HttpClient();
            var command = new {sn = SerialNumber, par = new[] {new dynamic[] {5, 1, temperature.ToString()}}};
            var jsonCommand = JsonConvert.SerializeObject(command);

            var response = await httpClient.PostAsync(_apiUri, new StringContent(jsonCommand));
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                //return JsonConvert.DeserializeObject<TerneoTelemetry>(json);
            }

            //return null;
        }
    }
}