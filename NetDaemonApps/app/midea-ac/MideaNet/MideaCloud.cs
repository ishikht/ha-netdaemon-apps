using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using JsonEasyNavigation;
using Newtonsoft.Json;

namespace MideaAcIntegration.MideaNet
{
    public class MideaCloud
    {
        private readonly string _email;
        private const string ApiBaseUrl = "https://mapp.appsmb.com/v1";

        public MideaCloud(string email)
        {
            _email = email;
        }

        private async Task<JsonNavigationElement?> ApiRequest(string endpoint, IDictionary<string, string> args)
        {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("User-Agent", MideaConstants.UserAgent);
            var form = new Dictionary<string, string>
            {
                {"clientType", MideaConstants.ClientType},
                {"src", MideaConstants.RequestSource},
                {"appId", MideaConstants.AppId},
                {"format", MideaConstants.RequestFormat},
                {"stamp", MideaUtils.GetStamp()},
                {"language", MideaConstants.Language}
            };

            foreach (var (key, value) in args)
            {
                form.Add(key, value);
            }

            var sign = MideaUtils.GetSign(endpoint, form);
            form.Add("sign", sign);

            var request = new HttpRequestMessage(HttpMethod.Post, "https://mapp.appsmb.com/v1" + endpoint) { Content = new FormUrlEncodedContent(form) };
            var response = await httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return null;
            
            var jsonDocument = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            var nav = jsonDocument.ToNavigation();

            if (!nav["errorCode"].Exist || nav["errorCode"].GetStringOrDefault() != "0") return null; //TODO: Throw error
            if (!nav["result"].Exist) return null;
            return nav["result"];
        }


        public async Task<string> GetLoginId()
        {
            var args = new Dictionary<string, string> {{"loginAccount", _email}};

            var result = await ApiRequest("/user/login/id/get", args);
            if (!result.HasValue || !result.Value["loginId"].Exist) return string.Empty;
            
            return result.Value["loginId"].GetStringOrDefault();
        }
    }
}