using OMineGuard.Backend;
using OMineGuard.Miners;
using System.Windows;

namespace OMineGuard
{
    public partial class App : Application
    {
        private void Application_Exit(object sender, ExitEventArgs e)
        {
            Miner.StopMiner();
            TCPserver.StopServers();
            Overclocker.ApplicationLive = false;
        }
    }
}
