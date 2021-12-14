using System.Linq;
using System.Threading.Tasks;
using MideaAcIntegration.MideaNet;
using NetDaemon.Common;
using NetDaemon.Common.Reactive;

namespace MideaAcIntegration
{
    public class MideaAcIntegration: NetDaemonRxApp
    {
        private IEnumerable<MideaDeviceConfig> _discoveredDevices;
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
                LogInformation($"MIDEA: New device discovered id: {mideaDevice.Id} sn: {mideaDevice.SerialNumber}");
            }

            if (Devices == null) return;
            _discoveredDevices = Devices.Where(dc => devices.Any(d => d.Id == dc.DeviceId));

            foreach (var mideaDeviceConfig in _discoveredDevices)
            {
                RunEveryMinute(0, async() => await UpdateEntityState(mideaDeviceConfig));
            }
            
            
            //om =4 Heat
            //om =5 fan
            //om =2 cold
            //om =3 dry
            //om =1 auto
        }

        private async Task UpdateEntityState(MideaDeviceConfig deviceConfig)
        {
            var telemetry = await _cloud.GetTelemetry(deviceConfig.DeviceId);
            SetState("climate." + deviceConfig.Name, "off", new
            {
                hvac_modes = new[] {"auto", "cool", "dry", "heat", "fan_only", "off"},
                min_temp = 17,
                max_temp = 30,
                target_temp_step = 1,
                fan_modes= new[]{ "Auto", "Full", "High", "Medium", "Low", "Silent"},
                swing_modes= new[] {"Off", "Vertical"},
                current_temperature = telemetry.IndoorTemperature,
                temperature = telemetry.TargetTemperature,
                fan_mode= "Auto",
                swing_mode= "Off",
                supported_features = 1,
                hvac_action = "off",
                hvac_mode = "off"
            });
        }
    }
}