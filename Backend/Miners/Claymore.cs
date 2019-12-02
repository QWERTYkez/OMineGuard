using Newtonsoft.Json;
using OMineGuard.Backend;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OMineGuard.Miners
{
    public class Claymore : Miner
    {
        private protected override string Directory { get; set; } = "Claymore's Dual Miner";
        private protected override string ProcessName { get; set; } = "EthDcrMiner64";
        private protected override Process miner { get; set; }

        public override event Action<long> MinerStarted;
        public override event Action MinerStoped;
        public override event Action<string> LogDataReceived;

        private protected override void RunThisMiner(Config Config)
        {
            Profile prof = Settings.GetProfile();
            string DT = DateTime.Now.ToString("HH.mm.ss - dd.MM.yy");
            string logfile = $"MinersLogs/log {DT} {Config.Name}.txt";

            string logbuffer = $"{LogFolder}/buflog.txt";
            string param = $" -epool {Config.Pool}:{Config.Port} " +
                    $"-ewal {Config.Wallet}.{prof.RigName} " +
                    $"-logfile \"{logfile}\" -wd 0 {Config.Params} -mport -{port}";
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

        private static readonly byte[] req = Encoding.UTF8.GetBytes("{ \"id\":0,\"jsonrpc\":\"2.0\",\"method\":\"miner_getstat2\"}");
        private protected override MinerInfo? CurrentMinerGetInfo()
        {
            try
            {
                using (NetworkStream stream = (new TcpClient("127.0.0.1", port)).GetStream())
                {
                    stream.Write(req, 0, req.Length);
                    byte[] data = new byte[1024];
                    int bytes = stream.Read(data, 0, data.Length);
                    string message = Encoding.UTF8.GetString(data, 0, bytes);

                    List<string> LS = JsonConvert.DeserializeObject<ClaymoreInfo>(message).result;

                    List<double>  Hashrates = JsonConvert.DeserializeObject<List<double>>($"[{LS[3].Replace(";", ",")}]");
                    for (int i = 0; i < Hashrates.Count; i++)
                    {
                        Hashrates[i] = Hashrates[i] / 1000;
                    }
                    int lt = Hashrates.Count;
                    int[] xx = JsonConvert.DeserializeObject<int[]>($"[{LS[6].Replace(";", ",")}]");
                    List<int> Temperatures = new List<int>(lt);
                    List<int> Fanspeeds = new List<int>(lt);
                    for (int n = 0; n < xx.Length; n = n + 2)
                    {
                        Temperatures[n / 2] = (byte)xx[n];
                        Fanspeeds[n / 2] = (byte)xx[n + 1];
                    }
                    List<int> ShAccepted = JsonConvert.DeserializeObject<List<int>>($"[{LS[9].Replace(";", ",")}]");
                    List<int> ShRejected = JsonConvert.DeserializeObject<List<int>>($"[{LS[10].Replace(";", ",")}]");
                    List<int> ShInvalid = JsonConvert.DeserializeObject<List<int>>($"[{LS[11].Replace(";", ",")}]");

                    if (GPUs != null)
                    {
                        for (int i = 0; i < GPUs.Count; i++)
                        {
                            if (!GPUs[i])
                            {
                                Hashrates.Insert(i, -1);
                                Temperatures.Insert(i, -1);
                                Fanspeeds.Insert(i, -1);
                                ShAccepted.Insert(i, -1);
                                ShRejected.Insert(i, -1);
                                ShInvalid.Insert(i, -1);
                            }
                        }
                    }
                    return new MinerInfo(Hashrates, Temperatures, Fanspeeds, ShAccepted, ShRejected, ShInvalid);
                }
            }
            catch { return null; }
        }
        private class ClaymoreInfo
        {
            public List<string> result { get; set; }
        }
    }
}
