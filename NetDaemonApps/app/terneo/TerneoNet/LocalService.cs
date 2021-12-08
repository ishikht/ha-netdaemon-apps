using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using JsonEasyNavigation;
using Newtonsoft.Json;

namespace TerneoIntegration.TerneoNet
{
    public class LocalService:ITerneoService
    {
        private const string ApiUri = "http://{0}/api.cgi";

        private readonly List<TerneoDevice> _onlineDevices = new();
        private readonly TerneoScanner _scanner = new();
        private readonly SemaphoreSlim _semaphoreSlim = new(1, 1);

        public event EventHandler<TerneoDevice>? OnDeviceDiscovered;

        public void Initialize()
        {
            _scanner.OnNewDeviceInfoReceived += Scanner_OnNewDeviceInfoReceived;
            _scanner.Start();
            
            var delayedThread = new Thread(() =>
            {
                Thread.Sleep(TimeSpan.FromMinutes(2));
                _scanner.Stop();
                _scanner.OnNewDeviceInfoReceived -= Scanner_OnNewDeviceInfoReceived;
            });

            delayedThread.Start();
        }

        public async Task<ITerneoTelemetry?> GetTelemetryAsync(string serialNumber)
        {
            var device = _onlineDevices.SingleOrDefault(d => d.SerialNumber == serialNumber);
            if (device == null) return null;

            var httpClient = new HttpClient();
            await _semaphoreSlim.WaitAsync();

            try
            {
                var response = await httpClient.PostAsJsonAsync(string.Format(ApiUri, device.Ip), new {cmd = 4});
                if (response.IsSuccessStatusCode) return null;

                var result = await response.Content.ReadAsStringAsync();
                if (string.IsNullOrEmpty(result)) throw new Exception("Telemetry request returned empty string");
                return JsonConvert.DeserializeObject<TerneoTelemetry>(result);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        public async Task<bool> SetTemperature(string serialNumber, int temperature)
        {
            var device = _onlineDevices.SingleOrDefault(d => d.SerialNumber == serialNumber);
            if (device == null) return false;

            var httpClient = new HttpClient();
            await _semaphoreSlim.WaitAsync();

            try
            {
                var response = await httpClient.PostAsJsonAsync(string.Format(ApiUri, device.Ip), new
                {
                    sn = device.SerialNumber,
                    par = new[] {new dynamic[] {5, 1, temperature.ToString()}}
                });
                if (response.IsSuccessStatusCode) return false;

                var jsonDocument = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
                var nav = jsonDocument.ToNavigation();

                var successItem = nav["success"];
                if (!successItem.Exist || successItem.IsNullValue) return false;

                return successItem.GetBooleanOrDefault();
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        public async Task<bool> PowerOn(string serialNumber)
        {
            return await PowerOnOff(serialNumber, false);
        }

        public async Task<bool> PowerOff(string serialNumber)
        {
            return await PowerOnOff(serialNumber, true);
        }

        public async Task<bool> PowerOnOff(string serialNumber, bool isOff)
        {
            var device = _onlineDevices.SingleOrDefault(d => d.SerialNumber == serialNumber);
            if (device == null) return false;

            var httpClient = new HttpClient();
            await _semaphoreSlim.WaitAsync();
            try
            {
                var response = await httpClient.PostAsJsonAsync(string.Format(ApiUri, device.Ip), new
                {
                    sn = device.SerialNumber,
                    par = new[] {new dynamic[] {125, 7, (isOff ? 1 : 0).ToString()}}
                });
                if (response.IsSuccessStatusCode) return false;

                var jsonDocument = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
                var nav = jsonDocument.ToNavigation();

                var successItem = nav["success"];
                if (!successItem.Exist || successItem.IsNullValue) return false;

                return successItem.GetBooleanOrDefault();
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        private void Scanner_OnNewDeviceInfoReceived(object? sender, TerneoDeviceScanInfo e)
        {
            if (string.IsNullOrEmpty(e.Ip) || string.IsNullOrEmpty(e.SerialNumber)) return;

            var device = new TerneoDevice(e.Ip, e.SerialNumber);
            _onlineDevices.Add(device);

            OnDeviceDiscovered?.Invoke(this, device);
        }
    }
}