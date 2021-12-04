namespace TerneoIntegration.TerneoNet
{
    public interface ITerneoTelemetry
    {
        int TargetTemperature { get; set; }
        decimal CurrentTemperature { get; set; }
        bool Heating { get; set; }
        bool PowerOff { get; set; }
    }
}