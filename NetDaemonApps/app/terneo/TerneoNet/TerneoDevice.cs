using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TerneoIntegration.TerneoNet
{
    public class TerneoDevice
    {
        private const string TelemetryCommand = "{\"cmd\":4}";
        private readonly string _apiUri;
        private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);

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

            await _semaphoreSlim.WaitAsync();
            try
            {
                var response = await httpClient.PostAsync(_apiUri, new StringContent(TelemetryCommand));
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<TerneoTelemetry>(result);
                }
            }
            finally
            {
                _semaphoreSlim.Release();
            }

            return null;
        }

        public async Task<bool> SetTemperature(int temperature)
        {
            var httpClient = new HttpClient();
            var command = new {sn = SerialNumber, par = new[] {new dynamic[] {5, 1, temperature.ToString()}}};
            var jsonCommand = JsonConvert.SerializeObject(command);

            await _semaphoreSlim.WaitAsync();
            try
            {
                var response = await httpClient.PostAsync(_apiUri, new StringContent(jsonCommand));
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                    if (result == null || !result.ContainsKey("success")) return false;
                    return result["success"] == "true";
                }
            }
            finally
            {
                _semaphoreSlim.Release();
            }


            return false;
        }

        public async Task<bool> PowerOn()
        {
            return await PowerOnOff(false);
        }
        
        public async Task<bool> PowerOff()
        {
            return await PowerOnOff(true);
        }

        private async Task<bool> PowerOnOff(bool isOff)
        {
            var httpClient = new HttpClient();
            var command = new {sn = SerialNumber, par = new[] {new dynamic[] {125, 7, (isOff ? 1 : 0).ToString()}}};
            var jsonCommand = JsonConvert.SerializeObject(command);

            await _semaphoreSlim.WaitAsync();
            try
            {
                var response = await httpClient.PostAsync(_apiUri, new StringContent(jsonCommand));
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                    if (result == null || !result.ContainsKey("success")) return false;
                    return result["success"] == "true";
                }
            }
            finally
            {
                _semaphoreSlim.Release();
            }


            return false;
        }
    }
}