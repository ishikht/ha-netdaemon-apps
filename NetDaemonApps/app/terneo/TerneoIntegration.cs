using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Timers;
using NetDaemon.Common.Reactive;
using Newtonsoft.Json;
using TerneoIntegration.TerneoNet;

namespace TerneoIntegration
{
    public class TerneoIntegration : NetDaemonRxApp
    {
        private readonly Dictionary<string, TerneoDevice> _onlineDevices = new(); 
        
        private CloudService? _cloudService;
        private readonly LocalService _localService = new LocalService();
        public IEnumerable<TerneoDeviceConfig>? Devices { get; set; }
        public CloudSettings? Cloud { get; set; }

        public override void Initialize()
        {
            //Init cloud Service
            if (Cloud == null) return;
            _cloudService = new CloudService(Cloud);
            _cloudService.InitializeAsync().GetAwaiter().GetResult();

            //Init local Service
            _localService.OnDeviceDiscovered += LocalServiceOnOnDeviceDiscovered;
            _localService.Initialize().GetAwaiter().GetResult();
            
            EventChanges.Where(e => e.Event == "set_temperature" && e.Domain == "climate")
                .Subscribe(async e => await OnHaClimateTemperatureSet(e));
            EventChanges.Where(e => e.Event == "set_hvac_mode" && e.Domain == "climate")
                .Subscribe(async e => await OnHaHvacModeSet(e));
        }

        private async Task OnHaHvacModeSet(RxEvent e)
        {
            var json = e.Data?.ToString();
            Dictionary<string, string> data = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

            if (data == null ||
                !data.ContainsKey("entity_id") ||
                !_onlineDevices.ContainsKey(data["entity_id"]))
                return;

            data.TryGetValue("hvac_mode", out var hvacMode);
            if (string.IsNullOrEmpty(hvacMode)) return;

            var device = _onlineDevices[data["entity_id"]];
            if (hvacMode == "off")
            {
                var isSucceed = await _localService.PowerOff(device.SerialNumber);
                if (!isSucceed)
                {
                    LogError($"TERNEO: Failed to power off the device {data["entity_id"]}");
                    return;
                }
            }

            if (hvacMode == "heat")
            {
                var isSucceed = await _localService.PowerOn(device.SerialNumber);
                if (!isSucceed)
                {
                    LogError($"TERNEO: Failed to power on the device {data["entity_id"]}");
                    return;
                }
            }

            await UpdateHaEntityState(data["entity_id"]);
            Log($"TERNEO: Set hvac mode to {hvacMode} on device {data["entity_id"]}");
        }

        private async Task OnHaClimateTemperatureSet(RxEvent e)
        {
            var json = e.Data?.ToString();
            Dictionary<string, string> data = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

            if (data == null ||
                !data.ContainsKey("entity_id") ||
                !_onlineDevices.ContainsKey(data["entity_id"]))
                return;

            data.TryGetValue("temperature", out var temperatureStr);
            if (string.IsNullOrEmpty(temperatureStr)) return;
            int.TryParse(temperatureStr, out var temperature);

            var device = _onlineDevices[data["entity_id"]];
            var isSucceed = await _localService.SetTemperature(device.SerialNumber, temperature);
            if (!isSucceed)
            {
                LogWarning($"TERNEO: Failed to set temperature {temperature} on device {data["entity_id"]}");
                if (_cloudService == null) return;
                await _cloudService.SetTemperature(device.SerialNumber, temperature);
                return;
            }

            await UpdateHaEntityState(data["entity_id"]);
            LogInformation($"TERNEO: Set temperature {temperature} on device {data["entity_id"]}");
        }

        private async void LocalServiceOnOnDeviceDiscovered(object? sender, TerneoDevice e)
        {
            Log($"TERNEO: New Device Discovered {e}");

            var deviceConfig = Devices?.FirstOrDefault(d => d.ip == e.Ip && d.serialNumber == e.SerialNumber);
            if (deviceConfig == null) return;

            var device = new TerneoDevice(e.Ip, e.SerialNumber);
            var entityName = "climate." + deviceConfig.name;

            _onlineDevices.Add(entityName, device);

            await UpdateHaEntityState(entityName);

            async void RegularUpdate()
            {
                await UpdateHaEntityState(entityName);
            }
            SetInterval(RegularUpdate, 60000);
        }

        private async Task UpdateHaEntityState(string entityName)
        {
            ITerneoTelemetry? telemetry = await GetTelemetry(entityName);

            if (telemetry == null) return;
            
            var action = telemetry.PowerOff ? "off" : telemetry.Heating ? "heating" : "idle";
            var state = action == "off" ? "off" : "heat";

            const int minTemperature = 5;
            const int maxTemperature = 45;
            var temperature = Math.Round(telemetry.CurrentTemperature, 1);
            if (temperature is < minTemperature or > maxTemperature)
            {
                LogError($"TERNEO: Wrong temperature, value {temperature} out of min/max temperature");
                return;
            }

            SetState(entityName, state, new
            {
                hvac_modes = new[] {"heat", "off"},
                min_temp = minTemperature,
                max_temp = maxTemperature,
                target_temp_step = 1,
                current_temperature = temperature,
                temperature = telemetry.TargetTemperature,
                supported_features = 1,
                hvac_action = action,
                hvac_mode = state
            });

            Log($"TERNEO: entity {entityName} set state {state} and read temperature {temperature}");
        }

        private async Task<ITerneoTelemetry?> GetTelemetry(string entityName)
        {
            TerneoDevice device = _onlineDevices[entityName];

            try
            {
                return await _localService.GetTelemetry(device.SerialNumber);
            }
            catch (Exception e)
            {
                LogWarning(e, $"TERNEO: Failed to obtain telemetry for {entityName}");
            }

            if (_cloudService == null) return null;
            
            try
            {
                return await _cloudService.GetTelemetryAsync(device.SerialNumber);
            }
            catch (Exception e)
            {
                LogError(e, $"TERNEO: Failed to obtain cloud telemetry for {entityName}");
            }

            return null;
        }

        private static System.Timers.Timer SetInterval(Action action, int interval)
        {
            System.Timers.Timer tmr = new();
            tmr.Elapsed += (sender, args) => action();
            tmr.AutoReset = true;
            tmr.Interval = interval;
            tmr.Start();

            return tmr;
        }
    }
}