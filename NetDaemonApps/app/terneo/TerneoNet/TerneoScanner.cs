using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;

namespace TerneoIntegration.TerneoNet
{
    public class TerneoScanner
    {
        private const ushort UDP_PORT = 23500;
        private readonly List<TerneoDeviceScanInfo> _devices = new();

        private bool _isRunning;
        private Thread? _udpListener;
        private UdpClient? _udpServer;

        /// <summary>
        ///     Even that will be called on every broadcast message from devices.
        /// </summary>
        public event EventHandler<TerneoDeviceScanInfo>? OnDeviceInfoReceived;

        /// <summary>
        ///     Even that will be called only once for every device.
        /// </summary>
        public event EventHandler<TerneoDeviceScanInfo>? OnNewDeviceInfoReceived;


        /// <summary>
        ///     Starts scanner.
        /// </summary>
        public void Start()
        {
            Stop();
            _isRunning = true;
            _devices.Clear();
            _udpServer = new UdpClient(UDP_PORT);
            _udpListener = new Thread(UdpListenerThread!);
            _udpListener.Start(_udpServer);
        }

        /// <summary>
        ///     Stops scanner.
        /// </summary>
        public void Stop()
        {
            _isRunning = false;
            if (_udpServer != null)
            {
                _udpServer.Dispose();
                _udpServer = null;
            }

            _udpListener = null;
        }

        private void UdpListenerThread(object o)
        {
            if (o is not UdpClient udpServer) 
                throw new ArgumentException("Parameter is null", nameof(o));
            
            while (_isRunning)
                try
                {
                    var ep = new IPEndPoint(IPAddress.Any, 0);
                    var data = udpServer.Receive(ref ep);
                    string returnData = Encoding.ASCII.GetString(data);
                    Parse(returnData, ep.Address); 
                }
                catch
                {
                    if (!_isRunning) return;
                    throw;
                }
        }

        private void Parse(string json, IPAddress ip)
        {
            var deviceInfo = JsonConvert.DeserializeObject<TerneoDeviceScanInfo>(json);
            if (deviceInfo == null) return;
            
            deviceInfo.Ip = ip.ToString();
            OnDeviceInfoReceived?.Invoke(this, deviceInfo);
            
            if (OnNewDeviceInfoReceived == null || _devices.Contains(deviceInfo)) return;

            _devices.Add(deviceInfo);
            OnNewDeviceInfoReceived?.Invoke(this, deviceInfo);
        }
    }
}