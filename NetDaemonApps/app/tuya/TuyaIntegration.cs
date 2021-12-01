using com.clusterrr.TuyaNet;
using NetDaemon.Common.Reactive;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Net;
using System;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace TuyaIntegrationApp
{
    public class TuyaIntegration : NetDaemonRxApp
    {
        private readonly TuyaScanner _scanner = new();
        public IEnumerable<TuyaDeviceConfig>? Devices { get; set; }

        public override void Initialize()
        {
            try
            {
                //https://github.com/ClusterM/tuyanet
                _scanner.OnDeviceInfoReceived += Scanner_OnDeviceInfoReceived;
                Log("TUYA: Starting discovery");
                _scanner.Start();
                Log("TUYA: Discovery started");
            }
            catch (Exception e)
            {
                LogError(e, "TUYA: Failed to start discovery");
            }
        }

        async void Scanner_OnDeviceInfoReceived(object sender, TuyaDeviceScanInfo e)
        {
            try
            {
                Log($"TUYA: Device discovered: IP: {e.IP}, ID: {e.GwId}, version: {e.Version}");
                var device = Devices?.FirstOrDefault(d => d.deviceId == e.GwId);
                if (device?.ip == null 
                    || device.localKey == null 
                    || device.deviceId == null 
                    || device.name == null)
                    return;

                Dictionary<int, object> result;
                var tuyaDevice = new com.clusterrr.TuyaNet.TuyaDevice(device.ip, device.localKey, device.deviceId);
                result = await tuyaDevice.GetDps();

                if (result.Any() && result.ContainsKey(1))
                {
                    var entityName = "binary_sensor." + device.name;
                    var state = (bool)result[1] == true ? "on" : "off";
                    SetState(entityName, state, new { device_class = "window", friendly_name = device.friendly_name });
                    Log($"TUYA: Set entity: {entityName} to state: {state}");
                }
                if (result.Any() && result.ContainsKey(2))
                {
                    var entityName = "sensor." + device.name + "_battery";
                    var state = result[2];
                    SetState(entityName, state, new { device_class = "battery", unit_of_measurement = "%" });
                    Log($"TUYA: Set entity: {entityName} to state: {state}");
                }
            }
            catch (Exception exception)
            {
                LogError(exception, "TUYA: Failed to process result");
            }
        }

        public override ValueTask DisposeAsync()
        {
            _scanner.Stop();
            _scanner.OnDeviceInfoReceived -= Scanner_OnDeviceInfoReceived;
            return base.DisposeAsync();
        }
    }
}
