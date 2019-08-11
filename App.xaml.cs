using System.Threading;
using System.Windows;
using IM = OMineGuard.InformManager;
using MM = OMineGuard.MinersManager;

namespace OMineGuard
{
    public partial class App : Application
    {
        private void Application_Exit(object sender, ExitEventArgs e)
        {
            MinersManager.KillProcess();
            AbortThread(InformManager.WachingThread);
            IM.StopWachdog();
            IM.StopIdleWatchdog();
            MM.StopRMT();
            AbortThread(MinersManager.IndicationThread);
            AbortThread(MinersManager.StaartProcessThread);
            TCPserver.AbortTCP();
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
