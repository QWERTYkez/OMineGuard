using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SM = OMineManager.SettingsManager;
using PM = OMineManager.ProfileManager;
using IM = OMineManager.InformManager;
using OCM = OMineManager.OverclockManager;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Documents;
using System.IO;
using System.Threading;
using System.Windows.Media;

namespace OMineManager
{
    public static class SettingsManager
    {
        public static Dictionary<string, Miners[]>
         MinersD = new Dictionary<string, Miners[]>
         {
             { "Ethash",
                 new Miners[] { Miners.Claymore, Miners.Bminer } },
             { "Equihash(96.5)",
                 new Miners[] { Miners.Gminer } },
             { "Equihash(144.5)",
                 new Miners[] { Miners.Gminer, Miners.Bminer } },
             { "Equihash(150.5)",
                 new Miners[] { Miners.Gminer, Miners.Bminer } },
             { "Equihash(192.7)",
                 new Miners[] { Miners.Gminer } },
             { "Equihash(200.9)",
                 new Miners[] { Miners.Bminer } },
             { "Equihash(210.9)",
                 new Miners[] { Miners.Gminer } },
             { "cuckARoo29",
                 new Miners[] { Miners.Gminer, Miners.Bminer } },
             { "cuckAToo31",
                 new Miners[] { Miners.Gminer, Miners.Bminer } },
             { "CuckooCycle",
                 new Miners[] { Miners.Gminer, Miners.Bminer } },
             { "Tensority",
                 new Miners[] { Miners.Bminer } },
             { "Zhash",
                 new Miners[] { Miners.Bminer } }
         };

        public enum Miners
        {
            Claymore,
            Gminer,
            Bminer
        }
    }
    public static class MinersManager
    {
        public static RichTextBox MinetOutput;
        static Process Miner;
        public static DateTime StartMiningTime;
        public static SM.Miners? StartedMiner;
        public static string StartedProcessName;

