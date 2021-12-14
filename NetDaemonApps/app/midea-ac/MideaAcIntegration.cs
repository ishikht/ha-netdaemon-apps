using System.Linq;
using System.Reactive.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using JsonEasyNavigation;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using MideaAcIntegration.MideaNet;
using NetDaemon.Common;
using NetDaemon.Common.Reactive;

namespace MideaAcIntegration
{
    public class MideaAcIntegration: NetDaemonRxApp
    {
        private Dictionary<string, MideaDeviceConfig>? _discoveredDevices;
        private MideaCloud _cloud;
        public string? Email { get; set; }
        public string? Password { get; set; }
        public IEnumerable<MideaDeviceConfig>? Devices { get; set; }
       
        public override async Task InitializeAsync()
        {
            _cloud = new MideaCloud(Email, Password);
            await _cloud.LoginAsync();
            
            var devices = await _cloud.GetUserDevicesList();
            if (devices == null) return;
            devices = devices.ToList();
            
            foreach (var mideaDevice in devices)
            {
                LogInformation($"MIDEA: New device discovered name: {mideaDevice.Name} id: {mideaDevice.Id} ");
            }

            if (Devices == null) return;
            _discoveredDevices = Devices.Where(dc => devices.Any(d => d.Id == dc.DeviceId))
                .ToDictionary(k => "climate." + k.Name, v =>v);

            foreach (var deviceRecord in _discoveredDevices)
            {
                RunEveryMinute(0, async() => await UpdateEntityState(deviceRecord.Key));
            }

            EventChanges.Where(e => e.Event == "set_temperature" && e.Domain == "climate")
                .Subscribe(async e => await OnHaClimateTemperatureSet(e));
            EventChanges.Where(e => e.Event == "set_hvac_mode" && e.Domain == "climate")
                .Subscribe(async e => await OnHaHvacModeSet(e));
        }

        private async Task OnHaHvacModeSet(RxEvent e)
        {
            if (_discoveredDevices == null) return;
            
            var jsonElement = (JsonElement)e.Data!;
            var nav = jsonElement.ToNavigation();
            
            var entityIdItem = nav["entity_id"];
            var hvacModeItem = nav["hvac_mode"];

            if (!entityIdItem.Exist || !hvacModeItem.Exist) return;

            var hvacMode = hvacModeItem.GetStringOrDefault();

            var entityId = entityIdItem.GetStringOrDefault();
            
            _discoveredDevices.TryGetValue(entityId, out var device);
            if (device == null ) return;
            
            var temperatureAttribute = State(entityId)?.Attribute?.temperature;
            if (temperatureAttribute == null) return;
            var setTemperature = temperatureAttribute is long ? Convert.ToInt32(temperatureAttribute) : null;
            if (setTemperature == null) return;
            
            int operationalMode = 0;
            bool isOn = true;
            switch (hvacMode)
            {
                case "auto":
                    operationalMode = 1;
                    break;
                case "cool":
                    operationalMode = 2;
                    break;
                case "dry":
                    operationalMode = 3;
                    break;
                case "heat":
                    operationalMode = 4;
                    break;
                case "fan_only":
                    operationalMode = 5;
                    break;
                case "off":
                    isOn = false;
                    break;
            }
            
            var command = new MideaSetCommand();
            command.PowerState = isOn;
            command.OperationalMode = operationalMode;
            command.TargetTemperature = setTemperature;
            var telemetry = await _cloud.SendCommand(device.DeviceId!,PacketBuilder.Build(command));

            if (telemetry == null) return; //Todo: reset to previous state, somehow
            UpdateEntityState(entityId, telemetry);
            LogInformation($"MIDEA: device command sent for entity: {entityId}, hvac: {hvacMode}");
        }

        private async Task OnHaClimateTemperatureSet(RxEvent e)
        {
            if (_discoveredDevices == null) return;
            
            var jsonElement = (JsonElement)e.Data!;
            var nav = jsonElement.ToNavigation();
            
            var entityItem = nav["entity_id"];
            var temperatureItem = nav["temperature"];

            if (!entityItem.Exist || !temperatureItem.Exist) return;

            var temperature = temperatureItem.GetInt32OrDefault();
            
            var entityId = entityItem.GetStringOrDefault();
            _discoveredDevices.TryGetValue(entityId, out var device);
            if (device == null ) return;

            var hvacMode = State(entityId)?.State as string ;
            if (hvacMode == null) return;
            
            int operationalMode = 0;
            bool isOn = true;
            switch (hvacMode)
            {
                case "auto":
                    operationalMode = 1;
                    break;
                case "cool":
                    operationalMode = 2;
                    break;
                case "dry":
                    operationalMode = 3;
                    break;
                case "heat":
                    operationalMode = 4;
                    break;
                case "fan_only":
                    operationalMode = 5;
                    break;
                case "off":
                    isOn = false;
                    break;
            }
            
            var command = new MideaSetCommand();
            command.PowerState = isOn;
            command.OperationalMode = operationalMode;
            command.TargetTemperature = temperature;
            var telemetry = await _cloud.SendCommand(device.DeviceId!,PacketBuilder.Build(command));

            if (telemetry == null) return; //Todo: reset to previous state, somehow
            UpdateEntityState(entityId, telemetry);
            LogInformation($"MIDEA: device command sent for entity: {entityId}, temperature: {temperature}");
        }

        private async Task UpdateEntityState(string entityId)
        {
            if (_discoveredDevices == null || !_discoveredDevices.ContainsKey(entityId)) return;
            var deviceConfig = _discoveredDevices[entityId];
            
            var telemetry = await _cloud.GetTelemetry(deviceConfig.DeviceId!);
            if(telemetry == null) return;
            UpdateEntityState(entityId, telemetry);
        }

        private void UpdateEntityState(string entityId, MideaTelemetry telemetry)
        {
            const int minTemperature = 17;
            const int maxTemperature = 30;

            string hvac = "";
            switch (telemetry.OperationalMode)
            {
                case 1:
                    hvac = "auto";
                    break;
                case 2:
                    hvac = "cool";
                    break;
                case 3:
                    hvac = "dry";
                    break;
                case 4:
                    hvac = "heat";
                    break;
                case 5:
                    hvac = "fan_only";
                    break;
            }

            if (telemetry.PowerState == false) hvac = "off";
            
            SetState(entityId, hvac, new
            {
                hvac_modes = new[] {"auto", "cool", "dry", "heat", "fan_only", "off"},
                min_temp = minTemperature,
                max_temp = maxTemperature,
                target_temp_step = 1,
                fan_modes= new[]{ "Auto", "Full", "High", "Medium", "Low", "Silent"},
                swing_modes= new[] {"Off", "Vertical"},
                current_temperature = telemetry.IndoorTemperature,
                temperature = telemetry.TargetTemperature,
                fan_mode= "Auto",
                swing_mode= "Off",
                supported_features = 1,
                //hvac_action = hvac
                hvac_mode = hvac
            });
            Log("MIDEA: Updated");
        }
    }
}