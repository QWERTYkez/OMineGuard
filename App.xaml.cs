using OMineGuard.Backend;
using System.Windows;

namespace OMineGuard
{
    public partial class App : Application
    {
        private void Application_Exit(object sender, ExitEventArgs e)
        {
            Backend.Models.MainModel.ExxtraStopMiner();
            TCPserver.StopServers();
            Overclocker.ApplicationLive = false;
        }
    }
}
