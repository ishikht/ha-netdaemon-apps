﻿using System.Text.Json;
using JsonEasyNavigation;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Wrap;

namespace MideaAcIntegration.MideaNet;

public class MideaCloud
{
    private const string ApiBaseUrl = "https://mapp.appsmb.com/v1";

    private readonly string _email;
    private readonly ILogger _logger;
    private readonly string _password;
    private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);

    private string _accessToken;
    private string _dataKey;
    private string _loginId;
    private string _sessionId;

    public MideaCloud(ILogger logger, string email, string password)
    {
        _logger = logger;
        _password = password;
        _email = email;
    }

    private async Task<JsonNavigationElement?> ApiRequestAsync(string endpoint, IDictionary<string, string>? args)
    {
        var form = new Dictionary<string, string>
        {
            {"clientType", MideaConstants.ClientType},
            {"src", MideaConstants.RequestSource},
            {"appId", MideaConstants.AppId},
            {"format", MideaConstants.RequestFormat},
            {"stamp", MideaUtils.GetStamp()},
            {"language", MideaConstants.Language}
        };

        if (args != null)
            foreach (var (key, value) in args)
                form.Add(key, value);

        // Add the sessionId if there is a valid session
        if (!string.IsNullOrEmpty(_sessionId))
            form["sessionId"] = _sessionId;

        var sign = MideaUtils.GetSign(endpoint, form);
        form.Add("sign", sign);

        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", MideaConstants.UserAgent);
        var request = new HttpRequestMessage(HttpMethod.Post, ApiBaseUrl + endpoint)
            {Content = new FormUrlEncodedContent(form)};
        var response = await httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode) throw new HttpRequestException("The request was not successful");

        var jsonDocument = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var nav = jsonDocument.ToNavigation();

        if (!nav["errorCode"].Exist || nav["errorCode"].GetStringOrDefault() != "0")
            throw new ApiException(nav["errorCode"].GetStringOrDefault(), nav["msg"].GetStringOrDefault());

        _logger.LogDebug($"Successfully executed {endpoint}");
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
            {"password", MideaUtils.GetSignPassword(_loginId, _password)}
        };

        var session = await ApiRequestAsync("/user/login", args);
        if (!session.HasValue ||
            !session.Value["accessToken"].Exist ||
            !session.Value["sessionId"].Exist) return;
        _accessToken = session.Value["accessToken"].GetStringOrDefault();
        _sessionId = session.Value["sessionId"].GetStringOrDefault();
        _dataKey = MideaUtils.GenerateDataKey(_accessToken);
    }

    public async Task<IEnumerable<MideaDevice>?> GetUserDevicesList()
    {
        var result = await ApiRequestAsync("/appliance/user/list/get", null);
        if (!result.HasValue) return null;
        var listItem = result.Value["list"];
        if (!listItem.Exist || !listItem.IsEnumerable) return null;

        var devices = listItem.Map<List<MideaDevice>>();
        return devices.Where(d => d.DeviceType == MideaConstants.DeviceTypeAc);
    }

    public async Task<MideaTelemetry?> SetOperationalModeAsync(string deviceId,
        OperationalMode operationalMode,
        int targetTemperature)
    {
        await _semaphoreSlim.WaitAsync();
        try
        {
            var command = new MideaSetCommand
            {
                PowerState = operationalMode != OperationalMode.Off,
                OperationalMode = operationalMode != OperationalMode.Off ? (int) operationalMode : 0,
                TargetTemperature = targetTemperature
            };

            var apiRequestPolicy = BuildApiRequestPolicy("setOpMode");
            return await apiRequestPolicy.ExecuteAsync(async () =>
                await SendCommandAsync(deviceId, command.BuildPacket()));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "MIDEA: Failed to get telemetry");
        }
        finally
        {
            _semaphoreSlim.Release();
        }

        return null;
    }

    public async Task<MideaTelemetry?> GetTelemetryAsync(string deviceId)
    {
        await _semaphoreSlim.WaitAsync();
        try
        {
            // STATUS ONLY OR POWER ON/OFF HEADER
            int[] acDataHeader =
            {
                90, 90, 1, 16, 89, 0, 32, 0, 80, 0, 0, 0, 169, 65, 48, 9, 14, 5, 20, 20, 213, 50, 1, 0, 0, 17, 0, 0, 0,
                4,
                2, 0, 0, 1, 0, 0, 0, 0, 0, 0
            };

            var data = acDataHeader.Concat(MideaConstants.GetTelemetryCommand).ToArray();

            var apiRequestPolicy = BuildApiRequestPolicy("telemetry");
            return await apiRequestPolicy.ExecuteAsync(async () => await SendCommandAsync(deviceId, data));
        }
        catch (Exception e)
        {
            _logger.LogError(e, "MIDEA: Failed to get telemetry");
        }
        finally
        {
            _semaphoreSlim.Release();
        }

        return null;
    }


    public async Task<MideaTelemetry?> SendCommandAsync(string id, int[] order)
    {
        var orderEncode = MideaUtils.Encode(order);
        var orderEncrypt = MideaUtils.EncryptAes(orderEncode, _dataKey);

        var args = new Dictionary<string, string>
        {
            {"order", orderEncrypt},
            {"funId", "0000"},
            {"applianceId", id}
        };

        var result = await ApiRequestAsync("/appliance/transparent/send", args);
        if (!result.HasValue) return null;

        var replyItem = result.Value["reply"];
        if (!replyItem.Exist) return null;

        var replyStr = replyItem.GetStringOrDefault();
        var decryptedReply = MideaUtils.DecryptAes(replyStr, _dataKey);
        if (decryptedReply == null) return null;
        var decodedReply = MideaUtils.Decode(decryptedReply);

        return new MideaTelemetry(decodedReply);
    }

    private AsyncPolicyWrap BuildApiRequestPolicy(string endpoint)
    {
        var httpRequestErrorPolicy = Policy.Handle<HttpRequestException>().RetryAsync();
        //The async reply does not exist.
        var ignorePolicy = Policy.Handle<ApiException>(e => e.ErrorCode == "3176").RetryAsync();
        var restartSessionPolicy = Policy
            .Handle<ApiException>(e => e.ErrorCode == "3106") //Invalid Session
            .Or<ApiException>(e => e.ErrorCode == "3004") //value is illegal
            .Or<ApiException>(e => e.ErrorCode == "9999") //system error
            .Or<ApiException>(e => e.ErrorCode == "3144")
            .RetryAsync(async (e, c) =>
            {
                _logger.LogError($"Failed to execute {endpoint}, retrying (session reset)...");
                _loginId = string.Empty;
                _sessionId = string.Empty;
                await LoginAsync();
            });

        var apiRequestPolicy = Policy.WrapAsync(httpRequestErrorPolicy,
            ignorePolicy,
            restartSessionPolicy);
        return apiRequestPolicy;
    }
}