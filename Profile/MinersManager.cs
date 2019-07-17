using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SM = OMineManager.SettingsManager;
using PM = OMineManager.ProfileManager;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Documents;
using System.IO;

namespace OMineManager
{
    public static class MinersManager
    {
        public static RichTextBox MinetOutput;
        static Process Miner;
        public static DateTime StartMiningTime;
        
        public static void StartMiner(Profile.Config Config)
        {
            StartMiningTime = DateTime.Now;
            string DT = StartMiningTime.ToString("HH.mm.ss - dd.MM.yy");
            string file = "";
            string param = "";
            string logfile = "";
            //клеймор
            if (Config.Miner == SM.Miners.Claymore)
            {
                file = "Claymore's Dual Miner/EthDcrMiner64.exe";
                logfile = "Claymore's Dual Miner/log " + DT + ".txt";
                param = " -epool " + Config.Pool + ":" + Config.Port + " -ewal " +
                    Config.Wallet + "." + PM.Profile.RigName + " -logfile \"" + logfile + "\" " + Config.Params;

                if (PM.Profile.GPUsSwitch != null)
                {
                    string di = "";
                    int n = PM.Profile.GPUsSwitch.Length;
                    for (int i = 0; i < n; i++)
                    {
                        if (PM.Profile.GPUsSwitch[i])
                        {
                            di += "," + i;
                        }
                        di = di.TrimStart(',');
                    }
                    if (di != "")
                    {
                        param += " -di " + di;
                    }
                }
            }
            //Гмайнер
            //Бмайнер

            Miner = new Process();
            Miner.StartInfo = new ProcessStartInfo(file, param);
            Miner.StartInfo.UseShellExecute = false;
            Miner.Start();
            Task.Run(() => ReadLog(logfile));
        }

        private static void ReadLog(string logfile)
        {
            long end = 0;
            while (!Miner.HasExited)
            {
                try
                {
                    using (FileStream fstream = new FileStream(logfile, FileMode.Open))
                    {
                        if (fstream.Length > end)
                        {
                            byte[] array = new byte[fstream.Length - end];
                            fstream.Seek(end, SeekOrigin.Current);
                            fstream.Read(array, 0, array.Length);

                            MainWindow.context.Send(WriteToRichTextBox, System.Text.Encoding.Default.GetString(array));

                            end = fstream.Length;
                        }
                    }
                }
                catch { }
            }
        }
        public static void WriteToRichTextBox(object t)
        {
            // Обработка лог файла
        }
    }
}


