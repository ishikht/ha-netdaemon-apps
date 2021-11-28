using System;
using NetDaemon.Common.Reactive;
using TerneoIntegration.TerneoNet;

namespace TerneoIntegration
{
    public class TerneoIntegration : NetDaemonRxApp
    {
        private readonly TerneoScanner _scanner = new();

        public override void Initialize()
        {
            _scanner.OnDeviceInfoReceived += Scanner_OnDeviceInfoReceived;
            Log("TERNEO: Starting discovery");
            _scanner.Start();
            Log("TERNEO: Discovery started");
        }

        private void Scanner_OnDeviceInfoReceived(object? sender, TerneoDeviceScanInfo e)
        {
            Console.WriteLine($"TERNEO: {e}");
        }
    }
}