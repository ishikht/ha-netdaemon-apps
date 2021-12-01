using NetDaemon.Common.Reactive;
using TerneoIntegration.TerneoNet;

namespace TerneoIntegration
{
    public class TerneoIntegration : NetDaemonRxApp
    {
        private readonly TerneoScanner _scanner = new();

        public override void Initialize()
        {
            _scanner.OnNewDeviceInfoReceived += Scanner_OnDeviceInfoReceived;
            Log("TERNEO: Starting discovery");
            _scanner.Start();
            Log("TERNEO: Discovery started");
        }

        private async void Scanner_OnDeviceInfoReceived(object? sender, TerneoDeviceScanInfo e)
        {
            if (string.IsNullOrEmpty(e.Ip) || string.IsNullOrEmpty(e.SerialNumber))
            {
                LogError("Failed to obtain IP or Serial number of device");
                return;
            }
            
            Log($"TERNEO: New Device Discovered {e}");
            var device = new TerneoDevice(e.Ip, e.SerialNumber);
            var telemetry = await device.GetTelemetry();
        }
    }
}