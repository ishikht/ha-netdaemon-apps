using System;
using System.Collections.Generic;
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
        private string _accessToken;

        public CloudService(CloudSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public async Task<bool> LoginAsync()
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


        public async Task<IEnumerable<CloudDevice>?> GetDevicesAsync()
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

        public async Task<CloudDevice?> GetDeviceAsync(int id)
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
    }
}