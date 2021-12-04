namespace TerneoIntegration.TerneoNet.JsonConverters
{
    public class CloudDevice
    {
        public int Id { get; set; }
        public string SerialNumber { get; set; }
        public string Name { get; set; }
        public bool IsOffline { get; set; }
        public bool CurrentTemperature { get; set; }
        public bool TargetTemperature { get; set; }
    }
}