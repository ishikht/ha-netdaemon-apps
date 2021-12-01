using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TerneoIntegration.TerneoNet
{
    public class TerneoDevice
    {
        private const string TelemetryCommand = "{\"cmd\":4}";

        public TerneoDevice(string ip, string serialNumber)
        {
            SerialNumber = serialNumber;
            Ip = ip;
        }

        public string Ip { get; }
        public string SerialNumber { get; }

        public async Task<TerneoTelemetry?> GetTelemetry()
        {
            var httpClient = new HttpClient();
            var uri = $"http://{Ip}/api.cgi";

            var response = await httpClient.PostAsync(uri, new StringContent(TelemetryCommand));
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<TerneoTelemetry>(result);
                //Console.WriteLine($"TERNEO: device result: {result}");
            }

            return null;
        }
    }
}