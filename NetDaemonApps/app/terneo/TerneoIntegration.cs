using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;
using JsonEasyNavigation;
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
            _localService.Initialize();
            
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
            if (e.Data == null ) return;
            var jsonElement = (JsonElement)e.Data;
            var nav = jsonElement.ToNavigation();
            
            var entityItem = nav["entity_id"];
            var temperatureItem = nav["temperature"];

            if (!entityItem.Exist || !temperatureItem.Exist) return;

            var temperature = temperatureItem.GetInt32OrDefault();
            
            var entity = entityItem.GetStringOrDefault();
            _onlineDevices.TryGetValue(entity, out var device);
            if (device == null ) return;

            var isSucceed = await SetTemperature(device.SerialNumber, temperature);
            if (!isSucceed)
            {
                LogError($"TERNEO: Failed to set temperature {temperature} on device {entity}");
                return;
            }

            await UpdateHaEntityState(entity);
            LogInformation($"TERNEO: Set temperature {temperature} on device {entity}");
        }

        private async void LocalServiceOnOnDeviceDiscovered(object? sender, TerneoDevice e)
        {
            LogInformation($"TERNEO: New Device Discovered {e}");

            var deviceConfig = Devices?.FirstOrDefault(d => d.ip == e.Ip && d.serialNumber == e.SerialNumber);
            if (deviceConfig == null) return;

            var device = new TerneoDevice(e.Ip, e.SerialNumber);
            var entityName = "climate." + deviceConfig.name;

            _onlineDevices.Add(entityName, device);

            await UpdateHaEntityState(entityName);
            
            SetInterval(async() => await UpdateHaEntityState(entityName), 60000);
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

            LogDebug($"TERNEO: entity {entityName} set state {state} and read temperature {temperature}");
        }

        private async Task<bool> SetTemperature(string serialNumber, int temperature, ITerneoService? service = null)
        {
            return service switch
            {
                null when _cloudService == null => await SetTemperature(serialNumber, temperature, _localService),
                null => await SetTemperature(serialNumber, temperature, _localService) ||
                        await SetTemperature(serialNumber, temperature, _cloudService),
                _ => await service.SetTemperature(serialNumber, temperature)
            };
        }
        private async Task<ITerneoTelemetry?> GetTelemetry(string entityName, ITerneoService? service = null)
        {
            switch (service)
            {
                case null when _cloudService == null:
                    return await GetTelemetry(entityName, _localService);
                case null:
                    return await GetTelemetry(entityName, _localService) ?? 
                           await GetTelemetry(entityName, _cloudService);
                default:
                    try
                    {
                        TerneoDevice device = _onlineDevices[entityName];
                        return await service.GetTelemetryAsync(device.SerialNumber);
                    }
                    catch (Exception e)
                    {
                        LogDebug(e, $"TERNEO: Failed to obtain telemetry for device {entityName}");
                        return null;
                    }
            }
        }

        private static System.Timers.Timer SetInterval(Func<Task> action, int interval)
        {
            async void OnTmrOnElapsed(object? sender, ElapsedEventArgs args) => await action();
            
            System.Timers.Timer timer = new();
            timer.Elapsed += OnTmrOnElapsed;
            timer.AutoReset = true;
            timer.Interval = interval;
            timer.Start();

            return timer;
        }
    }
}