        public static void StartLastMiner(object o)
        {
            StartMiner(PM.Profile.ConfigsList.Single(p => p.Name == PM.profile.StartedConfig));
        }
        public static void StartLastMiner()
        {
            StartMiner(PM.Profile.ConfigsList.Single(p => p.Name == PM.profile.StartedConfig));
        }
        public static Thread StaartProcessThread;
        public static void StartMiner(Profile.Config Config)
        {
            KillProcess();
            try
            {
                StaartProcessThread.Abort();
            }
            catch { }
            StaartProcessThread = new Thread(new ThreadStart(() => 
            {
                List<Profile.Overclock> LPO =
                PM.Profile.ClocksList.Where(oc => oc.Name == Config.Overclock).ToList();
                if (LPO.Count > 0)
                {
                    if (!OCM.MSIconnecting)
                    {
                        MainWindow.SystemMessage("Ожидание соединения с MSI Afterburner");
                        while (!OCM.MSIconnecting)
                        {
                            Thread.Sleep(1000);
                        }
                        MainWindow.SystemMessage("Соединение с MSI Afterburner установлено");
                    }

                    OCM.ApplyOverclock(LPO[0]);
                }
                StartedMiner = Config.Miner;
                string DT = DateTime.Now.ToString("HH.mm.ss - dd.MM.yy");
                StartedProcessName = default;
                string param = default;
                string logfile = $"MinersLogs/log {DT} {Config.Name}.txt";
                string dir = default;
                //Клеймор
                if (Config.Miner == SM.Miners.Claymore)
                {
                    dir = "Claymore's Dual Miner";
                    string lf = "MinersLogs/buflog.txt";
                    StartedProcessName = "EthDcrMiner64";
                    param = $" -epool {Config.Pool}:{Config.Port} " +
                        $"-ewal {Config.Wallet}.{PM.Profile.RigName} " +
                        $"-logfile \"{lf}\" -wd 0 {Config.Params}";

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
                    Miner.StartInfo = new ProcessStartInfo($"{dir}/{StartedProcessName}", param);
                    Miner.StartInfo.UseShellExecute = false;
                    Miner.StartInfo.CreateNoWindow = true;
                    Miner.Start();

                    Task.Run(() => ReadLog(lf, logfile));
                }
                //Гмайнер
                if (Config.Miner == SM.Miners.Gminer)
                {
                    dir = "Gminer";
                    StartedProcessName = "miner";
                    string algo = "";

                    switch (Config.Algoritm)
                    {
                        case "Equihash(96.5)":
                            algo = "equihash96_5";
                            break;
                        case "Equihash(144.5)":
                            algo = "equihash144_5";
                            break;
                        case "Equihash(150.5)":
                            algo = "equihash150_5";
                            break;
                        case "Equihash(192.7)":
                            algo = "equihash192_7";
                            break;
                        case "Equihash(210.9)":
                            algo = "equihash210_9";
                            break;
                        case "cuckARoo29":
                            algo = "cuckaroo29";
                            break;
                        case "cuckAToo31":
                            algo = "cuckatoo31";
                            break;
                        case "CuckooCycle":
                            algo = "cuckoo";
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
                    Miner.StartInfo = new ProcessStartInfo($"{dir}/{StartedProcessName}", param);
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
                if (Config.Miner == SM.Miners.Bminer)
                {
                    dir = "Bminer";
                    StartedProcessName = "bminer";
                    string algo = "";

                    switch (Config.Algoritm)
                    {
                        case "Equihash(144.5)":
                            algo = "equihash1445";
                            break;
                        case "Equihash(150.5)":
                            algo = "beam";
                            break;
                        case "Equihash(200.9)":
                            algo = "stratum";
                            break;
                        case "Ethash":
                            algo = "ethash";
                            break;
                        case "Tensority":
                            algo = "tensority";
                            break;
                        case "CuckooCycle":
                            algo = "aeternity";
                            break;
                        case "cuckARoo29":
                            algo = "cuckaroo29d";
                            break;
                        case "cuckAToo31":
                            algo = "cuckatoo31";
                            break;
                        case "Zhash":
                            algo = "zhash";
                            break;
                    }
                    param = $"-uri {algo}://{Config.Wallet}.{PM.Profile.RigName}@{Config.Pool}:{Config.Port} {Config.Params} -logfile \"{logfile}\" -api 127.0.0.1:3333";

                    if (algo == "equihash1445")
                    {
                        param += " -pers Auto";
                    }

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
                            param += $" -devices {di}";
                        }
                    }

                    Miner = new Process();
                    Miner.StartInfo = new ProcessStartInfo($"{dir}/{StartedProcessName}", param);
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
                MainWindow.SystemMessage($"{dir} запущен");
                IM.InformMessage($"{dir} запущен");
                MainWindow.context.Send(ButtonSetTitleToStop, null);
                IM.MinHashrate = Config.MinHashrate;
                RunIndication();
                PM.Profile.StartedProcess = StartedProcessName;
                PM.Profile.StartedConfig = Config.Name;
                PM.SaveProfile();
                IM.StartWaching(Config.Miner);
                IM.StartInternetWachdog();
            }));
            StaartProcessThread.Start();
        }
        public static void KillProcess(object o)
        {
            KillProcess();
        }
        public static void KillProcess()
        {
            bool b = false;
            foreach (Process proc in Process.GetProcessesByName(PM.Profile.StartedProcess))
            {
                try
                {
                    proc.Kill();
                    b = true;
                }
                catch { }
            }
            if (b) MainWindow.SystemMessage("Процесс завершен");
            try
            {
                IndicationThread.Abort();
                IM.WachingThread.Abort();
            }
            catch { }
            try
            {
                File.Delete("MinersLogs/buflog.txt");
            }
            catch { }
            MainWindow.context.Send(MainWindow.Sethashrate, null);
            MainWindow.context.Send(SetIndicationColor, Brushes.Red);
            MainWindow.context.Send(ButtonSetTitleToStart, null);
        }
        private static void ButtonSetTitleToStart(object o)
        {
            MainWindow.This.KillProcess.Content = "Запустить процесс";
            MainWindow.This.KillProcess2.Content = "Запустить процесс";
        }
        private static void ButtonSetTitleToStop(object o)
        {
            MainWindow.This.KillProcess.Content = "Завершить процесс";
            MainWindow.This.KillProcess2.Content = "Завершить процесс";
        }
        public static void RestartMining()
        {
            IM.StartIdleWatchdog();
            KillProcess();
            Thread.Sleep(10000);
            StartLastMiner();
        }
        #region Indicator
        public static Thread IndicationThread;
        private static void RunIndication()
        {
            try
            {
                IndicationThread.Abort();
            }
            catch { }
            IndicationThread = new Thread(new ThreadStart(() =>
             {
                 IndicationThread = Thread.CurrentThread;
                 if (Process.GetProcessesByName(StartedProcessName).Length == 0)
                 {
                     while (Process.GetProcessesByName(StartedProcessName).Length == 0)
                     {
                         Thread.Sleep(50);
                     }
                 }
                 while (Process.GetProcessesByName(StartedProcessName).Length != 0)
                 {
                     MainWindow.context.Send(SetIndicationColor, Brushes.Lime);
                     Thread.Sleep(700);
                     MainWindow.context.Send(SetIndicationColor, null);
                     Thread.Sleep(300);
                 }
                 MainWindow.context.Send(SetIndicationColor, Brushes.Red);
                 MainWindow.context.Send((object o) =>
                 {
                     MainWindow.This.KillProcess.Content = "Запустить процесс";
                     MainWindow.This.KillProcess2.Content = "Запустить процесс";
                 }, null);
             }));
            IndicationThread.Start();


        }
        private static void SetIndicationColor(object o)
        {
            MainWindow.This.IndicatorEl.Fill = (Brush)o;
            MainWindow.This.IndicatorEl2.Fill = (Brush)o;
        }
        #endregion
        #region Logfile
        private static void proc_DataReceived(object sender, DataReceivedEventArgs e)
        {
            if(e.Data != "")
            {
                MainWindow.context.Send(WriteToRichTextBox, e.Data);
            }
        }
        private static void ReadLog(string lf, string logfile)
        {
            long end = 0;
            string str;
            if (Process.GetProcessesByName(StartedProcessName).Length == 0)
            {
                while (Process.GetProcessesByName(StartedProcessName).Length != 0)
                {
                    Thread.Sleep(50);
                }
            }
            while (Process.GetProcessesByName(StartedProcessName).Length != 0)
            {
                try
                {
                    using (FileStream fstream = new FileStream(lf, FileMode.Open))
                    {
                        if (fstream.Length > end)
                        {
                            byte[] array = new byte[fstream.Length - end];
                            fstream.Seek(end, SeekOrigin.Current);
                            fstream.Read(array, 0, array.Length);
                            str = System.Text.Encoding.Default.GetString(array);

                            if (!(str.Contains("srv") || 
                                  str.Contains("recv") || 
                                  str.Contains("sent")))
                            {
                                MainWindow.context.Send(WriteToRichTextBox, str);
                                Task.Run(() =>
                                {
                                    using (StreamWriter fstr = new StreamWriter(logfile, true))
                                    {
                                        fstr.WriteLine(str);
                                    }
                                });
                            }

                            end = fstream.Length;
                        }
                    }
                }
                catch { }
                Thread.Sleep(200);
            }
            File.Delete("MinersLogs/buflog.txt");
        }
        public static void WriteToRichTextBox(object t)
        {
            string str = (string)t;
            if (str != null)
            {
                if (StartedMiner == SM.Miners.Claymore)
                {
                    bool[] red =
                    {
                    str.Contains("Cannot connect to"),
                    str.Contains("Failed to connect")
                };
                    bool[] green =
                    {
                    str.Contains(" Connected ")
                };
                    if (red.Contains(true))
                    {
                        WriteLog(str, Brushes.Red);
                    }
                    else if (green.Contains(true))
                    {
                        WriteLog(str, Brushes.Lime);
                    }
                    else
                    {
                        WriteLog(str);
                    }
                }
                if (StartedMiner == SM.Miners.Gminer)
                {
                    WriteLog(str);
                }
                if (StartedMiner == SM.Miners.Bminer)
                {
                    WriteLog(str);
                }

                if (MainWindow.AutoScroll)
                {
                    MainWindow.This.LogScroller.ScrollToEnd();
                }
            }
        }
        public static void WriteLog(string str, Brush brush)
        {
            //TextRange tr = new TextRange(MainWindow.This.MinerLog.Document.ContentEnd,
            //    MainWindow.This.MinerLog.Document.ContentEnd);
            //tr.Text = str;
            //tr.ApplyPropertyValue(TextElement.ForegroundProperty, brush);
            //MainWindow.This.MinerLog.AppendText(Environment.NewLine);
        }
        public static void WriteLog(string str)
        {
            //TextRange tr = new TextRange(MainWindow.This.MinerLog.Document.ContentEnd,
            //    MainWindow.This.MinerLog.Document.ContentEnd);
            //tr.Text = str;
            //tr.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.White);
            //MainWindow.This.MinerLog.AppendText(Environment.NewLine);
        }
        #endregion
    }
}