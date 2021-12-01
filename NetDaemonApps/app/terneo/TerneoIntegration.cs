using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using NetDaemon.Common.Reactive;
using Newtonsoft.Json;
using TerneoIntegration.TerneoNet;

namespace TerneoIntegration
{
    public class TerneoIntegration : NetDaemonRxApp
    {
        private readonly Dictionary<string, TerneoDevice> _onlineDevices = new();
        private readonly TerneoScanner _scanner = new();
        public IEnumerable<TerneoDeviceConfig>? Devices { get; set; }

        public override void Initialize()
        {
            _scanner.OnNewDeviceInfoReceived += Scanner_OnDeviceInfoReceived;
            Log("TERNEO: Starting discovery");
            _scanner.Start();
            Log("TERNEO: Discovery started");

            EventChanges.Where(e => e.Event == "set_temperature" && e.Domain == "climate")
                .Subscribe(async e => await OnHaClimateTemperatureSet(e));
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
            await device.SetTemperature(temperature);
            
            Log($"TERNEO: Set temperature {temperature} on device {data["entity_id"]}");
        }

        private async void Scanner_OnDeviceInfoReceived(object? sender, TerneoDeviceScanInfo e)
        {
            if (string.IsNullOrEmpty(e.Ip) || string.IsNullOrEmpty(e.SerialNumber))
            {
                LogError("Failed to obtain IP or Serial number of device");
                return;
            }

            Log($"TERNEO: New Device Discovered {e}");

            var deviceConfig = Devices?.FirstOrDefault(d => d.ip == e.Ip && d.serialNumber == e.SerialNumber);
            if (deviceConfig == null) return;

            var device = new TerneoDevice(e.Ip, e.SerialNumber);
            var entityName = "climate." + deviceConfig.name;

            _onlineDevices.Add(entityName, device);

            await UpdateHaEntityState(entityName);

            async void RegularUpdate() => await UpdateHaEntityState(entityName);
            SetInterval(RegularUpdate, 10000);
        }

        private async Task UpdateHaEntityState(string entityName)
        {
            TerneoDevice device = _onlineDevices[entityName];
            var telemetry = await device.GetTelemetry();
            var action = telemetry.PowerOff ? "off" : telemetry.Heating ? "heating" : "idle";
            var state = action == "off" ? "off" : "heat";
            var temperature = Math.Round(telemetry.FloorTemperature, 1);
            
            SetState(entityName, state, new
            {
                hvac_modes = new[] {"heat", "off"},
                min_temp = 5,
                max_temp = 45,
                target_temp_step = 1,
                current_temperature = temperature,
                temperature = telemetry.TargetTemperature,
                supported_features = 1,
                hvac_action = action,
                hvac_mode = state
            });
            
            Log($"TERNEO: entity {entityName} set state {state} and read temperature {temperature}");
        }
        
        public static System.Timers.Timer SetInterval(Action action, int interval)
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