using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using IM = OMineManager.InformManager;
using MM = OMineManager.MinersManager;

namespace OMineManager
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
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
