using System.Threading.Tasks;

namespace TerneoIntegration.TerneoNet
{
    public interface ITerneoService
    {
        Task<ITerneoTelemetry?> GetTelemetryAsync(string serialNumber);
        Task<bool> SetTemperature(string serialNumber, int temperature);

        Task<bool> PowerOnOff(string serialNumber, bool isOff);
    }
}