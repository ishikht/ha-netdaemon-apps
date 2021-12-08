using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using JsonEasyNavigation;
using Newtonsoft.Json;

namespace TerneoIntegration.TerneoNet
{
    public class CloudService
    {
        private const string ApiBaseUrl = "https://my.terneo.ua/api";
        private readonly CloudSettings _settings;
        private string? _accessToken;
        private IEnumerable<CloudDevice>? _cloudDevices;

        public CloudService(CloudSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public async Task InitializeAsync()
        {
            var isSucceed = await LoginAsync();
            if (isSucceed)
                _cloudDevices = await GetDevicesAsync();
        }

        private async Task<bool> LoginAsync()
        {
            var httpClient = new HttpClient();

            var request = new {email = _settings.Email, password = _settings.Password};
            var jsonRequest = JsonConvert.SerializeObject(request);

            var response = await httpClient.PostAsync($"{ApiBaseUrl}/login/",
                new StringContent(jsonRequest, Encoding.UTF8, "application/json"));

            if (response.IsSuccessStatusCode)
            {
                var jsonDocument = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

                var nav = jsonDocument.ToNavigation();
                if (!nav["access_token"].Exist) return false;

                _accessToken = nav["access_token"].GetStringOrDefault();
                return !string.IsNullOrEmpty(_accessToken);
            }

            return false;
        }


        private async Task<IEnumerable<CloudDevice>?> GetDevicesAsync()
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", "Token " + _accessToken);

            var response = await httpClient.GetAsync($"{ApiBaseUrl}/device/");
            if (response.IsSuccessStatusCode)
            {
                var jsonDocument = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
                var nav = jsonDocument.ToNavigation();

                var devicesArrayItem = nav["results"];
                if (!devicesArrayItem.Exist || !devicesArrayItem.IsEnumerable || devicesArrayItem.Count == 0)
                    return null;

                var devices = devicesArrayItem.Map<List<CloudDevice>>();

                return devices;
            }

            return null;
        }

        public async Task<ITerneoTelemetry?> GetTelemetryAsync(string serialNumber)
        {
            var cloudDevice = _cloudDevices?.SingleOrDefault(d => d.SerialNumber == serialNumber);
            if (cloudDevice == null) return null;
            var result = await GetDeviceAsync(cloudDevice.Id);
            return result?.Telemetry;
        }

        private async Task<CloudDevice?> GetDeviceAsync(int id)
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", "Token " + _accessToken);

            var response = await httpClient.GetAsync($"{ApiBaseUrl}/device/{id}/");
            if (response.IsSuccessStatusCode)
            {
                var jsonDocument = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
                return jsonDocument.Deserialize<CloudDevice>();
            }

            return null;
        }
        
        public async Task SetTemperature(string serialNumber, int temperature)
        {
            var cloudDevice = _cloudDevices?.SingleOrDefault(d => d.SerialNumber == serialNumber);
            if (cloudDevice == null) return;
            await SetTemperature(cloudDevice.Id, temperature);
        }

        private async Task<bool> SetTemperature(int id, int temperature)
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", "Token " + _accessToken);

            var request = new {value = temperature};
            var jsonRequest = JsonConvert.SerializeObject(request);

            var response = await httpClient.PutAsync($"{ApiBaseUrl}/device/{id}/setpoint/",
                new StringContent(jsonRequest, Encoding.UTF8, "application/json"));

            if (response.IsSuccessStatusCode)
            {
                var jsonDocument = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

                var nav = jsonDocument.ToNavigation();
                if (!nav["value"].Exist) return false;

                var responseTemperature = nav["value"].GetInt32OrDefault();
                return responseTemperature == temperature;
            }

            return false;
        }
    }
}