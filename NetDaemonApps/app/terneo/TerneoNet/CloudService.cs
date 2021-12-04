using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
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
            
            httpClient.DefaultRequestHeaders
                .Accept
                .Add(new MediaTypeWithQualityHeaderValue("application/json"));
            var response = await httpClient.PostAsync($"{ApiBaseUrl}/login/", 
                new StringContent(jsonRequest,
                    Encoding.UTF8, 
                    "application/json"));
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                if (result == null || !result.ContainsKey("access_token")) return false;
                _accessToken = result["access_token"];
                return !string.IsNullOrEmpty(_accessToken);
            }

            return false;
        }
        
        
        public async Task<bool> GetDevicesAsync()
        {
            var httpClient = new HttpClient();
            
            var request = new {email = _settings.Email, password = _settings.Password};
            var jsonRequest = JsonConvert.SerializeObject(request);
            
            httpClient.DefaultRequestHeaders.Add("Authorization", "Token " + _accessToken);
            var response = await httpClient.GetAsync($"{ApiBaseUrl}/device/");
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                
                return !string.IsNullOrEmpty(_accessToken);
            }

            return false;
        }
    }
}