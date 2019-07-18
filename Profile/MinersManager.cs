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
using System.Threading;
using System.Windows.Media;

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
            string logfile = $"logs/log {DT} {Config.Name}.txt";
            string dir = "";
            //Клеймор
            if (Config.Miner == SM.Miners.Claymore)
            {
                dir = "Claymore's Dual Miner";
                file = "EthDcrMiner64.exe";
                param = $" -epool {Config.Pool}:{Config.Port} " +
                    $"-ewal {Config.Wallet}.{PM.Profile.RigName} " +
                    $"-logfile \"{logfile}\" {Config.Params}";

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

                Miner = new Process();
                Miner.StartInfo = new ProcessStartInfo($"{dir}/{file}", param);
                Miner.StartInfo.UseShellExecute = false;
                Miner.StartInfo.CreateNoWindow = true;
                Miner.Start();
                Task.Run(() => ReadLog(logfile));
            }
            //Гмайнер
            if (Config.Miner == SM.Miners.Gminer)
            {
                dir = "Gminer";
                file = "miner.exe";
                string algo = "";

                switch (Config.Algoritm)
                {
                    case "Equihash(150,5)":
                        algo = "150_5";
                        break;
                }
                param = $"--algo {algo} --server {Config.Pool} --port {Config.Port} " +
                    $"--api 3333 --user {Config.Wallet}.{PM.Profile.RigName} " +
                    $"{Config.Params} --logfile \"{logfile}\"";

                if (PM.Profile.GPUsSwitch != null)
                {
                    string di = "";
                    int n = PM.Profile.GPUsSwitch.Length;
                    for (int i = 0; i < n; i++)
                    {
                        if (PM.Profile.GPUsSwitch[i])
                        {
                            di += " " + i;
                        }
                        di = di.TrimStart(' ');
                    }
                    if (di != "")
                    {
                        param += $" --devices {di}";
                    }
                }

                Miner = new Process();
                Miner.StartInfo = new ProcessStartInfo($"{dir}/{file}", param);
                Miner.StartInfo.UseShellExecute = false;
                // set up output redirection
                Miner.StartInfo.RedirectStandardOutput = true;
                Miner.StartInfo.RedirectStandardError = true;
                Miner.EnableRaisingEvents = true;
                Miner.StartInfo.CreateNoWindow = true;
                // see below for output handler
                Miner.ErrorDataReceived += proc_DataReceived;
                Miner.OutputDataReceived += proc_DataReceived;

                Task.Run(() => 
                {
                    Miner.Start();

                    Miner.BeginErrorReadLine();
                    Miner.BeginOutputReadLine();

                    Miner.WaitForExit();

                });
                
            }
            //Бмайнер

            
        }

        #region Logfile
        private static void proc_DataReceived(object sender, DataReceivedEventArgs e)
        {
            MainWindow.context.Send(WriteToRichTextBox, e.Data);
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
            string str = (string)t;
            WriteLog(str);
            //if (MainWindow.AutoScroll)
            //{
            //    MainWindow.LogScroller.ScrollToEnd();
            //}
        }
        public static void WriteLog(string str, Brush brush)
        {
            TextRange tr = new TextRange(MainWindow.MinerLogBox.Document.ContentEnd,
                MainWindow.MinerLogBox.Document.ContentEnd);
            tr.Text = str;
            tr.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Red);
        }
        public static void WriteLog(string str)
        {
            MainWindow.MinerLogBox.AppendText(str);
            MainWindow.MinerLogBox.AppendText(str);
            MainWindow.MinerLogBox.AppendText(str);
            MainWindow.MinerLogBox.AppendText(str);
        }
        #endregion
    }
}


