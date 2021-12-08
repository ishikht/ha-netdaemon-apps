using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using JsonEasyNavigation;

namespace TerneoIntegration.TerneoNet
{
    public class CloudService : ITerneoService
    {
        private const string ApiBaseUrl = "https://my.terneo.ua/api";
        private const string ApiV2BaseUrl = "https://my.terneo.ua/api-v2";
        private readonly CloudSettings _settings;
        private string? _accessToken;
        private IEnumerable<CloudDevice>? _cloudDevices;

        public CloudService(CloudSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public async Task<ITerneoTelemetry?> GetTelemetryAsync(string serialNumber)
        {
            var cloudDevice = _cloudDevices?.SingleOrDefault(d => d.SerialNumber == serialNumber);
            if (cloudDevice == null) return null;
            var result = await GetDeviceAsync(cloudDevice.Id);
            return result?.Telemetry;
        }

        public async Task<bool> SetTemperature(string serialNumber, int temperature)
        {
            var cloudDevice = _cloudDevices?.SingleOrDefault(d => d.SerialNumber == serialNumber);
            if (cloudDevice == null) return false;

            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", "Token " + _accessToken);

            var response = await httpClient.PutAsJsonAsync($"{ApiBaseUrl}/device/{cloudDevice.Id}/setpoint/",
                new {value = temperature});

            if (!response.IsSuccessStatusCode) return false;

            var jsonDocument = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

            var nav = jsonDocument.ToNavigation();
            if (!nav["value"].Exist) return false;

            var responseTemperature = nav["value"].GetInt32OrDefault();
            return responseTemperature == temperature;
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

            var response = await httpClient.PostAsJsonAsync($"{ApiBaseUrl}/login/",
                new {email = _settings.Email, password = _settings.Password});

            if (!response.IsSuccessStatusCode) return false;
            var jsonDocument = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());

            var nav = jsonDocument.ToNavigation();
            if (!nav["access_token"].Exist) return false;

            _accessToken = nav["access_token"].GetStringOrDefault();
            return !string.IsNullOrEmpty(_accessToken);
        }


        private async Task<IEnumerable<CloudDevice>?> GetDevicesAsync()
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", "Token " + _accessToken);

            var response = await httpClient.GetAsync($"{ApiBaseUrl}/device/");
            if (!response.IsSuccessStatusCode) return null;

            var jsonDocument = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            var nav = jsonDocument.ToNavigation();

            var devicesArrayItem = nav["results"];
            if (!devicesArrayItem.Exist || !devicesArrayItem.IsEnumerable || devicesArrayItem.Count == 0)
                return null;

            var devices = devicesArrayItem.Map<List<CloudDevice>>();

            return devices;
        }

        private async Task<CloudDevice?> GetDeviceAsync(int id)
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", "Token " + _accessToken);

            var response = await httpClient.GetAsync($"{ApiBaseUrl}/device/{id}/");
            if (!response.IsSuccessStatusCode) return null;

            var jsonDocument = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            return jsonDocument.Deserialize<CloudDevice>();
        }

        public async Task<bool> PowerOnOff(string serialNumber, bool isOff)
        {
            var cloudDevice = _cloudDevices?.SingleOrDefault(d => d.SerialNumber == serialNumber);
            if (cloudDevice == null) return false;

            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", "Token " + _accessToken);

            var response = await httpClient.PutAsJsonAsync($"{ApiV2BaseUrl}/device/{cloudDevice.Id}/basic-parameters/",
                new {power_off = isOff});


            if (!response.IsSuccessStatusCode) return false;

            var jsonDocument = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            var nav = jsonDocument.ToNavigation();

            var resultItem = nav["result"]["power_off"];
            if (!resultItem.Exist) return false;

            return resultItem.GetBooleanOrDefault() == isOff;
        }
    }
}