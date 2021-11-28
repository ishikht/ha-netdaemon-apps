using System;
using Newtonsoft.Json;

namespace TerneoIntegration.TerneoNet
{
    public class TerneoDeviceScanInfo:IEquatable<TerneoDeviceScanInfo>
    {
        public string? Ip { get; set; } = null;

        [JsonProperty("sn")]
        public string? SerialNumber { get; set; } = null;

        [JsonProperty("hw")]
        public string? Hardware { get; set; } = null;

        [JsonProperty("cloud")]
        public bool? Cloud { get; set; } = null;

        [JsonProperty("connection")]
        public string? Connection { get; set; } = null;

        [JsonProperty("wifi")]
        public string? WifiSignal { get; set; } = null;

        [JsonProperty("display")]
        public string? Display { get; set; } = null;

        public bool Equals(TerneoDeviceScanInfo? other)
            => (Ip == other?.Ip) && (SerialNumber == other?.SerialNumber);

        public override string ToString()
            => $"IP: {Ip}, sn: {SerialNumber}, hw: {Hardware}, cloud: {Cloud}, connection: {Connection}, Wifi: {WifiSignal}, display: {Display}";
    }
}