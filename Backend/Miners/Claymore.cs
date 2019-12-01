using OMineGuard.Backend;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace OMineGuard.Miners
{
    public class Claymore : Miner
    {
        private protected override string Directory { get; set; } = "Claymore's Dual Miner";
        private protected override string ProcessName { get; set; } = "EthDcrMiner64";
        private protected override Process miner { get; set; }
        private protected override void RunThisMiner(Config Config)
        {
            Profile prof = Settings.GetProfile();
            string DT = DateTime.Now.ToString("HH.mm.ss - dd.MM.yy");
            string logfile = $"MinersLogs/log {DT} {Config.Name}.txt";

            string logbuffer = $"{LogFolder}/buflog.txt";
            string param = $" -epool {Config.Pool}:{Config.Port} " +
                    $"-ewal {Config.Wallet}.{prof.RigName} " +
                    $"-logfile \"{logfile}\" -wd 0 {Config.Params}";
            if (prof.GPUsSwitch != null)
            {
                string di = "";
                int n = prof.GPUsSwitch.Count;
                for (int i = 0; i < n; i++)
                {
                    if (prof.GPUsSwitch[i])
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
            {
                miner = new Process();
                miner.StartInfo = new ProcessStartInfo($"{Directory}/{ProcessName}", param);
                miner.StartInfo.UseShellExecute = false;
                miner.StartInfo.CreateNoWindow = true;
                miner.Start();
                {
                    StartReadLog(logbuffer, logfile);
                }
            }
        }
        private void StartReadLog(string logbuffer, string logfile)
        {
            long end = 0;
            string str;
            if (Process.GetProcessesByName(ProcessName).Length == 0)
            {
                while (Process.GetProcessesByName(ProcessName).Length != 0)
                {
                    Thread.Sleep(50);
                }
            }
            while (Process.GetProcessesByName(ProcessName).Length != 0)
            {
                try
                {
                    using (FileStream fstream = new FileStream(logbuffer, FileMode.Open))
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
                                Task.Run(() => LogDataReceived?.Invoke(str));
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
            File.Delete(logbuffer);
        }
    }
}
