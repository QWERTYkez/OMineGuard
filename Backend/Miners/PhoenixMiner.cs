using Newtonsoft.Json;
using OMineGuard.Backend;
using OMineGuardControlLibrary;
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
    public class PhoenixMiner : Miner
    {
        private protected override string Directory { get; } = "PhoenixMiner";
        private protected override string ProcessName { get => "PhoenixMiner"; }

        private protected override void RunThisMiner(IConfig Config)
        {
            string DT = DateTime.Now.ToString("HH.mm.ss - dd.MM.yy");
            string logfile = $"MinersLogs/log {DT} {Config.Name}.txt";

            string logbuffer = $"{LogFolder}/buflog.txt";
            string param = $" -pool {Config.Pool}:{Config.Port} " +
                    $"-wal {Config.Wallet} " +
                    $"-worker {Settings.Profile.RigName} " +
                    $"-logfile \"{logfile}\" -retrydelay 2 -Wdog 0 {Config.Params} -mport -{port}";
            if (Settings.Profile.GPUsSwitch != null)
            {
                string di = "";
                int n = Settings.Profile.GPUsSwitch.Count;
                for (int i = 0; i < n; i++)
                {
                    if (Settings.Profile.GPUsSwitch[i])
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
                req = new byte[51];
                var xx = Encoding.UTF8.GetBytes("{\"id\":0,\"jsonrpc\":\"2.0\",\"method\":\"miner_getstat2\"}");
                for (int i = 0; i < xx.Length; i++) req[i] = xx[i]; req[50] = 10;
            }
            {
                process = new Process
                {
                    EnableRaisingEvents = true,
                    StartInfo = new ProcessStartInfo($"{Directory}/{ProcessName}", param)
                    {
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    }
                };
                process.ErrorDataReceived += (s, e) => { if (e.Data != "") Logging(e.Data); };
                process.OutputDataReceived += (s, e) => { if (e.Data != "") Logging(e.Data); };

                process.Start();
                process.BeginErrorReadLine();
                process.BeginOutputReadLine();
            }
        }

        private static byte[] req;
        private protected override MinerInfo GetInformation()
        {
            using (TcpClient client = new TcpClient("127.0.0.1", port))
            {
                using (NetworkStream stream = client.GetStream())
                {
                    stream.Write(req, 0, req.Length);
                    byte[] data = new byte[1024];
                    int bytes = stream.Read(data, 0, data.Length);
                    string message = Encoding.UTF8.GetString(data, 0, bytes);

                    List<string> LS = JsonConvert.DeserializeObject<ClaymoreInfo>(message).result;

                    List<double?> Hashrates = JsonConvert.DeserializeObject<List<double?>>($"[{LS[3].Replace(";", ",")}]");
                    for (int i = 0; i < Hashrates.Count; i++)
                    {
                        Hashrates[i] = Hashrates[i] / 1000;
                    }
                    int lt = Hashrates.Count;
                    int?[] xx = JsonConvert.DeserializeObject<int?[]>($"[{LS[6].Replace(";", ",")}]");
                    List<int?> Temperatures = new List<int?>();
                    List<int?> Fanspeeds = new List<int?>();
                    for (int n = 0; n < xx.Length; n += 2)
                    {
                        Temperatures.Add(xx[n]);
                        Fanspeeds.Add(xx[n + 1]);
                    }
                    List<int?> ShAccepted = JsonConvert.DeserializeObject<List<int?>>($"[{LS[9].Replace(";", ",")}]");
                    List<int?> ShRejected = JsonConvert.DeserializeObject<List<int?>>($"[{LS[10].Replace(";", ",")}]");
                    List<int?> ShInvalid = JsonConvert.DeserializeObject<List<int?>>($"[{LS[11].Replace(";", ",")}]");

                    if (GPUs != null)
                    {
                        for (int i = 0; i < GPUs.Count; i++)
                        {
                            if (!GPUs[i])
                            {
                                Hashrates.Insert(i, null);
                                Temperatures.Insert(i, null);
                                Fanspeeds.Insert(i, null);
                                ShAccepted.Insert(i, null);
                                ShRejected.Insert(i, null);
                                ShInvalid.Insert(i, null);
                            }
                        }
                    }
                    return new MinerInfo(Hashrates, Temperatures, Fanspeeds, ShAccepted, ShRejected, ShInvalid);
                }
            }
        }

        private class ClaymoreInfo
        {
#pragma warning disable IDE1006 // Стили именования
            public List<string> result { get; set; }
#pragma warning restore IDE1006 // Стили именования
        }
    }
}
