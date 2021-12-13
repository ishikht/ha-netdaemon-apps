using System.Linq;
using System.Threading.Tasks;
using MideaAcIntegration.MideaNet;
using NetDaemon.Common;
using NetDaemon.Common.Reactive;

namespace MideaAcIntegration
{
    public class MideaAcIntegration: NetDaemonRxApp
    {
        public string? Email { get; set; }
        public string? Password { get; set; }
        public override async Task InitializeAsync()
        {
            var cloud = new MideaCloud(Email, Password);
            await cloud.LoginAsync();
            var devices = await cloud.GetUserDevicesList();
            var device = devices.Single(d => d.Id == "");
            var telemetry = await cloud.GetTelemetry(device);
            //om =4 Heat
            //om =5 fan
            //om =2 cold
            //om =3 dry
            //om =1 auto
        }
    }
}