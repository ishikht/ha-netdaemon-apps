namespace TerneoIntegration.TerneoNet
{
    public class TerneoDevice
    {
        public TerneoDevice(string ip, string serialNumber)
        {
            SerialNumber = serialNumber;
            Ip = ip;
        }

        public string Ip { get; }
        public string SerialNumber { get; }
        
        
        public override string ToString() => $"IP: {Ip}, sn: {SerialNumber}";
    }
}