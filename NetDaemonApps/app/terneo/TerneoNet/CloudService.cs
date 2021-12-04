using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TerneoIntegration.TerneoNet
{
    public class CloudService
    {
        private const string ApiBaseUrl = " https://my.hmarex.com/api";
        private readonly CloudSettings _settings;

        public CloudService(CloudSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public async Task<bool> LoginAsync()
        {
            var httpClient = new HttpClient();
            
            var request = new {email = _settings.Email, password = _settings.Password};
            var jsonRequest = JsonConvert.SerializeObject(request);
            
            
            var response = await httpClient.PostAsync($"{ApiBaseUrl}/login", new StringContent(jsonRequest));
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);
                if (result == null || !result.ContainsKey("access_token")) return false;
                var accessToken = result["access_token"];
                return !string.IsNullOrEmpty(accessToken);
            }

            return false;
        }
    }
}