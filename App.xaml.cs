using System.Threading;
using System.Windows;
using IM = OMineGuard.InformManager;
using MM = OMineGuard.MinersManager;
using MW = OMineGuard.MainWindow;

namespace OMineGuard
{
    public partial class App : Application
    {
        private void Application_Exit(object sender, ExitEventArgs e)
        {
            MM.KillProcess();
            AbortThread(IM.WachingThread);
            IM.StopWachdog();
            MM.StopRMT();
            AbortThread(MM.IndicationThread);
            AbortThread(MM.StaartProcessThread);
            TCPserver.AbortTCP();
            AbortThread(MW.ShowMinerLogThread);
            IM.StopIdleWatchdog();
            IM.StopLHWatchdog();
        }
        private void AbortThread(Thread t)
        {
            try
            {
                t.Abort();
            }
            catch { }
        }
    }
}
