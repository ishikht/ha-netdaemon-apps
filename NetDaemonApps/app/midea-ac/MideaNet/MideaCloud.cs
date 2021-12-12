using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using JsonEasyNavigation;

namespace MideaAcIntegration.MideaNet
{
    public class MideaCloud
    {
        private const string ApiBaseUrl = "https://mapp.appsmb.com/v1";
        private readonly string _email;
        private readonly string _password;

        private string _loginId;
        private string _sessionId;
        private string _accessToken;
        private string _dataKey;

        public MideaCloud(string email, string password)
        {
            _password = password;
            _email = email;
        }

        private async Task<JsonNavigationElement?> ApiRequestAsync(string endpoint, IDictionary<string, string> args)
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

            foreach (var (key, value) in args) form.Add(key, value);

            // Add the sessionId if there is a valid session
            if (!string.IsNullOrEmpty(_sessionId))
                form["sessionId"] = _sessionId;
            
            var sign = MideaUtils.GetSign(endpoint, form);
            form.Add("sign", sign);

            var request = new HttpRequestMessage(HttpMethod.Post, "https://mapp.appsmb.com/v1" + endpoint)
                {Content = new FormUrlEncodedContent(form)};
            var response = await httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return null;

            var jsonDocument = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
            var nav = jsonDocument.ToNavigation();

            if (!nav["errorCode"].Exist || nav["errorCode"].GetStringOrDefault() != "0")
                return null; //TODO: Throw error
            if (!nav["result"].Exist) return null;
            return nav["result"];
        }


        private async Task<string> GetLoginIdAsync()
        {
            var args = new Dictionary<string, string> {{"loginAccount", _email}};

            var result = await ApiRequestAsync("/user/login/id/get", args);
            if (!result.HasValue || !result.Value["loginId"].Exist) return string.Empty;

            return result.Value["loginId"].GetStringOrDefault();
        }
        
        public async Task LoginAsync()
        {
            if (string.IsNullOrEmpty(_loginId)) _loginId = await GetLoginIdAsync();
            if (!string.IsNullOrEmpty(_sessionId) && 
                !string.IsNullOrEmpty(_accessToken) && 
                !string.IsNullOrEmpty(_dataKey)) return; 
            
            var args = new Dictionary<string, string>
            {
                {"loginAccount", _email},
                {"password",MideaUtils.GetSignPassword(_loginId, _password)}
            };

            var session = await ApiRequestAsync("/user/login", args);
            if (!session.HasValue || 
                !session.Value["accessToken"].Exist || 
                !session.Value["sessionId"].Exist ) return;
            _accessToken = session.Value["accessToken"].GetStringOrDefault();
            _sessionId = session.Value["sessionId"].GetStringOrDefault();
            _dataKey = MideaUtils.GenerateDataKey(_accessToken);
        }
    }
}