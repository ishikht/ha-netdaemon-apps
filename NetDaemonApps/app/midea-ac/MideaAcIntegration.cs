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

        }
    }
}