using Newtonsoft.Json;
using OMineGuard.Backend;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace OMineGuard.Miners
{
    class Bminer : Miner
    {
        private protected override string Directory { get; set; } = "Bminer";
        private protected override string ProcessName { get; set; } = "bminer";

        private protected override void RunThisMiner(Config Config)
        {
            string DT = DateTime.Now.ToString("HH.mm.ss - dd.MM.yy");
            string logfile = $"MinersLogs/log {DT} {Config.Name}.txt";

            string algo = "";

            switch (Config.Algoritm)
            {
                case "Equihash 144.5":
                    algo = "equihash1445";
                    break;
                case "Equihash 150.5":
                    algo = "beam";
                    break;
                case "Equihash 200.9":
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
            string param = $"-uri {algo}://{Config.Wallet}.{Settings.Profile.RigName}" +
                $"@{Config.Pool}:{Config.Port} {Config.Params} " +
                $"-logfile \"{logfile}\" -api 127.0.0.1:{port}";
            if (algo == "equihash1445")
            {
                param += " -pers Auto";
            }
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
                    param += $" -devices {di}";
                }
            }

            miner = new Process
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
            miner.ErrorDataReceived += (s, e) => { if (e.Data != "") Logging(e.Data); };
            miner.OutputDataReceived += (s, e) => { if (e.Data != "") Logging(e.Data); };

            miner.Start();
            miner.BeginErrorReadLine();
            miner.BeginOutputReadLine();
        }

        private protected override MinerInfo CurrentMinerGetInfo()
        {
            try
            {
                WebRequest request = WebRequest.Create($"http://localhost:{port}/api/status");
                using (WebResponse response = request.GetResponse())
                {
                    using (Stream stream = response.GetResponseStream())
                    {
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            string content = reader.ReadToEnd();
                            for (int i = 0; i < 20; i++)
                            {
                                content = content.Replace("{\"" + i + "\":", "[");
                            }
                            content = content.Replace("}}}}", "}}}]");

                            BminerInfo INF = JsonConvert.DeserializeObject<BminerInfo>(content);

                            List<double?> Hashrates = INF.miners.Select(m => m.solver.solution_rate).ToList();
                            List<int?> Temperatures = INF.miners.Select(m => m.device.temperature).ToList();
                            List<int?> Fanspeeds = INF.miners.Select(m => m.device.fan_speed).ToList();

                            if (GPUs != null)
                            {
                                for (int i = 0; i < GPUs.Count; i++)
                                {
                                    if (!GPUs[i])
                                    {
                                        Hashrates.Insert(i, null);
                                        Temperatures.Insert(i, null);
                                        Fanspeeds.Insert(i, null);
                                    }
                                }
                            }

                            return new MinerInfo(Hashrates, Temperatures, Fanspeeds, 
                                INF.stratum.accepted_shares, INF.stratum.rejected_shares, null);
                        }
                    }
                }
            }
            catch { return new MinerInfo(); }
        }
        private class BminerInfo
        {
#pragma warning disable IDE1006 // Стили именования
            public BminerStratum stratum { get; set; }
            public List<BminerMiner> miners { get; set; }
#pragma warning restore IDE1006 // Стили именования
        }
        private class BminerStratum
        {
#pragma warning disable IDE1006 // Стили именования
            public int accepted_shares { get; set; }
            public int rejected_shares { get; set; }
#pragma warning restore IDE1006 // Стили именования
        }
        private class BminerMiner
        {
#pragma warning disable IDE1006 // Стили именования
            public BminerSolver solver { get; set; }
            public BminerDevice device { get; set; }
#pragma warning restore IDE1006 // Стили именования
        }
        private class BminerSolver
        {
#pragma warning disable IDE1006 // Стили именования
            public double? solution_rate { get; set; }
#pragma warning restore IDE1006 // Стили именования
        }
        private class BminerDevice
        {
#pragma warning disable IDE1006 // Стили именования
            public int? temperature { get; set; }
            public int? fan_speed { get; set; }
#pragma warning restore IDE1006 // Стили именования
        }
    }
}